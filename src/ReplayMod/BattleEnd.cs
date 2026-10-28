using System.Text.Json;
using System.Diagnostics;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal static class BattleEnd
{
    internal static CombatRoom? Room;
    private static Creature[] _enemies=[];
    internal static Task? VictoryTask;
    private static Task[] _deathTasks=[];
    internal static bool Ending,WonEvent;
    internal static string? Projection;
    private static long _wonAt;
    private static bool _readyLogged;
    internal static void Reset(){Room=null;_enemies=[];VictoryTask=null;_deathTasks=[];Ending=false;WonEvent=false;Projection=null;_wonAt=0;_readyLogged=false;}
    internal static void Begin()
    {
        Reset();Room=(CombatRoom)LiveState.Run.CurrentRoom!;_enemies=Room.CombatState.Enemies.ToArray();
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
}
