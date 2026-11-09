using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// Only whole-operation boundaries are restored. Fine phases live in the current native
// action; ahead submissions reserve existing requests and never count as completion.
internal static class NativeSeek
{
    private sealed record Navigation(int Host,PlaybackIntent Intent,bool Back);
    private sealed record TurnJump(PlayerTurnPoint Target,int From,long Requested,bool Visited)
    {
        internal bool TriedAnchor;
    }
    private sealed record PotionArrival(int Host,NHandCardHolder Holder,long Since);
    private static readonly SortedDictionary<int,DirectCheckpoint> Points=[];
    private static readonly Dictionary<int,int> Attempts=[];
    private static readonly HashSet<int> CancelledAhead=[];
    private static Clip _clip=null!;
    private static OperationRequests _requests=null!;
    private static ChoiceScope? _choice;
    private static long _choiceReadyAt;
    private const double PileChoicePreviewSeconds=.4;
    private static bool _choiceReady,_choiceMoving,_retiring,_restoring,_coverRetirement,_applyingRestore,_stopping;
    private static int _completed,_generation,_choiceOwner;
    private static Navigation? _seek;
    private static PlayerTurnPoint[] _turnPoints=[];
    private static TurnJump? _turnJump;
    private static TurnAnchor? _turnAnchor;
    private static PotionArrival? _potionArrival;
    private static string _phase="";
    private static Func<Task> _loadRoom=null!,_resetRoom=null!;
    private static Navigation? _openingNavigation;
    private static bool _rebuildingOpening,_openingReentryPending;
    private static long _openingRequestedAt;
    private static bool _deathAutoSummary;
    internal static bool Opening {get;private set;}
    internal static bool OpeningWaiting=>Opening&&HasView&&_requests.ManualWaiting;
    internal static bool Enabled {get;private set;}
    internal static int Host {get;private set;}
    internal static int Generation=>_generation;
    internal static bool HasView=>_choice is {Closed:false,Cancelled:false}&&_choiceReady&&!_choiceMoving;
    internal static bool Ready=>Enabled&&!_stopping&&!_restoring&&(Opening?HasView:!InFlight||HasView);
    internal static bool InFlight=>Host>_completed;
    internal static bool ForwardWorkPending=>Enabled&&_requests.HasForwardWork;
    private static bool Quiescent=>!ActionMonitor.Outstanding&&(LiveState.Playable||PlayerLoop.VictoryReached&&BattleEnd.Ready||DefeatReached&&BattleEnd.DeathReady);
    internal static bool CanRestore=>Enabled&&_restoring&&Quiescent;
    internal static bool Finished=>Enabled&&_completed==_clip.Operations.Length;
    internal static bool DefeatReached=>Enabled&&_completed>0&&_clip.Operations[_completed-1].Defeat;
    internal static Operation? ExpectedDeath=>Enabled&&ActionMonitor.Pending is {Executing:true,PlaybackIndex:>=0} p&&
        p.PlaybackIndex<_clip.Operations.Length&&_clip.Operations[p.PlaybackIndex].Defeat?_clip.Operations[p.PlaybackIndex]:null;
    internal static bool DeathTransition=>ExpectedDeath is not null||RunEndings.DeathActive;
    internal static bool AllMatched=>Finished;
    internal static bool CanStep=>Enabled&&(!PlayerLoop.Loading||Opening&&Ready)&&!_stopping&&!_restoring&&_seek is null&&!Finished&&
        (Opening||!RunPlayback.Active||!BattleEnd.Ending&&!CombatManager.Instance.IsEnding)&&_requests.Stage<_requests.Route[^1].Stage;
    internal static bool CanFine=>CanStep&&Ready&&_requests.CanRequestFine;
    internal static bool CanBack=>Enabled&&!DeathTransition&&(!RunPlayback.Active||!Opening&&(InFlight||_completed>0))&&!_stopping&&!_restoring&&_seek is null&&_openingNavigation is null&&
        (Opening?HasView:InFlight||_completed>=0);
    internal static bool Stopping=>_stopping;
    internal static bool StopDrainsCombat=>Enabled&&!Opening&&!DeathTransition;
    internal static bool Restoring=>_restoring;
    internal static bool RetiringPresentation=>Enabled&&(_restoring||_seek is not null||_openingNavigation is not null||_stopping&&StopDrainsCombat);
    internal static bool Running=>Enabled&&_requests.Running;
    internal static bool Auto=>Enabled&&_requests.Auto;
    internal static bool Covered=>Enabled&&(_coverRetirement||_applyingRestore||_rebuildingOpening||_turnJump is not null);
    internal static bool SilentRebuild=>Enabled&&(_restoring||_retiring||_rebuildingOpening);
    internal static int Position=>_completed;
    internal static bool JumpingTurn=>_turnJump is not null;
    internal static string? TurnJumpStatus=>_turnJump is {} jump?$"正在定位玩家第{jump.Target.Turn}回合…":null;
    internal static bool RebuildingTurn=>_turnJump is {Visited:false};
    internal static bool CanLeaveRoom=>Enabled&&!Opening&&!InFlight&&!_stopping&&!_restoring&&_seek is null&&_turnJump is null&&
        !DeathTransition&&!ActionMonitor.Outstanding;
    internal static bool CanJumpTurn(bool next)=>Enabled&&!Opening&&!_stopping&&!_restoring&&_seek is null&&_turnJump is null&&
        !InFlight&&!ActionMonitor.Outstanding&&!PlayerLoop.Loading&&!DeathTransition&&!ReplayMap.IsOpen&&!RunPlayback.RoomJumping&&
        PlayerTurnPoints.Target(_turnPoints,_completed,next) is not null;
    internal static void JumpTurn(bool next)
    {
        if(!CanJumpTurn(next))return;
        var target=PlayerTurnPoints.Target(_turnPoints,_completed,next)!;
        var visited=Points.ContainsKey(target.Host);
        _turnJump=new(target,_completed,Stopwatch.GetTimestamp(),visited);
        if(visited)QueueSeek(new(target.Host,new(true,false),false));
        else
        {
            Codec.Require(target.Host>_completed,"unvisited-turn-behind-live-boundary");
            _requests.Restore(_completed,new(true,false));
            SeekAudio.Begin(room:false);
        }
        Runtime.Log("player-turn-jump-requested",new{From=_completed,target.Host,target.Turn,Visited=visited});
        Ui.Refresh();
    }
    internal static void PauseAuto()
    {
        if(Auto)_requests.ToggleAuto();
    }
    internal static void AutoForWarmup()
    {
        if(!Auto)_requests.ToggleAuto();
    }
    internal static string Pending
    {
        get
        {
            var active=_requests.Active;var pending=_requests.Pending;
            string Item(ForwardRequest r)=>$"{(r.Kind==ForwardKind.Fine?">":">>")} {r.Target.Host} · {r.Target.Label}"+(r.Continuation?"（续当前）":"")+
                (ActionMonitor.Submitted(r.Target.Host-1) is {} native?native.Executing?" [结算中]":" [已原生入队]":CancelledAhead.Contains(r.Target.Host)?" [退队待重排]":" [尚待提交]");
            return $" · {(Finished?"播放结束":Auto?"自动连续":_requests.ManualWaiting?"手动等待":"手动执行")} · 待播{pending.Length}项"+
                (_phase.Length>0?$" · {_phase}":"")+"\n"+
                (active is null?"当前：等待请求":$"当前目标：{Item(active)}")+
                (pending.Length==0?"":"\n待播："+string.Join(" → ",pending.Take(5).Select(Item))+(pending.Length>5?" …":""));
        }
    }
    internal static KeyValuePair<int,string>[] Index=>(Points.Count>0||Ready?new[]{new KeyValuePair<int,string>(-1,"战斗开场 · 原生重入")}:[])
        .Concat(Points.Select(p=>new KeyValuePair<int,string>(p.Key,p.Key==0?"0 · 首次可出牌":$"操作{p.Key}后 · {_requests.Route.Single(n=>n.Complete&&n.Host==p.Key).Label}"))).ToArray();
    internal static void Start(Clip clip,bool preserveAudio=false)
    {
        Clear(preserveAudio);Codec.Require(NativePlaybackSupport.Reason(clip) is null,NativePlaybackSupport.Reason(clip)??"native-playback-unsupported");
        _clip=clip;_requests=new(clip,clip.Operations.Length,opening:true);_turnPoints=PlayerTurnPoints.Read(clip);Enabled=true;Opening=true;
    }
    internal static void Clear(bool preserveAudio=false)
    {
        Enabled=false;_generation++;Points.Clear();Attempts.Clear();CancelledAhead.Clear();_choice=null;_deathAutoSummary=false;_potionArrival=null;
        _turnPoints=[];_turnJump=null;_turnAnchor=null;
        _loadRoom=null!;_resetRoom=null!;
        NativePhaseRetirement.Clear(preserveAudio);NativeSpawnPresentation.Clear();NativeDisplayTrace.ClearSourceVisuals("native-seek-clear");
        CombatMusic.Clear();CombatLoops.Clear();EnemyScenePresentation.Clear();_applyingRestore=false;_coverRetirement=false;
        Opening=false;_openingNavigation=null;_rebuildingOpening=false;_openingReentryPending=false;_openingRequestedAt=0;
        _choiceReady=false;_choiceMoving=false;_retiring=false;_restoring=false;_stopping=false;_completed=0;Host=0;_seek=null;_phase="";_choiceOwner=0;
    }
    internal static void Control(string command)
    {
        var accepted=false;ForwardRequest? added=null;var fineEligibleBefore=CanFine;
        if(command=="stop"&&!_stopping){_stopping=true;_requests.Stop();_seek=null;accepted=true;}
        else if(_turnJump is not null||ReplayMap.Observing||RunPlayback.RoomJumping&&(!Opening||command=="auto"))return;
        else if(command=="step"&&CanStep||command=="fine"&&CanFine){added=_requests.Add(command=="fine"?ForwardKind.Fine:ForwardKind.Whole);accepted=added is not null;}
        else if(command=="auto"&&(!PlayerLoop.Loading||Opening)&&!Finished&&!_stopping&&!_restoring&&_seek is null){_requests.ToggleAuto();accepted=true;}
        else if(command=="back"&&CanBack){RequestSeek(Opening?-1:InFlight?Host-1:_completed-1,preserveIntent:false);accepted=true;}
        Runtime.Log("operation-control",new{Command=command,Accepted=accepted,Host,Completed=_completed,Running,Auto,
            Added=added,Active=_requests.Active,Pending=_requests.Pending,Phase=_phase,FineEligibleBefore=fineEligibleBefore,
            ManualRequestIdle=_requests.ManualWaiting,PublicPlayPause=false});
    }
    internal static void RequestSeek(int host,bool preserveIntent=true)
    {
        if(DeathTransition){Runtime.Log("operation-death-navigation-disabled",new{Target=host,NativeGameOverOwned=true,OnlyStopExits=true});return;}
        Codec.Require(!RunPlayback.Active||host>=0,"run-cross-room-opening-navigation-disabled");
        Codec.Require(Enabled&&!_restoring&&!_stopping&&_turnJump is null&&(host==-1||Points.ContainsKey(host)),"operation-seek-only-complete-indexed-boundaries");
        var priorIntent=_seek?.Intent??_requests.Intent;
        var intent=PlaybackIntent.ForNavigation(priorIntent.Auto,back:!preserveIntent);
        QueueSeek(new(host,intent,Back:!preserveIntent));
    }
    private static void QueueSeek(Navigation navigation)
    {
        var host=navigation.Host;var intent=navigation.Intent;
        _requests.Seek(_completed);
        _potionArrival=null;
        if(host==-1)_openingRequestedAt=Stopwatch.GetTimestamp();
        if(Opening&&host==-1)_openingNavigation=navigation;else _seek=navigation;
        NativeDisplayTrace.RetireVisibleRelicFlashes();
        NativeDisplayTrace.HideRetiringCardVisuals();
        Runtime.Log("operation-seek-request",new{From=_completed,Target=host,ChoicesAddressable=false,IntendedRunning=intent.Running,IntendedAuto=intent.Auto,navigation.Back});
    }
    private static void OpeningReentryReady(string landing)
    {
        if(!_openingReentryPending)return;
        _openingReentryPending=false;Status();
        Runtime.Log("native-opening-reentry-ready",new{Landing=landing,Source="recorded-pre-room-checkpoint",InitialUsedAsBeforeState=false,Auto,Pending=_requests.Pending.Length,
            RoomAlreadyRevealed=!_rebuildingOpening,
            RequestToReadyMilliseconds=_openingRequestedAt==0?(double?)null:Stopwatch.GetElapsedTime(_openingRequestedAt).TotalMilliseconds});
    }
    internal static void OpeningRoomVisible(MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom room,CombatState state)
    {
        if(!Enabled||!Opening||!_rebuildingOpening||
            !ReferenceEquals(room,MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance)||
            !ReferenceEquals(state,CombatManager.Instance.DebugOnlyGetState()))return;
        // Native setup has installed the room, its background and combat UI. Reveal
        // it BEFORE native start/choice animations, independently of request readiness.
        _rebuildingOpening=false;Ui.Refresh();
        Runtime.Log("native-opening-room-revealed",new{Boundary="NCombatRoom.OnCombatSetUp",NativeOpeningStillRuns=true,
            ChoiceReady=HasView,ExtraRequestGranted=false,NativeEffectsAllowed=!SilentRebuild,AnimationReplayedByMod=false,
            RequestToRevealMilliseconds=_openingRequestedAt==0?(double?)null:Stopwatch.GetElapsedTime(_openingRequestedAt).TotalMilliseconds});
    }
    internal static async Task<bool> LoadOpening(Func<Task> loadRoom,Func<Task> resetRoom,PlaybackIntent? restart=null)
    {
        _loadRoom=loadRoom;_resetRoom=resetRoom;
        if(restart is {} intent)await ResetOpening(intent);
        while(!_stopping)
        {
            Opening=true;var load=loadRoom();
            while(!load.IsCompleted&&_openingNavigation is null)
            {
                // Asset/scene loading is not cancellable. Let it reach a native wait
                // before dismantling the room; never let a late loader enter a new run.
                if(_stopping&&(PlayerLoop.ChoiceAbort||HasView||ChoiceTape.Scope is {Ui:not null,Closed:false,Cancelled:false}))break;
                PlayerLoop.RequireHealthy();
                if(_choice?.Driver is {IsFaulted:true} failed)await failed;
                if(_requests.Acquire()&&HasView&&ChoicePreviewElapsed())await AdvanceChoice();
                await Runtime.NextFrame();
            }
            if(_stopping||_openingNavigation is not null)
            {
                var navigation=_openingNavigation;_openingNavigation=null;
                await resetRoom();
                try{await load;}catch(OperationCanceledException) { }
                PlayerLoop.ChoiceAbort=false;
                if(_stopping)return false;
                ResetOpeningState(navigation!.Intent);continue;
            }
            await load;OpeningReentryReady("playable-zero");Opening=false;
            Arrive(_requests.Route.Single(p=>p.Complete&&p.Host==0));
            Runtime.Log("native-opening-completed",new{ChoiceCursor=ChoiceTape.Position,InitialMatched=true,Operation1Started=false,
                NativeOrder=true,Auto,Request=_requests.Active,Pending=_requests.Pending.Length});return true;
        }
        return false;
    }
    private static async Task ResetOpening(PlaybackIntent intent)
    {
        await _resetRoom();PlayerLoop.ChoiceAbort=false;ResetOpeningState(intent);
    }
    private static void ResetOpeningState(PlaybackIntent intent)
    {
        Points.Clear();Attempts.Clear();CancelledAhead.Clear();_choice=null;_choiceReady=false;_choiceMoving=false;_choiceOwner=0;_turnAnchor=null;
        _completed=0;Host=0;_generation++;PlayerLoop.VictoryReached=false;Opening=true;_rebuildingOpening=true;_openingReentryPending=true;_coverRetirement=false;
        _requests.Restore(-1,intent);ChoiceTape.Play(_clip);
        Runtime.Log("native-opening-reenter",new{Generation=_generation,Source="recorded-pre-room-checkpoint",Auto=intent.Auto,
            DirectMemoryRestore=false,OldTasksRestored=false,ChoiceSeekPoints=false,VisitedRoomCheckpointsCleared=true});
    }
    internal static async Task Run()
    {
        Capture(0);PlayerLoop.Loading=false;_phase="";Status();
        Runtime.Log("operation-driver-ready",new{ControlModel="D131-two-modes",DefaultManual=true,Running,Auto,
            ManualRequestIdle=_requests.ManualWaiting,CanFine,InFlight,PublicPlayPause=false});
        try
        {
            while(!_stopping)
            {
                if(RunPlayback.RoomJumpPending)break;
                if(RunPlayback.RoomJumpSelecting)RunPlayback.CheckStop();
                if(PlayerLoop.HistoryMenuOpen||ReplayMap.Observing||RunPlayback.RoomJumpSelecting){await Runtime.NextFrame();continue;}
                if(DefeatReached)
                {
                    if(RunPlayback.Active)break;
                    try{await EndingPlayback.Run(_deathAutoSummary);}
                    catch(OperationCanceledException) when(_stopping){}
                    break;
                }
                if(Finished&&RunPlayback.WantsNext&&_seek is null)break;
                PlayerLoop.RequireHealthy();
                if(_choice?.Driver is {IsFaulted:true} failed)await failed;
                if(_seek is {} target)
                {
                    _seek=null;
                    if(target.Host==-1)
                    {
                        _restoring=true;try{await Drain(fastPhase:true);}finally{_restoring=false;}
                        if(!await LoadOpening(_loadRoom,_resetRoom,target.Intent))break;
                        Capture(0);PlayerLoop.Loading=false;Status();
                    }
                    else await Restore(target);
                    continue;
                }
                ActionMonitor.Tick();
                if(_turnJump is {} jump)
                {
                    if(!jump.Visited&&!jump.TriedAnchor)
                    {
                        jump.TriedAnchor=true;
                        if(_turnAnchor?.Prepare(_completed,jump.Target.Host) is {} apply&&
                            await Restore(new(jump.Target.Host,new(true,false),false),turnAnchor:apply))
                        {FinishTurnJump(jump,fromFile:true);continue;}
                    }
                    if(_completed==jump.Target.Host&&!InFlight&&!ActionMonitor.Outstanding&&LiveState.Playable)
                    {
                        if(!jump.Visited)await Restore(new(jump.Target.Host,new(true,false),false),computedTurn:true);
                        FinishTurnJump(jump);
                    }
                    else if(!_requests.HasForwardWork&&_completed<jump.Target.Host)
                        _requests.Add(ForwardKind.Whole);
                }
                if(_requests.Acquire())
                {
                    if(HasView)
                    {
                        if(ChoicePreviewElapsed())await AdvanceChoice();
                    }
                    else if(!InFlight&&ActionMonitor.Submitted(_completed) is null&&LiveState.Playable)Submit(_completed+1,false);
                }
                FillNativeQueue();WakeExecutor();await Runtime.NextFrame();
            }
        }
        finally
        {
            _requests.Stop();
            if(!Opening&&PlayerLoop.Error is null&&ActionMonitor.Error is null)
            {
                var fastStop=_stopping&&StopDrainsCombat&&!Quiescent;
                var at=Stopwatch.GetTimestamp();
                await Drain(fastPhase:fastStop,target:-1);
                if(_stopping)Runtime.Log("operation-stop-drained",new{Host,Completed=_completed,FastPresentationRetirement=fastStop,
                    NativeBoundaryReached=Quiescent,Milliseconds=Stopwatch.GetElapsedTime(at).TotalMilliseconds});
            }
            else if(!Opening&&_choice is {} choice)NativeChoice.Cancel(choice);
            Runtime.Log("operation-driver-closed",new{Completed=_completed,Host,Generation=_generation,Indexed=Points.Keys.ToArray(),
                Outstanding=ActionMonitor.Outstanding,Running,Auto,Pending=_requests.Pending.Length,DisplayTracePlayed=false,
                TotalOperations=_clip.Operations.Length,AllOperationsMatched=AllMatched,PlayerLoop.VictoryReached});
        }
    }
    private static void FinishTurnJump(TurnJump jump,bool fromFile=false)
    {
        Codec.Require(Points.ContainsKey(jump.Target.Host)&&LiveState.Turn==jump.Target.Turn&&
            LiveState.Capture()==(jump.Target.Host==0?_clip.Initial:_clip.Operations[jump.Target.Host-1].After),
            "player-turn-jump-boundary-mismatch");
        _turnJump=null;
        if(!jump.Visited)SeekAudio.End();
        Status();Ui.Refresh();
        Runtime.Log("player-turn-jump-arrived",new{jump.From,jump.Target.Host,jump.Target.Turn,
            Source=fromFile?"verified-turn-file":jump.Visited?"visited-memory":"verified-native-prefix",
            RebuiltNativeHosts=jump.Visited||fromFile?0:jump.Target.Host-jump.From,
            StateAndRngMatched=true,Auto,PrefixPresentationRetired=!jump.Visited&&!fromFile,
            Milliseconds=Stopwatch.GetElapsedTime(jump.Requested).TotalMilliseconds});
        if(!jump.Visited&&!fromFile)_turnAnchor?.Capture(jump.Target.Host,Points[jump.Target.Host]);
    }
    private static void Status()=>Runtime.Status=_turnJump is {} jump?$"正在定位玩家第{jump.Target.Turn}回合":Opening?
        (HasView?"开场选择 · >细进／>>完成开场":"正在原生初始化开场"):
        $"原生回放 · 已完成 {_completed}/{_clip.Operations.Length} · "+
        (DefeatReached?"原生死亡；可查看结算详情，点Stop退出":PlayerLoop.VictoryReached?"战斗胜利；可Back再播或Stop":InFlight?$"正在操作{Host}":Finished?"片段结束":"完整操作边界");
    private static void Capture(int host)
    {
        var roster=PlayerLoop.EnsureRoster();
        Points[host]=new(host,roster,route:true,extraRoots:[LiveState.Run.CurrentRoom!]);
        if(host==0)_turnAnchor=TurnAnchor.Bind(_clip,roster);
        Runtime.Log("operation-boundary-captured",new{Host=host,ChoicePosition=ChoiceTape.Position,ChoiceSnapshot=false,Projection=Points[host].Projection});
    }
    private static void Arrive(ForwardPoint point)
    {
        var done=_requests.Arrive(point);_phase=point.Complete?"":point.Label;Status();
        Runtime.Log("operation-phase",new{point.Stage,point.Host,point.Choice,point.Prefix,point.Complete,
            RequestFinished=done,Running,Auto,Pending=_requests.Pending,DirectSeekTarget=point.Complete,
            ManualRequestIdle=_requests.ManualWaiting,HostInFlight=InFlight});
    }
    internal static void Completed(PendingOperation pending,Operation actual)
    {
        var host=pending.PlaybackIndex+1;var expected=_clip.Operations[host-1];
        Codec.Require(host==_completed+1&&actual.After==expected.After&&actual.Victory==expected.Victory&&actual.Defeat==expected.Defeat,"operation-result-or-rng-mismatch");
        Codec.Require(!actual.Victory||host==_clip.Operations.Length,"victory-before-recording-end");
        _completed=host;Host=host;PlayerLoop.VictoryReached=actual.Victory;
        if(host==_clip.Operations.Length){RunPlayback.AtBattleBoundary(Auto);_deathAutoSummary=actual.Defeat&&Auto;}
        // The terminal result is the frozen native pre-reward victory projection. Restore
        // targets remain playable boundaries; the win is reached again through native business.
        if(!_retiring&&!_restoring)
        {
            if(!actual.Victory&&!actual.Defeat)Capture(host);
            Arrive(_requests.Route.Single(p=>p.Complete&&p.Host==host));
            if(!RebuildingTurn&&!actual.Victory&&!actual.Defeat)_turnAnchor?.Capture(host,Points[host]);
        }
        if(actual.Defeat)Runtime.Log("native-battle-death-verified",new{_clip.Id,Host,TotalOperations=_clip.Operations.Length,
            AllOperationsMatched=AllMatched,ActualDeathProjectionMatched=true,NativeGameOverShown=RunEndings.GameOverActive,BackEnabled=false});
        if(actual.Victory)Runtime.Log("native-battle-victory-verified",new{_clip.Id,Host,TotalOperations=_clip.Operations.Length,
            AllOperationsMatched=AllMatched,PlayerLoop.VictoryReached,OriginalVictoryProjectionMatched=true,
            NativeVictoryTaskSucceeded=BattleEnd.VictoryTask?.IsCompletedSuccessfully==true,BackTarget=host-1,RewardsAllowed=false});
        Runtime.Log("operation-native-verified",new{Host=host,NativeActionId=pending.Action.Id,Generation=_generation,
            ResultAndRngMatched=true,Retiring=_retiring,NativeAttempts=Attempts.GetValueOrDefault(host),NormalNativeBusiness=true});
    }
    internal static void Started(PendingOperation pending)
    {
        var host=pending.PlaybackIndex+1;var op=_clip.Operations[host-1];
        Codec.Require(host==_completed+1&&(_retiring||_requests.Active?.Target.Host==host),"operation-start-without-navigation-grant");
        Codec.Require(LiveState.Capture()==(host==1?_clip.Initial:_clip.Operations[host-2].After),"operation-start-state-or-rng-mismatch");
        // Format 10 resolves targets by stable combat identity. Older queued recordings
        // can also carry an acceptance-time index; compare both at the current boundary.
        var target=LiveState.Target(LiveState.Resolve(op.Target));
        Codec.Require(pending.Template.Kind==op.Kind&&pending.Template.HandIndex==op.HandIndex&&pending.Template.CardState==op.CardState&&
            pending.Template.Potion==op.Potion&&pending.Template.Target==target&&LiveState.Turn==op.Turn,"operation-execution-input-identity-mismatch");
        ChoiceTape.Boundary(host);Host=host;_phase="原生结算";Status();
        Runtime.Log("operation-native-started",new{Host,NativeActionId=pending.Action.Id,Generation=_generation,Attempt=Attempts.GetValueOrDefault(host),
            Request=_requests.Active,ReenteredAfterNativeCancellation=CancelledAhead.Remove(host)});
    }
    internal static void HandCardAdded(NHandCardHolder holder)
    {
        if(!Enabled||RetiringPresentation||ActionMonitor.Pending is not {Template.Kind:OperationKind.Potion,PlaybackIndex:>=0} pending||
            holder.CardModel is not {} card)return;
        var next=pending.PlaybackIndex+1;
        if(next>=_clip.Operations.Length||_clip.Operations[next] is not {Kind:OperationKind.Card,CardState:{} key} op||
            card.Id.ToString()!=op.Card||!NetCombatCardDb.Instance.TryGetCardId(card,out var index)||index!=key.CombatIndex)return;
        _potionArrival=new(next+1,holder,Stopwatch.GetTimestamp());
    }

    private static bool PotionCardReady(int host)
    {
        if(_potionArrival is not {} arrival||arrival.Host!=host)return true;
        if(RebuildingTurn)
        {
            // Submit still checks the actual native card, identity and legality.
            _potionArrival=null;
            Runtime.Log("cold-turn-potion-hand-layout-skipped",new{Host=host,NativePlayabilityChecked=true});
            return true;
        }
        var holder=arrival.Holder;
        var elapsed=Stopwatch.GetElapsedTime(arrival.Since);
        if(GodotObject.IsInstanceValid(holder)&&!holder.IsQueuedForDeletion()&&holder.IsInsideTree())
        {
            // Logical arrival precedes the native hand motion. Keep this play
            // pending until the actual card is selectable and near its slot.
            var ready=holder.Hitbox.IsEnabled&&holder.Position.DistanceSquaredTo(holder.TargetPosition)<=18f*18f;
            if(!ready&&elapsed.TotalSeconds<1.5)return false;
            Runtime.Log("native-potion-card-arrival-boundary",new{Host=host,elapsed.TotalMilliseconds,LayoutReady=ready});
        }
        _potionArrival=null;return true;
    }

    private static bool Submit(int host,bool ahead)
    {
        if(host>_clip.Operations.Length||PlayerLoop.VictoryReached||_stopping||_restoring||_retiring)return false;
        if(!PotionCardReady(host))return false;
        var op=_clip.Operations[host-1];
        if(ahead&&(op.Kind!=OperationKind.Card||!LiveState.InputPhase||ChoiceTape.Waiting))return false;
        Creature? target;
        try{target=LiveState.Resolve(op.Target);}catch(InvalidDataException) when(ahead){return false;}
        var card=op.Kind==OperationKind.Card?LiveState.Player.PlayerCombatState!.Hand.Cards.FirstOrDefault(c=>LiveState.CardIdentity(c).CombatIndex==op.CardState!.CombatIndex):null;
        // Ahead acceptance uses the current instance and native legality. Earlier cards may
        // legitimately change its cost/upgrades; the full recorded key is checked at START.
        if(op.Kind==OperationKind.Card&&(card is null||card.Id.ToString()!=op.Card||(!ahead&&LiveState.CardIdentity(card)!=op.CardState)||!card.CanPlayTargeting(target)))
        {if(ahead)return false;throw new InvalidDataException("operation-requested-card-not-ready");}
        Attempts[host]=Attempts.GetValueOrDefault(host)+1;
        PlayerLoop.SubmitOperation(op,host-1,card);
        var pending=ActionMonitor.Submitted(host-1)??throw new InvalidDataException("operation-native-acceptance-missing");
        Runtime.Log("operation-native-enqueued",new{Host=host,NativeActionId=pending.Action.Id,Ahead=ahead,Attempt=Attempts[host],
            EarlierOriginalTaskIncomplete=ActionMonitor.Pending?.ActionTask is {IsCompleted:false},RequestConsumedAgain=false,
            AcceptedWhilePaused=!Running,BusinessStarted=pending.Executing});
        return true;
    }
    private static void FillNativeQueue()
    {
        if(_restoring||_retiring||_stopping||_seek is not null||ChoiceTape.Waiting||InFlight&&_choiceOwner==Host)return;
        int? turn=null;
        foreach(var request in _requests.NativeQueueCandidates(_completed))
        {
            var expected=request.Target.Host;
            var next=_clip.Operations[expected-1];
            turn??=next.Turn;
            if(next.Kind!=OperationKind.Card||next.Turn!=turn||next.Turn!=LiveState.Turn)break;
            // A choice-cancelled reservation must wait for the WHOLE owner operation,
            // including Burst's later choices, rather than reentering between choices.
            if(CancelledAhead.Contains(expected)&&InFlight)break;
            if(ActionMonitor.Submitted(expected-1) is null&&!Submit(expected,true))break;
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
        _choiceOwner=scope.Frame.Boundary;
        ChoiceTape.Display=$"原生选择{scope.Frame.Sequence} · 录像决策只读 · 可浏览；Back撤回整项";
        _choice=scope;_choiceReady=false;scope.Driver=PrepareChoice(scope);
    }
    private static async Task PrepareChoice(ChoiceScope scope)
    {
        try{await PrepareChoiceCore(scope);}
        catch(OperationCanceledException) when(scope.Cancelled||PlayerLoop.ChoiceAbort) { }
        catch(Exception ex)
        {
            PlayerLoop.Error="native-choice-prepare-"+ex.GetBaseException().Message;
            Runtime.Log("native-choice-prepare-failed",new{Reason=PlayerLoop.Error});
            NativeChoice.Cancel(scope);
        }
    }
    private static async Task PrepareChoiceCore(ChoiceScope scope)
    {
        await NativeChoice.WaitReady(scope,()=>RetiringPresentation||RebuildingTurn&&scope.Ui is NChooseACardSelectionScreen);
        if(scope.Cancelled||PlayerLoop.ChoiceAbort)return;
        Codec.Require(ReferenceEquals(_choice,scope),"operation-choice-generation-changed");
        _choiceReady=true;_choiceReadyAt=Stopwatch.GetTimestamp();
        if(!_retiring&&!RetiringPresentation)Arrive(_requests.Route.Single(p=>p.Choice==scope.Frame.Sequence&&p.Prefix==0));
        if(Opening&&scope.Frame.Boundary==0)OpeningReentryReady("native-opening-choice");
        Runtime.Log("operation-native-choice-opened",new{scope.Frame.Sequence,scope.Frame.Boundary,NativeId=scope.Id,NativeUi=scope.Ui!.GetType().Name,
            IndependentSeekPoint=false,ProxyUi=false});
    }
    internal static void Closed(object ui,bool settled=false)
    {
        if(PlayerLoop.ChoiceAbort||PlayerLoop.TearingDown||_choice is not {} scope||!ReferenceEquals(scope.Ui,ui)||scope.Cancelled)return;
        if(!scope.Closed)
        {
            Codec.Require(scope.EventPosition==scope.PlaybackEvents.Length-1&&scope.PlaybackEvents[scope.EventPosition].Kind=="Close","operation-choice-closed-before-recorded-confirm");
            scope.EventPosition++;scope.Closed=true;
        }
        if(settled)scope.UiClosed.TrySetResult(true);
    }
    private static bool ChoicePreviewElapsed()=>RebuildingTurn||RunPlayback.RebuildingRoom||_choice is not {Ui:NCombatPileCardSelectScreen,EventPosition:1}||
        _requests.Active?.Kind!=ForwardKind.Whole||Stopwatch.GetElapsedTime(_choiceReadyAt).TotalSeconds>=PileChoicePreviewSeconds;
    private static async Task AdvanceChoice()
    {
        var scope=_choice!;_choiceMoving=true;
        try
        {
            if(!RetiringPresentation&&scope.Ui is NCombatPileCardSelectScreen&&scope.EventPosition==1)
                Runtime.Log("native-pile-choice-preview",new{scope.Frame.Sequence,scope.Frame.Boundary,
                    Mode=_requests.Active?.Kind.ToString(),VisibleMilliseconds=Stopwatch.GetElapsedTime(_choiceReadyAt).TotalMilliseconds});
            var next=scope.PlaybackEvents[scope.EventPosition++];NativeChoice.Apply(scope,next);
            if(!scope.Closed&&scope.EventPosition<scope.PlaybackEvents.Length-1)
            {
                var at=Stopwatch.GetTimestamp();
                while(scope.Ui is NPlayerHand hand&&AccessTools.Field(typeof(NPlayerHand),"_selectedCardScaleTween").GetValue(hand) is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning())
                {
                    if(RetiringPresentation)
                    {
                        tween.CustomStep(1); // The native tween only positions and scales the selected-card container.
                        Runtime.Log("native-choice-selection-motion-retired",new{scope.Frame.Sequence,scope.Frame.Boundary});
                        break;
                    }
                    Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<3,"operation-selection-motion-timeout");await Runtime.NextFrame();
                }
                NativeChoice.AssertPrefix(scope,scope.EventPosition);
                if(!_retiring&&!RetiringPresentation)Arrive(_requests.Route.Single(p=>p.Choice==scope.Frame.Sequence&&p.Prefix==scope.EventPosition-1));
            }
            else
            {
                await scope.UiClosed.Task;
                if(ReferenceEquals(_choice,scope)){_choice=null;_choiceReady=false;}
            }
        }
        finally{_choiceMoving=false;}
    }
    private static async Task Drain(bool fastPhase=false,int target=0)
    {
        _retiring=true;_coverRetirement=fastPhase||!Quiescent;
        _phase=_stopping?"正在停止回放":fastPhase?"正在快速收束已撤销操作":"正在结束当前整项操作";
        Ui.Refresh();var at=Stopwatch.GetTimestamp();
        try
        {
            RunManager.Instance.ActionQueueSet.CancelNonExecutingActionsForPlayer(LiveState.Player.NetId);
            if(fastPhase)
            {
                var room=NCombatRoom.Instance;
                if(room is not null)NativeCombatCamera.SettleOffsets(room,"covered-before-drain");
                NativePhaseRetirement.Begin(target);
                if(room is not null)
                {
                    CreatureHudTrace.RetireIntentBursts(room);
                    await NativeCombatCamera.RetireForNavigation(room);
                }
            }
            while(!Quiescent)
            {
                PlayerLoop.RequireHealthy();Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<60,"operation-retirement-timeout");
                if(HasView)await AdvanceChoice();
                ActionMonitor.Tick();
                if(!Quiescent)await Runtime.NextFrame();
            }
        }
        finally{if(fastPhase&&NativePhaseRetirement.Active)NativePhaseRetirement.End();_retiring=false;_phase="";Ui.Refresh();}
    }
    private static async Task<bool> Restore(Navigation navigation,bool computedTurn=false,Func<bool>? turnAnchor=null)
    {
        var target=navigation.Host;
        var at=Stopwatch.GetTimestamp();var point=Points[turnAnchor is null?target:_completed];_restoring=true;var fromVictory=false;
        try
        {
            await Drain(fastPhase:true,target:target);var drained=Stopwatch.GetTimestamp();var sourceGeneration=_generation++;fromVictory=PlayerLoop.VictoryReached;
            _phase="正在恢复操作边界";Ui.Refresh();
            var continueTurnLoop=false;
            if(fromVictory)
            {
                BattleEnd.RetireUiCleanup();continueTurnLoop=await NativeTurnContinuation.PrepareVictoryRestore();
                BattleEnd.RestoreVictoryProgress();
            }
            _applyingRestore=true;Ui.Refresh();
            var pictureOnly=computedTurn&&point.CanSettleComputedTurn;
            var fileApplied=false;
            if(turnAnchor is not null)
            {
                fileApplied=await point.RestoreTurnAnchor(turnAnchor,sourceGeneration);
                if(!fileApplied)target=point.Position;
            }
            else if(pictureOnly)
            {
                pictureOnly=await point.SettleComputedTurn(sourceGeneration);
                if(!pictureOnly)await point.Restore(sourcePicture:_generation++);
            }
            else await point.Restore(sourcePicture:sourceGeneration);
            if(point.Music is {} music)
            {
                if(computedTurn)CombatMusic.AlignComputedTurn(music);
                else CombatMusic.Restore(music,fromVictory);
            }
            if(fromVictory)
            {
                RunManager.Instance.ActionQueueSynchronizer.SetCombatState(ActionSynchronizerCombatState.PlayPhase);
                RunManager.Instance.ActionExecutor.Unpause();
                if(continueTurnLoop)NativeTurnContinuation.Resume();
            }
            PlayerLoop.VictoryReached=false;ActionMonitor.RebaseNativeSeek();
            _completed=target;Host=target;_requests.Restore(target,navigation.Intent);Attempts.Clear();CancelledAhead.Clear();_choice=null;_choiceReady=false;_choiceOwner=0;
            HandPresentation.FinishHighlights(NPlayerHand.Instance!);Status();
            NativeCombatCamera.SettleOffsets(NCombatRoom.Instance!,"before-reveal");
            if(fileApplied){Capture(target);point=Points[target];}
            Codec.Require(LiveState.ChoiceProjection()==point.Projection,"operation-seek-result-or-rng-mismatch");
            Runtime.Log("operation-seek-arrived",new{Target=target,StateAndRngMatched=true,Running,Auto,Pending=_requests.Pending.Length,
                IntendedRunning=navigation.Intent.Running,IntendedAuto=navigation.Intent.Auto,Back=navigation.Back,
                FromVictory=fromVictory,NativeCombatReactivated=fromVictory,
                ChoiceSnapshotRestored=false,RebuiltNativeHosts=0,PictureOnly=pictureOnly,FileAnchorApplied=fileApplied,
                RetirementMilliseconds=Stopwatch.GetElapsedTime(at,drained).TotalMilliseconds,
                TotalMilliseconds=Stopwatch.GetElapsedTime(at).TotalMilliseconds});
            return turnAnchor is null||fileApplied;
        }
        finally{_coverRetirement=false;_applyingRestore=false;_restoring=false;_phase="";Ui.Refresh();}
    }
}
