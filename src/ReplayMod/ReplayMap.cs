using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class ReplayMap
{
    private sealed record MapView(ActMap Map,ulong Seed,ActModel? IconAct=null);
    private static readonly Dictionary<int,MapView?> Maps=[];
    private static readonly Dictionary<TextureRect,(Color Color,Vector2 Scale)> Ticks=[];
    private static NMapScreen? _screen;
    private static NMapScreen? _openRequested;
    private static Control? _previous,_next;
    private static Polygon2D? _marker;
    private static NMapCircleVfx? _arrival;
    private static int _act;
    private static bool _browsing;
    private static MapView? _iconView;
    private static Dictionary<MapCoord,RunRoomPoint> _targets=[];
    internal static bool Swapping {get;private set;}
    internal static bool Observing=>_browsing&&IsOpen;
    internal static bool IsOpen=>RunPlayback.Active&&NMapScreen.Instance is {IsOpen:true};

    internal static void Install(Harmony h)
    {
        void Patch(Type type,string method,string? prefix=null,string? postfix=null)=>
            h.Patch(AccessTools.DeclaredMethod(type,method)??throw new MissingMethodException(type.Name,method),
                prefix is null?null:new(typeof(ReplayMap),prefix),postfix is null?null:new(typeof(ReplayMap),postfix));
        Patch(typeof(NMapScreen),nameof(NMapScreen.Open),nameof(BeforeOpen),nameof(Opened));
        Patch(typeof(NMapScreen),nameof(NMapScreen.Close),nameof(Closing));
        Patch(typeof(NMapScreen),"RecalculateTravelability",nameof(Recalculate));
        Patch(typeof(NMapPoint),"get_IsTravelable",nameof(Travelable));
        Patch(typeof(NMapPoint),"get_TargetColor",postfix:nameof(PointColor));
        Patch(typeof(NMapPoint),"OnRelease",nameof(Release));
        Patch(typeof(NNormalMapPoint),"_Process",nameof(IdlePoint));
        Patch(typeof(NAncientMapPoint),"_Process",nameof(IdlePoint));
        Patch(typeof(NRun),nameof(NRun.SetCurrentRoom),postfix:nameof(SceneInstalled));
        var viewInputs=new HarmonyMethod(typeof(ReplayMap),nameof(MapViewInputs));
        h.Patch(AccessTools.DeclaredMethod(typeof(NMapBg),"OnVisibilityChanged"),transpiler:viewInputs);
        h.Patch(AccessTools.DeclaredMethod(typeof(NBossMapPoint),nameof(NBossMapPoint.Create)),transpiler:viewInputs);
        h.Patch(AccessTools.DeclaredMethod(typeof(NBossMapPoint),nameof(NBossMapPoint._Ready)),transpiler:viewInputs);
        foreach(var type in new[]{typeof(NAncientMapPoint),typeof(NNormalMapPoint)})
            foreach(var method in new[]{"_Ready","OnSelected","AnimUnhover","AnimPressDown"})
                h.Patch(AccessTools.DeclaredMethod(type,method),transpiler:viewInputs);
        Patch(typeof(NAncientMapPoint),"RefreshColorInstantly",postfix:nameof(RefreshAncientOutline));
        h.Patch(AccessTools.DeclaredMethod(typeof(NBossMapPoint),"RefreshColorInstantly"),
            transpiler:new(typeof(ReplayMap),nameof(BossColorInput)));
        h.Patch(AccessTools.AsyncMoveNext(AccessTools.DeclaredMethod(typeof(NMapScreen),nameof(NMapScreen.TravelToMapCoord))),
            transpiler:new(typeof(ReplayMap),nameof(TravelColorInput)));
    }

    // The visible parchment and its nodes use the same act while browsing.
    // The live run and its native travel logic keep their original act and map.
    private static IEnumerable<CodeInstruction> MapViewInputs(IEnumerable<CodeInstruction> instructions)
    {
        var act=AccessTools.PropertyGetter(typeof(IRunState),nameof(IRunState.Act));
        var map=AccessTools.PropertyGetter(typeof(IRunState),nameof(IRunState.Map));
        foreach(var instruction in instructions)
        {
            var replacement=instruction.Calls(act)?nameof(DisplayedAct):instruction.Calls(map)?nameof(IconMap):null;
            if(replacement is not null)
            {
                instruction.opcode=OpCodes.Call;
                instruction.operand=AccessTools.DeclaredMethod(typeof(ReplayMap),replacement);
            }
            yield return instruction;
        }
    }
    private static ActModel DisplayedAct(IRunState run)=>_iconView?.IconAct??
        (_screen is not null&&_act!=run.CurrentActIndex?Maps.GetValueOrDefault(_act)?.IconAct:null)??run.Act;
    private static ActMap IconMap(IRunState run)=>_iconView?.Map??run.Map;
    private static IEnumerable<CodeInstruction> TravelColorInput(IEnumerable<CodeInstruction> instructions)
    {
        var setter=AccessTools.PropertySetter(typeof(CanvasItem),nameof(CanvasItem.Modulate));
        foreach(var instruction in instructions)
        {
            if(instruction.Calls(setter))
            {
                instruction.opcode=OpCodes.Call;
                instruction.operand=AccessTools.DeclaredMethod(typeof(ReplayMap),nameof(SetTravelTickColor));
            }
            yield return instruction;
        }
    }
    private static void SetTravelTickColor(CanvasItem item,Color color)
    {
        if(item is not TextureRect tick||!Ticks.ContainsKey(tick))item.Modulate=color;
    }
    private static IEnumerable<CodeInstruction> BossColorInput(IEnumerable<CodeInstruction> instructions)
    {
        var state=AccessTools.PropertyGetter(typeof(NMapPoint),nameof(NMapPoint.State));
        foreach(var instruction in instructions)
        {
            if(instruction.Calls(state))
            {
                instruction.opcode=OpCodes.Call;
                instruction.operand=AccessTools.DeclaredMethod(typeof(ReplayMap),nameof(DisplayedBossState));
            }
            yield return instruction;
        }
    }
    private static MapPointState DisplayedBossState(NMapPoint point)=>
        Owns(point)&&Target(point.Point.coord) is not null?MapPointState.Traveled:point.State;
    private static void RefreshAncientOutline(NAncientMapPoint __instance)
    {
        if(!Owns(__instance))return;
        var outline=(TextureRect)RunPatches.Field(__instance,"_outline")!;
        outline.Modulate=DisplayedAct(LiveState.Run).MapBgColor;
    }

    internal static void Clear()
    {
        _screen=null;_openRequested=null;_previous=null;_next=null;_marker=null;_arrival=null;
        _browsing=false;_iconView=null;Swapping=false;Ticks.Clear();Maps.Clear();_targets.Clear();
    }
    private static void SceneInstalled(Control? __0)
    {
        if(__0 is not null&&RunPlayback.RoomJumping)Runtime.Guard(RunPlayback.RoomSceneInstalled);
    }
    private static bool BeforeOpen(NMapScreen __instance,bool __0,ref NMapScreen __result)
    {
        if(!RunPlayback.Active||!__0||__instance.IsOpen||RunPlayback.CanOpenObserverMap)return true;
        if(RunPlayback.MapBrowsingAvailable&&!NativeSeek.DeathTransition)
        {
            RunPlayback.PauseForMap();_openRequested=__instance;
            Runtime.Log("replay-map-waiting-for-room-boundary",new{Auto=false});
        }
        __result=__instance;return false;
    }
    internal static void Pump()
    {
        if(_openRequested is not {} requested)return;
        if(!GodotObject.IsInstanceValid(requested)||RunPlayback.Stopping||PlayerLoop.TearingDown)
        {_openRequested=null;return;}
        if(RunPlayback.CanOpenObserverMap){_openRequested=null;requested.Open(isOpenedFromTopBar:true);}
    }
    private static void Opened(NMapScreen __instance,bool __0)
    {
        if(!RunPlayback.Active||RunPlayback.RoomJumping||!__instance.IsOpen)return;
        Runtime.Guard(()=>
        {
            if(__0)RunPlayback.PauseForMap();
            if(!ReferenceEquals(_screen,__instance))
            {
                _screen=__instance;_act=LiveState.Run.CurrentActIndex;IndexTargets();
                Paint();
            }
            _browsing|=__0;
            Runtime.Log("replay-map-opened",new{Act=_act,Observer=__0,Nodes=_targets.Count,Jumpable=_targets.Values.Count(point=>point.StartSource is not null),
                Auto=RunPlayback.NonCombat?RunPlayback.Auto:NativeSeek.Auto});
            Ui.Refresh();
        });
    }
    private static bool Closing(NMapScreen __instance)
    {
        if(ReferenceEquals(_screen,__instance)&&RunPlayback.RoomJumpSelecting&&!RunPlayback.Stopping)return false;
        if(ReferenceEquals(_screen,__instance))Runtime.Guard(RestoreNative);
        return true;
    }
    private static bool Recalculate(NMapScreen __instance)=>!ReferenceEquals(_screen,__instance)||!__instance.IsOpen;
    private static bool Travelable(NMapPoint __instance,ref bool __result)
    {
        if(!Owns(__instance))return true;
        __result=Target(__instance.Point.coord)?.StartSource is not null;return false;
    }
    private static void PointColor(NMapPoint __instance,ref Color __result)
    {
        if(Owns(__instance)&&Target(__instance.Point.coord) is not null)
            __result=(Color)AccessTools.Property(__instance.GetType(),"TraveledColor").GetValue(__instance)!;
    }
    private static bool IdlePoint()=>_screen is not {IsOpen:true};
    private static bool Release(NMapPoint __instance)
    {
        if(!Owns(__instance))return true;
        if(_screen is {IsOpen:true,IsTraveling:false} screen&&
            AccessTools.Method(typeof(NMapPoint),"IsInputAllowed").Invoke(__instance,null) is true&&
            Target(__instance.Point.coord) is {} target)
            Runtime.Guard(()=>RunPlayback.RequestRoomJump(target,screen,__instance));
        return false;
    }
    private static bool Owns(NMapPoint point)=>_screen is {IsOpen:true} screen&&
        ReferenceEquals(RunPatches.Field(point,"_screen"),screen);
    private static RunRoomPoint? Target(MapCoord coord)=>_targets.GetValueOrDefault(coord);
    private static Dictionary<MapCoord,NMapPoint> Points(NMapScreen screen)=>
        (Dictionary<MapCoord,NMapPoint>)RunPatches.Field(screen,"_mapPointDictionary")!;
    internal static NMapPoint TravelPoint(NMapScreen screen,ActMap liveMap,MapCoord coord)
    {
        Codec.Require(ReferenceEquals(RunPatches.Field(screen,"_map"),liveMap),"run-native-travel-map-mismatch");
        return Points(screen)[coord];
    }

    private static MapView? MapFor(int act)
    {
        var run=LiveState.Run;
        if(act==run.CurrentActIndex)return new(run.Map,run.Rng.Seed);
        if(Maps.TryGetValue(act,out var cached))return cached;
        var source=RunPlayback.MapSource(act);
        if(source is null)return Maps[act]=null;
        try
        {
            var clip=Runtime.ReadClip(source.Clip!);
            var parsed=JsonSerializationUtility.FromJson<SerializableRun>(clip.Checkpoint);
            if(!parsed.Success||parsed.SaveData is not {} save||save.CurrentActIndex!=act||
                save.Acts[act].SavedMap is not {} map)return Maps[act]=null;
            return Maps[act]=new(new SavedActMap(map),run.Rng.Seed,ActModel.FromSave(save.Acts[act]));
        }
        catch(Exception ex) when(ex is InvalidDataException or IOException)
        {
            Runtime.Log("replay-map-act-unavailable",new{Act=act,Reason=ex.Message});
            return Maps[act]=null;
        }
    }
    private static void Flip(int offset)
    {
        if(RunPlayback.RoomJumping||_screen is not {IsOpen:true} screen||MapFor(_act+offset) is not {} view)return;
        RunPlayback.PauseForMap();_browsing=true;
        ClearDecorations();Ticks.Clear();_act+=offset;
        IndexTargets();
        SetMap(screen,view);
        var map=(Control)RunPatches.Field(screen,"_mapContainer")!;
        map.Position=new(map.Position.X,offset>0?-600f:1800f);
        AccessTools.Field(typeof(NMapScreen),"_targetDragPos").SetValue(screen,map.Position);
        Paint();
        var icons=view.IconAct??LiveState.Run.Act;
        Runtime.Log("replay-map-act-browsed",new{Act=_act,ActualAct=LiveState.Run.CurrentActIndex,RoomUnchanged=true,
            Boss=icons.BossEncounter.Id.ToString(),Ancient=icons.Ancient.Id.ToString()});
    }
    private static void SetMap(NMapScreen screen,MapView view)
    {
        var played=AccessTools.Field(typeof(NMapScreen),"_hasPlayedAnimation");
        var wasPlayed=played.GetValue(screen);
        Swapping=true;_iconView=view.IconAct is not null?view:null;
        try
        {
            screen.SetMap(view.Map,view.Seed,clearDrawings:false);
            var background=RunPatches.Field(screen,"_mapBgContainer")!;
            AccessTools.DeclaredMethod(typeof(NMapBg),"OnVisibilityChanged").Invoke(background,null);
        }
        finally{played.SetValue(screen,wasPlayed);_iconView=null;Swapping=false;}
    }

    private static void IndexTargets()
    {
        _targets.Clear();
        foreach(var point in RunPlayback.RoomPoints.Where(p=>p.Visit.Act==_act))
            _targets[new(point.Visit.Col,point.Visit.Row)]=point;
    }
    private static void Paint()
    {
        var screen=_screen!;
        var points=Points(screen);
        foreach(var point in points.Values)
        {
            // Traveled in the live map also places permanent circles. The
            // observer uses the same colors with a separate arrival cue.
            point.State=MapPointState.Untravelable;
            if(point is NNormalMapPoint&&RunPatches.Field(point,"_circleVfx") is NMapCircleVfx circle)
            {
                circle.QueueFree();
                AccessTools.Field(typeof(NNormalMapPoint),"_circleVfx").SetValue(point,null);
            }
            point.RefreshVisualsInstantly();
        }
        var selected=RunPlayback.RoomPoints.Where(p=>p.Visit.Act==_act)
            .Select(p=>new MapCoord(p.Visit.Col,p.Visit.Row)).Distinct().ToArray();
        var route=selected.Zip(selected.Skip(1)).ToHashSet();
        var untraveled=DisplayedAct(LiveState.Run).MapUntraveledColor;
        var paths=(Dictionary<(MapCoord,MapCoord),IReadOnlyList<TextureRect>>)RunPatches.Field(screen,"_paths")!;
        foreach(var (pair,path) in paths)
            foreach(var tick in path)
            {
                Ticks.TryAdd(tick,(tick.Modulate,tick.Scale));
                tick.Modulate=route.Contains(pair)?new(.24f,.17f,.13f,.96f):untraveled;
            }
        ((NMapMarker)RunPatches.Field(screen,"_marker")!).Hide();
        screen.Drawings.Visible=_act==LiveState.Run.CurrentActIndex;
        var map=(ActMap)RunPatches.Field(screen,"_map")!;
        if(_act>0&&RunPlayback.MapSource(_act-1) is not null)_previous=Arrow(points[map.StartingMapPoint.coord],-1);
        if(_act+1<LiveState.Run.Acts.Count&&RunPlayback.MapSource(_act+1) is not null)
            _next=Arrow(points[(map.SecondBossMapPoint??map.BossMapPoint).coord],1);
        UpdateMarker(animate:false);
    }
    private static Button Arrow(NMapPoint end,int direction)
    {
        var button=new Button{Flat=true,TooltipText=$"浏览第{_act+direction+1}幕",Size=new(106,78),
            MouseDefaultCursorShape=Control.CursorShape.PointingHand,ZIndex=10};
        Vector2[] arrow=[new(0,-29),new(35,3),new(13,3),new(13,28),new(-13,28),new(-13,3),new(-35,3)];
        var shape=arrow.Select(point=>new Vector2(point.X,point.Y*direction)).ToArray();
        var gold=new Color(.89f,.58f,.19f);
        var icon=new Polygon2D{Polygon=shape,Color=gold,Position=new(53,39),Antialiased=true};
        icon.AddChild(new Line2D{Points=shape,Closed=true,Width=4,DefaultColor=new(.25f,.14f,.07f),Antialiased=true});
        button.AddChild(icon);
        void Highlight()=>icon.Color=button.IsHovered()||button.HasFocus()?new(1,.77f,.29f):gold;
        button.MouseEntered+=Highlight;button.MouseExited+=Highlight;
        button.FocusEntered+=Highlight;button.FocusExited+=Highlight;
        button.Pressed+=()=>Runtime.Guard(()=>{SfxCmd.Play("event:/sfx/ui/clicks/ui_click");Flip(direction);});
        end.GetParent().AddChild(button);
        var y=direction>0?-button.Size.Y-64:end.Size.Y+38;
        button.Position=end.Position+new Vector2((end.Size.X-button.Size.X)*.5f,y);
        return button;
    }
    internal static async Task ShowSelection(NMapScreen screen,NMapPoint point)
    {
        var ink=PlayInk(point);
        if(NMapNodeSelectVfx.Create(point is NBossMapPoint?2f:point is NAncientMapPoint?1.5f:1f) is {} selected)
        {
            point.AddChild(selected);selected.Position+=point.PivotOffset;
        }
        SfxCmd.Play("event:/sfx/ui/map/map_select");
        Runtime.Log("replay-map-target-feedback-started",new{Act=_act,Row=point.Point.coord.row,Col=point.Point.coord.col,
            Ink=ink,MapVisible=screen.IsVisibleInTree(),ActualAct=LiveState.Run.CurrentActIndex});
        await Runtime.Game.ToSignal(screen.GetTree().CreateTimer(.18),SceneTreeTimer.SignalName.Timeout);
    }
    internal static void Arrived()
    {
        if(_screen is {IsOpen:true})UpdateMarker(animate:true);
    }
    private static void UpdateMarker(bool animate)
    {
        var screen=_screen!;
        if(_marker is not null){_marker.QueueFree();_marker=null;}
        if(_arrival is not null&&GodotObject.IsInstanceValid(_arrival)){_arrival.QueueFree();_arrival=null;}
        var run=LiveState.Run;
        if(_act!=run.CurrentActIndex||run.CurrentMapCoord is not {} coord||!Points(screen).TryGetValue(coord,out var point))return;
        var marker=_marker=new Polygon2D{Color=new(.81f,.16f,.13f),Polygon=[new(-10,-7),new(10,-7),new(0,8)],
            Position=point.Position+point.PivotOffset+new Vector2(0,-29),ZIndex=5};
        point.GetParent().AddChild(marker);
        if(animate)PlayInk(point);
    }
    private static bool PlayInk(NMapPoint point)
    {
        if(_arrival is not null&&GodotObject.IsInstanceValid(_arrival)){_arrival.Hide();_arrival.QueueFree();}
        _arrival=null;
        if(point is not NNormalMapPoint||NMapCircleVfx.Create(LiveState.Run,point.Point.coord,playAnim:true) is not {} circle)return false;
        _arrival=circle;point.AddChild(circle);circle.Position+=point.PivotOffset;
        var tween=circle.CreateTween();tween.TweenProperty(circle,"modulate:a",0f,.35).SetDelay(.75);
        tween.TweenCallback(Callable.From(circle.QueueFree));
        return true;
    }

    private static void ClearDecorations()
    {
        foreach(var node in new Node?[]{_previous,_next,_marker,_arrival})
            if(node is CanvasItem item&&GodotObject.IsInstanceValid(item)){item.Hide();item.QueueFree();}
        _previous=null;_next=null;_marker=null;_arrival=null;
    }
    internal static void RestoreNative()
    {
        if(_screen is not {} screen)return;
        _screen=null;_browsing=false;ClearDecorations();_targets.Clear();
        if(RunManager.Instance.DebugOnlyGetState() is not {} run){Ticks.Clear();return;}
        if(_act!=run.CurrentActIndex)SetMap(screen,new(run.Map,run.Rng.Seed));
        else
        {
            foreach(var (tick,(color,scale)) in Ticks)
                if(GodotObject.IsInstanceValid(tick)){tick.Modulate=color;tick.Scale=scale;}
            AccessTools.Method(typeof(NMapScreen),"RecalculateTravelability").Invoke(screen,null);
            screen.RefreshAllPointVisuals();
        }
        Ticks.Clear();screen.Drawings.Show();
        var marker=(NMapMarker)RunPatches.Field(screen,"_marker")!;
        marker.ResetMapPoint();
        if(run.CurrentMapCoord is {} coord&&Points(screen).ContainsKey(coord))screen.InitMarker(coord);
    }
}
