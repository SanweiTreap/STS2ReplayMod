namespace SpikeInv007A;

internal static class ContractRules
{
    internal static readonly IReadOnlySet<FailureSite> PreconditionSites = new HashSet<FailureSite>
    {
        FailureSite.FixturePlayerCardinality,
        FailureSite.ReplaySetupFailed,
        FailureSite.ShouldSaveDisableFailed,
        FailureSite.NGameUnavailable,
        FailureSite.SceneLoadFailed,
        FailureSite.ManagerOverrideLost,
        FailureSite.RunStateBindingLost,
        FailureSite.CurrentRoomNotCombat,
        FailureSite.UnsupportedRoomType,
        FailureSite.CombatStateUnavailable,
        FailureSite.EncounterUnavailable,
        FailureSite.CharacterUnavailable,
        FailureSite.DiscoveredEnemiesUnavailable,
        FailureSite.EnemyListUnreadable,
        FailureSite.EnemyEntryInvalid,
        FailureSite.ProgressSubsystemUnavailable,
    };

    internal static bool MutationPassed(SentinelEvidence value)
        => value.IdsReadable && value.FullMutationEqual && value.DiscoveredEnemiesEqual &&
           value.NoWriteBeforeSave && value.NoWriteAfterUpdate &&
           value.CandidateUpdateCount == 1 && value.CandidateSaveCount == 0 &&
           value.CandidateWriteCount == 0;

    internal static bool WritePassed(SentinelEvidence value)
        => value.IdsReadable && value.FullMutationEqual && value.DiscoveredEnemiesEqual &&
           value.NoWriteBeforeSave && value.NoWriteAfterUpdate &&
           value.CandidateUpdateCount == 1 && value.CandidateSaveCount == 1 &&
           value.CandidateWriteCount == 1 && value.SynchronousWrite &&
           value.ExactProgressLogicalName && value.PayloadDeserialized &&
           value.PayloadCanonicalEqual;
}
