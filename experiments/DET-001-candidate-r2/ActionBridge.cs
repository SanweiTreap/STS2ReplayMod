using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Player=MegaCrit.Sts2.Core.Entities.Players.Player;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using Det001.Core;

namespace Det001;

// All handlers are observers wrapped by Probe.Safe; no new wrapper around an action/effect Task.
internal static class ActionBridge
{
    private sealed class Pending(GameAction action,AcceptedAction planned,int turn,CardModel? card)
    {
        internal readonly GameAction Action=action;
        internal readonly AcceptedAction Planned=planned;
        internal readonly int Turn=turn;
        internal readonly CardModel? Card=card;
        internal Task? Effect;
        internal bool Executed,Cancelled,Choice,EnteredCard,EndAccepted,Finished;
    }
    private static Pending? _pending;
    private static readonly HashSet<GameAction> Seen=new(ReferenceEqualityComparer.Instance);
    private static ActionObservation? _observations;
    private static Flow? _diagnosticFlow;
    private static void Observe(ActionSite site,Action body)
    {
        var flow=Probe.CurrentFlow;if(flow is null)return;
        if(!ReferenceEquals(flow,_diagnosticFlow)) { _diagnosticFlow=flow; _observations=new(flow,Probe.Fail); }
        _observations!.Guard(site,body);
    }
    internal static void Queue(GameAction action)=>Observe(ActionSite.Queue,()=>QueueCore(action));
    private static void QueueCore(GameAction action)
    {
        var flow=Probe.CurrentFlow;
        if(flow is null || flow.Stage is Stage.Startup or Stage.ReadyArm or Stage.AwaitSave or Stage.ProductionSave or Stage.Seal or Stage.ReadyB or Stage.Terminal or Stage.Complete)return;
        if(action is not (PlayCardAction or EndPlayerTurnAction))
        { if(ActionQueueSet.IsGameActionPlayerDriven(action))Probe.Fail("unsupported-player-action"); return; }
        if(!Seen.Add(action))return; // Repeated notification for the same object is not a second operation.
        ActionObservation.Require(Seen.Count<=8 && flow.Stage==Stage.AComplete,"extra-action-or-not-ready");
        var state=RunManager.Instance.DebugOnlyGetState(); ActionObservation.Require(state is not null,"action-state");
        ActionObservation.Require(Probe.Identity() && state!.Players.Count==1,"action-environment");
        var player=state!.Players[0]; var observedTurn=player.PlayerCombatState?.TurnNumber; ActionObservation.Require(observedTurn.HasValue,"action-turn"); var turn=observedTurn!.Value;
        var trajectory=Probe.CurrentTrajectory; ActionObservation.Require(trajectory is not null,"trajectory-missing");
        var planned=ActionObservation.Core(()=>trajectory!.Planned(turn),ActionReject.PlanOrRequest);
        if(!_observations!.Binding(planned))return;
        var live=Projection.Thaw(LiveRead.Capture()); ActionObservation.Core(()=>Actions.Validate(planned,live,turn),ActionReject.PreState);
        CardModel? card=null;
        if(action is PlayCardAction play)
        {
            ActionObservation.Require(planned.Type==Operation.CardPlay && ReferenceEquals(play.Player,player) && play.TargetId is null,"wrong-card-action");
            card=player.PlayerCombatState!.Hand.Cards[ActionObservation.Core(()=>Actions.Resolve(planned.Card!,live),ActionReject.CardLocator)];
            ActionObservation.Require(card is DefendIronclad && ReferenceEquals(play.NetCombatCard.ToCardModel(),card) &&
                card.Id.ToString()==planned.Card!.Model && card.TargetType==MegaCrit.Sts2.Core.Entities.Cards.TargetType.Self,"wrong-card-instance");
        }
        else
        {
            ActionObservation.Require(planned.Type==Operation.EndTurn && action.OwnerId==player.NetId && !CombatManager.Instance.IsPlayerReadyToEndTurn(player),"wrong-end-action");
            // Exact private scheduled turn is checked against production before the Action is allowed to count.
            var scheduled=(int)HarmonyLib.AccessTools.Field(typeof(EndPlayerTurnAction),"_turnNumber").GetValue(action)!;
            ActionObservation.Require(scheduled==turn,"stale-end-turn");
        }
        ActionObservation.Core(()=>trajectory!.Requested(planned,turn,Probe.Dispatching),ActionReject.PlanOrRequest);
        ActionObservation.Require(flow.ActionStarted(),"action-start-deadline");
        var pending=new Pending(action,planned,turn,card); _pending=pending;
        if(!_observations.Requested(planned))return;
        action.BeforeExecuted+=a=>Observe(ActionSite.BeforeExecuted,()=> { ActionObservation.Require(ReferenceEquals(a,pending.Action) && !pending.Executed,"duplicate-execute"); pending.Executed=true; _observations!.Executed(); });
        action.BeforeCancelled+=_=>Observe(ActionSite.Cancel,()=> { pending.Cancelled=true; _observations!.Cancelled(); Probe.Fail("action-cancelled-not-accepted"); });
        action.BeforePausedForPlayerChoice+=_=>Observe(ActionSite.Choice,()=> { pending.Choice=true; _observations!.Choice(); Probe.Fail("unexpected-action-choice"); });
        action.AfterFinished+=a=>Observe(ActionSite.AfterFinished,()=>Finished(pending,a));
    }
    internal static void CardEffect(CardModel card,Creature? target,bool auto,Task effect)=>Observe(ActionSite.CardEffect,()=>CardEffectCore(card,target,auto,effect));
    private static void CardEffectCore(CardModel card,Creature? target,bool auto,Task effect)
    {
        var pending=_pending;
        if(pending is null || Probe.CurrentFlow?.Stage!=Stage.Action)return;
        ActionObservation.Require(pending.Planned.Type==Operation.CardPlay && ReferenceEquals(card,pending.Card) && target is null && !auto &&
            ReferenceEquals(RunManager.Instance.ActionExecutor.CurrentlyRunningAction,pending.Action) && !pending.EnteredCard,"card-effect-origin");
        pending.EnteredCard=true; pending.Effect=effect;
        _observations!.CardEffect(effect.Status);
    }
    internal static bool BeforeEnd(Player player)
    { var ready=false; Observe(ActionSite.EndReady,()=>ready=CombatManager.Instance.IsPlayerReadyToEndTurn(player)); return ready; }
    internal static void EndReady(Player player,bool wasReady)=>Observe(ActionSite.EndReady,()=>EndReadyCore(player,wasReady));
    private static void EndReadyCore(Player player,bool wasReady)
    {
        var pending=_pending;
        if(pending is null || Probe.CurrentFlow?.Stage!=Stage.Action)return;
        ActionObservation.Require(pending.Planned.Type==Operation.EndTurn && !wasReady && CombatManager.Instance.IsPlayerReadyToEndTurn(player) &&
            ReferenceEquals(RunManager.Instance.ActionExecutor.CurrentlyRunningAction,pending.Action) &&
            ReferenceEquals(RunManager.Instance.DebugOnlyGetState()!.Players[0],player) && !pending.EndAccepted,"end-not-accepted-transition");
        pending.EndAccepted=true;
        _observations!.EndReady();
    }
    private static void Finished(Pending p,GameAction action)
    {
        if(p.Finished)return; p.Finished=true;
        if(!_observations!.Finished(p.Effect?.Status,action.Exception is not null))return;
        ActionObservation.Require(ReferenceEquals(p.Action,action) && p.Executed && !p.Cancelled && !p.Choice && action.State==GameActionState.Finished && action.Exception is null,"action-completion-failed");
        ActionObservation.Require(p.Planned.Type==Operation.CardPlay?p.EnteredCard && p.Effect?.IsCompletedSuccessfully==true:p.EndAccepted,"no-accepted-effect");
        Probe.ActionSucceeded(p.Planned,p.Turn);
    }
}
