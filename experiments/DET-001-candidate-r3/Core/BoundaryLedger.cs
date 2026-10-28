namespace Det001.Core;

// Frozen DTOs only. Local tokens and timing never participate in A/B equivalence.
public sealed class BoundaryLedger(ShuffleTrace? expected=null)
{
    private readonly string[]? _expected=expected is null?null:Values(expected);
    private int _count;private EventStamp? _last;
    private readonly FirstAnswer _first=new();
    public int Count=>_count;
    public DifferenceAnswer? Difference=>_first.Value;
    public bool Observe(string name,string normalized,EventStamp stamp,int points,int intents,int accepted,(TaskStatus? Shuffle,TaskStatus? Draw) status)
    {
        if(Difference is not null)return false;
        Contract.Require(_count<5 && name=="E"+_count && stamp.Sequence>0,"shuffle-boundary-ordinal");
        if(_last is not null)TraceContract.Order(_last,stamp);
        _last=stamp;var index=_count++;
        if(_expected is null || _expected[index]==normalized)return true;
        _first.Freeze(new(name,_expected[index],normalized,points,intents,accepted,stamp.Sequence,status.Shuffle?.ToString()??"UnknownNotReturned",status.Draw?.ToString()??"UnknownNotReturned","Already in-flight production is neither cancelled nor skipped; remaining consumption/completion is unknown at this boundary."));
        return false;
    }
    public static string[] Values(ShuffleTrace trace)=>[TraceContract.Normalize(trace.E0),TraceContract.Normalize(trace.E1),TraceContract.Normalize(trace.E2),TraceContract.Normalize(trace.E3),TraceContract.Normalize(trace.E4)];
}
