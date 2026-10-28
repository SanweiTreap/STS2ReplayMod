using System.Text.Json;
using System.Text.Json.Serialization;

namespace Det001.Core;

public sealed class PreparationException:Exception
{
    public string Code {get;}
    internal PreparationException(string code){Code=code;}
}

public sealed record DeckCardFact([property:JsonRequired]string Model,[property:JsonRequired]int Upgrade,[property:JsonRequired]bool Upgraded,
    [property:JsonRequired]string Persistent,[property:JsonRequired]bool EnchantmentAbsent,[property:JsonRequired]bool AfflictionAbsent,
    [property:JsonRequired]int Replays,[property:JsonRequired]bool ExhaustNext,[property:JsonRequired]string[] Keywords);
public sealed record ArmEligibility([property:JsonRequired]bool ShuffleFtueSeen,[property:JsonRequired]string[] Relics,
    [property:JsonRequired]string[] RunModifiers,[property:JsonRequired]bool DebugTopAbsent,[property:JsonRequired]DeckCardFact[] Deck,
    [property:JsonRequired]int Gold,[property:JsonRequired]NeowState Neow);
public sealed record NeowState([property:JsonRequired]string Model,[property:JsonRequired]int? Counter,
    [property:JsonRequired]bool? Flag,[property:JsonRequired]string Persistent);
public sealed record PointEligibility([property:JsonRequired]ArmEligibility Arm,[property:JsonRequired]bool PlayPileEmpty,[property:JsonRequired]int PlayerTurn);

public static class Eligibility
{
    private static void Require(bool condition,string code){if(!condition)throw new PreparationException(code);}
    public const string NeowRelic="RELIC.GOLDEN_PEARL";
    public static bool AllowedNeow(string model)=>model is NeowRelic or "RELIC.SILKEN_TRESS" or "RELIC.BOOMING_CONCH" or "RELIC.LAVA_ROCK" or "RELIC.WINGED_BOOTS" or "RELIC.FISHING_ROD";
    public static void Arm(ArmEligibility e)
    {
        Require(e is not null && e.ShuffleFtueSeen && e.DebugTopAbsent && e.Gold>=0,"eligibility-ftue-debug-gold");
        Require(e!.Relics is {Length:2} && e.Relics.Count(x=>x=="RELIC.BURNING_BLOOD")==1 && e.Relics.Count(AllowedNeow)==1,"eligibility-relic-whitelist");
        Require(e.Neow is not null && AllowedNeow(e.Neow.Model) && e.Relics.Contains(e.Neow.Model,StringComparer.Ordinal),"eligibility-neow-binding");
        var n=e.Neow!;Json.Canonical(n.Persistent);
        Require(n.Model switch {
            "RELIC.SILKEN_TRESS" or "RELIC.LAVA_ROCK"=>n.Counter is null && n.Flag==false,
            "RELIC.WINGED_BOOTS" or "RELIC.FISHING_ROD"=>n.Counter==0 && n.Flag is null,
            _=>n.Counter is null && n.Flag is null
        },"eligibility-neow-used-state");
        using(var doc=JsonDocument.Parse(n.Persistent))
        {
            var root=doc.RootElement;
            Require(root.ValueKind==JsonValueKind.Object && root.TryGetProperty("id",out var id) && id.ValueKind==JsonValueKind.String && id.GetString()==n.Model,"eligibility-neow-persistent-id");
            string? property=n.Model switch {"RELIC.SILKEN_TRESS"=>"IsUsed","RELIC.LAVA_ROCK"=>"HasTriggered","RELIC.WINGED_BOOTS"=>"TimesUsed","RELIC.FISHING_ROD"=>"CombatsSeen",_=>null};
            var present=root.TryGetProperty("props",out var props) && props.ValueKind!=JsonValueKind.Null;
            if(property is null)Require(!present,"eligibility-neow-unexpected-props");
            else
            {
                var key=n.Counter.HasValue?"ints":"bools";
                Require(present && props.ValueKind==JsonValueKind.Object && props.EnumerateObject().Count()==1 && props.TryGetProperty(key,out _),"eligibility-neow-persistent-state");
                var values=props.GetProperty(key);
                Require(values.ValueKind==JsonValueKind.Array && values.GetArrayLength()==1,"eligibility-neow-persistent-state");
                var value=values[0];
                Require(value.ValueKind==JsonValueKind.Object && value.TryGetProperty("name",out var name) && name.ValueKind==JsonValueKind.String && name.GetString()==property && value.TryGetProperty("value",out _),"eligibility-neow-persistent-state");
                var actual=value.GetProperty("value");
                Require(n.Counter.HasValue?actual.ValueKind==JsonValueKind.Number && actual.TryGetInt32(out var count) && count==n.Counter:actual.ValueKind==JsonValueKind.False && n.Flag==false,"eligibility-neow-persistent-state");
            }
        }
        Require(e.RunModifiers is {Length:0} && e.Deck is {Length:10},"eligibility-modifiers-deck");
        foreach(var c in e.Deck!)
        {
            Require(c is not null && c.Upgrade==0 && !c.Upgraded && c.EnchantmentAbsent && c.AfflictionAbsent && c.Replays==0 && !c.ExhaustNext && c.Keywords is {Length:0},"eligibility-deck-properties");
            Json.Canonical(c!.Persistent);Contract.Text(c.Model,"deck-model");
        }
        Counts(e.Deck.Select(c=>c.Model));
    }
    public static void Point(PointEligibility e,ArmEligibility expected,Semantic semantic,int point)
    {
        Require(e is not null && e.PlayPileEmpty && e.PlayerTurn==Stages.RoundAt(point),"eligibility-live-turn-play-pile");
        Arm(e!.Arm);Arm(expected);
        Require(Json.Canonical(Json.Serialize(e.Arm))==Json.Canonical(Json.Serialize(expected)),"eligibility-changed");
        var cards=semantic.Player.Hand.Concat(semantic.Player.Draw).Concat(semantic.Player.Discard).Concat(semantic.Player.Exhaust).ToArray();
        Require(cards.Length==10,"eligibility-combat-card-count");Counts(cards.Select(c=>c.Model));
        Require(semantic.Player.Hand.Length==new[]{5,4,5,4,5}[point] && semantic.Player.Draw.Length==new[]{5,5,0,0,5}[point] && semantic.Player.Discard.Length==new[]{0,1,5,6,0}[point] && semantic.Player.Exhaust.Length==0,"eligibility-finite-piles");
        if(point==0)
        {
            Require(semantic.Player.Hand.Length==5 && semantic.Player.Draw.Length==5 && semantic.Player.Discard.Length==0 && semantic.Player.Exhaust.Length==0 && semantic.Player.Powers.Length==0,"eligibility-initial-piles-powers");
            Require(cards.Select(c=>c.Persistent).Order(StringComparer.Ordinal).SequenceEqual(expected.Deck.Select(c=>c.Persistent).Order(StringComparer.Ordinal)),"eligibility-deck-combat-persistent");
            NoDistinctSortTies(cards);
            Require(semantic.Player.Hand.Any(Actions.Simple) && semantic.Player.Draw.Any(Actions.Simple),"eligibility-defend-distribution");
            Require(semantic.Position.Encounter is "ENCOUNTER.SHRINKER_BEETLE_WEAK" or "ENCOUNTER.CORPSE_SLUGS_WEAK","eligibility-weak-encounter");
        }
        if(point==2)Require(semantic.Player.Hand.Length==5 && semantic.Player.Draw.Length==0 && semantic.Player.Discard.Length==5 && semantic.Player.Hand.Any(Actions.Simple),"eligibility-round2-piles-defend");
        if(point==4)Require(semantic.Player.Hand.Length==5 && semantic.Player.Draw.Length==5 && semantic.Player.Discard.Length==0,"eligibility-round3-piles");
    }
    public static void NoDistinctSortTies(IEnumerable<Card> cards)
    { Require(!cards.GroupBy(c=>(c.Model,c.Upgrade)).Any(g=>g.Select(Actions.Fingerprint).Distinct(StringComparer.Ordinal).Count()>1),"eligibility-distinct-sort-tie"); }
    private static void Counts(IEnumerable<string> models)
    {
        var a=models.ToArray();Require(a.Length==10 && a.Count(x=>x=="CARD.STRIKE_IRONCLAD")==5 && a.Count(x=>x==Actions.Defend)==4 && a.Count(x=>x=="CARD.BASH")==1,"eligibility-plain-ten-card-starter");
    }
}
