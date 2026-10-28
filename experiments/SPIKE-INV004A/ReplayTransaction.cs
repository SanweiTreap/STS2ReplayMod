using System.Reflection;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv004A;

internal static class ReplayTransaction
{
    private static readonly PropertyInfo ShouldSaveProperty = typeof(RunManager).GetProperty(
        nameof(RunManager.ShouldSave), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingMemberException(typeof(RunManager).FullName, nameof(RunManager.ShouldSave));
    private static readonly MethodInfo LoadMainMenuMethod = typeof(NGame).GetMethod(
        "LoadMainMenu", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMemberException(typeof(NGame).FullName, "LoadMainMenu");
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static volatile bool _active;
    private static string? _failClosedReason;

    internal static bool Active => _active;

    internal static string ReadControlPhase()
    {
        var path = Path.Combine(ProbeConstants.EvidenceRoot, "phase-control.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("phase").GetString()
               ?? throw new InvalidDataException("phase-control phase is missing");
    }

    internal static void StartFromUi()
    {
        if (_active || Gate.CurrentCount == 0)
        {
            ProbeController.ProbeError("Direct-write transaction start rejected because another transaction is active.");
            return;
        }
        TaskHelper.RunSafely(RunAsync());
    }

    internal static void RequestFailClosed(string reason)
    {
        if (!_active) return;
        _failClosedReason ??= reason;
    }

    private static async Task RunAsync()
    {
        if (!await Gate.WaitAsync(0)) return;
        var started = DateTimeOffset.UtcNow;
        SaveManager? original = null;
        SaveManager? isolated = null;
        MockGodotFileIo? memoryStore = null;
        IReadOnlyList<FileIdentity>? beforeFiles = null;
        DirectoryInventory? beforeProfileInventory = null;
        DirectoryInventory? beforeAccountInventory = null;
        Exception? operationException = null;
        Exception? teardownException = null;
        var checkpoints = new List<string>();
        var validationFailures = new List<string>();
        var overrideInstalled = false;
        var sceneLaunched = false;
        var cleanupCalled = false;
        object? mutationEvidence = null;
        object? writeEvidence = null;
        string? originalProgressCanonicalBefore = null;
        string? originalProgressCanonicalAfter = null;
        var continueVisibleAfterTeardown = false;
        _failClosedReason = null;
        _active = true;

        try
        {
            var preflight = ProbeController.ReadPreflightRoot();
            ValidateGates(preflight);
            beforeFiles = ProbeController.CaptureProtectedFiles(preflight);
            beforeProfileInventory = ProbeController.CaptureProfileInventory(preflight);
            beforeAccountInventory = ProbeController.CaptureAccountTopLevelInventory(preflight);
            validationFailures.AddRange(CompareToPreflight(preflight, beforeFiles));
            validationFailures.AddRange(CompareInventoryToPreflight(preflight, beforeProfileInventory, "profile"));
            validationFailures.AddRange(CompareInventoryToPreflight(preflight, beforeAccountInventory, "account-top-level"));
            ThrowIfFailures("Protected baseline changed before transaction", validationFailures);

            original = SaveManager.Instance;
            originalProgressCanonicalBefore = JsonSerializationUtility.ToJson(original.Progress.ToSerializable());
            (isolated, memoryStore) = CreateIsolatedManager(original, preflight);
            checkpoints.Add("isolated-manager-deep-copy-ready");
            TransactionAudit.Start(original, isolated, memoryStore);
            SaveManager.MockInstanceForTesting(isolated);
            overrideInstalled = true;
            if (!ReferenceEquals(SaveManager.Instance, isolated))
                throw new InvalidOperationException("SaveManager override identity assertion failed.");
            checkpoints.Add("isolated-manager-installed-before-setup");

            var replay = ReadFixture();
            var runState = RunState.FromSerializable(replay.serializableRun);
            if (runState.Players.Count != 1)
                throw new InvalidOperationException($"Fixed fixture must reconstruct exactly one player; actual {runState.Players.Count}.");
            RunManager.Instance.SetUpReplay(runState, replay, runState.Players[0].NetId);
            ShouldSaveProperty.SetValue(RunManager.Instance, false);
            if (RunManager.Instance.ShouldSave)
                throw new InvalidOperationException("RunManager.ShouldSave remained true after replay setup.");
            checkpoints.Add("setup-complete-should-save-false");

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
            await game.GetTree().Root.AwaitProcessFrame();
            checkpoints.Add("stable-watchable-scene-ready-no-events-read-or-dispatched");

            if (!ReferenceEquals(SaveManager.Instance, isolated))
                throw new InvalidOperationException("SaveManager.Instance is not the isolated manager at sentinel boundary.");
            if (!ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), runState))
                throw new InvalidOperationException("RunManager state identity differs from the reconstructed replay run.");
            var player = runState.Players.Single();
            var room = runState.CurrentRoom as CombatRoom
                ?? throw new InvalidOperationException("Current replay room is not a CombatRoom.");
            if (room.RoomType != RoomType.Monster)
                throw new InvalidOperationException($"Fixed sentinel requires a normal Monster room; actual {room.RoomType}.");
            if (!ReferenceEquals(room.CombatState.RunState, runState))
                throw new InvalidOperationException("Combat room RunState identity does not match the current replay run.");
            var encounter = room.Encounter ?? throw new InvalidOperationException("Combat room encounter is null.");
            var spawnedEnemies = encounter.SpawnedEnemies
                ?? throw new InvalidOperationException("Encounter SpawnedEnemies is null.");
            if (spawnedEnemies.Count == 0)
                throw new InvalidOperationException("Encounter SpawnedEnemies is empty.");
            checkpoints.Add("direct-write-legal-parameters-asserted");

            var baselineProgressJson = JsonSerializationUtility.ToJson(isolated.Progress.ToSerializable());
            var expectedProgress = ParseProgress(baselineProgressJson, "detached expected-progress baseline");
            var playerDiscoveredBefore = player.DiscoveredEnemies.Select(IdText).ToList();
            var expectedPlayerDiscovered = playerDiscoveredBefore.ToList();
            ApplyExpectedNormalRoomMutation(expectedProgress, player, encounter, spawnedEnemies, expectedPlayerDiscovered);
            var expectedProgressJson = JsonSerializationUtility.ToJson(expectedProgress);
            var baselineTargets = DescribeTargets(ParseProgress(baselineProgressJson, "baseline target snapshot"), player, encounter, spawnedEnemies);
            var expectedTargets = DescribeTargets(expectedProgress, player, encounter, spawnedEnemies);
            var preCallAudit = TransactionAudit.EventsSnapshot();
            if (preCallAudit.Any(x => x.Boundary is "original-manager" or "original-store" or "unknown-manager" or "unknown-store" or "external-submit"))
                throw new InvalidOperationException("An original/unknown/external boundary was observed before the direct write.");
            if (TransactionAudit.MemoryStoreCallsSnapshot().Any(x => IsWrite(x.Method)))
                throw new InvalidOperationException("Isolated store already contains a write before the sentinel.");

            SaveManager.Instance.UpdateProgressAfterCombatWon(player, room);
            if (_failClosedReason is not null)
                throw new InvalidOperationException("Fail-closed audit violation after progress update: " + _failClosedReason);
            var actualProgressJson = JsonSerializationUtility.ToJson(isolated.Progress.ToSerializable());
            var actualPlayerDiscovered = player.DiscoveredEnemies.Select(IdText).ToList();
            if (!string.Equals(actualProgressJson, expectedProgressJson, StringComparison.Ordinal))
                validationFailures.Add("isolated progress canonical state did not equal the exact expected normal-room mutation");
            if (!actualPlayerDiscovered.SequenceEqual(expectedPlayerDiscovered, StringComparer.Ordinal))
                validationFailures.Add("live replay player DiscoveredEnemies did not equal the exact expected sequence");
            if (TransactionAudit.CountEvents("isolated-manager", nameof(SaveManager.UpdateProgressAfterCombatWon)) != 1)
                validationFailures.Add("UpdateProgressAfterCombatWon did not hit the isolated manager exactly once");
            if (TransactionAudit.CountEvents("isolated-manager", nameof(SaveManager.SaveProgressFile)) != 0)
                validationFailures.Add("SaveProgressFile was observed before its explicit sentinel call");
            if (TransactionAudit.MemoryStoreCallsSnapshot().Any(x => IsWrite(x.Method)))
                validationFailures.Add("UpdateProgressAfterCombatWon unexpectedly wrote to the isolated store");
            ThrowIfFailures("Progress mutation validation failed", validationFailures);
            checkpoints.Add("exact-progress-and-player-mutation-validated");

            SaveManager.Instance.SaveProgressFile();
            if (_failClosedReason is not null)
                throw new InvalidOperationException("Fail-closed audit violation after progress save: " + _failClosedReason);
            var expectedPath = UserDataPathProvider.GetProfileDir(isolated.CurrentProfileId)
                .PathJoin(UserDataPathProvider.SavesDir)
                .PathJoin("progress.save");
            var writeCalls = TransactionAudit.MemoryStoreCallsSnapshot().Where(x => IsWrite(x.Method)).ToList();
            if (writeCalls.Count != 1 || writeCalls[0].Method != "WriteFile")
                validationFailures.Add($"isolated progress store expected one synchronous WriteFile; actual writes={writeCalls.Count}");
            var actualWritePath = writeCalls.Count == 1 && writeCalls[0].Args.Length > 0 ? writeCalls[0].Args[0] as string : null;
            if (!string.Equals(NormalizeStorePath(actualWritePath), NormalizeStorePath(expectedPath), StringComparison.OrdinalIgnoreCase))
                validationFailures.Add($"isolated progress write path mismatch: expected {expectedPath}, actual {actualWritePath}");
            if (TransactionAudit.CountEvents("isolated-manager", nameof(SaveManager.SaveProgressFile)) != 1)
                validationFailures.Add("SaveProgressFile did not hit the isolated manager exactly once");
            if (TransactionAudit.CountEvents("isolated-store", "WriteFile") != 1)
                validationFailures.Add("isolated store audit did not observe exactly one WriteFile");

            var payload = memoryStore.ReadFile(expectedPath)
                          ?? throw new InvalidDataException("Isolated progress.save payload was null.");
            var payloadProgress = ParseProgress(payload, "isolated progress.save payload");
            var payloadCanonical = JsonSerializationUtility.ToJson(payloadProgress);
            if (!string.Equals(payloadCanonical, actualProgressJson, StringComparison.Ordinal))
                validationFailures.Add("deserialized progress.save payload is not canonically equivalent to post-mutation progress");
            ValidateBoundaryCounts(validationFailures);
            ThrowIfFailures("Progress write validation failed", validationFailures);

            mutationEvidence = new
            {
                roomType = room.RoomType.ToString(),
                characterId = IdText(player.Character.Id),
                encounterId = IdText(encounter.Id),
                spawnedEnemyIds = spawnedEnemies.Select(x => IdText(x.Id)).ToList(),
                baselineCanonicalSha256 = JsonSupport.Sha256Text(baselineProgressJson),
                expectedCanonicalSha256 = JsonSupport.Sha256Text(expectedProgressJson),
                actualCanonicalSha256 = JsonSupport.Sha256Text(actualProgressJson),
                baselineTargets,
                expectedTargets,
                actualTargets = DescribeTargets(ParseProgress(actualProgressJson, "actual target snapshot"), player, encounter, spawnedEnemies),
                playerDiscoveredBefore,
                expectedPlayerDiscovered,
                actualPlayerDiscovered,
            };
            writeEvidence = new
            {
                expectedPath,
                actualWritePath,
                writeCount = writeCalls.Count,
                writeMethods = writeCalls.Select(x => x.Method).ToList(),
                payloadLength = payload.Length,
                payloadSha256 = JsonSupport.Sha256Text(payload),
                payloadDeserialized = true,
                payloadEquivalentToMutation = string.Equals(payloadCanonical, actualProgressJson, StringComparison.Ordinal),
            };
            checkpoints.Add("single-isolated-progress-write-and-payload-validated");
        }
        catch (Exception ex)
        {
            operationException = ex;
            ProbeController.ProbeError($"Direct-write transaction operation failed: {ex}");
        }
        finally
        {
            try
            {
                TransactionAudit.BeginRestore();
                checkpoints.Add("restore-begun-new-operations-forbidden");
                if (isolated is not null)
                    await ObserveSaveTask(isolated);
                if (RunManager.Instance?.DebugOnlyGetState() is not null)
                {
                    RunManager.Instance.CleanUp(true);
                    cleanupCalled = true;
                }
                if (isolated is not null)
                    await ObserveSaveTask(isolated);
                checkpoints.Add("isolated-task-drained-cleanup-once-isolated-task-redrained");

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
                continueVisibleAfterTeardown = ProbeController.IsContinueVisibleAndEnabled();
                if (!continueVisibleAfterTeardown)
                    throw new InvalidOperationException("Continue was not visibly enabled on the rebuilt main menu.");
                if (original is not null)
                {
                    originalProgressCanonicalAfter = JsonSerializationUtility.ToJson(original.Progress.ToSerializable());
                    if (!string.Equals(originalProgressCanonicalBefore, originalProgressCanonicalAfter, StringComparison.Ordinal))
                        throw new InvalidOperationException("Original manager progress object changed across the transaction.");
                }
                checkpoints.Add("main-menu-rebuilt-after-original-manager-restore");
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

            IReadOnlyList<FileIdentity> afterFiles = Array.Empty<FileIdentity>();
            DirectoryInventory? afterProfileInventory = null;
            DirectoryInventory? afterAccountInventory = null;
            var differences = new List<string>();
            try
            {
                var preflight = ProbeController.ReadPreflightRoot();
                afterFiles = ProbeController.CaptureProtectedFiles(preflight);
                afterProfileInventory = ProbeController.CaptureProfileInventory(preflight);
                afterAccountInventory = ProbeController.CaptureAccountTopLevelInventory(preflight);
                if (beforeFiles is not null) differences.AddRange(ProbeController.CompareFileIdentities(beforeFiles, afterFiles));
                if (beforeProfileInventory is not null) differences.AddRange(ProbeController.CompareInventories(beforeProfileInventory, afterProfileInventory, "profile"));
                if (beforeAccountInventory is not null) differences.AddRange(ProbeController.CompareInventories(beforeAccountInventory, afterAccountInventory, "account-top-level"));
            }
            catch (Exception ex)
            {
                differences.Add("post-transaction protection capture failed: " + ex.Message);
            }

            ValidateBoundaryCounts(differences);
            var passed = sceneLaunched && cleanupCalled && operationException is null && teardownException is null
                         && differences.Count == 0 && _failClosedReason is null
                         && original is not null && ReferenceEquals(SaveManager.Instance, original)
                         && RunManager.Instance?.DebugOnlyGetState() is null
                         && checkpoints.Contains("single-isolated-progress-write-and-payload-validated");
            var outputPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "direct-write-transaction.json");
            JsonSupport.WriteAtomic(outputPath, new
            {
                schemaVersion = ProbeConstants.SchemaVersion,
                probeVersion = ProbeConstants.ProbeVersion,
                startedUtc = started.ToString("O"),
                finishedUtc = DateTimeOffset.UtcNow.ToString("O"),
                outcome = passed ? "PASS" : "FAIL",
                limitation = "Only direct production progress-update/save routing in this fixed isolated session; no replay events, natural victory, achievement helper, replay fidelity, reconstruction, or complete session isolation were tested.",
                sceneLaunched,
                replayEventsReadByProbe = false,
                replayEventsDispatched = false,
                endCombatInternalCalledByProbe = false,
                achievementHelperCalledByProbe = false,
                cleanupCalled,
                failClosedReason = _failClosedReason,
                checkpoints,
                validationFailures,
                operationException = operationException?.ToString(),
                teardownException = teardownException?.ToString(),
                originalManagerRestored = original is not null && ReferenceEquals(SaveManager.Instance, original),
                runManagerStateIsNull = RunManager.Instance?.DebugOnlyGetState() is null,
                continueVisibleAfterTeardown,
                originalProgressCanonicalBeforeSha256 = originalProgressCanonicalBefore is null ? null : JsonSupport.Sha256Text(originalProgressCanonicalBefore),
                originalProgressCanonicalAfterSha256 = originalProgressCanonicalAfter is null ? null : JsonSupport.Sha256Text(originalProgressCanonicalAfter),
                mutation = mutationEvidence,
                write = writeEvidence,
                differences,
                beforeFiles,
                afterFiles,
                beforeProfileInventory,
                afterProfileInventory,
                beforeAccountInventory,
                afterAccountInventory,
                audit = TransactionAudit.Snapshot(),
            });
            TransactionAudit.Stop();
            _active = false;
            Gate.Release();
            if (passed) ProbeController.ProbeLog($"Direct-write sentinel PASS. Evidence: {outputPath}");
            else ProbeController.ProbeError($"Direct-write sentinel FAIL. Evidence: {outputPath}");
        }
    }

    private static void ValidateGates(JsonElement preflight)
    {
        if (!string.Equals(ReadControlPhase(), "DIRECT_WRITE", StringComparison.Ordinal))
            throw new InvalidOperationException("Direct-write runtime is locked pending Phase 0 acceptance.");
        if (preflight.GetProperty("probeVersion").GetString() != ProbeConstants.ProbeVersion)
            throw new InvalidOperationException("F8 must be rerun with the installed SPIKE-INV004A build.");
        if (preflight.GetProperty("blockingReasons").GetArrayLength() != 0)
            throw new InvalidOperationException("Phase 0 contains blocking reasons.");
        if (RunManager.Instance?.DebugOnlyGetState() is not null)
            throw new InvalidOperationException("RunManager.State must be null before transaction start.");
        var validationPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "fixture-validation.json");
        using var validation = JsonDocument.Parse(File.ReadAllText(validationPath));
        var fixture = validation.RootElement.GetProperty("fixture");
        if (validation.RootElement.GetProperty("probeVersion").GetString() != ProbeConstants.ProbeVersion
            || validation.RootElement.GetProperty("outcome").GetString() != "VALID_NATIVE_FIXTURE"
            || fixture.GetProperty("sha256").GetString() != ProbeConstants.ExpectedFixtureSha256
            || fixture.GetProperty("length").GetInt32() != ProbeConstants.ExpectedFixtureLength)
            throw new InvalidOperationException("F9 must validate the fixed native fixture with this installed build.");
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

    private static List<string> CompareInventoryToPreflight(JsonElement preflight, DirectoryInventory current, string property)
    {
        var key = property == "profile" ? "profileInventory" : "accountTopLevelInventory";
        var root = preflight.GetProperty(key);
        var expected = new DirectoryInventory(
            root.GetProperty("root").GetString() ?? "",
            root.GetProperty("directories").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
            root.GetProperty("files").EnumerateArray().Select(x => x.GetString() ?? "").ToList());
        return ProbeController.CompareInventories(expected, current, property);
    }

    private static (SaveManager Manager, MockGodotFileIo Store) CreateIsolatedManager(SaveManager original, JsonElement preflight)
    {
        var store = new MockGodotFileIo("user://spike-inv004a-memory") { getCurrentTime = () => DateTimeOffset.UtcNow };
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
            throw new InvalidDataException($"Isolated initialization failed: settings={settings.Status}, prefs={prefs.Status}, progress={progress.Status}");
        if (ReferenceEquals(ReadProfileState(manager), ReadProfileState(original))
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
        var field = typeof(SaveManager).GetField("_profileSaveManager", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(SaveManager).FullName, "_profileSaveManager");
        var child = field.GetValue(manager) ?? throw new InvalidOperationException("ProfileSaveManager is null.");
        var property = child.GetType().GetProperty("Profile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(child.GetType().FullName, "Profile");
        return property.GetValue(child) ?? throw new InvalidOperationException("ProfileSave state is null.");
    }

    private static CombatReplay ReadFixture()
    {
        var bytes = File.ReadAllBytes(ProbeConstants.ExpectedFixturePath);
        if (bytes.Length != ProbeConstants.ExpectedFixtureLength
            || Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) != ProbeConstants.ExpectedFixtureSha256)
            throw new InvalidDataException("Fixed fixture identity changed at transaction start.");
        var reader = new PacketReader();
        reader.Reset(bytes);
        var replay = reader.Read<CombatReplay>();
        if (replay.version != ProbeConstants.ExpectedGameVersion || replay.gitCommit != ProbeConstants.ExpectedCommit)
            throw new InvalidDataException("Fixed replay version/commit gate failed.");
        return replay;
    }

    private static SerializableProgress ParseProgress(string json, string label)
    {
        var result = JsonSerializationUtility.FromJson<SerializableProgress>(json);
        if (!result.Success || result.SaveData is null)
            throw new InvalidDataException($"Could not deserialize {label}: status={result.Status}, error={result.ErrorMessage}");
        return result.SaveData;
    }

    private static void ApplyExpectedNormalRoomMutation(
        SerializableProgress progress,
        Player player,
        EncounterModel encounter,
        IReadOnlyList<MonsterModel> spawnedEnemies,
        List<string> expectedPlayerDiscovered)
    {
        var encounterStats = progress.EncounterStats.FirstOrDefault(x => x.Id.Equals(encounter.Id));
        if (encounterStats is null)
        {
            encounterStats = new EncounterStats { Id = encounter.Id, FightStats = new List<FightStats>() };
            progress.EncounterStats.Add(encounterStats);
        }
        IncrementOrAdd(encounterStats.FightStats, player.Character.Id);

        foreach (var enemy in spawnedEnemies)
        {
            var enemyStats = progress.EnemyStats.FirstOrDefault(x => x.Id.Equals(enemy.Id));
            if (enemyStats is null)
            {
                enemyStats = new EnemyStats { Id = enemy.Id, FightStats = new List<FightStats>() };
                progress.EnemyStats.Add(enemyStats);
            }
            var wasCompletelyEmpty = enemyStats.FightStats.Count == 0;
            IncrementOrAdd(enemyStats.FightStats, player.Character.Id);
            if (wasCompletelyEmpty)
                expectedPlayerDiscovered.Add(IdText(enemy.Id));
        }
    }

    private static void IncrementOrAdd(List<FightStats> stats, ModelId characterId)
    {
        var item = stats.FirstOrDefault(x => x.Character.Equals(characterId));
        if (item is null)
            stats.Add(new FightStats { Character = characterId, Wins = 1, Losses = 0 });
        else
            item.Wins++;
    }

    private static object DescribeTargets(SerializableProgress progress, Player player, EncounterModel encounter, IReadOnlyList<MonsterModel> enemies)
    {
        var encounterStats = progress.EncounterStats.FirstOrDefault(x => x.Id.Equals(encounter.Id));
        var encounterFight = encounterStats?.FightStats.FirstOrDefault(x => x.Character.Equals(player.Character.Id));
        var enemyRows = enemies.GroupBy(x => IdText(x.Id), StringComparer.Ordinal)
            .Select(group =>
            {
                var id = group.First().Id;
                var stats = progress.EnemyStats.FirstOrDefault(x => x.Id.Equals(id));
                var fight = stats?.FightStats.FirstOrDefault(x => x.Character.Equals(player.Character.Id));
                return new
                {
                    enemyId = group.Key,
                    spawnedCount = group.Count(),
                    totalFightStatRows = stats?.FightStats.Count ?? 0,
                    characterWins = fight?.Wins,
                    characterLosses = fight?.Losses,
                };
            }).ToList();
        return new
        {
            encounterId = IdText(encounter.Id),
            encounterTotalFightStatRows = encounterStats?.FightStats.Count ?? 0,
            encounterCharacterWins = encounterFight?.Wins,
            encounterCharacterLosses = encounterFight?.Losses,
            enemies = enemyRows,
        };
    }

    private static void ValidateBoundaryCounts(List<string> failures)
    {
        var events = TransactionAudit.EventsSnapshot();
        foreach (var boundary in new[] { "original-manager", "original-store", "unknown-manager", "unknown-store", "external-submit" })
            if (events.Any(x => x.Boundary == boundary)) failures.Add($"forbidden audit boundary observed: {boundary}");
        var isolatedManagerMethods = events.Where(x => x.Boundary == "isolated-manager").Select(x => x.Method).ToList();
        if (isolatedManagerMethods.Count(x => x == nameof(SaveManager.UpdateProgressAfterCombatWon)) != 1
            || isolatedManagerMethods.Count(x => x == nameof(SaveManager.SaveProgressFile)) != 1
            || isolatedManagerMethods.Any(x => x != nameof(SaveManager.UpdateProgressAfterCombatWon) && x != nameof(SaveManager.SaveProgressFile)))
            failures.Add("isolated manager audit calls were not exactly one update followed by one save");
        var writes = TransactionAudit.MemoryStoreCallsSnapshot().Where(x => IsWrite(x.Method)).ToList();
        if (writes.Count != 1 || writes[0].Method != "WriteFile")
            failures.Add("isolated store writes were not exactly one synchronous WriteFile");
    }

    private static bool IsWrite(string method) => method is "WriteFile" or "WriteFileAsync";
    private static string IdText(ModelId id) => id.ToString();
    private static string NormalizeStorePath(string? path) => (path ?? "").Replace('\\', '/').TrimStart('/');

    private static void ThrowIfFailures(string prefix, List<string> failures)
    {
        if (failures.Count != 0) throw new InvalidOperationException(prefix + ": " + string.Join("; ", failures));
    }

    private static async Task ObserveSaveTask(SaveManager manager)
    {
        var task = manager.CurrentRunSaveTask;
        if (task is not null) await task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
