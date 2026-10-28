using System.Text.Json.Serialization;

namespace Det001.Core;

public enum Step { Initial=0, PostCard=1, Round2=2, PostRound2Card=3, Round3=4 }
public enum Operation { CardPlay=1, EndTurn=2 }
public static class Stages
{
    public static int RoundAt(int point) { Contract.Require(point is >=0 and <5,"point-range"); return point<2?1:point<4?2:3; }
    public static int ActionRound(int sequence) { Contract.Require(sequence is >=1 and <=4,"action-range");return sequence<=2?1:2; }
}
public sealed record CardLocator([property:JsonRequired]int Index,[property:JsonRequired]string Model,
    [property:JsonRequired]string HandHash,[property:JsonRequired]string Fingerprint,[property:JsonRequired]string Target);
public sealed record AcceptedAction([property:JsonRequired]int Sequence,[property:JsonRequired]Operation Type,
    [property:JsonRequired]Step Before,[property:JsonRequired]int Round,[property:JsonRequired]int PlayerTurn,
    [property:JsonRequired]CardLocator? Card,[property:JsonRequired]string BeforeHash);

public static class Actions
{
    public const string Defend="CARD.DEFEND_IRONCLAD";
    public static string HandHash(Semantic s)=>Json.Hash(Json.Canonical(Json.Serialize(s.Player.Hand)));
    public static string Fingerprint(Card c)=>Json.Hash(Json.Canonical(Json.Serialize(c)));
    public static CardLocator Select(Semantic s)
    {
        Projection.Validate(s); Contract.Require(s.Position.Round is 1 or 2,"select-round");
        // Full hand order plus exact slot disambiguates even identical Defends. Never search by model alone in B.
        var index=Array.FindIndex(s.Player.Hand,c=>c.Model==Defend && Simple(c) && c.EffectiveCost<=s.Player.Energy);
        Contract.Require(index>=0,"no-eligible-defend-stop"); var card=s.Player.Hand[index];
        return new(index,card.Model,HandHash(s),Fingerprint(card),"Self:null");
    }
    public static bool Simple(Card c)=>c.Model==Defend && c.Upgrade==0 && !c.Upgraded && !c.CostsX && !c.ExhaustNext &&
        c.Affliction is null && c.CostModifiers.Length==0 && c.Keywords.Length==0 && c.Replays==0 &&
        c.BaseCost==1 && c.CanonicalCost==1 && c.EffectiveCost==1 && c.StarCost<=0 &&
        c.Variables is [{Name:"Block",Base:5,Enchanted:5}];
    public static int Resolve(CardLocator locator,Semantic live)
    {
        Contract.Require(locator is not null && locator.Target=="Self:null" && locator.Model==Defend,"card-target-or-model");
        Contract.Require(locator!.Index>=0 && locator.Index<live.Player.Hand.Length && locator.HandHash==HandHash(live),"hand-order-or-index-changed");
        var c=live.Player.Hand[locator.Index];
        Contract.Require(Simple(c) && c.Model==locator.Model && Fingerprint(c)==locator.Fingerprint && c.EffectiveCost<=live.Player.Energy,"card-fingerprint-or-energy");
        return locator.Index;
    }
    public static void Validate(AcceptedAction a,Semantic before,int turn)
    {
        Contract.Require(a is not null && Enum.IsDefined(a.Type) && a.Round==Stages.ActionRound(a.Sequence) && a.PlayerTurn==turn && turn==a.Round &&
            (int)a.Before==a.Sequence-1 && a.BeforeHash==Json.Hash(Projection.Freeze(before)) && before.Position.Round==a.Round,"action-phase-or-state");
        if(a!.Type==Operation.CardPlay)
        { Contract.Require(a.Sequence is 1 or 3 && a.Card is not null,"card-sequence"); Resolve(a.Card!,before); }
        else Contract.Require(a.Sequence is 2 or 4 && a.Card is null,"end-sequence");
    }
}

// Detached semantics only. The live adapter must separately prove production acceptance/completion.
public sealed class Trajectory
{
    private readonly Binding _binding;
    private readonly string[]? _expected;
    private readonly AcceptedAction[]? _expectedActions;
    private readonly List<string> _states=[];
    private readonly List<AcceptedAction> _actions=[];
    private bool _inFlight;
    private AcceptedAction? _requested;
    private readonly HashSet<int> _dispatches=[];
    public bool IsB=>_expected is not null;
    public bool Done {get;private set;}
    public Step Point=>(Step)_states.Count;
    public int Round=>Stages.RoundAt(_states.Count);
    public string? Last=>_states.LastOrDefault();
    public string[] States=>_states.ToArray();
    public AcceptedAction[] Accepted=>_actions.Select(a=>Json.Read<AcceptedAction>(Json.Serialize(a))).ToArray();
    public CardLocator? Selection {get;private set;}
    public Trajectory(Binding binding,string[]? expected=null,AcceptedAction[]? actions=null)
    {
        _binding=binding;
        Contract.Require((expected is null)==(actions is null),"B-input-pair");
        if(expected is not null)
        {
            ValidateA(binding,expected,actions!);
            _expected=expected.Select(s=>Projection.Freeze(Projection.Thaw(s))).ToArray();
            _expectedActions=Json.Read<AcceptedAction[]>(Json.Serialize(actions));
        }
    }
    public string[] Capture(string snapshot)
    {
        Contract.Require(!Done && !_inFlight && _states.Count<5 && _actions.Count==_states.Count,"point-order-or-action-incomplete");
        var semantic=Projection.Thaw(snapshot); var first=_states.Count>0?Projection.Thaw(_states[0]):null;
        Contract.Playable(_binding,semantic,first?.Position.RoomModel,first?.Position.Encounter,Round);
        Contract.Require(semantic.Player.Hp>0 && semantic.Enemies.Any(e=>e.Alive && !e.Dead && !e.Escaped),"combat-ended");
        var frozen=Projection.Freeze(semantic);
        var diff=IsB?Projection.Compare(_expected![_states.Count],frozen):[];
        _states.Add(frozen);
        if(diff.Length>0 || _states.Count==5) { Done=true; return diff; }
        if(_states.Count is 1 or 3)Selection=IsB?_expectedActions![_states.Count-1].Card:Actions.Select(semantic);
        return diff;
    }
    public AcceptedAction Planned(int turn)
    {
        Contract.Require(!Done && !_inFlight && _states.Count is >=1 and <=4 && _actions.Count==_states.Count-1,"action-not-ready");
        var op=_states.Count is 1 or 3?Operation.CardPlay:Operation.EndTurn;
        var result=IsB?_expectedActions![_actions.Count]:new AcceptedAction(_actions.Count+1,op,(Step)(_states.Count-1),Stages.ActionRound(_actions.Count+1),turn,op==Operation.CardPlay?Selection:null,Json.Hash(Last!));
        Actions.Validate(result,Projection.Thaw(Last!),turn); return result;
    }
    public void Dispatch(AcceptedAction a,int turn)
    {
        Contract.Require(IsB && !_inFlight && !Done,"dispatch-phase");
        Contract.Require(Json.Serialize(a)==Json.Serialize(Planned(turn)) && _dispatches.Add(a.Sequence),"dispatch-duplicate-or-mismatch");
    }
    public void Requested(AcceptedAction a,int turn,bool dispatcher)
    {
        Contract.Require(!Done && !_inFlight && dispatcher==IsB,"B-recapture-or-unexpected-input");
        Contract.Require(!IsB || _dispatches.Contains(a.Sequence),"B-without-dispatch");
        Contract.Require(Json.Serialize(a)==Json.Serialize(Planned(turn)),"requested-operation-mismatch");
        _requested=Json.Read<AcceptedAction>(Json.Serialize(a)); _inFlight=true;
    }
    public void Succeeded(AcceptedAction a,bool executed,bool effectSucceeded,bool cancelled,bool choice,int turn)
    {
        Contract.Require(_inFlight && !Done && executed && effectSucceeded && !cancelled && !choice,"action-not-successfully-accepted");
        Contract.Require(_requested is not null && Json.Serialize(a)==Json.Serialize(_requested),"accepted-request-identity");
        Contract.Require(a.Sequence==_actions.Count+1 && a.Sequence==_states.Count,"duplicate-or-unexpected-success");
        Actions.Validate(a,Projection.Thaw(Last!),turn); _actions.Add(Json.Read<AcceptedAction>(Json.Serialize(a))); _inFlight=false; _requested=null;
    }
    public static void ValidateA(Binding binding,string[] states,AcceptedAction[] actions)
    {
        Contract.Require(states is {Length:5} && actions is {Length:4},"A-incomplete");
        var sequence=new Trajectory(binding);
        sequence.Capture(states[0]);
        for(var i=0;i<4;i++)
        { var a=actions[i]; sequence.Requested(a,a.PlayerTurn,false); sequence.Succeeded(a,true,true,false,false,a.PlayerTurn); sequence.Capture(states[i+1]); }
        Contract.Require(sequence.Done && actions.Select(x=>x.PlayerTurn).SequenceEqual(new[]{1,1,2,2}),"A-turn-or-commit");
    }
}
