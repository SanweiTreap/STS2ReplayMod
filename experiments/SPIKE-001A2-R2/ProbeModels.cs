using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Spike001A2R2;

internal static class ProbeConstants
{
    internal const string Id = "SPIKE001A2R2";
    internal const string Version = "0.1.2-r3";
    internal const string Schema = "spike-001a2-r2.reconstruction-control.v3";
    internal const string GameVersion = "v0.111.0";
    internal const string GameCommit = "41cef1ea";
    internal const long GameDllLength = 9_757_184;
    internal const string GameDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-001a2-r2";
    internal const int QuietFrames = 30;
    internal const string ArmFile = "arm-a-r3.json";
    internal const string BaselineFile = "checkpoint-baseline-r3.json";
    internal const string SnapshotAFile = "snapshot-a-r3.json";
    internal const string PostAFile = "post-a-checkpoint-verification-r3.json";
    internal const string BPreFile = "b-precontinue-r3.json";
    internal const string SnapshotBFile = "snapshot-b-r3.json";
    internal const string FinalFile = "final-audit-r3.json";
    internal const string TerminalFile = "terminal-r3.json";
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
internal sealed record CheckpointEvidence(FileIdentity File, string RawSha256, string CanonicalSha256, int NumReloads,
    bool PreFinishedRoomNull, string Seed, string Character, int ActIndex, int TotalFloor, int? MapRow, int? MapCol);
internal sealed record BaselineArtifact(string SchemaId, string ProbeVersion, string CreatedUtc, int ProcessId,
    EnvironmentEvidence Environment, CheckpointEvidence Checkpoint, string CheckpointCanonicalJson, ProtectedState Protection);
internal sealed record ArmArtifact(string SchemaId, string ProbeVersion, string CreatedUtc, int ProcessId,
    EnvironmentEvidence Environment, CheckpointEvidence MapCheckpointBeforeNode, ProtectedState PreBaselineObservation);
internal sealed record ReadinessSnapshot(string Phase, bool IsPartOfPlayerTurn, bool PlayerActionsDisabled,
    bool CombatIsPaused, bool RunIsPaused, bool IsInProgress, bool IsOverOrEnding, bool ExecutorIsRunning,
    bool ExecutorIsPaused, string? CurrentAction, bool CurrentRunNodePresent, int HandCount,
    int QuietFramesObserved, long FirstReadyFrame, long SnapshotFrame, bool CandidateReady);
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
    RngSnapshot Rng, ReadinessSnapshot Readiness, string CapturedUtc, int ProcessId);
internal sealed record SnapshotArtifact(string SchemaId, string ProbeVersion, string Arm, string CreatedUtc,
    int ProcessId, EnvironmentEvidence Environment, CheckpointEvidence BaselineCheckpoint,
    FileIdentity CheckpointAtCapture, ProtectedState ProtectionAtCapture, CombatSnapshot Snapshot);
internal sealed record JsonDiff(string Path, JsonNode? Before, JsonNode? After);
internal sealed record SaveDiffResult(bool Passed, int BeforeNumReloads, int AfterNumReloads,
    IReadOnlyList<JsonDiff> AllDiffs, IReadOnlyList<JsonDiff> NonAllowlistedDiffs, string Failure);
internal sealed record ProtectionDiffResult(bool Passed, bool InventoryEqual, IReadOnlyList<string> ContentChanges,
    IReadOnlyList<string> LengthChanges, IReadOnlyList<string> MtimeChanges, IReadOnlyList<string> NonAllowlistedMtimeChanges);
internal sealed record VerificationArtifact(string SchemaId, string ProbeVersion, string CreatedUtc, int ProcessId,
    string Stage, FileIdentity BaselineCheckpoint, FileIdentity CurrentCheckpoint, bool CheckpointContentEqual,
    ProtectionDiffResult Protection, bool Passed, string Classification);
internal sealed record FinalArtifact(string SchemaId, string ProbeVersion, string CreatedUtc, int ProcessId,
    string Classification, IReadOnlyList<JsonDiff> SnapshotDiff, SaveDiffResult SaveDiff,
    ProtectionDiffResult Protection, string FirstDifference, string Limitation);
internal sealed record TerminalArtifact(string SchemaId, string ProbeVersion, string CreatedUtc, int ProcessId,
    string Classification, string Reason);
