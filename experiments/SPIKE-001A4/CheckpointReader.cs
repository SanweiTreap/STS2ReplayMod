using System.Text;
using MegaCrit.Sts2.Core.Saves;

namespace Spike001A4;

internal static class CheckpointReader
{
    internal static (CheckpointEvidence Evidence, string RawJson, byte[] RawBytes) Capture()
    {
        var stable = ProtectionScope.StableReadTwice();
        var raw = Encoding.UTF8.GetString(stable.Bytes);
        var parsed = JsonSerializationUtility.FromJson<SerializableRun>(raw);
        if (!parsed.Success || parsed.SaveData is null)
            throw new InvalidDataException("native current_run.save parse failed: " + parsed.ErrorMessage);
        var save = parsed.SaveData;
        if (save.Players.Count != 1) throw new InvalidDataException("checkpoint must contain exactly one player");
        if (save.VisitedMapCoords.Count == 0) throw new InvalidDataException("checkpoint has no visited map coordinate");
        var player = save.Players[0];
        var seed = SemanticProjectionBuilder.RequireText(save.SerializableRng?.Seed, "checkpoint seed");
        var character = SemanticProjectionBuilder.RequireText(player.CharacterId?.ToString(), "checkpoint character");
        if (save.CurrentActIndex < 0) throw new InvalidDataException("checkpoint act is negative");
        if (save.FloorReached < 0) throw new InvalidDataException("checkpoint floor is negative");
        var coord = save.VisitedMapCoords[^1];
        if (coord.row < 0 || coord.col < 0) throw new InvalidDataException("checkpoint coordinate is negative");
        var canonical = JsonSupport.Canonicalize(raw);
        return (new(stable.First, stable.Second, stable.Second.Sha256!, JsonSupport.Sha256Text(canonical),
            save.NumReloads, save.PreFinishedRoom is null, seed, character, save.CurrentActIndex,
            save.FloorReached, coord.row, coord.col), raw, stable.Bytes);
    }

    internal static void ValidateTarget(CheckpointEvidence checkpoint, TargetIdentity target,
        FileIdentity armSave, FloorContract floors)
    {
        if (!checkpoint.FirstRead.Exists || !checkpoint.SecondRead.Exists)
            throw new FileNotFoundException("room-before checkpoint is missing");
        if (checkpoint.FirstRead.Path != checkpoint.SecondRead.Path || checkpoint.FirstRead.Length != checkpoint.SecondRead.Length
            || checkpoint.FirstRead.Sha256 != checkpoint.SecondRead.Sha256
            || checkpoint.FirstRead.LastWriteUtc != checkpoint.SecondRead.LastWriteUtc)
            throw new InvalidDataException("two stable checkpoint identities differ");
        if (!checkpoint.PreFinishedRoomNull) throw new InvalidDataException("PreFinishedRoom is not null");
        if (checkpoint.RawSha256 == armSave.Sha256 && checkpoint.SecondRead.Length == armSave.Length)
            throw new InvalidDataException("room-before checkpoint equals arm-time non-null save");
        if (checkpoint.Seed != target.Seed || checkpoint.Character != target.Character
            || checkpoint.ActIndex != target.ActIndex || checkpoint.MapRow != target.MapRow
            || checkpoint.MapCol != target.MapCol)
            throw new InvalidDataException("checkpoint seed/character/act/map does not match armed target");
        floors.BindCheckpoint(checkpoint.FloorReached);
    }
}
