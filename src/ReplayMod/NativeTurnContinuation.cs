using System.Diagnostics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;

namespace ReplayMod;

// A win during turn settlement can return the native turn loop. Resume at its
// existing player-phase wait; never rerun combat setup or player turn-start hooks.
internal static class NativeTurnContinuation
{
    private static object? State=>AccessTools.Field(typeof(CombatManager),"_turnState").GetValue(CombatManager.Instance);
    private static Task? Loop=>AccessTools.Field(typeof(CombatManager),"_turnLoopTask").GetValue(CombatManager.Instance) as Task;
    private static Task? Signal(object state,string name)=>AccessTools.Property(state.GetType(),name).GetValue(state) is {} source?
        AccessTools.Property(source.GetType(),"Task").GetValue(source) as Task:null;
    internal static async Task<bool> PrepareVictoryRestore()
    {
        var state=State??throw new InvalidDataException("victory-no-native-turn-state");
        if(Loop is {IsCompleted:false}&&Signal(state,"EndTurnSignalSource") is {IsCompleted:false}&&
            Signal(state,"BeginEnemyTurnSignalSource") is {IsCompleted:false})
        {
            Runtime.Log("native-victory-turn-waiters-preserved",new{OldTasksRestored=false,CurrentWaitingLoopRetained=true});return false;
        }
        var at=Stopwatch.GetTimestamp();var old=Loop??throw new InvalidDataException("victory-no-native-turn-loop");
        while(!old.IsCompleted)
        {
            Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<5,"victory-native-turn-loop-did-not-finish");await Runtime.NextFrame();
        }
        Codec.Require(old.IsCompletedSuccessfully&&ReferenceEquals(State,state),"victory-native-turn-loop-fault-or-context-change");
        foreach(var name in new[]{"EndTurnSignalSource","BeginEnemyTurnSignalSource"})
        {
            var property=AccessTools.Property(state.GetType(),name);
            property.SetValue(state,Activator.CreateInstance(property.PropertyType));
        }
        Codec.Require(Signal(state,"EndTurnSignalSource") is {IsCompleted:false}&&Signal(state,"BeginEnemyTurnSignalSource") is {IsCompleted:false},
            "victory-fresh-turn-waiters-missing");return true;
    }
    internal static void Resume()
    {
        var state=State??throw new InvalidDataException("restore-no-native-turn-state");
        Codec.Require(Loop is {IsCompletedSuccessfully:true}&&LiveState.InputPhase,"restore-turn-loop-not-at-player-phase");
        var task=Run(state);AccessTools.Field(typeof(CombatManager),"_turnLoopTask").SetValue(CombatManager.Instance,task);
        Codec.Require(!task.IsCompleted,"restore-turn-loop-did-not-await");
        Runtime.Log("native-turn-loop-continued",new{FreshSignals=true,OldTasksRestored=false,CombatSetupRun=false,PlayerTurnStartRun=false,
            NativePhaseMethods=true,Turn=LiveState.Turn});
    }
    private static async Task Run(object state)
    {
        var manager=CombatManager.Instance;
        bool Live()=>(bool)AccessTools.Property(state.GetType(),"IsLive").GetValue(state)!;
        Task<Func<Task>?> AwaitPlayer()=>(Task<Func<Task>?>)AccessTools.Method(typeof(CombatManager),"AwaitTurnEndAndSwitchSides").Invoke(manager,[state])!;
        Task Start(Func<Task>? enemyAction)=>(Task)AccessTools.Method(typeof(CombatManager),"StartTurn").Invoke(manager,[state,enemyAction])!;
        try
        {
            var enemyAction=await AwaitPlayer();
            while(Live())
            {
                Codec.Require(ReferenceEquals(State,state),"restored-turn-loop-owner-changed");
                if(LiveState.Player.Creature.CombatState!.CurrentSide==CombatSide.Player)
                {await Start(null);enemyAction=await AwaitPlayer();}
                else {await Start(enemyAction);enemyAction=null;}
            }
        }
        catch(OperationCanceledException) when(!Live()) { }
        catch(Exception ex){Runtime.Guard(()=>throw new InvalidDataException("native-restored-turn-loop-failed",ex));}
    }
}
