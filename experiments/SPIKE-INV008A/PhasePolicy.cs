using System.Security.Cryptography;
using System.Text.Json;

namespace SpikeInv008A;

internal sealed record PhaseDecision(ProbePhase Phase, string ArtifactPath, string? PreviousArtifact, string? Error,
    PriorAttemptEvidence? PriorAttempt = null);

internal static class PhasePolicy
{
    internal static PhaseDecision Resolve(string root)
    {
        var prior = ValidatePriorAttempts();
        if (!prior.Passed) return Invalid(root, "prior attempt unresolved: " + prior.Failure, prior);
        var paths = new[]
        {
            Path.Combine(root, ProbeConstants.SetupArtifact),
            Path.Combine(root, ProbeConstants.SetupRestartArtifact),
            Path.Combine(root, ProbeConstants.PostLaunchArtifact),
            Path.Combine(root, ProbeConstants.PostLaunchRestartArtifact)
        };
        if (paths.Any(path => File.Exists(path + ".tmp"))) return Invalid(root, "temporary artifact exists");
        var exists = paths.Select(File.Exists).ToArray();
        for (var i = 1; i < exists.Length; i++)
            if (exists[i] && !exists[i - 1]) return Invalid(root, "artifact chain has a gap");
        var completed = Array.FindLastIndex(exists, value => value);
        for (var i = 0; i <= completed; i++)
            if (!ValidPassArtifact(paths[i], (ProbePhase)i, i == 0 ? null : paths[i - 1]))
                return Invalid(root, "artifact chain is not identity-valid PASS");
        return completed switch
        {
            -1 => new(ProbePhase.SetupOwnerFault, paths[0], null, null, prior),
            0 => new(ProbePhase.RestartAfterSetup, paths[1], paths[0], null, prior),
            1 => new(ProbePhase.PostLaunchOwnerFault, paths[2], paths[1], null, prior),
            2 => new(ProbePhase.RestartAfterPostLaunch, paths[3], paths[2], null, prior),
            3 => new(ProbePhase.Complete, paths[3], paths[3], null, prior),
            _ => Invalid(root, "unknown chain state")
        };
    }

    internal static bool IsFault(ProbePhase phase) => phase is ProbePhase.SetupOwnerFault or ProbePhase.PostLaunchOwnerFault;
    internal static bool IsRestart(ProbePhase phase) => phase is ProbePhase.RestartAfterSetup or ProbePhase.RestartAfterPostLaunch;
    internal static string RecoveryPrefix(ProbePhase phase) => phase switch
    {
        ProbePhase.SetupOwnerFault => "r6-setup-",
        ProbePhase.PostLaunchOwnerFault => "r6-post-launch-",
        _ => throw new InvalidOperationException("restart phases may not create recovery")
    };
    internal static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    internal static PriorAttemptEvidence ValidatePriorAttempts()
    {
        var setupIdentity = Exact(ProbeConstants.V1SetupArtifact, ProbeConstants.V1SetupArtifactLength,
            ProbeConstants.V1SetupArtifactSha256);
        var invalidIdentity = Exact(ProbeConstants.V1InvalidArtifact, ProbeConstants.V1InvalidArtifactLength,
            ProbeConstants.V1InvalidArtifactSha256);
        var setupNull = setupIdentity && PreBusinessNull(ProbeConstants.V1SetupArtifact, "SetupOwnerFault",
            "BLOCKED_IDENTITY", "Identity");
        var invalidNull = invalidIdentity && PreBusinessNull(ProbeConstants.V1InvalidArtifact, "Invalid",
            "BLOCKED_PHASE_ORDER", "PhaseOrder");
        var recoveryAbsent = V1RecoveryAbsent();
        var r2Identity = Exact(ProbeConstants.R2PrecheckArtifact, ProbeConstants.R2PrecheckArtifactLength,
            ProbeConstants.R2PrecheckArtifactSha256);
        var r2Contract = r2Identity && R2PrecheckContract();
        var r2BusinessAbsent = R2BusinessArtifactsAbsent();
        var r2RecoveryAbsent = R2RecoveryAbsent();
        var r3Identity = Exact(ProbeConstants.R3SetupArtifact, ProbeConstants.R3SetupArtifactLength,
            ProbeConstants.R3SetupArtifactSha256);
        var r3Contract = r3Identity && R3PreBusinessContract();
        var r3LaterAbsent = R3LaterArtifactsAbsent();
        var r3RecoveryAbsent = R3RecoveryAbsent();
        var r4Identity = Exact(ProbeConstants.R4SetupArtifact, ProbeConstants.R4SetupArtifactLength,
            ProbeConstants.R4SetupArtifactSha256);
        var r4Contract = r4Identity && R4Contract();
        var r4LaterAbsent = R4LaterArtifactsAbsent();
        var r4Recovery = R4RecoveryIdentityValid();
        var inv009Il = Exact(ProbeConstants.Inv009IlOutput, ProbeConstants.Inv009IlOutputLength,
            ProbeConstants.Inv009IlOutputSha256);
        var inv009Summary = Exact(ProbeConstants.Inv009Summary, ProbeConstants.Inv009SummaryLength,
            ProbeConstants.Inv009SummarySha256);
        var r5Identity = Exact(ProbeConstants.R5SetupArtifact, ProbeConstants.R5SetupArtifactLength,
            ProbeConstants.R5SetupArtifactSha256);
        var r5Contract = r5Identity && R5Contract();
        var r5LaterAbsent = R5LaterArtifactsAbsent();
        var r5Recovery = R5RecoveryIdentityValid();
        var passed = setupIdentity && invalidIdentity && setupNull && invalidNull && recoveryAbsent && r2Identity &&
                     r2Contract && r2BusinessAbsent && r2RecoveryAbsent && r3Identity && r3Contract &&
                     r3LaterAbsent && r3RecoveryAbsent && r4Identity && r4Contract && r4LaterAbsent && r4Recovery &&
                     inv009Il && inv009Summary && r5Identity && r5Contract && r5LaterAbsent && r5Recovery;
        var failure = !setupIdentity ? "setup artifact identity" : !invalidIdentity ? "invalid artifact identity" :
            !setupNull ? "setup artifact pre-business-null contract" : !invalidNull ? "invalid artifact pre-business-null contract" :
            !recoveryAbsent ? "v1 recovery unexpectedly exists" : !r2Identity ? "r2 precheck artifact identity" :
            !r2Contract ? "r2 precheck contract" : !r2BusinessAbsent ? "r2 business artifact unexpectedly exists" :
            !r2RecoveryAbsent ? "r2 recovery unexpectedly exists" : !r3Identity ? "r3 setup artifact identity" :
            !r3Contract ? "r3 pre-business contract" : !r3LaterAbsent ? "r3 later artifact unexpectedly exists" :
            !r3RecoveryAbsent ? "r3 recovery unexpectedly exists" : !r4Identity ? "r4 setup artifact identity" :
            !r4Contract ? "r4 revised-oracle contract" : !r4LaterAbsent ? "r4 later artifact unexpectedly exists" :
            !r4Recovery ? "r4 recovery identity" : !inv009Il ? "inv009 IL output identity" :
            !inv009Summary ? "inv009 bounded summary identity" : !r5Identity ? "r5 setup artifact identity" :
            !r5Contract ? "r5 observer-failure contract" : !r5LaterAbsent ? "r5 later artifact unexpectedly exists" :
            !r5Recovery ? "r5 recovery identity" : "None";
        return new(setupIdentity, invalidIdentity, setupNull, invalidNull, recoveryAbsent, r2Identity, r2Contract,
            r2BusinessAbsent, r2RecoveryAbsent, r3Identity, r3Contract, r3LaterAbsent, r3RecoveryAbsent,
            r4Identity, r4Contract, r4LaterAbsent, r4Recovery, inv009Il, inv009Summary,
            r5Identity, r5Contract, r5LaterAbsent, r5Recovery, passed, failure);
    }

    private static bool R5Contract()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ProbeConstants.R5SetupArtifact));
            var root = doc.RootElement;
            if (root.GetProperty("schemaId").GetString() != "spike-inv008a.owner-fault.v5" ||
                root.GetProperty("probeVersion").GetString() != "0.5.0-owner-fault-matrix-r5-b1" ||
                root.GetProperty("phase").GetString() != "SetupOwnerFault" ||
                root.GetProperty("terminalClassification").GetString() != "FAIL_TEARDOWN_MENU" ||
                root.GetProperty("firstFailureSite").GetString() != "MainMenu" ||
                root.GetProperty("restart").ValueKind != JsonValueKind.Null ||
                root.GetProperty("recoveryRoot").GetString() != ProbeConstants.R5RecoveryRoot) return false;
            var teardown = root.GetProperty("teardown");
            var cleanup = teardown.GetProperty("cleanup");
            return root.GetProperty("lobbyOracle").GetProperty("passed").GetBoolean() &&
                   cleanup.GetProperty("callOracle").GetProperty("callSetPass").GetBoolean() &&
                   teardown.GetProperty("originalManagerRestored").GetBoolean() &&
                   teardown.GetProperty("protectionUnchanged").GetBoolean() &&
                   teardown.GetProperty("continueFailure").GetString() == "None" &&
                   root.GetProperty("scene").GetProperty("passed").GetBoolean() &&
                   !teardown.GetProperty("menuReadyObserved").GetBoolean() &&
                   !teardown.GetProperty("refreshButtonsObserved").GetBoolean() &&
                   teardown.GetProperty("stepErrors").EnumerateArray().Any(x => x.GetString() == "main-menu-load:TimeoutException");
        }
        catch { return false; }
    }

    private static bool R5LaterArtifactsAbsent()
    {
        try
        {
            return new[] { "restart-after-setup-r5.json", "post-launch-owner-fault-r5.json",
                "restart-after-post-launch-r5.json", "invalid-phase-r5.json" }.All(name =>
                !File.Exists(Path.Combine(ProbeConstants.EvidenceRoot, name)) &&
                !File.Exists(Path.Combine(ProbeConstants.EvidenceRoot, name + ".tmp")));
        }
        catch { return false; }
    }

    private static bool R5RecoveryIdentityValid()
    {
        try
        {
            var recovery = ProtectionScope.CaptureRecoveryBaseline(ProbeConstants.R5RecoveryRoot);
            var root = Path.Combine(ProbeConstants.EvidenceRoot, "restricted-recovery");
            var directories = Directory.EnumerateDirectories(root).Select(Path.GetFileName)
                .Where(name => name?.StartsWith("r5-", StringComparison.Ordinal) == true).ToList();
            return directories.Count == 1 && recovery.FileCount == ProbeConstants.R5RecoveryFileCount &&
                   recovery.AggregateSha256 == ProbeConstants.R5RecoveryAggregateSha256;
        }
        catch { return false; }
    }

    private static bool R4Contract()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ProbeConstants.R4SetupArtifact));
            var root = doc.RootElement;
            if (root.GetProperty("schemaId").GetString() != "spike-inv008a.owner-fault.v4" ||
                root.GetProperty("probeVersion").GetString() != "0.4.0-owner-fault-matrix-r4-b1" ||
                root.GetProperty("phase").GetString() != "SetupOwnerFault" ||
                root.GetProperty("terminalClassification").GetString() != "FAIL_TEARDOWN_CLEANUP" ||
                root.GetProperty("firstFailureSite").GetString() != "Cleanup" ||
                root.GetProperty("restart").ValueKind != JsonValueKind.Null ||
                root.GetProperty("recoveryRoot").GetString() != ProbeConstants.R4RecoveryRoot) return false;
            var cleanup = root.GetProperty("teardown").GetProperty("cleanup");
            var counts = cleanup.GetProperty("reachedSteps").EnumerateArray()
                .ToDictionary(x => x.GetString()!, _ => 1, StringComparer.Ordinal);
            var callSet = PhaseAwareOracle.EvaluateCleanup(counts, true, cleanup.GetProperty("exceptions").GetInt32());
            return callSet.CoreCallSetPass && callSet.LobbyGroupPass && callSet.CallSetPass &&
                   !root.GetProperty("teardown").GetProperty("menuReadyObserved").GetBoolean() &&
                   !root.GetProperty("teardown").GetProperty("refreshButtonsObserved").GetBoolean();
        }
        catch { return false; }
    }

    private static bool R4LaterArtifactsAbsent()
    {
        try
        {
            return new[] { "restart-after-setup-r4.json", "postlaunch-owner-fault-r4.json",
                "restart-after-postlaunch-r4.json", "invalid-phase-r4.json" }.All(name =>
                !File.Exists(Path.Combine(ProbeConstants.EvidenceRoot, name)) &&
                !File.Exists(Path.Combine(ProbeConstants.EvidenceRoot, name + ".tmp")));
        }
        catch { return false; }
    }

    private static bool R4RecoveryIdentityValid()
    {
        try
        {
            var recovery = ProtectionScope.CaptureRecoveryBaseline(ProbeConstants.R4RecoveryRoot);
            var root = Path.Combine(ProbeConstants.EvidenceRoot, "restricted-recovery");
            var r4Directories = Directory.EnumerateDirectories(root).Select(Path.GetFileName)
                .Where(name => name?.StartsWith("r4-", StringComparison.Ordinal) == true).ToList();
            return r4Directories.Count == 1 && recovery.FileCount == ProbeConstants.R4RecoveryFileCount &&
                   recovery.AggregateSha256 == ProbeConstants.R4RecoveryAggregateSha256;
        }
        catch { return false; }
    }

    private static bool R3PreBusinessContract()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ProbeConstants.R3SetupArtifact));
            var root = doc.RootElement;
            if (root.GetProperty("schemaId").GetString() != "spike-inv008a.owner-fault.v3" ||
                root.GetProperty("probeVersion").GetString() != "0.3.0-owner-fault-matrix-r3-b1" ||
                root.GetProperty("phase").GetString() != "SetupOwnerFault" ||
                root.GetProperty("terminalClassification").GetString() != "BLOCKED_PRIOR_INTEGRITY" ||
                root.GetProperty("firstFailureSite").GetString() != "PriorIntegrity") return false;
            foreach (var name in new[] { "gate", "runVerify", "protectionBefore", "protectionAfter", "recoveryRoot",
                         "candidate", "fault", "audit", "teardown", "restart", "previousArtifactSha256" })
                if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Null) return false;
            return root.TryGetProperty("priorIntegrity", out var prior) && prior.ValueKind == JsonValueKind.Object;
        }
        catch { return false; }
    }

    private static bool R3LaterArtifactsAbsent()
    {
        try
        {
            return new[] { "restart-after-setup-r3.json", "postlaunch-owner-fault-r3.json",
                "restart-after-postlaunch-r3.json", "invalid-phase-r3.json" }.All(name =>
                !File.Exists(Path.Combine(ProbeConstants.EvidenceRoot, name)) &&
                !File.Exists(Path.Combine(ProbeConstants.EvidenceRoot, name + ".tmp")));
        }
        catch { return false; }
    }

    private static bool R3RecoveryAbsent()
    {
        try
        {
            var root = Path.Combine(ProbeConstants.EvidenceRoot, "restricted-recovery");
            if (!Directory.Exists(root)) return true;
            return !Directory.EnumerateDirectories(root).Select(Path.GetFileName).Any(name => name is not null &&
                (name.StartsWith("r3-setup-", StringComparison.Ordinal) ||
                 name.StartsWith("r3-postlaunch-", StringComparison.Ordinal)));
        }
        catch { return false; }
    }

    private static bool R2PrecheckContract()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ProbeConstants.R2PrecheckArtifact));
            var root = doc.RootElement;
            return root.GetProperty("schemaId").GetString() == "spike-inv008a.modset-precheck.v2" &&
                   root.GetProperty("probeRevision").GetString() == "0.2.0-owner-fault-matrix-r2" &&
                   root.GetProperty("attemptToken").GetString() == "998e07af004a4268b1c33dd2e7644bc5" &&
                   root.GetProperty("loadedModCount").GetInt32() == 0 &&
                   root.GetProperty("loadedModIds").GetArrayLength() == 0 &&
                   root.GetProperty("failure").GetString() == "WrongLoadedModSet";
        }
        catch { return false; }
    }

    private static bool R2BusinessArtifactsAbsent()
    {
        try
        {
            return new[] { "setup-owner-fault-r2.json", "restart-after-setup-r2.json",
                "postlaunch-owner-fault-r2.json", "restart-after-postlaunch-r2.json", "invalid-phase-r2.json" }
                .All(name => !File.Exists(Path.Combine(ProbeConstants.EvidenceRoot, name)) &&
                             !File.Exists(Path.Combine(ProbeConstants.EvidenceRoot, name + ".tmp")));
        }
        catch { return false; }
    }

    private static bool R2RecoveryAbsent()
    {
        try
        {
            var root = Path.Combine(ProbeConstants.EvidenceRoot, "restricted-recovery");
            if (!Directory.Exists(root)) return true;
            return !Directory.EnumerateDirectories(root).Select(Path.GetFileName).Any(name => name is not null &&
                (name.StartsWith("r2-setup-", StringComparison.Ordinal) ||
                 name.StartsWith("r2-postlaunch-", StringComparison.Ordinal)));
        }
        catch { return false; }
    }

    private static bool PreBusinessNull(string path, string phase, string terminal, string site)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.GetProperty("schemaId").GetString() != "spike-inv008a.owner-fault.v1" ||
                root.GetProperty("probeVersion").GetString() != "0.1.0-owner-fault-matrix" ||
                root.GetProperty("phase").GetString() != phase ||
                root.GetProperty("terminalClassification").GetString() != terminal ||
                root.GetProperty("firstFailureSite").GetString() != site) return false;
            foreach (var name in new[] { "gate", "runVerify", "priorIntegrity", "protectionBefore", "protectionAfter",
                         "recoveryRoot", "candidate", "fault", "audit", "teardown", "restart", "previousArtifactSha256" })
                if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Null) return false;
            return true;
        }
        catch { return false; }
    }

    private static bool V1RecoveryAbsent()
    {
        try
        {
            var root = Path.Combine(ProbeConstants.EvidenceRoot, "restricted-recovery");
            if (!Directory.Exists(root)) return true;
            return !Directory.EnumerateDirectories(root).Select(Path.GetFileName).Any(name =>
                name is not null && (name.StartsWith("setup-", StringComparison.Ordinal) ||
                                     name.StartsWith("postlaunch-", StringComparison.Ordinal)));
        }
        catch { return false; }
    }

    private static bool Exact(string path, long length, string hash)
    {
        try { var item = new FileInfo(path); return item.Length == length && Sha256(path) == hash; }
        catch { return false; }
    }

    private static bool ValidPassArtifact(string path, ProbePhase expected, string? previous)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.GetProperty("schemaId").GetString() != ProbeConstants.SchemaId ||
                root.GetProperty("probeVersion").GetString() != ProbeConstants.ProbeVersion ||
                root.GetProperty("phase").GetString() != expected.ToString()) return false;
            var terminal = root.GetProperty("terminalClassification").GetString();
            var hasPrior = root.TryGetProperty("previousArtifactSha256", out var priorValue);
            var previousValid = previous is null
                ? !hasPrior || priorValue.ValueKind == JsonValueKind.Null
                : hasPrior && priorValue.GetString() == Sha256(previous);
            return previousValid && (expected is ProbePhase.SetupOwnerFault or ProbePhase.PostLaunchOwnerFault
                ? terminal == "PASS_OWNER_FAULT_TEARDOWN"
                : terminal == "PASS_RESTART_INTEGRITY");
        }
        catch { return false; }
    }

    private static PhaseDecision Invalid(string root, string error, PriorAttemptEvidence? prior = null)
        => new(ProbePhase.Invalid, Path.Combine(root, ProbeConstants.InvalidArtifact), null, error, prior);
}

internal sealed class TeardownPlan
{
    private int _owner;
    internal int OwnerCount { get; private set; }
    internal List<string> Reached { get; } = [];
    internal List<string> Errors { get; } = [];

    internal bool TryOwn()
    {
        if (Interlocked.CompareExchange(ref _owner, 1, 0) != 0) return false;
        OwnerCount++;
        return true;
    }

    internal void Step(string name, Action action)
    {
        Reached.Add(name + ":entry");
        try { action(); Reached.Add(name + ":return"); }
        catch (Exception exception) { Errors.Add(name + ":" + exception.GetType().Name); }
    }
}

internal static class RestartRules
{
    internal static (bool FileSet, bool Content, bool Inventory, IReadOnlyList<string> MtimeChanges, bool MtimeAllowed)
        Compare(ProtectionSnapshot prior, ProtectionSnapshot current)
    {
        var left = prior.Files.ToDictionary(x => (x.Scope, x.RelativeName));
        var right = current.Files.ToDictionary(x => (x.Scope, x.RelativeName));
        var fileSet = left.Count == right.Count && left.Keys.All(right.ContainsKey);
        var content = fileSet && left.All(pair =>
        {
            var value = right[pair.Key];
            return pair.Value.Exists == value.Exists && pair.Value.Length == value.Length && pair.Value.Sha256 == value.Sha256;
        });
        var inventory = prior.ProfileInventory.Directories.SequenceEqual(current.ProfileInventory.Directories, StringComparer.Ordinal)
                        && prior.ProfileInventory.Files.SequenceEqual(current.ProfileInventory.Files, StringComparer.Ordinal);
        var changes = fileSet
            ? left.Where(pair => pair.Value.LastWriteUtc != right[pair.Key].LastWriteUtc)
                .Select(pair => pair.Key.Scope + ":" + pair.Key.RelativeName).OrderBy(x => x, StringComparer.Ordinal).ToList()
            : [];
        return (fileSet, content, inventory, changes, changes.All(ProbeConstants.RestartMtimeWhitelist.Contains));
    }
}
