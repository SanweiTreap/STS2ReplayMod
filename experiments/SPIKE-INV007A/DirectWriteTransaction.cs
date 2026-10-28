using System.Reflection;
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
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv007A;

internal sealed class StopRunException(TerminalClassification classification, FailureSite site) : Exception
{
    internal TerminalClassification Classification { get; } = classification;
    internal FailureSite Site { get; } = site;
}

internal static class DirectWriteTransaction
{
    private static readonly PropertyInfo ShouldSaveProperty = typeof(RunManager).GetProperty(nameof(RunManager.ShouldSave), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMemberException(nameof(RunManager.ShouldSave));
    private static readonly MethodInfo LoadMainMenuMethod = typeof(NGame).GetMethod("LoadMainMenu", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMemberException("LoadMainMenu");
    private static int _started, _teardownOwner, _boundaryFailure;

    internal static bool Active => Volatile.Read(ref _started) != 0;
    internal static void SignalBoundaryFailure(BoundaryFailure failure) => Interlocked.CompareExchange(ref _boundaryFailure, (int)failure, 0);
    internal static void Start(SaveManager original) => TaskHelper.RunSafely(RunAsync(original));

    private static async Task RunAsync(SaveManager original)
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;
        SaveManager? candidate = null;
        MockGodotFileIo? memory = null;
        ProtectionRoots? roots = null;
        ProtectionSnapshot? before = null;
        IReadOnlyList<ProtectedFile>? afterFiles = null;
        CandidateEvidence? candidateEvidence = null;
        OracleEvidence? oracleEvidence = null;
        SentinelEvidence? sentinel = null;
        AuditSnapshot? audit = null;
        TeardownStage stage = TeardownStage.None;
        var cleanupRequired = false;
        var cleanupCount = 0;
        var originalRestored = false;
        var clearRestoredOriginal = false;
        var stateNull = false;
        var continueVisible = false;
        var protectionUnchanged = false;
        var classification = TerminalClassification.BlockedPrecondition;
        var site = FailureSite.ProtectionBaseline;
        var overrideInstalled = false;
        var originalCanonical = CaptureOriginalCanonical(original);

        try
        {
            (roots, before) = ProtectionScope.CaptureAndBackup(original);
            ProbeController.CommitTransactionEvent(EventKind.BaselineReady);
            ThrowBoundary();

            var live = LiveSeedCandidate.CaptureLive(original);
            var built = LiveSeedCandidate.CreateLiveCandidate(live);
            candidate = built.Manager;
            memory = built.Store;
            var components = built.Components.ToDictionary(x => x.Component, StringComparer.Ordinal);
            var referencesIndependent = built.References.ManagerIndependent && built.References.SettingsIndependent &&
                                        built.References.PrefsIndependent && built.References.ProgressIndependent &&
                                        !ReferenceEquals(live.Profile, ReadProfile(candidate));
            var initMutators = built.StoreAudit.Any(x => x.Phase != "seed-live-serialized" && IsMutator(x.Method));
            candidateEvidence = new CandidateEvidence(
                candidate.CurrentProfileId == 1,
                components["Profile"].RawEqual, components["Profile"].CanonicalEqual,
                components["Settings"].RawEqual, components["Settings"].CanonicalEqual,
                components["Prefs"].RawEqual, components["Prefs"].CanonicalEqual,
                components["Progress"].RawEqual, components["Progress"].CanonicalEqual,
                referencesIndependent, built.References.SharedMutableReferences.Count,
                built.StoreContainmentPassed, !initMutators,
                originalCanonical.SequenceEqual(CaptureOriginalCanonical(original), StringComparer.Ordinal));
            if (!AllCandidate(candidateEvidence))
                throw new StopRunException(TerminalClassification.FailPreoverrideEquivalence, FailureSite.Candidate);
            if (!ProgressSubsystemReady(candidate, memory))
                throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.ProgressSubsystemUnavailable);
            if (!ProbeController.RevalidateTimingGate(original))
                throw new StopRunException(TerminalClassification.StopMissingTimingGate, FailureSite.GateEvaluation);
            ProbeController.CommitTransactionEvent(EventKind.CandidateReady);

            memory.Calls.Clear();
            BoundaryAudit.Start(original, candidate, memory);
            SaveManager.MockInstanceForTesting(candidate);
            overrideInstalled = true;
            RequireCandidate(candidate);
            ProbeController.CommitTransactionEvent(EventKind.OverrideInstalled);
            ThrowBoundary();

            var replay = ReadFixture();
            RunState runState;
            try { runState = RunState.FromSerializable(replay.serializableRun); }
            catch { throw new StopRunException(TerminalClassification.BlockedInstrumentation, FailureSite.RunCallback); }
            if (runState.Players.Count != 1)
                throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.FixturePlayerCardinality);
            var player = runState.Players[0];
            ProbeController.CommitTransactionEvent(EventKind.RunStateParsed);

            RunSetup(() => RunManager.Instance.SetUpReplay(runState, replay, player.NetId), FailureSite.ReplaySetupFailed);
            SetShouldSaveFalse();
            RunSetup(() => RunManager.Instance.CombatStateSynchronizer.IsDisabled = true, FailureSite.ReplaySetupFailed);
            ProbeController.CommitTransactionEvent(EventKind.ReplaySetupComplete);
            ThrowBoundary();

            await AwaitSetup(() => PreloadManager.LoadRunAssets(runState.Players.Select(p => p.Character)), FailureSite.SceneLoadFailed);
            ThrowBoundary();
            await AwaitSetup(() => PreloadManager.LoadActAssets(runState.Act), FailureSite.SceneLoadFailed);
            ThrowBoundary();
            RunSetup(() => RunManager.Instance.Launch(), FailureSite.SceneLoadFailed);
            NAudioManager.Instance?.StopMusic();
            var game = NGame.Instance;
            if (game is null)
                throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.NGameUnavailable);
            RunSetup(() => game.RootSceneContainer.SetCurrentScene(NRun.Create(runState)), FailureSite.SceneLoadFailed);
            await AwaitSetup(() => RunManager.Instance.GenerateMap(), FailureSite.SceneLoadFailed);
            ThrowBoundary();
            RunSetup(() =>
            {
                RunManager.Instance.ActionQueueSet.FastForwardNextActionId(replay.nextActionId);
                RunManager.Instance.ActionQueueSynchronizer.FastForwardHookId(replay.nextHookId);
                RunManager.Instance.ChecksumTracker.LoadReplayChecksums(replay.checksumData, replay.nextChecksumId);
                RunManager.Instance.PlayerChoiceSynchronizer.FastForwardChoiceIds(replay.choiceIds);
                RunManager.Instance.RewardsSetSynchronizer.FastForwardRewardIds(replay.rewardIds);
            }, FailureSite.SceneLoadFailed);
            await AwaitSetup(() => RunManager.Instance.LoadIntoLatestMapCoord(
                AbstractRoom.FromSerializable(replay.serializableRun.PreFinishedRoom, runState)), FailureSite.SceneLoadFailed);
            ThrowBoundary();
            ProbeController.CommitTransactionEvent(EventKind.SceneLoadComplete);

            RequireCandidate(candidate);
            if (!ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), runState))
                throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunStateBindingLost);
            var room = ReadReference(() => runState.CurrentRoom as CombatRoom, FailureSite.CurrentRoomNotCombat);
            var combatState = ReadReference(() => room.CombatState, FailureSite.CombatStateUnavailable);
            var encounter = ReadReference(() => combatState.Encounter, FailureSite.EncounterUnavailable);
            var character = ReadReference(() => player.Character, FailureSite.CharacterUnavailable);
            var characterId = ReadId(character.Id, FailureSite.CharacterUnavailable);
            var encounterId = ReadId(encounter.Id, FailureSite.EncounterUnavailable);
            var discoveredBaseline = ReadDiscovered(player);
            var enemies = ReadEnemies(encounter);
            var enemyIds = new List<ModelId>(enemies.Count);
            foreach (var enemy in enemies)
            {
                if (enemy is null)
                    throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.EnemyEntryInvalid);
                enemyIds.Add(ReadId(enemy.Id, FailureSite.EnemyEntryInvalid));
            }
            RoomType roomType;
            try { roomType = room.RoomType; }
            catch { throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.UnsupportedRoomType); }
            if (roomType != RoomType.Monster)
                throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.UnsupportedRoomType);
            if (!ProgressSubsystemReady(candidate, memory))
                throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.ProgressSubsystemUnavailable);
            ProbeController.CommitTransactionEvent(EventKind.DirectArgumentsReady);
            ThrowBoundary();

            DetachedOracleSnapshot oracle;
            try { oracle = DetachedMutationOracle.Capture(candidate); }
            catch (OracleParseException)
            {
                oracleEvidence = new OracleEvidence(false, 0, false, false, false, false);
                throw new StopRunException(TerminalClassification.BlockedOracleParse, FailureSite.OracleParse);
            }
            var discoveredExpected = new HashSet<string>(discoveredBaseline, StringComparer.Ordinal);
            if (oracle.SharedMutableReferences != 0)
            {
                oracleEvidence = new OracleEvidence(true, oracle.SharedMutableReferences, false,
                    false, false, false);
                throw new StopRunException(TerminalClassification.BlockedOracleAliasing, FailureSite.OracleAliasing);
            }
            DetachedMutationOracle.ApplyExpected(oracle.Expected, characterId, encounterId, enemyIds, discoveredExpected);
            var candidateAfterOracle = LiveSeedCandidate.CanonicalProgress(candidate);
            var discoveredAfterOracle = ReadDiscovered(player);
            var candidateUnchanged = string.Equals(candidateAfterOracle, oracle.BaselineCanonical, StringComparison.Ordinal);
            var discoveredUnchanged = discoveredAfterOracle.SetEquals(discoveredBaseline);
            var precallAudit = BoundaryAudit.Snapshot();
            var precallAuditZero = precallAudit.CandidateUpdateCount == 0 && precallAudit.CandidateSaveCount == 0
                                   && precallAudit.CandidateWriteCount == 0;
            oracleEvidence = new OracleEvidence(true, oracle.SharedMutableReferences, true,
                candidateUnchanged, discoveredUnchanged, precallAuditZero);
            if (!DetachedMutationOracle.AdmissionPassed(true, oracle.SharedMutableReferences,
                    candidateUnchanged, discoveredUnchanged, precallAudit))
                throw new StopRunException(TerminalClassification.BlockedOracleAliasing, FailureSite.OracleAliasing);

            var expected = oracle.Expected;
            var expectedCanonical = JsonSupport.Canonicalize(JsonSerializationUtility.ToJson(expected));
            var expectedHash = JsonSupport.Sha256Text(expectedCanonical);
            var noWriteBefore = BoundaryAudit.Snapshot().CandidateWriteCount == 0 && memory.Calls.All(x => !IsMutator(x.Item1));

            RequireCandidate(candidate);
            ThrowBoundary();
            SaveManager.Instance.UpdateProgressAfterCombatWon(player, room);
            ThrowBoundary();

            var mutationAudit = BoundaryAudit.Snapshot();
            string actualCanonical;
            HashSet<string> discoveredActual;
            try
            {
                actualCanonical = LiveSeedCandidate.CanonicalProgress(candidate);
                discoveredActual = ReadDiscovered(player);
            }
            catch
            {
                throw new StopRunException(TerminalClassification.FailMutation, FailureSite.Mutation);
            }
            var actualHash = JsonSupport.Sha256Text(actualCanonical);
            var mutationEqual = string.Equals(actualCanonical, expectedCanonical, StringComparison.Ordinal);
            var discoveredEqual = discoveredActual.SetEquals(discoveredExpected);
            var noWriteAfterUpdate = mutationAudit.CandidateWriteCount == 0 && memory.Calls.All(x => !IsMutator(x.Item1));
            sentinel = Evidence(expectedHash, actualHash, mutationEqual, discoveredEqual, noWriteBefore, noWriteAfterUpdate, mutationAudit);
            if (!ContractRules.MutationPassed(sentinel))
                throw new StopRunException(TerminalClassification.FailMutation, FailureSite.Mutation);
            ProbeController.CommitTransactionEvent(EventKind.ProgressUpdated);

            BoundaryAudit.SetExpectedPayloadCanonical(expectedCanonical);
            RequireCandidate(candidate);
            ThrowBoundary();
            SaveManager.Instance.SaveProgressFile();
            ThrowBoundary();

            audit = BoundaryAudit.Snapshot();
            sentinel = Evidence(expectedHash, actualHash, mutationEqual, discoveredEqual, noWriteBefore, noWriteAfterUpdate, audit);
            if (!ContractRules.WritePassed(sentinel))
                throw new StopRunException(TerminalClassification.FailWrite, FailureSite.Write);
            ProbeController.CommitTransactionEvent(EventKind.ProgressSaved);
            classification = TerminalClassification.PassDirectWriteIsolated;
            site = FailureSite.None;
        }
        catch (StopRunException stop)
        {
            classification = stop.Classification;
            site = stop.Site;
        }
        catch
        {
            classification = TerminalClassification.BlockedInstrumentation;
            site = FailureSite.RunCallback;
        }
        finally
        {
            if (Interlocked.CompareExchange(ref _teardownOwner, 1, 0) == 0)
            {
                try
                {
                    stage = TeardownStage.ActionsDisabled;
                    ProbeController.CommitTransactionEvent(EventKind.TeardownStarted);
                    stage = TeardownStage.IsolatedTaskWait1;
                    if (candidate is not null) await WaitTask(candidate);
                    stage = TeardownStage.Cleanup;
                    cleanupRequired = RunManager.Instance?.DebugOnlyGetState() is not null;
                    if (cleanupRequired)
                    {
                        RunManager.Instance!.CleanUp(true);
                        cleanupCount++;
                    }
                    stage = TeardownStage.IsolatedTaskWait2;
                    if (candidate is not null) await WaitTask(candidate);
                    stage = TeardownStage.OverrideCleared;
                    if (overrideInstalled) SaveManager.ClearInstanceForTesting();
                    clearRestoredOriginal = !overrideInstalled || ReferenceEquals(SaveManager.Instance, original);
                    originalRestored = clearRestoredOriginal;
                    if (!originalRestored)
                    {
                        SaveManager.MockInstanceForTesting(original);
                        originalRestored = ReferenceEquals(SaveManager.Instance, original);
                    }
                    if (!originalRestored) throw new InvalidOperationException();
                    ProbeController.CommitTransactionEvent(EventKind.ManagerRestored);
                    stage = TeardownStage.OriginalTaskWait;
                    await WaitTask(original);
                    stage = TeardownStage.MainMenuLoad;
                    var game = NGame.Instance ?? throw new InvalidOperationException();
                    await ((Task?)LoadMainMenuMethod.Invoke(game, new object[] { false }) ?? throw new InvalidOperationException());
                    await game.GetTree().Root.AwaitProcessFrame();
                    await game.GetTree().Root.AwaitProcessFrame();
                    stage = TeardownStage.MenuReady;
                    stateNull = RunManager.Instance?.DebugOnlyGetState() is null;
                    continueVisible = ProbeController.IsContinueVisibleAndEnabled();
                    ProbeController.CommitTransactionEvent(EventKind.MainMenuRestored);
                    stage = TeardownStage.ProtectionCheck;
                    if (roots is not null && before is not null)
                    {
                        var after = ProtectionScope.Capture(roots);
                        afterFiles = after.Files;
                        protectionUnchanged = ProtectionScope.Equivalent(before, after);
                    }
                    if (protectionUnchanged) ProbeController.CommitTransactionEvent(EventKind.ProtectionVerified);
                }
                catch
                {
                    if (classification == TerminalClassification.PassDirectWriteIsolated)
                    {
                        classification = TerminalClassification.FailTeardown;
                        site = FailureSite.Cleanup;
                    }
                }
            }

            audit = BoundaryAudit.SealAndSnapshot();
            if (audit.FirstFailure != BoundaryFailure.None && classification == TerminalClassification.PassDirectWriteIsolated)
            {
                classification = TerminalClassification.FailBoundary;
                site = FailureSite.Boundary;
            }
            if (!protectionUnchanged && before is not null)
            {
                classification = TerminalClassification.FailSafety;
                site = FailureSite.ProtectionAfter;
            }
            else if (classification == TerminalClassification.PassDirectWriteIsolated &&
                     (!clearRestoredOriginal || !originalRestored || !stateNull || !continueVisible ||
                      cleanupCount != (cleanupRequired ? 1 : 0)))
            {
                classification = TerminalClassification.FailTeardown;
                site = FailureSite.Cleanup;
            }

            var teardown = new TeardownEvidence(stage, cleanupRequired, cleanupCount == (cleanupRequired ? 1 : 0),
                clearRestoredOriginal, originalRestored, stateNull, continueVisible, protectionUnchanged);
            ProbeController.Complete(classification, site, before, afterFiles, candidateEvidence, oracleEvidence,
                sentinel, audit, teardown);
        }
    }

    private static SentinelEvidence Evidence(string expectedHash, string actualHash, bool mutationEqual,
        bool discoveredEqual, bool noWriteBefore, bool noWriteAfter, AuditSnapshot audit)
        => new(true, expectedHash, actualHash, mutationEqual, discoveredEqual, noWriteBefore, noWriteAfter,
            audit.CandidateUpdateCount, audit.CandidateSaveCount, audit.CandidateWriteCount,
            audit.CandidateWriteSynchronous, audit.CandidateWriteExactLogicalName,
            audit.CandidatePayloadDeserialized, audit.CandidatePayloadEquivalent);

    private static void SetShouldSaveFalse()
    {
        try
        {
            ShouldSaveProperty.SetValue(RunManager.Instance, false);
            if (RunManager.Instance.ShouldSave)
                throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.ShouldSaveDisableFailed);
        }
        catch (StopRunException) { throw; }
        catch { throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.ShouldSaveDisableFailed); }
    }

    private static void RequireCandidate(SaveManager candidate)
    {
        if (!ReferenceEquals(SaveManager.Instance, candidate))
            throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.ManagerOverrideLost);
    }

    private static T ReadReference<T>(Func<T?> reader, FailureSite site) where T : class
    {
        try { return reader() ?? throw new StopRunException(TerminalClassification.BlockedPrecondition, site); }
        catch (StopRunException) { throw; }
        catch { throw new StopRunException(TerminalClassification.BlockedPrecondition, site); }
    }

    private static ModelId ReadId(ModelId id, FailureSite site)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id.ToString()))
                throw new StopRunException(TerminalClassification.BlockedPrecondition, site);
            return id;
        }
        catch (StopRunException) { throw; }
        catch { throw new StopRunException(TerminalClassification.BlockedPrecondition, site); }
    }

    private static HashSet<string> ReadDiscovered(Player player)
    {
        try
        {
            var discovered = player.DiscoveredEnemies;
            if (discovered is null)
                throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.DiscoveredEnemiesUnavailable);
            return discovered.Select(id => ReadId(id, FailureSite.DiscoveredEnemiesUnavailable).ToString())
                .ToHashSet(StringComparer.Ordinal);
        }
        catch (StopRunException) { throw; }
        catch { throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.DiscoveredEnemiesUnavailable); }
    }

    private static List<MonsterModel> ReadEnemies(EncounterModel encounter)
    {
        try { return encounter.SpawnedEnemies.ToList(); }
        catch { throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.EnemyListUnreadable); }
    }

    private static void RunSetup(Action action, FailureSite site)
    {
        try { action(); }
        catch (StopRunException) { throw; }
        catch { throw new StopRunException(TerminalClassification.BlockedPrecondition, site); }
    }

    private static async Task AwaitSetup(Func<Task> start, FailureSite site)
    {
        try { await start(); }
        catch { throw new StopRunException(TerminalClassification.BlockedPrecondition, site); }
    }

    private static void ThrowBoundary()
    {
        if (Volatile.Read(ref _boundaryFailure) != 0)
            throw new StopRunException(TerminalClassification.FailBoundary, FailureSite.Boundary);
    }

    private static bool AllCandidate(CandidateEvidence x) => x.ProfileIsOne && x.ProfileRawEqual &&
        x.ProfileCanonicalEqual && x.SettingsRawEqual && x.SettingsCanonicalEqual && x.PrefsRawEqual &&
        x.PrefsCanonicalEqual && x.ProgressRawEqual && x.ProgressCanonicalEqual && x.RootsIndependent &&
        x.SharedMutableReferences == 0 && x.StoreContained && x.InitializationMemoryOnly &&
        x.OriginalCanonicalUnchanged;

    private static bool IsMutator(string name) => name is "WriteFile" or "WriteFileAsync" or "DeleteFile" or
        "DeleteDirectory" or "RenameFile" or "BeginSaveBatch" or "EndSaveBatch" or "SetLastModifiedTime";

    private static string[] CaptureOriginalCanonical(SaveManager manager)
    {
        var live = LiveSeedCandidate.CaptureLive(manager);
        return [JsonSupport.Canonicalize(live.ProfileJson), JsonSupport.Canonicalize(live.SettingsJson),
            JsonSupport.Canonicalize(live.PrefsJson), JsonSupport.Canonicalize(live.ProgressJson)];
    }

    private static object ReadProfile(SaveManager manager)
    {
        var field = typeof(SaveManager).GetField("_profileSaveManager", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var child = field.GetValue(manager)!;
        return child.GetType().GetProperty("Profile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(child)!;
    }

    private static bool ProgressSubsystemReady(SaveManager manager, MockGodotFileIo memory)
    {
        try
        {
            var progressManager = typeof(SaveManager).GetField("_progressSaveManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(manager);
            var migration = typeof(SaveManager).GetField("_migrationManager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(manager);
            if (progressManager is null || migration is null || manager.Progress is null || manager.CurrentProfileId != 1) return false;
            var stores = progressManager.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => typeof(ISaveStore).IsAssignableFrom(field.FieldType)).Select(field => field.GetValue(progressManager)).ToList();
            var provider = progressManager.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(field => field.Name.Contains("profileIdProvider", StringComparison.OrdinalIgnoreCase))?.GetValue(progressManager);
            return stores.Count == 1 && ReferenceEquals(stores[0], memory) && provider is not null &&
                   string.Equals(ProgressSaveManager.GetProgressPathForProfile(manager.CurrentProfileId, null)
                       .Replace('\\', '/').TrimStart('/'), ProbeConstants.ProgressLogicalName, StringComparison.Ordinal);
        }
        catch { return false; }
    }

    private static CombatReplay ReadFixture()
    {
        var bytes = File.ReadAllBytes(ProbeConstants.FixturePath);
        if (bytes.Length != ProbeConstants.FixtureLength ||
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) != ProbeConstants.FixtureSha256)
            throw new StopRunException(TerminalClassification.BlockedIdentity, FailureSite.Identity);
        var reader = new PacketReader();
        reader.Reset(bytes);
        var replay = reader.Read<CombatReplay>();
        if (replay.version != ProbeConstants.GameVersion || replay.gitCommit != ProbeConstants.GameCommit)
            throw new StopRunException(TerminalClassification.BlockedIdentity, FailureSite.Identity);
        return replay;
    }

    private static async Task WaitTask(SaveManager manager)
    {
        var task = manager.CurrentRunSaveTask;
        if (task is not null) await task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
