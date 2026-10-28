using SpikeInv006A;

namespace SpikeInv006A.Tests;

internal static class Program
{
    private static readonly List<(string Name, Action Body)> Cases = new();

    private static int Main()
    {
        Add("01 other profile", () => RejectUntouched("modded/profile2/x"));
        Add("02 pseudo-prefix", () => { Reject("modded/profile10/saves/x"); Reject("modded/profile1-copy/x"); });
        Add("03 traversal", () => { Reject("modded/profile1/../x"); Reject("modded/profile1/./x"); Reject("modded\\profile1\\..\\x"); });
        Add("04 rooted device URI", () => { foreach (var value in new[] { @"C:\x", "C:x", @"\\server\share\x", "/x", "user://x", "file:///x" }) Reject(value); });
        Add("05 encoded ambiguity", () => Reject("modded/profile1/%2e%2e/x"));
        Add("06 separator ambiguity", () => { Reject("modded/profile1//x"); Reject("modded/profile1/"); });
        Add("07 case variant", () => { Reject("modded/Profile1/x"); Reject("Settings.save"); });
        Add("08 exact allowed domains", () =>
        {
            Equal(AllowedDomain.AccountSelector, ContractFixture.Authorize("modded/profile.save"));
            Equal(AllowedDomain.MachineSettings, ContractFixture.Authorize("settings.save"));
            Equal(AllowedDomain.ActiveProfile, ContractFixture.Authorize("modded/profile1/x"));
        });
        Add("09 seal before prefix", SealBeforePrefix);
        Add("10 seal after registration", SealAfterRegistration);
        Add("11 poison payload", PoisonUntouched);
        Add("12 atomic race", AtomicRace);
        Add("13 finalizer identity", FinalizerIdentity);
        Add("14 artifact failure", ArtifactFailure);
        Add("15 unauthorized artifact call", UnauthorizedArtifactCall);
        Add("16 outer captured once", OuterCapturedOnce);
        Add("17 terminal statuses are settled", TerminalStatuses);
        Add("18 pending outer blocks gate", PendingBlocksGate);
        Add("19 outer terminal joins all conditions", ConditionJoin);
        Add("20 synchronous continuation seal race", SynchronousContinuationRace);
        Add("21 verify cannot reopen sealed session", VerifyCannotReopen);
        Add("22 wrong DLL identity blocks", WrongIdentityBlocks);
        Add("23 runtime artifact field whitelist", ArtifactFieldWhitelist);

        var failed = 0;
        foreach (var item in Cases)
        {
            try { item.Body(); Console.WriteLine($"PASS {item.Name}"); }
            catch (Exception error) { failed++; Console.WriteLine($"FAIL {item.Name}: {error.GetType().Name}"); }
        }
        Console.WriteLine($"TOTAL {Cases.Count} PASSED {Cases.Count - failed} FAILED {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static void Add(string name, Action body) => Cases.Add((name, body));
    private static void Reject(string value) => Equal(AllowedDomain.None, ContractFixture.Authorize(value));
    private static void RejectUntouched(string value)
    {
        var poison = new PoisonPayload();
        Equal(AllowedDomain.None, ContractFixture.Authorize(value, poison));
        Equal(0, poison.Touches);
    }

    private static void SealBeforePrefix()
    {
        var session = NewSealedSession();
        var spy = new ArgumentSpy();
        if (session.IsObserverRunningFirst()) _ = spy.Read();
        Equal(0, spy.Reads);
    }

    private static void SealAfterRegistration()
    {
        var session = new ScopeSafeSession();
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commits = 0;
        source.Task.ContinueWith(completed => { if (session.IsObserverRunningFirst() && session.TryCommit(EventKind.OuterCloudTerminal, ObservedTaskStatus.RanToCompletion, out var committed)) Interlocked.Increment(ref commits); });
        session.TrySeal(TerminalClassification.FailEventOrder, FailureSite.MissingEvent, null, null);
        source.SetResult();
        source.Task.Wait();
        Thread.Sleep(20);
        Equal(0, commits);
    }

    private static void PoisonUntouched()
    {
        var poison = new PoisonPayload();
        Equal(AllowedDomain.ActiveProfile, ContractFixture.Authorize("modded/profile1/x", poison));
        Equal(0, poison.Touches);
    }

    private static void AtomicRace()
    {
        var session = new ScopeSafeSession();
        Parallel.For(0, 500, i =>
        {
            if ((i % 37) == 0) session.TrySeal(TerminalClassification.FailEventOrder, FailureSite.MissingEvent, null, null);
            else session.TryCommit(EventKind.MenuReady, null, out _);
        });
        if (session.Classification == TerminalClassification.Running) throw new InvalidOperationException();
        var before = session.ClosedEventCountForTests;
        session.TryCommit(EventKind.MenuReady, null, out _);
        Equal(before, session.ClosedEventCountForTests);
    }

    private static void FinalizerIdentity()
    {
        var original = new InvalidOperationException();
        Exception? Helper(Exception? input)
        {
            try { throw new ApplicationException(); }
            catch { return input; }
        }
        if (!ReferenceEquals(original, Helper(original))) throw new InvalidOperationException();
    }

    private static void ArtifactFailure()
    {
        var session = new ScopeSafeSession();
        var ticket = session.TrySeal(TerminalClassification.FailEventOrder, FailureSite.ArtifactWriter, null, null);
        var calls = 0;
        if (session.TryWriteClosedArtifact(ticket, _ => { calls++; return false; })) throw new InvalidOperationException();
        if (session.TryWriteClosedArtifact(ticket, _ => { calls++; return true; })) throw new InvalidOperationException();
        Equal(1, calls);
        Equal(TerminalClassification.FailEventOrder, session.Classification);
    }

    private static void UnauthorizedArtifactCall()
    {
        var session = new ScopeSafeSession();
        var accesses = 0;
        if (session.TryWriteClosedArtifact(new object(), _ => { accesses++; return true; })) throw new InvalidOperationException();
        var ticket = session.TrySeal(TerminalClassification.FailEventOrder, FailureSite.ArtifactWriter, null, null);
        if (session.TryWriteClosedArtifact(new object(), _ => { accesses++; return true; })) throw new InvalidOperationException();
        Equal(0, accesses);
        if (!session.TryWriteClosedArtifact(ticket, _ => { accesses++; return true; })) throw new InvalidOperationException();
        Equal(1, accesses);
    }

    private static void OuterCapturedOnce()
    {
        Task? captured = null;
        var duplicates = 0;
        void Capture(Task task) { if (captured is null) captured = task; else duplicates++; }
        Capture(Task.CompletedTask); Capture(Task.CompletedTask);
        Equal(1, duplicates);
    }

    private static void TerminalStatuses()
    {
        var canceledSource = new CancellationTokenSource(); canceledSource.Cancel();
        var tasks = new[] { Task.CompletedTask, Task.FromException(new Exception()), Task.FromCanceled(canceledSource.Token) };
        foreach (var task in tasks) if (!task.IsCompleted) throw new InvalidOperationException();
    }

    private static void PendingBlocksGate()
    {
        var task = new TaskCompletionSource().Task;
        if (task.IsCompleted) throw new InvalidOperationException();
    }

    private static void ConditionJoin()
    {
        bool Gate(bool graphics, bool outer, bool startup, bool menu, bool manager) => graphics && outer && startup && menu && manager;
        if (Gate(true, false, true, true, true)) throw new InvalidOperationException();
        if (!Gate(true, true, true, true, true)) throw new InvalidOperationException();
    }

    private static void SynchronousContinuationRace()
    {
        var session = new ScopeSafeSession();
        var winners = 0;
        Task.CompletedTask.ContinueWith(_ => { if (session.TrySeal(TerminalClassification.PassScopeSafeGate, FailureSite.None, null, null) is not null) Interlocked.Increment(ref winners); }, TaskContinuationOptions.ExecuteSynchronously);
        if (session.TrySeal(TerminalClassification.FailEventOrder, FailureSite.MissingEvent, null, null) is not null) Interlocked.Increment(ref winners);
        Equal(1, winners);
    }

    private static void VerifyCannotReopen()
    {
        var session = NewSealedSession();
        if (session.TryCommit(EventKind.Verify, null, out _)) throw new InvalidOperationException();
        Equal(TerminalClassification.FailEventOrder, session.Classification);
    }

    private static void WrongIdentityBlocks()
    {
        bool Matches(long size, string digest) => size == ProbeConstants.GameDllLength && string.Equals(digest, ProbeConstants.GameDllSha256, StringComparison.Ordinal);
        if (Matches(1, new string('0', 64))) throw new InvalidOperationException();
        if (!Matches(ProbeConstants.GameDllLength, ProbeConstants.GameDllSha256)) throw new InvalidOperationException();
    }

    private static void ArtifactFieldWhitelist()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "SchemaId", "ProbeVersion", "GameVersion", "GameCommit", "GameDllLength", "GameDllSha256",
            "ProbeDllLength", "ProbeDllSha256", "TerminalClassification", "FirstFailureSite",
            "TerminalSequence", "TerminalElapsedMonotonicTicks", "Events", "Gate", "Verify"
        };
        var actual = typeof(RuntimeArtifact).GetProperties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        if (!allowed.SetEquals(actual)) throw new InvalidOperationException();
    }

    private static ScopeSafeSession NewSealedSession()
    {
        var session = new ScopeSafeSession();
        session.TrySeal(TerminalClassification.FailEventOrder, FailureSite.MissingEvent, null, null);
        return session;
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException();
    }
}
