namespace SpikeInv005A;

internal static class ProbeConstants
{
    internal const string ProbeVersion = "0.1.0-startup-barrier";
    internal const int SchemaVersion = 1;
    internal const string ExpectedGameVersion = "v0.111.0";
    internal const string ExpectedCommit = "41cef1ea";
    internal const long ExpectedDllLength = 9757184;
    internal const string ExpectedDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const int MissingEventTimeoutSeconds = 120;
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv005a";
}

internal sealed record Milestone(long Sequence, string Name, string Utc, ulong ProcessFrame, int ManagedThreadId, int? ManagerIdentity, string? RunSaveTaskStatus, object? Detail);
internal sealed record ComponentFingerprint(string RawSha256, string CanonicalSha256, int RawLength, int CanonicalLength);
internal sealed record ComponentSet(IReadOnlyDictionary<string, ComponentFingerprint> Components);
internal sealed record FileIdentity(string Scope, string Path, string? RelativePath, bool Exists, long? Length, string? Sha256, string? LastWriteUtc);
internal sealed record DirectoryInventory(string Root, IReadOnlyList<string> Directories, IReadOnlyList<string> Files);
internal sealed record ProtectionSnapshot(IReadOnlyList<FileIdentity> Files, DirectoryInventory Profile1Inventory);
internal sealed record EnvironmentIdentity(string GameVersion, string Commit, string Branch, string Sts2DllPath, long Sts2DllLength, string Sts2DllSha256, string ProbeAssemblyPath, string ProbeAssemblySha256, IReadOnlyList<string> LoadedModIds, IReadOnlyList<string> Mismatches);
internal sealed record WriterEvent(long Sequence, string Utc, ulong ProcessFrame, int ManagedThreadId, string Layer, string Phase, string Operation, string TargetType, int TargetIdentity, string OwnerRelation, int? OriginalManagerIdentity, string? LogicalPath, bool IsAsync, string? TaskStatus, long? PayloadLength, string? PayloadSha256, string? Exception);
internal sealed record GateSnapshot(string Utc, int ManagerIdentity, string RunSaveTaskStatus, ComponentSet Components, ProtectionSnapshot Protection, long LastWriterEntrySequence, long LastGraphicsEntrySequence);
