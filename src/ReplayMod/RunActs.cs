using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// The rewards button owns the native vote. The vote itself starts the native
// transition asynchronously; observe that task rather than entering an act twice.
internal static class RunActs
{
    internal static bool StopTestArmed;
    private static RunOperation? _recorded;
    private static Task? _transition;
    private static RunState? _owner;
    private static int _source;
    private static bool _ready,_voted;
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(ActChangeSynchronizer),"SetLocalPlayerReady"),
            prefix:new HarmonyMethod(typeof(RunActs),nameof(Ready)));
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"EnterNextAct"),
            postfix:new HarmonyMethod(typeof(RunActs),nameof(Transition)));
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"SetActInternal"),
            postfix:new HarmonyMethod(typeof(RunActs),nameof(ActSet)));
    }
    private static bool Ready()
    {
        if(!RunPlayback.Active)
        {
            if(RunRecording.Enabled)
            {
                if(LiveState.Run.CurrentActIndex is <0 or >2||LiveState.Run.CurrentRoom is not CombatRoom {RoomType:RoomType.Boss,IsPreFinished:true})
                {Runtime.Guard(()=>RunRecording.Unsupported("act-proceed","前往下一幕/终局不在已完成Boss边界"));return true;}
                _recorded=RunPatches.Record("act-proceed",()=>RunRecording.Begin("act-proceed",new()));
            }
            return true;
        }
        var owned=false;Runtime.Guard(()=>
        {
            Codec.Require(RunPlayback.Driving&&RunPlayback.NonCombat&&RunPlayback.Current?.Kind=="act-proceed"&&
                ReferenceEquals(_owner,LiveState.Run)&&_owner.CurrentActIndex==_source&&!_ready,"act-ready-outside-recorded-operation");
            _ready=true;owned=true;
        });return owned;
    }
    internal static bool OwnsVote(VoteToMoveToNextActAction action)
    {
        Codec.Require(RunPlayback.Active&&RunPlayback.NonCombat&&RunPlayback.Current?.Kind=="act-proceed"&&
            _ready&&!_voted&&ReferenceEquals(_owner,LiveState.Run)&&action.CurrentActIndex==_source&&
            action.OwnerId==LiveState.Player.NetId,"act-vote-outside-recorded-operation");
        _voted=true;Runtime.Log("run-native-act-vote-owned",new{Operation=RunPlayback.Current!.Sequence,Act=_source,
            NativeQueue=true,CheckpointLoaded=false});return true;
    }
    private static void Transition(ref Task __result)
    {
        if(!RunPlayback.Active&&_recorded is null)return;
        var operation=_recorded;_recorded=null;
        if(RunPlayback.Active)
            Codec.Require(_ready&&_voted&&_transition is null&&ReferenceEquals(_owner,LiveState.Run)&&RunPlayback.Current?.Kind=="act-proceed",
                "act-transition-without-native-vote");
        __result=RunRecording.Wrap(__result,operation);
        if(RunPlayback.Active){_transition=__result;RunPlayback.Track(__result);}
    }
    private static void ActSet(int actIndex,ref Task __result)
    {
        if(!RunPlayback.Active||RunPlayback.Current?.Kind!="act-proceed")return;
        var native=__result;__result=Boundary();RunPlayback.Track(__result);
        async Task Boundary()
        {
            await native;
            Codec.Require(ReferenceEquals(_owner,LiveState.Run)&&actIndex==_source+1&&LiveState.Run.CurrentActIndex==actIndex,
                "native-next-act-index-mismatch");
            Runtime.Log("run-native-act-map-created",new{Act=actIndex,Model=LiveState.Run.Act.Id.ToString(),
                NativeMapGenerated=true,SameRunState=true,CheckpointLoaded=false});
            if(StopTestArmed)
            {
                StopTestArmed=false;
                Runtime.Log("run-act-loading-stop-test",new{Phase="SetActInternal-completed-before-EnterAct-scene-finished",
                    OneShotConsumed=true,PublicControl="PlayerLoop.Control(stop)"});
                PlayerLoop.Control("stop");
            }
        }
    }
    internal static async Task Play()
    {
        var run=LiveState.Run;
        Codec.Require(run.CurrentActIndex is >=0 and <=2&&run.CurrentRoom is CombatRoom {RoomType:RoomType.Boss,IsPreFinished:true}&&
            run.Acts.Count==3,"act-proceed-not-at-supported-completed-boss");
        _owner=run;_source=run.CurrentActIndex;_transition=null;_ready=false;_voted=false;
        PlayerLoop.ReleaseBattleForTravel();
        try
        {
            var screen=RunPatches.RewardsScreen??throw new InvalidDataException("act-rewards-screen-missing");
            await RunPlayback.Wait(()=>((MegaCrit.Sts2.Core.Nodes.CommonUi.NProceedButton)RunPatches.Field(screen,"_proceedButton")!).IsEnabled,
                "act-proceed-button-not-ready");
            RunPlayback.Native(()=>AccessTools.Method(typeof(NRewardsScreen),"OnProceedButtonPressed").Invoke(screen,[null]));
            Codec.Require(_ready&&_voted,"act-native-rewards-vote-not-submitted");
            await RunPlayback.Wait(()=>_transition is not null,"act-native-transition-not-started");
            // Once submitted, native asset loading, scene replacement and hooks
            // must settle before Stop can tear down the isolated context.
            await _transition!;RunPlayback.CheckStop();
            if(_source==run.Acts.Count-1)
            {
                Codec.Require(ReferenceEquals(run,LiveState.Run)&&run.CurrentActIndex==_source&&run.CurrentRoom?.IsVictoryRoom==true,
                    "native-final-event-entry-mismatch");
                RunPlayback.VerifyFinalEvent();return;
            }
            Codec.Require(ReferenceEquals(run,LiveState.Run)&&run.CurrentActIndex==_source+1&&run.CurrentRoom is MapRoom&&
                run.CurrentMapCoord is null&&run.VisitedMapCoords.Count==0,"act-native-entry-boundary-mismatch");
            Runtime.Log("run-native-act-transition-verified",new{FromAct=_source,ToAct=run.CurrentActIndex,
                Model=run.Act.Id.ToString(),SameRunState=true,NativeRewardsVote=true,NativeTransitionCalls=1,
                CheckpointLoaded=false,ExpectedStateInjected=false});
        }
        finally{_owner=null;}
    }
    internal static void Clear(){_recorded=null;_transition=null;_owner=null;_ready=false;_voted=false;}
}
