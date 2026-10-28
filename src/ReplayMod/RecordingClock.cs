using System.Diagnostics;

namespace ReplayMod;

// Raw elapsed real time, independent of game timescale, UI pauses and the watching presentation clock.
internal static class RecordingClock
{
    internal static long Origin {get;private set;}
    internal static bool Active=>Origin!=0;
    internal static double Now=>Active?Stopwatch.GetElapsedTime(Origin).TotalMilliseconds:throw new InvalidDataException("recording-clock-not-started");
    internal static void Reset()=>Origin=0;
    internal static void Start()=>Origin=Stopwatch.GetTimestamp();
}
