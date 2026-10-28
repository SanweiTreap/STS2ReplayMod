namespace SpikeInv007A;

internal enum StartupGateState { Waiting, GateReady, RunAccepted, TimedOut }

internal sealed class StartupGateArbiter
{
    private readonly object _sync = new();
    private StartupGateState _state;

    internal StartupGateState State { get { lock (_sync) return _state; } }

    internal bool TryCommitGateReady()
    {
        lock (_sync)
        {
            if (_state != StartupGateState.Waiting) return false;
            _state = StartupGateState.GateReady;
            return true;
        }
    }

    internal bool TryAcceptRun()
    {
        lock (_sync)
        {
            if (_state != StartupGateState.GateReady) return false;
            _state = StartupGateState.RunAccepted;
            return true;
        }
    }

    internal bool TryTimeout()
    {
        lock (_sync)
        {
            if (_state != StartupGateState.Waiting) return false;
            _state = StartupGateState.TimedOut;
            return true;
        }
    }
}

internal static class AttemptArtifactPolicy
{
    internal static bool CanStartR3(bool priorArtifactExists, bool r3ArtifactExists, bool r3TemporaryExists)
        => priorArtifactExists && !r3ArtifactExists && !r3TemporaryExists;
}

internal static class InstallIdentityPolicy
{
    internal const int ExactTargetCount = 2;
    internal const long R2DllLength = 174_592;
    internal const string R2DllSha256 = "5447E69F11041639ED9DD92E152D1B78B7A9A6A6025906C7BDD9900929021ACD";
    internal const long R2ManifestLength = 370;
    internal const string R2ManifestSha256 = "AA910BF2376FE5642372FF8C64E8F3138708F843BA2335112611F3E954E32869";

    internal static bool CanReplace(int targetCount, long dllLength, string dllSha256,
        long manifestLength, string manifestSha256)
        => targetCount == ExactTargetCount
           && dllLength == R2DllLength
           && string.Equals(dllSha256, R2DllSha256, StringComparison.Ordinal)
           && manifestLength == R2ManifestLength
           && string.Equals(manifestSha256, R2ManifestSha256, StringComparison.Ordinal);
}
