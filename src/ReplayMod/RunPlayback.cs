using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal static class RunPlayback
{
    internal static bool Active,NonCombat,Driving;
    private static RunDocument _run=null!;
    private static int _index,_decision,_fine,_wholeRequests;
    private static bool _auto,_whole,_stop,_continuation,_allowRewards;
    private static CombatRoom? _rewardsRoom;
    private static readonly HashSet<Task> OwnedTasks=[];
    private static RunState? _mapRun;
    private static MapCoord _mapDestination;
    private static int _mapGeneration;
    private static bool _mapVoted;
    private static MoveToMapCoordAction? _mapMove;
    private static Task? _mapTask;
    private static long _phaseAt;
    private static Dictionary<int,RunContinuation[]> _continuations=[];
    private static int _rawOperations;
    // This exact recording build has the same run/clip format and native inputs.
    internal const string RecordingBuild="145E3A3DE8A918CBD8AE90EBF4EFE1D7413E3F46911A1A5C26298266828F66EC";
    internal const string AcceptedChainRecordingBuild="DD563F456E5A693E9139F669B3C7E9B95ECFB11D6096A32A79F6EE82E89CF1FA";
    internal const string Run001Baseline="392B6DEB1899B1D5D9E71325382F2999D049CC16E0A9568A9B08951017D4E9DB";
    internal static bool Compatible(string hash)=>hash==Runtime.ModHash||hash==RecordingBuild||hash==AcceptedChainRecordingBuild||hash==Run001Baseline;
    internal static RunOperation? Current;
    internal static bool HasFollowing=>Active&&_index+1<_run.Operations.Count&&_run.Operations[_index+1].Status=="complete"&&_run.Operations[_index+1].Kind!="gap";
    internal static bool WantsNext=>HasFollowing&&(_auto||_continuation);
    internal static bool Stopping=>_stop;
    internal static bool Auto=>_auto;
    internal static bool CompletingOperation=>_whole;
    internal static string Display=>Active?$"\n路线 · 第{_run.Visits.Single(v=>v.Id==(Current?.Visit??_run.Operations[_index].Visit)).Floor}层 · 操作{_index+1}/{_run.Operations.Count}"+
        (Current?.Attempt is {} attempt?$" · 采用尝试#{attempt}（最终延续路线）":"")+
        (NonCombat?$" · {(_auto?"自动连续":"手动等待／执行")} · 待播{_fine+_wholeRequests}":""):"";
    internal static int StartIndex(RunDocument run)=>run.Operations.FindIndex(o=>o.Kind=="battle"&&o.Status=="complete"&&o.Clip is not null);
    internal static void Begin(RunDocument run,int? startSequence=null)
    {
        _rawOperations=run.Operations.Count;run=RunRoutes.Playback(run);
        Codec.Require(run.GameHash==Runtime.GameHash&&run.Scope==Runtime.Scope&&Compatible(run.ModHash),"run-environment-or-build-mismatch");
        _index=startSequence is {} sequence?run.Operations.FindIndex(o=>(o.SourceSequence??o.Sequence)==sequence):StartIndex(run);
        Codec.Require(_index>=0&&run.Operations[_index] is {Kind:"battle",Status:"complete",Clip:not null},"run-has-no-recorded-battle-start");
        Codec.Require(!RunStore.ResourceDiscontinuities(run).Any(s=>s>run.Operations[_index].Sequence),"run-has-unrecorded-resource-transition");
        var first=run.Operations[_index];
        _continuations=run.Continuations.Where(c=>c.PreFinished&&c.Visit>=first.Visit)
            .Select(c=>(Before:RunStore.ContinueBefore(run,c),Point:c)).Where(c=>c.Before>=first.Sequence)
            .GroupBy(c=>c.Before).ToDictionary(g=>g.Key,g=>g.Select(c=>c.Point).ToArray());
        _run=run;Active=true;NonCombat=false;Driving=false;_auto=false;_whole=false;_stop=false;_continuation=false;
        _fine=0;_wholeRequests=0;_decision=0;_phaseAt=0;_rewardsRoom=null;OwnedTasks.Clear();_mapRun=null;Current=run.Operations[_index];RunPatches.Clear();
    }
    internal static void Clear(){Active=false;NonCombat=false;Driving=false;Current=null;_rewardsRoom=null;OwnedTasks.Clear();_continuations.Clear();_mapRun=null;_mapMove=null;_mapTask=null;RunPatches.Clear();RunTreasures.Clear();}
    internal static void Track(Task task)
    {
        if(!Active||!OwnedTasks.Add(task))return;
        _=task.ContinueWith(t=>{_ =t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
    }
    internal static async Task DrainAfterSceneExit()
    {
        if(!Active)return;
        var at=Stopwatch.GetTimestamp();
        while(OwnedTasks.Any(t=>!t.IsCompleted))
        {Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<5,"run-native-action-did-not-retire");await Runtime.NextFrame();}
        // Faults have already been observed by the operation or its fault observer.
        // A settled failure must not prevent restoring the original manager.
        Runtime.Log("run-native-action-retired",new{Completed=true,Tasks=OwnedTasks.Count,Canceled=OwnedTasks.Count(t=>t.IsCanceled),Faulted=OwnedTasks.Count(t=>t.IsFaulted),BeforeOriginalManagerRestored=true});
        OwnedTasks.Clear();
    }
    internal static void Control(string command)
    {
        if(command=="stop")
        {Runtime.Log("run-playback-stop",new{Operation=_index+1,NonCombat,AtRecordedEnd=_index==_run.Operations.Count-1});
            _stop=true;_auto=false;_fine=0;_wholeRequests=0;PlayerLoop.ChoiceAbort=true;PlayerLoop.Cursor?.Stop();if(NativeSeek.Enabled)NativeSeek.Control("stop");return;}
        if(_stop)return;
        if(!NonCombat&&!NativeSeek.Finished){NativeSeek.Control(command);return;}
        if(command=="auto")_auto=!_auto;
        else if(command=="fine")_fine++;
        else if(command=="step")_wholeRequests++;
        else return;
        _continuation=true;
        Runtime.Log("run-playback-control",new{Command=command,Operation=_index+1,NonCombat,Auto=_auto,Fine=_fine,Whole=_wholeRequests});
    }
    internal static void AtBattleBoundary(bool auto){if(Active&&HasFollowing)_auto=auto;}
    internal static bool DeferRewards(CombatRoom room)
    {if(!Active||_allowRewards)return false;_rewardsRoom=room;return true;}
    internal static void Native(Action action)
    {var prior=Driving;Driving=true;try{action();}finally{Driving=prior;}}
    internal static void CheckStop()
    {
        if(_stop||PlayerLoop.TearingDown)throw new OperationCanceledException();PlayerLoop.RequireHealthy();
        if(OwnedTasks.FirstOrDefault(t=>t.IsFaulted) is {} failed)throw new InvalidDataException("run-native-owned-task-failed",failed.Exception);
    }
    internal static async Task TakePhase(string label)
    {
        Runtime.Status=label;
        while(true)
        {
            CheckStop();
            if(_auto&&_phaseAt!=0&&Stopwatch.GetElapsedTime(_phaseAt).TotalSeconds<.65){await Runtime.NextFrame();continue;}
            if(_auto||_whole)break;
            if(_wholeRequests>0){_wholeRequests--;_whole=true;break;}
            if(_fine>0){_fine--;break;}
            await Runtime.NextFrame();
        }
        _phaseAt=Stopwatch.GetTimestamp();
        Runtime.Log("run-playback-phase",new{Operation=Current?.Sequence,Label=label,Auto=_auto,Whole=_whole});
    }
    internal static RunDecision Decision(string kind,string[] options)
    {
        Codec.Require(Current is not null&&_decision<Current.Decisions.Length,"run-unexpected-native-choice");
        var expected=Current!.Decisions[_decision++];
        Codec.Require(expected.Kind==kind&&expected.Options.SequenceEqual(options),"run-native-choice-options-mismatch");return expected;
    }
    internal static async Task Wait(Func<bool> ready,string reason)
    {
        var at=Stopwatch.GetTimestamp();
        while(!ready()){CheckStop();Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<45,reason);await Runtime.NextFrame();}
    }
    internal static async Task Await(Task task)
    {
        Track(task);
        while(!task.IsCompleted){CheckStop();await Runtime.NextFrame();}
        await task;
    }
    internal static async Task<bool> Run()
    {
        Runtime.Log("run-playback-open",new{_run.Id,StartOperation=_index+1,Visits=_run.Visits.Count,SameMemorySession=true,
            RecordedOperations=_rawOperations,SelectedOperations=_run.Operations.Count,PreservedAttempts=_run.Branches?.Attempts.Count});
        try
        {
            while(!_stop)
            {
                Current=_run.Operations[_index];
                if(Current.Kind=="battle")
                {
                    NonCombat=false;await NativeSeek.Run();
                    if(_stop)return NativeSeek.AllMatched&&_index==_run.Operations.Count-1;
                    Codec.Require(NativeSeek.Finished&&HasFollowing,"run-battle-transition-without-complete-result");
                    NativeSeek.Clear();ChoiceTape.Clear();ActionMonitor.Reset();NonCombat=true;PlayerLoop.Loading=false;
                    _continuation=false;
                    _index++;continue;
                }
                Codec.Require(Current.Status=="complete"&&Current.Kind!="gap","run-recording-gap");
                NonCombat=true;_whole=false;_decision=0;
                ApplyContinuations(Current);
                if(_rewardsRoom is {} room)
                {
                    Codec.Require(Current.Kind is "reward" or "proceed","run-missing-reward-operation");
                    await TakePhase("显示战斗奖励");
                    _allowRewards=true;
                    try{Native(()=>AccessTools.Method(typeof(NCombatUi),"OnCombatWon").Invoke(NCombatRoom.Instance!.Ui,[room]));}
                    finally{_allowRewards=false;_rewardsRoom=null;}
                    await RewardsReady(requireVisible:true);
                    // Give the fully presented page a readable Auto interval.
                    _phaseAt=Stopwatch.GetTimestamp();
                }
                await Execute(Current);
                Codec.Require(_decision==Current.Decisions.Length,"run-recorded-choice-not-consumed");
                if(Current.After is {} expected)Compare(Current.Sequence,"after",expected,RunResources.Capture());
                Runtime.Log("run-operation-verified",new{_run.Id,Current.Sequence,Current.SourceSequence,Current.Attempt,Current.Visit,Current.Kind,ResourcesAndRngMatched=true,NativeBusinessExecuted=true,ExpectedStateInjected=false});
                if(_index==_run.Operations.Count-1)
                {
                    _auto=false;Runtime.Status="已到记录终点；可Stop返回菜单";
                    Runtime.Log("run-playback-endpoint",new{Operation=Current.Sequence,Auto=false,AwaitingStop=true,SceneKeptOpen=true});
                    while(!_stop)await Runtime.NextFrame();return true;
                }
                _index++;
            }
            return false;
        }
        catch(OperationCanceledException) when(_stop){return false;}
    }
    private static void ApplyContinuations(RunOperation operation)
    {
        if(!_continuations.Remove(operation.Sequence,out var points))return;
        foreach(var point in points)
        {
            var visit=_run.Visits.Single(v=>v.Id==point.Visit);
            Codec.Require(operation.Visit==point.Visit&&LiveState.Run.CurrentMapCoord is {} coord&&coord.row==visit.Row&&coord.col==visit.Col&&
                LiveState.Run.CurrentRoom is CombatRoom {IsPreFinished:true},"run-continue-room-boundary-mismatch");
            // EnterMapPointInternal invokes this before restoring an already
            // completed room. Reuse that native history finalization while
            // keeping this playback's live RunState, resources and RNGs.
            AccessTools.Method(typeof(RunManager),"UpdatePlayerStatsInMapPointHistory").Invoke(RunManager.Instance,[]);
            Runtime.Log("run-native-continue-boundary",new{Operation=operation.Sequence,point.Visit,LegacyVisitBoundary=point.BeforeOperation is null,
                NativeHistoryFinalized=true,SameRunState=true,CheckpointLoaded=false,ExpectedStateInjected=false});
        }
    }
    private static void Compare(int sequence,string point,string expected,string actual)
    {
        if(expected==actual)return;
        Runtime.Log("run-resource-difference",new{Sequence=sequence,Point=point,Expected=expected,Actual=actual});
        throw new InvalidDataException("run-resources-or-rng-mismatch-"+sequence+"-"+point);
    }
    private static async Task Execute(RunOperation operation)
    {
        if(operation.Kind is "reward" or "proceed")await RewardsReady(requireVisible:false);
        if(operation.Kind=="map")await Wait(()=>NMapScreen.Instance is {IsOpen:true,IsTravelEnabled:true} map&&!TweenRunning(map,"_tween")&&!TweenRunning(map,"_actAnimTween"),"run-map-not-ready");
        if(operation.Kind=="rest")await Wait(()=>NRestSiteRoom.Instance is not null,"run-rest-site-not-ready");
        await TakePhase(operation.Kind switch{"reward"=>"领取奖励","map"=>"地图选路","rest"=>"营火选择","treasure-open"=>"打开宝箱","treasure-pick"=>"领取遗物",
            "proceed" or "rest-proceed" or "treasure-proceed"=>"离开房间",_=>operation.Kind});
        if(operation.Before is {} expected)Compare(operation.Sequence,"before",expected,RunResources.Capture());
        switch(operation.Kind)
        {
            case "treasure-open":case "treasure-pick":case "treasure-proceed":await RunTreasures.Play(operation);break;
            case "reward":
                var rewards=RunPatches.Rewards!;var index=operation.Argument.RewardIndex??-1;
                Codec.Require(index>=0&&index<rewards.Rewards.Count,"run-reward-index");var reward=rewards.Rewards[index];
                Codec.Require(RunPatches.RewardKey(reward)==operation.Argument.RewardKey,"run-reward-content-mismatch");
                var buttons=(IEnumerable<Control>)RunPatches.Field(RunPatches.RewardsScreen!,"_rewardButtons")!;
                var button=buttons.OfType<NRewardButton>().SingleOrDefault(b=>ReferenceEquals(b.Reward,reward));
                Codec.Require(button is not null,"run-reward-button-missing");
                RunPatches.UiTask=null;RunPatches.ResultTask=null;
                Native(()=>AccessTools.Method(typeof(NRewardButton),"OnRelease").Invoke(button,[]));
                Codec.Require(RunPatches.UiTask is not null&&RunPatches.ResultTask is not null,"run-reward-native-handler-not-started");
                await Await(RunPatches.UiTask!);
                Codec.Require(RunPatches.ResultTask!.Result==operation.Success,"run-reward-result-mismatch");break;
            case "rest":
                Codec.Require(operation.Argument.Option is "HEAL" or "SMITH","run-rest-option-unsupported");
                var options=RunManager.Instance.RestSiteSynchronizer.GetLocalOptions();var option=options.ToList().FindIndex(o=>o.OptionId==operation.Argument.Option);
                Codec.Require(option>=0&&options[option].IsEnabled,"run-rest-option-unavailable");
                var restButton=NRestSiteRoom.Instance!.GetButtonForOption(options[option]);
                Codec.Require(restButton is not null,"run-rest-button-missing");
                await Wait(()=>restButton!.Modulate.A>=.99f,"run-rest-button-not-presented");
                RunPatches.UiTask=null;RunPatches.ResultTask=null;RunPatches.RestAnimation=null;
                Native(()=>AccessTools.Method(restButton!.GetType(),"OnRelease").Invoke(restButton,[]));
                Codec.Require(RunPatches.UiTask is not null&&RunPatches.ResultTask is not null,"run-rest-native-handler-not-started");
                await Await(RunPatches.UiTask!);
                Codec.Require(RunPatches.ResultTask!.Result==operation.Success,"run-rest-result-mismatch");
                if(RunPatches.RestAnimation is {} animation)await Await(animation);break;
            case "proceed":
                await Wait(()=>((MegaCrit.Sts2.Core.Nodes.CommonUi.NProceedButton)RunPatches.Field(RunPatches.RewardsScreen!,"_proceedButton")!).IsEnabled,"run-reward-proceed-disabled");
                RunPatches.ProceedTask=null;
                Native(()=>AccessTools.Method(RunPatches.RewardsScreen!.GetType(),"OnProceedButtonPressed").Invoke(RunPatches.RewardsScreen,[null]));
                await Wait(()=>RunPatches.ProceedTask is not null,"run-native-proceed-not-started");
                await Await(RunPatches.ProceedTask!);break;
            case "rest-proceed":
                await Wait(()=>NRestSiteRoom.Instance?.ProceedButton.IsEnabled==true,"run-rest-animation-pending");
                Native(()=>AccessTools.Method(typeof(NRestSiteRoom),"OnProceedButtonReleased").Invoke(NRestSiteRoom.Instance,[null]));break;
            case "map":
                var coord=new MapCoord(operation.Argument.Col!.Value,operation.Argument.Row!.Value);var run=LiveState.Run;
                Codec.Require(run.CurrentMapPoint!.Children.Any(p=>p.coord.Equals(coord)),"run-recorded-route-not-reachable");
                var point=run.Map.GetPoint(coord)??throw new InvalidDataException("run-map-point-missing");
                PlayerLoop.ReleaseBattleForTravel();
                async Task Enter()
                {
                    RunPatches.Clear();
                    _mapRun=run;_mapDestination=coord;_mapGeneration=RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount;
                    _mapVoted=false;_mapMove=null;_mapTask=null;
                    var screen=NMapScreen.Instance??throw new InvalidDataException("run-map-screen-missing");
                    var dictionary=(System.Collections.IDictionary)RunPatches.Field(screen,"_mapPointDictionary")!;
                    try
                    {
                        Native(()=>screen.OnMapPointSelectedLocally((NMapPoint)dictionary[coord]!));
                        await Wait(()=>_mapTask is not null,"run-native-travel-not-started");
                        await Await(_mapTask!);
                        Codec.Require(RunPatches.Travel is not null,"run-native-room-entry-not-started");await Await(RunPatches.Travel!);
                    }
                    finally{_mapRun=null;}
                }
                if(point.PointType==MapPointType.Monster)
                {
                    Codec.Require(_index+1<_run.Operations.Count&&_run.Operations[_index+1] is {Kind:"battle",Status:"complete",Clip:not null},"run-next-battle-recording-missing");
                    var battle=_run.Operations[_index+1];var clip=Runtime.ReadClip(battle.Clip!);
                    await PlayerLoop.LoadEnteredBattle(clip,Enter);
                }
                else {Codec.Require(point.PointType is MapPointType.RestSite or MapPointType.Treasure,"run-next-room-unsupported");await Enter();}
                break;
            default:throw new InvalidDataException("run-operation-unsupported-"+operation.Kind);
        }
    }
    private static bool TweenRunning(object owner,string name)=>RunPatches.Field(owner,name) is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning();
    private static async Task RewardsReady(bool requireVisible)
    {
        await Wait(()=>RunPatches.Rewards is not null&&RunPatches.RewardsScreen is {} screen&&GodotObject.IsInstanceValid(screen)&&screen.IsNodeReady()&&screen.IsVisibleInTree()&&
            ReferenceEquals(NOverlayStack.Instance?.Peek(),screen)&&!TweenRunning(screen,"_fadeTween")&&
            ((!requireVisible&&screen.IsComplete)||((Control)RunPatches.Field(screen,"_rewardsWindow")!).Modulate.A>=.99f),"run-reward-screen-not-presented");
        // Node readiness and tween completion can precede this frame's draw.
        await Runtime.NextFrame();CheckStop();await Runtime.NextFrame();
    }
    internal static bool MapAction(GameAction action)
    {
        if(!Active||action is not (VoteForMapCoordAction or MoveToMapCoordAction))return false;
        if(_stop||PlayerLoop.TearingDown)return false;
        Codec.Require(_mapRun is not null&&ReferenceEquals(_mapRun,LiveState.Run)&&action.OwnerId==LiveState.Player.NetId&&
            RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount==_mapGeneration,"run-map-action-outside-recorded-transition");
        if(action.ToNetAction() is NetVoteForMapCoordAction vote)
        {
            Codec.Require(Driving&&!_mapVoted&&vote.source==_mapRun!.MapLocation&&vote.destination is {} target&&target.coord.Equals(_mapDestination)&&target.mapGenerationCount==_mapGeneration,"run-map-vote-mismatch");
            _mapVoted=true;
        }
        else if(action.ToNetAction() is NetMoveToMapCoordAction move)
        {
            Codec.Require(_mapVoted&&_mapMove is null&&move.destination.Equals(_mapDestination),"run-map-move-mismatch");
            _mapMove=(MoveToMapCoordAction)action;
        }
        Runtime.Log("run-native-map-action-owned",new{Operation=Current?.Sequence,Action=action.GetType().Name,Generation=_mapGeneration});return true;
    }
    internal static void MapTask(MoveToMapCoordAction action,Task task)
    {if(Active&&ReferenceEquals(action,_mapMove)){_mapTask=task;Track(task);}}
}
