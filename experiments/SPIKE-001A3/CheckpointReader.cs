using System.Text;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Spike001A3;

internal static class CheckpointReader
{
    internal static (CheckpointEvidence Evidence, string RawJson, byte[] RawBytes) Capture()
    {
        var stable = ProtectionScope.StableReadTwice();
        var raw = Encoding.UTF8.GetString(stable.Bytes);
        var parsed = JsonSerializationUtility.FromJson<SerializableRun>(raw);
        if (!parsed.Success || parsed.SaveData is null)
            throw new InvalidDataException("native current_run.save parse failed: " + parsed.ErrorMessage);
        var save = parsed.SaveData; var player = save.Players.SingleOrDefault();
        var coord = save.VisitedMapCoords.LastOrDefault(); var canonical = JsonSupport.Canonicalize(raw);
        return (new(stable.First, stable.Second, stable.Second.Sha256!, JsonSupport.Sha256Text(canonical),
            save.NumReloads, save.PreFinishedRoom is null, save.SerializableRng?.Seed ?? "",
            player?.CharacterId?.ToString() ?? "", save.CurrentActIndex, save.FloorReached,
            coord.row, coord.col), raw, stable.Bytes);
    }

    internal static void ValidateTarget(CheckpointEvidence checkpoint, TargetIdentity target, FileIdentity armSave)
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
            || checkpoint.ActIndex != target.ActIndex || checkpoint.FloorReached != target.TotalFloor
            || checkpoint.MapRow != target.MapRow || checkpoint.MapCol != target.MapCol)
            throw new InvalidDataException("checkpoint seed/character/act/floor/map does not match armed target");
    }
}
