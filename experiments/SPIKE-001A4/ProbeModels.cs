using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Spike001A4;

internal static class ProbeConstants
{
    internal const string Id = "SPIKE001A4";
    internal const string Version = "0.1.1-r2";
    internal const string Schema = "spike-001a4.reconstruction-control.v2";
    internal const string AttemptPrefix = "spike-001a4-r2-";
    internal const string GameVersion = "v0.111.0";
    internal const string GameCommit = "41cef1ea";
    internal const long GameDllLength = 9_757_184;
    internal const string GameDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const string Inv011ReportSha256 = "7BE386861AC14433F4C61FB42A93C3C9879AF6B2B7B0E42D6463C2F50CAF2DBB";
    internal const string Inv011ContractSha256 = "1527728A8481D98D357D4484D7A3269153F2F9A918B2657A02A3C9096511CFB7";
    internal const string Inv011AuditSha256 = "C5CF56B9C11CF4D43893ADE5E4C5E73AD9F0DDBEDAD262787AE5F6C609258843";
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-001a4";
    internal const int QuietFrames = 30;
    internal const int CaptureDeadlineSeconds = 30;
    internal const int TelemetryEventLimit = 256;
    internal const int PeriodicSampleTicks = 15;
    internal const string ArmFile = "arm.json";
    internal const string BaselineFile = "checkpoint-baseline.json";
    internal const string BaselineRecoveryFile = "recovery/checkpoint-baseline.save";
    internal const string SnapshotAFile = "snapshot-a.json";
    internal const string PostAFile = "post-a-checkpoint-verification.json";
    internal const string BPreFile = "b-precontinue.json";
    internal const string SnapshotBFile = "snapshot-b.json";
    internal const string FinalFile = "final-audit.json";
    internal const string TerminalFile = "terminal.json";
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ProbePhase { FixturePreparation, AwaitA, WaitForAExit, ReadyBContinue, AwaitB, Terminal }

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum BindingState { Unbound, Bound }

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ObservationStatus { Value, Missing, ReadException }

internal sealed record CheckpointFloorBinding(BindingState State, int? Value)
{
    internal static CheckpointFloorBinding Unbound() => new(BindingState.Unbound, null);
    internal static CheckpointFloorBinding Bound(int value) => new(BindingState.Bound, value);
}

internal sealed record FileIdentity(string Path, bool Exists, long? Length, string? Sha256, string? LastWriteUtc);
internal sealed record ProfileInventory(IReadOnlyList<string> Directories, IReadOnlyList<string> Files);
internal sealed record ProtectedState(string ProfileRoot, IReadOnlyList<FileIdentity> Files, ProfileInventory Inventory);
internal sealed record EnvironmentEvidence(string? GameVersion, string? GameCommit, long GameDllLength, string GameDllSha256,
    string ProbeVersion, long ProbeDllLength, string ProbeDllSha256, IReadOnlyList<string> LoadedModIds,
    int? ActiveProfileId, string? ProfileToken, bool? IsRunningModded, bool? SteamCloudAccountEnabled,
    bool? SteamCloudAppEnabled, bool? CloudOff, IReadOnlyList<string> Failures);
internal sealed record TargetIdentity(string Seed, string Character, int ActIndex, int ArmPredecessorFloor,
    int ExpectedPlayableFloor, int MapRow, int MapCol, string PointType);
internal sealed record SemanticExpectation(string RoomModelId, string EncounterId);
internal sealed record CheckpointEvidence(FileIdentity FirstRead, FileIdentity SecondRead, string RawSha256,
    string CanonicalSha256, int NumReloads, bool PreFinishedRoomNull, string Seed, string Character,
    int ActIndex, int FloorReached, int MapRow, int MapCol);
internal sealed record ArmArtifact(string SchemaId, string ProbeVersion, string AttemptId, string CreatedUtc, int ProcessId,
    EnvironmentEvidence Environment, TargetIdentity Target, CheckpointFloorBinding CheckpointFloor,
    FileIdentity NonNullSaveAtArm, CheckpointEvidence MapCheckpointBeforeNode, ProtectedState PreBaselineObservation);
internal sealed record BaselineArtifact(string SchemaId, string ProbeVersion, string AttemptId, string CreatedUtc, int ProcessId,
    EnvironmentEvidence Environment, TargetIdentity Target, CheckpointFloorBinding CheckpointFloor,
    CheckpointEvidence Checkpoint, string CheckpointCanonicalJson, FileIdentity Recovery, ProtectedState Protection,
    string ProductionTaskCompletedUtc, long ProductionTaskCompletedMonotonicTicks);
internal sealed record FieldObservation(string Name, ObservationStatus Status, string? Value, string Site,
    string? ExceptionType, string? ExceptionMessage);
internal sealed record ObjectIdentities(string? State, string? Player, string? PlayerCombatState, string? Hand,
    string? Combat, string? Executor, string? Room);
internal sealed record RawReadinessSample(long Tick, ulong EngineFrame, long MonotonicTicks, string CreatedUtc,
    IReadOnlyList<FieldObservation> Fields, ObjectIdentities Identities, string TargetIdentity,
    bool RequiredReady, int QuietFramesObserved, string? Breaker);
internal sealed record TelemetryEvent(string Kind, long Tick, ulong EngineFrame, string Detail, RawReadinessSample Sample);
internal sealed record ExceptionSummary(string Site, string? ExceptionType, string? Message, int Count,
    long FirstTick, long LastTick, string FirstUtc, string LastUtc);
internal sealed record DriverHeartbeat(string Event, string CreatedUtc, long Tick, ulong EngineFrame, long MonotonicTicks,
    string? Parent, string? Path, ulong InstanceId, bool InstanceValid, bool InsideTree, bool ProcessEnabled,
    string EffectiveProcessMode, bool TreePaused);
internal sealed record TelemetrySnapshot(IReadOnlyList<TelemetryEvent> Events, int DroppedEvents,
    IReadOnlyList<ExceptionSummary> Exceptions, RawReadinessSample? LastRawSample,
    IReadOnlyList<DriverHeartbeat> Heartbeats, int DroppedHeartbeats, long TotalTicks, long CombatTicks,
    long? FirstGlobalTick, long? FirstBaselineTick, long? FirstCombatTick, long LastTick,
    ulong LastEngineFrame, long LastMonotonicTicks);

internal sealed record CardSnapshot(string ModelId, string SerializableCanonicalJson, int CurrentUpgradeLevel,
    bool IsUpgraded, string EnergyCost, int BaseReplayCount, bool ExhaustOnNextPlay, int? FloorAddedToDeck);
internal sealed record PositionSnapshot(int ActIndex, int MapRow, int MapCol, int RoomId, string RoomModelId,
    string RoomType, string EncounterId, int TotalFloor, int CombatRound);
internal sealed record PlayerSnapshot(string CharacterModelId, int Hp, int MaxHp, int Energy,
    IReadOnlyList<CardSnapshot> Hand, IReadOnlyList<CardSnapshot> Draw,
    IReadOnlyList<CardSnapshot> Discard, IReadOnlyList<CardSnapshot> Exhaust);
internal sealed record EnemySnapshot(string ModelId, uint CombatId, int Hp, int MaxHp, bool IsAlive,
    bool IsDead, bool IsEscaping);
internal sealed record RngEntrySnapshot(string StableName, int Counter, ulong State0, ulong State1, ulong State2, ulong State3);
internal sealed record RngSnapshot(string Seed, string DigestSha256, IReadOnlyList<RngEntrySnapshot> Entries);
internal sealed record CombatSnapshot(PositionSnapshot Position, PlayerSnapshot Player, IReadOnlyList<EnemySnapshot> Enemies,
    RngSnapshot Rng, RawReadinessSample Readiness, long QuietStartTick, ulong QuietStartFrame,
    long SnapshotTick, ulong SnapshotFrame, string CapturedUtc, int ProcessId);
internal sealed record SemanticProjection(PositionSnapshot Position, PlayerSnapshot Player,
    IReadOnlyList<EnemySnapshot> Enemies, RngSnapshot Rng);
internal sealed record SnapshotArtifact(string SchemaId, string ProbeVersion, string AttemptId, string Arm,
    string CreatedUtc, int ProcessId, EnvironmentEvidence Environment, CheckpointEvidence BaselineCheckpoint,
    FileIdentity CheckpointAtCapture, ProtectedState ProtectionAtCapture, CombatSnapshot Snapshot,
    SemanticProjection Semantic, TelemetrySnapshot Telemetry);
internal sealed record JsonDiff(string Path, JsonNode? Before, JsonNode? After);
internal sealed record SaveDiffResult(bool Passed, int BeforeNumReloads, int AfterNumReloads,
    IReadOnlyList<JsonDiff> AllDiffs, IReadOnlyList<JsonDiff> NonAllowlistedDiffs, string Failure);
internal sealed record ProtectionDiffResult(bool Passed, bool InventoryEqual, IReadOnlyList<string> ContentChanges,
    IReadOnlyList<string> LengthChanges, IReadOnlyList<string> MtimeChanges, IReadOnlyList<string> NonAllowlistedMtimeChanges);
internal sealed record VerificationArtifact(string SchemaId, string ProbeVersion, string AttemptId, string CreatedUtc,
    int ProcessId, string Stage, FileIdentity BaselineCheckpoint, FileIdentity CurrentCheckpoint,
    bool CheckpointContentEqual, ProtectionDiffResult Protection, bool NoExistingRunState, bool Passed, string Classification);
internal sealed record FinalArtifact(string SchemaId, string ProbeVersion, string AttemptId, string CreatedUtc, int ProcessId,
    string Classification, IReadOnlyList<JsonDiff> SnapshotDiff, SaveDiffResult SaveDiff,
    ProtectionDiffResult Protection, string FirstDifference, string Limitation);
internal sealed record TerminalArtifact(string SchemaId, string ProbeVersion, string AttemptId, string CreatedUtc,
    int ProcessId, string Classification, string FailureSite, string Reason, CheckpointEvidence? BaselineIdentity,
    TelemetrySnapshot Telemetry);
internal sealed record PrecheckArtifact(string SchemaId, string ProbeVersion, string CreatedUtc, int ProcessId,
    EnvironmentEvidence Environment, string Classification);
