using MegaCrit.Sts2.Core.Runs;

namespace SpikeInv008A;

internal static class PhaseAwareOracle
{
    internal static readonly IReadOnlyList<string> CoreCleanupSteps = new[]
    {
        "ActionQueueSet.Reset", "CardSelectCmd.Reset", "CombatManager.Reset",
        "ActionExecutor.remove_JustBeforeActionFinishedExecuting", "CombatReplayWriter.Dispose",
        "ActionQueueSynchronizer.Dispose", "PlayerChoiceSynchronizer.Dispose", "RewardSynchronizer.Dispose",
        "RewardsSetSynchronizer.Dispose", "RestSiteSynchronizer.Dispose", "FlavorSynchronizer.Dispose",
        "ChecksumTracker.Dispose", "INetGameService.Disconnect"
    };
    internal const string LobbyRemove = "RunLobby.remove_RemotePlayerDisconnected";
    internal const string LobbyDispose = "RunLobby.Dispose";

    internal static LobbyObservation CaptureLobby(string point)
    {
        var manager = RunManager.Instance;
        var lobby = manager.RunLobby;
        var service = manager.NetService;
        return new(point, service is not null, service?.Type.ToString(), lobby is null,
            lobby is null ? 0 : ScopeSafeSession.Identity(lobby));
    }

    internal static LobbyOracleEvidence EvaluateLobby(IReadOnlyList<LobbyObservation> values)
    {
        var pre = values.SingleOrDefault(x => x.Point == "PreSetup");
        if (pre is null) return new(false, false, false, false, "MissingPreSetup");
        if (!pre.RunLobbyNull) return new(false, false, false, false, "StaleRunLobby");
        var applicable = values.Where(x => x.Point is "PostSetup" or "PostLaunch" or "PostScene" or "CleanupEntry").ToList();
        var replay = applicable.Count >= 2 && applicable.All(x => x.NetServicePresent && x.NetServiceType == "Replay" && x.RunLobbyNull);
        var stable = applicable.All(x => x.RunLobbyRuntimeIdentity == 0);
        var failure = !replay ? "ReplayBoundary" : !stable ? "LobbyIdentityChanged" : "None";
        return new(true, replay, stable, replay && stable, failure);
    }

    internal static CleanupCallOracleEvidence EvaluateCleanup(IReadOnlyDictionary<string, int> counts,
        bool preCleanupLobbyNull, int cleanupExceptions)
    {
        var failures = new List<string>();
        foreach (var step in CoreCleanupSteps)
            if (!counts.TryGetValue(step, out var count) || count != 1) failures.Add(step + "=" + count);
        var remove = counts.TryGetValue(LobbyRemove, out var removeCount) ? removeCount : 0;
        var dispose = counts.TryGetValue(LobbyDispose, out var disposeCount) ? disposeCount : 0;
        var lobbyApplicable = !preCleanupLobbyNull;
        var lobbyPass = lobbyApplicable ? remove == 1 && dispose == 1 : remove == 0 && dispose == 0;
        if (!lobbyPass) failures.Add($"RunLobbyGroup={remove}/{dispose}/applicable:{lobbyApplicable}");
        if (cleanupExceptions != 0) failures.Add("CleanupExceptions=" + cleanupExceptions);
        var corePass = CoreCleanupSteps.All(step => counts.TryGetValue(step, out var count) && count == 1);
        return new(corePass, lobbyApplicable, lobbyPass, corePass && lobbyPass && cleanupExceptions == 0, failures);
    }

    internal static bool PreSetupAllowed(LobbyObservation value) => value.Point == "PreSetup" && value.RunLobbyNull;

    internal static bool ScenePassed(ProbePhase phase, SceneEvidence value) => phase switch
    {
        ProbePhase.SetupOwnerFault => value.SetupFaultHadNoOwnedNRun && value.OwnedNRunRuntimeIdentity == 0 &&
                                      !value.FaultSceneWasOwnedNRun,
        ProbePhase.PostLaunchOwnerFault => value.OwnedNRunRuntimeIdentity != 0 && value.FaultSceneWasOwnedNRun &&
                                           value.CleanupEntrySceneRuntimeIdentity == value.OwnedNRunRuntimeIdentity &&
                                           value.RestoredSceneIsMainMenu && value.OwnedNRunNoLongerCurrent &&
                                           value.RestoredSceneRuntimeIdentity != value.OwnedNRunRuntimeIdentity,
        _ => false
    };
}
