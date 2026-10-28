using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Player=MegaCrit.Sts2.Core.Entities.Players.Player;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using Spike001B.Core;

namespace Spike001B;

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
    internal static void Queue(GameAction action)
    {
        var flow=Probe.CurrentFlow;
        if(flow is null || flow.Stage is Stage.Startup or Stage.ReadyArm or Stage.AwaitSave or Stage.ProductionSave or Stage.Seal or Stage.ReadyB or Stage.Terminal or Stage.Complete)return;
        if(action is not (PlayCardAction or EndPlayerTurnAction))
        { if(ActionQueueSet.IsGameActionPlayerDriven(action))Probe.Fail("unsupported-player-action"); return; }
        if(!Seen.Add(action))return; // Repeated notification for the same object is not a second operation.
        Contract.Require(Seen.Count<=8 && flow.Stage==Stage.AComplete,"extra-action-or-not-ready");
        var state=RunManager.Instance.DebugOnlyGetState()??throw new InvalidDataException("action-state");
        Contract.Require(Probe.Identity() && state.Players.Count==1,"action-environment");
        var player=state.Players[0]; var turn=player.PlayerCombatState?.TurnNumber??throw new InvalidDataException("action-turn");
        var trajectory=Probe.CurrentTrajectory??throw new InvalidDataException("trajectory-missing");
        var planned=trajectory.Planned(turn);
        var live=Projection.Thaw(LiveRead.Capture()); Actions.Validate(planned,live,turn);
        CardModel? card=null;
        if(action is PlayCardAction play)
        {
            Contract.Require(planned.Type==Operation.CardPlay && ReferenceEquals(play.Player,player) && play.TargetId is null,"wrong-card-action");
            card=player.PlayerCombatState!.Hand.Cards[Actions.Resolve(planned.Card!,live)];
            Contract.Require(card is DefendIronclad && ReferenceEquals(play.NetCombatCard.ToCardModel(),card) &&
                card.Id.ToString()==planned.Card!.Model && card.TargetType==MegaCrit.Sts2.Core.Entities.Cards.TargetType.Self,"wrong-card-instance");
        }
        else
        {
            Contract.Require(planned.Type==Operation.EndTurn && action.OwnerId==player.NetId && !CombatManager.Instance.IsPlayerReadyToEndTurn(player),"wrong-end-action");
            // Exact private scheduled turn is checked against production before the Action is allowed to count.
            var scheduled=(int)HarmonyLib.AccessTools.Field(typeof(EndPlayerTurnAction),"_turnNumber").GetValue(action)!;
            Contract.Require(scheduled==turn,"stale-end-turn");
        }
        trajectory.Requested(planned,turn,Probe.Dispatching);
        Contract.Require(flow.ActionStarted(),"action-start-deadline");
        var pending=new Pending(action,planned,turn,card); _pending=pending;
        action.BeforeExecuted+=a=>Probe.Safe(()=> { Contract.Require(ReferenceEquals(a,pending.Action) && !pending.Executed,"duplicate-execute"); pending.Executed=true; });
        action.BeforeCancelled+=_=>Probe.Safe(()=> { pending.Cancelled=true; Probe.Fail("action-cancelled-not-accepted"); });
        action.BeforePausedForPlayerChoice+=_=>Probe.Safe(()=> { pending.Choice=true; Probe.Fail("unexpected-action-choice"); });
        action.AfterFinished+=a=>Probe.Safe(()=>Finished(pending,a));
    }
    internal static void CardEffect(CardModel card,Creature? target,bool auto,Task effect)
    {
        var pending=_pending;
        if(pending is null || Probe.CurrentFlow?.Stage!=Stage.Action)return;
        Contract.Require(pending.Planned.Type==Operation.CardPlay && ReferenceEquals(card,pending.Card) && target is null && !auto &&
            ReferenceEquals(RunManager.Instance.ActionExecutor.CurrentlyRunningAction,pending.Action) && !pending.EnteredCard,"card-effect-origin");
        pending.EnteredCard=true; pending.Effect=effect;
    }
    internal static bool BeforeEnd(Player player)=>CombatManager.Instance.IsPlayerReadyToEndTurn(player);
    internal static void EndReady(Player player,bool wasReady)
    {
        var pending=_pending;
        if(pending is null || Probe.CurrentFlow?.Stage!=Stage.Action)return;
        Contract.Require(pending.Planned.Type==Operation.EndTurn && !wasReady && CombatManager.Instance.IsPlayerReadyToEndTurn(player) &&
            ReferenceEquals(RunManager.Instance.ActionExecutor.CurrentlyRunningAction,pending.Action) &&
            ReferenceEquals(RunManager.Instance.DebugOnlyGetState()!.Players[0],player) && !pending.EndAccepted,"end-not-accepted-transition");
        pending.EndAccepted=true;
    }
    private static void Finished(Pending p,GameAction action)
    {
        if(p.Finished)return; p.Finished=true;
        Contract.Require(ReferenceEquals(p.Action,action) && p.Executed && !p.Cancelled && !p.Choice && action.State==GameActionState.Finished && action.Exception is null,"action-completion-failed");
        Contract.Require(p.Planned.Type==Operation.CardPlay?p.EnteredCard && p.Effect?.IsCompletedSuccessfully==true:p.EndAccepted,"no-accepted-effect");
        Probe.ActionSucceeded(p.Planned,p.Turn);
    }
}
