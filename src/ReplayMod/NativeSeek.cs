using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// Only whole-operation boundaries are restored. Fine phases live in the current native
// action; ahead submissions reserve existing requests and never count as completion.
internal static class NativeSeek
{
    internal const string HunterClip="1d5346b085b04635ab3e00b43fe9190c";
    internal const int SliceHosts=22;
    private static readonly SortedDictionary<int,DirectCheckpoint> Points=[];
    private static readonly Dictionary<int,int> Attempts=[];
    private static readonly HashSet<int> CancelledAhead=[];
    private static Clip _clip=null!;
    private static OperationRequests _requests=null!;
    private static ChoiceScope? _choice;
    private static bool _choiceReady,_choiceMoving,_retiring,_restoring,_stopping;
    private static int _completed,_generation;
    private static int? _seek;
    private static string _phase="";
    internal static bool Enabled {get;private set;}
    internal static int Host {get;private set;}
    internal static int Generation=>_generation;
    internal static bool HasView=>_choice is {Closed:false,Cancelled:false}&&_choiceReady&&!_choiceMoving;
    internal static bool Ready=>Enabled&&!_stopping&&!_restoring&&(!InFlight||HasView);
    internal static bool InFlight=>Host>_completed;
    internal static bool CanRestore=>Enabled&&_restoring&&!ActionMonitor.Outstanding&&LiveState.Playable;
    internal static bool CanStep=>Enabled&&!_stopping&&!_restoring&&_seek is null&&_requests.Stage<_requests.Route[^1].Stage;
    internal static bool CanBack=>Enabled&&!_stopping&&!_restoring&&_seek is null&&(InFlight||_completed>0);
    internal static bool Stopping=>_stopping;
    internal static bool Restoring=>_restoring;
    internal static bool Running=>Enabled&&_requests.Running;
    internal static bool Auto=>Enabled&&_requests.Auto;
    internal static bool Covered=>Enabled&&(_restoring||_retiring);
    internal static bool SilentRebuild=>Covered;
    internal static int Position=>_completed;
    internal static string Pending
    {
        get
        {
            var active=_requests.Active;var pending=_requests.Pending;
            string Item(ForwardRequest r)=>$"{(r.Kind==ForwardKind.Fine?">":">>")} {r.Target.Host} · {r.Target.Label}"+(r.Continuation?"（续当前）":"");
            return $" · {(Running?"运行":"暂停")} · Auto：{(Auto?"开":"关")} · 待播{pending.Length}项"+
                (_phase.Length>0?$" · {_phase}":"")+"\n"+
                (active is null?"当前：等待请求":$"当前目标：{Item(active)}")+
                (pending.Length==0?"":"\n待播："+string.Join(" → ",pending.Take(5).Select(Item))+(pending.Length>5?" …":""));
        }
    }
    internal static KeyValuePair<int,string>[] Index=>Points.Select(p=>new KeyValuePair<int,string>(p.Key,$"操作{p.Key}后 · {_requests.Route.Single(n=>n.Complete&&n.Host==p.Key).Label}")).ToArray();
    internal static void Start(Clip clip)
    {
        Clear();Codec.Require(clip.Id==HunterClip&&clip.Format>=4&&!(clip.Choices??[]).Any(c=>c.Boundary==0),"playback002-known-hunter-required");
        _clip=clip;_requests=new(clip,SliceHosts);Enabled=true;
    }
    internal static void Clear()
    {
        Enabled=false;_generation++;Points.Clear();Attempts.Clear();CancelledAhead.Clear();_choice=null;
        _choiceReady=false;_choiceMoving=false;_retiring=false;_restoring=false;_stopping=false;_completed=0;Host=0;_seek=null;_phase="";
    }
    internal static void Control(string command)
    {
        var accepted=false;ForwardRequest? added=null;
        if(command=="stop"&&!_stopping){_stopping=true;_requests.Stop();_seek=null;accepted=true;}
        else if(command is "step" or "fine"&&CanStep){added=_requests.Add(command=="fine"?ForwardKind.Fine:ForwardKind.Whole);accepted=added is not null;}
        else if(command=="toggle"&&!_stopping&&!_restoring&&_seek is null){_requests.ToggleRunning();accepted=true;}
        else if(command=="auto"&&!_stopping&&!_restoring&&_seek is null){_requests.ToggleAuto();accepted=true;}
        else if(command=="back"&&CanBack){RequestSeek(InFlight?Host-1:_completed-1);accepted=true;}
        Runtime.Log("operation-control",new{Command=command,Accepted=accepted,Host,Completed=_completed,Running,Auto,
            Added=added,Active=_requests.Active,Pending=_requests.Pending,Phase=_phase});
    }
    internal static void RequestSeek(int host)
    {
        Codec.Require(Enabled&&!_restoring&&!_stopping&&Points.ContainsKey(host),"operation-seek-only-complete-indexed-boundaries");
        _requests.Seek(_completed);_seek=host;
        Runtime.Log("operation-seek-request",new{From=_completed,Target=host,ChoicesAddressable=false,Running,Auto});
    }
    internal static async Task Run()
    {
        Capture(0);PlayerLoop.Loading=false;Status();
        try
        {
            while(!_stopping)
            {
                PlayerLoop.RequireHealthy();
                if(_choice?.Driver is {IsFaulted:true} failed)await failed;
                if(_seek is {} target){_seek=null;await Restore(target);continue;}
                ActionMonitor.Tick();
                if(_requests.Acquire())
                {
                    if(HasView)await AdvanceChoice();
                    else if(!InFlight&&ActionMonitor.Submitted(_completed) is null&&LiveState.Playable)Submit(_completed+1,false);
                }
                FillAhead();WakeExecutor();await Runtime.NextFrame();
            }
        }
        finally
        {
            _requests.Stop();
            if(PlayerLoop.Error is null&&ActionMonitor.Error is null)await Drain();
            else if(_choice is {} choice)NativeChoice.Cancel(choice);
            Runtime.Log("operation-driver-closed",new{Completed=_completed,Host,Generation=_generation,Indexed=Points.Keys.ToArray(),
                Outstanding=ActionMonitor.Outstanding,Running,Auto,Pending=_requests.Pending.Length,DisplayTracePlayed=false});
        }
    }
    private static void Status()=>Runtime.Status=$"PLAYBACK-002 · 已完成 {_completed}/{SliceHosts} · "+
        (InFlight?$"正在操作{Host}":_completed==SliceHosts?"切片结束":"完整操作边界");
    private static void Capture(int host)
    {
        Points[host]=new(host,PlayerLoop.EnsureRoster(),route:true,extraRoots:[LiveState.Run.CurrentRoom!]);
        Runtime.Log("operation-boundary-captured",new{Host=host,ChoicePosition=ChoiceTape.Position,ChoiceSnapshot=false,Projection=Points[host].Projection});
    }
    private static void Arrive(ForwardPoint point)
    {
        var done=_requests.Arrive(point);_phase=point.Complete?"":point.Label;Status();
        Runtime.Log("operation-phase",new{point.Stage,point.Host,point.Choice,point.Prefix,point.Complete,
            RequestFinished=done,Running,Auto,Pending=_requests.Pending,DirectSeekTarget=point.Complete});
    }
    internal static void Completed(PendingOperation pending,Operation actual)
    {
        var host=pending.PlaybackIndex+1;var expected=_clip.Operations[host-1];
        Codec.Require(host==_completed+1&&actual.After==expected.After&&actual.Victory==expected.Victory,"operation-result-or-rng-mismatch");
        _completed=host;Host=host;
        if(!_retiring&&!_restoring){Capture(host);Arrive(_requests.Route.Single(p=>p.Complete&&p.Host==host));}
        Runtime.Log("operation-native-verified",new{Host=host,NativeActionId=pending.Action.Id,Generation=_generation,
            ResultAndRngMatched=true,Retiring=_retiring,NativeAttempts=Attempts.GetValueOrDefault(host),NormalNativeBusiness=true});
    }
    internal static void Started(PendingOperation pending)
    {
        var host=pending.PlaybackIndex+1;var op=_clip.Operations[host-1];
        Codec.Require(host==_completed+1&&(_retiring||_requests.Active?.Target.Host==host),"operation-start-without-navigation-grant");
        Codec.Require(LiveState.Capture()==(host==1?_clip.Initial:_clip.Operations[host-2].After),"operation-start-state-or-rng-mismatch");
        Codec.Require(pending.Template.HandIndex==op.HandIndex&&pending.Template.CardState==op.CardState&&LiveState.Turn==op.Turn,"operation-execution-card-identity-mismatch");
        ChoiceTape.Boundary(host);Host=host;_phase="原生结算";Status();
        Runtime.Log("operation-native-started",new{Host,NativeActionId=pending.Action.Id,Generation=_generation,Attempt=Attempts.GetValueOrDefault(host),
            Request=_requests.Active,ReenteredAfterNativeCancellation=CancelledAhead.Remove(host)});
    }
    private static bool Submit(int host,bool ahead)
    {
        if(host>SliceHosts||_stopping||_restoring||_retiring)return false;
        var op=_clip.Operations[host-1];
        if(ahead&&(op.Kind!=OperationKind.Card||!LiveState.InputPhase||ChoiceTape.Waiting))return false;
        var card=op.Kind==OperationKind.Card?LiveState.Player.PlayerCombatState!.Hand.Cards.FirstOrDefault(c=>LiveState.CardIdentity(c).CombatIndex==op.CardState!.CombatIndex):null;
        if(op.Kind==OperationKind.Card&&(card is null||card.Id.ToString()!=op.Card||LiveState.CardIdentity(card)!=op.CardState||!card.CanPlayTargeting(LiveState.Resolve(op.Target))))
        {if(ahead)return false;throw new InvalidDataException("operation-requested-card-not-ready");}
        Attempts[host]=Attempts.GetValueOrDefault(host)+1;
        PlayerLoop.SubmitOperation(op,host-1,card);
        var pending=ActionMonitor.Submitted(host-1)??throw new InvalidDataException("operation-native-acceptance-missing");
        Runtime.Log("operation-native-enqueued",new{Host=host,NativeActionId=pending.Action.Id,Ahead=ahead,Attempt=Attempts[host],
            EarlierOriginalTaskIncomplete=ActionMonitor.Pending?.ActionTask is {IsCompleted:false},RequestConsumedAgain=false});
        return true;
    }
    private static void FillAhead()
    {
        if(!Running||_restoring||_retiring||_stopping||ChoiceTape.Waiting||_requests.Active is not {Kind:ForwardKind.Whole} active||
            ActionMonitor.Pending is not {Executing:true,Card:not null} current||current.PlaybackIndex+1!=active.Target.Host)return;
        var expected=active.Target.Host+1;var filled=0;
        foreach(var request in _requests.Pending)
        {
            if(request.Kind!=ForwardKind.Whole||request.Target.Host!=expected||filled++>=2)break;
            var next=_clip.Operations[expected-1];
            if(next.Kind!=OperationKind.Card||next.Turn!=_clip.Operations[active.Target.Host-1].Turn)break;
            // A choice-cancelled reservation must wait for the WHOLE owner operation,
            // including Burst's later choices, rather than reentering between choices.
            if(CancelledAhead.Contains(expected))break;
            if(ActionMonitor.Submitted(expected-1) is null&&!Submit(expected,true))break;
            expected++;
        }
    }
    // Leave the executor free to resume the CURRENT action after a choice. Globally
    // pausing ActionExecutor would also block that continuation and its natural tail.
    internal static GameAction? FilterReady(GameAction? action)
    {
        if(!Enabled)return action;
        if(action is not null&&ActionMonitor.Find(action) is null)return action;
        ActionMonitor.CompleteAtQueueBoundary();
        if(action is null)return null;
        var pending=ActionMonitor.Find(action);if(pending is null||pending.Executing)return action;
        if(_stopping||_retiring||_restoring||_seek is not null||!_requests.Acquire()||_requests.Active!.Target.Host!=pending.PlaybackIndex+1)return null;
        return action;
    }
    private static void WakeExecutor()
    {
        if(!Enabled||!Running||_stopping||_restoring||_retiring)return;
        var executor=RunManager.Instance.ActionExecutor;
        if(!executor.IsRunning&&ActionMonitor.Outstanding)AccessTools.Method(executor.GetType(),"ActionQueueChanged").Invoke(executor,null);
    }
    internal static void Cancelled(PendingOperation pending)
    {
        Codec.Require(!pending.Executing,"operation-executing-native-action-cancelled");
        var host=pending.PlaybackIndex+1;CancelledAhead.Add(host);
        Runtime.Log("operation-native-cancelled",new{Host=host,NativeActionId=pending.Action.Id,Generation=_generation,
            DueToChoice=ChoiceTape.Scope?.Frame.Sequence,Retiring=_retiring||_restoring||_stopping,
            StillReserved=_requests.Pending.Any(r=>r.Target.Host==host),BusinessStarted=false,RequestConsumedAgain=false});
    }
    internal static void CancelSettled(GameAction action)
    {
        if(action is not PlayCardAction play)return;
        var card=play.NetCombatCard.ToCardModelOrNull();
        Runtime.Log("operation-native-returned-to-hand",new{NativeActionId=action.Id,Card=card?.Id.ToString(),
            InLogicalHand=card?.Pile?.Type.ToString()=="Hand",NativeHandHolderPresent=card is not null&&NPlayerHand.Instance?.GetCardHolder(card) is not null});
    }
    internal static void Opened(ChoiceScope scope,object ui)
    {
        Codec.Require(_choice is null||_choice.Closed,"operation-overlapping-choice-ui");
        scope.Ui=ui;scope.PlaybackEvents=ChoiceTrace.ResultDemo(scope.Expected!);scope.EventPosition=1;
        ChoiceTape.Display=$"原生选择{scope.Frame.Sequence} · 录像决策只读 · 可浏览；Back撤回整项";
        _choice=scope;_choiceReady=false;scope.Driver=PrepareChoice(scope);
    }
    private static async Task PrepareChoice(ChoiceScope scope)
    {
        await NativeChoice.WaitReady(scope);Codec.Require(ReferenceEquals(_choice,scope),"operation-choice-generation-changed");
        _choiceReady=true;
        if(!_retiring&&!_restoring)Arrive(_requests.Route.Single(p=>p.Choice==scope.Frame.Sequence&&p.Prefix==0));
        Runtime.Log("operation-native-choice-opened",new{scope.Frame.Sequence,scope.Frame.Boundary,NativeId=scope.Id,NativeUi=scope.Ui!.GetType().Name,
            IndependentSeekPoint=false,ProxyUi=false});
    }
    internal static void Closed(object ui,bool settled=false)
    {
        if(_choice is not {} scope||!ReferenceEquals(scope.Ui,ui)||scope.Cancelled)return;
        if(!scope.Closed)
        {
            Codec.Require(scope.EventPosition==scope.PlaybackEvents.Length-1&&scope.PlaybackEvents[scope.EventPosition].Kind=="Close","operation-choice-closed-before-recorded-confirm");
            scope.EventPosition++;scope.Closed=true;
        }
        if(settled)scope.UiClosed.TrySetResult(true);
    }
    private static async Task AdvanceChoice()
    {
        var scope=_choice!;_choiceMoving=true;
        try
        {
            var next=scope.PlaybackEvents[scope.EventPosition++];NativeChoice.Apply(scope,next);
            if(!scope.Closed&&scope.EventPosition<scope.PlaybackEvents.Length-1)
            {
                var at=Stopwatch.GetTimestamp();
                while(scope.Ui is NPlayerHand hand&&AccessTools.Field(typeof(NPlayerHand),"_selectedCardScaleTween").GetValue(hand) is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning())
                {Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<3,"operation-selection-motion-timeout");await Runtime.NextFrame();}
                NativeChoice.AssertPrefix(scope,scope.EventPosition);
                if(!_retiring&&!_restoring)Arrive(_requests.Route.Single(p=>p.Choice==scope.Frame.Sequence&&p.Prefix==scope.EventPosition-1));
            }
            else
            {
                await scope.UiClosed.Task;
                if(ReferenceEquals(_choice,scope)){_choice=null;_choiceReady=false;}
            }
        }
        finally{_choiceMoving=false;}
    }
    private static async Task Drain()
    {
        _retiring=true;_phase="正在结束当前整项操作";Ui.Refresh();var at=Stopwatch.GetTimestamp();
        try
        {
            RunManager.Instance.ActionQueueSet.CancelNonExecutingActionsForPlayer(LiveState.Player.NetId);
            while(ActionMonitor.Outstanding||!LiveState.Playable)
            {
                PlayerLoop.RequireHealthy();Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<60,"operation-retirement-timeout");
                if(HasView)await AdvanceChoice();ActionMonitor.Tick();await Runtime.NextFrame();
            }
        }
        finally{_retiring=false;_phase="";Ui.Refresh();}
    }
    private static async Task Restore(int target)
    {
        var at=Stopwatch.GetTimestamp();var point=Points[target];_restoring=true;
        try
        {
            await Drain();var drained=Stopwatch.GetTimestamp();_generation++;
            _phase="正在恢复操作边界";Ui.Refresh();await point.Restore();ActionMonitor.RebaseNativeSeek();
            _completed=target;Host=target;_requests.Seek(target);Attempts.Clear();CancelledAhead.Clear();_choice=null;_choiceReady=false;
            HandPresentation.FinishHighlights(NPlayerHand.Instance!);Status();
            Codec.Require(LiveState.ChoiceProjection()==point.Projection,"operation-seek-result-or-rng-mismatch");
            Runtime.Log("operation-seek-arrived",new{Target=target,StateAndRngMatched=true,Running,Auto,Pending=_requests.Pending.Length,
                ChoiceSnapshotRestored=false,RebuiltNativeHosts=0,RetirementMilliseconds=Stopwatch.GetElapsedTime(at,drained).TotalMilliseconds,
                TotalMilliseconds=Stopwatch.GetElapsedTime(at).TotalMilliseconds});
        }
        finally{_restoring=false;_phase="";Ui.Refresh();}
    }
}
