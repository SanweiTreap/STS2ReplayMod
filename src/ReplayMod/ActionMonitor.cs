using System.Diagnostics;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;

namespace ReplayMod;

internal sealed class PendingOperation(GameAction action,OperationKind kind,int turn,int? index,string? model,TargetKey? target,CardModel? card,int generation,PotionModel? potion=null,int? slot=null,double? accepted=null)
{
    internal readonly GameAction Action=action;
    internal readonly CardModel? Card=card;
    internal readonly PotionModel? Potion=potion;
    internal object? BeforeMechanics=LiveState.Format>=3?LiveState.Mechanics():null;
    internal int Generation=generation;
    internal bool Executing;
    internal int PlaybackIndex=-1;
    internal readonly TaskCompletionSource<Operation> Delivery=new();
    internal readonly long Started=Stopwatch.GetTimestamp();
    internal Operation Template=new(0,kind,turn,index,model,target,"",CardState:card is not null&&LiveState.Format>=3?LiveState.CardIdentity(card):null,
        Potion:potion is not null?LiveState.PotionIdentity(potion,slot!.Value):null,Timing:accepted is {} at?new(at,at):null);
    internal Task? ActionTask,EffectTask;
    internal readonly List<Task> DerivedTasks=[];
    internal readonly double ChoiceWaitAtStart=ChoiceTape.WaitingSeconds;
}
internal static class ActionMonitor
{
    internal static PendingOperation? Pending;
    internal static Operation? Completed;
    internal static string? Error;
    internal static Task? StartTurnTask;
    internal static int Generation;
    private static readonly Dictionary<GameAction,PendingOperation> Accepted=[];
    private static readonly Dictionary<int,PendingOperation> PlaybackAccepted=[];
    internal static bool Outstanding=>Pending is not null||Accepted.Count>0;
    internal static PendingOperation? Submitted(int index)=>PlaybackAccepted.GetValueOrDefault(index);
    internal static void Reset(){Pending=null;Completed=null;Error=null;StartTurnTask=null;Generation=0;Accepted.Clear();PlaybackAccepted.Clear();BattleEnd.Reset();}
    internal static void Queue(GameAction action)
    {
        double? accepted=Recorder.Active&&LiveState.Format>=5?RecordingClock.Now:null;
        if(Recorder.Active)Tick();
        Codec.Require(Recorder.Active||PlayerLoop.Prequeuing?LiveState.InputPhase&&!ChoiceTape.Waiting:Pending is null&&LiveState.Playable,"wait-for-completed-operation");
        PendingOperation acceptedOperation;
        if(action is PlayCardAction a)
        {
            var card=a.NetCombatCard.ToCardModelOrNull()??throw new InvalidDataException("missing-queued-card");
            var index=LiveState.Player.PlayerCombatState!.Hand.Cards.ToList().FindIndex(c=>ReferenceEquals(c,card));
            Codec.Require(index>=0&&LiveState.IsCard(card),"incomplete-unsupported-card-input-mechanism");
            acceptedOperation=new(action,OperationKind.Card,LiveState.Turn,index,card.Id.ToString(),LiveState.Target(a.Target),card,Generation,accepted:accepted);
        }
        else if(action is UsePotionAction use)
        {
            Codec.Require(LiveState.Format>=3&&ReferenceEquals(use.Player,LiveState.Player)&&use.WasEnqueuedInCombat,"unsupported-potion-context");
            var potion=use.Player.GetPotionAtSlotIndex((int)use.PotionIndex)??throw new InvalidDataException("queued-potion-slot-empty");
            Codec.Require(potion.GetType().Assembly==typeof(PotionModel).Assembly&&LiveState.SimpleTarget(potion.TargetType),"incomplete-unsupported-potion-input-mechanism");
            var state=use.Player.Creature.CombatState!;var target=use.TargetId.HasValue?state.GetCreature(use.TargetId):null;
            if(!use.TargetId.HasValue&&potion.IsValidTarget(use.Player.Creature))target=use.Player.Creature;
            Codec.Require(potion.IsValidTarget(target),"queued-potion-target-invalid");
            acceptedOperation=new(action,OperationKind.Potion,LiveState.Turn,null,null,LiveState.Target(target),null,Generation,potion,(int)use.PotionIndex,accepted);
        }
        else acceptedOperation=new(action,OperationKind.EndTurn,LiveState.Turn,null,null,null,null,Generation,accepted:accepted);
        acceptedOperation.PlaybackIndex=PlayerLoop.Active?PlayerLoop.DispatchIndex:-1;
        Codec.Require(Accepted.TryAdd(action,acceptedOperation),"duplicate-accepted-action");
        if(PlayerLoop.Active)Codec.Require(acceptedOperation.PlaybackIndex>=0&&PlaybackAccepted.TryAdd(acceptedOperation.PlaybackIndex,acceptedOperation),"duplicate-playback-submission");
        if(Recorder.Active||PlayerLoop.Prequeuing)
        {
            Runtime.Log("native-input-accepted",new{Kind=acceptedOperation.Template.Kind.ToString(),acceptedOperation.Template.Card,
                AcceptedMilliseconds=accepted,EarlierActionStillPending=Pending is not null,
                EarlierActionTaskIncomplete=Pending?.ActionTask is {IsCompleted:false},UnfinishedInputs=Accepted.Count});
        }
        if(PlayerLoop.Active&&Pending is null)Pending=acceptedOperation;
        Completed=null;
    }
    internal static void Started(GameAction action)
    {
        if(!Recorder.Active&&!PlayerLoop.Active||!Accepted.TryGetValue(action,out var operation)||operation.Executing)return;
        // The previous action and its win check have completed, but this action has not spent
        // resources or changed piles. This is the real boundary even when the executor stays busy.
        if(Pending is {} previous&&!ReferenceEquals(previous,operation))
        {
            Codec.Require(previous.Template.Kind!=OperationKind.EndTurn&&LiveState.InputPhase&&!BattleEnd.Ending&&
                Success(previous.ActionTask)&&Success(previous.EffectTask)&&previous.DerivedTasks.All(Success),"queued-action-before-prior-semantic-boundary");
            Complete(previous,false,"before-next-action");
        }
        Pending=operation;operation.Executing=true;operation.Generation=Generation;
        operation.BeforeMechanics=LiveState.Format>=3?LiveState.Mechanics():null;
        if(operation.Card is {} card)
        {
            var index=LiveState.Player.PlayerCombatState!.Hand.Cards.ToList().FindIndex(c=>ReferenceEquals(c,card));
            Codec.Require(index>=0,"queued-card-left-hand-before-execution");
            operation.Template=operation.Template with{Turn=LiveState.Turn,HandIndex=index,CardState=LiveState.CardIdentity(card)};
        }
        if(PlayerLoop.Active)PlayerLoop.VerifySubmittedStart(operation);
        Runtime.Log("native-input-executing",new{Kind=operation.Template.Kind.ToString(),operation.Template.Card,
            operation.Template.HandIndex,AcceptedMilliseconds=operation.Template.Timing?.AcceptedMilliseconds,UnfinishedInputs=Accepted.Count});
    }
    internal static void ActionTask(GameAction action,Task task){if(ReferenceEquals(action,Pending?.Action))Pending.ActionTask=task;}
    internal static void CardTask(CardModel card,bool auto,Task task)
    {
        if(Pending is {} p)
        {
            if(!auto&&p.Template.Kind==OperationKind.Card&&ReferenceEquals(card,p.Card))p.EffectTask=task;
            else {Codec.Require(auto&&LiveState.Format>=4,"unexpected-card-effect");p.DerivedTasks.Add(task);}
        }
    }
    internal static void PotionTask(PotionModel potion,Task task)
    {
        Codec.Require(Pending is {Template.Kind:OperationKind.Potion} p&&ReferenceEquals(p.Potion,potion),"incomplete-unexpected-potion-effect");
        Pending!.EffectTask=task;
    }
    private static bool Success(Task? task)
    {
        if(task is null)return false;
        Codec.Require(!task.IsCanceled&&!task.IsFaulted,"original-operation-task-fault-or-cancel");return task.IsCompletedSuccessfully;
    }
    internal static void Tick()
    {
        if(PlayerLoop.Active&&PlayerLoop.ChoiceAbort)return;
        if(Pending is not {} p||Error is not null)return;
        Codec.Require(Stopwatch.GetElapsedTime(p.Started).TotalSeconds-(ChoiceTape.WaitingSeconds-p.ChoiceWaitAtStart)<60,"operation-did-not-reach-stable-boundary");
        if(!Success(p.ActionTask))return;
        if(p.Template.Kind is OperationKind.Card or OperationKind.Potion){if(!Success(p.EffectTask))return;}
        if(p.DerivedTasks.Any(t=>!Success(t)))return;
        var victory=BattleEnd.Ready;
        if(!victory)
        {
            if(BattleEnd.Ending)return;
            if(p.Template.Kind==OperationKind.EndTurn&&(Generation<=p.Generation||!Success(StartTurnTask)||LiveState.Turn!=p.Template.Turn+1))return;
            if(!LiveState.Playable)return;
        }
        Complete(p,victory,"idle-or-turn-boundary");
    }
    private static void Complete(PendingOperation p,bool victory,string boundary)
    {
        ChoiceTape.AssertBoundary(PlayerLoop.Active?PlayerLoop.Cursor!.Position+1:Recorder.ChoiceBoundary);
        var completion=Recorder.Active&&p.Template.Timing is {} time?time with{CompletedMilliseconds=RecordingClock.Now}:null;
        Completed=p.Template with{After=victory?BattleEnd.Projection!:LiveState.Capture(),Victory=victory,Timing=completion};Pending=null;Accepted.Remove(p.Action);
        if(PlayerLoop.Active&&boundary=="before-next-action")DecisionRoute.QueuedBoundary(p.PlaybackIndex+1);
        Runtime.Log("native-input-completed",new{Kind=p.Template.Kind.ToString(),p.Template.Card,Boundary=boundary,Timing=completion,RemainingAccepted=Accepted.Count});
        if(LiveState.Format>=3)Runtime.Log("operation-mechanics",new{Mode=PlayerLoop.Active?"playback":"recording",Kind=p.Template.Kind.ToString(),p.Template.Turn,
            p.Template.Card,p.Template.CardState,p.Template.Potion,p.Template.Target,Victory=victory,Before=p.BeforeMechanics,After=victory?null:LiveState.Mechanics()});
        if(Recorder.Active)Recorder.Append(Completed);
        p.Delivery.TrySetResult(Completed);
    }
}
