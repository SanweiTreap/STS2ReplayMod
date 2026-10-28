using System.Security.Cryptography;
using System.Text.Json;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace SpikeInv008A;

internal sealed record PriorIntegrityResult(PriorIntegrityEvidence Evidence, ProtectionSnapshot Baseline,
    string? RecoveryRoot, ContinueEvidence? ContinueBefore, bool IdentityValid, bool Passed);

internal static class PriorIntegrity
{
    internal static PriorIntegrityResult VerifyR6(SaveManager original, int identity,
        Func<ContinueCapture> captureContinue)
    {
        ProtectionSnapshot? current = null;
        var active = false;
        try { (var ignored, current) = ProtectionScope.CaptureCurrent(original); active = true; } catch { }
        return VerifyR6FixedInputs(current, SafeContinue(captureContinue), active,
            ReferenceEquals(original, SaveManager.Instance) && ScopeSafeSession.Identity(original) == identity,
            RunManager.Instance?.DebugOnlyGetState() is null);
    }

    internal static PriorIntegrityResult VerifyR6FixedInputs(ProtectionSnapshot? current, ContinueCapture fresh,
        bool activeProfileOne, bool managerStable, bool runStateNull)
    {
        var artifactValid = Exact(ProbeConstants.R5SetupArtifact, ProbeConstants.R5SetupArtifactLength,
            ProbeConstants.R5SetupArtifactSha256);
        RecoveryBaseline? recovery = null;
        try { recovery = ProtectionScope.CaptureRecoveryBaseline(ProbeConstants.R5RecoveryRoot); } catch { }
        var recoveryValid = recovery is not null && recovery.FileCount == ProbeConstants.R5RecoveryFileCount &&
                            recovery.AggregateSha256 == ProbeConstants.R5RecoveryAggregateSha256;
        var historical = artifactValid ? ReadProtection(ProbeConstants.R5SetupArtifact, "protectionAfter") : null;
        var artifactRecovery = historical is not null && recovery is not null &&
                               AllEqual(ProtectionScope.ComparePrior(historical, recovery.Snapshot));
        var content = historical is not null && current is not null && ComparableHistoricalContentEqual(historical, current);
        var inventory = historical is not null && current is not null &&
                        historical.ProfileInventory.Directories.SequenceEqual(current.ProfileInventory.Directories, StringComparer.Ordinal) &&
                        historical.ProfileInventory.Files.SequenceEqual(current.ProfileInventory.Files, StringComparer.Ordinal);
        var settings = current is not null && CurrentSettingsIdentityValid(current);
        var continueOk = fresh.Failure == ContinueFailure.None && fresh.Evidence is { Visible: true, Enabled: true };
        var evidence = new PriorIntegrityEvidence(artifactValid, recoveryValid, artifactRecovery, activeProfileOne,
            content, inventory, settings, [], true, continueOk, fresh.Evidence, fresh.Failure, runStateNull,
            managerStable, "SPIKE-INV008A-R5 protected content + fixed settings + fresh Continue; R5 observer terminal is not a phase PASS");
        var identity = artifactValid && recoveryValid && settings;
        return new(evidence, current ?? Empty(), null, fresh.Evidence, identity, identity && artifactRecovery &&
            activeProfileOne && content && inventory && continueOk && runStateNull && managerStable);
    }

    internal static PriorIntegrityResult VerifyPreviousPhase(string artifactPath, SaveManager original, int identity,
        Func<ContinueCapture> captureContinue, bool restartComparison)
    {
        ProtectionSnapshot? baseline = null;
        string? recoveryRoot = null;
        var artifactValid = false;
        ContinueCapture expected = new(null, ContinueFailure.MissingSchemaField);
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(artifactPath));
            var root = doc.RootElement;
            artifactValid = root.GetProperty("schemaId").GetString() == ProbeConstants.SchemaId &&
                            root.GetProperty("probeVersion").GetString() == ProbeConstants.ProbeVersion;
            baseline = ReadProtection(root, "protectionAfter");
            if (root.TryGetProperty("recoveryRoot", out var recovery) && recovery.ValueKind == JsonValueKind.String)
                recoveryRoot = recovery.GetString();
            expected = ReadContinue(root);
        }
        catch { }
        RecoveryBaseline? recoveryBaseline = null;
        if (recoveryRoot is not null) try { recoveryBaseline = ProtectionScope.CaptureRecoveryBaseline(recoveryRoot); } catch { }
        var recoveryValid = recoveryRoot is null || recoveryBaseline is not null;
        var artifactRecovery = recoveryRoot is null || (baseline is not null && recoveryBaseline is not null &&
            AllEqual(ProtectionScope.ComparePrior(baseline, recoveryBaseline.Snapshot)));
        ProtectionSnapshot? current = null;
        var active = false;
        try { (var ignored, current) = ProtectionScope.CaptureCurrent(original); active = true; } catch { }
        var comparison = (FileSet: false, Content: false, Inventory: false,
            MtimeChanges: (IReadOnlyList<string>)[], MtimeAllowed: false);
        if (baseline is not null && current is not null)
        {
            if (restartComparison) comparison = RestartRules.Compare(baseline, current);
            else
            {
                var exact = ProtectionScope.ComparePrior(baseline, current);
                comparison = (exact.Item1, exact.Item2, exact.Item3, (IReadOnlyList<string>)[], exact.Item1 && exact.Item2 && exact.Item3);
            }
        }
        var actual = SafeContinue(captureContinue);
        var continueFailure = CompareContinue(expected.Evidence, actual);
        var continueEqual = expected.Failure == ContinueFailure.None && continueFailure == ContinueFailure.None;
        var manager = ReferenceEquals(original, SaveManager.Instance) && ScopeSafeSession.Identity(original) == identity;
        var stateNull = RunManager.Instance?.DebugOnlyGetState() is null;
        var evidence = new PriorIntegrityEvidence(artifactValid, recoveryValid, artifactRecovery, active,
            comparison.Content, comparison.Inventory, true, comparison.MtimeChanges, comparison.MtimeAllowed,
            continueEqual, actual.Evidence, continueFailure, stateNull, manager, artifactPath);
        var identityValid = artifactValid && recoveryValid && expected.Failure == ContinueFailure.None;
        return new(evidence, baseline ?? Empty(), recoveryRoot, actual.Evidence, identityValid, identityValid &&
            artifactRecovery && active && comparison.FileSet && comparison.Content && comparison.Inventory &&
            comparison.MtimeAllowed && continueEqual && stateNull && manager);
    }

    internal static ContinueCapture ReadHistoricalContinueCompatibility(string path)
    {
        try { using var doc = JsonDocument.Parse(File.ReadAllText(path)); return ReadContinue(doc.RootElement); }
        catch { return new(null, ContinueFailure.ReadError); }
    }

    internal static ContinueFailure CompareContinue(ContinueEvidence? expected, ContinueCapture actual)
    {
        if (actual.Failure != ContinueFailure.None) return actual.Failure;
        if (expected is null) return ContinueFailure.MissingSchemaField;
        var value = actual.Evidence!;
        if (value.NumReloads != expected.NumReloads) return ContinueFailure.NumReloadsMismatch;
        return value.CanonicalSha256 == expected.CanonicalSha256 ? ContinueFailure.None : ContinueFailure.CanonicalMismatch;
    }

    internal static ProtectionSnapshot ReadProtection(string path, string property)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return ReadProtection(doc.RootElement, property);
    }

    private static ProtectionSnapshot ReadProtection(JsonElement root, string property)
    {
        var files = new List<ProtectedFile>();
        foreach (var item in root.GetProperty(property).EnumerateArray())
        {
            Enum.TryParse(item.GetProperty("scope").GetString(), out ProtectedScope scope);
            files.Add(new(scope, item.GetProperty("relativeName").GetString()!, item.GetProperty("exists").GetBoolean(),
                NullableInt64(item, "length"), NullableString(item, "sha256"), NullableString(item, "lastWriteUtc")));
        }
        var inventoryRoot = root.GetProperty("protectionBefore").GetProperty("profileInventory");
        var dirs = inventoryRoot.GetProperty("directories").EnumerateArray().Select(x => x.GetString()!).ToList();
        var names = inventoryRoot.GetProperty("files").EnumerateArray().Select(x => x.GetString()!).ToList();
        return new(files, new(dirs, names), []);
    }

    private static ContinueCapture ReadContinue(JsonElement root)
    {
        JsonElement value;
        if (root.TryGetProperty("teardown", out var teardown) && teardown.ValueKind == JsonValueKind.Object &&
            teardown.TryGetProperty("continueAfter", out value) && value.ValueKind == JsonValueKind.Object)
            return new(ParseContinue(value), ContinueFailure.None);
        if (root.TryGetProperty("restart", out var restart) && restart.ValueKind == JsonValueKind.Object &&
            restart.TryGetProperty("continueCurrent", out value) && value.ValueKind == JsonValueKind.Object)
            return new(ParseContinue(value), ContinueFailure.None);
        return new(null, ContinueFailure.MissingSchemaField);
    }

    private static bool ComparableHistoricalContentEqual(ProtectionSnapshot historical, ProtectionSnapshot current)
    {
        var left = historical.Files.Where(x => x.Scope != ProtectedScope.MachineSettings).ToDictionary(x => (x.Scope, x.RelativeName));
        var right = current.Files.Where(x => x.Scope != ProtectedScope.MachineSettings).ToDictionary(x => (x.Scope, x.RelativeName));
        return left.Count == 11 && right.Count == 11 && left.Keys.All(right.ContainsKey) && left.All(pair =>
        {
            var value = right[pair.Key];
            return pair.Value.Exists == value.Exists && pair.Value.Length == value.Length && pair.Value.Sha256 == value.Sha256;
        });
    }

    private static bool CurrentSettingsIdentityValid(ProtectionSnapshot current)
    {
        var values = current.Files.Where(x => x.Scope == ProtectedScope.MachineSettings && x.RelativeName == "settings.save").ToList();
        return values.Count == 1 && values[0].Exists && values[0].Length == ProbeConstants.R4SettingsLength && values[0].Sha256 == ProbeConstants.R4SettingsSha256;
    }

    private static ContinueEvidence ParseContinue(JsonElement value) => new(value.GetProperty("visible").GetBoolean(),
        value.GetProperty("enabled").GetBoolean(), value.GetProperty("numReloads").GetInt32(), value.GetProperty("canonicalSha256").GetString()!);
    private static ContinueCapture SafeContinue(Func<ContinueCapture> read)
    { try { return read(); } catch { return new(null, ContinueFailure.ReadError); } }
    private static bool AllEqual((bool, bool, bool) value) => value.Item1 && value.Item2 && value.Item3;
    private static long? NullableInt64(JsonElement item, string name) => item.GetProperty(name).ValueKind == JsonValueKind.Null ? null : item.GetProperty(name).GetInt64();
    private static string? NullableString(JsonElement item, string name) => item.GetProperty(name).ValueKind == JsonValueKind.Null ? null : item.GetProperty(name).GetString();
    private static ProtectionSnapshot Empty() => new([], new([], []), []);
    private static bool Exact(string path, long length, string hash)
    {
        try { var info = new FileInfo(path); using var stream = File.OpenRead(path); return info.Length == length && Convert.ToHexString(SHA256.HashData(stream)) == hash; }
        catch { return false; }
    }
}

internal static class PriorGateReachability
{
    internal static bool CanReachGateReady(PriorIntegrityResult prior, GateConditions timing, bool exactLoadedSet) =>
        prior.IdentityValid && prior.Passed && exactLoadedSet && timing.PatchBeforeGraphics && timing.GraphicsNormalAndIdle &&
        timing.ExactlyOneOuterTask && timing.OuterTaskTerminal && timing.StartupMarkerTerminal && timing.MenuReady &&
        timing.ManagerIdentityStable && timing.RunStateNull && timing.CurrentRunTaskNullOrTerminal && timing.NoTerminalFailure && timing.NoPostGateLifecycle;
}
