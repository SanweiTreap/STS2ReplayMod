using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpikeInv006B;

internal static class ProbeController
{
    private static readonly ScopeSafeSession Session = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static long _probeDllLength;
    private static string _probeDllSha256 = "";
    private static bool _identityValid;
    private static bool _patchActive;
    private static long _patchActiveSequence;
    private static int _graphicsEntries;
    private static long _firstGraphicsEntrySequence;
    private static int _graphicsNormalExits;
    private static int _graphicsActive;
    private static bool _graphicsFault;
    private static Task? _outerTask;
    private static int _outerIdentity;
    private static ObservedTaskStatus _outerStatus = ObservedTaskStatus.Null;
    private static Task? _startupTask;
    private static ObservedTaskStatus _startupStatus = ObservedTaskStatus.Null;
    private static NMainMenu? _menu;
    private static SaveManager? _originalManager;
    private static int _originalManagerIdentity;
    private static bool _menuReady;
    private static bool _gateReady;
    private static bool _verifyUsed;
    private static int _transactionOwner;
    private static GateSnapshot? _gateSnapshot;
    private static GateSnapshot? _verifySnapshot;
    private static ProtectionSnapshot? _protectionBefore;
    private static IReadOnlyList<ProtectedFile>? _protectionAfter;
    private static CandidateEvidence? _candidateEvidence;
    private static SentinelEvidence? _sentinelEvidence;
    private static AuditSnapshot? _auditEvidence;
    private static TeardownEvidence? _teardownEvidence;

    internal sealed record GraphicsCallToken(long EntrySequence);

    internal static bool IsObserverRunningFirst() => Session.IsObserverRunningFirst();

    internal static bool IsContinueVisibleAndEnabled()
    {
        var menu = _menu;
        if (menu is null || !GodotObject.IsInstanceValid(menu) || !menu.IsInsideTree()) return false;
        var button = menu.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/ContinueButton");
        if (button?.Visible != true) return false;
        var property = typeof(NClickableControl).GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? typeof(NClickableControl).GetProperty("Enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(button) as bool? ?? true;
    }

    internal static void PrepareIdentity()
    {
        try
        {
            var gameAssembly = typeof(NGame).Assembly.Location;
            var gameInfo = new FileInfo(gameAssembly);
            string gameDigest;
            using (var gameStream = File.OpenRead(gameAssembly))
                gameDigest = Convert.ToHexString(SHA256.HashData(gameStream));
            var probeAssembly = Assembly.GetExecutingAssembly().Location;
            var probeInfo = new FileInfo(probeAssembly);
            _probeDllLength = probeInfo.Length;
            using (var probeStream = File.OpenRead(probeAssembly))
                _probeDllSha256 = Convert.ToHexString(SHA256.HashData(probeStream));
            _identityValid = gameInfo.Length == ProbeConstants.GameDllLength
                && string.Equals(gameDigest, ProbeConstants.GameDllSha256, StringComparison.Ordinal);
            var gameDir = Directory.GetParent(Path.GetDirectoryName(gameAssembly)!)!.FullName;
            using var release = JsonDocument.Parse(File.ReadAllText(Path.Combine(gameDir, "release_info.json")));
            _identityValid &= release.RootElement.GetProperty("version").GetString() == ProbeConstants.GameVersion
                && release.RootElement.GetProperty("commit").GetString() == ProbeConstants.GameCommit
                && release.RootElement.GetProperty("branch").GetString() == ProbeConstants.GameBranch;
            var fixture = new FileInfo(ProbeConstants.FixturePath);
            using var fixtureStream = File.OpenRead(ProbeConstants.FixturePath);
            _identityValid &= fixture.Length == ProbeConstants.FixtureLength
                && Convert.ToHexString(SHA256.HashData(fixtureStream)) == ProbeConstants.FixtureSha256;
        }
        catch
        {
            _identityValid = false;
        }
    }

    internal static void Activate()
    {
        if (!Session.IsObserverRunningFirst()) return;
        lock (Session.SyncRoot)
        {
            if (!Session.IsObserverRunningFirst()) return;
            _patchActive = true;
            if (Session.TryCommit(EventKind.PatchActive, null, out var active))
                _patchActiveSequence = active!.Sequence;
        }
        if (!_identityValid)
        {
            Seal(TerminalClassification.BlockedIdentity, FailureSite.Identity);
            return;
        }
        _ = Task.Delay(TimeSpan.FromSeconds(ProbeConstants.MissingEventTimeoutSeconds)).ContinueWith(
            static _ => TimeoutCallback(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        GD.Print("[SPIKE-INV006B] Scope-safe outer-cloud timing probe active.");
    }

    internal static GraphicsCallToken? OnGraphicsEntry()
    {
        if (!Session.IsObserverRunningFirst()) return null;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst()) return null;
                _graphicsEntries++;
                _graphicsActive++;
                Session.TryCommit(EventKind.GraphicsEntry, null, out var entry);
                if (_firstGraphicsEntrySequence == 0) _firstGraphicsEntrySequence = entry!.Sequence;
                if (_graphicsEntries > 1 || _gateReady)
                {
                    Seal(TerminalClassification.StopMissingTimingGate, FailureSite.DuplicateGraphics);
                    return null;
                }
                return new GraphicsCallToken(entry!.Sequence);
            }
        }
        catch
        {
            Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GraphicsPrefix);
            return null;
        }
    }

    internal static void OnGraphicsNormalExit(GraphicsCallToken? token)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst()) return;
                if (token is null || _graphicsActive <= 0)
                {
                    Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GraphicsPostfix);
                    return;
                }
                _graphicsNormalExits++;
                Session.TryCommit(EventKind.GraphicsNormalExit, null, out _);
            }
            TryOpenGate();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GraphicsPostfix); }
    }

    internal static void OnGraphicsFinalizer(GraphicsCallToken? token, bool originalFaulted)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst()) return;
                if (token is not null && _graphicsActive > 0) _graphicsActive--;
                if (originalFaulted)
                {
                    _graphicsFault = true;
                    Session.TryCommit(EventKind.GraphicsFault, null, out _);
                    Seal(TerminalClassification.StopMissingTimingGate, FailureSite.GraphicsFinalizer);
                    return;
                }
            }
            TryOpenGate();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GraphicsFinalizer); }
    }

    internal static void OnOuterCloudReturned(Task result)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst()) return;
                if (_outerTask is not null)
                {
                    Seal(TerminalClassification.StopMissingTimingGate, FailureSite.DuplicateOuter);
                    return;
                }
                _outerTask = result;
                _outerIdentity = ScopeSafeSession.Identity(result);
                _outerStatus = ClassifyTask(result);
                Session.TryCommit(EventKind.OuterCloudReturned, _outerStatus, out _);
                result.ContinueWith(static completed => OuterTerminalContinuation(completed),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            TryOpenGate();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.OuterPostfix); }
    }

    private static void OuterTerminalContinuation(Task completed)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst()) return;
                if (!ReferenceEquals(completed, _outerTask))
                {
                    Seal(TerminalClassification.BlockedInstrumentation, FailureSite.OuterContinuation);
                    return;
                }
                _outerStatus = ClassifyTask(completed);
                Session.TryCommit(EventKind.OuterCloudTerminal, _outerStatus, out _);
            }
            TryOpenGate();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.OuterContinuation); }
    }

    internal static void OnMainMenuReady(NMainMenu menu)
    {
        if (!Session.IsObserverRunningFirst()) return;
        if (_verifyUsed)
        {
            lock (Session.SyncRoot) { if (!Session.IsObserverRunningFirst()) return; _menu = menu; }
            ProbeUi.AttachRecovery(menu);
            return;
        }
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst()) return;
                _menu = menu;
                _menuReady = true;
                _originalManager ??= SaveManager.Instance;
                _originalManagerIdentity = ScopeSafeSession.Identity(_originalManager);
                Session.TryCommit(EventKind.MenuReady, null, out _);
                _startupTask = NGame.Instance?.GameStartupComplete;
                if (_startupTask is null)
                {
                    Seal(TerminalClassification.BlockedInstrumentation, FailureSite.MenuCallback);
                    return;
                }
                _startupStatus = ClassifyTask(_startupTask);
                _startupTask.ContinueWith(static completed => StartupTerminalContinuation(completed),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            ProbeUi.Attach(menu);
            TryOpenGate();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.MenuCallback); }
    }

    private static void StartupTerminalContinuation(Task completed)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst()) return;
                if (!ReferenceEquals(completed, _startupTask))
                {
                    Seal(TerminalClassification.BlockedInstrumentation, FailureSite.StartupContinuation);
                    return;
                }
                _startupStatus = ClassifyTask(completed);
                Session.TryCommit(EventKind.StartupMarkerTerminal, _startupStatus, out _);
            }
            TryOpenGate();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.StartupContinuation); }
    }

    private static void TryOpenGate()
    {
        if (!Session.IsObserverRunningFirst()) return;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst() || _gateReady) return;
                var snapshot = CaptureGateSnapshot(false);
                var explicitEventsPresent = _graphicsEntries > 0 && _graphicsNormalExits > 0
                    && _outerTask is not null && IsTerminal(_outerStatus)
                    && IsTerminal(_startupStatus) && _menuReady;
                if (explicitEventsPresent && !snapshot.Conditions.PatchBeforeGraphics)
                {
                    Seal(TerminalClassification.StopMissingTimingGate, FailureSite.GraphicsPrefix);
                    return;
                }
                if (explicitEventsPresent && !snapshot.Conditions.ManagerIdentityStable)
                {
                    Seal(TerminalClassification.StopMissingTimingGate, FailureSite.ManagerIdentity);
                    return;
                }
                if (explicitEventsPresent && !snapshot.Conditions.RunStateNull)
                {
                    Seal(TerminalClassification.StopMissingTimingGate, FailureSite.RunState);
                    return;
                }
                if (!AllGateConditions(snapshot.Conditions)) return;
                if (!LoadedModIdentityValid()) { Seal(TerminalClassification.BlockedIdentity, FailureSite.Identity); return; }
                Session.TryCommit(EventKind.GateReady, null, out var gateEvent);
                _gateSnapshot = snapshot with { Sequence = gateEvent!.Sequence };
                _gateReady = true;
            }
            ProbeUi.QueueGateReady();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GateEvaluation); }
    }

    internal static void RunFromUi()
    {
        if (!Session.IsObserverRunningFirst()) return;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst() || _verifyUsed) return;
                if (!_gateReady || _gateSnapshot is null)
                {
                    Seal(TerminalClassification.StopMissingTimingGate, FailureSite.RunCallback);
                    return;
                }
                if (Interlocked.CompareExchange(ref _transactionOwner, 1, 0) != 0) return;
                _verifyUsed = true;
                var snapshot = CaptureGateSnapshot(true);
                Session.TryCommit(EventKind.RunAccepted, null, out var verifyEvent);
                _verifySnapshot = snapshot with { Sequence = verifyEvent!.Sequence };
                if (!AllGateConditions(_verifySnapshot.Conditions)
                    || !IsContinueVisibleAndEnabled()
                    || _verifySnapshot.OuterTaskRuntimeIdentity != _gateSnapshot.OuterTaskRuntimeIdentity
                    || _verifySnapshot.OriginalManagerRuntimeIdentity != _gateSnapshot.OriginalManagerRuntimeIdentity
                    || _verifySnapshot.GraphicsEntryCount != _gateSnapshot.GraphicsEntryCount
                    || _verifySnapshot.GraphicsNormalExitCount != _gateSnapshot.GraphicsNormalExitCount)
                {
                    Seal(TerminalClassification.StopMissingTimingGate, FailureSite.RunCallback);
                    return;
                }
            }
            ProbeUi.DisableActions();
            DirectWriteTransaction.Start(_originalManager!);
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.RunCallback); }
    }

    internal static bool RevalidateTimingGate(SaveManager original)
    {
        if (!Session.IsObserverRunningFirst()) return false;
        lock (Session.SyncRoot)
        {
            if (!Session.IsObserverRunningFirst() || !_verifyUsed || _gateSnapshot is null || !ReferenceEquals(original, _originalManager) || !LoadedModIdentityValid() || !IsContinueVisibleAndEnabled()) return false;
            var snapshot = CaptureGateSnapshot(true);
            return AllGateConditions(snapshot.Conditions) && snapshot.OuterTaskRuntimeIdentity == _gateSnapshot.OuterTaskRuntimeIdentity
                && snapshot.OriginalManagerRuntimeIdentity == _gateSnapshot.OriginalManagerRuntimeIdentity
                && snapshot.GraphicsEntryCount == _gateSnapshot.GraphicsEntryCount && snapshot.GraphicsNormalExitCount == _gateSnapshot.GraphicsNormalExitCount;
        }
    }

    private static bool LoadedModIdentityValid()
    {
        var ids = ModManager.GetLoadedMods().Where(x => x.manifest is not null).Select(x => x.manifest!.id ?? "").ToList();
        return ids.Count == 1 && string.Equals(ids[0], "SPIKEINV006B", StringComparison.Ordinal);
    }

    internal static void CommitTransactionEvent(EventKind kind)
    {
        if (!Session.IsObserverRunningFirst()) return;
        lock (Session.SyncRoot) { if (!Session.IsObserverRunningFirst()) return; Session.TryCommit(kind, null, out _); }
    }

    internal static void Complete(TerminalClassification classification, FailureSite site, ProtectionSnapshot? before,
        IReadOnlyList<ProtectedFile>? after, CandidateEvidence? candidate, SentinelEvidence? sentinel,
        AuditSnapshot audit, TeardownEvidence teardown)
    {
        if (!Session.IsObserverRunningFirst()) return;
        _protectionBefore = before; _protectionAfter = after; _candidateEvidence = candidate;
        _sentinelEvidence = sentinel; _auditEvidence = audit; _teardownEvidence = teardown;
        Seal(classification, site);
    }

    private static GateSnapshot CaptureGateSnapshot(bool verifying)
    {
        var manager = SaveManager.Instance;
        var managerStable = _originalManager is not null && ReferenceEquals(manager, _originalManager);
        var currentRun = _originalManager?.CurrentRunSaveTask;
        var currentRunStatus = ClassifyTask(currentRun);
        var runStateNull = RunManager.Instance?.DebugOnlyGetState() is null;
        var noPostGateLifecycle = !verifying || (_graphicsEntries == _gateSnapshot!.GraphicsEntryCount
            && _graphicsNormalExits == _gateSnapshot.GraphicsNormalExitCount && _outerTask is not null
            && ScopeSafeSession.Identity(_outerTask) == _gateSnapshot.OuterTaskRuntimeIdentity);
        var conditions = new GateConditions(
            _patchActive && _patchActiveSequence > 0 && _firstGraphicsEntrySequence > _patchActiveSequence,
            _graphicsEntries == 1 && _graphicsNormalExits == 1 && _graphicsActive == 0 && !_graphicsFault,
            _outerTask is not null,
            IsTerminal(_outerStatus),
            IsTerminal(_startupStatus),
            _menuReady && _menu is not null,
            managerStable && ScopeSafeSession.Identity(manager) == _originalManagerIdentity,
            runStateNull,
            currentRun is null || currentRun.IsCompleted,
            Session.Classification == TerminalClassification.Running,
            noPostGateLifecycle);
        return new GateSnapshot(0, _originalManagerIdentity, _outerIdentity, _outerStatus,
            _startupStatus, currentRunStatus, runStateNull, _graphicsEntries,
            _graphicsNormalExits, _graphicsActive, conditions);
    }

    private static bool AllGateConditions(GateConditions value)
        => value.PatchBeforeGraphics && value.GraphicsNormalAndIdle && value.ExactlyOneOuterTask
           && value.OuterTaskTerminal && value.StartupMarkerTerminal && value.MenuReady
           && value.ManagerIdentityStable && value.RunStateNull && value.CurrentRunTaskNullOrTerminal
           && value.NoTerminalFailure && value.NoPostGateLifecycle;

    private static void TimeoutCallback()
    {
        if (!Session.IsObserverRunningFirst()) return;
        lock (Session.SyncRoot)
        {
            if (!Session.IsObserverRunningFirst()) return;
            if (_verifyUsed) return;
            if (_outerTask is null || !IsTerminal(_outerStatus))
                Seal(TerminalClassification.StopMissingTimingGate, FailureSite.TimeoutCallback);
            else if (_originalManager?.CurrentRunSaveTask is { IsCompleted: false })
                Seal(TerminalClassification.StopMissingTimingGate, FailureSite.CurrentRunTask);
            else
                Seal(TerminalClassification.StopMissingTimingGate, FailureSite.TimeoutCallback);
        }
    }

    private static void Seal(TerminalClassification classification, FailureSite site)
    {
        var ticket = Session.TrySeal(classification, site, _gateSnapshot, _verifySnapshot);
        if (ticket is null) return;
        var written = Session.TryWriteClosedArtifact(ticket, WriteClosedArtifact);
        try { ProbeUi.ShowTerminalFixed(written ? ToExternalClassification(classification) : "BLOCKED_ARTIFACT_WRITE"); }
        catch { }
    }

    private static bool WriteClosedArtifact(ClosedSnapshot closed)
    {
        try
        {
            var artifact = new RuntimeArtifact(ProbeConstants.SchemaId, ProbeConstants.ProbeVersion,
                ProbeConstants.GameVersion, ProbeConstants.GameCommit, ProbeConstants.GameBranch, ProbeConstants.GameDllLength,
                ProbeConstants.GameDllSha256, _probeDllLength, _probeDllSha256,
                ToExternalClassification(closed.TerminalClassification), closed.FirstFailureSite.ToString(), closed.TerminalSequence,
                closed.TerminalElapsedMonotonicTicks, closed.Events, closed.Gate, closed.Verify,
                _protectionBefore, _protectionAfter, _candidateEvidence, _sentinelEvidence, _auditEvidence, _teardownEvidence,
                "Only the two direct production progress methods in this fixed live-seeded isolated replay session; no replay events, natural victory, achievement helper, fault/restart, reconstruction, replay fidelity, or complete session isolation were tested.");
            var destination = ProbeConstants.ArtifactFile;
            var directory = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(directory);
            if (File.Exists(destination)) return false;
            var temporary = destination + ".tmp";
            if (File.Exists(temporary)) return false;
            File.WriteAllText(temporary, JsonSerializer.Serialize(artifact, JsonOptions), new UTF8Encoding(false));
            File.Move(temporary, destination);
            return true;
        }
        catch { return false; }
    }

    private static ObservedTaskStatus ClassifyTask(Task? task)
    {
        if (task is null) return ObservedTaskStatus.Null;
        if (!task.IsCompleted) return ObservedTaskStatus.Pending;
        if (task.IsCanceled) return ObservedTaskStatus.Canceled;
        if (task.IsFaulted) return ObservedTaskStatus.Faulted;
        return ObservedTaskStatus.RanToCompletion;
    }

    private static bool IsTerminal(ObservedTaskStatus status)
        => status is ObservedTaskStatus.RanToCompletion or ObservedTaskStatus.Faulted or ObservedTaskStatus.Canceled;

    private static string ToExternalClassification(TerminalClassification value) => value switch
    {
        TerminalClassification.PassDirectWriteIsolated => "PASS_DIRECT_WRITE_ISOLATED",
        TerminalClassification.FailPreoverrideEquivalence => "FAIL_PREOVERRIDE_EQUIVALENCE",
        TerminalClassification.FailBoundary => "FAIL_BOUNDARY",
        TerminalClassification.FailMutationOrWrite => "FAIL_MUTATION_OR_WRITE",
        TerminalClassification.FailTeardown => "FAIL_TEARDOWN",
        TerminalClassification.StopMissingTimingGate => "STOP_MISSING_TIMING_GATE",
        TerminalClassification.FailSafety => "FAIL_SAFETY",
        TerminalClassification.BlockedIdentity => "BLOCKED_IDENTITY",
        TerminalClassification.BlockedPrecondition => "BLOCKED_PRECONDITION",
        TerminalClassification.BlockedArtifactWrite => "BLOCKED_ARTIFACT_WRITE",
        _ => "BLOCKED_INSTRUMENTATION"
    };
}
