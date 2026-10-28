namespace Det001.Core;

public enum ActionSite { None, Queue, BeforeExecuted, CardEffect, EndReady, AfterFinished, Cancel, Choice }
public enum ActionReject
{
    None, QueueNotReady, StateMissing, Environment, TurnMissing, TrajectoryMissing, PlanOrRequest,
    PreState, CardLocator, CardBinding, EndBinding, StaleEndTurn, ActionDeadline, DuplicateExecution,
    CardEffectOrigin, EndReadyTransition, ActionCompletion, MissingAcceptedEffect, UnexpectedContract, UnexpectedObserver
}
// Values mean LAST OBSERVED on the game thread. false means no positive observation,
// and a null TaskStatus/exception flag is NoSample, never a current-task verdict.
public sealed record ActionDiagnostics(int? Sequence,Operation? Type,Step? Before,int? Round,int? PlayerTurn,
    ActionSite Site,bool Requested,bool Executed,bool CardEffectSeen,TaskStatus? EffectTaskStatus,
    bool EndReadySeen,bool Finished,bool Cancelled,bool Choice,bool? ProductionExceptionObserved,
    ActionSite? RejectionSite,ActionReject Rejection,int ObservationIndex=0);

// The same small adapter is used by ActionBridge and persisted offline regressions.
// No Task, GameAction, player/card reference, model value or exception text can enter the DTO.
public sealed class ActionObservation(Flow flow,Action<string> terminal)
{
    private sealed class ControlledRejection(ActionReject code):Exception {internal ActionReject Code {get;}=code;}
    public static void Require(bool condition,string literalCode)
    {if(!condition)throw new ControlledRejection(Code(literalCode));}
    // Only fixed pure Core validators are passed here, never live/game getters.
    public static T Core<T>(Func<T> check,ActionReject code)
    {try{return check();}catch(InvalidDataException){throw new ControlledRejection(code);}}
    public static void Core(Action check,ActionReject code)
    {try{check();}catch(InvalidDataException){throw new ControlledRejection(code);}}
    private ActionDiagnostics Snapshot=>flow.ActionDiagnostics??new(null,null,null,null,null,ActionSite.None,false,false,false,null,false,false,false,false,null,null,ActionReject.None);
    public bool Guard(ActionSite site,Action body)
    {
        if(flow.Stage is Stage.Terminal or Stage.Complete)return false;
        try { body(); return flow.Stage!=Stage.Terminal; }
        catch(ControlledRejection rejection) { Reject(site,rejection.Code); }
        catch { Reject(site,ActionReject.UnexpectedObserver); }
        return false;
    }
    public bool Requested(AcceptedAction action)=>flow.ObserveAction(new(action.Sequence,action.Type,action.Before,action.Round,action.PlayerTurn,
        ActionSite.Queue,true,false,false,null,false,false,false,false,null,null,ActionReject.None));
    public bool Binding(AcceptedAction action)=>flow.ObserveAction(new(action.Sequence,action.Type,action.Before,action.Round,action.PlayerTurn,
        ActionSite.Queue,false,false,false,null,false,false,false,false,null,null,ActionReject.None));
    public bool Executed()=>flow.ObserveAction(Snapshot with{Site=ActionSite.BeforeExecuted,Executed=true});
    public bool CardEffect(TaskStatus status)=>flow.ObserveAction(Snapshot with{Site=ActionSite.CardEffect,CardEffectSeen=true,EffectTaskStatus=status});
    public bool EndReady()=>flow.ObserveAction(Snapshot with{Site=ActionSite.EndReady,EndReadySeen=true});
    public bool Finished(TaskStatus? status,bool exceptionObserved)=>flow.ObserveAction(Snapshot with{Site=ActionSite.AfterFinished,Finished=true,EffectTaskStatus=status,ProductionExceptionObserved=exceptionObserved});
    public bool Cancelled()=>flow.ObserveAction(Snapshot with{Site=ActionSite.Cancel,Cancelled=true});
    public bool Choice()=>flow.ObserveAction(Snapshot with{Site=ActionSite.Choice,Choice=true});
    private void Reject(ActionSite site,ActionReject code)
    {
        if(!Enum.IsDefined(site))site=ActionSite.None;
        flow.ObserveAction(Snapshot with{Site=site,RejectionSite=site,Rejection=code});
        // Finite site/code only; no raw exception message or type is exported.
        try { terminal("action-rejected-"+site+"-"+code); } catch { }
    }
    private static ActionReject Code(string text)=>text switch
    {
        "extra-action-or-not-ready"=>ActionReject.QueueNotReady,
        "action-state"=>ActionReject.StateMissing,
        "action-environment"=>ActionReject.Environment,
        "action-turn"=>ActionReject.TurnMissing,
        "trajectory-missing"=>ActionReject.TrajectoryMissing,
        "action-not-ready" or "B-recapture-or-unexpected-input" or "B-without-dispatch" or "requested-operation-mismatch"=>ActionReject.PlanOrRequest,
        "action-phase-or-state"=>ActionReject.PreState,
        "card-target-or-model" or "hand-order-or-index-changed" or "card-fingerprint-or-energy"=>ActionReject.CardLocator,
        "wrong-card-action" or "wrong-card-instance"=>ActionReject.CardBinding,
        "wrong-end-action"=>ActionReject.EndBinding,
        "stale-end-turn"=>ActionReject.StaleEndTurn,
        "action-start-deadline"=>ActionReject.ActionDeadline,
        "duplicate-execute"=>ActionReject.DuplicateExecution,
        "card-effect-origin"=>ActionReject.CardEffectOrigin,
        "end-not-accepted-transition"=>ActionReject.EndReadyTransition,
        "action-completion-failed"=>ActionReject.ActionCompletion,
        "no-accepted-effect"=>ActionReject.MissingAcceptedEffect,
        _=>ActionReject.UnexpectedContract
    };
}
