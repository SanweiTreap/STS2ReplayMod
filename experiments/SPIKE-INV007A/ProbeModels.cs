using System.Text.Json.Serialization;

namespace SpikeInv007A;

internal static class ProbeConstants
{
    internal const string SchemaId = "spike-inv007a.direct-write.v3";
    internal const string ProbeVersion = "0.3.0-direct-write-r3";
    internal const string GameVersion = "v0.111.0";
    internal const string GameCommit = "41cef1ea";
    internal const string GameBranch = "v0.111.0";
    internal const long GameDllLength = 9_757_184;
    internal const string GameDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const string FixtureSha256 = "4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631";
    internal const int FixtureLength = 20_441;
    internal const string FixturePath = @"C:\Projects\STS2ReplayMod\evidence\spike-inv003a\protected-backup\20260907T175924.5461327Z\profile1\replays\latest.mcr";
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv007a";
    internal const string ArtifactFile = @"C:\Projects\STS2ReplayMod\evidence\spike-inv007a\runtime-artifact-r3.json";
    internal const string PriorArtifactFile = @"C:\Projects\STS2ReplayMod\evidence\spike-inv007a\runtime-artifact-r2.json";
    internal const long PriorArtifactLength = 19_259;
    internal const string PriorArtifactSha256 = "4B48A75F650A687F206255F2425140B3F640277E3D62F21495F99CC6086B691F";
    internal const string PriorRecoveryRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv007a\restricted-recovery\r2-20260914T091555.1356848Z";
    internal const int PriorRecoveryFileCount = 12;
    internal const string PriorRecoveryAggregateSha256 = "9719C685A08E86786D96DE55309F19C6242A71D8001B1E841BF88B89598AB9E2";
    internal const int OracleSharedReferenceCountCap = 1_024;
    internal const int MissingEventTimeoutSeconds = 120;
    internal const string ProgressLogicalName = "modded/profile1/saves/progress.save";
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum TerminalClassification { Running = 0, PassDirectWriteIsolated, FailPreoverrideEquivalence, FailBoundary, FailMutation, FailWrite, FailTeardown, FailSafety, StopMissingTimingGate, BlockedIdentity, BlockedPriorAttemptUnresolved, BlockedOracleParse, BlockedOracleAliasing, BlockedPrecondition, BlockedInstrumentation, BlockedArtifactWrite }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum FailureSite { None, Identity, GraphicsPrefix, GraphicsPostfix, GraphicsFinalizer, OuterPostfix, OuterContinuation, StartupContinuation, MenuCallback, GateEvaluation, RunCallback, TimeoutCallback, ArtifactWriter, DuplicateOuter, DuplicateGraphics, ManagerIdentity, CurrentRunTask, PriorAttemptIntegrity, ProtectionBaseline, Candidate, Boundary, FixturePlayerCardinality, ReplaySetupFailed, ShouldSaveDisableFailed, NGameUnavailable, SceneLoadFailed, ManagerOverrideLost, RunStateBindingLost, CurrentRoomNotCombat, UnsupportedRoomType, CombatStateUnavailable, EncounterUnavailable, CharacterUnavailable, DiscoveredEnemiesUnavailable, EnemyListUnreadable, EnemyEntryInvalid, ProgressSubsystemUnavailable, OracleParse, OracleAliasing, Mutation, Write, Cleanup, RestoreManager, MainMenu, ProtectionAfter }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum EventKind { PatchActive, GraphicsEntry, GraphicsNormalExit, GraphicsFault, OuterCloudReturned, OuterCloudTerminal, StartupMarkerTerminal, MenuReady, PriorAttemptVerified, GateReady, RunAccepted, BaselineReady, CandidateReady, OverrideInstalled, RunStateParsed, ReplaySetupComplete, SceneLoadComplete, DirectArgumentsReady, ProgressUpdated, ProgressSaved, TeardownStarted, ManagerRestored, MainMenuRestored, ProtectionVerified, Terminal }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ObservedTaskStatus { Null, Pending, RanToCompletion, Faulted, Canceled }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ProtectedScope { ActiveProfile, AccountSelector, MachineSettings }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum BoundaryFailure { None, OriginalManager, UnknownManager, OriginalStore, UnknownStore, WrongCandidateOperation, WrongCandidatePath, WrongCandidatePayload, ExternalBoundary, UserAction }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum TeardownStage { None, ActionsDisabled, IsolatedTaskWait1, Cleanup, IsolatedTaskWait2, OverrideCleared, OriginalTaskWait, MainMenuLoad, MenuReady, ProtectionCheck, Sealed }

internal sealed record TimingEvent(long Sequence, long ElapsedMonotonicTicks, EventKind Kind, ObservedTaskStatus? TaskStatus = null);
internal sealed record GateConditions(bool PatchBeforeGraphics, bool GraphicsNormalAndIdle, bool ExactlyOneOuterTask, bool OuterTaskTerminal, bool StartupMarkerTerminal, bool MenuReady, bool ManagerIdentityStable, bool RunStateNull, bool CurrentRunTaskNullOrTerminal, bool NoTerminalFailure, bool NoPostGateLifecycle);
internal sealed record GateSnapshot(long Sequence, int OriginalManagerRuntimeIdentity, int OuterTaskRuntimeIdentity, ObservedTaskStatus OuterTaskStatus, ObservedTaskStatus StartupTaskStatus, ObservedTaskStatus CurrentRunTaskStatus, bool RunStateNull, int GraphicsEntryCount, int GraphicsNormalExitCount, int GraphicsActiveCount, GateConditions Conditions);
internal sealed record ClosedSnapshot(TerminalClassification TerminalClassification, FailureSite FirstFailureSite, long TerminalSequence, long TerminalElapsedMonotonicTicks, IReadOnlyList<TimingEvent> Events, GateSnapshot? Gate, GateSnapshot? Verify);

internal sealed record ProtectedFile(ProtectedScope Scope, string RelativeName, bool Exists, long? Length, string? Sha256, string? LastWriteUtc);
internal sealed record ProfileInventory(IReadOnlyList<string> Directories, IReadOnlyList<string> Files);
internal sealed record BackupEntry(ProtectedScope Scope, string RelativeName, long Length, string SourceSha256, string BackupSha256);
internal sealed record ProtectionSnapshot(IReadOnlyList<ProtectedFile> Files, ProfileInventory ProfileInventory, IReadOnlyList<BackupEntry> Backups);
internal sealed record RecoveryBaseline(ProtectionSnapshot Snapshot, int FileCount, string AggregateSha256);
internal sealed record PriorAttemptIntegrityEvidence(bool PriorArtifactIdentityValid, bool PriorRecoveryIdentityValid, int PriorRecoveryFileCount, int PriorArtifactProtectionFileCount, bool ArtifactRecoveryFileSetEqual, bool ArtifactRecoveryContentEqual, bool ArtifactRecoveryInventoryEqual, bool ActiveModdedProfileOne, bool FileSetEqual, bool ContentEqual, bool InventoryEqual, bool ContinueVisible, bool RunStateNull, bool OriginalManagerIdentityStable);
internal sealed record CandidateEvidence(bool ProfileIsOne, bool ProfileRawEqual, bool ProfileCanonicalEqual, bool SettingsRawEqual, bool SettingsCanonicalEqual, bool PrefsRawEqual, bool PrefsCanonicalEqual, bool ProgressRawEqual, bool ProgressCanonicalEqual, bool RootsIndependent, int SharedMutableReferences, bool StoreContained, bool InitializationMemoryOnly, bool OriginalCanonicalUnchanged);
internal sealed record SentinelEvidence(bool IdsReadable, string ExpectedCanonicalSha256, string ActualCanonicalSha256, bool FullMutationEqual, bool DiscoveredEnemiesEqual, bool NoWriteBeforeSave, bool NoWriteAfterUpdate, int CandidateUpdateCount, int CandidateSaveCount, int CandidateWriteCount, bool SynchronousWrite, bool ExactProgressLogicalName, bool PayloadDeserialized, bool PayloadCanonicalEqual);
internal sealed record OracleEvidence(bool ParseSucceeded, int SharedMutableReferences, bool NoSharedMutableReferences, bool CandidateCanonicalUnchanged, bool DiscoveredEnemiesUnchanged, bool PrecallAuditZero);
internal sealed record AuditSnapshot(BoundaryFailure FirstFailure, bool OriginalManagerSeen, bool UnknownManagerSeen, bool OriginalStoreSeen, bool UnknownStoreSeen, bool ExternalBoundarySeen, int CandidateUpdateCount, int CandidateSaveCount, int CandidateWriteCount, bool CandidateWriteSynchronous, bool CandidateWriteExactLogicalName, bool CandidatePayloadDeserialized, bool CandidatePayloadEquivalent);
internal sealed record TeardownEvidence(TeardownStage LastStage, bool CleanupRequired, bool CleanupCalledExactlyOnceWhenRequired, bool OverrideClearRestoredOriginal, bool OriginalManagerRestored, bool RunStateNull, bool ContinueVisible, bool ProtectionUnchanged);
internal sealed record RuntimeArtifact(string SchemaId, string ProbeVersion, string GameVersion, string GameCommit, string GameBranch, long GameDllLength, string GameDllSha256, long ProbeDllLength, string ProbeDllSha256, string TerminalClassification, string FirstFailureSite, long TerminalSequence, long TerminalElapsedMonotonicTicks, IReadOnlyList<TimingEvent> Events, GateSnapshot? Gate, GateSnapshot? RunVerify, PriorAttemptIntegrityEvidence? PriorAttemptIntegrity, ProtectionSnapshot? ProtectionBefore, IReadOnlyList<ProtectedFile>? ProtectionAfter, CandidateEvidence? Candidate, OracleEvidence? Oracle, SentinelEvidence? Sentinel, AuditSnapshot? Audit, TeardownEvidence? Teardown, string Limitation);

internal sealed record JsonSnapshot(string RawSha256, string CanonicalSha256, int RawLength, int CanonicalLength);
internal sealed record JsonDiff(string Path, string Category, string? LiveType, string? CandidateType, object? LiveSummary, object? CandidateSummary);
internal sealed record ComponentComparison(string Component, JsonSnapshot Live, JsonSnapshot Candidate, bool RawEqual, bool CanonicalEqual, int TotalDiffCount, bool DiffsTruncated, IReadOnlyList<JsonDiff> Diffs);
internal sealed record StoreCallEvidence(string Phase, int Index, string Method, string? Path, IReadOnlyList<object> ArgumentSummaries);
internal sealed record ReferenceNode(string Path, string Type, int RuntimeIdentity, string Kind, int? Count);
internal sealed record ReferenceCheck(bool ManagerIndependent, bool SettingsIndependent, bool PrefsIndependent, bool ProgressIndependent, int LiveMutableNodeCount, int CandidateMutableNodeCount, IReadOnlyList<string> SharedMutableReferences, IReadOnlyList<ReferenceNode> CandidateCollections);
