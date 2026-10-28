using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Det001.Core;
using Json=Det001.Core.Json;

namespace Det001;

internal static class EligibilityRead
{
    internal static ArmEligibility Arm(RunState state)
    {
        Contract.Require(Probe.Identity() && state.Players.Count==1 && state.Modifiers is not null,"eligibility-environment-missing");
        var player=state.Players[0];Contract.Require(player.Relics is not null && player.Deck?.Cards is not null && SaveManager.Instance is not null && CombatManager.Instance is not null,"eligibility-source-missing");
        var before=player.Deck!.Cards.Select(c=>Json.Canonical(System.Text.Json.JsonSerializer.Serialize(c.ToSerializable(),JsonSerializationUtility.Options))).ToArray();
        var facts=new ArmEligibility(SaveManager.Instance!.SeenFtue("shuffle_ftue"),player.Relics!.Select(r=>r.Id.ToString()).Order(StringComparer.Ordinal).ToArray(),
            state.Modifiers!.Select(m=>m.Id.ToString()).Order(StringComparer.Ordinal).ToArray(),CombatManager.Instance!.DebugForcedTopCardOnNextShuffle is null,
            player.Deck.Cards.Select((c,i)=>new DeckCardFact(c.Id.ToString(),c.CurrentUpgradeLevel,c.IsUpgraded,before[i],c.Enchantment is null,c.Affliction is null,c.BaseReplayCount,c.ExhaustOnNextPlay,c.GetKeywordsWithSources(MegaCrit.Sts2.Core.Entities.Cards.KeywordSources.All).Select(k=>k.ToString()).Order(StringComparer.Ordinal).ToArray())).ToArray(),player.Gold);
        var after=player.Deck.Cards.Select(c=>Json.Canonical(System.Text.Json.JsonSerializer.Serialize(c.ToSerializable(),JsonSerializationUtility.Options))).ToArray();
        Contract.Require(before.SequenceEqual(after),"eligibility-deck-oracle-mutated");Eligibility.Arm(facts);return facts;
    }
    internal static PointEligibility Point(RunState state)
    {
        var facts=Arm(state);var pcs=state.Players[0].PlayerCombatState;Contract.Require(pcs is not null && pcs.PlayPile?.Cards is not null,"eligibility-combat-source-missing");
        return new(facts,pcs!.PlayPile.Cards.Count==0,pcs.TurnNumber);
    }
}
