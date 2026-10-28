using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// One live native host at a time. Seek drains that host BEFORE applying data, then
// reconstructs at most the target's own host. No display trace or old action task is replayed.
internal static class NativeSeek
{
    internal const string HunterClip="1d5346b085b04635ab3e00b43fe9190c";
    private sealed record Point(SeekNode Node,DirectCheckpoint? Data,string Projection,int ChoicePosition);
    private static readonly SortedDictionary<int,Point> Points=[];
    private static readonly AdvanceRequests Requests=new();
    private static SeekNode[] _plan=[];
    private static Clip _clip=null!;
    private static Task<bool>? _hostTask;
    private static ChoiceScope? _gate,_view;
    private static Task<CardModel[]>? _viewResult;
    private static bool _moving,_restoring,_draining,_stopping,_playing,_viewReady;
    private static int _position,_completedHost,_generation;
    private static int? _seek;
    internal static bool Enabled {get;private set;}
    internal static int Host {get;private set;}
    internal static int Generation=>_generation;
    internal static bool SilentRebuild=>Enabled&&(_restoring||_draining);
    internal static bool Ready=>Enabled&&!_moving&&!_restoring&&!_stopping&&(_hostTask is null||HasView);
    internal static bool HasView=>_view is {Closed:false,Cancelled:false}&&_viewReady;
    internal static bool CanRestore=>Enabled&&_restoring&&_hostTask is null&&!ActionMonitor.Outstanding&&LiveState.Playable;
    internal static bool CanStep=>Enabled&&Points.Count>0&&!_stopping&&!_restoring&&(_position<_plan.Length-1||_moving);
    internal static bool CanBack=>Enabled&&!_stopping&&!_restoring&&(_position>0||_moving);
    internal static bool Stopping=>_stopping;
    internal static bool Restoring=>_restoring;
    internal static int Position=>_position;
    internal static string Pending=>_restoring?" · 正在恢复":_moving?" · 当前效果收尾中":Requests.Count>0?$" · 待前进{Requests.Count}步":"";
    internal static KeyValuePair<int,string>[] Index=>Points.Select(p=>new KeyValuePair<int,string>(p.Key,p.Value.Node.Label)).ToArray();
    internal static void Start(Clip clip)
    {
        Clear();Codec.Require(clip.Id==HunterClip&&clip.Format>=4&&!(clip.Choices??[]).Any(c=>c.Boundary==0),"seek-first-slice-requires-known-hunter-recording");
        Enabled=true;_clip=clip;_plan=SeekPlan.Build(clip,4);Host=0;
    }
    internal static void Clear()
    {
        Enabled=false;_generation++;Points.Clear();Requests.Clear();_plan=[];_hostTask=null;_gate=null;_view=null;_viewResult=null;
        _moving=false;_restoring=false;_draining=false;_stopping=false;_playing=false;_viewReady=false;_position=0;_completedHost=0;Host=0;_seek=null;
    }
    internal static void Control(string command)
    {
        var accepted=false;
        if(command=="stop"&&!_stopping){_stopping=true;_playing=false;Requests.Clear();_seek=null;accepted=true;}
        else if(command=="step"&&CanStep){_playing=false;Requests.Add();accepted=true;}
        else if(command=="toggle"&&!_stopping&&!_restoring)
        {var pause=_playing||Requests.Count>0||_moving;_playing=!pause;if(pause)Requests.Clear();accepted=true;}
        else if(command=="back"&&CanBack)
        {RequestSeek(_moving?_position:Math.Max(0,_position-1));accepted=true;}
        Runtime.Log("native-seek-control",new{Command=command,Accepted=accepted,Node=_position,Host,QueuedSteps=Requests.Count,_playing,_moving,_restoring});
    }
    internal static void RequestSeek(int index)
    {
        Codec.Require(Enabled&&!_stopping&&Points.ContainsKey(index),"seek-target-not-indexed");
        Requests.Clear();_playing=false;_seek=index;
        Runtime.Log("native-seek-request",new{From=_position,Target=index,DefaultPaused=true});
    }
    internal static async Task Run()
    {
        Publish(_plan[0],capture:true);PlayerLoop.Loading=false;
        try
        {
            while(!_stopping)
            {
                PlayerLoop.RequireHealthy();
                if(_seek is {} target){_seek=null;await Restore(target);continue;}
                if(_hostTask is {IsCompleted:true})await FinishHost();
                if(!_moving&&!_restoring&&(_playing||Requests.Count>0))
                {
                    if(_position==_plan.Length-1){_playing=false;Requests.Clear();Runtime.Status="原生 Seek · 节点8/8 · 切片已结束并暂停，可Seek或Stop";}
                    else
                    {
                        if(!_playing)Requests.Take();_moving=true;
                        if(HasView)await AdvanceChoice();
                        else if(_hostTask is null)StartHost(_completedHost+1);
                    }
                }
                await Runtime.NextFrame();
            }
        }
        finally
        {
            _playing=false;Requests.Clear();
            try{if(PlayerLoop.Error is null&&ActionMonitor.Error is null)await Drain();else CancelGate();}
            finally{Runtime.Log("native-seek-closed",new{Node=_position,Host,Generation=_generation,Indexed=Points.Count,PendingHost=_hostTask is not null,DisplayTracePlayed=false});}
        }
    }
    private static void StartHost(int host)
    {
        if(_stopping)return;
        Codec.Require(_hostTask is null&&_gate is null&&host>0&&host<=4,"seek-native-host-context");
        Host=host;_moving=true;_hostTask=PlayerLoop.Execute(_clip,host-1,"native-seek-forward");
        Runtime.Log("native-seek-host-started",new{Host,Generation=_generation,Reconstruction=_restoring,NormalNativeBusiness=true});
    }
    private static async Task FinishHost()
    {
        Codec.Require(_hostTask is not null&&await _hostTask,"seek-native-host-not-completed");
        _hostTask=null;_completedHost=Host;_moving=false;
        Codec.Require(_gate is null&&_view is null&&!ActionMonitor.Outstanding&&LiveState.Playable,"seek-host-not-quiescent");
        Codec.Require(LiveState.Capture()==_clip.Operations[Host-1].After,"seek-live-native-result-or-rng-differs");
        if(!_draining&&!_restoring)Publish(_plan.Single(n=>n.Host==Host&&n.Stable),capture:true);
        Runtime.Log("native-seek-host-verified",new{Host,Generation=_generation,Reconstruction=_restoring,RetiringGeneration=_draining,
            ResultAndRngMatchedOriginal=true,NativeBusinessExecuted=true,LiveStateVerified=true});
    }
    private static void Publish(SeekNode node,bool capture)
    {
        var projection=LiveState.ChoiceProjection();
        if(Points.TryGetValue(node.Index,out var prior))Codec.Require(prior.Projection==projection&&prior.ChoicePosition==ChoiceTape.Position,"seek-reexecution-node-or-rng-differs");
        DirectCheckpoint? data=node.Stable&&capture?new(node.Host,PlayerLoop.EnsureRoster(),route:true,extraRoots:[LiveState.Run.CurrentRoom!]):null;
        Points[node.Index]=new(node,data??prior?.Data,projection,ChoiceTape.Position);_position=node.Index;_moving=false;
        Runtime.Status=$"原生 Seek · 节点 {node.Index}/8 · {node.Label} · "+(_playing?"播放":"暂停");
        ChoiceTape.Display=node.Stable?"":$"原生选择{node.Choice} · 已选{node.Stage} · 决策只读，可浏览";
        Runtime.Log("native-seek-node",new{Node=node.Index,node.Host,node.Choice,node.Stage,node.Anchor,Generation=_generation,
            StateProjection=projection,ChoicePosition=ChoiceTape.Position,DataSnapshot=data is not null,NativeContinuation=true});
    }
    internal static async Task<PlayerChoiceResult> OpenChoice(ChoiceScope source,Player player,uint id)
    {
        Codec.Require(Enabled&&_gate is null&&ReferenceEquals(source.Player,player)&&source.Id==id,"seek-choice-context");
        var generation=_generation;_gate=source;
        Runtime.Log("native-seek-choice-context",new{Host,Generation=generation,Choice=source.Expected!.Sequence,
            RecordedNativeId=source.Expected.NativeId,FreshNativeId=id,OldWaiterResumed=false});
        var view=new ChoiceScope(source.Player,source.Options,source.Frame,source.Generation)
        {Expected=source.Expected,Prefs=source.Prefs,Pile=source.Pile,SourceModel=source.SourceModel,PlaybackEvents=SeekPlan.Events(source.Expected!)};
        _view=view;_viewReady=false;_viewResult=NativeChoice.Show(view);
        await NativeChoice.WaitReady(view);
        Codec.Require(generation==_generation,"seek-stale-choice-generation");_viewReady=true;
        if(!_draining&&!_restoring)Publish(_plan.Single(n=>n.Choice==source.Expected!.Sequence&&n.Stage==0),capture:false);
        try{return await source.Decision.Task;}
        finally{if(ReferenceEquals(_gate,source))_gate=null;}
    }
    internal static void Closed(object ui,bool settled=false)
    {
        if(_view is not {} view||!ReferenceEquals(view.Ui,ui)||view.Cancelled)return;
        view.Closed=true;if(settled)view.UiClosed.TrySetResult(true);
    }
    private static async Task AdvanceChoice()
    {
        var view=_view??throw new InvalidDataException("seek-no-choice-view");
        var source=_gate??throw new InvalidDataException("seek-no-native-choice-gate");
        Codec.Require(_viewReady&&view.EventPosition<view.PlaybackEvents.Length,"seek-choice-event-overrun");
        var action=view.PlaybackEvents[view.EventPosition++];NativeChoice.Apply(view,action);
        if(!view.Closed&&view.EventPosition<view.PlaybackEvents.Length)
        {
            await WaitSelectionMotion(view);NativeChoice.AssertPrefix(view,view.EventPosition);
            if(!_draining&&!_restoring)Publish(_plan.Single(n=>n.Choice==source.Expected!.Sequence&&n.Stage==view.EventPosition),capture:false);
            return;
        }
        Codec.Require(_viewResult is not null,"seek-native-choice-result-task-missing");
        var cards=await _viewResult!;await view.UiClosed.Task;
        var chosen=cards.Select(c=>Array.FindIndex(source.Options,o=>o.Contains(c))).Distinct().ToArray();
        Codec.Require(chosen.SequenceEqual(source.Expected!.Chosen),"seek-native-choice-result-differs");
        _view=null;_viewReady=false;_viewResult=null;_moving=true;
        var native=source.Frame.NativeType switch
        {
            "CombatCard"=>PlayerChoiceResult.FromMutableCombatCards(cards),"DeckCard"=>PlayerChoiceResult.FromMutableDeckCards(cards),
            "Index" when source.Frame.Kind is "FromChooseACardScreen" or "FromChooseABundleScreen"=>PlayerChoiceResult.FromIndex(chosen.Length==0?-1:chosen[0]),
            "Index"=>PlayerChoiceResult.FromIndexes(chosen.ToList()),_=>throw new InvalidDataException("seek-choice-result-type")
        };
        Codec.Require(source.Decision.TrySetResult(native),"seek-duplicate-choice-submission");
        Runtime.Log("native-seek-choice-submitted",new{source.Expected.Sequence,Host,Generation=_generation,DeliveredOnce=true,Reconstruction=_restoring,RetiringGeneration=_draining});
    }
    private static async Task WaitSelectionMotion(ChoiceScope view)
    {
        var at=Stopwatch.GetTimestamp();
        while(view.Ui is NPlayerHand hand&&AccessTools.Field(typeof(NPlayerHand),"_selectedCardScaleTween").GetValue(hand) is Tween tween&&
            GodotObject.IsInstanceValid(tween)&&tween.IsRunning())
        {Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<3,"seek-selection-motion-did-not-finish");await Runtime.NextFrame();}
    }
    private static async Task Drain()
    {
        _draining=true;var at=Stopwatch.GetTimestamp();var hidden=NRun.Instance;var visible=hidden?.Visible??false;if(hidden is not null)hidden.Visible=false;
        try
        {
            while(_hostTask is not null)
            {
                PlayerLoop.RequireHealthy();Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<60,"seek-old-host-did-not-drain");
                if(HasView)await AdvanceChoice();
                if(_hostTask.IsCompleted)await FinishHost();else await Runtime.NextFrame();
            }
            Codec.Require(!ActionMonitor.Outstanding&&LiveState.Playable,"seek-old-generation-not-quiescent");
        }
        finally{_draining=false;if(hidden is not null&&GodotObject.IsInstanceValid(hidden))hidden.Visible=visible;}
    }
    private static async Task Restore(int target)
    {
        var at=Stopwatch.GetTimestamp();var point=Points[target];_restoring=true;Requests.Clear();_playing=false;
        var retiredHosts=_hostTask is null?0:1;
        var run=NRun.Instance!;var visible=run.Visible;run.Visible=false;
        try
        {
            await Drain();var drainedAt=Stopwatch.GetTimestamp();_generation++;
            var anchor=point.Node.Stable?point:Points[point.Node.Anchor];
            Codec.Require(anchor.Data is not null,"seek-stable-anchor-not-built");
            await anchor.Data!.Restore();ActionMonitor.RebaseNativeSeek();_completedHost=anchor.Node.Host;Host=_completedHost;_position=anchor.Node.Index;
            HandPresentation.FinishHighlights(NPlayerHand.Instance!);
            var appliedAt=Stopwatch.GetTimestamp();
            var nativeHosts=0;
            if(!point.Node.Stable)
            {
                StartHost(point.Node.Host);nativeHosts=1;
                while(true)
                {
                    PlayerLoop.RequireHealthy();Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<20,"seek-local-host-reconstruction-timeout");
                    if(_stopping){await Drain();return;}
                    if(HasView)
                    {
                        var sequence=_gate!.Expected!.Sequence;
                        Codec.Require(sequence<=point.Node.Choice,"seek-reconstruction-passed-target-choice");
                        if(sequence<point.Node.Choice)await AdvanceChoice();
                        else
                        {
                            while(_view!.EventPosition<point.Node.Stage)await AdvanceChoice();
                            NativeChoice.AssertPrefix(_view,point.Node.Stage);break;
                        }
                    }
                    if(_hostTask!.IsCompleted)throw new InvalidDataException("seek-host-finished-before-target-choice");
                    await Runtime.NextFrame();
                }
            }
            Codec.Require(LiveState.ChoiceProjection()==point.Projection&&ChoiceTape.Position==point.ChoicePosition,"seek-arrival-state-or-rng-differs");
            _position=target;_moving=false;Runtime.Status=$"原生 Seek · 节点 {target}/8 · 已恢复并暂停";
            ChoiceTape.Display=point.Node.Stable?"":$"原生选择{point.Node.Choice} · 已选{point.Node.Stage} · 已重新建立等待";
            Runtime.Log("native-seek-arrived",new{Target=target,Anchor=anchor.Node.Index,Generation=_generation,LocalNativeHosts=nativeHosts,
                FromBattlePrefixOperations=0,StateAndRngMatched=true,FreshChoiceContext=!point.Node.Stable,DefaultPaused=true,
                OldNativeHostDrainedBeforeDataApply=true,OldTasksRestored=false,DisplayTracePlayed=false,Milliseconds=Stopwatch.GetElapsedTime(at).TotalMilliseconds});
            Runtime.Log("native-seek-arrival-timing",new{Target=target,RetiredNativeHosts=retiredHosts,RebuiltNativeHosts=nativeHosts,
                DrainMilliseconds=Stopwatch.GetElapsedTime(at,drainedAt).TotalMilliseconds,ApplyMilliseconds=Stopwatch.GetElapsedTime(drainedAt,appliedAt).TotalMilliseconds,
                LocalRebuildMilliseconds=Stopwatch.GetElapsedTime(appliedAt).TotalMilliseconds,FromBattlePrefixOperations=0});
        }
        finally{_restoring=false;if(GodotObject.IsInstanceValid(run))run.Visible=visible;}
    }
    private static void CancelGate()
    {
        if(_view is {} view)NativeChoice.Cancel(view);_gate?.Decision.TrySetCanceled();_view=null;_gate=null;_viewReady=false;
    }
}
