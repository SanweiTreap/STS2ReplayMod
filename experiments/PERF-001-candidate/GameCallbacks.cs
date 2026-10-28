using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using Perf001Candidate.Core;

namespace Perf001Candidate;

public static class GameCallbacks
{
    public static void Menu()=>Probe.Menu();
    public static void Fresh(RunState state,bool shouldSave)=>Probe.Guard(()=>
    {
        if(Probe.Flow is null||Probe.View is null)return;Probe.CheckPublishedIdentity();Probe.View.FreshState=state;
        Probe.Flow.NewRun(state.GameMode.ToString(),state.Rng.ToSerializable().Seed??throw new InvalidDataException("live-seed-missing"),shouldSave,state.Players.Count,state.Modifiers.Count,state.AscensionLevel);
    });
    public static void Saved()=>Probe.Guard(()=>Probe.Flow?.ContinueObserved());
    public static void MapBefore(MapCoord coord,out bool __state)
    {
        var measured=false;Probe.Guard(()=>
        {
            if(Probe.Flow is null||Probe.Flow.Terminal||Probe.View is null)return;
            if(Probe.Flow.Stage==Stage.AwaitMap&&Probe.View.IsInitialStartingCoordinate(coord))
            {Probe.Flow.InitialStartingMapObserved();return;}
            Probe.Flow.MapRequested(Probe.View.IsTarget(coord));measured=Probe.Flow.Stage==Stage.Loading;
        });__state=measured;
    }
    public static void MapAfter(bool __state,Task __result)=>Probe.Guard(()=>{if(__state)Probe.Flow?.MapReturned(__result);});
    public static void Queued(GameAction action)=>Probe.Guard(()=>
    {
        if(Probe.Flow is null||Probe.View is null||Probe.Flow.Terminal)return;
        if(action is VoteForMapCoordAction){Probe.Flow.MapInputRequested(Probe.View.IsExpectedMapQueue(action));return;}
        if(action is MoveToMapCoordAction){Probe.View.PendingMapMove=action;Probe.Flow.MapMoveQueued(Probe.View.IsExpectedMapQueue(action));return;}
        if(action is not (PlayCardAction or EndPlayerTurnAction))
        {if(action is not (GenericHookGameAction or ReadyToBeginEnemyTurnAction))Probe.Flow.Fail("unsupported-action-"+action.GetType().Name);return;}
        Probe.View.PendingAction=action;
        Probe.Flow.ActionRequested(action is PlayCardAction,Probe.View.Player.PlayerCombatState?.TurnNumber??0);
    });
    public static void Execute(GameAction __instance)=>Probe.Guard(()=>
    {if(__instance is PlayCardAction or EndPlayerTurnAction)Identity.Require(ReferenceEquals(__instance,Probe.View?.PendingAction),"executed-action-origin");});
    public static void ActionTask(GameAction __instance,Task __result)=>Probe.Guard(()=>
    {if(ReferenceEquals(__instance,Probe.View?.PendingAction))Probe.Flow?.ActionTaskReturned(__result);});
    public static void MapMoveTask(GameAction __instance,Task __result)=>Probe.Guard(()=>
    {if(ReferenceEquals(__instance,Probe.View?.PendingMapMove))Probe.Flow?.MapTravelTaskReturned(__result);});
    public static void CardBefore(CardModel __instance,bool isAutoPlay)=>Probe.Guard(()=>
    {Probe.Flow?.CardEntered(Probe.View!.IsExpectedCard(__instance,isAutoPlay),isAutoPlay);});
    public static void CardAfter(Task __result)=>Probe.Guard(()=>Probe.Flow?.CardTaskReturned(__result));
    public static void EndBefore(Player player,out bool __state)
    {
        var before=true;Probe.Guard(()=>{if(Probe.Flow?.Stage==Stage.EndRequest&&Probe.View?.PendingAction is EndPlayerTurnAction&&ReferenceEquals(player,Probe.View.Player))
            before=CombatManager.Instance.IsPlayerReadyToEndTurn(player);});__state=before;
    }
    public static void EndAfter(Player player,bool __state)=>Probe.Guard(()=>
    {if(Probe.Flow?.Stage==Stage.EndRequest)Probe.Flow.EndAccepted(__state,CombatManager.Instance.IsPlayerReadyToEndTurn(player),ReferenceEquals(player,Probe.View?.Player));});
    public static void TurnBefore(object __0,out int __state)
    {
        var generation=0;Probe.Guard(()=>
        {
            if(Probe.Flow is null||Probe.Flow.Terminal)return;
            var state=(CombatState?)AccessTools.Property(__0.GetType(),"State").GetValue(__0);
            Identity.Require(ReferenceEquals(state,CombatManager.Instance.DebugOnlyGetState()),"stale-turn-combat-origin");
            generation=Probe.Flow.TurnStarted(state?.CurrentSide==CombatSide.Player);
        });__state=generation;
    }
    public static void TurnAfter(int __state,Task __result)=>Probe.Guard(()=>Probe.Flow?.TurnTaskReturned(__state,__result));
    public static void Cancelled(GameAction __instance)=>Probe.Guard(()=>
    {if(ReferenceEquals(__instance,Probe.View?.PendingAction))Probe.Flow?.Fail("original-action-cancelled");});
    public static void OriginalFault(Exception? __exception)
    {if(__exception is not null)Probe.Guard(()=>Probe.Flow?.Fail("original-synchronous-fault"));}
    public static void CandidateMap(bool __state){if(Probe.FrozenMode!=Mode.C2)return;Probe.Guard(()=>{if(__state)Probe.Flow?.Candidate(EventKind.MapCommit);});}
    public static void CandidateCard(){if(Probe.FrozenMode!=Mode.C2)return;Probe.Guard(()=>Probe.Flow?.Candidate(EventKind.CardAccepted));}
    public static void CandidateEnd(bool __state)
    {if(Probe.FrozenMode!=Mode.C2)return;Probe.Guard(()=>{if(!__state&&Probe.Flow?.Stage==Stage.EndTurn)Probe.Flow.Candidate(EventKind.EndTurnAccepted);});}
}
