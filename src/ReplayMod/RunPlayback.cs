using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
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
    private static bool _auto,_whole,_stop,_continuation,_allowRewards,_endpoint;
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
    private static Dictionary<int,RunContinuation[]> _progressContexts=[];
    private static int _rawOperations;
    private static readonly HashSet<int> VerifiedBattles=[];
    private static readonly HashSet<int> NeededComputedSaves=[];
    private static (RunContinuation Point,RunOperation Battle)? _eventLoad;
    // This exact recording build has the same run/clip format and native inputs.
    internal const string RecordingBuild="145E3A3DE8A918CBD8AE90EBF4EFE1D7413E3F46911A1A5C26298266828F66EC";
    internal const string AcceptedChainRecordingBuild="DD563F456E5A693E9139F669B3C7E9B95ECFB11D6096A32A79F6EE82E89CF1FA";
    internal const string Run001Baseline="392B6DEB1899B1D5D9E71325382F2999D049CC16E0A9568A9B08951017D4E9DB";
    internal const string TreasureRecordingBuild="2FB5E5938104CA23256FFF25895DD2AB27A324A31CDD5484704F4149F4C584F3";
    internal const string TreasureObtainBuild="0DE66D1936532107143E6AE9B42E80CC94A6D5884A7C2FD5939C51EBDEE25B3E";
    internal const string AccountContextBuild="1AD070C28A881D7E9ACA8A7F13EFFD5F975F56217C6ECB8A43F8672AECB15C08";
    internal const string MerchantRecordingBuild="1C02153EFE00347294CBAADB2789ECDD304CCE495EE5C3C34FDB549047909A8F";
    internal const string EventRecordingBuild="B09A643DF2ECE027ECBDA8EB3EA437BCA6FF8086173B192AD3CA1E9CE7480C04";
    internal const string EventAcceptedBuild="3902D4A1203D9A1CCA5248B972565348ED321745EA335D3A6F86569CED6DBDC8";
    internal const string InternalEventRecordingBuild="7B27D6217ACE4D1B66A0D43FCAC4AF67E9C4DE21BD15D584D4109531CC0F108D";
    internal const string EventLoadBuild="0C9EAC791A373AB641FBABFF66E26C48CD45363BD7F559F685A1A0E05413B2EB";
    internal const string EventPresentationBuild="56163611CA0AA73A28D87CC2E2F3F33B5460AB8932D3A1CD23A62011688C1D99";
    internal const string EventStopBuild="A91D084118A288EEFF21E7CE1D7AE47E68B6FA9942066AAC3F5A4C3B959F07E4";
    internal const string NeowRecordingBuild="6FA1CE6B4D74C6290A3A264D3E7524EB29E848FE00A0BE13334738B177CDFBF1";
    internal const string EliteRecordingBuild="8178809C473A012396E85374C9BD2865AD126F7179BCF9DF48DEAFFF719189CC";
    internal const string BossRecordingBuild="69A1529F939E9452F2FF4AB96A753C532153C287378C63E01CE69154753CC0E7";
    internal const string ActRecordingBuild="BAE17032415D20C4F7844779B921379D8994ABC7C3BE69AC9106988EDADDA70B";
    internal const string AcceptedActRecordingBuild="7B7410D2A03968C5C8032E1351B078A8508D44348306F314848695BEDFE4E0B1";
    internal const string ThirdActRecordingBuild="4B591FA3D071A67E8D4332355825BF769B6708C919D2093A62F7531332DF8657";
    internal const string ThirdActPlaybackBuild="90B730881BE26B2B35E23E44383DADD976CF6C17CD40CAE97BF6DE0F203BE5F3";
    internal const string FinalEventRecordingBuild="565CD1A9A9AF0C54B09E6FF3E1179FB341D412ABF56AB5ED2D1FD7E0B551DB7A";
    internal const string PreparedFinalRecordingBuild="329A01248024F56D130AF6A09022BD9DCA2B540C5B260D413A05BA4F3B891310";
    internal const string DeathRecordingBuild="045FAF8B7A31A005889D591573919361828BACCC2BA9DF6CF00B044C1FB72172";
    internal const string AbandonRecordingBuild="5EF89002446D5E464717FC8E8A2F87845C3805871332274FE75FB6FAC1C9760B";
    internal const string FullRunRecordingBuild="572FFCB169D37C5EB7705A6306D4F4063CB8AD7F1380E81E2F3E7DD1FC7BE39F";
    internal const string FullRunVictoryRecordingBuild="D58370E28CFE3FE06F32FE96F15FF31111375F5607FDAFE8B45555DD3D384401";
    internal const string RunProgressRewindBuild="17FA8B05F614A93BE9D4857EE1ECED733CC5B0ED4F99C7D3BBDABDF1EFD5E6E3";
    internal const string RunRewardsRecordingBuild="177EE6F3709A7316761AEAB8E673FDF5A2FD4688E3C7851B19F594D029A14BF6";
    internal const string HollowRecordingBuild="912353A0EEAB33775E39EFA0538194835A65C120027BC7D74E1AA0D02DC7445D";
    internal static bool Compatible(string hash)=>hash==Runtime.ModHash||hash==HollowRecordingBuild||hash==RunRewardsRecordingBuild||hash==RunProgressRewindBuild||hash==FullRunVictoryRecordingBuild||hash==FullRunRecordingBuild||hash==AbandonRecordingBuild||hash==DeathRecordingBuild||hash==PreparedFinalRecordingBuild||hash==FinalEventRecordingBuild||hash==ThirdActPlaybackBuild||hash==ThirdActRecordingBuild||hash==RecordingBuild||hash==AcceptedChainRecordingBuild||hash==Run001Baseline||hash==TreasureRecordingBuild||hash==TreasureObtainBuild||hash==AccountContextBuild||hash==MerchantRecordingBuild||hash==EventRecordingBuild||hash==EventAcceptedBuild||hash==InternalEventRecordingBuild||hash==EventLoadBuild||hash==EventPresentationBuild||hash==EventStopBuild||hash==NeowRecordingBuild||hash==EliteRecordingBuild||hash==BossRecordingBuild||hash==ActRecordingBuild||hash==AcceptedActRecordingBuild;
    internal static bool BattleVerified(int source)=>VerifiedBattles.Contains(source);
    internal static bool NeedsComputedSave(int source)=>NeededComputedSaves.Contains(source);
    internal static RunOperation? Current;
    internal static bool HasFollowing=>Active&&_index+1<_run.Operations.Count&&_run.Operations[_index+1].Status=="complete"&&_run.Operations[_index+1].Kind!="gap";
    internal static bool WantsNext=>HasFollowing&&(_auto||_continuation);
    internal static bool Stopping=>_stop;
    internal static bool Finished=>Active&&_endpoint;
    internal static bool Auto=>_auto;
    internal static bool CompletingOperation=>_whole;
    internal static string Display=>Active?$"\n路线 · 第{_run.Visits.Single(v=>v.Id==(Current?.Visit??_run.Operations[_index].Visit)).Act+1}幕{_run.Visits.Single(v=>v.Id==(Current?.Visit??_run.Operations[_index].Visit)).Floor}层 · 操作{_index+1}/{_run.Operations.Count}"+
        (Current?.Attempt is {} attempt?$" · 采用尝试#{attempt}（最终延续路线）":"")+
        (NonCombat?$" · {(_auto?"自动连续":"手动等待／执行")} · 待播{_fine+_wholeRequests}":""):"";
    internal static int StartIndex(RunDocument run)=>run.Operations.FindIndex(o=>o.Kind=="battle"&&o.Status=="complete"&&o.Clip is not null&&o.Battle?.Entry!="event");
    internal static Clip EventBattle(string parent,string encounter)
    {
        Codec.Require(Current?.Kind=="event-choice"&&_index+1<_run.Operations.Count,"event-battle-without-parent-choice");
        var op=_run.Operations[_index+1];
        Codec.Require(op is {Kind:"battle",Status:"complete",Clip:not null}&&op.Visit==Current!.Visit&&
            op.Attempt==Current.Attempt&&op.Battle is {Entry:"event",ResumeParent:true} b&&b.ParentEvent==parent,"event-battle-stage-or-owner-mismatch");
        var clip=Runtime.ReadClip(op.Clip!);
        Codec.Require(clip.Encounter==encounter&&clip.Battle==op.Battle,"event-battle-clip-mismatch");return clip;
    }
    internal static void Begin(RunDocument run,int? startSequence=null)
    {
        _rawOperations=run.Operations.Count;run=RunRoutes.Playback(run);
        Codec.Require(run.GameHash==Runtime.GameHash&&run.Scope==Runtime.Scope&&Compatible(run.ModHash),"run-environment-or-build-mismatch");
        var opening=startSequence==0;
        _index=opening?0:startSequence is {} sequence?run.Operations.FindIndex(o=>(o.SourceSequence??o.Sequence)==sequence):StartIndex(run);
        Codec.Require(opening?RunOpening.Available(run):_index>=0&&run.Operations[_index] is {Kind:"battle",Status:"complete",Clip:not null}&&run.Operations[_index].Battle?.Entry!="event","run-has-no-recorded-battle-start");
        Codec.Require(!RunStore.HasUnrecordedAfter(run,run.Operations[_index].Sequence),"run-unrecorded-gap-after-selected-start");
        Codec.Require(!RunStore.ResourceDiscontinuities(run).Any(s=>s>run.Operations[_index].Sequence),"run-has-unrecorded-resource-transition");
        var first=run.Operations[_index];
        _progressContexts=run.Continuations.Where(c=>c.Status=="native-continue"&&c.Progress is not null&&c.BeforeOperation>=first.Sequence)
            .Select(c=>(Before:RunRoutes.ProgressBefore(run,c),Point:c)).Where(c=>c.Before>=first.Sequence)
            .GroupBy(c=>c.Before).ToDictionary(g=>g.Key,g=>g.Select(c=>c.Point).ToArray());
        _continuations=run.Continuations.Where(c=>c.PreFinished&&c.Visit>=first.Visit)
            .Select(c=>(Before:RunStore.ContinueBefore(run,c),Point:c)).Where(c=>c.Before>=first.Sequence)
            .GroupBy(c=>c.Before).ToDictionary(g=>g.Key,g=>g.Select(c=>c.Point).ToArray());
        _run=run;Active=true;NonCombat=opening;Driving=false;_auto=false;_whole=false;_stop=false;_continuation=false;
        _endpoint=false;
        RunEndings.Clear();
        _fine=0;_wholeRequests=0;_decision=0;_phaseAt=0;_rewardsRoom=null;OwnedTasks.Clear();_mapRun=null;Current=run.Operations[_index];RunPatches.Clear();
        VerifiedBattles.Clear();NeededComputedSaves.Clear();_eventLoad=null;
        foreach(var point in _continuations.Values.SelectMany(p=>p))
        {
            var battle=RunRoutes.CompletedEventBattle(run,point)??RunRoutes.CompletedMapBattle(run,point);
            if(battle is not null)NeededComputedSaves.Add(battle.SourceSequence??battle.Sequence);
        }
    }
    internal static void Clear(){Active=false;NonCombat=false;Driving=false;_endpoint=false;Current=null;_rewardsRoom=null;OwnedTasks.Clear();VerifiedBattles.Clear();NeededComputedSaves.Clear();_eventLoad=null;_continuations.Clear();_progressContexts.Clear();_mapRun=null;_mapMove=null;_mapTask=null;RunPatches.Clear();RunTreasures.Clear();RunMerchants.Clear();RunEvents.Clear();RunEventCombat.Clear();RunActs.Clear();RunCombatLoads.Clear();RunEndings.Clear();}
    internal static RunLifecycleEvent? TerminalFact()=>RunLifecycle.AtOperation(_run,Current!);
    internal static void VerifyDeath(string state)
    {
        var fact=TerminalFact();
        Codec.Require(Current is {Kind:"battle",Battle.Completion:"death"}&&_index==_run.Operations.Count-1&&
            fact is {Kind:"death"}&&fact.Act==LiveState.Run.CurrentActIndex&&fact.State==state,"run-native-death-position-resources-or-rng-mismatch");
    }
    internal static bool HasNativeFinalOpening=>_index+1<_run.Operations.Count&&RunStore.NativeFinalOpening(_run,Current!,_run.Operations[_index+1]);
    internal static void VerifyFinalEvent()
    {
        Codec.Require(_index+1<_run.Operations.Count,"run-final-event-recording-missing");
        var next=_run.Operations[_index+1];var visit=_run.Visits.Single(v=>v.Id==next.Visit);
        Codec.Require(next.Kind=="event-choice"&&visit is {Act:2,MapType:"Victory",RoomType:"Event",Model:"EVENT.THE_ARCHITECT"}&&
            LiveState.Run.CurrentRoom?.ModelId?.ToString()==visit.Model,"run-final-event-destination-mismatch");
        Runtime.Log("run-native-final-event-entry-verified",new{Visit=visit.Id,Model=visit.Model,SameRunState=true,
            NativeRewardsVote=true,NativeTransitionCalls=1,CheckpointLoaded=false,ExpectedStateInjected=false});
    }
    internal static string? InitialProgress()
    {
        if(!_progressContexts.Remove(_run.Operations[_index].Sequence,out var points))return Current?.Progress;
        var point=points[^1];LogProgress(point,_run.Operations[_index].Sequence,true);return point.Progress;
    }
    private static void LogProgress(RunContinuation point,int before,bool initial)=>Runtime.Log("run-native-load-progress-context",new{
        BeforeOperation=before,point.Visit,point.Attempt,point.PreFinished,InitialMemoryContext=initial,
        SHA256=Codec.Hash(System.Text.Encoding.UTF8.GetBytes(point.Progress!)),IsolatedManagerOnly=true,RunResourcesOrRngRestored=false});
    private static void ApplyProgressContexts(int before)
    {
        if(!_progressContexts.Remove(before,out var points))return;
        foreach(var point in points){MemorySession.ApplyRecordedProgress(point.Progress!);LogProgress(point,before,false);}
    }
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
        var faults=OwnedTasks.Where(t=>t.IsFaulted).Select(t=>t.Exception!.ToString()).ToArray();
        if(faults.Length>0)Runtime.Log("run-native-task-faults",new{Faults=faults,BeforeOriginalManagerRestored=true});
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
        if(_endpoint)
        {Runtime.Log("run-endpoint-control-ignored",new{Command=command,Operation=Current?.Sequence,Pending=0,RecordedEndpoint=true});return;}
        if(!NonCombat&&!NativeSeek.Finished){NativeSeek.Control(command);return;}
        if(command=="auto")_auto=!_auto;
        else if(command=="fine")_fine++;
        else if(command=="step")_wholeRequests++;
        else return;
        _continuation=true;
        Runtime.Log("run-playback-control",new{Command=command,Operation=_index+1,NonCombat,Auto=_auto,Fine=_fine,Whole=_wholeRequests});
    }
    internal static void AtBattleBoundary(bool auto){if(Active&&(HasFollowing||Current?.Battle?.Completion=="death"))_auto=auto;}
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
    internal static RunDecision? NextDecision=>Current is {} operation&&_decision<operation.Decisions.Length?operation.Decisions[_decision]:null;
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
                    ApplyProgressContexts(Current.Sequence);
                    NonCombat=false;await NativeSeek.Run();
                    if(_stop)return NativeSeek.AllMatched&&_index==_run.Operations.Count-1;
                    if(NativeSeek.DefeatReached)
                    {
                        Codec.Require(NativeSeek.AllMatched&&!HasFollowing&&RunEndings.DeathActive,"run-death-before-recorded-end");
                        VerifiedBattles.Add(Current.SourceSequence??Current.Sequence);NonCombat=true;
                        return await EndAtCurrent();
                    }
                    Codec.Require(NativeSeek.Finished&&HasFollowing,"run-battle-transition-without-complete-result");
                    Codec.Require(NativeSeek.AllMatched,"run-battle-unverified-before-continuation");VerifiedBattles.Add(Current.SourceSequence??Current.Sequence);
                    NativeSeek.Clear();ChoiceTape.Clear();ActionMonitor.Reset();NonCombat=true;PlayerLoop.Loading=false;
                    _continuation=false;
                    _index++;continue;
                }
                Codec.Require(Current.Status=="complete"&&Current.Kind!="gap","run-recording-gap");
                NonCombat=true;_whole=false;_decision=0;
                await ApplyContinuations(Current);
                if(_rewardsRoom is {} room&&room.Encounter.ShouldGiveRewards)
                {
                    Codec.Require(Current.Kind is "reward" or "proceed" or "act-proceed" or RunRewardPotions.Kind,"run-missing-reward-operation");
                    await TakePhase("显示战斗奖励");
                    _allowRewards=true;
                    try{Native(()=>AccessTools.Method(typeof(NCombatUi),"OnCombatWon").Invoke(NCombatRoom.Instance!.Ui,[room]));}
                    finally{_allowRewards=false;_rewardsRoom=null;}
                    // A native empty/completed terminal reward set hides its
                    // rewards window and leaves only Proceed. A recorded next
                    // reward still requires the actual visible offer.
                    await RewardsReady(requireVisible:Current.Kind=="reward");
                    // Give the fully presented page a readable Auto interval.
                    _phaseAt=Stopwatch.GetTimestamp();
                }
                await Execute(Current);
                Codec.Require(_decision==Current.Decisions.Length,"run-recorded-choice-not-consumed");
                if(Current.After is {} expected)Compare(Current.Sequence,"after",expected,
                    Current.Terminal is not null?RunEndings.PlaybackTerminal(Current):Current.Kind=="act-proceed"?RunActs.Result():RunResources.Capture());
                Runtime.Log("run-operation-verified",new{_run.Id,Current.Sequence,Current.SourceSequence,Current.Attempt,Current.Visit,Current.Kind,ResourcesAndRngMatched=true,NativeBusinessExecuted=true,ExpectedStateInjected=false});
                if(_index==_run.Operations.Count-1)
                    return await EndAtCurrent();
                _index++;
            }
            return false;
        }
        catch(OperationCanceledException) when(_stop){return false;}
    }
    private static async Task<bool> EndAtCurrent()
    {
        var autoAtEnd=_auto;
        _endpoint=true;_fine=0;_wholeRequests=0;_whole=false;_continuation=false;
        Runtime.Log("run-recorded-end-reached",new{Operation=Current!.Sequence,AutoAtArrival=autoAtEnd,
            Pending=0,RecordedOperationsExhausted=true,NativeGameOver=RunEndings.GameOverActive});
        _auto=false;
        if(RunEndings.GameOverActive)
        {
            try{await EndingPlayback.Run(autoAtEnd);}
            catch(OperationCanceledException) when(_stop){return true;}
        }
        Runtime.Status="已到记录终点；可Stop返回菜单";
        Runtime.Log("run-playback-endpoint",new{Operation=Current.Sequence,Auto=false,AwaitingStop=true,SceneKeptOpen=true,
            Pending=0,AdvanceDisabled=true,NativeGameOverControls=RunEndings.GameOverActive,
            AutoAtArrival=autoAtEnd,NativeSummaryCompleted=RunEndings.SummaryComplete});
        while(!_stop){CheckStop();await Runtime.NextFrame();}return true;
    }
    private static async Task ApplyContinuations(RunOperation operation)
    {
        if(!_continuations.Remove(operation.Sequence,out var points))return;
        ApplyProgressContexts(operation.Sequence);
        foreach(var point in points)
        {
            var visit=_run.Visits.Single(v=>v.Id==point.Visit);
            Codec.Require(operation.Visit==point.Visit&&LiveState.Run.CurrentActIndex==visit.Act&&
                (visit.ActModel is null||visit.ActModel==LiveState.Run.Act.Id.ToString())&&
                LiveState.Run.CurrentMapCoord is {} coord&&coord.row==visit.Row&&coord.col==visit.Col&&
                LiveState.Run.CurrentRoom is CombatRoom {IsPreFinished:true},"run-continue-room-boundary-mismatch");
            if(RunRoutes.CompletedEventBattle(_run,point) is {} battle)
            {
                Codec.Require(_eventLoad is null,"run-event-load-boundary-ambiguous");_eventLoad=(point,battle);continue;
            }
            if(RunRoutes.CompletedMapBattle(_run,point) is {} completed)
            {
                _rewardsRoom=null;
                await RunCombatLoads.Restore(point,completed);continue;
            }
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
    internal static void CompareBefore(RunOperation operation)
    {if(operation.Before is {} expected)Compare(operation.Sequence,"before",expected,RunResources.Capture());}
    private static async Task Execute(RunOperation operation)
    {
        if(operation.Kind is "reward" or "proceed" or "act-proceed" or RunRewardPotions.Kind)await RewardsReady(requireVisible:false);
        if(operation.Kind=="map")await Wait(()=>NMapScreen.Instance is {IsOpen:true,IsTravelEnabled:true} map&&!TweenRunning(map,"_tween")&&!TweenRunning(map,"_actAnimTween"),"run-map-not-ready");
        if(operation.Kind=="rest")await Wait(()=>NRestSiteRoom.Instance is not null,"run-rest-site-not-ready");
        await TakePhase(operation.Kind switch{"reward"=>"领取奖励",RunRewardPotions.Kind=>"丢弃药水","act-proceed"=>"前往下一幕","map"=>"地图选路","rest"=>"营火选择","treasure-open"=>"打开宝箱","treasure-pick"=>"领取遗物",
            "merchant-open"=>"打开商店","merchant-close"=>"关闭商店","merchant-buy"=>"购买商品","merchant-remove"=>"商店删牌","event-choice"=>"事件选项","event-return"=>"返回事件","abandon"=>"打开放弃确认",
            "proceed" or "rest-proceed" or "treasure-proceed" or "merchant-proceed" or "event-proceed"=>"离开房间",_=>operation.Kind});
        if(operation.Progress is {} progress)
        {
            MemorySession.ApplyRecordedProgress(progress);
            Runtime.Log("run-operation-progress-context",new{operation.Sequence,operation.SourceSequence,operation.Attempt,
                SHA256=Codec.Hash(System.Text.Encoding.UTF8.GetBytes(progress)),IsolatedManagerOnly=true,RunResourcesOrRngRestored=false});
        }
        // The retained map action predates a later room-entry Load. Apply the
        // Load's newer account context after that action's own input context,
        // before it generates the room for the selected restored attempt.
        ApplyProgressContexts(operation.Sequence);
        var eventLoad=_eventLoad;_eventLoad=null;
        Codec.Require(eventLoad is null||operation.Kind=="event-return","event-load-outside-recorded-return");
        if(eventLoad is null)CompareBefore(operation);
        switch(operation.Kind)
        {
            case "abandon":await RunAbandon.Play(operation);break;
            case "treasure-open":case "treasure-pick":case "treasure-proceed":await RunTreasures.Play(operation);break;
            case "merchant-open":case "merchant-close":case "merchant-proceed":case "merchant-buy":case "merchant-remove":
                await RunMerchants.Play(operation);break;
            case "event-choice":case "event-proceed":await RunEvents.Play(operation);break;
            case "act-proceed":await RunActs.Play();break;
            case "event-return":
                _rewardsRoom=null;
                if(eventLoad is {} load)await RunEventCombat.RestoreCompletedReturn(load.Point,load.Battle,operation);
                else await RunEventCombat.PlayReturn();break;
            case "reward":
                Codec.Require(await RunPatches.PlayReward(operation.Argument.RewardIndex??-1,operation.Argument.RewardKey!)==operation.Success,
                    "run-reward-result-mismatch");break;
            case RunRewardPotions.Kind:await RunRewardPotions.Play(operation);break;
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
                Codec.Require(run.CurrentMapPoint is {} source?MapTravel.GetTravelablePointsFrom(run,source).Any(p=>p.coord.Equals(coord)):
                    run.CurrentRoom is MapRoom&&coord.Equals(run.Map.StartingMapPoint.coord),"run-recorded-route-not-reachable");
                var point=run.Map.GetPoint(coord)??throw new InvalidDataException("run-map-point-missing");
                Codec.Require(_index+1<_run.Operations.Count,"run-next-room-recording-missing");
                var destination=RunRoutes.Destination(_run,operation,_run.Operations[_index+1]);
                Codec.Require(destination.Visit.Act==run.CurrentActIndex&&
                    (destination.Visit.ActModel is null||destination.Visit.ActModel==run.Act.Id.ToString()),"run-map-destination-act-mismatch");
                Codec.Require(point.PointType.ToString()==destination.Visit.MapType,"run-recorded-map-type-mismatch");
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
                        // A submitted scene load must reach its real native boundary.
                        // LoadOpening retires a choice wait; Stop must not abandon a
                        // wrapper while the underlying scene task can still run.
                        Track(_mapTask!);await _mapTask!;
                        Codec.Require(RunPatches.Travel is not null,"run-native-room-entry-not-started");Track(RunPatches.Travel!);await RunPatches.Travel!;
                    }
                    finally{_mapRun=null;}
                }
                if(destination.Type is "Monster" or "Elite" or "Boss"&&LiveState.CombatMapType(point.PointType))
                {
                    Codec.Require(_index+1<_run.Operations.Count&&_run.Operations[_index+1] is {Kind:"battle",Status:"complete",Clip:not null},"run-next-battle-recording-missing");
                    var battle=_run.Operations[_index+1];var clip=Runtime.ReadClip(battle.Clip!);
                    await PlayerLoop.LoadEnteredBattle(clip,Enter);
                }
                else
                {
                    Codec.Require(point.PointType==MapPointType.Ancient&&destination.Type=="Event"&&destination.Visit.Act is 1 or 2||
                        point.PointType is MapPointType.RestSite or MapPointType.Treasure or MapPointType.Shop or MapPointType.Unknown&&
                        destination.Type is "RestSite" or "Treasure" or "Shop" or "Event","run-next-room-unsupported");await Enter();
                }
                Codec.Require(run.CurrentRoom?.RoomType.ToString()==destination.Type&&run.CurrentRoom?.ModelId?.ToString()==destination.Model,
                    "run-generated-room-type-or-model-mismatch");
                Runtime.Log("run-native-room-verified",new{destination.Visit.Id,destination.Visit.MapType,ActualRoomType=destination.Type,
                    destination.Model,NativeEntryExecuted=true,RoomTypeForced=false});
                break;
            default:throw new InvalidDataException("run-operation-unsupported-"+operation.Kind);
        }
    }
    private static bool TweenRunning(object owner,string name)=>RunPatches.Field(owner,name) is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning();
    internal static async Task RewardsReady(bool requireVisible)
    {
        await Wait(()=>RunPatches.Rewards is not null&&RunPatches.RewardsScreen is {} screen&&GodotObject.IsInstanceValid(screen)&&screen.IsNodeReady()&&screen.IsVisibleInTree()&&
            ReferenceEquals(NOverlayStack.Instance?.Peek(),screen)&&!TweenRunning(screen,"_fadeTween")&&
            ((!requireVisible&&screen.IsComplete)||((Control)RunPatches.Field(screen,"_rewardsWindow")!).Modulate.A>=.99f),"run-reward-screen-not-presented");
        Runtime.Log("run-rewards-ready",new{Operation=Current?.Sequence,Kind=Current?.Kind,RequireVisibleOffer=requireVisible,
            RunPatches.RewardsScreen!.IsComplete,Rewards=RunPatches.Rewards!.Rewards.Count,
            NativeCompletedStateAccepted=!requireVisible&&RunPatches.RewardsScreen.IsComplete});
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
