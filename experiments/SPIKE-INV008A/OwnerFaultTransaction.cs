using System.Reflection;
using System.Security.Cryptography;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv008A;

internal sealed class ControlledOwnerFaultException(ProbePhase phase, FaultSite site, string attemptToken)
    : Exception("controlled owner fault")
{
    internal ProbePhase Phase { get; } = phase;
    internal FaultSite Site { get; } = site;
    internal string AttemptToken { get; } = attemptToken;
}

internal static class OwnerFaultTransaction
{
    private static readonly PropertyInfo ShouldSaveProperty = typeof(RunManager).GetProperty(nameof(RunManager.ShouldSave), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMemberException(nameof(RunManager.ShouldSave));
    private static readonly PropertyInfo CleaningProperty = typeof(RunManager).GetProperty("IsCleaningUp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMemberException("IsCleaningUp");
    private static readonly MethodInfo LoadMainMenuMethod = typeof(NGame).GetMethod("LoadMainMenu", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMemberException("LoadMainMenu");
    private static int _started, _boundaryFailure;

    internal static bool Active => Volatile.Read(ref _started) != 0;
    internal static void SignalBoundaryFailure(BoundaryFailure failure) => Interlocked.CompareExchange(ref _boundaryFailure, (int)failure, 0);
    internal static void Start(ProbePhase phase, SaveManager original, ContinueEvidence baseline)
    {
        var attempt = Guid.NewGuid().ToString("N");
        LobbyObservation preSetup;
        try { preSetup = PhaseAwareOracle.CaptureLobby("PreSetup"); }
        catch { ProbeController.CompletePreSetupBlocked(attempt, TerminalClassification.BlockedInstrumentation, FailureSite.RunLobbyBoundary, null); return; }
        if (!PhaseAwareOracle.PreSetupAllowed(preSetup))
        { ProbeController.CompletePreSetupBlocked(attempt, TerminalClassification.BlockedPreconditionStaleRunLobby, FailureSite.StaleRunLobby, preSetup); return; }
        ProbeController.CommitTransactionEvent(EventKind.PreSetupLobbyVerified);
        TaskHelper.RunSafely(RunAsync(phase, original, baseline, attempt, preSetup));
    }
    internal static void ObserveMainMenuLoad()
    {
        if (!Active || !BoundaryAudit.IsObserverRunningFirst()) return;
        if (!ReferenceEquals(SaveManager.Instance, ProbeController.OriginalManager)) SignalBoundaryFailure(BoundaryFailure.UnknownManager);
        ProbeController.CommitTransactionEvent(EventKind.MainMenuLoad);
    }

    private static async Task RunAsync(ProbePhase phase, SaveManager original, ContinueEvidence continueBefore,
        string attempt, LobbyObservation preSetup)
    {
        if (!PhasePolicy.IsFault(phase) || Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;
        SaveManager? candidate = null;
        MockGodotFileIo? memory = null;
        ProtectionRoots? roots = null;
        ProtectionSnapshot? before = null;
        IReadOnlyList<ProtectedFile>? after = null;
        string? recoveryRoot = null;
        CandidateEvidence? candidateEvidence = null;
        FaultEvidence? faultEvidence = null;
        AuditSnapshot? audit = null;
        TeardownEvidence? teardownEvidence = null;
        var plan = new TeardownPlan();
        var candidateTasks = new List<TaskObservation>();
        var originalTasks = new List<TaskObservation>();
        var classification = TerminalClassification.BlockedPrecondition;
        var site = FailureSite.ProtectionBaseline;
        var overrideInstalled = false;
        var clearRestored = false;
        var fallbackUsed = false;
        var originalRestored = false;
        var menuReady = false;
        var refreshButtons = false;
        RestoredMenuSignalEvidence? restoredMenuSignals = null;
        var protectionUnchanged = false;
        var faultThrow = 0;
        var faultCatch = 0;
        var ownerFinally = 0;
        var expectedFault = phase == ProbePhase.SetupOwnerFault ? FaultSite.AfterSetup : FaultSite.AfterSceneInstall;
        var actualFault = FaultSite.None;
        var escapedOwner = false;
        string[]? originalCanonical = null;
        object? ownedRunScene = null;
        var setupFaultHadNoOwnedNRun = false;
        var faultSceneIdentity = 0;
        var faultSceneWasOwnedNRun = false;
        var cleanupEntrySceneIdentity = 0;
        var restoredSceneIdentity = 0;
        var restoredSceneIsMainMenu = false;
        var ownedNRunNoLongerCurrent = false;
        SceneEvidence? sceneEvidence = null;

        try
        {
            (roots, before, recoveryRoot) = ProtectionScope.CaptureAndBackup(original, phase, attempt);
            ProbeController.CommitTransactionEvent(EventKind.BaselineReady);
            originalCanonical = CaptureOriginalCanonical(original);
            var live = LiveSeedCandidate.CaptureLive(original);
            var built = LiveSeedCandidate.CreateLiveCandidate(live);
            candidate = built.Manager; memory = built.Store;
            var components = built.Components.ToDictionary(x => x.Component, StringComparer.Ordinal);
            var independent = built.References.ManagerIndependent && built.References.SettingsIndependent &&
                built.References.PrefsIndependent && built.References.ProgressIndependent && built.References.SharedMutableReferences.Count == 0;
            var initMutators = built.StoreAudit.Any(x => x.Phase != "seed-live-serialized" && IsMutator(x.Method));
            candidateEvidence = new(candidate.CurrentProfileId == 1,
                components["Profile"].RawEqual, components["Profile"].CanonicalEqual,
                components["Settings"].RawEqual, components["Settings"].CanonicalEqual,
                components["Prefs"].RawEqual, components["Prefs"].CanonicalEqual,
                components["Progress"].RawEqual, components["Progress"].CanonicalEqual,
                independent, built.References.SharedMutableReferences.Count, built.StoreContainmentPassed,
                !initMutators, originalCanonical.SequenceEqual(CaptureOriginalCanonical(original), StringComparer.Ordinal));
            if (!CandidatePassed(candidateEvidence)) throw new InvalidOperationException("candidate gate");
            if (!ProbeController.RevalidateTimingGate(original)) throw new InvalidOperationException("timing gate");
            ProbeController.CommitTransactionEvent(EventKind.CandidateReady);

            memory.Calls.Clear();
            BoundaryAudit.Start(original, candidate, memory, preSetup);
            SaveManager.MockInstanceForTesting(candidate); overrideInstalled = true;
            RequireCandidate(candidate);
            ProbeController.CommitTransactionEvent(EventKind.OverrideInstalled);
            ThrowBoundary();

            var replay = ReadFixture();
            var runState = RunState.FromSerializable(replay.serializableRun);
            if (runState.Players.Count != 1) throw new InvalidOperationException("player cardinality");
            var player = runState.Players[0];
            ProbeController.CommitTransactionEvent(EventKind.RunStateParsed);
            RunManager.Instance.SetUpReplay(runState, replay, player.NetId);
            SetShouldSaveFalse();
            RunManager.Instance.CombatStateSynchronizer.IsDisabled = true;
            RequireSetupGates(candidate, runState);
            ProbeController.CommitTransactionEvent(EventKind.ReplaySetupComplete);
            if (!BoundaryAudit.ObserveLobby("PostSetup")) throw new InvalidOperationException("post-setup lobby boundary");
            ProbeController.CommitTransactionEvent(EventKind.PostSetupLobbyVerified);
            ThrowBoundary();

            if (phase == ProbePhase.PostLaunchOwnerFault)
            {
                await PreloadManager.LoadRunAssets(runState.Players.Select(p => p.Character));
                await PreloadManager.LoadActAssets(runState.Act);
                ProbeController.CommitTransactionEvent(EventKind.AssetsPreloaded);
                RequireSetupGates(candidate, runState);
                RunManager.Instance.Launch();
                ProbeController.CommitTransactionEvent(EventKind.LaunchReturned);
                RequireLaunchCounts();
                if (!BoundaryAudit.ObserveLobby("PostLaunch")) throw new InvalidOperationException("post-launch lobby boundary");
                ProbeController.CommitTransactionEvent(EventKind.PostLaunchLobbyVerified);
                var game = NGame.Instance ?? throw new InvalidOperationException("NGame unavailable");
                var runScene = NRun.Create(runState);
                ownedRunScene = runScene;
                game.RootSceneContainer.SetCurrentScene(runScene);
                BoundaryAudit.SceneInstall();
                if (!ReferenceEquals(game.RootSceneContainer.CurrentScene, runScene) ||
                    !ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), runState))
                    throw new InvalidOperationException("scene identity");
                RequireWriterNotRecording();
                ProbeController.CommitTransactionEvent(EventKind.SceneInstalled);
                if (!BoundaryAudit.ObserveLobby("PostScene")) throw new InvalidOperationException("post-scene lobby boundary");
                ProbeController.CommitTransactionEvent(EventKind.PostSceneLobbyVerified);
            }
            var faultScene = NGame.Instance?.RootSceneContainer.CurrentScene;
            setupFaultHadNoOwnedNRun = phase != ProbePhase.SetupOwnerFault || faultScene is not NRun;
            faultSceneIdentity = faultScene is null ? 0 : ScopeSafeSession.Identity(faultScene);
            faultSceneWasOwnedNRun = ownedRunScene is not null && ReferenceEquals(faultScene, ownedRunScene);
            RequireFaultPointCounts(phase);
            actualFault = expectedFault;
            faultThrow++;
            ProbeController.CommitTransactionEvent(EventKind.ControlledFaultThrown);
            throw new ControlledOwnerFaultException(phase, expectedFault, attempt);
        }
        catch (ControlledOwnerFaultException controlled) when (controlled.Phase == phase && controlled.Site == expectedFault && controlled.AttemptToken == attempt)
        {
            faultCatch++;
            actualFault = controlled.Site;
            classification = TerminalClassification.PassOwnerFaultTeardown;
            site = FailureSite.None;
            ProbeController.CommitTransactionEvent(EventKind.OwnerCatch);
        }
        catch (ControlledOwnerFaultException)
        {
            faultCatch++;
            classification = TerminalClassification.FailOwnerFault;
            site = FailureSite.OwnerCatch;
        }
        catch
        {
            classification = TerminalClassification.BlockedInstrumentation;
            site = FailureSite.RunCallback;
        }
        finally
        {
            ownerFinally++;
            ProbeController.CommitTransactionEvent(EventKind.OwnerFinally);
            if (!plan.TryOwn()) { classification = TerminalClassification.FailTeardownOrder; site = FailureSite.TeardownOwnership; }
            else
            {
                ProbeController.CommitTransactionEvent(EventKind.TeardownStarted);
                try
                {
                    plan.Step("actions-disabled", () => { RequireWriterNotRecording(); RequireBusinessClosed(phase); });
                    await StepTask(plan, "isolated-task-wait-1", candidate, candidateTasks, EventKind.IsolatedTaskWait1);
                    plan.Step("cleanup", () =>
                    {
                        if (RunManager.Instance.DebugOnlyGetState() is null) throw new InvalidOperationException("cleanup state unexpectedly null");
                        var currentScene = NGame.Instance?.RootSceneContainer.CurrentScene;
                        cleanupEntrySceneIdentity = currentScene is null ? 0 : ScopeSafeSession.Identity(currentScene);
                        if (phase == ProbePhase.PostLaunchOwnerFault && !ReferenceEquals(currentScene, ownedRunScene))
                            throw new InvalidOperationException("owned NRun not current at cleanup entry");
                        RunManager.Instance.CleanUp(true);
                    });
                    await StepTask(plan, "isolated-task-wait-2", candidate, candidateTasks, EventKind.IsolatedTaskWait2);
                }
                finally
                {
                    plan.Step("restore-manager", () =>
                    {
                        if (overrideInstalled) SaveManager.ClearInstanceForTesting();
                        clearRestored = !overrideInstalled || ReferenceEquals(SaveManager.Instance, original);
                        if (!clearRestored)
                        {
                            fallbackUsed = true;
                            SaveManager.MockInstanceForTesting(original);
                        }
                        originalRestored = ReferenceEquals(SaveManager.Instance, original);
                        if (!originalRestored) throw new InvalidOperationException("manager restore failed");
                    });
                    ProbeController.CommitTransactionEvent(EventKind.OverrideCleared);
                    if (originalRestored) ProbeController.CommitTransactionEvent(EventKind.ManagerRestored);
                }

                await StepTask(plan, "original-task-wait", original, originalTasks, EventKind.OriginalTaskChecked);
                var preliminary = SnapshotCleanup();
                var cleanupPassed = BoundaryAudit.CleanupComplete(preliminary) && !plan.Errors.Any(x => x.StartsWith("cleanup:", StringComparison.Ordinal));
                if (cleanupPassed && originalRestored && !plan.Errors.Any(x => x.StartsWith("original-task-wait:", StringComparison.Ordinal)))
                {
                    plan.Step("main-menu-load", () => ProbeController.ResetRestoredMenuSignals());
                    if (!plan.Errors.Any(x => x.StartsWith("main-menu-load:", StringComparison.Ordinal)))
                    {
                        try
                        {
                            var game = NGame.Instance ?? throw new InvalidOperationException("NGame unavailable");
                            await ((Task?)LoadMainMenuMethod.Invoke(game, [false]) ?? throw new InvalidOperationException("LoadMainMenu returned null"));
                            restoredMenuSignals = await ProbeController.WaitForRestoredMenuAsync(TimeSpan.FromSeconds(ProbeConstants.TaskWaitSeconds));
                            menuReady = restoredMenuSignals.MenuReadyStatus == ObservedTaskStatus.RanToCompletion;
                            refreshButtons = restoredMenuSignals.RefreshButtonsStatus == ObservedTaskStatus.RanToCompletion;
                            if (!restoredMenuSignals.Passed)
                                plan.Errors.Add("restored-menu:" + restoredMenuSignals.FailureSite + ":" + restoredMenuSignals.Failure);
                        }
                        catch (Exception exception)
                        {
                            restoredMenuSignals = ProbeController.CaptureRestoredMenuLoadException(exception);
                            menuReady = restoredMenuSignals.MenuReadyStatus == ObservedTaskStatus.RanToCompletion;
                            refreshButtons = restoredMenuSignals.RefreshButtonsStatus == ObservedTaskStatus.RanToCompletion;
                            plan.Errors.Add("main-menu-load:" + exception.GetType().Name);
                        }
                        var restored = NGame.Instance?.RootSceneContainer.CurrentScene;
                        restoredSceneIdentity = restored is null ? 0 : ScopeSafeSession.Identity(restored);
                        restoredSceneIsMainMenu = restored is NMainMenu;
                        ownedNRunNoLongerCurrent = ownedRunScene is null || !ReferenceEquals(restored, ownedRunScene);
                        if (restoredMenuSignals?.Passed == true) ProbeController.CommitTransactionEvent(EventKind.MainMenuRestored);
                        var candidateScene = new SceneEvidence(setupFaultHadNoOwnedNRun,
                            ownedRunScene is null ? 0 : ScopeSafeSession.Identity(ownedRunScene), faultSceneIdentity,
                            faultSceneWasOwnedNRun, cleanupEntrySceneIdentity, restoredSceneIdentity,
                            restoredSceneIsMainMenu, ownedNRunNoLongerCurrent, false);
                        sceneEvidence = candidateScene with { Passed = PhaseAwareOracle.ScenePassed(phase, candidateScene) };
                        if (sceneEvidence.Passed) ProbeController.CommitTransactionEvent(EventKind.SceneReplacementVerified);
                    }
                }
                if (roots is not null && before is not null)
                {
                    plan.Step("protection-after", () =>
                    {
                        var captured = ProtectionScope.Capture(roots);
                        after = captured.Files;
                        protectionUnchanged = ProtectionScope.Equivalent(before, captured);
                        if (protectionUnchanged) ProbeController.CommitTransactionEvent(EventKind.ProtectionVerified);
                    });
                }
            }

            faultEvidence = new(attempt, expectedFault, actualFault, typeof(ControlledOwnerFaultException).FullName!, faultThrow, faultCatch, ownerFinally, escapedOwner);
            var finalManager = RunManager.Instance;
            var stateNull = finalManager?.DebugOnlyGetState() is null;
            var shouldSaveFalse = finalManager is not null && !finalManager.ShouldSave;
            var cleaningFalse = finalManager is not null && !(bool)(CleaningProperty.GetValue(finalManager) ?? true);
            var netIdNull = LocalContext.NetId is null;
            var sealedEvidence = BoundaryAudit.SealAndSnapshot(stateNull, shouldSaveFalse, cleaningFalse, netIdNull);
            audit = sealedEvidence.Audit;
            var lobbyObservations = BoundaryAudit.SnapshotLobbyObservations();
            var lobbyOracle = PhaseAwareOracle.EvaluateLobby(lobbyObservations);
            if (sceneEvidence is null)
            {
                var restored = NGame.Instance?.RootSceneContainer.CurrentScene;
                restoredSceneIdentity = restored is null ? 0 : ScopeSafeSession.Identity(restored);
                restoredSceneIsMainMenu = restored is NMainMenu;
                ownedNRunNoLongerCurrent = ownedRunScene is null || !ReferenceEquals(restored, ownedRunScene);
                var candidateScene = new SceneEvidence(setupFaultHadNoOwnedNRun,
                    ownedRunScene is null ? 0 : ScopeSafeSession.Identity(ownedRunScene), faultSceneIdentity,
                    faultSceneWasOwnedNRun, cleanupEntrySceneIdentity, restoredSceneIdentity,
                    restoredSceneIsMainMenu, ownedNRunNoLongerCurrent, false);
                sceneEvidence = candidateScene with { Passed = PhaseAwareOracle.ScenePassed(phase, candidateScene) };
            }
            var continueCapture = ProbeController.CaptureContinueEvidence();
            var continueAfter = continueCapture.Evidence;
            var continueFailure = PriorIntegrity.CompareContinue(continueBefore, continueCapture);
            teardownEvidence = new(plan.Reached.Count == 0 ? TeardownStage.None : TeardownStage.Sealed,
                plan.OwnerCount, candidateTasks, originalTasks, sealedEvidence.Cleanup, clearRestored, fallbackUsed,
                originalRestored, menuReady, refreshButtons, restoredMenuSignals, continueAfter, continueFailure, protectionUnchanged, plan.Errors);

            if (faultThrow != 1 || faultCatch != 1 || ownerFinally != 1 || actualFault != expectedFault)
            { classification = TerminalClassification.FailOwnerFault; site = FailureSite.ControlledFault; }
            else if (!lobbyOracle.Passed)
            { classification = TerminalClassification.FailBoundary; site = FailureSite.RunLobbyBoundary; }
            else if (audit.FirstFailure != BoundaryFailure.None || !BoundaryAudit.BusinessZero(audit.Business))
            { classification = TerminalClassification.FailBoundary; site = audit.FirstFailure == BoundaryFailure.RunLobbyBoundary ? FailureSite.RunLobbyBoundary : FailureSite.Boundary; }
            else if (!BoundaryAudit.CleanupComplete(sealedEvidence.Cleanup))
            { classification = TerminalClassification.FailTeardownCleanup; site = FailureSite.Cleanup; }
            else if (!originalRestored)
            { classification = TerminalClassification.FailTeardownRestoreManager; site = FailureSite.RestoreManager; }
            else if (candidateTasks.Any(x => x.Status is ObservedTaskStatus.Faulted or ObservedTaskStatus.Canceled or ObservedTaskStatus.TimedOut) || originalTasks.Any(x => x.Status is not ObservedTaskStatus.Null and not ObservedTaskStatus.RanToCompletion))
            { classification = TerminalClassification.FailTeardownTask; site = FailureSite.CurrentRunTask; }
            else if (!sceneEvidence.Passed)
            { classification = TerminalClassification.FailTeardownScene; site = FailureSite.SceneReplacement; }
            else if (restoredMenuSignals is null || !restoredMenuSignals.Passed || continueFailure != ContinueFailure.None)
            {
                classification = TerminalClassification.FailTeardownMenu;
                site = restoredMenuSignals is null ? FailureSite.MainMenu :
                    !restoredMenuSignals.Passed ? restoredMenuSignals.FailureSite : ProbeController.FailureSiteForContinue(continueFailure);
            }
            else if (!protectionUnchanged)
            { classification = TerminalClassification.FailProtection; site = FailureSite.ProtectionAfter; }
            else if (plan.Errors.Count != 0)
            { classification = TerminalClassification.FailTeardownOrder; site = FailureSite.OwnerFinally; }

            ProbeController.CompleteFault(phase, attempt, classification, site, before, after, recoveryRoot,
                candidateEvidence, faultEvidence, audit, teardownEvidence, lobbyObservations, lobbyOracle, sceneEvidence);
        }
    }

    private static CleanupEvidence SnapshotCleanup()
    {
        var manager = RunManager.Instance;
        return BoundaryAudit.PeekCleanup(manager.DebugOnlyGetState() is null, !manager.ShouldSave,
            !(bool)(CleaningProperty.GetValue(manager) ?? true), LocalContext.NetId is null);
    }
    private static async Task StepTask(TeardownPlan plan, string name, SaveManager? manager,
        List<TaskObservation> output, EventKind eventKind)
    {
        plan.Reached.Add(name + ":entry");
        var task = manager?.CurrentRunSaveTask;
        var identity = task is null ? 0 : ScopeSafeSession.Identity(task);
        var status = Classify(task);
        string? exception = null;
        try
        {
            if (task is not null && !task.IsCompleted) await task.WaitAsync(TimeSpan.FromSeconds(ProbeConstants.TaskWaitSeconds));
            status = Classify(task);
            if (status is ObservedTaskStatus.Faulted or ObservedTaskStatus.Canceled) throw new InvalidOperationException("task terminal failure");
            plan.Reached.Add(name + ":return");
        }
        catch (TimeoutException) { status = ObservedTaskStatus.TimedOut; exception = nameof(TimeoutException); plan.Errors.Add(name + ":TimeoutException"); }
        catch (Exception value) { status = Classify(task); exception = value.GetType().Name; plan.Errors.Add(name + ":" + exception); }
        output.Add(new(name, identity, status, exception));
        ProbeController.CommitTransactionEvent(eventKind);
    }
    private static ObservedTaskStatus Classify(Task? task) => task is null ? ObservedTaskStatus.Null :
        !task.IsCompleted ? ObservedTaskStatus.Pending : task.IsCanceled ? ObservedTaskStatus.Canceled :
        task.IsFaulted ? ObservedTaskStatus.Faulted : ObservedTaskStatus.RanToCompletion;
    private static void RequireSetupGates(SaveManager candidate, RunState state)
    {
        RequireCandidate(candidate);
        if (!ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), state)) throw new InvalidOperationException("run-state binding");
        if (RunManager.Instance.ShouldSave) throw new InvalidOperationException("ShouldSave true");
        if (!RunManager.Instance.CombatStateSynchronizer.IsDisabled) throw new InvalidOperationException("sync enabled");
        RequireWriterNotRecording();
    }
    private static void RequireWriterNotRecording()
    {
        if (RunManager.Instance?.CombatReplayWriter?.IsRecordingReplay == true)
            throw new InvalidOperationException("unexpected replay recording");
    }
    private static void RequireLaunchCounts()
    {
        var business = BoundaryAudit.Snapshot().Business;
        if (business.LaunchEntries != 1 || business.LaunchReturns != 1) throw new InvalidOperationException("launch count");
    }
    private static void RequireFaultPointCounts(ProbePhase phase)
    {
        var x = BoundaryAudit.Snapshot().Business;
        var expectedLaunch = phase == ProbePhase.PostLaunchOwnerFault ? 1 : 0;
        var expectedScene = phase == ProbePhase.PostLaunchOwnerFault ? 1 : 0;
        if (x.LaunchEntries != expectedLaunch || x.LaunchReturns != expectedLaunch || x.SceneInstalls != expectedScene || !BoundaryAudit.BusinessZero(x))
            throw new InvalidOperationException("fault point counts");
    }
    private static void RequireBusinessClosed(ProbePhase phase) { RequireFaultPointCounts(phase); }
    private static void ThrowBoundary() { if (Volatile.Read(ref _boundaryFailure) != 0) throw new InvalidOperationException("boundary failure"); }
    private static void RequireCandidate(SaveManager candidate) { if (!ReferenceEquals(SaveManager.Instance, candidate)) throw new InvalidOperationException("candidate identity"); }
    private static void SetShouldSaveFalse() { ShouldSaveProperty.SetValue(RunManager.Instance, false); if (RunManager.Instance.ShouldSave) throw new InvalidOperationException("ShouldSave remained true"); }
    private static bool CandidatePassed(CandidateEvidence x) => x.ProfileIsOne && x.ProfileRawEqual && x.ProfileCanonicalEqual && x.SettingsRawEqual && x.SettingsCanonicalEqual && x.PrefsRawEqual && x.PrefsCanonicalEqual && x.ProgressRawEqual && x.ProgressCanonicalEqual && x.RootsIndependent && x.SharedMutableReferences == 0 && x.StoreContained && x.InitializationMemoryOnly && x.OriginalCanonicalUnchanged;
    private static bool IsMutator(string name) => name is "WriteFile" or "WriteFileAsync" or "DeleteFile" or "DeleteDirectory" or "RenameFile" or "BeginSaveBatch" or "EndSaveBatch" or "SetLastModifiedTime";
    private static string[] CaptureOriginalCanonical(SaveManager manager)
    {
        var live = LiveSeedCandidate.CaptureLive(manager);
        return [JsonSupport.Canonicalize(live.ProfileJson), JsonSupport.Canonicalize(live.SettingsJson), JsonSupport.Canonicalize(live.PrefsJson), JsonSupport.Canonicalize(live.ProgressJson)];
    }
    private static CombatReplay ReadFixture()
    {
        var bytes = File.ReadAllBytes(ProbeConstants.FixturePath);
        if (bytes.Length != ProbeConstants.FixtureLength || Convert.ToHexString(SHA256.HashData(bytes)) != ProbeConstants.FixtureSha256) throw new InvalidDataException("fixture identity");
        var reader = new PacketReader(); reader.Reset(bytes); var replay = reader.Read<CombatReplay>();
        if (replay.version != ProbeConstants.GameVersion || replay.gitCommit != ProbeConstants.GameCommit) throw new InvalidDataException("fixture release identity");
        return replay;
    }
}
