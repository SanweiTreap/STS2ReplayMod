using System.Text.Json.Nodes;

namespace Spike001A4;

internal static class ExperimentRules
{
    internal static readonly IReadOnlySet<string> ExitMtimeWhitelist = new HashSet<string>(StringComparer.Ordinal)
    {
        "settings.save", "saves/prefs.save.backup", "saves/progress.save.backup"
    };

    internal static bool ExactModSet(IReadOnlyList<string> ids) => ids.Count == 1 && ids[0] == ProbeConstants.Id;
    internal static bool IsProfileOne(int id, string token, bool runningModded)
        => id == 1 && token == "modded/profile1" && runningModded;
    internal static bool IsCloudOff(bool accountEnabled, bool appEnabled) => !(accountEnabled && appEnabled);
    internal static bool SameCheckpointContent(FileIdentity baseline, FileIdentity current)
        => baseline.Path == current.Path && baseline.Exists && current.Exists
           && baseline.Length == current.Length && baseline.Sha256 == current.Sha256;
    internal static bool ShouldWrapCheckpoint(bool armed, bool targetMatches, object? preFinishedRoom, bool saveProgress)
        => armed && targetMatches && preFinishedRoom is null && saveProgress;
    internal static bool BreaksContinuity(bool requiredReady, bool consecutiveFrame, bool identitiesStable)
        => !requiredReady || !consecutiveFrame || !identitiesStable;
    internal static bool CollectionAllowed(bool terminal, ProbePhase phase, bool baselineBound)
        => !terminal && baselineBound && phase is ProbePhase.AwaitA or ProbePhase.AwaitB;

    internal static SaveDiffResult CompareContinueSave(string beforeJson, string afterJson, int beforeReloads, int afterReloads)
    {
        var all = JsonSupport.DiffJson(beforeJson, afterJson);
        var nonAllowed = all.Where(x => !IsNumReloadsPath(x.Path)).ToList();
        var deltaOk = afterReloads == beforeReloads + 1;
        var numDiffs = all.Where(x => IsNumReloadsPath(x.Path)).ToList();
        var passed = deltaOk && nonAllowed.Count == 0 && numDiffs.Count == 1;
        var failure = passed ? "None" : !deltaOk ? "NumReloadsNotExactlyPlusOne" :
            nonAllowed.Count != 0 ? "NonAllowlistedSaveDiff" : "MissingOrDuplicateNumReloadsDiff";
        return new(passed, beforeReloads, afterReloads, all, nonAllowed, failure);
    }
    private static bool IsNumReloadsPath(string path)
    {
        var leaf = path.Split('.').Last().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();
        return leaf == "numreloads";
    }

    internal static ProtectionDiffResult CompareProtection(ProtectedState before, ProtectedState after,
        bool allowExitMtimes, bool allowCurrentRunMutation)
    {
        var inventoryEqual = before.Inventory.Directories.SequenceEqual(after.Inventory.Directories, StringComparer.Ordinal)
            && before.Inventory.Files.SequenceEqual(after.Inventory.Files, StringComparer.Ordinal);
        var left = before.Files.ToDictionary(x => Relative(before.ProfileRoot, x.Path), StringComparer.Ordinal);
        var right = after.Files.ToDictionary(x => Relative(after.ProfileRoot, x.Path), StringComparer.Ordinal);
        var content = new List<string>(); var lengths = new List<string>(); var mtimes = new List<string>();
        foreach (var name in left.Keys.Concat(right.Keys).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!left.TryGetValue(name, out var l) || !right.TryGetValue(name, out var r)) { content.Add(name); continue; }
            var currentRun = name == "saves/current_run.save";
            if ((!currentRun || !allowCurrentRunMutation) && l.Sha256 != r.Sha256) content.Add(name);
            if ((!currentRun || !allowCurrentRunMutation) && l.Length != r.Length) lengths.Add(name);
            if (l.LastWriteUtc != r.LastWriteUtc) mtimes.Add(name);
        }
        var nonAllowedMtime = mtimes.Where(x => !(allowCurrentRunMutation && x == "saves/current_run.save")
            && !(allowExitMtimes && ExitMtimeWhitelist.Contains(x))).ToList();
        return new(inventoryEqual && content.Count == 0 && lengths.Count == 0 && nonAllowedMtime.Count == 0,
            inventoryEqual, content, lengths, mtimes, nonAllowedMtime);
    }
    private static string Relative(string profileRoot, string path)
    {
        if (path.StartsWith(profileRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            return Path.GetRelativePath(profileRoot, path).Replace('\\', '/');
        return Path.GetFileName(path);
    }
}
