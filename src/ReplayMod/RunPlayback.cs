using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
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
    private static Task? _pending;
    internal static RunOperation? Current;
    internal static bool HasFollowing=>Active&&_index+1<_run.Operations.Count&&_run.Operations[_index+1].Status=="complete"&&_run.Operations[_index+1].Kind!="gap";
    internal static bool WantsNext=>HasFollowing&&(_auto||_continuation);
    internal static bool Stopping=>_stop;
    internal static bool Auto=>_auto;
    internal static bool CompletingOperation=>_whole;
    internal static string Display=>Active?$"\n路线 · 房间{Current?.Visit??_run.Operations[_index].Visit}/{_run.Visits.Count} · 操作{_index+1}/{_run.Operations.Count}"+
        (NonCombat?$" · {(_auto?"自动连续":"手动等待／执行")} · 待播{_fine+_wholeRequests}":""):"";
    internal static int StartIndex(RunDocument run)=>run.Operations.FindIndex(o=>o.Kind=="battle"&&o.Status=="complete"&&o.Clip is not null);
    internal static void Begin(RunDocument run,int? startSequence=null)
    {
        Codec.Require(run.GameHash==Runtime.GameHash&&run.Scope==Runtime.Scope&&run.ModHash==Runtime.ModHash,"run-environment-or-build-mismatch");
        _index=startSequence is {} sequence?run.Operations.FindIndex(o=>o.Sequence==sequence):StartIndex(run);
        Codec.Require(_index>=0&&run.Operations[_index] is {Kind:"battle",Status:"complete",Clip:not null},"run-has-no-recorded-battle-start");
        _run=run;Active=true;NonCombat=false;Driving=false;_auto=false;_whole=false;_stop=false;_continuation=false;
        _fine=0;_wholeRequests=0;_decision=0;_rewardsRoom=null;Current=run.Operations[_index];RunPatches.Clear();
    }
    internal static void Clear(){Active=false;NonCombat=false;Driving=false;Current=null;_rewardsRoom=null;_pending=null;RunPatches.Clear();}
    internal static async Task DrainAfterSceneExit()
    {
        if(!Active||_pending is not {} task)return;
        var at=Stopwatch.GetTimestamp();
        while(!task.IsCompleted)
        {Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<5,"run-native-action-did-not-retire");await Runtime.NextFrame();}
        try{await task;}catch(OperationCanceledException) when(_stop){}
        Runtime.Log("run-native-action-retired",new{Completed=true,task.IsCanceled,BeforeOriginalManagerRestored=true});_pending=null;
    }
    internal static void Control(string command)
    {
        if(command=="stop")
        {_stop=true;_auto=false;_fine=0;_wholeRequests=0;PlayerLoop.ChoiceAbort=true;PlayerLoop.Cursor?.Stop();if(NativeSeek.Enabled)NativeSeek.Control("stop");return;}
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
    {Driving=true;try{action();}finally{Driving=false;}}
    internal static void CheckStop(){if(_stop||PlayerLoop.TearingDown)throw new OperationCanceledException();PlayerLoop.RequireHealthy();}
    internal static async Task TakePhase(string label)
    {
        Runtime.Status=label;
        while(true)
        {
            CheckStop();
            if(_auto||_whole)return;
            if(_wholeRequests>0){_wholeRequests--;_whole=true;return;}
            if(_fine>0){_fine--;return;}
            await Runtime.NextFrame();
        }
    }
    internal static RunDecision Decision(string kind,string[] options)
    {
        Codec.Require(Current is not null&&_decision<Current.Decisions.Length,"run-unexpected-native-choice");
        var expected=Current!.Decisions[_decision++];
        Codec.Require(expected.Kind==kind&&expected.Options.SequenceEqual(options),"run-native-choice-options-mismatch");return expected;
    }
    private static async Task Wait(Func<bool> ready,string reason)
    {
        var at=Stopwatch.GetTimestamp();
        while(!ready()){CheckStop();Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<45,reason);await Runtime.NextFrame();}
    }
    private static async Task Await(Task task)
    {
        _pending=task;
        _=task.ContinueWith(t=>{_ =t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
        while(!task.IsCompleted){CheckStop();await Runtime.NextFrame();}
        await task;_pending=null;
    }
    internal static async Task<bool> Run()
    {
        Runtime.Log("run-playback-open",new{_run.Id,StartOperation=_index+1,Visits=_run.Visits.Count,SameMemorySession=true});
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
                    if(_rewardsRoom is {} room)
                    {
                        _allowRewards=true;
                        try{AccessTools.Method(typeof(NCombatUi),"OnCombatWon").Invoke(NCombatRoom.Instance!.Ui,[room]);}
                        finally{_allowRewards=false;_rewardsRoom=null;}
                    }
                    _index++;continue;
                }
                Codec.Require(Current.Status=="complete"&&Current.Kind!="gap","run-recording-gap");
                NonCombat=true;_whole=false;_decision=0;
                await Execute(Current);
                Codec.Require(_decision==Current.Decisions.Length,"run-recorded-choice-not-consumed");
                if(Current.After is {} expected)Compare(Current.Sequence,"after",expected,RunResources.Capture());
                Runtime.Log("run-operation-verified",new{_run.Id,Current.Sequence,Current.Visit,Current.Kind,ResourcesAndRngMatched=true,NativeBusinessExecuted=true,ExpectedStateInjected=false});
                if(_index==_run.Operations.Count-1)
                {_auto=false;Runtime.Status="已到记录终点；可Stop返回菜单";while(!_stop)await Runtime.NextFrame();return true;}
                _index++;
            }
            return false;
        }
        catch(OperationCanceledException) when(_stop){return false;}
    }
    private static void Compare(int sequence,string point,string expected,string actual)
    {
        if(expected==actual)return;
        Runtime.Log("run-resource-difference",new{Sequence=sequence,Point=point,Expected=expected,Actual=actual});
        throw new InvalidDataException("run-resources-or-rng-mismatch-"+sequence+"-"+point);
    }
    private static async Task Execute(RunOperation operation)
    {
        if(operation.Kind is "reward" or "proceed")await Wait(()=>RunPatches.Rewards is not null&&RunPatches.RewardsScreen is { } screen&&GodotObject.IsInstanceValid(screen)&&screen.IsNodeReady(),"run-reward-screen-not-ready");
        if(operation.Kind=="map")await Wait(()=>NMapScreen.Instance is {IsOpen:true,IsTravelEnabled:true},"run-map-not-ready");
        if(operation.Kind=="rest")await Wait(()=>NRestSiteRoom.Instance is not null,"run-rest-site-not-ready");
        await TakePhase(operation.Kind switch{"reward"=>"领取奖励","map"=>"地图选路","rest"=>"营火选择","proceed" or "rest-proceed"=>"离开房间",_=>operation.Kind});
        if(operation.Before is {} expected)Compare(operation.Sequence,"before",expected,RunResources.Capture());
        Task task;
        switch(operation.Kind)
        {
            case "reward":
                var rewards=RunPatches.Rewards!;var index=operation.Argument.RewardIndex??-1;
                Codec.Require(index>=0&&index<rewards.Rewards.Count,"run-reward-index");var reward=rewards.Rewards[index];
                Codec.Require(RunPatches.RewardKey(reward)==operation.Argument.RewardKey,"run-reward-content-mismatch");
                task=RunManager.Instance.RewardsSetSynchronizer.SelectLocalReward(reward);await Await(task);
                Codec.Require(((Task<bool>)task).Result==operation.Success,"run-reward-result-mismatch");break;
            case "rest":
                Codec.Require(operation.Argument.Option is "HEAL" or "SMITH","run-rest-option-unsupported");
                var options=RunManager.Instance.RestSiteSynchronizer.GetLocalOptions();var option=options.ToList().FindIndex(o=>o.OptionId==operation.Argument.Option);
                Codec.Require(option>=0&&options[option].IsEnabled,"run-rest-option-unavailable");
                task=RunManager.Instance.RestSiteSynchronizer.ChooseLocalOption(option);await Await(task);
                Codec.Require(((Task<bool>)task).Result==operation.Success,"run-rest-result-mismatch");break;
            case "proceed":await Await(RunManager.Instance.ProceedFromTerminalRewardsScreen());break;
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
                    var screen=NMapScreen.Instance??throw new InvalidDataException("run-map-screen-missing");
                    var dictionary=(System.Collections.IDictionary)RunPatches.Field(screen,"_mapPointDictionary")!;
                    Native(()=>screen.OnMapPointSelectedLocally((NMapPoint)dictionary[coord]!));
                    await Wait(()=>RunPatches.Travel is not null,"run-native-travel-not-started");await Await(RunPatches.Travel!);
                }
                if(point.PointType==MapPointType.Monster)
                {
                    Codec.Require(_index+1<_run.Operations.Count&&_run.Operations[_index+1] is {Kind:"battle",Status:"complete",Clip:not null},"run-next-battle-recording-missing");
                    var battle=_run.Operations[_index+1];var clip=Runtime.ReadClip(battle.Clip!);
                    await PlayerLoop.LoadEnteredBattle(clip,Enter);
                }
                else {Codec.Require(point.PointType==MapPointType.RestSite,"run-next-room-unsupported");await Enter();}
                break;
            default:throw new InvalidDataException("run-operation-unsupported-"+operation.Kind);
        }
    }
}
