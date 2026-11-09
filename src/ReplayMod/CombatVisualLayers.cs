using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ReplayMod;

// Keep the physical native parents tied to their picture. A reference captured
// before navigation can still receive native cleanup, but can no longer draw.
internal static class CombatVisualLayers
{
    private sealed record Pair(int Generation,Control Front,Control Back)
    {
        internal Control At(bool front)=>front?Front:Back;
        internal bool Retired {get;set;}
    }
    private sealed class RoomLayers
    {
        internal Pair Current;
        internal readonly Dictionary<int,Pair> Pictures;
        internal RoomLayers(Pair original)
        {
            Current=original;Pictures=new(){{original.Generation,original}};
        }
    }
    private sealed record Layer(RoomLayers Room,Pair Picture,bool Front);
    private static readonly ConditionalWeakTable<NCombatRoom,RoomLayers> Rooms=new();
    private static readonly ConditionalWeakTable<Node,Layer> Parents=new();
    private static readonly ConditionalWeakTable<Node,Layer> PendingParents=new();
    private static readonly AsyncLocal<Layer?> CueOrigin=new();
    internal static bool RetiredCue=>CueOrigin.Value?.Picture.Retired==true;
    private static readonly System.Reflection.FieldInfo FrontField=AccessTools.Field(typeof(NCombatRoom),"<CombatVfxContainer>k__BackingField");
    private static readonly System.Reflection.FieldInfo BackField=AccessTools.Field(typeof(NCombatRoom),"<BackCombatVfxContainer>k__BackingField");
    private static readonly System.Reflection.FieldInfo DoomOverlayInstance=AccessTools.Field(typeof(NDoomOverlayVfx),"_instance");

    internal static void Install(Harmony harmony)
    {
        HarmonyMethod Hook(string method)=>new(typeof(CombatVisualLayers),method);
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NCombatRoom),"_Ready",[]),postfix:Hook(nameof(RoomReady)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(NCombatRoom),nameof(NCombatRoom.CombatVfxContainer)),postfix:Hook(nameof(ResolveGetter)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(NCombatRoom),nameof(NCombatRoom.BackCombatVfxContainer)),postfix:Hook(nameof(ResolveGetter)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(GodotTreeExtensions),nameof(GodotTreeExtensions.AddChildSafely),[typeof(Node),typeof(Node)]),
            prefix:Hook(nameof(ResolveAttach)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NDoomOverlayVfx),"_Ready",[]),prefix:Hook(nameof(AllowDoomOverlayReady)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NDoomOverlayVfx),"_ExitTree",[]),postfix:Hook(nameof(DoomOverlayExited)));
        foreach(var type in new[]{typeof(NSpikeSplashVfx),typeof(NHellraiserVfx),typeof(NRollingBoulderVfx),typeof(NVfxProjectileHandler)})
            harmony.Patch(AccessTools.DeclaredMethod(type,"_Ready",[]),prefix:Hook(nameof(EnterCue)),finalizer:Hook(nameof(ExitCue)));
        foreach(var type in new[]{typeof(NVineShamblerVinesVfx),typeof(NPowerUpVfx)})
            harmony.Patch(AccessTools.DeclaredMethod(type,"_Ready",[]),transpiler:Hook(nameof(BindReparent)));
        var boulder=AccessTools.DeclaredMethod(typeof(NRollingBoulderVfx),"PlayAnim");
        var machine=boulder.GetCustomAttributes(typeof(AsyncStateMachineAttribute),false).Cast<AsyncStateMachineAttribute>().Single().StateMachineType;
        harmony.Patch(AccessTools.DeclaredMethod(machine,"MoveNext"),transpiler:Hook(nameof(BindReparent)));
    }

    private static void RoomReady(NCombatRoom __instance)
    {
        if(NativeSeek.Enabled)EnsureRoom(__instance,NativeSeek.Generation);
    }

    private static RoomLayers EnsureRoom(NCombatRoom room,int generation)=>Rooms.GetValue(room,native=>
        {
            var initial=new Pair(generation,native.CombatVfxContainer,native.BackCombatVfxContainer);
            var history=new RoomLayers(initial);
            Register(history,initial);
            return history;
        });

    internal static void Rotate(NCombatRoom room,int sourceGeneration,int targetGeneration)
    {
        var layers=EnsureRoom(room,sourceGeneration);
        var old=layers.Current;
        Codec.Require(old.Generation==sourceGeneration&&targetGeneration>sourceGeneration,"combat-picture-generation-mismatch");
        var fresh=new Pair(targetGeneration,EmptySibling(old.Front),EmptySibling(old.Back));
        Retire(old);
        Register(layers,fresh);
        layers.Pictures.Add(targetGeneration,fresh);
        layers.Current=fresh;
        // Native code can inline these getters. Update the backing fields too,
        // so unpatched forward callers still attach to the actual new pair.
        FrontField.SetValue(room,fresh.Front);
        BackField.SetValue(room,fresh.Back);
        Runtime.Log("native-combat-picture-rotated",new{Source=sourceGeneration,Target=targetGeneration,OldFront=old.Front.GetInstanceId(),
            OldBack=old.Back.GetInstanceId(),NewFront=fresh.Front.GetInstanceId(),NewBack=fresh.Back.GetInstanceId(),
            RetiredPairs=layers.Pictures.Count-1,NativeRoomKept=true});
    }

    private static void Register(RoomLayers room,Pair picture)
    {
        Parents.Add(picture.Front,new(room,picture,true));
        Parents.Add(picture.Back,new(room,picture,false));
    }

    private static Control EmptySibling(Control template)
    {
        var parent=template.GetParent()!;
        var fresh=new Control{MouseFilter=template.MouseFilter,ClipContents=template.ClipContents,
            GrowHorizontal=template.GrowHorizontal,GrowVertical=template.GrowVertical,
            ZIndex=template.ZIndex,ZAsRelative=template.ZAsRelative,VisibilityLayer=template.VisibilityLayer,
            Scale=template.Scale,Rotation=template.Rotation,PivotOffset=template.PivotOffset};
        parent.AddChild(fresh);
        parent.MoveChild(fresh,template.GetIndex()+1);
        foreach(var side in new[]{Side.Left,Side.Top,Side.Right,Side.Bottom})
            fresh.SetAnchorAndOffset(side,template.GetAnchor(side),template.GetOffset(side));
        return fresh;
    }

    private static void Retire(Pair picture)
    {
        picture.Retired=true;
        RetireDoomOverlay(picture);
        foreach(var root in new[]{picture.Front,picture.Back})
        {
            root.Hide();
            // Top-level canvas items can escape the inherited canvas state.
            // Keep native processing and completion callbacks in the tree.
            foreach(var child in root.GetChildren())MaskDetachedCanvas(child);
            root.ChildEnteredTree+=MaskDetachedCanvas;
        }
    }

    private static void RetireDoomOverlay(Pair picture)
    {
        // The native singleton reuses its overlay within one picture. End that
        // lifetime before the next picture can ask for the same singleton.
        if(DoomOverlayInstance.GetValue(null) is not NDoomOverlayVfx overlay||!ReferenceEquals(LayerOf(overlay)?.Picture,picture))return;
        DoomOverlayInstance.SetValue(null,null);
        if(overlay.GetParent() is {} parent)
        {
            parent.RemoveChild(overlay); // Native _ExitTree stops the old tween and its singleton reset callback.
            overlay.QueueFreeSafely();
        }
        // An AddChild already deferred to the old layer is handled in _Ready.
        Runtime.Log("native-doom-overlay-retired",new{picture.Generation});
    }

    private static bool AllowDoomOverlayReady(NDoomOverlayVfx __instance)
    {
        if(LayerOf(__instance) is not {} layer)return true;
        if(layer.Picture.Retired)
        {
            __instance.QueueFreeSafely();
            return false;
        }
        Runtime.Log("native-doom-overlay-mounted",new{layer.Picture.Generation});
        return true;
    }

    private static void DoomOverlayExited(NDoomOverlayVfx __instance)
    {
        // Stop can exit the room without rotating its layers.
        if(LayerOf(__instance) is not null&&ReferenceEquals(DoomOverlayInstance.GetValue(null),__instance))
            DoomOverlayInstance.SetValue(null,null);
    }

    private static void MaskDetachedCanvas(Node node)
    {
        if(node is CanvasItem {TopLevel:true} canvas)canvas.Hide();
        if(node is CanvasLayer layer)layer.Hide();
        foreach(var child in node.GetChildren())MaskDetachedCanvas(child);
    }

    private static Layer? LayerOf(Node? node)
    {
        while(node is not null&&GodotObject.IsInstanceValid(node))
        {
            if(Parents.TryGetValue(node,out var layer))return layer;
            var parent=node.GetParent();
            if(parent is null&&PendingParents.TryGetValue(node,out var pending))return pending;
            node=parent;
        }
        return null;
    }

    private static Pair OriginFor(Layer destination,Layer? source)
    {
        // An explicit old parent always wins over an ambient current callback.
        if(destination.Picture.Retired)return destination.Picture;
        var origin=source??CueOrigin.Value;
        if(origin is not null&&ReferenceEquals(origin.Room,destination.Room))return origin.Picture;
        return NativePhaseRetirement.VisualFlowGeneration is {} generation&&
            destination.Room.Pictures.TryGetValue(generation,out var earlier)&&earlier.Retired?earlier:destination.Picture;
    }

    internal static bool IsRetiredSource(Node node)
    {
        if(!NativeSeek.Enabled)return false;
        if(LayerOf(node)?.Picture.Retired==true||RetiredCue)return true;
        return NativePhaseRetirement.VisualFlowGeneration is {} generation&&generation<NativeSeek.Generation&&
            NCombatRoom.Instance is {} room&&Rooms.TryGetValue(room,out var layers)&&
            layers.Pictures.TryGetValue(generation,out var picture)&&picture.Retired;
    }

    internal static bool OwnsRoomEffect(NCombatRoom room,Node node)=>Rooms.TryGetValue(room,out var layers)&&
        LayerOf(node) is {} layer&&ReferenceEquals(layers,layer.Room);

    private static void ResolveGetter(ref Control __result)
    {
        if(__result is not null&&Parents.TryGetValue(__result,out var layer))__result=OriginFor(layer,null).At(layer.Front);
    }
    private static void ResolveAttach(ref Node __0,Node? __1)
    {
        if(__1 is null||!Parents.TryGetValue(__0,out var layer))return;
        var target=OriginFor(layer,LayerOf(__1));
        __0=target.At(layer.Front);
        PendingParents.Remove(__1);
        PendingParents.Add(__1,new(layer.Room,target,layer.Front));
        if(target.Retired)MaskDetachedCanvas(__1);
    }
    private static void EnterCue(Node __instance,out Layer? __state)
    {
        __state=CueOrigin.Value;
        if(LayerOf(__instance) is {} layer)CueOrigin.Value=layer;
    }
    private static Exception? ExitCue(Exception? __exception,Layer? __state)
    {CueOrigin.Value=__state;return __exception;}

    private static void ReparentVisual(Node child,Node parent,bool keepGlobalTransform)
    {
        if(Parents.TryGetValue(parent,out var layer))parent=OriginFor(layer,LayerOf(child)).At(layer.Front);
        child.Reparent(parent,keepGlobalTransform);
    }

    private static IEnumerable<CodeInstruction> BindReparent(IEnumerable<CodeInstruction> source)
    {
        var native=AccessTools.DeclaredMethod(typeof(Node),nameof(Node.Reparent),[typeof(Node),typeof(bool)]);
        var instructions=source.ToArray();
        Codec.Require(instructions.Count(instruction=>instruction.Calls(native))==1,"combat-picture-reparent-site-mismatch");
        return instructions.MethodReplacer(native,AccessTools.DeclaredMethod(typeof(CombatVisualLayers),nameof(ReparentVisual)));
    }
}
