namespace SpikeInv012A.Core;

// Runs the payload-free identity check before the attempt factory can create evidence files.
public static class PrebusinessGate
{
    public const string BlockedMarker = "SPIKEINV012A PRECHECK_BLOCKED";

    public static bool ExactIdentity(IReadOnlyList<string?> loadedIds, bool managerPresent, int? profileId, bool modded) =>
        loadedIds.Count == 1 && loadedIds[0] == "SPIKEINV012A" &&
        managerPresent && profileId == 1 && modded;

    public static DiagnosticCore? Open(Func<bool> identity, Func<DiagnosticCore> createAttempt, Action<string> log)
    {
        bool passed;
        try { passed = identity(); }
        catch { passed = false; }
        if (!passed)
        {
            log(BlockedMarker);
            return null;
        }
        return createAttempt();
    }

    public static bool RequireTickerOwner(DiagnosticCore core, Func<bool> attach, Action announceTerminal)
    {
        bool attached;
        try { attached = attach(); }
        catch { attached = false; }
        if (!attached) core.PrebusinessOwnerUnavailable();
        if (core.Stage != Stage.Terminal) return true;
        announceTerminal();
        return false;
    }

    // A successful subscription alone is not a runtime heartbeat. The visible PASS
    // marker is emitted only after a real frame callback and its evidence write.
    public static bool ReadyToAnnounce(DiagnosticCore core) =>
        core.Ticks > 0 && core.EvidenceComplete && core.Stage != Stage.Terminal;
}
