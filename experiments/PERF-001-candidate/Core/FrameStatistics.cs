namespace Perf001Candidate.Core;

public static class FrameStatistics
{
    public static object Build(FrameStamp[] frames,IReadOnlyList<Boundary> boundaries,long frequency)
    {
        var intervals=frames.GroupBy(x=>x.Segment).SelectMany(g=>g.Zip(g.Skip(1),(a,b)=>new{Begin=a.Tick,End=b.Tick,
            Tag=a.Tag==b.Tag?b.Tag.ToString():"CrossBoundary",Milliseconds=Statistics.TicksToMilliseconds(b.Tick-a.Tick,frequency)})).ToArray();
        var formalBegin=boundaries.LastOrDefault(x=>x.Name=="map-idle-begin")?.Tick;
        var formalEnd=boundaries.FirstOrDefault(x=>x.Name=="next-player-turn-first-observed-success-and-idle")?.Tick;
        var formal=intervals.Where(x=>formalBegin.HasValue&&formalEnd.HasValue&&x.End>formalBegin&&x.Begin<formalEnd).ToArray();
        return new{Frequency=frequency,Stamps=frames.Length,Intervals=intervals.Length,
            AllRecordedSamplingSegmentsIncludingRetainedInterruptedWindows=Statistics.Describe(intervals.Select(x=>x.Milliseconds)),
            Segments=intervals.GroupBy(x=>x.Tag).Select(g=>new{Tag=g.Key,Summary=Statistics.Describe(g.Select(x=>x.Milliseconds))}).ToArray(),
            FormalWindowBeginTick=formalBegin,FormalWindowEndTick=formalEnd,
            FormalIntersectingWholeIntervals=Statistics.Describe(formal.Select(x=>x.Milliseconds)),
            Rule="Adjacent intervals are formed only within each explicit Segment; never bridge unlimited main-menu/manual waiting gaps. All retains interrupted preparation windows. Formal view begins at latest idle attempt; sampling-windows declares eligibility. Complete intervals can extend past raw bounds; no continuous whole-session/GPU/present inference.",
            TailCoverage="Final post-drain frame closes qualification/drain interval; later metadata/commit I/O is outside frame window and must not be claimed as sampled frames."};
    }
}
