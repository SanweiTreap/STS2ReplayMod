namespace SpikeInv004B;

internal static class ProbeConstants
{
    internal const string ProbeVersion = "0.1.0-deep-copy-diagnostic";
    internal const int SchemaVersion = 1;
    internal const string ExpectedGameVersion = "v0.111.0";
    internal const string ExpectedCommit = "41cef1ea";
    internal const string ExpectedDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const string EvidenceRoot = @"C:\Projects\STS2ReplayMod\evidence\spike-inv004b";
}

internal sealed record FileIdentity(string Scope, string Path, string? RelativePath, bool Exists, long? Length, string? Sha256, string? LastWriteUtc);
internal sealed record DirectoryInventory(string Root, IReadOnlyList<string> Directories, IReadOnlyList<string> Files);
internal sealed record BackupEntry(string SourcePath, string RelativePath, string DestinationPath, long Length, string SourceSha256, string DestinationSha256);
internal sealed record JsonSnapshot(string RawSha256, string CanonicalSha256, int RawLength, int CanonicalLength);
internal sealed record JsonDiff(string Path, string Category, string? LiveType, string? CandidateType, object? LiveSummary, object? CandidateSummary);
internal sealed record ComponentComparison(string Component, JsonSnapshot Live, JsonSnapshot Candidate, bool RawEqual, bool CanonicalEqual, int TotalDiffCount, bool DiffsTruncated, IReadOnlyList<JsonDiff> Diffs);
internal sealed record StoreCallEvidence(string Phase, int Index, string Method, string? Path, IReadOnlyList<object> ArgumentSummaries);
internal sealed record ReferenceNode(string Path, string Type, int RuntimeIdentity, string Kind, int? Count);
internal sealed record ReferenceCheck(bool ManagerIndependent, bool SettingsIndependent, bool PrefsIndependent, bool ProgressIndependent, int LiveMutableNodeCount, int CandidateMutableNodeCount, IReadOnlyList<string> SharedMutableReferences, IReadOnlyList<ReferenceNode> CandidateCollections);
