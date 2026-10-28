namespace Perf001;

public static class Statistics
{
    public static double TicksToMilliseconds(long ticks, long frequency)
    {
        if (ticks < 0 || frequency <= 0) throw new ArgumentOutOfRangeException(nameof(ticks));
        return (double)ticks / frequency * 1000d;
    }

    // Nearest rank: sorted[ceil(p*n)-1], with p=0 explicitly the minimum.
    public static double Quantile(IEnumerable<double> values, double probability)
    {
        if (!double.IsFinite(probability) || probability < 0 || probability > 1)
            throw new ArgumentOutOfRangeException(nameof(probability));
        var sorted = Validate(values).Order().ToArray();
        if (sorted.Length == 0) throw new ArgumentException("empty-quantile");
        var index = probability == 0 ? 0 : (int)Math.Ceiling(probability * sorted.Length) - 1;
        return sorted[index];
    }

    private static double[] Validate(IEnumerable<double> values)
    {
        var array = values.ToArray();
        if (array.Any(x => !double.IsFinite(x) || x < 0)) throw new ArgumentException("invalid-duration");
        return array;
    }

    public static Summary Describe(IEnumerable<double> values)
    {
        var sorted = Validate(values).Order().ToArray();
        if (sorted.Length == 0)
            return new(0, null, null, null, null, null, null, null, [], [], "Empty window");
        // Small action samples retain their actual values. No tail-stability claim.
        var enough = sorted.Length >= 100;
        var sum = sorted.Sum();
        if (!double.IsFinite(sum)) throw new ArgumentException("duration-sum-overflow");
        return new(sorted.Length, sum, sum / sorted.Length, sorted[0], sorted[^1],
            enough ? Quantile(sorted, .5) : null,
            enough ? Quantile(sorted, .95) : null,
            enough ? Quantile(sorted, .99) : null,
            enough ? [] : sorted,
            new[] { 16.667, 33.333, 50d, 100d }.Select(t => new ThresholdCount(t, sorted.Count(x => x > t))).ToArray(),
            enough ? null : "n<100: raw values/range only; no p50/p95/p99");
    }
}

public sealed record ThresholdCount(double GreaterThanMilliseconds, int Count);
public sealed record Summary(int Count, double? SumMilliseconds, double? MeanMilliseconds,
    double? MinMilliseconds, double? MaxMilliseconds, double? P50Milliseconds,
    double? P95Milliseconds, double? P99Milliseconds, double[] SmallSampleValuesMilliseconds,
    ThresholdCount[] ThresholdCounts, string? QuantilesUnavailableReason);

// Used for future process-frame observations and deterministic timestamp tests.
// The offending raw timestamp is retained; duplicates/reversals invalidate the window.
public sealed class TimestampWindow(long frequency)
{
    private readonly List<long> _raw = [];
    private string? _failure;
    public IReadOnlyList<long> Raw => _raw.AsReadOnly();
    public string? Failure => _failure;
    public void Add(long stamp)
    {
        if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
        if (stamp < 0 || _raw.Count > 0 && stamp <= _raw[^1]) _failure ??= "non-monotonic-or-duplicate-timestamp";
        _raw.Add(stamp);
    }
    public double[] Intervals()
    {
        if (_failure is not null) throw new InvalidDataException(_failure);
        return _raw.Zip(_raw.Skip(1), (a, b) => Statistics.TicksToMilliseconds(b - a, frequency)).ToArray();
    }
}
