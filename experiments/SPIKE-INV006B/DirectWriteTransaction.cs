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
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv006B;

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
        SentinelEvidence? sentinel = null;
        AuditSnapshot? audit = null;
        TeardownStage stage = TeardownStage.None;
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
            candidate = built.Manager; memory = built.Store;
            var afterCandidateCanonical = CaptureOriginalCanonical(original);
            var components = built.Components.ToDictionary(x => x.Component, StringComparer.Ordinal);
            var rootsIndependent = built.References.ManagerIndependent && built.References.SettingsIndependent && built.References.PrefsIndependent && built.References.ProgressIndependent && !ReferenceEquals(live.Profile, ReadProfile(candidate));
            var initMutators = built.StoreAudit.Any(x => x.Phase != "seed-live-serialized" && IsMutator(x.Method));
            candidateEvidence = new CandidateEvidence(candidate.CurrentProfileId == 1,
                components["Profile"].RawEqual, components["Profile"].CanonicalEqual,
                components["Settings"].RawEqual, components["Settings"].CanonicalEqual,
                components["Prefs"].RawEqual, components["Prefs"].CanonicalEqual,
                components["Progress"].RawEqual, components["Progress"].CanonicalEqual,
                rootsIndependent, built.References.SharedMutableReferences.Count,
                built.StoreContainmentPassed, !initMutators,
                originalCanonical.SequenceEqual(afterCandidateCanonical, StringComparer.Ordinal));
            if (!AllCandidate(candidateEvidence)) throw new StopRunException(TerminalClassification.FailPreoverrideEquivalence, FailureSite.Candidate);
            if (!ProbeController.RevalidateTimingGate(original)) throw new StopRunException(TerminalClassification.StopMissingTimingGate, FailureSite.GateEvaluation);
            ProbeController.CommitTransactionEvent(EventKind.CandidateReady);

            memory.Calls.Clear();
            BoundaryAudit.Start(original, candidate, memory);
            SaveManager.MockInstanceForTesting(candidate); overrideInstalled = true;
            if (!ReferenceEquals(SaveManager.Instance, candidate)) throw new StopRunException(TerminalClassification.BlockedInstrumentation, FailureSite.ManagerIdentity);
            ProbeController.CommitTransactionEvent(EventKind.OverrideInstalled);
            ThrowBoundary();

            var replay = ReadFixture();
            var runState = RunState.FromSerializable(replay.serializableRun);
            if (runState.Players.Count != 1) throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            RunManager.Instance.SetUpReplay(runState, replay, runState.Players[0].NetId);
            ShouldSaveProperty.SetValue(RunManager.Instance, false);
            if (RunManager.Instance.ShouldSave) throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            RunManager.Instance.CombatStateSynchronizer.IsDisabled = true;
            await PreloadManager.LoadRunAssets(runState.Players.Select(p => p.Character)); ThrowBoundary();
            await PreloadManager.LoadActAssets(runState.Act); ThrowBoundary();
            RunManager.Instance.Launch();
            NAudioManager.Instance?.StopMusic();
            var game = NGame.Instance ?? throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            game.RootSceneContainer.SetCurrentScene(NRun.Create(runState));
            await RunManager.Instance.GenerateMap(); ThrowBoundary();
            RunManager.Instance.ActionQueueSet.FastForwardNextActionId(replay.nextActionId);
            RunManager.Instance.ActionQueueSynchronizer.FastForwardHookId(replay.nextHookId);
            RunManager.Instance.ChecksumTracker.LoadReplayChecksums(replay.checksumData, replay.nextChecksumId);
            RunManager.Instance.PlayerChoiceSynchronizer.FastForwardChoiceIds(replay.choiceIds);
            RunManager.Instance.RewardsSetSynchronizer.FastForwardRewardIds(replay.rewardIds);
            await RunManager.Instance.LoadIntoLatestMapCoord(AbstractRoom.FromSerializable(replay.serializableRun.PreFinishedRoom, runState)); ThrowBoundary();
            while (RunManager.Instance.ActionExecutor.IsPaused) { await game.GetTree().Root.AwaitProcessFrame(); ThrowBoundary(); }
            await game.GetTree().Root.AwaitProcessFrame(); ThrowBoundary();

            if (!ReferenceEquals(SaveManager.Instance, candidate) || !ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), runState)) throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            var player = runState.Players.Single();
            var room = runState.CurrentRoom as CombatRoom ?? throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            var encounter = room.Encounter ?? throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            var combatEncounter = room.CombatState.Encounter ?? throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            var enemies = encounter.SpawnedEnemies ?? throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            var fixtureEncounter = room.RoomType == RoomType.Monster && encounter.Id.Equals(combatEncounter.Id);
            var spawnedMatch = IdText(player.Character.Id) == "CHARACTER.IRONCLAD" && enemies.Count == 2 && enemies.All(x => IdText(x.Id) == "MONSTER.TOADPOLE");
            if (room.RoomType != RoomType.Monster || !ReferenceEquals(room.CombatState.RunState, runState) || !spawnedMatch) throw new StopRunException(TerminalClassification.BlockedPrecondition, FailureSite.RunState);
            ProbeController.CommitTransactionEvent(EventKind.SceneReady);

            var baselineJson = CanonicalProgress(candidate);
            var expected = ParseProgress(baselineJson);
            var discoveredExpected = player.DiscoveredEnemies.Select(IdText).ToList();
            ApplyExpected(expected, player, encounter, enemies, discoveredExpected);
            var expectedCanonical = JsonSupport.Canonicalize(JsonSerializationUtility.ToJson(expected));
            var noWriteBefore = BoundaryAudit.Snapshot().CandidateWriteCount == 0 && memory.Calls.All(x => !IsMutator(x.Method));

            SaveManager.Instance.UpdateProgressAfterCombatWon(player, room); ThrowBoundary();
            var mutationEqual = string.Equals(CanonicalProgress(candidate), expectedCanonical, StringComparison.Ordinal);
            var discoveredEqual = player.DiscoveredEnemies.Select(IdText).SequenceEqual(discoveredExpected, StringComparer.Ordinal);
            if (!mutationEqual || !discoveredEqual || !noWriteBefore) throw new StopRunException(TerminalClassification.FailMutationOrWrite, FailureSite.Mutation);
            ProbeController.CommitTransactionEvent(EventKind.ProgressUpdated);

            BoundaryAudit.SetExpectedPayloadCanonical(expectedCanonical);
            SaveManager.Instance.SaveProgressFile(); ThrowBoundary();
            audit = BoundaryAudit.Snapshot();
            sentinel = new SentinelEvidence(true, fixtureEncounter, spawnedMatch, mutationEqual, discoveredEqual, noWriteBefore,
                audit.CandidateUpdateCount, audit.CandidateSaveCount, audit.CandidateWriteCount, audit.CandidateWriteSynchronous,
                audit.CandidateWriteExactLogicalName, audit.CandidatePayloadEquivalent, audit.CandidatePayloadEquivalent);
            if (!SentinelPassed(sentinel)) throw new StopRunException(TerminalClassification.FailMutationOrWrite, FailureSite.Write);
            ProbeController.CommitTransactionEvent(EventKind.ProgressSaved);
            classification = TerminalClassification.PassDirectWriteIsolated; site = FailureSite.None;
        }
        catch (StopRunException stop) { classification = stop.Classification; site = stop.Site; }
        catch { classification = TerminalClassification.BlockedInstrumentation; site = FailureSite.RunCallback; }
        finally
        {
            if (Interlocked.CompareExchange(ref _teardownOwner, 1, 0) == 0)
            {
                try
                {
                    stage = TeardownStage.ActionsDisabled; ProbeController.CommitTransactionEvent(EventKind.TeardownStarted);
                    stage = TeardownStage.IsolatedTaskWait1; if (candidate is not null) await WaitTask(candidate);
                    stage = TeardownStage.Cleanup;
                    if (RunManager.Instance?.DebugOnlyGetState() is not null) { RunManager.Instance.CleanUp(true); cleanupCount++; }
                    stage = TeardownStage.IsolatedTaskWait2; if (candidate is not null) await WaitTask(candidate);
                    stage = TeardownStage.OverrideCleared;
                    if (overrideInstalled) SaveManager.ClearInstanceForTesting();
                    clearRestoredOriginal = !overrideInstalled || ReferenceEquals(SaveManager.Instance, original);
                    originalRestored = clearRestoredOriginal;
                    if (!originalRestored) { SaveManager.MockInstanceForTesting(original); originalRestored = ReferenceEquals(SaveManager.Instance, original); }
                    if (!originalRestored) throw new InvalidOperationException();
                    ProbeController.CommitTransactionEvent(EventKind.ManagerRestored);
                    stage = TeardownStage.OriginalTaskWait; await WaitTask(original);
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
                    if (classification == TerminalClassification.PassDirectWriteIsolated) { classification = TerminalClassification.FailTeardown; site = FailureSite.Cleanup; }
                }
            }

            audit = BoundaryAudit.SealAndSnapshot();
            if (audit.FirstFailure != BoundaryFailure.None && classification == TerminalClassification.PassDirectWriteIsolated) { classification = TerminalClassification.FailBoundary; site = FailureSite.Boundary; }
            if (!protectionUnchanged && before is not null) { classification = TerminalClassification.FailSafety; site = FailureSite.ProtectionAfter; }
            else if (classification == TerminalClassification.PassDirectWriteIsolated && (!clearRestoredOriginal || !originalRestored || !stateNull || !continueVisible || cleanupCount != 1)) { classification = TerminalClassification.FailTeardown; site = FailureSite.Cleanup; }
            var teardown = new TeardownEvidence(stage, cleanupCount == 1, clearRestoredOriginal, originalRestored, stateNull, continueVisible, protectionUnchanged);
            ProbeController.Complete(classification, site, before, afterFiles, candidateEvidence, sentinel, audit, teardown);
        }
    }

    private static void ThrowBoundary() { if (Volatile.Read(ref _boundaryFailure) != 0) throw new StopRunException(TerminalClassification.FailBoundary, FailureSite.Boundary); }
    private static bool AllCandidate(CandidateEvidence x) => x.ProfileIsOne && x.ProfileRawEqual && x.ProfileCanonicalEqual && x.SettingsRawEqual && x.SettingsCanonicalEqual && x.PrefsRawEqual && x.PrefsCanonicalEqual && x.ProgressRawEqual && x.ProgressCanonicalEqual && x.RootsIndependent && x.SharedMutableReferences == 0 && x.StoreContained && x.InitializationMemoryOnly && x.OriginalCanonicalUnchanged;
    private static bool SentinelPassed(SentinelEvidence x) => x.LegalScene && x.FixtureEncounterMatched && x.SpawnedEnemiesMatched && x.FullMutationEqual && x.DiscoveredEnemiesEqual && x.NoWriteBeforeSave && x.CandidateUpdateCount == 1 && x.CandidateSaveCount == 1 && x.CandidateWriteCount == 1 && x.SynchronousWrite && x.ExactProgressLogicalName && x.PayloadDeserialized && x.PayloadCanonicalEqual;
    private static bool IsMutator(string name) => name is "WriteFile" or "WriteFileAsync" or "DeleteFile" or "DeleteDirectory" or "RenameFile" or "BeginSaveBatch" or "EndSaveBatch" or "SetLastModifiedTime";
    private static string[] CaptureOriginalCanonical(SaveManager manager) { var live = LiveSeedCandidate.CaptureLive(manager); return new[] { JsonSupport.Canonicalize(live.ProfileJson), JsonSupport.Canonicalize(live.SettingsJson), JsonSupport.Canonicalize(live.PrefsJson), JsonSupport.Canonicalize(live.ProgressJson) }; }
    private static string CanonicalProgress(SaveManager manager) => JsonSupport.Canonicalize(JsonSerializationUtility.ToJson(manager.Progress.ToSerializable()));
    private static SerializableProgress ParseProgress(string json) { var result = JsonSerializationUtility.FromJson<SerializableProgress>(json); return result.Success && result.SaveData is not null ? result.SaveData : throw new InvalidDataException(); }
    private static object ReadProfile(SaveManager manager) { var f = typeof(SaveManager).GetField("_profileSaveManager", BindingFlags.Instance | BindingFlags.NonPublic)!; var child = f.GetValue(manager)!; return child.GetType().GetProperty("Profile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(child)!; }
    private static CombatReplay ReadFixture()
    {
        var bytes = File.ReadAllBytes(ProbeConstants.FixturePath);
        if (bytes.Length != ProbeConstants.FixtureLength || Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) != ProbeConstants.FixtureSha256) throw new StopRunException(TerminalClassification.BlockedIdentity, FailureSite.Identity);
        var reader = new PacketReader(); reader.Reset(bytes); var replay = reader.Read<CombatReplay>();
        if (replay.version != ProbeConstants.GameVersion || replay.gitCommit != ProbeConstants.GameCommit) throw new StopRunException(TerminalClassification.BlockedIdentity, FailureSite.Identity);
        return replay;
    }
    private static void ApplyExpected(SerializableProgress progress, Player player, EncounterModel encounter, IReadOnlyList<MonsterModel> enemies, List<string> discovered)
    {
        var encounterStats = progress.EncounterStats.FirstOrDefault(x => x.Id.Equals(encounter.Id));
        if (encounterStats is null) { encounterStats = new EncounterStats { Id = encounter.Id, FightStats = new List<FightStats>() }; progress.EncounterStats.Add(encounterStats); }
        Increment(encounterStats.FightStats, player.Character.Id);
        foreach (var enemy in enemies)
        {
            var stats = progress.EnemyStats.FirstOrDefault(x => x.Id.Equals(enemy.Id));
            if (stats is null) { stats = new EnemyStats { Id = enemy.Id, FightStats = new List<FightStats>() }; progress.EnemyStats.Add(stats); }
            var empty = stats.FightStats.Count == 0; Increment(stats.FightStats, player.Character.Id); if (empty) discovered.Add(IdText(enemy.Id));
        }
    }
    private static void Increment(List<FightStats> rows, ModelId id) { var row = rows.FirstOrDefault(x => x.Character.Equals(id)); if (row is null) rows.Add(new FightStats { Character = id, Wins = 1, Losses = 0 }); else row.Wins++; }
    private static string IdText(ModelId id) => id.ToString();
    private static async Task WaitTask(SaveManager manager) { var task = manager.CurrentRunSaveTask; if (task is not null) await task.WaitAsync(TimeSpan.FromSeconds(10)); }
}
