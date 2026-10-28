using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv008A;

internal static class BoundaryAudit
{
    private static readonly object Gate = new();
    private static HashSet<object> _originalStores = new(ReferenceEqualityComparer.Instance);
    private static SaveManager? _original;
    private static SaveManager? _candidate;
    private static MockGodotFileIo? _memory;
    private static int _enabled, _sealed;
    private static BoundaryFailure _failure;
    private static bool _originalManagerSeen, _unknownManagerSeen, _originalStoreSeen, _unknownStoreSeen, _externalSeen;
    private static int _launchEntries, _launchReturns, _sceneInstalls, _generateMap, _roomEntries, _processFrames,
        _dispatches, _actions, _victories, _rewards, _progressUpdates, _progressSaves, _replayRecord, _replayWrites;
    private static int _cleanupEntries, _cleanupReturns, _cleanupExceptions;
    private static readonly Dictionary<string, int> CleanupCounts = new(StringComparer.Ordinal);
    private static readonly List<LobbyObservation> LobbyObservations = [];

    internal static IReadOnlyList<string> RequiredCleanupSteps => PhaseAwareOracle.CoreCleanupSteps;

    internal static bool IsObserverRunningFirst() => Volatile.Read(ref _sealed) == 0 && Volatile.Read(ref _enabled) != 0;

    internal static void Start(SaveManager original, SaveManager candidate, MockGodotFileIo memory,
        LobbyObservation preSetup)
    {
        var stores = CollectStores(original);
        lock (Gate)
        {
            _original = original; _candidate = candidate; _memory = memory; _originalStores = stores;
            _failure = BoundaryFailure.None; _originalManagerSeen = _unknownManagerSeen = _originalStoreSeen =
                _unknownStoreSeen = _externalSeen = false;
            _launchEntries = _launchReturns = _sceneInstalls = _generateMap = _roomEntries = _processFrames =
                _dispatches = _actions = _victories = _rewards = _progressUpdates = _progressSaves =
                _replayRecord = _replayWrites = 0;
            _cleanupEntries = _cleanupReturns = _cleanupExceptions = 0; CleanupCounts.Clear();
            LobbyObservations.Clear(); LobbyObservations.Add(preSetup);
            Volatile.Write(ref _sealed, 0); Volatile.Write(ref _enabled, 1);
        }
    }

    internal static bool ManagerEntry(SaveManager instance)
    {
        if (!IsObserverRunningFirst()) return true;
        lock (Gate)
        {
            if (!IsObserverRunningFirst()) return true;
            if (ReferenceEquals(instance, _original)) { _originalManagerSeen = true; Fail(BoundaryFailure.OriginalManager); return false; }
            if (!ReferenceEquals(instance, _candidate)) { _unknownManagerSeen = true; Fail(BoundaryFailure.UnknownManager); return false; }
            Fail(BoundaryFailure.CandidateMutator); return false;
        }
    }

    internal static bool StoreEntry(object instance)
    {
        if (!IsObserverRunningFirst()) return true;
        lock (Gate)
        {
            if (!IsObserverRunningFirst()) return true;
            if (_originalStores.Contains(instance)) { _originalStoreSeen = true; Fail(BoundaryFailure.OriginalStore); return false; }
            if (!ReferenceEquals(instance, _memory)) { _unknownStoreSeen = true; Fail(BoundaryFailure.UnknownStore); return false; }
            Fail(BoundaryFailure.CandidateMutator); return false;
        }
    }

    internal static void ExternalEntry() { if (!IsObserverRunningFirst()) return; lock (Gate) { if (!IsObserverRunningFirst()) return; _externalSeen = true; Fail(BoundaryFailure.ExternalBoundary); } }
    internal static void UserEntry(BoundaryFailure failure) { if (!IsObserverRunningFirst()) return; lock (Gate) { if (!IsObserverRunningFirst()) return; Fail(failure); } }
    internal static void LaunchEntry() { if (!IsObserverRunningFirst()) return; lock (Gate) { _launchEntries++; } }
    internal static void LaunchReturn(Exception? exception) { if (!IsObserverRunningFirst()) return; lock (Gate) { if (exception is null) _launchReturns++; } }
    internal static void SceneInstall() { if (!IsObserverRunningFirst()) return; lock (Gate) { _sceneInstalls++; } }
    internal static void Business(BoundaryFailure failure)
    {
        if (!IsObserverRunningFirst()) return;
        lock (Gate)
        {
            switch (failure)
            {
                case BoundaryFailure.GenerateMap: _generateMap++; break;
                case BoundaryFailure.RoomEntry: _roomEntries++; break;
                case BoundaryFailure.ProcessFrame: _processFrames++; break;
                case BoundaryFailure.Dispatcher: _dispatches++; break;
                case BoundaryFailure.GameAction: _actions++; break;
                case BoundaryFailure.Victory: _victories++; break;
                case BoundaryFailure.Reward: _rewards++; break;
                case BoundaryFailure.Progress: _progressUpdates++; break;
                case BoundaryFailure.ReplayRecord: _replayRecord++; break;
                case BoundaryFailure.ReplayWrite: _replayWrites++; break;
            }
            Fail(failure);
        }
    }
    internal static void ProgressSave() { if (!IsObserverRunningFirst()) return; lock (Gate) { _progressSaves++; Fail(BoundaryFailure.Progress); } }
    internal static void CleanupEntry()
    {
        if (!IsObserverRunningFirst()) return;
        var lobby = PhaseAwareOracle.CaptureLobby("CleanupEntry");
        lock (Gate) { _cleanupEntries++; LobbyObservations.Add(lobby); }
    }
    internal static void CleanupReturn() { if (!IsObserverRunningFirst()) return; lock (Gate) { _cleanupReturns++; } }
    internal static void CleanupException() { if (!IsObserverRunningFirst()) return; lock (Gate) { _cleanupExceptions++; } }
    internal static void CleanupStep(string step)
    {
        if (!IsObserverRunningFirst()) return;
        lock (Gate) CleanupCounts[step] = CleanupCounts.TryGetValue(step, out var count) ? count + 1 : 1;
    }
    internal static bool ObserveLobby(string point)
    {
        if (!IsObserverRunningFirst()) return false;
        var value = PhaseAwareOracle.CaptureLobby(point);
        lock (Gate)
        {
            LobbyObservations.Add(value);
            if (!value.NetServicePresent || value.NetServiceType != "Replay" || !value.RunLobbyNull)
            { Fail(BoundaryFailure.RunLobbyBoundary); return false; }
            return true;
        }
    }
    internal static IReadOnlyList<LobbyObservation> SnapshotLobbyObservations()
    { lock (Gate) return LobbyObservations.ToList(); }

    internal static AuditSnapshot Snapshot() { lock (Gate) return SnapshotLocked(); }
    internal static CleanupEvidence PeekCleanup(bool stateNull, bool shouldSaveFalse, bool cleaningFalse, bool netIdNull)
    {
        lock (Gate) return MakeCleanup(stateNull, shouldSaveFalse, cleaningFalse, netIdNull);
    }
    internal static (AuditSnapshot Audit, CleanupEvidence Cleanup) SealAndSnapshot(bool stateNull, bool shouldSaveFalse,
        bool cleaningFalse, bool netIdNull)
    {
        Interlocked.Exchange(ref _sealed, 1); Volatile.Write(ref _enabled, 0);
        lock (Gate)
        {
            var cleanup = MakeCleanup(stateNull, shouldSaveFalse, cleaningFalse, netIdNull);
            return (SnapshotLocked(), cleanup);
        }
    }
    internal static bool CleanupComplete(CleanupEvidence value) => value.Entries == 1 && value.Returns == 1 &&
        value.Exceptions == 0 && value.CallOracle.CallSetPass && value.StateNull &&
        value.ShouldSaveFalse && value.IsCleaningUpFalse && value.LocalNetIdNull && value.NoReplayWrite;
    internal static bool BusinessZero(BusinessCounters x) => x.GenerateMap == 0 && x.RoomEntries == 0 &&
        x.ProcessFrames == 0 && x.Dispatches == 0 && x.Actions == 0 && x.Victories == 0 && x.Rewards == 0 &&
        x.ProgressUpdates == 0 && x.ProgressSaves == 0 && x.ReplayRecordInitial == 0 && x.ReplayWrites == 0;

    private static AuditSnapshot SnapshotLocked() => new(_failure, _originalManagerSeen, _unknownManagerSeen,
        _originalStoreSeen, _unknownStoreSeen, _externalSeen, new(_launchEntries, _launchReturns, _sceneInstalls,
            _generateMap, _roomEntries, _processFrames, _dispatches, _actions, _victories, _rewards,
            _progressUpdates, _progressSaves, _replayRecord, _replayWrites));
    private static CleanupEvidence MakeCleanup(bool stateNull, bool shouldSaveFalse, bool cleaningFalse, bool netIdNull)
    {
        var counts = CleanupCounts.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var cleanupLobby = LobbyObservations.LastOrDefault(x => x.Point == "CleanupEntry");
        var oracle = PhaseAwareOracle.EvaluateCleanup(counts, cleanupLobby?.RunLobbyNull ?? false, _cleanupExceptions);
        return new(_cleanupEntries, _cleanupReturns, _cleanupExceptions, RequiredCleanupSteps,
            counts.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList(), counts, oracle, stateNull,
            shouldSaveFalse, cleaningFalse, netIdNull, _replayWrites == 0);
    }
    private static void Fail(BoundaryFailure failure) { if (_failure == BoundaryFailure.None) { _failure = failure; OwnerFaultTransaction.SignalBoundaryFailure(failure); } }

    private static HashSet<object> CollectStores(SaveManager manager)
    {
        var result = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var field in manager.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var value = field.GetValue(manager);
            if (value is ISaveStore store) AddStore(store, result);
            if (value is null || field.FieldType.Namespace?.Contains("Saves.Managers", StringComparison.Ordinal) != true) continue;
            foreach (var child in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (child.GetValue(value) is ISaveStore childStore) AddStore(childStore, result);
        }
        return result;
    }
    private static void AddStore(ISaveStore store, HashSet<object> result)
    {
        if (!result.Add(store)) return;
        foreach (var field in store.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.GetValue(store) is ISaveStore child) AddStore(child, result);
    }
}

[HarmonyPatch]
internal static class SaveManagerMutationPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new HashSet<string>(StringComparer.Ordinal) { nameof(SaveManager.SaveRun), nameof(SaveManager.SaveProgressFile), nameof(SaveManager.UpdateProgressAfterCombatWon), nameof(SaveManager.SaveRunHistory), nameof(SaveManager.SavePrefsFile), nameof(SaveManager.SaveSettings), nameof(SaveManager.SaveProfile), nameof(SaveManager.DeleteCurrentRun), nameof(SaveManager.DeleteCurrentMultiplayerRun), nameof(SaveManager.DeleteDirectoryRecursive), "DeleteInDirectoryRecursive", nameof(SaveManager.BeginSaveBatch), nameof(SaveManager.EndSaveBatch), nameof(SaveManager.SwitchProfileId) };
        return typeof(SaveManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => names.Contains(x.Name));
    }
    private static bool Prefix(MethodBase __originalMethod, SaveManager __instance)
    {
        if (!BoundaryAudit.IsObserverRunningFirst()) return true;
        if (__originalMethod.Name == nameof(SaveManager.SaveProgressFile)) BoundaryAudit.ProgressSave();
        return BoundaryAudit.ManagerEntry(__instance);
    }
}

[HarmonyPatch]
internal static class StoreMutationPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new HashSet<string>(StringComparer.Ordinal) { "WriteFile", "WriteFileAsync", "DeleteFile", "DeleteDirectory", "RenameFile", "BeginSaveBatch", "EndSaveBatch", "OverwriteCloudWithLocal", "OverwriteCloudWithLocalDirectory", "ForgetFile", "SyncCloudToLocal", "SyncCloudToLocalDirectory", "SetLastModifiedTime" };
        return typeof(SaveManager).Assembly.GetTypes().Where(t => typeof(ISaveStore).IsAssignableFrom(t) && !t.IsInterface)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)).Where(m => names.Contains(m.Name));
    }
    private static bool Prefix(object __instance)
    {
        if (!BoundaryAudit.IsObserverRunningFirst()) return true;
        return BoundaryAudit.StoreEntry(__instance);
    }
}

[HarmonyPatch]
internal static class ExternalSubmitPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(SaveManager).Assembly.GetTypes()
        .Where(t => t.Name.Contains("Metric", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Leaderboard", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Achievement", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Daily", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Unlock", StringComparison.OrdinalIgnoreCase))
        .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        .Where(m => (m.Name.StartsWith("Upload", StringComparison.OrdinalIgnoreCase) || m.Name.StartsWith("Submit", StringComparison.OrdinalIgnoreCase) || m.Name.Equals("Unlock", StringComparison.OrdinalIgnoreCase)) && !m.ContainsGenericParameters && m.GetMethodBody() is not null);
    private static bool Prefix() { if (!BoundaryAudit.IsObserverRunningFirst()) return true; BoundaryAudit.ExternalEntry(); return false; }
}

[HarmonyPatch(typeof(NGame), nameof(NGame.Quit))]
internal static class QuitGuardPatch { private static bool Prefix() { if (!BoundaryAudit.IsObserverRunningFirst()) return true; BoundaryAudit.UserEntry(BoundaryFailure.Quit); return false; } }
[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
internal static class InputGuardPatch { private static bool Prefix() => !BoundaryAudit.IsObserverRunningFirst(); }
[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SwitchProfileId))]
internal static class ProfileGuardPatch { private static bool Prefix() { if (!BoundaryAudit.IsObserverRunningFirst()) return true; BoundaryAudit.UserEntry(BoundaryFailure.ProfileChange); return false; } }
[HarmonyPatch]
internal static class MainMenuActionGuardPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(NMainMenu).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name is "OnContinueButtonPressed" or "SingleplayerButtonPressed" or "OpenMultiplayerSubmenu");
    private static bool Prefix() { if (!BoundaryAudit.IsObserverRunningFirst()) return true; BoundaryAudit.UserEntry(BoundaryFailure.UserAction); return false; }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.Launch))]
internal static class LaunchAuditPatch
{
    private static void Prefix() => BoundaryAudit.LaunchEntry();
    private static void Postfix() => BoundaryAudit.LaunchReturn(null);
    private static Exception? Finalizer(Exception? __exception) { if (__exception is not null) BoundaryAudit.LaunchReturn(__exception); return __exception; }
}
[HarmonyPatch(typeof(NGame), "LoadMainMenu")]
internal static class MainMenuLoadAuditPatch { private static void Prefix() { if (BoundaryAudit.IsObserverRunningFirst()) OwnerFaultTransaction.ObserveMainMenuLoad(); } }
[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu.RefreshButtons))]
internal static class RefreshButtonsAuditPatch { private static void Postfix(NMainMenu __instance) => ProbeController.OnRefreshButtons(__instance); }

[HarmonyPatch]
internal static class ForbiddenBusinessPatch
{
    private static readonly Dictionary<string, BoundaryFailure> Names = new(StringComparer.Ordinal)
    {
        [nameof(RunManager.GenerateMap)] = BoundaryFailure.GenerateMap,
        [nameof(RunManager.LoadIntoLatestMapCoord)] = BoundaryFailure.RoomEntry,
        ["EnterMapPointInternal"] = BoundaryFailure.RoomEntry,
        ["EnterMapCoordInternal"] = BoundaryFailure.RoomEntry,
        ["EnterRoomInternal"] = BoundaryFailure.RoomEntry,
        [nameof(SaveManager.UpdateProgressAfterCombatWon)] = BoundaryFailure.Progress,
        [nameof(CombatReplayWriter.RecordInitialState)] = BoundaryFailure.ReplayRecord,
        [nameof(CombatReplayWriter.WriteReplay)] = BoundaryFailure.ReplayWrite
    };
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var types = new[] { typeof(RunManager), typeof(SaveManager), typeof(CombatReplayWriter) };
        return types.SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)).Where(m => Names.ContainsKey(m.Name));
    }
    private static void Prefix(MethodBase __originalMethod)
    {
        if (!BoundaryAudit.IsObserverRunningFirst()) return;
        BoundaryAudit.Business(Names[__originalMethod.Name]);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
internal static class CleanupAuditPatch
{
    private static void Prefix() { if (BoundaryAudit.IsObserverRunningFirst()) BoundaryAudit.CleanupEntry(); }
    private static void Postfix() { if (BoundaryAudit.IsObserverRunningFirst()) BoundaryAudit.CleanupReturn(); }
    private static Exception? Finalizer(Exception? __exception) { if (__exception is not null && BoundaryAudit.IsObserverRunningFirst()) BoundaryAudit.CleanupException(); return __exception; }
}

[HarmonyPatch]
internal static class CleanupStepAuditPatch
{
    private static readonly Dictionary<string, string> Targets = new(StringComparer.Ordinal)
    {
        ["MegaCrit.Sts2.Core.GameActions.Multiplayer.ActionQueueSet::Reset"] = "ActionQueueSet.Reset",
        ["MegaCrit.Sts2.Core.Commands.CardSelectCmd::Reset"] = "CardSelectCmd.Reset",
        ["MegaCrit.Sts2.Core.Combat.CombatManager::Reset"] = "CombatManager.Reset",
        ["MegaCrit.Sts2.Core.GameActions.ActionExecutor::remove_JustBeforeActionFinishedExecuting"] = "ActionExecutor.remove_JustBeforeActionFinishedExecuting",
        ["MegaCrit.Sts2.Core.Multiplayer.Replay.CombatReplayWriter::Dispose"] = "CombatReplayWriter.Dispose",
        ["MegaCrit.Sts2.Core.GameActions.Multiplayer.ActionQueueSynchronizer::Dispose"] = "ActionQueueSynchronizer.Dispose",
        ["MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceSynchronizer::Dispose"] = "PlayerChoiceSynchronizer.Dispose",
        ["MegaCrit.Sts2.Core.Multiplayer.Game.RewardSynchronizer::Dispose"] = "RewardSynchronizer.Dispose",
        ["MegaCrit.Sts2.Core.Multiplayer.Game.RewardsSetSynchronizer::Dispose"] = "RewardsSetSynchronizer.Dispose",
        ["MegaCrit.Sts2.Core.Multiplayer.Game.RestSiteSynchronizer::Dispose"] = "RestSiteSynchronizer.Dispose",
        ["MegaCrit.Sts2.Core.Multiplayer.Game.FlavorSynchronizer::Dispose"] = "FlavorSynchronizer.Dispose",
        ["MegaCrit.Sts2.Core.Multiplayer.Game.ChecksumTracker::Dispose"] = "ChecksumTracker.Dispose",
        ["MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.RunLobby::remove_RemotePlayerDisconnected"] = "RunLobby.remove_RemotePlayerDisconnected",
        ["MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.RunLobby::Dispose"] = "RunLobby.Dispose",
        ["MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService::Disconnect"] = "INetGameService.Disconnect"
    };
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var assembly = typeof(RunManager).Assembly;
        foreach (var pair in Targets)
        {
            var split = pair.Key.Split("::", StringSplitOptions.None);
            var type = assembly.GetType(split[0]);
            if (type is null || type.IsInterface) continue;
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => m.Name == split[1])) yield return method;
        }
        var net = assembly.GetType("MegaCrit.Sts2.Core.Multiplayer.Game.INetGameService");
        if (net is not null)
            foreach (var type in assembly.GetTypes().Where(t => !t.IsInterface && net.IsAssignableFrom(t)))
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => m.Name == "Disconnect")) yield return method;
    }
    private static void Prefix(MethodBase __originalMethod)
    {
        if (!BoundaryAudit.IsObserverRunningFirst()) return;
        var key = __originalMethod.DeclaringType!.FullName + "::" + __originalMethod.Name;
        if (Targets.TryGetValue(key, out var value)) BoundaryAudit.CleanupStep(value);
        else if (__originalMethod.Name == "Disconnect") BoundaryAudit.CleanupStep("INetGameService.Disconnect");
    }
}
