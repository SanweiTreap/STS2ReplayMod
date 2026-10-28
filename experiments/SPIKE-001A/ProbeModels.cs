using System.Text.Json.Nodes;

namespace Spike001A;

internal static class ProbeConstants
{
    public const string ProbeVersion = "0.1.0-spike";
    public const int SchemaVersion = 1;
    public const string ExpectedGameVersion = "v0.111.0";
    public const string ExpectedCommit = "41cef1ea";
    public const string ExpectedDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
}

internal sealed record EnvironmentIdentity(
    string GameVersion,
    string Commit,
    string Branch,
    string Sts2DllPath,
    string Sts2DllSha256,
    string ProbeVersion,
    int ProbeSchemaVersion,
    string ProbeAssemblyPath,
    string ProbeArtifactSha256,
    string OutputDirectory,
    int ProcessId,
    string SessionStartedUtc,
    IReadOnlyList<LoadedModInfo> LoadedMods,
    bool MatchesFixedEnvironment,
    IReadOnlyList<string> Mismatches);

internal sealed record LoadedModInfo(
    string Id,
    string Name,
    string Version,
    bool AffectsGameplay,
    string LoadState,
    IReadOnlyList<ModAssemblyInfo> Assemblies);

internal sealed record ModAssemblyInfo(string Path, string Sha256);

internal sealed record FixtureMetadata(
    int SchemaVersion,
    string ProbeVersion,
    string CreatedUtc,
    int CreatedProcessId,
    string CreatedSessionStartedUtc,
    string GameVersion,
    string Commit,
    string Sts2DllSha256,
    string ProbeArtifactSha256,
    string FixtureFile,
    string FixtureSha256,
    string Character,
    string Seed,
    string GameMode,
    int Ascension,
    IReadOnlyList<LoadedModInfo> LoadedMods,
    FileIdentity LiveSaveBefore,
    FileIdentity LiveSaveAfter,
    IReadOnlyList<JsonDiffEntry> PrePostToSaveDiff);

internal sealed record WhitelistSnapshot(
    PositionSnapshot Position,
    PlayerSnapshot Player,
    IReadOnlyList<EnemySnapshot> Enemies,
    RngSnapshot Rng,
    ReadinessSnapshot Readiness);

internal sealed record PositionSnapshot(
    int ActIndex,
    int? MapRow,
    int? MapCol,
    int? RoomId,
    string RoomModelId,
    string RoomType,
    int TotalFloor,
    int CombatRound);

internal sealed record PlayerSnapshot(
    string CharacterModelId,
    int Hp,
    int Energy,
    IReadOnlyList<string> Hand,
    IReadOnlyList<string> DrawPileMultiset,
    IReadOnlyList<string> DiscardPileMultiset,
    IReadOnlyList<string> ExhaustPileMultiset);

internal sealed record EnemySnapshot(
    string ModelId,
    uint? CombatId,
    int Hp,
    bool IsAlive);

internal sealed record RngSnapshot(
    string Seed,
    string DigestSha256,
    IReadOnlyList<RngEntrySnapshot> Entries);

internal sealed record RngEntrySnapshot(
    string Key,
    int Counter,
    ulong State0,
    ulong State1,
    ulong State2,
    ulong State3);

internal sealed record ReadinessSnapshot(
    string Phase,
    bool IsPartOfPlayerTurn,
    bool PlayerActionsDisabled,
    bool CombatIsPaused,
    bool RunIsPaused,
    bool IsInProgress,
    bool IsOverOrEnding,
    bool ExecutorIsRunning,
    bool ExecutorIsPaused,
    string? CurrentAction,
    bool CurrentRunNodePresent,
    bool CandidateReady);

internal sealed record FileIdentity(string Path, bool Exists, long? Length, string? Sha256, string? LastWriteUtc);

internal sealed record StageObservation(
    string Name,
    string StartedUtc,
    string FinishedUtc,
    long ElapsedMilliseconds,
    string Outcome,
    string? ExceptionType,
    string? ExceptionMessage,
    string? ExceptionStackTrace);

internal sealed record JsonDiffEntry(string Path, JsonNode? Expected, JsonNode? Actual);

internal sealed record RestoreAttemptResult(
    int Attempt,
    string StartedUtc,
    string FinishedUtc,
    int ProcessId,
    string SessionStartedUtc,
    bool AfterFullRestart,
    string FixtureSha256,
    bool EnvironmentValidated,
    IReadOnlyList<string> EnvironmentMismatches,
    IReadOnlyList<StageObservation> Stages,
    string? FirstFailure,
    WhitelistSnapshot? Expected,
    WhitelistSnapshot? Actual,
    IReadOnlyList<JsonDiffEntry> Diff,
    FileIdentity LiveSaveBefore,
    FileIdentity LiveSaveAfter,
    bool LiveSaveUnchanged,
    bool Passed);

internal sealed record ProbeSummary(
    string UpdatedUtc,
    int AttemptCount,
    int PassedAttemptCount,
    bool HasPassedAttemptAfterFullRestart,
    bool RestoreBaselinePassed,
    string Conclusion,
    IReadOnlyList<string> ResultFiles);
