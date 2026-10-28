using System.Numerics;
using System.Text.Json.Serialization;

namespace Det001.Core;

public sealed record StreamSample([property:JsonRequired]string Owner,[property:JsonRequired]string Seed,[property:JsonRequired]RngEntry Entry,[property:JsonRequired]string Digest)
{
    public static StreamSample From(string seed,RngEntry entry)=>new("Run",seed,entry,Projection.Rng(seed,[entry]).Digest);
    public void Validate()
    {
        Contract.Require(Owner=="Run" && !string.IsNullOrWhiteSpace(Seed) && Entry is not null && Entry.Key=="Shuffle" && Entry.Counter>=0 &&
            (Entry.State0|Entry.State1|Entry.State2|Entry.State3)!=0,"shuffle-stream-shape");
        Contract.Require(Digest==Projection.Rng(Seed,[Entry!]).Digest,"shuffle-stream-digest");
    }
}
public static class ShuffleOracle
{
    public static RngEntry Advance(RngEntry before,int steps)
    {
        Contract.Require(before is not null && before.Key=="Shuffle" && before.Counter>=0 && steps==9 && (before.State0|before.State1|before.State2|before.State3)!=0,"shuffle-oracle-input");
        var a=before!.State0;var b=before.State1;var c=before.State2;var d=before.State3;
        for(var i=0;i<steps;i++){var t=b<<17;c^=a;d^=b;b^=c;a^=d;c^=t;d=BitOperations.RotateLeft(d,45);}
        return new(before.Key,checked(before.Counter+steps),a,b,c,d);
    }
    public static void Validate(StreamSample before,StreamSample after)
    {
        before.Validate();after.Validate();Contract.Require(before.Seed==after.Seed,"shuffle-seed-changed");
        Contract.Require(after.Entry==Advance(before.Entry,9),"shuffle-full-nine-step-oracle");
    }
}
public sealed record TraceCard([property:JsonRequired]long LocalToken,[property:JsonRequired]Card Value);
public sealed record PermutationSample([property:JsonRequired]EventStamp Stamp,[property:JsonRequired]TraceCard[] Cards,[property:JsonRequired]StreamSample Rng);
public sealed record ShuffleEntry([property:JsonRequired]EventStamp Stamp,[property:JsonRequired]string Origin,[property:JsonRequired]int Round,
    [property:JsonRequired]int PlayerTurn,[property:JsonRequired]int Floor,[property:JsonRequired]string Encounter,
    [property:JsonRequired]int Draw,[property:JsonRequired]int Discard,[property:JsonRequired]int Hand);
public sealed record HookSample([property:JsonRequired]EventStamp Stamp,[property:JsonRequired]TraceCard[] Cards);
public sealed record TaskSample([property:JsonRequired]EventStamp Stamp,[property:JsonRequired]TaskStatus Shuffle,[property:JsonRequired]TaskStatus Draw);
public sealed record ShuffleTrace([property:JsonRequired]ShuffleEntry E0,[property:JsonRequired]PermutationSample E1,
    [property:JsonRequired]PermutationSample E2,[property:JsonRequired]HookSample E3,[property:JsonRequired]TaskSample E4);
public static class TraceContract
{
    public static void Entry(ShuffleEntry entry,Binding binding,string encounter)
    {
        Contract.Require(entry is not null && entry.Stamp.Sequence>0 && entry.Origin=="Draw.Hand/NaturalShuffle" && entry.Round==3 && entry.PlayerTurn==3 && entry.Floor==binding.PlayableFloor && entry.Encounter==encounter && entry.Draw==0 && entry.Discard==10 && entry.Hand==0,"shuffle-entry-identity");
    }
    public static void Input(PermutationSample input)
    {
        Contract.Require(input is not null && input.Rng is not null,"shuffle-input-missing");
        input.Rng.Validate();Contract.Require(input.Cards is {Length:10} && input.Cards.All(c=>c is not null && c.LocalToken>0 && c.Value is not null) && input.Cards.Select(c=>c.LocalToken).Distinct().Count()==10,"shuffle-input-members");
        Eligibility.NoDistinctSortTies(input.Cards.Select(c=>c.Value));
    }
    public static void Members(TraceCard[] before,TraceCard[] after)
    {
        Contract.Require(before is {Length:10} && after is {Length:10} && before.All(c=>c is not null && c.Value is not null) && after.All(c=>c is not null && c.Value is not null) && before.Select(c=>c.LocalToken).Distinct().Count()==10 && after.Select(c=>c.LocalToken).Distinct().Count()==10,"shuffle-member-count");
        var expected=before.ToDictionary(c=>c.LocalToken,c=>Actions.Fingerprint(c.Value));
        Contract.Require(after.All(c=>expected.TryGetValue(c.LocalToken,out var fingerprint) && fingerprint==Actions.Fingerprint(c.Value)),"shuffle-member-replacement");
    }
    public static void Order(EventStamp before,EventStamp after)
    { Contract.Require(before.Sequence>0 && after.Sequence>before.Sequence && after.Monotonic>=before.Monotonic,"shuffle-event-order"); }
    public static void Complete(ShuffleTrace trace,Binding binding,string encounter)
    {
        Contract.Require(trace is not null && trace.E0?.Stamp is not null && trace.E1?.Stamp is not null && trace.E2?.Stamp is not null && trace.E3?.Stamp is not null && trace.E4?.Stamp is not null && trace.E1.Rng is not null && trace.E2.Rng is not null,"shuffle-trace-missing");
        Entry(trace.E0,binding,encounter);Input(trace.E1);Order(trace.E0.Stamp,trace.E1.Stamp);Order(trace.E1.Stamp,trace.E2.Stamp);Order(trace.E2.Stamp,trace.E3.Stamp);Order(trace.E3.Stamp,trace.E4.Stamp);
        Members(trace.E1.Cards,trace.E2.Cards);Members(trace.E2.Cards,trace.E3.Cards);
        Contract.Require(trace.E2.Cards.Select(c=>c.LocalToken).SequenceEqual(trace.E3.Cards.Select(c=>c.LocalToken)),"shuffle-hook-order-interference");
        ShuffleOracle.Validate(trace.E1.Rng,trace.E2.Rng);
        Contract.Require(trace.E1.Rng.Seed==binding.Target.Seed && trace.E4.Shuffle==TaskStatus.RanToCompletion && trace.E4.Draw==TaskStatus.RanToCompletion,"shuffle-original-tasks-or-seed");
    }
    public static string Normalize(ShuffleEntry e)=>Json.Canonical(Json.Serialize(new {e.Origin,e.Round,e.PlayerTurn,e.Floor,e.Encounter,e.Draw,e.Discard,e.Hand}));
    public static string Normalize(PermutationSample e)=>Json.Canonical(Json.Serialize(new {Cards=e.Cards.Select(c=>c.Value).ToArray(),e.Rng}));
    public static string Normalize(HookSample e)=>Json.Canonical(Json.Serialize(e.Cards.Select(c=>c.Value).ToArray()));
    public static string Normalize(TaskSample e)=>Json.Canonical(Json.Serialize(new {e.Shuffle,e.Draw}));
    public static string Normalize(ShuffleTrace e)=>Json.Canonical(Json.Serialize(new {E0=Normalize(e.E0),E1=Normalize(e.E1),E2=Normalize(e.E2),E3=Normalize(e.E3),E4=Normalize(e.E4)}));
}
public sealed record DifferenceAnswer(string Boundary,string Expected,string Actual,int StablePoints,int DispatchIntents,int AcceptedCompleted,
    long EventSequence,string OriginalShuffleStatus,string OriginalDrawStatus,string InFlightMeaning);
public sealed class FirstAnswer
{
    private readonly object _sync=new();private DifferenceAnswer? _answer;
    public DifferenceAnswer? Value {get{lock(_sync)return _answer is null?null:_answer with{};}}
    public bool Freeze(DifferenceAnswer answer){lock(_sync){if(_answer is not null)return false;_answer=answer with{};return true;}}
}
