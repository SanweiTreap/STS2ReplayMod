using System.Text.Json.Serialization;

namespace SpikeInv006A;

internal static class ProbeConstants
{
    internal const string SchemaId = "spike-inv006a.timing.v1";
    internal const string ProbeVersion = "0.1.0-timing-v1";
    internal const string GameVersion = "v0.111.0";
    internal const string GameCommit = "41cef1ea";
    internal const long GameDllLength = 9_757_184;
    internal const string GameDllSha256 = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    internal const int MissingEventTimeoutSeconds = 120;
    internal const string ArtifactFile = @"C:\Projects\STS2ReplayMod\evidence\spike-inv006a\runtime-artifact.json";
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum TerminalClassification
{
    Running = 0,
    PassScopeSafeGate = 1,
    StopMissingOuterTask = 2,
    FailEventOrder = 3,
    FailSafety = 4,
    BlockedIdentity = 5,
    BlockedInstrumentation = 6
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum FailureSite
{
    None,
    Identity,
    GraphicsPrefix,
    GraphicsPostfix,
    GraphicsFinalizer,
    OuterPostfix,
    OuterContinuation,
    StartupContinuation,
    MenuCallback,
    GateEvaluation,
    VerifyCallback,
    TimeoutCallback,
    ArtifactWriter,
    DuplicateOuter,
    DuplicateGraphics,
    ManagerIdentity,
    RunState,
    CurrentRunTask,
    MissingEvent
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum EventKind
{
    PatchActive,
    GraphicsEntry,
    GraphicsNormalExit,
    GraphicsFault,
    OuterCloudReturned,
    OuterCloudTerminal,
    StartupMarkerTerminal,
    MenuReady,
    GateReady,
    Verify,
    Terminal
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum ObservedTaskStatus { Null, Pending, RanToCompletion, Faulted, Canceled }

internal sealed record TimingEvent(long Sequence, long ElapsedMonotonicTicks, EventKind Kind, ObservedTaskStatus? TaskStatus = null);

internal sealed record GateConditions(
    bool PatchBeforeGraphics,
    bool GraphicsNormalAndIdle,
    bool ExactlyOneOuterTask,
    bool OuterTaskTerminal,
    bool StartupMarkerTerminal,
    bool MenuReady,
    bool ManagerIdentityStable,
    bool RunStateNull,
    bool CurrentRunTaskNullOrTerminal,
    bool NoTerminalFailure,
    bool NoPostGateLifecycle);

internal sealed record GateSnapshot(
    long Sequence,
    int OriginalManagerRuntimeIdentity,
    int OuterTaskRuntimeIdentity,
    ObservedTaskStatus OuterTaskStatus,
    ObservedTaskStatus StartupTaskStatus,
    ObservedTaskStatus CurrentRunTaskStatus,
    bool RunStateNull,
    int GraphicsEntryCount,
    int GraphicsNormalExitCount,
    int GraphicsActiveCount,
    GateConditions Conditions);

internal sealed record ClosedSnapshot(
    TerminalClassification TerminalClassification,
    FailureSite FirstFailureSite,
    long TerminalSequence,
    long TerminalElapsedMonotonicTicks,
    IReadOnlyList<TimingEvent> Events,
    GateSnapshot? Gate,
    GateSnapshot? Verify);

internal sealed record RuntimeArtifact(
    string SchemaId,
    string ProbeVersion,
    string GameVersion,
    string GameCommit,
    long GameDllLength,
    string GameDllSha256,
    long ProbeDllLength,
    string ProbeDllSha256,
    string TerminalClassification,
    string FirstFailureSite,
    long TerminalSequence,
    long TerminalElapsedMonotonicTicks,
    IReadOnlyList<TimingEvent> Events,
    GateSnapshot? Gate,
    GateSnapshot? Verify);
