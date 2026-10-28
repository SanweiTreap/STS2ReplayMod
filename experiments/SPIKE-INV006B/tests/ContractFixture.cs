namespace SpikeInv006B.Tests;

internal sealed class PoisonArgs
{
    internal int Reads;
    internal object Read(int index) { Reads++; throw new InvalidOperationException($"poison {index}"); }
}

internal sealed class IdentityFirstFixture
{
    private readonly object _original, _candidate, _memory;
    internal bool Enabled = true, Sealed;
    internal int ArgumentReads, Failures, CandidateWrites;
    internal IdentityFirstFixture(object original, object candidate, object memory) { _original = original; _candidate = candidate; _memory = memory; }
    internal void Store(object instance, Func<int, object> argument)
    {
        if (Sealed || !Enabled) return;
        if (ReferenceEquals(instance, _original) || !ReferenceEquals(instance, _memory)) { Failures++; return; }
        ArgumentReads++; _ = argument(0); ArgumentReads++; _ = argument(1); CandidateWrites++;
    }
    internal void Manager(object instance)
    {
        if (Sealed || !Enabled) return;
        if (!ReferenceEquals(instance, _candidate)) Failures++;
    }
}
