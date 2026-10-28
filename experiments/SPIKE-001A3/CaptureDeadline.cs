using System.Diagnostics;

namespace Spike001A3;

internal sealed class CaptureDeadline
{
    private readonly TimeSpan _duration;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Action<string> _expired;
    private CancellationTokenSource? _source;
    private int _started;

    internal CaptureDeadline(TimeSpan duration, Action<string> expired,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _duration = duration; _expired = expired; _delay = delay ?? Task.Delay;
    }

    internal bool IsStarted => Volatile.Read(ref _started) != 0;
    internal bool IsExpired => IsStarted && Stopwatch.GetElapsedTime(StartedMonotonicTicks) >= _duration;
    internal long StartedMonotonicTicks { get; private set; }
    internal string StartedUtc { get; private set; } = "";

    internal void Start(string site)
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0) return;
        StartedMonotonicTicks = Stopwatch.GetTimestamp(); StartedUtc = DateTimeOffset.UtcNow.ToString("O");
        _source = new CancellationTokenSource(); _ = OwnAsync(site, _source.Token);
    }

    internal void Complete() => _source?.Cancel();

    private async Task OwnAsync(string site, CancellationToken token)
    {
        try { await _delay(_duration, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
        _expired(site);
    }
}
