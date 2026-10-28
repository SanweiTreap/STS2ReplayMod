namespace Det001;

public sealed record StarterPlan(string Classification,int? FirstReshuffleRound=null,int? EndTurns=null,int? MaxOperations=null,int? StablePoints=null);
public static class StarterPreparation
{
    public static StarterPlan Plan(Spike001B.Core.Semantic s,bool basicPassives=true,bool ftueAlreadySeen=true)
    {
        if(!basicPassives)return new("FIXTURE_UNSUPPORTED_PASSIVE_INPUT");
        if(!ftueAlreadySeen)return new("FIXTURE_FTUE_NOT_READY");
        var cards=s.Player.Hand.Concat(s.Player.Draw).ToArray();
        if(s.Position.Round!=1 || s.Player.Character!="CHARACTER.IRONCLAD" || s.Player.Hand.Length!=5 || s.Player.Draw.Length!=5 || s.Player.Discard.Length!=0 || s.Player.Exhaust.Length!=0 || s.Player.Powers.Length!=0 ||
           cards.Count(c=>c.Model=="CARD.STRIKE_IRONCLAD")!=5 || cards.Count(c=>c.Model=="CARD.DEFEND_IRONCLAD")!=4 || cards.Count(c=>c.Model=="CARD.BASH")!=1 ||
           cards.Any(c=>c.Upgraded || c.Upgrade!=0 || c.Keywords.Length!=0 || c.Affliction is not null || c.CostModifiers.Length!=0 || c.ExhaustNext))return new("FIXTURE_NOT_PLAIN_TEN_CARD_STARTER");
        if(cards.GroupBy(c=>(c.Model,c.Upgrade)).Any(g=>g.Select(Spike001B.Core.Actions.Fingerprint).Distinct(StringComparer.Ordinal).Count()>1))return new("FIXTURE_DISTINCT_SORT_TIE");
        if(!s.Player.Hand.Any(Spike001B.Core.Actions.Simple) || !s.Player.Draw.Any(Spike001B.Core.Actions.Simple))return new("FIXTURE_DEFEND_DISTRIBUTION");
        var endTurns=s.Player.Draw.Length/5+1;
        return new("SYNTHETIC_PREPARATION_ELIGIBLE",s.Position.Round+endTurns,endTurns,2*endTurns,2*endTurns+1);
    }
}

// Synthetic evidence DTOs only. No game objects, effects, scheduling or runtime probe.
public sealed record ReshuffleEvidence(
    bool InitialEqual=true, bool Round2Equal=true, bool BeforeShuffleEqual=true,
    string Boundary="CardPileCmd.Shuffle/StableShuffle<CardModel>",
    bool Natural=true, bool InitialShuffle=false, string Owner="Run", string Stream="Shuffle",
    int Round=3, int PlayerTurn=3, int Floor=2, bool SameRoom=true,
    int InputCount=10, int DrawAtEntry=0, int DiscardAtEntry=10,
    int CounterBefore=9, int CounterAfter=18, bool StateChanged=true,
    long Before=100, long RandomReturned=101, long ShuffleFinished=120, long DrawStable=150, long CaptureStarted=90,
    bool OriginalShuffleTaskSucceeded=true, bool OriginalDrawTaskSucceeded=true,
    int AcceptedCompleted=4, bool AllActionsBound=true, bool NoOtherTargetRngUse=true,
    int AdjacentFrames=30, int TrueFields=20, int MaxGap=9, bool CombatAlive=true);

public static class ReshuffleContract
{
    public static string Check(ReshuffleEvidence e)
    {
        if(!e.InitialEqual || !e.Round2Equal || !e.BeforeShuffleEqual)return "SEMANTIC_DIFFERENT_STOP_NO_NEXT_DISPATCH";
        if(!e.Natural || e.InitialShuffle || e.Boundary!="CardPileCmd.Shuffle/StableShuffle<CardModel>")return "TARGET_RESHUFFLE_NOT_PROVEN";
        if(e.Owner!="Run" || e.Stream!="Shuffle" || !e.NoOtherTargetRngUse)return "RNG_ATTRIBUTION_REJECTED";
        if(e.Round!=3 || e.PlayerTurn!=3 || e.Floor!=2 || !e.SameRoom)return "PHASE_IDENTITY_REJECTED";
        if(e.InputCount!=10 || e.DrawAtEntry!=0 || e.DiscardAtEntry!=10)return "FIXTURE_NOT_ELIGIBLE";
        if(!(e.Before<e.RandomReturned && e.RandomReturned<e.ShuffleFinished && e.ShuffleFinished<e.DrawStable))return "SAMPLE_ORDER_REJECTED";
        if(e.CaptureStarted>e.Before || e.DrawStable-e.CaptureStarted>20000)return "TARGET_CAPTURE_DEADLINE";
        if(e.CounterAfter-e.CounterBefore!=e.InputCount-1 || !e.StateChanged)return "RNG_ADVANCE_NOT_PROVEN";
        if(!e.OriginalShuffleTaskSucceeded || !e.OriginalDrawTaskSucceeded || e.AcceptedCompleted!=4 || !e.AllActionsBound)return "ORIGINAL_COMPLETION_NOT_PROVEN";
        if(e.AdjacentFrames!=30 || e.TrueFields!=20 || e.MaxGap is <0 or >250 || !e.CombatAlive)return "NOT_SETTLED_OR_COMBAT_ENDED";
        return "SYNTHETIC_CONTRACT_READY_NOT_RUNTIME_PASS";
    }
    public static string NextDispatch(bool previousEqual,bool terminal,int completedOperations)
        =>terminal?"STOP_TERMINAL":!previousEqual?"STOP_DIFFERENCE":completedOperations>=4?"STOP_OPERATION_BOUND":"ALLOW_ONE_LOGGED_OPERATION";
}
