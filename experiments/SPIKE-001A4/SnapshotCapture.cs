using System.Text.Json;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Spike001A4;

internal static class SnapshotCapture
{
    internal static CombatSnapshot Capture(RawReadinessSample readiness, long quietStartTick, ulong quietStartFrame)
    {
        var state = RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("run state unavailable");
        if (state.Players.Count != 1) throw new InvalidOperationException("snapshot requires exactly one player");
        var player = state.Players[0];
        var pcs = player.PlayerCombatState ?? throw new InvalidOperationException("player combat unavailable");
        var combat = CombatManager.Instance ?? throw new InvalidOperationException("combat manager unavailable");
        var combatState = combat.DebugOnlyGetState() ?? throw new InvalidOperationException("combat state unavailable");
        var coord = state.CurrentMapCoord ?? throw new InvalidOperationException("current map coordinate unavailable");
        var room = state.CurrentRoom as CombatRoom ?? throw new InvalidOperationException("current room is not combat");
        var roomModel = SemanticProjectionBuilder.RequireText(room.ModelId.ToString(), "actual room model id");
        var encounter = room.CombatState.Encounter
            ?? throw new InvalidOperationException("actual combat encounter unavailable");
        var encounterId = SemanticProjectionBuilder.RequireText(encounter.Id.ToString(), "actual encounter id");
        var roomId = room.Id ?? throw new InvalidOperationException("actual room id unavailable");
        var seedAndEntries = CaptureRng(state);
        var snapshot = new CombatSnapshot(
            new(state.CurrentActIndex, coord.row, coord.col, roomId, roomModel, room.RoomType.ToString(),
                encounterId, state.TotalFloor, combatState.RoundNumber),
            new(SemanticProjectionBuilder.RequireText(player.Character.Id.ToString(), "player character id"),
                player.Creature.CurrentHp, player.Creature.MaxHp, pcs.Energy,
                Cards(pcs.Hand), Cards(pcs.DrawPile), Cards(pcs.DiscardPile), Cards(pcs.ExhaustPile)),
            combatState.Enemies.OrderBy(x => x.CombatId).Select(x =>
            {
                var combatId = x.CombatId ?? throw new InvalidOperationException("enemy CombatId is missing");
                return new EnemySnapshot(SemanticProjectionBuilder.RequireText(x.ModelId.ToString(), "enemy model id"),
                    combatId, x.CurrentHp, x.MaxHp, x.IsAlive, x.IsDead, combatState.EscapedCreatures.Contains(x));
            }).ToList(),
            seedAndEntries, readiness, quietStartTick, quietStartFrame, readiness.Tick, readiness.EngineFrame,
            DateTimeOffset.UtcNow.ToString("O"), Environment.ProcessId);
        SemanticProjectionBuilder.ValidateRequired(SemanticProjectionBuilder.From(snapshot));
        return snapshot;
    }

    private static RngSnapshot CaptureRng(RunState state)
    {
        var serializable = state.Rng.ToSerializable();
        var seed = SemanticProjectionBuilder.RequireText(serializable.Seed, "RNG seed");
        var entries = serializable.Rngs.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)
            .Select(x => new RngEntrySnapshot(
                SemanticProjectionBuilder.RequireText(x.Key.ToString(), "RNG entry name"),
                x.Value.counter, x.Value.state0, x.Value.state1, x.Value.state2, x.Value.state3)).ToList();
        if (entries.Count == 0) throw new InvalidOperationException("RNG entry set is empty");
        var canonical = seed + "\n" + string.Join("\n", entries.Select(x =>
            $"{x.StableName}|{x.Counter}|{x.State0:X16}|{x.State1:X16}|{x.State2:X16}|{x.State3:X16}"));
        return new(seed, JsonSupport.Sha256Text(canonical), entries);
    }

    private static IReadOnlyList<CardSnapshot> Cards(CardPile pile) => pile.Cards.Select(Card).ToList();

    private static CardSnapshot Card(CardModel card)
    {
        var serializable = card.ToSerializable();
        var id = SemanticProjectionBuilder.RequireText(serializable.Id?.ToString(), "card model id");
        var canonical = JsonSupport.Canonicalize(JsonSerializer.Serialize(serializable, JsonSupport.Options));
        return new(id, canonical, card.CurrentUpgradeLevel, card.IsUpgraded,
            SemanticProjectionBuilder.RequireText(card.EnergyCost.ToString(), "card energy cost"),
            card.BaseReplayCount, card.ExhaustOnNextPlay, card.FloorAddedToDeck);
    }
}
