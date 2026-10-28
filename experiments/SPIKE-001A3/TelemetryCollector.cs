namespace Spike001A3;

internal sealed class TelemetryCollector
{
    private readonly object _gate = new();
    private readonly List<TelemetryEvent> _events = [];
    private readonly List<DriverHeartbeat> _heartbeats = [];
    private readonly Dictionary<string, MutableException> _exceptions = new(StringComparer.Ordinal);
    private Dictionary<string, string>? _lastFieldState;
    private int _droppedEvents; private int _droppedHeartbeats;
    private long _totalTicks; private long _combatTicks; private long? _firstGlobalTick;
    private long? _firstBaselineTick; private long? _firstCombatTick;
    private long _lastTick; private ulong _lastFrame; private long _lastMonotonic;
    private RawReadinessSample? _lastSample;

    internal void TickSeen(long tick, ulong frame, long monotonic, bool baseline, bool combat)
    {
        lock (_gate)
        {
            _totalTicks++; _firstGlobalTick ??= tick;
            if (baseline) _firstBaselineTick ??= tick;
            if (combat) { _combatTicks++; _firstCombatTick ??= tick; }
            _lastTick = tick; _lastFrame = frame; _lastMonotonic = monotonic;
        }
    }

    internal void RecordSample(RawReadinessSample sample, bool breaker)
    {
        lock (_gate)
        {
            _lastSample = sample;
            var current = sample.Fields.ToDictionary(x => x.Name,
                x => x.Status + "|" + x.Value + "|" + x.ExceptionType + "|" + x.ExceptionMessage,
                StringComparer.Ordinal);
            var first = _lastFieldState is null;
            var changed = first || current.Count != _lastFieldState!.Count
                || current.Any(x => !_lastFieldState.TryGetValue(x.Key, out var prior) || prior != x.Value);
            var exceptional = sample.Fields.Any(x => x.Status == "exception");
            var periodic = sample.Tick % ProbeConstants.PeriodicSampleTicks == 0;
            if (first || changed || exceptional || periodic || breaker)
                AddEvent(new(first ? "first" : breaker ? "breaker" : exceptional ? "exception" : changed ? "transition" : "periodic",
                    sample.Tick, sample.EngineFrame, sample.Breaker ?? "sample", sample));
            foreach (var field in sample.Fields.Where(x => x.Status == "exception")) Aggregate(field, sample);
            _lastFieldState = current;
        }
    }

    internal void RecordHeartbeat(DriverHeartbeat heartbeat)
    {
        lock (_gate)
        {
            if (_heartbeats.Count < ProbeConstants.TelemetryEventLimit) _heartbeats.Add(heartbeat);
            else _droppedHeartbeats++;
        }
    }

    internal TelemetrySnapshot Snapshot()
    {
        lock (_gate)
        {
            return new(_events.ToList(), _droppedEvents,
                _exceptions.Values.OrderBy(x => x.Site, StringComparer.Ordinal).Select(x => x.Freeze()).ToList(),
                _lastSample, _heartbeats.ToList(), _droppedHeartbeats, _totalTicks, _combatTicks,
                _firstGlobalTick, _firstBaselineTick, _firstCombatTick, _lastTick, _lastFrame, _lastMonotonic);
        }
    }

    private void AddEvent(TelemetryEvent value)
    {
        if (_events.Count < ProbeConstants.TelemetryEventLimit) _events.Add(value); else _droppedEvents++;
    }

    private void Aggregate(FieldObservation field, RawReadinessSample sample)
    {
        var key = field.Site + "|" + field.ExceptionType + "|" + field.ExceptionMessage;
        if (!_exceptions.TryGetValue(key, out var value))
        {
            value = new(field.Site, field.ExceptionType ?? "", field.ExceptionMessage ?? "", sample.Tick, sample.CreatedUtc);
            _exceptions.Add(key, value);
        }
        value.Count++; value.LastTick = sample.Tick; value.LastUtc = sample.CreatedUtc;
    }

    private sealed class MutableException(string site, string type, string message, long firstTick, string firstUtc)
    {
        internal string Site { get; } = site; internal string Type { get; } = type; internal string Message { get; } = message;
        internal int Count { get; set; } internal long FirstTick { get; } = firstTick; internal long LastTick { get; set; } = firstTick;
        internal string FirstUtc { get; } = firstUtc; internal string LastUtc { get; set; } = firstUtc;
        internal ExceptionSummary Freeze() => new(Site, Type, Message, Count, FirstTick, LastTick, FirstUtc, LastUtc);
    }
}
