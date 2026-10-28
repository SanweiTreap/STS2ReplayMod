using System.Security.Cryptography;
using System.Text.Json;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpikeInv007A;

internal sealed record PriorAttemptIntegrityResult(PriorAttemptIntegrityEvidence Evidence,
    bool FixedEvidenceIdentityValid, bool Passed);

internal static class PriorAttemptIntegrity
{
    internal static PriorAttemptIntegrityResult Verify(SaveManager original, int originalIdentity,
        Func<bool> continueVisible)
    {
        var artifactValid = ExactFileIdentity(ProbeConstants.PriorArtifactFile,
            ProbeConstants.PriorArtifactLength, ProbeConstants.PriorArtifactSha256);
        RecoveryBaseline? recovery = null;
        try { recovery = ProtectionScope.CaptureRecoveryBaseline(ProbeConstants.PriorRecoveryRoot); }
        catch { }
        var recoveryValid = recovery is not null
            && recovery.FileCount == ProbeConstants.PriorRecoveryFileCount
            && string.Equals(recovery.AggregateSha256, ProbeConstants.PriorRecoveryAggregateSha256, StringComparison.Ordinal);
        ProtectionSnapshot? artifactBaseline = null;
        if (artifactValid)
        {
            try { artifactBaseline = ReadPriorProtectionAfter(); }
            catch { }
        }
        var artifactRecovery = artifactBaseline is not null && recovery is not null
            ? ProtectionScope.ComparePrior(artifactBaseline, recovery.Snapshot)
            : (false, false, false);

        ProtectionSnapshot? current = null;
        var activeProfile = false;
        try
        {
            var captured = ProtectionScope.CaptureCurrent(original);
            current = captured.Snapshot;
            activeProfile = true;
        }
        catch { }

        var recoveryCurrent = recovery is not null && current is not null
            ? ProtectionScope.ComparePrior(recovery.Snapshot, current)
            : (false, false, false);
        var artifactCurrent = artifactBaseline is not null && current is not null
            ? ProtectionScope.ComparePrior(artifactBaseline, current)
            : (false, false, false);
        var comparison = (recoveryCurrent.Item1 && artifactCurrent.Item1,
            recoveryCurrent.Item2 && artifactCurrent.Item2,
            recoveryCurrent.Item3 && artifactCurrent.Item3);
        var visible = false;
        try { visible = continueVisible(); } catch { }
        var runStateNull = false;
        try { runStateNull = RunManager.Instance?.DebugOnlyGetState() is null; } catch { }
        var managerStable = ReferenceEquals(original, SaveManager.Instance)
            && ScopeSafeSession.Identity(SaveManager.Instance) == originalIdentity;
        var evidence = new PriorAttemptIntegrityEvidence(artifactValid, recoveryValid,
            recovery?.FileCount ?? 0, artifactBaseline?.Files.Count ?? 0,
            artifactRecovery.Item1, artifactRecovery.Item2, artifactRecovery.Item3,
            activeProfile, comparison.Item1, comparison.Item2,
            comparison.Item3, visible, runStateNull, managerStable);
        var fixedIdentity = artifactValid && recoveryValid;
        var passed = fixedIdentity && artifactRecovery.Item1 && artifactRecovery.Item2
            && artifactRecovery.Item3 && activeProfile && comparison.Item1 && comparison.Item2
            && comparison.Item3 && visible && runStateNull && managerStable;
        return new PriorAttemptIntegrityResult(evidence, fixedIdentity, passed);
    }

    private static ProtectionSnapshot ReadPriorProtectionAfter()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ProbeConstants.PriorArtifactFile));
        var root = document.RootElement;
        var inventoryJson = root.GetProperty("protectionBefore").GetProperty("profileInventory");
        var directories = inventoryJson.GetProperty("directories").EnumerateArray()
            .Select(x => x.GetString() ?? throw new InvalidDataException()).ToList();
        var names = inventoryJson.GetProperty("files").EnumerateArray()
            .Select(x => x.GetString() ?? throw new InvalidDataException()).ToList();
        var files = new List<ProtectedFile>();
        foreach (var item in root.GetProperty("protectionAfter").EnumerateArray())
        {
            if (!Enum.TryParse<ProtectedScope>(item.GetProperty("scope").GetString(), false, out var scope))
                throw new InvalidDataException();
            var relative = item.GetProperty("relativeName").GetString() ?? throw new InvalidDataException();
            var exists = item.GetProperty("exists").GetBoolean();
            long? length = item.GetProperty("length").ValueKind == JsonValueKind.Null
                ? null : item.GetProperty("length").GetInt64();
            var sha256 = item.GetProperty("sha256").ValueKind == JsonValueKind.Null
                ? null : item.GetProperty("sha256").GetString();
            var lastWrite = item.GetProperty("lastWriteUtc").ValueKind == JsonValueKind.Null
                ? null : item.GetProperty("lastWriteUtc").GetString();
            files.Add(new ProtectedFile(scope, relative, exists, length, sha256, lastWrite));
        }
        if (files.Count != ProbeConstants.PriorRecoveryFileCount) throw new InvalidDataException();
        return new ProtectionSnapshot(files.OrderBy(x => x.Scope)
                .ThenBy(x => x.RelativeName, StringComparer.Ordinal).ToList(),
            new ProfileInventory(directories, names), Array.Empty<BackupEntry>());
    }

    private static bool ExactFileIdentity(string path, long expectedLength, string expectedSha256)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0
                || info.Length != expectedLength) return false;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return string.Equals(Convert.ToHexString(SHA256.HashData(stream)), expectedSha256,
                StringComparison.Ordinal);
        }
        catch { return false; }
    }
}
