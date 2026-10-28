namespace ReplayMod;

// One clock for all tracks/cues; equal timestamps retain native call order, not per-card playback loops.
internal static class PresentationTiming
{
    internal static int Frame(IReadOnlyList<double> times,double elapsed,int previous)
    {
        if(times.Count==0)throw new InvalidDataException("empty-display-timeline");
        var index=Math.Clamp(previous,0,times.Count-1);
        while(index+1<times.Count&&times[index+1]<=elapsed)index++;
        return index;
    }
    internal static double Blend(double from,double to,double elapsed)=>to<=from?0:Math.Clamp((elapsed-from)/(to-from),0,1);
    internal static bool Once(double? at,double elapsed,ref bool delivered)
    {
        if(delivered||at is not {} time||elapsed<time)return false;
        delivered=true;return true;
    }
    internal static void ValidateChoice(double? entry,double? ready,double end)
    {
        if(entry is null&&ready is null)return;
        if(entry is not {} at||ready is not {} done||!double.IsFinite(at)||!double.IsFinite(done)||at<0||done<at||done>end)
            throw new InvalidDataException("invalid-display-choice-boundary");
    }
    internal static void Validate(IReadOnlyList<double> times)
    {
        if(times.Count==0||times[0]<0||times.Any(t=>!double.IsFinite(t))||times.Where((t,i)=>i>0&&t<times[i-1]).Any())
            throw new InvalidDataException("invalid-display-timeline");
    }
}
