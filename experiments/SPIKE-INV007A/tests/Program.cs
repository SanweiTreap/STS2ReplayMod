using SpikeInv007A;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace SpikeInv007A.Tests;

internal static class Program
{
    private static int Main()
    {
        var cases = new (string, Action)[]
        {
            ("original store returns before args", OriginalStorePoison),
            ("unknown store returns before args", UnknownStorePoison),
            ("candidate store may read args", CandidateReads),
            ("sealed prefix returns before instance work", SealedReturns),
            ("original and unknown manager fail", ManagerIdentity),
            ("single CAS terminal winner", AtomicTerminal),
            ("post-seal commit rejected", PostSealCommit),
            ("closed set remains stable", ClosedSetStable),
            ("unauthorized artifact ticket rejected", ArtifactTicket),
            ("runtime schema whitelist", SchemaWhitelist),
            ("new identity and schema", NewIdentity),
            ("fixed build identity negative", IdentityNegative),
            ("exact logical progress name ordinal", LogicalName),
            ("precondition enums are independent", PreconditionSites),
            ("mutation requires one update and zero writes", MutationContract),
            ("swallowed save write fails closed", SwallowedWrite),
            ("write requires exact single synchronous payload", WriteContract),
            ("duplicate write fails", DuplicateWrite),
            ("classification strings are complete", Classifications),
            ("GateReady disarms timeout without effects", GateReadyBeforeTimeout),
            ("timeout before gate yields one terminal", TimeoutBeforeGate),
            ("GateReady timeout race has one winner", GateReadyTimeoutRace),
            ("RunAccepted makes timeout inert", RunAcceptedBeforeTimeout),
            ("R2 artifact admits R3", PriorArtifactDoesNotBlock),
            ("existing R3 artifact blocks without overwrite", R2ArtifactBlocks),
            ("R2 artifact and recovery identities unchanged", PriorEvidenceIdentity),
            ("install replacement requires exact R2 pair", InstallScopeIdentity),
            ("old ToSerializable aliases and pollutes", OldSerializableAliases),
            ("JSON round trip detaches mutable stats", JsonRoundTripDetaches),
            ("detached expected leaves candidate and discovery baseline unchanged", DetachedApplyLeavesBaselines),
            ("detached oracle shared reference count is zero", DetachedSharedCountZero),
            ("encounter and every enemy occurrence increment once", EveryOccurrenceIncrements),
            ("parse and alias gates fail closed before update", OracleGatesFailClosed)
        };
        var failed = 0;
        foreach (var (name, action) in cases)
        {
            try { action(); Console.WriteLine("PASS " + name); }
            catch { failed++; Console.WriteLine("FAIL " + name); }
        }
        Console.WriteLine($"TOTAL {cases.Length} PASSED {cases.Length - failed} FAILED {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static (IdentityFirstFixture F, object O, object C, object M) New()
    {
        var original = new object();
        var candidate = new object();
        var memory = new object();
        return (new IdentityFirstFixture(original, candidate, memory), original, candidate, memory);
    }

    private static void OriginalStorePoison()
    {
        var x = New();
        var poison = new PoisonArgs();
        x.F.Store(x.O, poison.Read);
        Eq(0, poison.Reads);
        Eq(1, x.F.Failures);
    }

    private static void UnknownStorePoison()
    {
        var x = New();
        var poison = new PoisonArgs();
        x.F.Store(new object(), poison.Read);
        Eq(0, poison.Reads);
        Eq(1, x.F.Failures);
    }

    private static void CandidateReads()
    {
        var x = New();
        var reads = 0;
        x.F.Store(x.M, _ => { reads++; return "x"; });
        Eq(2, reads);
        Eq(1, x.F.CandidateWrites);
    }

    private static void SealedReturns()
    {
        var x = New();
        var poison = new PoisonArgs();
        x.F.Sealed = true;
        x.F.Store(x.M, poison.Read);
        Eq(0, poison.Reads);
        Eq(0, x.F.ArgumentReads);
    }

    private static void ManagerIdentity()
    {
        var x = New();
        x.F.Manager(x.O);
        x.F.Manager(new object());
        x.F.Manager(x.C);
        Eq(2, x.F.Failures);
    }

    private static void AtomicTerminal()
    {
        var session = new ScopeSafeSession();
        var wins = 0;
        Parallel.For(0, 100, _ =>
        {
            if (session.TrySeal(TerminalClassification.FailBoundary, FailureSite.Boundary, null, null) is not null)
                Interlocked.Increment(ref wins);
        });
        Eq(1, wins);
    }

    private static void PostSealCommit()
    {
        var session = new ScopeSafeSession();
        session.TrySeal(TerminalClassification.FailBoundary, FailureSite.Boundary, null, null);
        if (session.TryCommit(EventKind.MenuReady, null, out _)) throw new InvalidOperationException();
    }

    private static void ClosedSetStable()
    {
        var session = new ScopeSafeSession();
        session.TryCommit(EventKind.PatchActive, null, out _);
        session.TrySeal(TerminalClassification.FailBoundary, FailureSite.Boundary, null, null);
        var count = session.ClosedEventCountForTests;
        Parallel.For(0, 100, _ => session.TryCommit(EventKind.MenuReady, null, out var ignored));
        Eq(count, session.ClosedEventCountForTests);
    }

    private static void ArtifactTicket()
    {
        var session = new ScopeSafeSession();
        var ticket = session.TrySeal(TerminalClassification.FailBoundary, FailureSite.Boundary, null, null);
        var reads = 0;
        if (session.TryWriteClosedArtifact(new object(), _ => { reads++; return true; })) throw new InvalidOperationException();
        Eq(0, reads);
        if (!session.TryWriteClosedArtifact(ticket, _ => { reads++; return true; })) throw new InvalidOperationException();
        Eq(1, reads);
    }

    private static void SchemaWhitelist()
    {
        var actual = typeof(RuntimeArtifact).GetProperties().Select(x => x.Name).ToHashSet();
        var expected = new[]
        {
            "SchemaId", "ProbeVersion", "GameVersion", "GameCommit", "GameBranch", "GameDllLength",
            "GameDllSha256", "ProbeDllLength", "ProbeDllSha256", "TerminalClassification",
            "FirstFailureSite", "TerminalSequence", "TerminalElapsedMonotonicTicks", "Events", "Gate",
            "RunVerify", "PriorAttemptIntegrity", "ProtectionBefore", "ProtectionAfter", "Candidate", "Sentinel", "Audit",
            "Oracle", "Teardown", "Limitation"
        }.ToHashSet();
        if (!actual.SetEquals(expected)) throw new InvalidOperationException();
    }

    private static void NewIdentity()
    {
        Eq("spike-inv007a.direct-write.v3", ProbeConstants.SchemaId);
        Eq("0.3.0-direct-write-r3", ProbeConstants.ProbeVersion);
        if (!ProbeConstants.EvidenceRoot.EndsWith(@"evidence\spike-inv007a", StringComparison.Ordinal))
            throw new InvalidOperationException();
    }

    private static void IdentityNegative()
    {
        if (ProbeConstants.GameDllLength == 1 || ProbeConstants.GameDllSha256 == new string('0', 64) ||
            ProbeConstants.FixtureLength == 1 || ProbeConstants.FixtureSha256 == new string('0', 64))
            throw new InvalidOperationException();
    }

    private static void LogicalName()
    {
        Eq("modded/profile1/saves/progress.save", ProbeConstants.ProgressLogicalName);
        if (string.Equals(ProbeConstants.ProgressLogicalName, "modded/Profile1/saves/progress.save", StringComparison.Ordinal))
            throw new InvalidOperationException();
    }

    private static void PreconditionSites()
    {
        Eq(16, ContractRules.PreconditionSites.Count);
        if (ContractRules.PreconditionSites.Contains(FailureSite.RunCallback) ||
            ContractRules.PreconditionSites.Contains(FailureSite.Mutation) ||
            ContractRules.PreconditionSites.Contains(FailureSite.Write))
            throw new InvalidOperationException();
    }

    private static void MutationContract()
    {
        if (!ContractRules.MutationPassed(NewSentinel(update: 1, save: 0, write: 0)))
            throw new InvalidOperationException();
        if (ContractRules.MutationPassed(NewSentinel(update: 1, save: 0, write: 1)))
            throw new InvalidOperationException();
    }

    private static void SwallowedWrite()
    {
        if (ContractRules.WritePassed(NewSentinel(update: 1, save: 1, write: 0)))
            throw new InvalidOperationException();
    }

    private static void WriteContract()
    {
        if (!ContractRules.WritePassed(NewSentinel(update: 1, save: 1, write: 1)))
            throw new InvalidOperationException();
    }

    private static void DuplicateWrite()
    {
        if (ContractRules.WritePassed(NewSentinel(update: 1, save: 1, write: 2)))
            throw new InvalidOperationException();
    }

    private static SentinelEvidence NewSentinel(int update, int save, int write)
        => new(true, "A", "A", true, true, true, true, update, save, write,
            true, true, true, true);

    private static void Classifications()
    {
        foreach (var value in Enum.GetValues<TerminalClassification>())
            if (string.IsNullOrEmpty(value.ToString())) throw new InvalidOperationException();
    }

    private sealed class TimeoutFixture
    {
        internal readonly StartupGateArbiter Gate = new();
        internal int Terminals, Events, Artifacts;
        internal bool Timeout()
        {
            if (!Gate.TryTimeout()) return false;
            Interlocked.Increment(ref Terminals);
            Interlocked.Increment(ref Events);
            Interlocked.Increment(ref Artifacts);
            return true;
        }
    }

    private static void GateReadyBeforeTimeout()
    {
        var fixture = new TimeoutFixture();
        if (!fixture.Gate.TryCommitGateReady()) throw new InvalidOperationException();
        if (fixture.Timeout()) throw new InvalidOperationException();
        Eq(0, fixture.Terminals); Eq(0, fixture.Events); Eq(0, fixture.Artifacts);
        Eq(StartupGateState.GateReady, fixture.Gate.State);
    }

    private static void TimeoutBeforeGate()
    {
        var fixture = new TimeoutFixture();
        Parallel.For(0, 100, _ => fixture.Timeout());
        Eq(1, fixture.Terminals); Eq(1, fixture.Events); Eq(1, fixture.Artifacts);
        if (fixture.Gate.TryCommitGateReady()) throw new InvalidOperationException();
    }

    private static void GateReadyTimeoutRace()
    {
        for (var iteration = 0; iteration < 250; iteration++)
        {
            var fixture = new TimeoutFixture();
            var ready = false;
            Parallel.Invoke(
                () => ready = fixture.Gate.TryCommitGateReady(),
                () => fixture.Timeout());
            if (ready == (fixture.Terminals == 1)) throw new InvalidOperationException();
            if (ready && fixture.Timeout()) throw new InvalidOperationException();
            if (fixture.Terminals > 1 || fixture.Events > 1 || fixture.Artifacts > 1)
                throw new InvalidOperationException();
        }
    }

    private static void RunAcceptedBeforeTimeout()
    {
        var fixture = new TimeoutFixture();
        if (!fixture.Gate.TryCommitGateReady() || !fixture.Gate.TryAcceptRun())
            throw new InvalidOperationException();
        if (fixture.Timeout()) throw new InvalidOperationException();
        Eq(0, fixture.Terminals); Eq(0, fixture.Events); Eq(0, fixture.Artifacts);
        Eq(StartupGateState.RunAccepted, fixture.Gate.State);
    }

    private static void PriorArtifactDoesNotBlock()
    {
        if (!AttemptArtifactPolicy.CanStartR3(true, false, false)) throw new InvalidOperationException();
    }

    private static void R2ArtifactBlocks()
    {
        var path = Path.Combine(Path.GetTempPath(), "spike-inv007a-r2-policy-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, "original");
        try
        {
            if (AttemptArtifactPolicy.CanStartR3(true, true, false)) throw new InvalidOperationException();
            Eq("original", File.ReadAllText(path));
            if (AttemptArtifactPolicy.CanStartR3(true, false, true)) throw new InvalidOperationException();
            if (AttemptArtifactPolicy.CanStartR3(false, false, false)) throw new InvalidOperationException();
        }
        finally { File.Delete(path); }
    }

    private static void PriorEvidenceIdentity()
    {
        Exact(ProbeConstants.PriorArtifactFile, ProbeConstants.PriorArtifactLength,
            ProbeConstants.PriorArtifactSha256);
        var files = Directory.EnumerateFiles(ProbeConstants.PriorRecoveryRoot, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                Relative = Path.GetRelativePath(ProbeConstants.PriorRecoveryRoot, path).Replace('\\', '/')
            }).OrderBy(x => x.Relative, StringComparer.Ordinal).ToList();
        Eq(ProbeConstants.PriorRecoveryFileCount, files.Count);
        var lines = new System.Text.StringBuilder();
        foreach (var file in files)
        {
            var info = new FileInfo(file.Path);
            using var stream = File.OpenRead(file.Path);
            lines.Append(file.Relative).Append('\t').Append(info.Length).Append('\t')
                .Append(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream))).Append('\n');
        }
        var aggregate = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(lines.ToString())));
        Eq(ProbeConstants.PriorRecoveryAggregateSha256, aggregate);
    }

    private static void InstallScopeIdentity()
    {
        if (!InstallIdentityPolicy.CanReplace(2, InstallIdentityPolicy.R2DllLength,
                InstallIdentityPolicy.R2DllSha256, InstallIdentityPolicy.R2ManifestLength,
                InstallIdentityPolicy.R2ManifestSha256)) throw new InvalidOperationException();
        if (InstallIdentityPolicy.CanReplace(3, InstallIdentityPolicy.R2DllLength,
                InstallIdentityPolicy.R2DllSha256, InstallIdentityPolicy.R2ManifestLength,
                InstallIdentityPolicy.R2ManifestSha256)) throw new InvalidOperationException();
        if (InstallIdentityPolicy.CanReplace(2, InstallIdentityPolicy.R2DllLength + 1,
                InstallIdentityPolicy.R2DllSha256, InstallIdentityPolicy.R2ManifestLength,
                InstallIdentityPolicy.R2ManifestSha256)) throw new InvalidOperationException();
        if (InstallIdentityPolicy.CanReplace(2, InstallIdentityPolicy.R2DllLength,
                new string('0', 64), InstallIdentityPolicy.R2ManifestLength,
                InstallIdentityPolicy.R2ManifestSha256)) throw new InvalidOperationException();
    }

    private static (ProgressState State, ModelId Character, ModelId Encounter, ModelId Enemy) StatsState()
    {
        var state = ProgressState.CreateDefault();
        var character = new ModelId("character", "r3-character");
        var encounter = new ModelId("encounter", "r3-encounter");
        var enemy = new ModelId("enemy", "r3-enemy");
        state.GetOrCreateEncounterStats(encounter).FightStats.Add(
            new FightStats { Character = character, Wins = 1, Losses = 0 });
        state.GetOrCreateEnemyStats(enemy).FightStats.Add(
            new FightStats { Character = character, Wins = 2, Losses = 0 });
        return (state, character, encounter, enemy);
    }

    private static void OldSerializableAliases()
    {
        var x = StatsState();
        var shallow = x.State.ToSerializable();
        var discovered = new HashSet<string>(StringComparer.Ordinal);
        DetachedMutationOracle.ApplyExpected(shallow, x.Character, x.Encounter, [x.Enemy], discovered);
        Eq(2, x.State.GetOrCreateEncounterStats(x.Encounter).FightStats.Single().Wins);
        Eq(3, x.State.GetOrCreateEnemyStats(x.Enemy).FightStats.Single().Wins);
        if (DetachedMutationOracle.CountSharedMutableReferences(shallow, x.State) == 0)
            throw new InvalidOperationException();
    }

    private static SerializableProgress Detached(ProgressState state)
    {
        var parsed = JsonSerializationUtility.FromJson<SerializableProgress>(
            JsonSerializationUtility.ToJson(state.ToSerializable()));
        if (!parsed.Success || parsed.SaveData is null) throw new InvalidOperationException();
        return parsed.SaveData;
    }

    private static void JsonRoundTripDetaches()
    {
        var x = StatsState();
        Eq(0, DetachedMutationOracle.CountSharedMutableReferences(Detached(x.State), x.State));
    }

    private static void DetachedApplyLeavesBaselines()
    {
        var x = StatsState();
        var expected = Detached(x.State);
        var before = JsonSupport.Canonicalize(JsonSerializationUtility.ToJson(x.State.ToSerializable()));
        var discoveredBaseline = new HashSet<string>(["existing"], StringComparer.Ordinal);
        var discoveredExpected = new HashSet<string>(discoveredBaseline, StringComparer.Ordinal);
        DetachedMutationOracle.ApplyExpected(expected, x.Character, x.Encounter, [x.Enemy], discoveredExpected);
        var after = JsonSupport.Canonicalize(JsonSerializationUtility.ToJson(x.State.ToSerializable()));
        Eq(before, after);
        if (!discoveredBaseline.SetEquals(["existing"])) throw new InvalidOperationException();
    }

    private static void DetachedSharedCountZero()
    {
        var x = StatsState();
        var expected = Detached(x.State);
        Eq(0, DetachedMutationOracle.CountSharedMutableReferences(expected, x.State));
    }

    private static void EveryOccurrenceIncrements()
    {
        var character = new ModelId("character", "r3-character");
        var encounter = new ModelId("encounter", "r3-encounter");
        var enemyA = new ModelId("enemy", "r3-a");
        var enemyB = new ModelId("enemy", "r3-b");
        var progress = new SerializableProgress
        {
            EncounterStats = new List<EncounterStats>(),
            EnemyStats =
            [
                new EnemyStats { Id = enemyA, FightStats = [new FightStats { Character = character, Wins = 5 }] }
            ]
        };
        var discovered = new HashSet<string>(StringComparer.Ordinal);
        DetachedMutationOracle.ApplyExpected(progress, character, encounter, [enemyA, enemyA, enemyB], discovered);
        Eq(1, progress.EncounterStats.Single(x => x.Id.Equals(encounter)).FightStats.Single().Wins);
        Eq(7, progress.EnemyStats.Single(x => x.Id.Equals(enemyA)).FightStats.Single().Wins);
        Eq(1, progress.EnemyStats.Single(x => x.Id.Equals(enemyB)).FightStats.Single().Wins);
        if (!discovered.SetEquals([enemyB.ToString()])) throw new InvalidOperationException();
    }

    private static void OracleGatesFailClosed()
    {
        var audit = new AuditSnapshot(BoundaryFailure.None, false, false, false, false, false,
            0, 0, 0, false, false, false, false);
        if (DetachedMutationOracle.AdmissionPassed(false, 0, true, true, audit))
            throw new InvalidOperationException();
        if (DetachedMutationOracle.AdmissionPassed(true, 1, true, true, audit))
            throw new InvalidOperationException();
        Eq(0, audit.CandidateUpdateCount);
    }

    private static void Exact(string path, long length, string sha256)
    {
        var info = new FileInfo(path);
        using var stream = File.OpenRead(path);
        Eq(length, info.Length);
        Eq(sha256, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)));
    }

    private static void Eq<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException();
    }
}
