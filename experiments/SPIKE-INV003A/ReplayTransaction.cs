using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv003A;

internal enum ReplayProbePhase { B, C, D, E }

internal static class ReplayTransaction
{
    private static readonly PropertyInfo ShouldSaveProperty = typeof(RunManager).GetProperty(
        nameof(RunManager.ShouldSave), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingMemberException(typeof(RunManager).FullName, nameof(RunManager.ShouldSave));
    private static readonly MethodInfo LoadMainMenuMethod = typeof(NGame).GetMethod(
        "LoadMainMenu", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMemberException(typeof(NGame).FullName, "LoadMainMenu");
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static TaskCompletionSource _stopSignal = NewStopSignal();
    private static volatile bool _active;
    private static string? _failClosedReason;

    internal static bool Active => _active;

    internal static ReplayProbePhase ReadUnlockedPhase()
    {
        var path = Path.Combine(ProbeConstants.EvidenceRoot, "phase-control.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var text = document.RootElement.GetProperty("phase").GetString();
        return Enum.TryParse<ReplayProbePhase>(text, ignoreCase: false, out var phase)
            ? phase
            : throw new InvalidDataException($"Unsupported phase-control value: {text}");
    }

    internal static void StartFromUi()
    {
        if (_active || Gate.CurrentCount == 0)
        {
            ProbeController.ProbeError("Replay transaction start rejected because another transaction is active.");
            return;
        }
        TaskHelper.RunSafely(RunAsync(ReadUnlockedPhase()));
    }

    internal static void RequestStop(string reason)
    {
        if (!_active) return;
        ProbeController.ProbeLog($"Replay transaction stop requested: {reason}");
        _stopSignal.TrySetResult();
    }

    internal static void RequestFailClosed(string reason)
    {
        if (!_active) return;
        _failClosedReason ??= reason;
        _stopSignal.TrySetResult();
    }

    private static async Task RunAsync(ReplayProbePhase phase)
    {
        if (!await Gate.WaitAsync(0)) return;
        var started = DateTimeOffset.UtcNow;
        SaveManager? original = null;
        SaveManager? isolated = null;
        MockGodotFileIo? memoryStore = null;
        IReadOnlyList<FileIdentity>? before = null;
        Exception? operationException = null;
        Exception? teardownException = null;
        var checkpoints = new List<string>();
        var overrideInstalled = false;
        var sceneLaunched = false;
        var expectedFaultObserved = false;
        var victoryProgressObserved = false;
        var isolatedProgressWriteObserved = false;
        _stopSignal = NewStopSignal();
        _failClosedReason = null;
        _active = true;

        try
        {
            var preflight = ProbeController.ReadPreflightRoot();
            ValidateGates(preflight, phase);
            before = ProbeController.CaptureProtectedFiles(preflight);
            var baselineDifferences = CompareToPreflight(preflight, before);
            if (baselineDifferences.Count != 0)
                throw new InvalidOperationException("Protected baseline changed before transaction: " + string.Join("; ", baselineDifferences));

            original = SaveManager.Instance;
            (isolated, memoryStore) = CreateIsolatedManager(original, preflight);
            checkpoints.Add("isolated-manager-deep-copy-ready");
            TransactionAudit.Start(original, isolated, memoryStore);
            SaveManager.MockInstanceForTesting(isolated);
            overrideInstalled = true;
            if (!ReferenceEquals(SaveManager.Instance, isolated))
                throw new InvalidOperationException("SaveManager override identity assertion failed.");
            checkpoints.Add("isolated-manager-installed");

            var replay = ReadFixture(preflight);
            var runState = RunState.FromSerializable(replay.serializableRun);
            var netId = runState.Players[0].NetId;
            RunManager.Instance.SetUpReplay(runState, replay, netId);
            ShouldSaveProperty.SetValue(RunManager.Instance, false);
            if (RunManager.Instance.ShouldSave)
                throw new InvalidOperationException("RunManager.ShouldSave remained true after replay setup.");
            checkpoints.Add("setup-complete-should-save-false");

            if (phase == ReplayProbePhase.D)
            {
                expectedFaultObserved = true;
                throw new ProbeInjectedException("Phase D injected setup failure after State assignment and before scene launch.");
            }

            RunManager.Instance.CombatStateSynchronizer.IsDisabled = true;
            await PreloadManager.LoadRunAssets(runState.Players.Select(player => player.Character));
            await PreloadManager.LoadActAssets(runState.Act);
            RunManager.Instance.Launch();
            NAudioManager.Instance?.StopMusic();
            var game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance is null during replay launch.");
            game.RootSceneContainer.SetCurrentScene(NRun.Create(runState));
            sceneLaunched = true;
            await RunManager.Instance.GenerateMap();
            RunManager.Instance.ActionQueueSet.FastForwardNextActionId(replay.nextActionId);
            RunManager.Instance.ActionQueueSynchronizer.FastForwardHookId(replay.nextHookId);
            RunManager.Instance.ChecksumTracker.LoadReplayChecksums(replay.checksumData, replay.nextChecksumId);
            RunManager.Instance.PlayerChoiceSynchronizer.FastForwardChoiceIds(replay.choiceIds);
            RunManager.Instance.RewardsSetSynchronizer.FastForwardRewardIds(replay.rewardIds);
            await RunManager.Instance.LoadIntoLatestMapCoord(AbstractRoom.FromSerializable(replay.serializableRun.PreFinishedRoom, runState));
            while (RunManager.Instance.ActionExecutor.IsPaused)
                await game.GetTree().Root.AwaitProcessFrame();
            checkpoints.Add("watchable-scene-ready");

            if (phase == ReplayProbePhase.E)
            {
                await game.GetTree().Root.AwaitProcessFrame();
                expectedFaultObserved = true;
                throw new ProbeInjectedException("Phase E injected post-launch failure after the watchable scene became ready.");
            }

            if (phase == ReplayProbePhase.B)
            {
                ProbeController.ProbeLog("Phase B watchable scene ready. Press F7 once to execute ordered Stop.");
                await _stopSignal.Task.WaitAsync(TimeSpan.FromMinutes(5));
                checkpoints.Add("normal-stop-requested");
            }
            else if (phase == ReplayProbePhase.C)
            {
                await DispatchReplayEvents(replay, runState);
                await RunManager.Instance.ActionExecutor.FinishedExecutingActions().WaitAsync(TimeSpan.FromSeconds(30));
                var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
                while (DateTimeOffset.UtcNow < deadline)
                {
                    victoryProgressObserved = TransactionAudit.HasEvent("isolated-manager", nameof(SaveManager.UpdateProgressAfterCombatWon));
                    isolatedProgressWriteObserved = TransactionAudit.HasEvent("isolated-manager", nameof(SaveManager.SaveProgressFile))
                                                    && TransactionAudit.HasIsolatedStoreWriteContaining("progress.save");
                    if (victoryProgressObserved && isolatedProgressWriteObserved) break;
                    await game.GetTree().Root.AwaitProcessFrame();
                }
                checkpoints.Add("native-events-dispatched");
            }
        }
        catch (Exception ex)
        {
            operationException = ex;
            if (ex is not ProbeInjectedException)
                ProbeController.ProbeError($"Replay transaction operation failed: {ex}");
        }
        finally
        {
            try
            {
                TransactionAudit.BeginRestore();
                if (isolated is not null)
                    await ObserveSaveTask(isolated);
                if (RunManager.Instance?.DebugOnlyGetState() is not null)
                    RunManager.Instance.CleanUp(true);
                if (isolated is not null)
                    await ObserveSaveTask(isolated);

                if (overrideInstalled)
                {
                    SaveManager.ClearInstanceForTesting();
                    if (original is not null && !ReferenceEquals(SaveManager.Instance, original))
                        SaveManager.MockInstanceForTesting(original);
                }
                if (original is not null && !ReferenceEquals(SaveManager.Instance, original))
                    throw new InvalidOperationException("Original SaveManager identity was not restored.");
                checkpoints.Add("original-manager-restored");

                if (original is not null)
                    await ObserveSaveTask(original);
                var game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance is null during teardown.");
                var loadTask = LoadMainMenuMethod.Invoke(game, new object[] { false }) as Task
                    ?? throw new InvalidOperationException("NGame.LoadMainMenu did not return a Task.");
                await loadTask;
                await game.GetTree().Root.AwaitProcessFrame();
                await game.GetTree().Root.AwaitProcessFrame();
                checkpoints.Add("main-menu-rebuilt-after-restore");
            }
            catch (Exception ex)
            {
                teardownException = ex;
                ProbeController.ProbeError($"Ordered teardown failed: {ex}");
                if (original is not null && !ReferenceEquals(SaveManager.Instance, original))
                {
                    try { SaveManager.MockInstanceForTesting(original); } catch { }
                }
            }

            var audit = TransactionAudit.Snapshot();
            IReadOnlyList<FileIdentity> after = Array.Empty<FileIdentity>();
            var differences = new List<string>();
            try
            {
                var preflight = ProbeController.ReadPreflightRoot();
                after = ProbeController.CaptureProtectedFiles(preflight);
                if (before is not null) differences.AddRange(ProbeController.CompareFileIdentities(before, after));
            }
            catch (Exception ex)
            {
                differences.Add("post-transaction identity capture failed: " + ex.Message);
            }

            var phaseSatisfied = phase switch
            {
                ReplayProbePhase.B => sceneLaunched && checkpoints.Contains("normal-stop-requested") && operationException is null,
                ReplayProbePhase.C => sceneLaunched && operationException is null && victoryProgressObserved && isolatedProgressWriteObserved,
                ReplayProbePhase.D => expectedFaultObserved && !sceneLaunched && operationException is ProbeInjectedException,
                ReplayProbePhase.E => expectedFaultObserved && sceneLaunched && operationException is ProbeInjectedException,
                _ => false,
            };
            var passed = phaseSatisfied && teardownException is null && differences.Count == 0
                         && _failClosedReason is null && original is not null && ReferenceEquals(SaveManager.Instance, original)
                         && RunManager.Instance?.DebugOnlyGetState() is null;
            var outputPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", $"phase-{phase.ToString().ToLowerInvariant()}-transaction.json");
            JsonSupport.WriteAtomic(outputPath, new
            {
                schemaVersion = ProbeConstants.SchemaVersion,
                probeVersion = ProbeConstants.ProbeVersion,
                phase = phase.ToString(),
                startedUtc = started.ToString("O"),
                finishedUtc = DateTimeOffset.UtcNow.ToString("O"),
                outcome = passed ? "PASS" : "FAIL",
                sceneLaunched,
                expectedFaultObserved,
                victoryProgressObserved,
                isolatedProgressWriteObserved,
                failClosedReason = _failClosedReason,
                checkpoints,
                operationException = operationException?.ToString(),
                teardownException = teardownException?.ToString(),
                originalManagerRestored = original is not null && ReferenceEquals(SaveManager.Instance, original),
                runManagerStateIsNull = RunManager.Instance?.DebugOnlyGetState() is null,
                differences,
                before,
                after,
                audit,
            });
            TransactionAudit.Stop();
            _active = false;
            Gate.Release();
            if (passed) ProbeController.ProbeLog($"Phase {phase} transaction PASS. Evidence: {outputPath}");
            else ProbeController.ProbeError($"Phase {phase} transaction FAIL. Evidence: {outputPath}");
        }
    }

    private static void ValidateGates(JsonElement preflight, ReplayProbePhase requested)
    {
        if (requested != ReadUnlockedPhase()) throw new InvalidOperationException("Requested phase is not unlocked.");
        if (preflight.GetProperty("probeVersion").GetString() != ProbeConstants.ProbeVersion)
            throw new InvalidOperationException("F8 must be rerun with the installed transaction build.");
        if (preflight.GetProperty("blockingReasons").GetArrayLength() != 0)
            throw new InvalidOperationException("Phase 0 contains blocking reasons.");
        if (RunManager.Instance?.DebugOnlyGetState() is not null)
            throw new InvalidOperationException("RunManager.State must be null before transaction start.");
        var validationPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "fixture-validation.json");
        using var validation = JsonDocument.Parse(File.ReadAllText(validationPath));
        if (validation.RootElement.GetProperty("probeVersion").GetString() != ProbeConstants.ProbeVersion
            || validation.RootElement.GetProperty("outcome").GetString() != "VALID_NATIVE_FIXTURE")
            throw new InvalidOperationException("F9 must be rerun successfully with the installed transaction build.");
    }

    private static List<string> CompareToPreflight(JsonElement preflight, IReadOnlyList<FileIdentity> current)
    {
        var expected = preflight.GetProperty("protectedFiles").EnumerateArray().Select(entry => new FileIdentity(
            entry.GetProperty("scope").GetString() ?? "unknown",
            entry.GetProperty("path").GetString() ?? "",
            entry.TryGetProperty("relativePath", out var relative) && relative.ValueKind != JsonValueKind.Null ? relative.GetString() : null,
            entry.GetProperty("exists").GetBoolean(),
            entry.GetProperty("length").ValueKind == JsonValueKind.Null ? null : entry.GetProperty("length").GetInt64(),
            entry.GetProperty("sha256").ValueKind == JsonValueKind.Null ? null : entry.GetProperty("sha256").GetString(),
            entry.GetProperty("lastWriteUtc").ValueKind == JsonValueKind.Null ? null : entry.GetProperty("lastWriteUtc").GetString())).ToList();
        return ProbeController.CompareFileIdentities(expected, current);
    }

    private static (SaveManager Manager, MockGodotFileIo Store) CreateIsolatedManager(SaveManager original, JsonElement preflight)
    {
        var store = new MockGodotFileIo("user://spike-inv003a-memory") { getCurrentTime = () => DateTimeOffset.UtcNow };
        var accountBase = Path.GetFullPath(ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath(null)));
        foreach (var entry in preflight.GetProperty("protectedFiles").EnumerateArray())
        {
            if (!entry.GetProperty("exists").GetBoolean()) continue;
            var source = entry.GetProperty("path").GetString()!;
            if (string.Equals(Path.GetExtension(source), ".mcr", StringComparison.OrdinalIgnoreCase)) continue;
            var relative = Path.GetRelativePath(accountBase, source).Replace(Path.DirectorySeparatorChar, '/');
            if (relative.StartsWith("../", StringComparison.Ordinal))
                throw new InvalidOperationException("Protected file is outside account store root: " + source);
            store.WriteFile(relative, File.ReadAllText(source));
        }

        var manager = new SaveManager(store, store, true);
        manager.InitProfileId(null);
        var settings = manager.InitSettingsData();
        var prefs = manager.InitPrefsData();
        var progress = manager.InitProgressData();
        if (manager.CurrentProfileId != original.CurrentProfileId)
            throw new InvalidDataException("Isolated profile round-trip selected a different profile id.");
        if (!settings.Success || !prefs.Success || !progress.Success)
            throw new InvalidDataException($"Isolated manager initialization failed: settings={settings.Status}, prefs={prefs.Status}, progress={progress.Status}");
        var originalProfile = ReadProfileState(original);
        var isolatedProfile = ReadProfileState(manager);
        if (ReferenceEquals(isolatedProfile, originalProfile)
            || ReferenceEquals(manager.SettingsSave, original.SettingsSave)
            || ReferenceEquals(manager.PrefsSave, original.PrefsSave)
            || ReferenceEquals(manager.Progress, original.Progress))
            throw new InvalidOperationException("Isolated manager shares mutable save objects with the original manager.");
        if (JsonSerializationUtility.ToJson(manager.SettingsSave) != JsonSerializationUtility.ToJson(original.SettingsSave)
            || JsonSerializationUtility.ToJson(manager.PrefsSave) != JsonSerializationUtility.ToJson(original.PrefsSave)
            || JsonSerializationUtility.ToJson(manager.Progress.ToSerializable()) != JsonSerializationUtility.ToJson(original.Progress.ToSerializable()))
            throw new InvalidDataException("Isolated manager deep-copy content does not match the original manager.");
        store.Calls.Clear();
        return (manager, store);
    }

    private static object ReadProfileState(SaveManager manager)
    {
        var profileManagerField = typeof(SaveManager).GetField("_profileSaveManager", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(SaveManager).FullName, "_profileSaveManager");
        var profileManager = profileManagerField.GetValue(manager)
            ?? throw new InvalidOperationException("ProfileSaveManager is null.");
        var profileProperty = profileManager.GetType().GetProperty("Profile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(profileManager.GetType().FullName, "Profile");
        return profileProperty.GetValue(profileManager)
            ?? throw new InvalidOperationException("ProfileSave state is null.");
    }

    private static CombatReplay ReadFixture(JsonElement preflight)
    {
        var profileRoot = preflight.GetProperty("profileRoot").GetString()!;
        var path = Path.Combine(profileRoot, "replays", "latest.mcr");
        var reader = new PacketReader();
        reader.Reset(File.ReadAllBytes(path));
        var replay = reader.Read<CombatReplay>();
        if (replay.version != ProbeConstants.ExpectedGameVersion || replay.gitCommit != ProbeConstants.ExpectedCommit || replay.events.Count == 0)
            throw new InvalidDataException("Fixture identity or event gate failed during transaction start.");
        return replay;
    }

    private static async Task DispatchReplayEvents(CombatReplay replay, RunState runState)
    {
        var game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance is null during event dispatch.");
        foreach (var replayEvent in replay.events)
        {
            if (_failClosedReason is not null) throw new InvalidOperationException("Fail-closed audit violation: " + _failClosedReason);
            switch (replayEvent.eventType)
            {
                case CombatReplayEventType.GameAction:
                    var combat = CombatManager.Instance ?? throw new InvalidOperationException("CombatManager.Instance is null during event dispatch.");
                    while (combat.EndingPlayerTurnPhaseOne || combat.EndingPlayerTurnPhaseTwo)
                        await game.GetTree().Root.AwaitProcessFrame();
                    var player = runState.GetPlayer(replayEvent.playerId!.Value)
                        ?? throw new InvalidOperationException("Replay event player was not found in RunState.");
                    var action = replayEvent.action!.ToGameAction(player);
                    if (action.ActionType == GameActionType.CombatPlayPhaseOnly)
                        while ((combat.DebugOnlyGetState() ?? throw new InvalidOperationException("Combat state became null during replay.")).CurrentSide == CombatSide.Enemy)
                            await game.GetTree().Root.AwaitProcessFrame();
                    RunManager.Instance.ActionQueueSet.EnqueueWithoutSynchronizing(action);
                    if (action is EndPlayerTurnAction or ReadyToBeginEnemyTurnAction)
                        await RunManager.Instance.ActionExecutor.FinishedExecutingActions();
                    break;
                case CombatReplayEventType.HookAction:
                    RunManager.Instance.ActionQueueSet.EnqueueWithoutSynchronizing(
                        RunManager.Instance.ActionQueueSynchronizer.GetHookActionForId(replayEvent.hookId!.Value, replayEvent.playerId!.Value, replayEvent.gameActionType!.Value));
                    break;
                case CombatReplayEventType.ResumeAction:
                    RunManager.Instance.ActionQueueSet.ResumeActionWithoutSynchronizing(replayEvent.actionId!.Value);
                    break;
                case CombatReplayEventType.PlayerChoice:
                    var choicePlayer = runState.GetPlayer(replayEvent.playerId!.Value)
                        ?? throw new InvalidOperationException("Replay choice player was not found in RunState.");
                    RunManager.Instance.PlayerChoiceSynchronizer.ReceiveReplayChoice(choicePlayer, replayEvent.choiceId!.Value, replayEvent.playerChoiceResult!.Value);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }

    private static async Task ObserveSaveTask(SaveManager manager)
    {
        var task = manager.CurrentRunSaveTask;
        if (task is not null) await task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static TaskCompletionSource NewStopSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ProbeInjectedException(string message) : Exception(message);
}
