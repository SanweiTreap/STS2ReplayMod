using System.Text.Json.Serialization;

namespace SpikeInv006B;

internal static class ProbeConstants
{
    internal const string SchemaId = "spike-inv006b.direct-write.v1";
    internal const string ProbeVersion = "0.1.0-direct-write-v1";
    internal const string GameVersion = "v0.111.0";
    internal const string GameCommit = "41cef1ea";
    internal const string GameBranch = "v0.111.0";
    internal const long GameDllLength = 9_757_184;
    internal const string GameDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const string FixtureSha256 = "4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631";
    internal const int FixtureLength = 20_441;
    internal const string FixturePath = @"C:\Projects\STS2ReplayMod\evidence\spike-inv003a\protected-backup\20260907T175924.5461327Z\profile1\replays\latest.mcr";
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv006b";
    internal const string ArtifactFile = @"C:\Projects\STS2ReplayMod\evidence\spike-inv006b\runtime-artifact.json";
    internal const int MissingEventTimeoutSeconds = 120;
    internal const string ProgressLogicalName = "modded/profile1/saves/progress.save";
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum TerminalClassification { Running = 0, PassDirectWriteIsolated, FailPreoverrideEquivalence, FailBoundary, FailMutationOrWrite, FailTeardown, FailSafety, StopMissingTimingGate, BlockedIdentity, BlockedPrecondition, BlockedInstrumentation, BlockedArtifactWrite }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum FailureSite { None, Identity, GraphicsPrefix, GraphicsPostfix, GraphicsFinalizer, OuterPostfix, OuterContinuation, StartupContinuation, MenuCallback, GateEvaluation, RunCallback, TimeoutCallback, ArtifactWriter, DuplicateOuter, DuplicateGraphics, ManagerIdentity, RunState, CurrentRunTask, ProtectionBaseline, Candidate, Boundary, Mutation, Write, Cleanup, RestoreManager, MainMenu, ProtectionAfter }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum EventKind { PatchActive, GraphicsEntry, GraphicsNormalExit, GraphicsFault, OuterCloudReturned, OuterCloudTerminal, StartupMarkerTerminal, MenuReady, GateReady, RunAccepted, BaselineReady, CandidateReady, OverrideInstalled, SceneReady, ProgressUpdated, ProgressSaved, TeardownStarted, ManagerRestored, MainMenuRestored, ProtectionVerified, Terminal }
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
internal sealed record CandidateEvidence(bool ProfileIsOne, bool ProfileRawEqual, bool ProfileCanonicalEqual, bool SettingsRawEqual, bool SettingsCanonicalEqual, bool PrefsRawEqual, bool PrefsCanonicalEqual, bool ProgressRawEqual, bool ProgressCanonicalEqual, bool RootsIndependent, int SharedMutableReferences, bool StoreContained, bool InitializationMemoryOnly, bool OriginalCanonicalUnchanged);
internal sealed record SentinelEvidence(bool LegalScene, bool FixtureEncounterMatched, bool SpawnedEnemiesMatched, bool FullMutationEqual, bool DiscoveredEnemiesEqual, bool NoWriteBeforeSave, int CandidateUpdateCount, int CandidateSaveCount, int CandidateWriteCount, bool SynchronousWrite, bool ExactProgressLogicalName, bool PayloadDeserialized, bool PayloadCanonicalEqual);
internal sealed record AuditSnapshot(BoundaryFailure FirstFailure, bool OriginalManagerSeen, bool UnknownManagerSeen, bool OriginalStoreSeen, bool UnknownStoreSeen, bool ExternalBoundarySeen, int CandidateUpdateCount, int CandidateSaveCount, int CandidateWriteCount, bool CandidateWriteSynchronous, bool CandidateWriteExactLogicalName, bool CandidatePayloadEquivalent);
internal sealed record TeardownEvidence(TeardownStage LastStage, bool CleanupCalledExactlyOnce, bool OverrideClearRestoredOriginal, bool OriginalManagerRestored, bool RunStateNull, bool ContinueVisible, bool ProtectionUnchanged);
internal sealed record RuntimeArtifact(string SchemaId, string ProbeVersion, string GameVersion, string GameCommit, string GameBranch, long GameDllLength, string GameDllSha256, long ProbeDllLength, string ProbeDllSha256, string TerminalClassification, string FirstFailureSite, long TerminalSequence, long TerminalElapsedMonotonicTicks, IReadOnlyList<TimingEvent> Events, GateSnapshot? Gate, GateSnapshot? RunVerify, ProtectionSnapshot? ProtectionBefore, IReadOnlyList<ProtectedFile>? ProtectionAfter, CandidateEvidence? Candidate, SentinelEvidence? Sentinel, AuditSnapshot? Audit, TeardownEvidence? Teardown, string Limitation);

internal sealed record JsonSnapshot(string RawSha256, string CanonicalSha256, int RawLength, int CanonicalLength);
internal sealed record JsonDiff(string Path, string Category, string? LiveType, string? CandidateType, object? LiveSummary, object? CandidateSummary);
internal sealed record ComponentComparison(string Component, JsonSnapshot Live, JsonSnapshot Candidate, bool RawEqual, bool CanonicalEqual, int TotalDiffCount, bool DiffsTruncated, IReadOnlyList<JsonDiff> Diffs);
internal sealed record StoreCallEvidence(string Phase, int Index, string Method, string? Path, IReadOnlyList<object> ArgumentSummaries);
internal sealed record ReferenceNode(string Path, string Type, int RuntimeIdentity, string Kind, int? Count);
internal sealed record ReferenceCheck(bool ManagerIndependent, bool SettingsIndependent, bool PrefsIndependent, bool ProgressIndependent, int LiveMutableNodeCount, int CandidateMutableNodeCount, IReadOnlyList<string> SharedMutableReferences, IReadOnlyList<ReferenceNode> CandidateCollections);
