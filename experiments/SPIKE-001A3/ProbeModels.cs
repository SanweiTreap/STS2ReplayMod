using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Spike001A3;

internal static class ProbeConstants
{
    internal const string Id = "SPIKE001A3";
    internal const string Version = "0.1.0-r2";
    internal const string Schema = "spike-001a3.reconstruction-control.v2";
    internal const string PredecessorSchema = "spike-001a3.reconstruction-control.v1";
    internal const string AttemptPrefix = "spike-001a3-r2-";
    internal const string GameVersion = "v0.111.0";
    internal const string GameCommit = "41cef1ea";
    internal const long GameDllLength = 9_757_184;
    internal const string GameDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const string Inv010ReportSha256 = "59D52227F5146253CEF41C5E93EDBC5AC4195EEE754CD4DCE6F39000F5FC61E0";
    internal const string Inv010ContractSha256 = "54D99A392E5B113E78F770984100549E486A19D06145469E1D22BC54F1B02CF1";
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-001a3";
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

internal sealed record FileIdentity(string Path, bool Exists, long? Length, string? Sha256, string? LastWriteUtc);
internal sealed record ProfileInventory(IReadOnlyList<string> Directories, IReadOnlyList<string> Files);
internal sealed record ProtectedState(string ProfileRoot, IReadOnlyList<FileIdentity> Files, ProfileInventory Inventory);
internal sealed record EnvironmentEvidence(string GameVersion, string GameCommit, long GameDllLength, string GameDllSha256,
    string ProbeVersion, long ProbeDllLength, string ProbeDllSha256, IReadOnlyList<string> LoadedModIds,
    int ActiveProfileId, string ProfileToken, bool IsRunningModded, bool SteamCloudAccountEnabled,
    bool SteamCloudAppEnabled, bool CloudOff, IReadOnlyList<string> Failures);
internal sealed record TargetIdentity(string Seed, string Character, int ActIndex, int TotalFloor,
    int MapRow, int MapCol, string PointType);
internal sealed record CheckpointEvidence(FileIdentity FirstRead, FileIdentity SecondRead, string RawSha256,
    string CanonicalSha256, int NumReloads, bool PreFinishedRoomNull, string Seed, string Character,
    int ActIndex, int FloorReached, int? MapRow, int? MapCol);
internal sealed record ArmArtifact(string SchemaId, string ProbeVersion, string AttemptId, string CreatedUtc, int ProcessId,
    EnvironmentEvidence Environment, TargetIdentity Target, FileIdentity NonNullSaveAtArm,
    CheckpointEvidence MapCheckpointBeforeNode, ProtectedState PreBaselineObservation);
internal sealed record BaselineArtifact(string SchemaId, string ProbeVersion, string AttemptId, string CreatedUtc, int ProcessId,
    EnvironmentEvidence Environment, TargetIdentity Target, CheckpointEvidence Checkpoint,
    string CheckpointCanonicalJson, FileIdentity Recovery, ProtectedState Protection,
    string ProductionTaskCompletedUtc, long ProductionTaskCompletedMonotonicTicks);
internal sealed record FieldObservation(string Name, string Status, string? Value, string Site,
    string? ExceptionType, string? ExceptionMessage);
internal sealed record ObjectIdentities(string? State, string? Player, string? PlayerCombatState, string? Hand,
    string? Combat, string? Executor, string? Room);
internal sealed record RawReadinessSample(long Tick, ulong EngineFrame, long MonotonicTicks, string CreatedUtc,
    IReadOnlyList<FieldObservation> Fields, ObjectIdentities Identities, string TargetIdentity,
    bool RequiredReady, int QuietFramesObserved, string? Breaker);
internal sealed record TelemetryEvent(string Kind, long Tick, ulong EngineFrame, string Detail, RawReadinessSample Sample);
internal sealed record ExceptionSummary(string Site, string ExceptionType, string Message, int Count,
    long FirstTick, long LastTick, string FirstUtc, string LastUtc);
internal sealed record DriverHeartbeat(string Event, string CreatedUtc, long Tick, ulong EngineFrame, long MonotonicTicks,
    string? Parent, string? Path, ulong InstanceId, bool InstanceValid, bool InsideTree, bool ProcessEnabled,
    string EffectiveProcessMode, bool TreePaused);
internal sealed record TelemetrySnapshot(IReadOnlyList<TelemetryEvent> Events, int DroppedEvents,
    IReadOnlyList<ExceptionSummary> Exceptions, RawReadinessSample? LastRawSample,
    IReadOnlyList<DriverHeartbeat> Heartbeats, int DroppedHeartbeats, long TotalTicks, long CombatTicks,
    long? FirstGlobalTick, long? FirstBaselineTick, long? FirstCombatTick, long LastTick,
    ulong LastEngineFrame, long LastMonotonicTicks);
internal sealed record CardSnapshot(string ModelId, string SerializableCanonicalJson,
    IReadOnlyDictionary<string, string?> InstanceSemantics);
internal sealed record PositionSnapshot(int ActIndex, int? MapRow, int? MapCol, int? RoomId, string RoomModelId,
    string RoomType, string EncounterId, int TotalFloor, int CombatRound);
internal sealed record PlayerSnapshot(string CharacterModelId, int Hp, int MaxHp, int Energy,
    IReadOnlyList<CardSnapshot> Hand, IReadOnlyList<CardSnapshot> Draw,
    IReadOnlyList<CardSnapshot> Discard, IReadOnlyList<CardSnapshot> Exhaust);
internal sealed record EnemySnapshot(string ModelId, uint? CombatId, int Hp, int MaxHp, bool IsAlive,
    bool IsDead, bool IsEscaping);
internal sealed record RngEntrySnapshot(string StableName, int Counter, ulong State0, ulong State1, ulong State2, ulong State3);
internal sealed record RngSnapshot(string Seed, string DigestSha256, IReadOnlyList<RngEntrySnapshot> Entries);
internal sealed record CombatSnapshot(PositionSnapshot Position, PlayerSnapshot Player, IReadOnlyList<EnemySnapshot> Enemies,
    RngSnapshot Rng, RawReadinessSample Readiness, long QuietStartTick, ulong QuietStartFrame,
    long SnapshotTick, ulong SnapshotFrame, string CapturedUtc, int ProcessId);
internal sealed record SnapshotArtifact(string SchemaId, string ProbeVersion, string AttemptId, string Arm,
    string CreatedUtc, int ProcessId, EnvironmentEvidence Environment, CheckpointEvidence BaselineCheckpoint,
    FileIdentity CheckpointAtCapture, ProtectedState ProtectionAtCapture, CombatSnapshot Snapshot,
    TelemetrySnapshot Telemetry);
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
