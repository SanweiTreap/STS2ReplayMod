using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Runs;

namespace Spike001A2R2;

internal static class CheckpointReader
{
    internal static (CheckpointEvidence Evidence, string RawJson) Capture(RunState? liveState)
    {
        var file = ProtectionScope.CurrentRunIdentity(); var raw = ProtectionScope.ReadStableText(file);
        var parsed = JsonSerializationUtility.FromJson<SerializableRun>(raw);
        if (!parsed.Success || parsed.SaveData is null) throw new InvalidDataException("native current_run.save parse failed: " + parsed.ErrorMessage);
        var save = parsed.SaveData; var canonical = JsonSupport.Canonicalize(raw);
        var state = liveState;
        var character = state?.Players.SingleOrDefault()?.Character.Id.ToString() ?? "unavailable-at-main-menu";
        return (new(file, file.Sha256!, JsonSupport.Sha256Text(canonical), save.NumReloads,
            save.PreFinishedRoom is null, save.SerializableRng.Seed ?? "", character,
            state?.CurrentActIndex ?? -1, state?.TotalFloor ?? -1, state?.CurrentMapCoord?.row, state?.CurrentMapCoord?.col), raw);
    }
}
