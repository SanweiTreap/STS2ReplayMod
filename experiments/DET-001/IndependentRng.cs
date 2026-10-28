using System.Numerics;

namespace Det001;

// Independent scalar implementation of the fixed IL state recurrence.
// No production methods or live state are invoked; used solely for valid mutants.
public sealed record RandomState(ulong S0,ulong S1,ulong S2,ulong S3)
{
    public (ulong Value,RandomState State) Next()
    {
        unchecked
        {
            var value=BitOperations.RotateLeft(S1*5,7)*9;
            var t=S1<<17;
            var a2=S2^S0;var a3=S3^S1;var a1=S1^a2;var a0=S0^a3;
            return(value,new(a0,a1,a2^t,BitOperations.RotateLeft(a3,45)));
        }
    }
}
