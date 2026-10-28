using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpikeInv006B;

internal sealed class ScopeSafeSession
{
    private readonly object _eventLock = new();
    private readonly List<TimingEvent> _events = new();
    private readonly long _started = Stopwatch.GetTimestamp();
    private int _enabled = 1;
    private int _sealState;
    private int _writerStarted;
    private long _sequence;
    private object? _winnerTicket;
    private ClosedSnapshot? _closedSnapshot;

    internal object SyncRoot => _eventLock;

    internal bool IsObserverRunningFirst()
        => Volatile.Read(ref _sealState) == (int)TerminalClassification.Running
           && Volatile.Read(ref _enabled) != 0;

    internal TerminalClassification Classification => (TerminalClassification)Volatile.Read(ref _sealState);

    internal bool TryCommit(EventKind kind, ObservedTaskStatus? status, out TimingEvent? committed)
    {
        committed = null;
        if (!IsObserverRunningFirst()) return false;
        lock (_eventLock)
        {
            if (!IsObserverRunningFirst()) return false;
            committed = new TimingEvent(++_sequence, Stopwatch.GetTimestamp() - _started, kind, status);
            _events.Add(committed);
            return true;
        }
    }

    internal object? TrySeal(TerminalClassification classification, FailureSite site, GateSnapshot? gate, GateSnapshot? verify)
    {
        if (classification == TerminalClassification.Running) return null;
        if (Interlocked.CompareExchange(ref _sealState, (int)classification, (int)TerminalClassification.Running)
            != (int)TerminalClassification.Running) return null;

        Volatile.Write(ref _enabled, 0);
        var ticket = new object();
        Volatile.Write(ref _winnerTicket, ticket);
        lock (_eventLock)
        {
            var terminal = new TimingEvent(++_sequence, Stopwatch.GetTimestamp() - _started, EventKind.Terminal);
            var closed = new List<TimingEvent>(_events.Count + 1);
            closed.AddRange(_events);
            closed.Add(terminal);
            _closedSnapshot = new ClosedSnapshot(classification, site, terminal.Sequence,
                terminal.ElapsedMonotonicTicks, closed.AsReadOnly(), gate, verify);
        }
        return ticket;
    }

    internal bool TryWriteClosedArtifact(object? ticket, Func<ClosedSnapshot, bool> writer)
    {
        if (ticket is null || !ReferenceEquals(ticket, Volatile.Read(ref _winnerTicket))
            || Volatile.Read(ref _sealState) == (int)TerminalClassification.Running) return false;
        if (Interlocked.CompareExchange(ref _writerStarted, 1, 0) != 0) return false;
        var snapshot = Volatile.Read(ref _closedSnapshot);
        return snapshot is not null && writer(snapshot);
    }

    internal int ClosedEventCountForTests => Volatile.Read(ref _closedSnapshot)?.Events.Count ?? -1;

    internal static int Identity(object value) => RuntimeHelpers.GetHashCode(value);
}
