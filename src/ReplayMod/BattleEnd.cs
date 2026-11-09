using System.Text.Json;
using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class BattleEnd
{
    internal static CombatRoom? Room;
    private static Creature[] _enemies=[];
    internal static Task? VictoryTask;
    private static Task[] _deathTasks=[];
    internal static bool Ending,WonEvent;
    internal static string? Projection;
    internal static string? DeathProjection;
    private static bool _deathScreen;
    private static long _wonAt;
    private static bool _readyLogged;
    private static Action? _restoreVictoryProgress;
    private static Tween? _playContainerFade;
    internal static void Reset(){Room=null;_enemies=[];VictoryTask=null;_deathTasks=[];Ending=false;WonEvent=false;Projection=null;DeathProjection=null;_deathScreen=false;_wonAt=0;_readyLogged=false;_restoreVictoryProgress=null;_playContainerFade=null;}
    internal static void Install(Harmony harmony)=>harmony.Patch(AccessTools.DeclaredMethod(typeof(NCombatUi),"PostCombatCleanUp",[]),
        prefix:new HarmonyMethod(typeof(BattleEnd),nameof(BeforeUiCleanup)),postfix:new HarmonyMethod(typeof(BattleEnd),nameof(AfterUiCleanup)));
    private static bool BeforeUiCleanup(NCombatUi __instance,out HashSet<ulong>? __state)
    {
        __state=null;
        if(!NativeSeek.Enabled||!WonEvent||!ReferenceEquals(NCombatRoom.Instance?.Ui,__instance))return true;
        if(NativeSeek.Restoring)return false;
        __state=__instance.GetTree().GetProcessedTweens().Select(t=>t.GetInstanceId()).ToHashSet();
        return true;
    }
    private static void AfterUiCleanup(NCombatUi __instance,HashSet<ulong>? __state)
    {
        if(__state is null)return;
        // This synchronous native method creates exactly one tween, the PlayContainer fade.
        _playContainerFade=__instance.GetTree().GetProcessedTweens().SingleOrDefault(t=>!__state.Contains(t.GetInstanceId()));
    }
    internal static void CaptureVictoryProgress(SaveManager manager)=>_restoreVictoryProgress??=MemorySession.CaptureVictoryProgress(manager);
    internal static void RestoreVictoryProgress()=>_restoreVictoryProgress?.Invoke();
    internal static void Lost(string terminalResources)
    {
        Codec.Require((Recorder.Active||PlayerLoop.Active)&&ReferenceEquals(Room,LiveState.Run.CurrentRoom)&&LiveState.Player.Creature.IsDead&&
            ActionMonitor.Pending is {Executing:true},"native-death-without-recorded-manual-operation");
        Ending=true;
        DeathProjection=JsonSerializer.Serialize(new{Outcome="Death",RoomPreFinished=Room!.IsPreFinished,
            CombatInProgress=CombatManager.Instance.IsInProgress,State=LiveState.Capture(),TerminalResources=terminalResources},Codec.Json);
        Runtime.Log(PlayerLoop.Active?"playback-death-observed":"recording-death-observed",new{NativeOnEnded=true,PlayerHp=0,OutcomeFrozenBeforeNativeHistory=true});
    }
    internal static void DeathScreen(){if(DeathProjection is not null)_deathScreen=true;}
    internal static bool DeathReady=>DeathProjection is not null&&_deathScreen&&!CombatManager.Instance.IsInProgress&&
        !RunManager.Instance.ActionExecutor.IsRunning&&RunManager.Instance.ActionExecutor.CurrentlyRunningAction is null;
    internal static void Begin()
    {
        Reset();Room=(CombatRoom)LiveState.Run.CurrentRoom!;
        // A native replay can restore a point after an earlier kill. The victory projection
        // still describes every original enemy, including those already dead at that point.
        _enemies=NativeSeek.Enabled?PlayerLoop.EnsureRoster().Initial.Where(c=>c.IsEnemy).ToArray():Room.CombatState.Enemies.ToArray();
    }
    internal static void EndStarted()
    {
        if(Room is null||!ReferenceEquals(Room,RunManager.Instance.DebugOnlyGetState()?.CurrentRoom))return;
        Ending=true;
    }
    internal static void Track(Task task){if(Ending)VictoryTask=task;}
    internal static void Won(CombatRoom room)
    {
        if(!ReferenceEquals(room,Room)||!Recorder.Active&&!PlayerLoop.Active)return;
        Codec.Require(Ending&&!CombatManager.Instance.IsInProgress&&room.IsPreFinished,"victory-event-before-finished-room");
        WonEvent=true;_wonAt=Stopwatch.GetTimestamp();
        // Freeze BEFORE the native reward UI listener can generate rewards or advance reward RNGs.
        Projection=JsonSerializer.Serialize(new{Outcome="Victory",RoomPreFinished=room.IsPreFinished,
            CombatInProgress=CombatManager.Instance.IsInProgress,State=LiveState.Capture(),
            Defeated=_enemies.Select(e=>{var rng=e.Monster!.Rng.ToSerializable();return new{Model=e.Monster.Id.ToString(),e.CombatId,e.CurrentHp,e.MaxHp,e.IsAlive,
                Rng=new{Counter=rng.counter,State0=rng.state0,State1=rng.state1,State2=rng.state2,State3=rng.state3}};}).ToArray()},Codec.Json);
        _deathTasks=NCombatRoom.Instance?.RemovingCreatureNodes.Where(GodotObject.IsInstanceValid).Select(n=>n.DeathAnimationTask).OfType<Task>().ToArray()??[];
        Runtime.Log(PlayerLoop.Active?"playback-victory-observed":"recording-victory-observed",new{Encounter=room.Encounter.Id.ToString(),EnemyCount=_enemies.Length,RoomPreFinished=true,RewardBoundary="before-generation",DeathTasks=_deathTasks.Length});
        CombatMusic.AtVictory(room);
    }
    internal static bool Ready
    {
        get
        {
            if(!Ending)return false;
            Codec.Require(VictoryTask is not {IsCanceled:true} and not {IsFaulted:true},"victory-task-failed");
            // The logical endpoint must not wait for a cosmetic death animation/fade (several seconds).
            // Native scene teardown cancels these animations if Stop is pressed while they are still playing.
            var ready=WonEvent&&Projection is not null&&VictoryTask?.IsCompletedSuccessfully==true&&
                !RunManager.Instance.ActionExecutor.IsRunning&&RunManager.Instance.ActionExecutor.CurrentlyRunningAction is null;
            if(ready&&!_readyLogged)
            {
                _readyLogged=true;Runtime.Log(PlayerLoop.Active?"playback-victory-ready":"recording-victory-ready",new{
                    WonToReadyMilliseconds=Stopwatch.GetElapsedTime(_wonAt).TotalMilliseconds,DeathAnimationsPending=_deathTasks.Count(t=>!t.IsCompleted),
                    WaitedForDeathAnimations=false,NativeVictoryTaskSucceeded=true,ExecutorIdle=true});
            }
            return ready;
        }
    }
    internal static void RetireUiCleanup()
    {
        var fade=_playContainerFade;_playContainerFade=null;
        var retired=fade is not null&&GodotObject.IsInstanceValid(fade)&&fade.IsValid()&&fade.IsRunning();
        if(retired)fade!.Kill(); // The restored checkpoint sets PlayContainer.Modulate.
        Runtime.Log("native-victory-ui-cleanup-retired",new{Captured=fade is not null,ActiveFadeRetired=retired});
    }
}
