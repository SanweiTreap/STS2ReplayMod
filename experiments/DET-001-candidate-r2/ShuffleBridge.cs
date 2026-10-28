using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using Det001.Core;
using Player=MegaCrit.Sts2.Core.Entities.Players.Player;
using Json=Det001.Core.Json;

namespace Det001;

internal static class ShuffleBridge
{
    internal sealed class Scope(Player player,RunState state,string encounter,Rng owner)
    {
        internal readonly Player Player=player;internal readonly RunState State=state;internal readonly string Encounter=encounter;internal readonly Rng Owner=owner;
        internal readonly OriginalTaskScope Tasks=new();
        internal readonly Dictionary<CardModel,long> Tokens=new(ReferenceEqualityComparer.Instance);
        internal ShuffleEntry? E0;internal TraceCard[]? EntryCards;internal PermutationSample? E1,E2;internal HookSample? E3;
        internal List<CardModel>? List;internal bool ShuffleBound,DrawBound,InsidePermutation;
    }
    private static readonly ScopeSlot<Scope> Slot=new();
    private static Scope? _scope {get=>Slot.Current;set{if(value is null)Slot.Clear();else Slot.Bind(value);}}
    private static long _tokenCounter;
    internal static ShuffleTrace? Completed {get;private set;}
    internal static void Reset(){Contract.Require(_scope is null,"shuffle-scope-leak");Completed=null;}
    private static EventStamp Stamp(string name)=>Probe.CurrentFlow?.BoundaryNote(name)??throw new InvalidDataException("shuffle-evidence-closed");
    private static T Frozen<T>(T value)=>Json.Read<T>(Json.Serialize(value));
    private static bool Live=>Probe.CurrentFlow?.Stage is Stage.CaptureA or Stage.CaptureB && Probe.CurrentTrajectory?.Point==Step.Round3;
    internal static Scope? DrawPrefix(Player player,decimal count,bool fromHandDraw)
    {
        if(!Live)return null;
        var state=RunManager.Instance.DebugOnlyGetState();Contract.Require(Probe.Identity() && state is not null && state.Players.Count==1 && ReferenceEquals(state.Players[0],player),"shuffle-draw-player-scope");
        Contract.Require(_scope is null && Completed is null && count==5 && fromHandDraw && player.PlayerCombatState?.TurnNumber==3 && CombatManager.Instance.DebugOnlyGetState()?.RoundNumber==3,"shuffle-draw-scope-or-shape");
        var encounter=Probe.InitialEncounter;
        _scope=new(player,state!,encounter,state!.Rng.Shuffle);return _scope;
    }
    internal static void DrawPostfix(Scope? scope,Task original)
    { if(scope is null || !ReferenceEquals(scope,_scope))return;scope.Tasks.BindDraw(original);scope.DrawBound=true; }
    internal static Scope? ShufflePrefix(Player player)
    {
        var scope=_scope;if(scope is null || !Live || !ReferenceEquals(scope.Player,player))return null;
        Contract.Require(scope.E0 is null && !scope.ShuffleBound,"shuffle-nested-natural-call");
        var pcs=player.PlayerCombatState??throw new InvalidDataException("shuffle-pcs");
        scope.E0=new(Stamp("shuffle-E0"),"Draw.Hand/NaturalShuffle",CombatManager.Instance.DebugOnlyGetState()!.RoundNumber,pcs.TurnNumber,scope.State.TotalFloor,scope.Encounter,pcs.DrawPile.Cards.Count,pcs.DiscardPile.Cards.Count,pcs.Hand.Cards.Count);
        TraceContract.Entry(scope.E0,Probe.Binding,scope.Encounter);
        Contract.Require(CombatManager.Instance.DebugForcedTopCardOnNextShuffle is null && pcs.PlayPile.Cards.Count==0,"shuffle-debug-or-play-pile");
        scope.EntryCards=Cards(scope,pcs.DiscardPile.Cards);Probe.ObserveBoundary("E0",TraceContract.Normalize(scope.E0),scope.E0.Stamp,scope.Tasks.LastOnGameThread(),scope.E0);
        return scope;
    }
    internal static void ShufflePostfix(Scope? scope,Task original)
    { if(scope is null || !ReferenceEquals(scope,_scope))return;scope.Tasks.BindShuffle(original);scope.ShuffleBound=true; }
    internal static Scope? PermutationPrefix(object list,object rng)
    {
        var scope=_scope;
        // Generic reference-type code can be shared. Check actual list type and
        // reference-equal target owner before any card/live-state read.
        if(scope is null || !Live || !ReferenceScope.Matches<CardModel>(list,rng,scope.Owner))return null;
        var cards=(List<CardModel>)list;
        Contract.Require(scope.E0 is not null && scope.E1 is null && !scope.InsidePermutation,"shuffle-permutation-nesting");
        var actual=Cards(scope,cards);
        Contract.Require(scope.EntryCards is not null && Json.Serialize(actual.Select(c=>c.Value).ToArray())==Json.Serialize(scope.EntryCards.Select(c=>c.Value).ToArray()),"shuffle-discard-input-order");
        scope.List=cards;scope.InsidePermutation=true;
        scope.E1=new(Stamp("shuffle-E1"),actual,Rng(scope));TraceContract.Input(scope.E1);TraceContract.Order(scope.E0!.Stamp,scope.E1.Stamp);
        Probe.ObserveBoundary("E1",TraceContract.Normalize(scope.E1),scope.E1.Stamp,scope.Tasks.LastOnGameThread(),scope.E1);return scope;
    }
    internal static void PermutationPostfix(Scope? scope,object list)
    {
        if(scope is null || !ReferenceEquals(scope,_scope) || !Live)return;
        Contract.Require(scope.InsidePermutation && ReferenceEquals(scope.List,list) && scope.E1 is not null && scope.E2 is null,"shuffle-permutation-return");
        scope.E2=new(Stamp("shuffle-E2"),Cards(scope,(List<CardModel>)list),Rng(scope));scope.InsidePermutation=false;
        TraceContract.Order(scope.E1!.Stamp,scope.E2.Stamp);TraceContract.Members(scope.E1.Cards,scope.E2.Cards);
        Probe.ObserveBoundary("E2",TraceContract.Normalize(scope.E2),scope.E2.Stamp,scope.Tasks.LastOnGameThread(),scope.E2);
        if(Probe.CurrentFlow?.Stage is not Stage.Terminal)ShuffleOracle.Validate(scope.E1.Rng,scope.E2.Rng);
    }
    internal static void AfterHook(Player player,List<CardModel> cards,bool initial)
    {
        var scope=_scope;if(scope is null || !Live || initial || !ReferenceEquals(scope.Player,player) || !ReferenceEquals(scope.List,cards))return;
        Contract.Require(scope.E2 is not null && scope.E3 is null && !scope.InsidePermutation && CombatManager.Instance.DebugForcedTopCardOnNextShuffle is null,"shuffle-hook-boundary");
        scope.E3=new(Stamp("shuffle-E3"),Cards(scope,cards));TraceContract.Order(scope.E2!.Stamp,scope.E3.Stamp);TraceContract.Members(scope.E2.Cards,scope.E3.Cards);
        Contract.Require(scope.E2.Cards.Select(c=>c.LocalToken).SequenceEqual(scope.E3.Cards.Select(c=>c.LocalToken)),"shuffle-hook-order-interference");
        Probe.ObserveBoundary("E3",TraceContract.Normalize(scope.E3),scope.E3.Stamp,scope.Tasks.LastOnGameThread(),scope.E3);
    }
    internal static void SynchronousException(Scope? scope,Exception? exception)
    {
        if(exception is null || scope is null || !ReferenceEquals(scope,_scope))return;
        Probe.Fail("shuffle-original-synchronous-fault");scope.Tasks.Close();_scope=null;
    }
    internal static void Tick()
    {
        var scope=_scope;if(scope is null)return;
        if(Probe.CurrentFlow?.Stage is Stage.Terminal or Stage.Complete){scope.Tasks.Close();_scope=null;return;}
        if(!scope.Tasks.FinishedSuccessfully())return;
        Contract.Require(scope.DrawBound && scope.ShuffleBound && scope.E0 is not null && scope.E1 is not null && scope.E2 is not null && scope.E3 is not null && !scope.InsidePermutation,"shuffle-incomplete-observation");
        var e4=new TaskSample(Stamp("shuffle-E4"),TaskStatus.RanToCompletion,TaskStatus.RanToCompletion);
        var trace=new ShuffleTrace(scope.E0!,scope.E1!,scope.E2!,scope.E3!,e4);TraceContract.Complete(trace,Probe.Binding,scope.Encounter);
        Probe.ObserveBoundary("E4",TraceContract.Normalize(e4),e4.Stamp,scope.Tasks.LastOnGameThread(),e4);
        if(Probe.CurrentFlow?.Stage is not Stage.Terminal){Completed=Frozen(trace);Probe.CommitShuffle(Completed);}
        scope.Tasks.Close();_scope=null;
    }
    private static StreamSample Rng(Scope scope)=>StreamSample.From(scope.State.Rng.StringSeed,LiveRead.Rng("Shuffle",scope.Owner.ToSerializable()));
    private static TraceCard[] Cards(Scope scope,IEnumerable<CardModel> cards)=>cards.Select(c=>
    {
        if(!scope.Tokens.TryGetValue(c,out var token)){token=checked(++_tokenCounter);scope.Tokens.Add(c,token);}
        return new TraceCard(token,Frozen(LiveRead.Card(c)));
    }).ToArray();
}
