namespace Spike001A4;

internal static class SemanticProjectionBuilder
{
    internal static SemanticProjection From(CombatSnapshot snapshot)
        => new(snapshot.Position, snapshot.Player, snapshot.Enemies, snapshot.Rng);

    internal static void ValidateRequired(SemanticProjection value)
    {
        RequireText(value.Position.RoomModelId, "room model id");
        RequireText(value.Position.EncounterId, "encounter id");
        RequireText(value.Player.CharacterModelId, "character model id");
        RequireText(value.Rng.Seed, "RNG seed");
        if (value.Position.TotalFloor < 0 || value.Position.MapRow < 0 || value.Position.MapCol < 0)
            throw new InvalidDataException("position contains a negative required value");
        if (value.Player.Hp < 0 || value.Player.MaxHp < 0 || value.Player.Energy < 0)
            throw new InvalidDataException("player creature field is negative");
        if (value.Enemies.Count == 0) throw new InvalidDataException("enemy set is empty");
        if (value.Rng.Entries.Count == 0) throw new InvalidDataException("RNG entry set is empty");
        foreach (var entry in value.Rng.Entries) RequireText(entry.StableName, "RNG entry name");
        foreach (var card in AllCards(value.Player))
        {
            RequireText(card.ModelId, "card model id");
            RequireText(card.SerializableCanonicalJson, "card canonical serialization");
            RequireText(card.EnergyCost, "card energy cost");
        }
        foreach (var enemy in value.Enemies)
        {
            RequireText(enemy.ModelId, "enemy model id");
            if (enemy.MaxHp < 0 || enemy.Hp < 0) throw new InvalidDataException("enemy HP field is negative");
        }
    }

    internal static string RequireText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(field + " is missing or empty");
        return value;
    }

    private static IEnumerable<CardSnapshot> AllCards(PlayerSnapshot player)
        => player.Hand.Concat(player.Draw).Concat(player.Discard).Concat(player.Exhaust);
}
