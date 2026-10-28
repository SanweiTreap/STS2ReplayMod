using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace Spike001A3;

internal static class SnapshotCapture
{
    private static readonly string[] SemanticMembers =
    [
        "UpgradeCount", "UpgradeLevel", "IsUpgraded", "CanonicalEnergyCost", "EnergyCost",
        "Cost", "CostForTurn", "GeneratedInCombat", "IsGenerated", "TemporaryCostReduction"
    ];

    internal static CombatSnapshot Capture(RawReadinessSample readiness, long quietStartTick, ulong quietStartFrame)
    {
        var state = RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("run state unavailable");
        var player = state.Players.Single();
        var pcs = player.PlayerCombatState ?? throw new InvalidOperationException("player combat unavailable");
        var combat = CombatManager.Instance ?? throw new InvalidOperationException("combat manager unavailable");
        var combatState = combat.DebugOnlyGetState() ?? throw new InvalidOperationException("combat state unavailable");
        var coord = state.CurrentMapCoord; var room = state.CurrentRoom;
        var serializableRng = state.Rng.ToSerializable();
        var entries = serializableRng.Rngs.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)
            .Select(x => new RngEntrySnapshot(x.Key.ToString(), x.Value.counter, x.Value.state0,
                x.Value.state1, x.Value.state2, x.Value.state3)).ToList();
        var canonical = string.Join("\n", entries.Select(x =>
            $"{x.StableName}|{x.Counter}|{x.State0:X16}|{x.State1:X16}|{x.State2:X16}|{x.State3:X16}"));
        return new(
            new(state.CurrentActIndex, coord?.row, coord?.col, room?.Id, room?.ModelId?.ToString() ?? "",
                room?.RoomType.ToString() ?? "", room?.ModelId?.ToString() ?? "", state.TotalFloor, combatState.RoundNumber),
            new(player.Character.Id.ToString(), player.Creature.CurrentHp, ReadInt(player.Creature, "MaxHp"), pcs.Energy,
                Cards(pcs.Hand), Cards(pcs.DrawPile), Cards(pcs.DiscardPile), Cards(pcs.ExhaustPile)),
            combatState.Enemies.OrderBy(x => x.CombatId).Select(x => new EnemySnapshot(x.ModelId.ToString(), x.CombatId,
                x.CurrentHp, ReadInt(x, "MaxHp"), x.IsAlive, ReadBool(x, "IsDead", !x.IsAlive),
                ReadBool(x, "IsEscaping", false))).ToList(),
            new(serializableRng.Seed ?? "", JsonSupport.Sha256Text(canonical), entries), readiness,
            quietStartTick, quietStartFrame, readiness.Tick, readiness.EngineFrame,
            DateTimeOffset.UtcNow.ToString("O"), Environment.ProcessId);
    }

    private static IReadOnlyList<CardSnapshot> Cards(CardPile pile) => pile.Cards.Select(Card).ToList();
    private static CardSnapshot Card(MegaCrit.Sts2.Core.Models.CardModel card)
    {
        var type = card.GetType(); var id = type.GetProperty("Id")?.GetValue(card)?.ToString() ?? type.FullName ?? type.Name;
        var values = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (var name in SemanticMembers)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null && property.GetIndexParameters().Length == 0)
            { try { values[name] = Stable(property.GetValue(card)); } catch (Exception ex) { values[name] = "<exception:" + ex.GetType().FullName + ">"; } }
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null)
            { try { values[name] = Stable(field.GetValue(card)); } catch (Exception ex) { values[name] = "<exception:" + ex.GetType().FullName + ">"; } }
        }
        var serializable = JsonSupport.Canonicalize(JsonSerializer.Serialize(card.ToSerializable(), JsonSupport.Options));
        return new(id, serializable, values);
    }
    private static string? Stable(object? value) => value switch
    {
        null => null, string s => s, bool b => b ? "true" : "false",
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
            => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
        Enum e => e.ToString(), _ => value.ToString()
    };
    private static int ReadInt(object value, string name)
    {
        var p = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return p?.GetValue(value) is int i ? i : -1;
    }
    private static bool ReadBool(object value, string name, bool fallback)
    {
        var p = value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return p?.GetValue(value) is bool b ? b : fallback;
    }
}
