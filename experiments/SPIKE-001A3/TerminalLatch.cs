namespace Spike001A3;

internal sealed class TerminalLatch
{
    private int _sealed;
    internal bool IsSealed => Volatile.Read(ref _sealed) != 0;
    internal bool TrySeal() => Interlocked.CompareExchange(ref _sealed, 1, 0) == 0;
}
