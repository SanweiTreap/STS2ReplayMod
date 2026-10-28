namespace SpikeInv004A;

internal static class ProbeConstants
{
    internal const string ProbeVersion = "0.1.0-direct-write-sentinel";
    internal const int SchemaVersion = 1;
    internal const string ExpectedGameVersion = "v0.111.0";
    internal const string ExpectedCommit = "41cef1ea";
    internal const string ExpectedDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const string ExpectedFixtureSha256 = "4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631";
    internal const int ExpectedFixtureLength = 20441;
    internal const string ExpectedFixturePath = @"C:\Projects\STS2ReplayMod\evidence\spike-inv003a\protected-backup\20260907T175924.5461327Z\profile1\replays\latest.mcr";
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv004a";
}

internal sealed record FileIdentity(
    string Scope,
    string Path,
    string? RelativePath,
    bool Exists,
    long? Length,
    string? Sha256,
    string? LastWriteUtc);

internal sealed record DirectoryInventory(
    string Root,
    IReadOnlyList<string> Directories,
    IReadOnlyList<string> Files);

internal sealed record ContinueSummary(
    int CurrentActIndex,
    int FloorReached,
    int? LatestMapRow,
    int? LatestMapCol,
    string Seed,
    int NumReloads,
    string GameMode,
    int Ascension,
    int PlayerCount,
    string CanonicalJsonSha256);

internal sealed record ObjectIdentity(
    string Role,
    string Type,
    int RuntimeIdentity,
    IReadOnlyList<string> StoreFields,
    IReadOnlyList<string> ProfileProviderFields,
    bool CloudWrapperDetected,
    string CloudStatus);

internal sealed record SaveTaskObservation(
    string StatusBeforeWait,
    string StatusAfterWait,
    bool IsCompleted,
    bool IsFaulted,
    bool IsCanceled,
    string? Exception);

internal sealed record ModAssemblyIdentity(string Path, string Sha256);

internal sealed record LoadedModIdentity(
    string Id,
    string Name,
    string Version,
    bool AffectsGameplay,
    string State,
    IReadOnlyList<ModAssemblyIdentity> Assemblies);

internal sealed record EnvironmentIdentity(
    string GameVersion,
    string Commit,
    string Branch,
    string Sts2DllPath,
    long Sts2DllLength,
    string Sts2DllSha256,
    string ProbeVersion,
    string ProbeAssemblyPath,
    string ProbeAssemblySha256,
    IReadOnlyList<LoadedModIdentity> LoadedMods,
    IReadOnlyList<string> Mismatches);

internal sealed record BackupEntry(
    string SourcePath,
    string RelativePath,
    string DestinationPath,
    long Length,
    string SourceSha256,
    string DestinationSha256);

internal sealed record PreflightEvidence(
    int SchemaVersion,
    string ProbeVersion,
    string StartedUtc,
    string FinishedUtc,
    int ProcessId,
    string Outcome,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RequiredUserConfirmations,
    EnvironmentIdentity Environment,
    int ActiveProfileId,
    string ProfileDirectoryToken,
    string ProfileRoot,
    string AccountRoot,
    string SettingsPath,
    string CurrentRunPath,
    bool IsRunningModded,
    bool RunManagerStateIsNull,
    bool MainMenuObserved,
    bool ContinueButtonVisible,
    bool ContinueButtonEnabled,
    ContinueSummary? Continue,
    ObjectIdentity SaveManager,
    SaveTaskObservation SaveTask,
    IReadOnlyList<FileIdentity> ProtectedFiles,
    DirectoryInventory ProfileInventory,
    DirectoryInventory AccountTopLevelInventory,
    string? BackupDirectory,
    IReadOnlyList<BackupEntry> BackupEntries,
    string Notes);
