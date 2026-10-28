using System.Text.Json.Serialization;

namespace SpikeInv008A;

internal static class ProbeConstants
{
    internal const string SchemaId = "spike-inv008a.owner-fault.v6";
    internal const string ProbeVersion = "0.6.0-owner-fault-matrix-r6-b1";
    internal const string ManifestVersion = "0.6.0";
    internal const string GameVersion = "v0.111.0";
    internal const string GameCommit = "41cef1ea";
    internal const string GameBranch = "v0.111.0";
    internal const long GameDllLength = 9_757_184;
    internal const string GameDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const string FixtureSha256 = "4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631";
    internal const int FixtureLength = 20_441;
    internal const string FixturePath = @"C:\Projects\STS2ReplayMod\evidence\spike-inv003a\protected-backup\20260907T175924.5461327Z\profile1\replays\latest.mcr";
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a";
    internal const string V1SetupArtifact = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a\setup-owner-fault.json";
    internal const long V1SetupArtifactLength = 2_359;
    internal const string V1SetupArtifactSha256 = "56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48";
    internal const string V1InvalidArtifact = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a\invalid-phase.json";
    internal const long V1InvalidArtifactLength = 1_479;
    internal const string V1InvalidArtifactSha256 = "A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F";
    internal const string R2PrecheckArtifact = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a\precheck-modset-r2-998e07af004a4268b1c33dd2e7644bc5.json";
    internal const long R2PrecheckArtifactLength = 242;
    internal const string R2PrecheckArtifactSha256 = "2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916";
    internal const string R3SetupArtifact = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a\setup-owner-fault-r3.json";
    internal const long R3SetupArtifactLength = 3_740;
    internal const string R3SetupArtifactSha256 = "5C8E49B98F7BC9A647033F779DC3CACCEBE3D840214E4D95995EBADB7C0B13F0";
    internal const string R4SetupArtifact = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a\setup-owner-fault-r4.json";
    internal const long R4SetupArtifactLength = 23_241;
    internal const string R4SetupArtifactSha256 = "40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040";
    internal const string R4RecoveryRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a\restricted-recovery\r4-setup-df7ceb3e2c724e73a6bc0c368d69aec5";
    internal const int R4RecoveryFileCount = 12;
    internal const string R4RecoveryAggregateSha256 = "DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C";
    internal const string Inv009IlOutput = @"C:\Projects\STS2ReplayMod\evidence\inv-009\static-lobby-query-output.txt";
    internal const long Inv009IlOutputLength = 253_870;
    internal const string Inv009IlOutputSha256 = "488814C65B9CD8E56F888CFCFE50AD7BEAA890E0396F084BED3916CFBDF772D7";
    internal const string Inv009Summary = @"C:\Projects\STS2ReplayMod\evidence\inv-009\r4-bounded-summary.txt";
    internal const long Inv009SummaryLength = 2_063;
    internal const string Inv009SummarySha256 = "2B50415302BBE5D38874C6AA5CA29EAF2012FA7D4DE923AA842A8AD26762D9D0";
    internal const string R5SetupArtifact = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a\setup-owner-fault-r5.json";
    internal const long R5SetupArtifactLength = 25_790;
    internal const string R5SetupArtifactSha256 = "9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9";
    internal const string R5RecoveryRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv008a\restricted-recovery\r5-setup-e41635f71d044ca29e8105ae8a007522";
    internal const int R5RecoveryFileCount = 12;
    internal const string R5RecoveryAggregateSha256 = "DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C";
    internal const long R4SettingsLength = 5_238;
    internal const string R4SettingsSha256 = "2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F";
    internal const string PriorArtifactFile = @"C:\Projects\STS2ReplayMod\evidence\spike-inv007a\runtime-artifact-r3.json";
    internal const long PriorArtifactLength = 19_953;
    internal const string PriorArtifactSha256 = "AE4D9998CAF03DA3FB2F17656E12A0216B7160FD69017154B128C3F6365C9553";
    internal const string PriorRecoveryRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv007a\restricted-recovery\r3-20260914T152630.2908445Z";
    internal const int PriorRecoveryFileCount = 12;
    internal const string PriorRecoveryAggregateSha256 = "9719C685A08E86786D96DE55309F19C6242A71D8001B1E841BF88B89598AB9E2";
    internal const int MissingEventTimeoutSeconds = 120;
    internal const int TaskWaitSeconds = 10;
    internal const string SetupArtifact = "setup-owner-fault-r6.json";
    internal const string SetupRestartArtifact = "restart-after-setup-r6.json";
    internal const string PostLaunchArtifact = "post-launch-owner-fault-r6.json";
    internal const string PostLaunchRestartArtifact = "restart-after-post-launch-r6.json";
    internal const string InvalidArtifact = "invalid-phase-r6.json";
    internal const string PrecheckSchemaId = "spike-inv008a.modset-precheck.v6";
    internal static readonly IReadOnlySet<string> RestartMtimeWhitelist = new HashSet<string>(StringComparer.Ordinal)
    {
        "MachineSettings:settings.save",
        "ActiveProfile:saves/prefs.save.backup",
        "ActiveProfile:saves/progress.save.backup"
    };
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ProbePhase { SetupOwnerFault, RestartAfterSetup, PostLaunchOwnerFault, RestartAfterPostLaunch, Complete, Invalid }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum TerminalClassification { Running = 0, PassOwnerFaultTeardown, PassRestartIntegrity, FailOwnerFault, FailBoundary, FailProtection, FailTeardownTask, FailTeardownCleanup, FailTeardownRestoreManager, FailTeardownMenu, FailTeardownScene, FailTeardownOrder, FailUnexpectedReplayRecording, BlockedIdentity, BlockedPriorIntegrity, BlockedPriorAttemptUnresolved, BlockedPrecheckModSet, BlockedPhaseOrder, BlockedPrecondition, BlockedPreconditionStaleRunLobby, BlockedInstrumentation, BlockedArtifactWrite }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum FailureSite { None, Identity, ConfigurationPrecheck, PriorAttempt, GraphicsPrefix, GraphicsPostfix, GraphicsFinalizer, OuterPostfix, OuterContinuation, StartupContinuation, MenuCallback, GateEvaluation, RunCallback, TimeoutCallback, ArtifactWriter, DuplicateOuter, DuplicateGraphics, ManagerIdentity, CurrentRunTask, PriorIntegrity, ContinueNotVisible, ContinueNotEnabled, ContinueMissingSaveData, ContinueNumReloadsMismatch, ContinueCanonicalMismatch, PhaseOrder, ProtectionBaseline, Candidate, StaleRunLobby, RunLobbyBoundary, ReplaySetup, ShouldSave, SynchronizerDisable, WriterRecording, AssetsPreload, Launch, SceneInstall, SceneReplacement, ControlledFault, OwnerCatch, OwnerFinally, TeardownOwnership, IsolatedTaskWait1, Cleanup, IsolatedTaskWait2, RestoreManager, OriginalTaskWait, MainMenu, MenuReady, RefreshButtons, RestoredMenuIdentityOrder, ProtectionAfter, RestartReadOnly, Boundary }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum EventKind { PatchActive, GraphicsEntry, GraphicsNormalExit, GraphicsFault, OuterCloudReturned, OuterCloudTerminal, StartupMarkerTerminal, MenuReady, PriorIntegrityVerified, GateReady, RunAccepted, RestartAccepted, PreSetupLobbyVerified, BaselineReady, CandidateReady, OverrideInstalled, RunStateParsed, ReplaySetupComplete, PostSetupLobbyVerified, AssetsPreloaded, LaunchReturned, PostLaunchLobbyVerified, SceneInstalled, PostSceneLobbyVerified, ControlledFaultThrown, OwnerCatch, OwnerFinally, TeardownStarted, IsolatedTaskWait1, CleanupEntry, CleanupReturn, IsolatedTaskWait2, OverrideCleared, ManagerRestored, OriginalTaskChecked, MainMenuLoad, RefreshButtons, MainMenuRestored, SceneReplacementVerified, ProtectionVerified, RestartVerified, Terminal }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ObservedTaskStatus { Null, Pending, RanToCompletion, Faulted, Canceled, TimedOut }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ProtectedScope { ActiveProfile, AccountSelector, MachineSettings }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum BoundaryFailure { None, OriginalManager, UnknownManager, OriginalStore, UnknownStore, CandidateMutator, ExternalBoundary, UserAction, RunLobbyBoundary, LaunchUnexpected, SceneUnexpected, GenerateMap, RoomEntry, ProcessFrame, Dispatcher, GameAction, Victory, Reward, Progress, ReplayRecord, ReplayWrite, ProfileChange, Quit }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum TeardownStage { None, Ownership, ActionsDisabled, IsolatedTaskWait1, Cleanup, IsolatedTaskWait2, OverrideCleared, OriginalTaskWait, MainMenuLoad, MenuReady, ProtectionCheck, Sealed }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum FaultSite { None, AfterSetup, AfterSceneInstall }
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ContinueFailure { None, NotVisible, NotEnabled, MissingSaveData, NumReloadsMismatch, CanonicalMismatch, MissingSchemaField, ReadError }

internal sealed record TimingEvent(long Sequence, long ElapsedMonotonicTicks, EventKind Kind, ObservedTaskStatus? TaskStatus = null);
internal sealed record GateConditions(bool PatchBeforeGraphics, bool GraphicsNormalAndIdle, bool ExactlyOneOuterTask, bool OuterTaskTerminal, bool StartupMarkerTerminal, bool MenuReady, bool ManagerIdentityStable, bool RunStateNull, bool CurrentRunTaskNullOrTerminal, bool NoTerminalFailure, bool NoPostGateLifecycle);
internal sealed record GateSnapshot(long Sequence, int OriginalManagerRuntimeIdentity, int OuterTaskRuntimeIdentity, ObservedTaskStatus OuterTaskStatus, ObservedTaskStatus StartupTaskStatus, ObservedTaskStatus CurrentRunTaskStatus, bool RunStateNull, int GraphicsEntryCount, int GraphicsNormalExitCount, int GraphicsActiveCount, GateConditions Conditions);
internal sealed record ClosedSnapshot(TerminalClassification TerminalClassification, FailureSite FirstFailureSite, long TerminalSequence, long TerminalElapsedMonotonicTicks, IReadOnlyList<TimingEvent> Events, GateSnapshot? Gate, GateSnapshot? Verify);
internal sealed record ProtectedFile(ProtectedScope Scope, string RelativeName, bool Exists, long? Length, string? Sha256, string? LastWriteUtc);
internal sealed record ProfileInventory(IReadOnlyList<string> Directories, IReadOnlyList<string> Files);
internal sealed record BackupEntry(ProtectedScope Scope, string RelativeName, long Length, string SourceSha256, string BackupSha256);
internal sealed record ProtectionSnapshot(IReadOnlyList<ProtectedFile> Files, ProfileInventory ProfileInventory, IReadOnlyList<BackupEntry> Backups);
internal sealed record RecoveryBaseline(ProtectionSnapshot Snapshot, int FileCount, string AggregateSha256);
internal sealed record PriorAttemptEvidence(bool SetupIdentityValid, bool InvalidIdentityValid, bool SetupPreBusinessNull,
    bool InvalidPreBusinessNull, bool V1RecoveryAbsent, bool R2PrecheckIdentityValid, bool R2PrecheckContractValid,
    bool R2BusinessArtifactsAbsent, bool R2RecoveryAbsent, bool R3SetupIdentityValid, bool R3PreBusinessContractValid,
    bool R3LaterArtifactsAbsent, bool R3RecoveryAbsent, bool R4SetupIdentityValid, bool R4ContractValid,
    bool R4LaterArtifactsAbsent, bool R4RecoveryIdentityValid, bool Inv009IlIdentityValid,
    bool Inv009SummaryIdentityValid, bool R5SetupIdentityValid, bool R5ContractValid,
    bool R5LaterArtifactsAbsent, bool R5RecoveryIdentityValid, bool Passed, string Failure);
internal sealed record ModSetPrecheckArtifact(string SchemaId, string ProbeRevision, string AttemptToken, int LoadedModCount, IReadOnlyList<string> LoadedModIds, string Failure);
internal sealed record PriorIntegrityEvidence(bool ArtifactIdentityValid, bool RecoveryIdentityValid, bool ArtifactRecoveryEqual, bool ActiveProfileOne, bool CurrentContentEqual, bool CurrentInventoryEqual, bool SettingsIdentityValid, IReadOnlyList<string> MtimeOnlyChanges, bool MtimeWhitelistPassed, bool ContinueEqual, ContinueEvidence? ContinueBefore, ContinueFailure ContinueFailure, bool RunStateNull, bool ManagerStable, string Source);
internal sealed record CandidateEvidence(bool ProfileIsOne, bool ProfileRawEqual, bool ProfileCanonicalEqual, bool SettingsRawEqual, bool SettingsCanonicalEqual, bool PrefsRawEqual, bool PrefsCanonicalEqual, bool ProgressRawEqual, bool ProgressCanonicalEqual, bool RootsIndependent, int SharedMutableReferences, bool StoreContained, bool InitializationMemoryOnly, bool OriginalCanonicalUnchanged);
internal sealed record StoreCallEvidence(string Phase, int Index, string Method, string? Path, IReadOnlyList<object> ArgumentSummaries);
internal sealed record JsonSnapshot(string RawSha256, string CanonicalSha256, int RawLength, int CanonicalLength);
internal sealed record JsonDiff(string Path, string Category, string? LiveType, string? CandidateType, object? LiveSummary, object? CandidateSummary);
internal sealed record ComponentComparison(string Component, JsonSnapshot Live, JsonSnapshot Candidate, bool RawEqual, bool CanonicalEqual, int TotalDiffCount, bool DiffsTruncated, IReadOnlyList<JsonDiff> Diffs);
internal sealed record ReferenceNode(string Path, string Type, int RuntimeIdentity, string Kind, int? Count);
internal sealed record ReferenceCheck(bool ManagerIndependent, bool SettingsIndependent, bool PrefsIndependent, bool ProgressIndependent, int LiveMutableNodeCount, int CandidateMutableNodeCount, IReadOnlyList<string> SharedMutableReferences, IReadOnlyList<ReferenceNode> CandidateCollections);
internal sealed record TaskObservation(string Point, int RuntimeIdentity, ObservedTaskStatus Status, string? ExceptionType);
internal sealed record FaultEvidence(string AttemptToken, FaultSite ExpectedSite, FaultSite ActualSite, string ExceptionType, int ThrowCount, int CatchCount, int FinallyCount, bool EscapedOwner);
internal sealed record BusinessCounters(int LaunchEntries, int LaunchReturns, int SceneInstalls, int GenerateMap, int RoomEntries, int ProcessFrames, int Dispatches, int Actions, int Victories, int Rewards, int ProgressUpdates, int ProgressSaves, int ReplayRecordInitial, int ReplayWrites);
internal sealed record LobbyObservation(string Point, bool NetServicePresent, string? NetServiceType, bool RunLobbyNull, int RunLobbyRuntimeIdentity);
internal sealed record LobbyOracleEvidence(bool PreSetupPassed, bool ReplayBoundariesPassed, bool IdentityStable, bool Passed, string Failure);
internal sealed record CleanupCallOracleEvidence(bool CoreCallSetPass, bool LobbyApplicable, bool LobbyGroupPass, bool CallSetPass, IReadOnlyList<string> Failures);
internal sealed record SceneEvidence(bool SetupFaultHadNoOwnedNRun, int OwnedNRunRuntimeIdentity, int FaultSceneRuntimeIdentity, bool FaultSceneWasOwnedNRun, int CleanupEntrySceneRuntimeIdentity, int RestoredSceneRuntimeIdentity, bool RestoredSceneIsMainMenu, bool OwnedNRunNoLongerCurrent, bool Passed);
internal sealed record RestoredMenuSignalEvidence(int CandidateRuntimeIdentity, int ReadyRuntimeIdentity,
    int RefreshRuntimeIdentity, int PrefixCount, int ReadyCount, int RefreshCount,
    ObservedTaskStatus MenuReadyStatus, ObservedTaskStatus RefreshButtonsStatus,
    FailureSite FailureSite, string Failure, bool Passed);
internal sealed record CleanupEvidence(int Entries, int Returns, int Exceptions, IReadOnlyList<string> RequiredSteps, IReadOnlyList<string> ReachedSteps, IReadOnlyDictionary<string, int> CallCounts, CleanupCallOracleEvidence CallOracle, bool StateNull, bool ShouldSaveFalse, bool IsCleaningUpFalse, bool LocalNetIdNull, bool NoReplayWrite);
internal sealed record AuditSnapshot(BoundaryFailure FirstFailure, bool OriginalManagerSeen, bool UnknownManagerSeen, bool OriginalStoreSeen, bool UnknownStoreSeen, bool ExternalBoundarySeen, BusinessCounters Business);
internal sealed record ContinueEvidence(bool Visible, bool Enabled, int NumReloads, string CanonicalSha256);
internal sealed record ContinueCapture(ContinueEvidence? Evidence, ContinueFailure Failure);
internal sealed record TeardownEvidence(TeardownStage LastStage, int OwnerCount, IReadOnlyList<TaskObservation> CandidateTasks, IReadOnlyList<TaskObservation> OriginalTasks, CleanupEvidence Cleanup, bool OverrideClearRestoredOriginal, bool ManagerFallbackUsed, bool OriginalManagerRestored, bool MenuReadyObserved, bool RefreshButtonsObserved, RestoredMenuSignalEvidence? RestoredMenuSignals, ContinueEvidence? ContinueAfter, ContinueFailure ContinueFailure, bool ProtectionUnchanged, IReadOnlyList<string> StepErrors);
internal sealed record RestartEvidence(bool ReadOnly, bool NoOverride, bool NoCandidate, bool NoRun, bool FileSetEqual, bool ContentEqual, bool InventoryEqual, IReadOnlyList<string> MtimeOnlyChanges, bool MtimeWhitelistPassed, bool ContinueEqual, ContinueEvidence? ContinueCurrent, ContinueFailure ContinueFailure, bool ManagerBaselinePassed);
internal sealed record RuntimeArtifact(string SchemaId, string ProbeVersion, ProbePhase Phase, string AttemptToken, string GameVersion, string GameCommit, string GameBranch, long GameDllLength, string GameDllSha256, long ProbeDllLength, string ProbeDllSha256, string TerminalClassification, string FirstFailureSite, long TerminalSequence, long TerminalElapsedMonotonicTicks, IReadOnlyList<TimingEvent> Events, PriorAttemptEvidence? PriorAttempt, GateSnapshot? Gate, GateSnapshot? RunVerify, PriorIntegrityEvidence? PriorIntegrity, ContinueEvidence? ContinueBefore, IReadOnlyList<LobbyObservation>? LobbyObservations, LobbyOracleEvidence? LobbyOracle, SceneEvidence? Scene, ProtectionSnapshot? ProtectionBefore, IReadOnlyList<ProtectedFile>? ProtectionAfter, string? RecoveryRoot, CandidateEvidence? Candidate, FaultEvidence? Fault, AuditSnapshot? Audit, TeardownEvidence? Teardown, RestartEvidence? Restart, string? PreviousArtifactSha256, string Limitation);
