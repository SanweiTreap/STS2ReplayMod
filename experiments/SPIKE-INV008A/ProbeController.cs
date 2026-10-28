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

namespace SpikeInv008A;

internal static class ProbeController
{
    private static readonly ScopeSafeSession Session = new();
    private static readonly StartupGateArbiter StartupGate = new();
    private static readonly PostLoaderModSetGate PostLoaderModSet = new();
    private static readonly RestoredMenuObserver RestoredMenu = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    private static long _probeDllLength;
    private static string _probeDllSha256 = "";
    private static bool _identityValid, _patchActive, _graphicsFault, _menuReady, _gateReady, _priorGateQueued, _verifyUsed;
    private static long _patchActiveSequence, _firstGraphicsEntrySequence;
    private static int _graphicsEntries, _graphicsNormalExits, _graphicsActive, _outerIdentity, _originalManagerIdentity, _transactionOwner;
    private static Task? _outerTask, _startupTask;
    private static ObservedTaskStatus _outerStatus, _startupStatus;
    private static NMainMenu? _menu;
    private static SaveManager? _originalManager;
    private static CancellationTokenSource? _startupTimeout;
    private static GateSnapshot? _gateSnapshot, _verifySnapshot;
    private static PhaseDecision _decision = new(ProbePhase.Invalid, "", null, "not initialized");
    private static PriorIntegrityResult? _priorResult;
    private static PriorIntegrityEvidence? _priorEvidence;
    private static ContinueEvidence? _continueBefore;
    private static IReadOnlyList<LobbyObservation>? _lobbyObservations;
    private static LobbyOracleEvidence? _lobbyOracle;
    private static SceneEvidence? _scene;
    private static ProtectionSnapshot? _protectionBefore;
    private static IReadOnlyList<ProtectedFile>? _protectionAfter;
    private static string? _recoveryRoot;
    private static CandidateEvidence? _candidate;
    private static FaultEvidence? _fault;
    private static AuditSnapshot? _audit;
    private static TeardownEvidence? _teardown;
    private static RestartEvidence? _restart;
    private static string? _previousArtifactSha256;
    private static string _attemptToken = "startup";

    internal sealed record GraphicsCallToken(long EntrySequence);
    internal static SaveManager? OriginalManager => _originalManager;
    internal static bool IsObserverRunningFirst() => Session.IsObserverRunningFirst();

    internal static void PrepareIdentity()
    {
        try
        {
            var gameAssembly = typeof(NGame).Assembly.Location;
            var gameInfo = new FileInfo(gameAssembly);
            using var gameStream = File.OpenRead(gameAssembly);
            var gameDigest = Convert.ToHexString(SHA256.HashData(gameStream));
            var probeAssembly = Assembly.GetExecutingAssembly().Location;
            var probeInfo = new FileInfo(probeAssembly); _probeDllLength = probeInfo.Length;
            using var probeStream = File.OpenRead(probeAssembly); _probeDllSha256 = Convert.ToHexString(SHA256.HashData(probeStream));
            var gameDir = Directory.GetParent(Path.GetDirectoryName(gameAssembly)!)!.FullName;
            using var release = JsonDocument.Parse(File.ReadAllText(Path.Combine(gameDir, "release_info.json")));
            var fixture = new FileInfo(ProbeConstants.FixturePath);
            using var fixtureStream = File.OpenRead(ProbeConstants.FixturePath);
            _identityValid = gameInfo.Length == ProbeConstants.GameDllLength && gameDigest == ProbeConstants.GameDllSha256 &&
                release.RootElement.GetProperty("version").GetString() == ProbeConstants.GameVersion &&
                release.RootElement.GetProperty("commit").GetString() == ProbeConstants.GameCommit &&
                release.RootElement.GetProperty("branch").GetString() == ProbeConstants.GameBranch &&
                fixture.Length == ProbeConstants.FixtureLength && Convert.ToHexString(SHA256.HashData(fixtureStream)) == ProbeConstants.FixtureSha256;
        }
        catch { _identityValid = false; }
    }

    internal static void Activate()
    {
        if (!Session.IsObserverRunningFirst()) return;
        lock (Session.SyncRoot)
        {
            _patchActive = true;
            if (Session.TryCommit(EventKind.PatchActive, null, out var active)) _patchActiveSequence = active!.Sequence;
        }
        _decision = PhasePolicy.Resolve(ProbeConstants.EvidenceRoot);
        if (!_identityValid) { Seal(TerminalClassification.BlockedIdentity, FailureSite.Identity); return; }
        if (_decision.Phase == ProbePhase.Invalid)
        {
            var prior = _decision.Error?.StartsWith("prior attempt unresolved:", StringComparison.Ordinal) == true;
            Seal(prior ? TerminalClassification.BlockedPriorAttemptUnresolved : TerminalClassification.BlockedPhaseOrder,
                prior ? FailureSite.PriorAttempt : FailureSite.PhaseOrder);
            return;
        }
        if (_decision.Phase == ProbePhase.Complete) { GD.Print("[SPIKE-INV008A] Four-phase matrix already complete; no probe action enabled."); return; }
        if (File.Exists(_decision.ArtifactPath) || File.Exists(_decision.ArtifactPath + ".tmp")) { Seal(TerminalClassification.BlockedArtifactWrite, FailureSite.ArtifactWriter); return; }
        _startupTimeout = new();
        _ = Task.Delay(TimeSpan.FromSeconds(ProbeConstants.MissingEventTimeoutSeconds), _startupTimeout.Token).ContinueWith(
            static task => { if (task.Status == TaskStatus.RanToCompletion) TimeoutCallback(); }, CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        GD.Print($"[SPIKE-INV008A] Active phase {_decision.Phase}.");
    }

    internal static GraphicsCallToken? OnGraphicsEntry()
    {
        if (!Session.IsObserverRunningFirst()) return null;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!Session.IsObserverRunningFirst()) return null;
                _graphicsEntries++; _graphicsActive++;
                Session.TryCommit(EventKind.GraphicsEntry, null, out var entry);
                if (_firstGraphicsEntrySequence == 0) _firstGraphicsEntrySequence = entry!.Sequence;
                if (_graphicsEntries > 1 || _gateReady) { Seal(TerminalClassification.BlockedPrecondition, FailureSite.DuplicateGraphics); return null; }
                return new(entry!.Sequence);
            }
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GraphicsPrefix); return null; }
    }
    internal static void OnGraphicsNormalExit(GraphicsCallToken? token)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try { lock (Session.SyncRoot) { if (!Session.IsObserverRunningFirst()) return; if (token is null || _graphicsActive <= 0) { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GraphicsPostfix); return; } _graphicsNormalExits++; Session.TryCommit(EventKind.GraphicsNormalExit, null, out _); } TryOpenGate(); }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GraphicsPostfix); }
    }
    internal static void OnGraphicsFinalizer(GraphicsCallToken? token, bool faulted)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try { lock (Session.SyncRoot) { if (!Session.IsObserverRunningFirst()) return; if (token is not null && _graphicsActive > 0) _graphicsActive--; if (faulted) { _graphicsFault = true; Session.TryCommit(EventKind.GraphicsFault, null, out _); Seal(TerminalClassification.BlockedPrecondition, FailureSite.GraphicsFinalizer); return; } } TryOpenGate(); }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.GraphicsFinalizer); }
    }
    internal static void OnOuterCloudReturned(Task task)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try { lock (Session.SyncRoot) { if (_outerTask is not null) { Seal(TerminalClassification.BlockedPrecondition, FailureSite.DuplicateOuter); return; } _outerTask = task; _outerIdentity = ScopeSafeSession.Identity(task); _outerStatus = Classify(task); Session.TryCommit(EventKind.OuterCloudReturned, _outerStatus, out _); task.ContinueWith(static value => OuterTerminal(value), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default); } TryOpenGate(); }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.OuterPostfix); }
    }
    private static void OuterTerminal(Task task)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try { lock (Session.SyncRoot) { if (!ReferenceEquals(task, _outerTask)) { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.OuterContinuation); return; } _outerStatus = Classify(task); Session.TryCommit(EventKind.OuterCloudTerminal, _outerStatus, out _); } TryOpenGate(); }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.OuterContinuation); }
    }
    internal static void OnMainMenuReadyPrefix(NMainMenu menu)
    {
        if (!Session.IsObserverRunningFirst() || !OwnerFaultTransaction.Active) return;
        RestoredMenu.RegisterCandidate(menu);
    }
    internal static void OnMainMenuReady(NMainMenu menu)
    {
        if (!Session.IsObserverRunningFirst()) return;
        if (!EnsurePostLoaderConfiguration()) return;
        if (OwnerFaultTransaction.Active)
        {
            if (RestoredMenu.ObserveReady(menu)) { _menu = menu; ProbeUi.AttachRecovery(menu); }
            return;
        }
        try
        {
            lock (Session.SyncRoot)
            {
                _menu = menu; _menuReady = true; _originalManager ??= SaveManager.Instance;
                _originalManagerIdentity = ScopeSafeSession.Identity(_originalManager); Session.TryCommit(EventKind.MenuReady, null, out _);
                _startupTask = NGame.Instance?.GameStartupComplete;
                if (_startupTask is null) { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.MenuCallback); return; }
                _startupStatus = Classify(_startupTask);
                _startupTask.ContinueWith(static value => StartupTerminal(value), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            ProbeUi.Attach(menu, _decision.Phase); TryOpenGate();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.MenuCallback); }
    }
    private static bool EnsurePostLoaderConfiguration()
    {
        if (PostLoaderModSet.Passed) return true;
        try
        {
            var precheck = PostLoaderModSet.EvaluateAtPostLoader(GetLoadedModIds);
            if (precheck is null || precheck.Passed) return true;
            SealConfigurationPrecheck(precheck);
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.ConfigurationPrecheck); }
        return false;
    }
    internal static void OnRefreshButtons(NMainMenu menu)
    {
        if (!Session.IsObserverRunningFirst() || !OwnerFaultTransaction.Active) return;
        if (RestoredMenu.ObserveRefreshButtons(menu)) CommitTransactionEvent(EventKind.RefreshButtons);
    }
    private static void StartupTerminal(Task task)
    {
        if (!Session.IsObserverRunningFirst()) return;
        try { lock (Session.SyncRoot) { if (!ReferenceEquals(task, _startupTask)) { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.StartupContinuation); return; } _startupStatus = Classify(task); Session.TryCommit(EventKind.StartupMarkerTerminal, _startupStatus, out _); } TryOpenGate(); }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.StartupContinuation); }
    }

    private static void TryOpenGate()
    {
        if (!Session.IsObserverRunningFirst()) return;
        var queue = false;
        lock (Session.SyncRoot)
        {
            if (_gateReady || _priorGateQueued) return;
            var snapshot = CaptureGate(false);
            if (!All(snapshot.Conditions)) return;
            if (!LoadedModIdentityValid()) { Seal(TerminalClassification.BlockedIdentity, FailureSite.Identity); return; }
            _priorGateQueued = true; queue = true;
        }
        if (queue) Callable.From(RunPriorGate).CallDeferred();
    }
    private static void RunPriorGate()
    {
        if (!Session.IsObserverRunningFirst() || _originalManager is null) return;
        try
        {
            _priorResult = _decision.Phase == ProbePhase.SetupOwnerFault
                ? PriorIntegrity.VerifyR6(_originalManager, _originalManagerIdentity, CaptureContinueEvidence)
                : PriorIntegrity.VerifyPreviousPhase(_decision.PreviousArtifact!, _originalManager, _originalManagerIdentity,
                    CaptureContinueEvidence, PhasePolicy.IsRestart(_decision.Phase));
            _priorEvidence = _priorResult.Evidence;
            _continueBefore = _priorResult.ContinueBefore;
            if (!_priorResult.IdentityValid) { Seal(TerminalClassification.BlockedIdentity, FailureSite.PriorIntegrity); return; }
            if (!_priorResult.Passed) { Seal(TerminalClassification.BlockedPriorIntegrity,
                _priorResult.Evidence.ContinueFailure == ContinueFailure.None ? FailureSite.PriorIntegrity : ContinueFailureSite(_priorResult.Evidence.ContinueFailure)); return; }
            if (_decision.PreviousArtifact is not null) _previousArtifactSha256 = PhasePolicy.Sha256(_decision.PreviousArtifact);
            lock (Session.SyncRoot)
            {
                var snapshot = CaptureGate(false);
                if (!PriorGateReachability.CanReachGateReady(_priorResult, snapshot.Conditions, LoadedModIdentityValid())) { Seal(TerminalClassification.BlockedPrecondition, FailureSite.GateEvaluation); return; }
                if (!StartupGate.TryCommitGateReady()) return;
                Session.TryCommit(EventKind.PriorIntegrityVerified, null, out _);
                Session.TryCommit(EventKind.GateReady, null, out var gate); _gateSnapshot = snapshot with { Sequence = gate!.Sequence };
                _gateReady = true; _startupTimeout?.Cancel();
            }
            ProbeUi.QueueGateReady(_decision.Phase);
            if (PhasePolicy.IsRestart(_decision.Phase)) Callable.From(RunRestartFromReady).CallDeferred();
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.PriorIntegrity); }
    }

    internal static void RunFromUi()
    {
        if (!Session.IsObserverRunningFirst() || !PhasePolicy.IsFault(_decision.Phase)) return;
        try
        {
            lock (Session.SyncRoot)
            {
                if (!_gateReady || _gateSnapshot is null || Interlocked.CompareExchange(ref _transactionOwner, 1, 0) != 0 || !StartupGate.TryAcceptRun()) return;
                _verifyUsed = true; var snapshot = CaptureGate(true); Session.TryCommit(EventKind.RunAccepted, null, out var verify); _verifySnapshot = snapshot with { Sequence = verify!.Sequence };
                if (!All(_verifySnapshot.Conditions) || _verifySnapshot.OuterTaskRuntimeIdentity != _gateSnapshot.OuterTaskRuntimeIdentity || _verifySnapshot.OriginalManagerRuntimeIdentity != _gateSnapshot.OriginalManagerRuntimeIdentity) { Seal(TerminalClassification.BlockedPrecondition, FailureSite.RunCallback); return; }
            }
            var current = CaptureContinueEvidence();
            var continueFailure = PriorIntegrity.CompareContinue(_continueBefore, current);
            if (continueFailure != ContinueFailure.None) { Seal(TerminalClassification.BlockedPrecondition, ContinueFailureSite(continueFailure)); return; }
            ProbeUi.DisableActions(); OwnerFaultTransaction.Start(_decision.Phase, _originalManager!, _continueBefore!);
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.RunCallback); }
    }
    private static void RunRestartFromReady()
    {
        if (!Session.IsObserverRunningFirst() || !PhasePolicy.IsRestart(_decision.Phase) || _priorResult is null || _originalManager is null) return;
        try
        {
            if (Interlocked.CompareExchange(ref _transactionOwner, 1, 0) != 0 || !StartupGate.TryAcceptRun()) return;
            _verifyUsed = true; _attemptToken = Guid.NewGuid().ToString("N"); Session.TryCommit(EventKind.RestartAccepted, null, out _); ProbeUi.DisableActions();
            var captured = ProtectionScope.CaptureCurrent(_originalManager).Snapshot;
            var comparison = RestartRules.Compare(_priorResult.Baseline, captured);
            var currentCapture = CaptureContinueEvidence();
            var currentContinue = currentCapture.Evidence;
            var expectedContinue = _priorResult.ContinueBefore;
            var continueFailure = PriorIntegrity.CompareContinue(expectedContinue, currentCapture);
            var continueEqual = continueFailure == ContinueFailure.None;
            var managerOk = ReferenceEquals(SaveManager.Instance, _originalManager) && RunManager.Instance.DebugOnlyGetState() is null && (_originalManager.CurrentRunSaveTask is null || _originalManager.CurrentRunSaveTask.IsCompleted);
            _restart = new(true, true, true, true, comparison.FileSet, comparison.Content, comparison.Inventory,
                comparison.MtimeChanges, comparison.MtimeAllowed, continueEqual, currentContinue, continueFailure, managerOk);
            _protectionBefore = _priorResult.Baseline; _protectionAfter = captured.Files;
            var pass = comparison.FileSet && comparison.Content && comparison.Inventory && comparison.MtimeAllowed && continueEqual && managerOk;
            if (pass) CommitTransactionEvent(EventKind.RestartVerified);
            Seal(pass ? TerminalClassification.PassRestartIntegrity : TerminalClassification.FailProtection,
                pass ? FailureSite.None : FailureSite.RestartReadOnly);
        }
        catch { Seal(TerminalClassification.BlockedInstrumentation, FailureSite.RestartReadOnly); }
    }

    internal static bool RevalidateTimingGate(SaveManager original)
    {
        lock (Session.SyncRoot)
        {
            if (!_verifyUsed || _gateSnapshot is null || !ReferenceEquals(original, _originalManager) || !LoadedModIdentityValid()) return false;
            var snapshot = CaptureGate(true);
            return All(snapshot.Conditions) && snapshot.OuterTaskRuntimeIdentity == _gateSnapshot.OuterTaskRuntimeIdentity && snapshot.OriginalManagerRuntimeIdentity == _gateSnapshot.OriginalManagerRuntimeIdentity;
        }
    }
    internal static void CommitTransactionEvent(EventKind kind) { if (Session.IsObserverRunningFirst()) lock (Session.SyncRoot) Session.TryCommit(kind, null, out _); }
    internal static void ResetRestoredMenuSignals() => RestoredMenu.Reset();
    internal static Task<RestoredMenuSignalEvidence> WaitForRestoredMenuAsync(TimeSpan timeout) => RestoredMenu.WaitAsync(timeout);
    internal static RestoredMenuSignalEvidence CaptureRestoredMenuLoadException(Exception exception) => RestoredMenu.SnapshotLoadException(exception);
    internal static ContinueEvidence? ReadContinueEvidence() => CaptureContinueEvidence().Evidence;

    internal static ContinueCapture CaptureContinueEvidence()
    {
        try
        {
            var menu = _menu;
            if (menu is null || !GodotObject.IsInstanceValid(menu) || !menu.IsInsideTree()) return new(null, ContinueFailure.ReadError);
            var button = menu.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/ContinueButton");
            if (button?.Visible != true) return new(null, ContinueFailure.NotVisible);
            var enabledProperty = typeof(NClickableControl).GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? typeof(NClickableControl).GetProperty("Enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var enabled = enabledProperty?.GetValue(button) as bool? ?? true;
            if (!enabled) return new(null, ContinueFailure.NotEnabled);
            var field = typeof(NMainMenu).GetField("_readRunSaveResult", BindingFlags.Instance | BindingFlags.NonPublic);
            var result = field?.GetValue(menu);
            var data = result?.GetType().GetProperty("SaveData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(result) as SerializableRun;
            if (data is null) return new(null, ContinueFailure.MissingSaveData);
            var canonical = JsonSupport.Canonicalize(JsonSerializationUtility.ToJson(data));
            return new(new ContinueEvidence(true, true, data.NumReloads, JsonSupport.Sha256Text(canonical)), ContinueFailure.None);
        }
        catch { return new(null, ContinueFailure.ReadError); }
    }

    internal static string TerminalTextForUi()
    {
        var classification = Session.Classification;
        return classification == TerminalClassification.Running ? "" : External(classification) + "\nFailure site: " + Session.FailureSite;
    }

    internal static void CompleteFault(ProbePhase phase, string attempt, TerminalClassification classification,
        FailureSite site, ProtectionSnapshot? before, IReadOnlyList<ProtectedFile>? after, string? recoveryRoot,
        CandidateEvidence? candidate, FaultEvidence fault, AuditSnapshot audit, TeardownEvidence teardown,
        IReadOnlyList<LobbyObservation> lobbyObservations, LobbyOracleEvidence lobbyOracle, SceneEvidence scene)
    {
        _attemptToken = attempt; _protectionBefore = before; _protectionAfter = after; _recoveryRoot = recoveryRoot;
        _candidate = candidate; _fault = fault; _audit = audit; _teardown = teardown;
        _lobbyObservations = lobbyObservations; _lobbyOracle = lobbyOracle; _scene = scene;
        Seal(classification, site);
    }

    internal static void CompletePreSetupBlocked(string attempt, TerminalClassification classification,
        FailureSite site, LobbyObservation? observation)
    {
        _attemptToken = attempt;
        _lobbyObservations = observation is null ? [] : [observation];
        _lobbyOracle = observation is null ? null : PhaseAwareOracle.EvaluateLobby(_lobbyObservations);
        Seal(classification, site);
    }

    private static GateSnapshot CaptureGate(bool verifying)
    {
        var manager = SaveManager.Instance; var task = _originalManager?.CurrentRunSaveTask;
        var noPost = !verifying || (_graphicsEntries == _gateSnapshot!.GraphicsEntryCount && _graphicsNormalExits == _gateSnapshot.GraphicsNormalExitCount && _outerTask is not null && ScopeSafeSession.Identity(_outerTask) == _gateSnapshot.OuterTaskRuntimeIdentity);
        var conditions = new GateConditions(_patchActive && _patchActiveSequence > 0 && _firstGraphicsEntrySequence > _patchActiveSequence,
            _graphicsEntries == 1 && _graphicsNormalExits == 1 && _graphicsActive == 0 && !_graphicsFault,
            _outerTask is not null, IsTerminal(_outerStatus), IsTerminal(_startupStatus), _menuReady && _menu is not null,
            _originalManager is not null && ReferenceEquals(manager, _originalManager) && ScopeSafeSession.Identity(manager) == _originalManagerIdentity,
            RunManager.Instance?.DebugOnlyGetState() is null, task is null || task.IsCompleted,
            Session.Classification == TerminalClassification.Running, noPost);
        return new(0, _originalManagerIdentity, _outerIdentity, _outerStatus, _startupStatus, Classify(task), conditions.RunStateNull, _graphicsEntries, _graphicsNormalExits, _graphicsActive, conditions);
    }
    private static bool All(GateConditions x) => x.PatchBeforeGraphics && x.GraphicsNormalAndIdle && x.ExactlyOneOuterTask && x.OuterTaskTerminal && x.StartupMarkerTerminal && x.MenuReady && x.ManagerIdentityStable && x.RunStateNull && x.CurrentRunTaskNullOrTerminal && x.NoTerminalFailure && x.NoPostGateLifecycle;
    private static bool LoadedModIdentityValid()
    {
        var ids = GetLoadedModIds();
        return ids.Count == 1 && ids[0] == "SPIKEINV008A";
    }
    private static List<string> GetLoadedModIds() => ModManager.GetLoadedMods().Where(x => x.manifest is not null)
        .Select(x => x.manifest!.id ?? "").ToList();
    private static void SealConfigurationPrecheck(ModSetPrecheckResult precheck)
    {
        _startupTimeout?.Cancel();
        var attempt = Guid.NewGuid().ToString("N");
        var ticket = Session.TrySeal(TerminalClassification.BlockedPrecheckModSet, FailureSite.ConfigurationPrecheck,
            null, null);
        if (ticket is null) return;
        var written = Session.TryWriteClosedArtifact(ticket, _ =>
        {
            try { ConfigurationPrecheck.WriteFailure(ProbeConstants.EvidenceRoot, attempt, precheck); return true; }
            catch { return false; }
        });
        GD.Print(written
            ? $"[SPIKE-INV008A-R6] BLOCKED_PRECHECK_MODSET attempt={attempt}; stop and preserve this pre-business evidence."
            : "[SPIKE-INV008A-R6] BLOCKED_ARTIFACT_WRITE while recording configuration precheck; stop.");
    }
    private static void TimeoutCallback()
    {
        if (!Session.IsObserverRunningFirst()) return;
        lock (Session.SyncRoot)
        {
            if (_gateReady || Volatile.Read(ref _transactionOwner) != 0 || _verifyUsed || !StartupGate.TryTimeout()) return;
            Seal(TerminalClassification.BlockedPrecondition, FailureSite.TimeoutCallback);
        }
    }
    private static void Seal(TerminalClassification classification, FailureSite site)
    {
        var ticket = Session.TrySeal(classification, site, _gateSnapshot, _verifySnapshot);
        if (ticket is null) return;
        var written = Session.TryWriteClosedArtifact(ticket, closed => WriteArtifact(closed));
        try { ProbeUi.ShowTerminalFixed(written ? TerminalTextForUi() : "BLOCKED_ARTIFACT_WRITE\nFailure site: ArtifactWriter"); } catch { }
    }
    private static bool WriteArtifact(ClosedSnapshot closed)
    {
        try
        {
            var artifact = new RuntimeArtifact(ProbeConstants.SchemaId, ProbeConstants.ProbeVersion, _decision.Phase,
                _attemptToken, ProbeConstants.GameVersion, ProbeConstants.GameCommit, ProbeConstants.GameBranch,
                ProbeConstants.GameDllLength, ProbeConstants.GameDllSha256, _probeDllLength, _probeDllSha256,
                External(closed.TerminalClassification), closed.FirstFailureSite.ToString(), closed.TerminalSequence,
                closed.TerminalElapsedMonotonicTicks, closed.Events, _decision.PriorAttempt, closed.Gate, closed.Verify, _priorEvidence,
                _continueBefore, _lobbyObservations, _lobbyOracle, _scene, _protectionBefore, _protectionAfter,
                _recoveryRoot, _candidate, _fault, _audit, _teardown, _restart,
                _previousArtifactSha256,
                "Only probe-owned synchronous owner faults with ordered teardown and normal-exit restart integrity on fixed v0.111.0; no crash, native fault, replay dispatch, natural victory, map/room, gameplay input, reward, or exhaustive native/deferred side-channel proof.");
            Directory.CreateDirectory(ProbeConstants.EvidenceRoot);
            if (File.Exists(_decision.ArtifactPath) || File.Exists(_decision.ArtifactPath + ".tmp")) return false;
            var payload = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(artifact, JsonOptions));
            using (var output = new FileStream(_decision.ArtifactPath + ".tmp", FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None)) { output.Write(payload); output.Flush(true); }
            File.Move(_decision.ArtifactPath + ".tmp", _decision.ArtifactPath);
            return true;
        }
        catch { return false; }
    }
    private static string External(TerminalClassification value) => value switch
    {
        TerminalClassification.PassOwnerFaultTeardown => "PASS_OWNER_FAULT_TEARDOWN",
        TerminalClassification.PassRestartIntegrity => "PASS_RESTART_INTEGRITY",
        TerminalClassification.FailOwnerFault => "FAIL_OWNER_FAULT",
        TerminalClassification.FailBoundary => "FAIL_BOUNDARY",
        TerminalClassification.FailProtection => "FAIL_PROTECTION",
        TerminalClassification.FailTeardownTask => "FAIL_TEARDOWN_TASK",
        TerminalClassification.FailTeardownCleanup => "FAIL_TEARDOWN_CLEANUP",
        TerminalClassification.FailTeardownRestoreManager => "FAIL_TEARDOWN_RESTORE_MANAGER",
        TerminalClassification.FailTeardownMenu => "FAIL_TEARDOWN_MENU",
        TerminalClassification.FailTeardownScene => "FAIL_TEARDOWN_SCENE",
        TerminalClassification.FailTeardownOrder => "FAIL_TEARDOWN_ORDER",
        TerminalClassification.FailUnexpectedReplayRecording => "FAIL_UNEXPECTED_REPLAY_RECORDING",
        TerminalClassification.BlockedIdentity => "BLOCKED_IDENTITY",
        TerminalClassification.BlockedPriorIntegrity => "BLOCKED_PRIOR_INTEGRITY",
        TerminalClassification.BlockedPriorAttemptUnresolved => "BLOCKED_PRIOR_ATTEMPT_UNRESOLVED",
        TerminalClassification.BlockedPrecheckModSet => "BLOCKED_PRECHECK_MODSET",
        TerminalClassification.BlockedPhaseOrder => "BLOCKED_PHASE_ORDER",
        TerminalClassification.BlockedPrecondition => "BLOCKED_PRECONDITION",
        TerminalClassification.BlockedPreconditionStaleRunLobby => "BLOCKED_PRECONDITION_STALE_RUNLOBBY",
        TerminalClassification.BlockedArtifactWrite => "BLOCKED_ARTIFACT_WRITE",
        _ => "BLOCKED_INSTRUMENTATION"
    };
    private static FailureSite ContinueFailureSite(ContinueFailure value) => value switch
    {
        ContinueFailure.NotVisible => FailureSite.ContinueNotVisible,
        ContinueFailure.NotEnabled => FailureSite.ContinueNotEnabled,
        ContinueFailure.MissingSaveData => FailureSite.ContinueMissingSaveData,
        ContinueFailure.NumReloadsMismatch => FailureSite.ContinueNumReloadsMismatch,
        ContinueFailure.CanonicalMismatch => FailureSite.ContinueCanonicalMismatch,
        _ => FailureSite.PriorIntegrity
    };
    internal static FailureSite FailureSiteForContinue(ContinueFailure value) => ContinueFailureSite(value);
    private static ObservedTaskStatus Classify(Task? task) => task is null ? ObservedTaskStatus.Null : !task.IsCompleted ? ObservedTaskStatus.Pending : task.IsCanceled ? ObservedTaskStatus.Canceled : task.IsFaulted ? ObservedTaskStatus.Faulted : ObservedTaskStatus.RanToCompletion;
    private static bool IsTerminal(ObservedTaskStatus status) => status is ObservedTaskStatus.RanToCompletion or ObservedTaskStatus.Faulted or ObservedTaskStatus.Canceled;
}

internal enum StartupGateState { Waiting, GateReady, RunAccepted, TimedOut }
internal sealed class StartupGateArbiter
{
    private readonly object _sync = new(); private StartupGateState _state;
    internal StartupGateState State { get { lock (_sync) return _state; } }
    internal bool TryCommitGateReady() { lock (_sync) { if (_state != StartupGateState.Waiting) return false; _state = StartupGateState.GateReady; return true; } }
    internal bool TryAcceptRun() { lock (_sync) { if (_state != StartupGateState.GateReady) return false; _state = StartupGateState.RunAccepted; return true; } }
    internal bool TryTimeout() { lock (_sync) { if (_state != StartupGateState.Waiting) return false; _state = StartupGateState.TimedOut; return true; } }
}
