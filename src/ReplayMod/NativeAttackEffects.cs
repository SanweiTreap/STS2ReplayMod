using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

namespace ReplayMod;

// These attacks put parts in different containers. Stamp the root before native
// attachment can be deferred, and give each detached part the same owner.
internal static class NativeAttackEffects
{
    private sealed record Owner(NCombatRoom Room,int Generation);
    private static readonly ConditionalWeakTable<CanvasItem,Owner> Owners=new();

    internal static void Install(Harmony harmony)
    {
        HarmonyMethod Hook(string name)=>new(typeof(NativeAttackEffects),name);
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NSpikeSplashVfx),nameof(NSpikeSplashVfx.Create),[typeof(Creature),typeof(VfxColor)]),
            postfix:Hook(nameof(SpikeSplashCreated)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NSpikeSplashVfx),nameof(NSpikeSplashVfx._Ready),[]),
            transpiler:Hook(nameof(BindSpikeParts)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(VfxCmd),nameof(VfxCmd.PlayVfx),[typeof(Vector2),typeof(string),typeof(Control)]),
            transpiler:Hook(nameof(BindVfxRoot)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NVineShamblerVinesVfx),nameof(NVineShamblerVinesVfx._Ready),[]),
            postfix:Hook(nameof(VinesReady)));
    }

    private static void SpikeSplashCreated(NSpikeSplashVfx? __result)
    {if(__result is not null)RegisterRoot(__result);}

    private static void RegisterRoot(CanvasItem root)
    {
        if(!NativeSeek.Enabled||NCombatRoom.Instance is not {} room)return;
        var owner=Owners.GetValue(root,_=>new(room,NativeSeek.Generation));
        Register(owner,root);
    }

    private static void Register(Owner owner,CanvasItem node)
    {
        var current=NativeSeek.Enabled&&owner.Generation==NativeSeek.Generation&&ReferenceEquals(owner.Room,NCombatRoom.Instance);
        if(current)NativeDisplayTrace.TrackNativeAttackEffect(owner.Room,node);
        if(!current||NativeSeek.RetiringPresentation)Retire(node);
    }

    internal static bool Retire(CanvasItem node)
    {
        // Keep the native tweens and Spine completion callbacks alive. Ground
        // spikes tween Modulate.a, so their independent SelfModulate masks them.
        var spike=node is NBgGroundSpikeVfx;
        var tint=spike?node.SelfModulate:node.Modulate;
        if(tint.A==0f)return false;
        tint.A=0f;
        if(spike)node.SelfModulate=tint;else node.Modulate=tint;
        return true;
    }

    private static void AttachVfx(Node parent,Node? child)
    {
        if(child is NVineShamblerVinesVfx vines)RegisterRoot(vines);
        parent.AddChildSafely(child);
    }

    private static void AttachSpike(Node parent,Node? child,NSpikeSplashVfx splash)
    {
        if(child is NBgGroundSpikeVfx spike&&Owners.TryGetValue(splash,out var owner))Register(owner,spike);
        parent.AddChildSafely(child);
    }

    private static void VinesReady(NVineShamblerVinesVfx __instance,Node2D ____backVinesNode)
    {
        if(Owners.TryGetValue(__instance,out var owner))Register(owner,____backVinesNode);
    }

    private static readonly System.Reflection.MethodInfo NativeAttach=AccessTools.DeclaredMethod(typeof(GodotTreeExtensions),nameof(GodotTreeExtensions.AddChildSafely),[typeof(Node),typeof(Node)]);

    private static IEnumerable<CodeInstruction> BindVfxRoot(IEnumerable<CodeInstruction> source)
    {
        var instructions=source.ToArray();
        Codec.Require(instructions.Count(instruction=>instruction.Calls(NativeAttach))==1,"vine-root-attach-site-mismatch");
        return instructions.MethodReplacer(NativeAttach,AccessTools.DeclaredMethod(typeof(NativeAttackEffects),nameof(AttachVfx)));
    }

    private static IEnumerable<CodeInstruction> BindSpikeParts(IEnumerable<CodeInstruction> source)
    {
        var instructions=source.ToArray();
        Codec.Require(instructions.Count(instruction=>instruction.Calls(NativeAttach))==4,"spike-parts-attach-sites-mismatch");
        var attach=AccessTools.DeclaredMethod(typeof(NativeAttackEffects),nameof(AttachSpike));
        foreach(var instruction in instructions)
        {
            if(!instruction.Calls(NativeAttach)){yield return instruction;continue;}
            var loadRoot=new CodeInstruction(OpCodes.Ldarg_0);
            loadRoot.labels.AddRange(instruction.labels);
            loadRoot.blocks.AddRange(instruction.blocks);
            yield return loadRoot;
            yield return new CodeInstruction(OpCodes.Call,attach);
        }
    }
}
