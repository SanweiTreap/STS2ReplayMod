using System.Diagnostics;

namespace Perf001;

public sealed class OutputBudget(long maximum)
{
    private long _used;
    public long Used => Interlocked.Read(ref _used);
    public bool Reserve(int count)
    {
        while (true)
        {
            var old = Interlocked.Read(ref _used);
            if (count < 0 || old > maximum - count) return false;
            if (Interlocked.CompareExchange(ref _used, old + count, old) == old) return true;
        }
    }
}

public sealed record WriterOutcome(bool Complete, string? Failure, long Accepted, long Written,
    long EnqueuedBytes, long WrittenBytes, int PeakQueuedRecords, long PeakQueuedBytes,
    int CapacityRecords, long CapacityBytes, int MaximumRecordBytes, int Overflows, int Rejections,
    long WriteCalls, long FlushCalls, long WriteTicks, long FlushTicks, long WriterWallTicks,
    long WriterThreadAllocatedBytes, bool ThreadStopped, bool CompletionRequested);

public sealed class BoundedWriter
{
    private readonly object _sync = new();
    private readonly Queue<FrozenRecord> _queue = new();
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new();
    private readonly int _capacity, _maxRecord, _expected;
    private readonly long _capacityBytes;
    private readonly OutputBudget _budget;
    private readonly ManualResetEventSlim? _testGate;
    private readonly int? _faultSequence;
    private bool _completeAdding, _stopped, _abort;
    private string? _failure;
    private long _accepted, _written, _queuedBytes, _acceptedBytes, _writtenBytes;
    private int _peakRecords, _overflows, _rejections;
    private long _peakBytes, _writeCalls, _flushCalls, _writeTicks, _flushTicks, _wallTicks, _allocated;
    public BoundedWriter(string path, int expected, OutputBudget budget, int capacity = 128,
        long capacityBytes = 2 * 1024 * 1024, int maximumRecordBytes = 16384,
        ManualResetEventSlim? testGate = null, int? faultSequence = null)
    {
        if (expected < 0 || capacity <= 0 || capacityBytes <= 0 || maximumRecordBytes <= 0)
            throw new ArgumentException("writer-limits");
        _expected = expected; _budget = budget; _capacity = capacity; _capacityBytes = capacityBytes;
        _maxRecord = maximumRecordBytes; _testGate = testGate; _faultSequence = faultSequence;
        _thread = new Thread(() => Run(path)) { IsBackground = true, Name = "PERF001-owned-writer" };
        _thread.Start();
        if (!_ready.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException("writer-not-ready");
    }
    public bool TryEnqueue(FrozenRecord record)
    {
        lock (_sync)
        {
            if (_failure is not null || _completeAdding || record.Sequence != _accepted + 1 || record.Length > _maxRecord)
            { _rejections++; _failure ??= "enqueue-state-sequence-or-record-size"; return false; }
            if (_queue.Count >= _capacity || _queuedBytes > _capacityBytes - record.Length)
            { _overflows++; _failure ??= "bounded-queue-overflow"; return false; }
            _queue.Enqueue(record); _accepted++; _queuedBytes += record.Length; _acceptedBytes += record.Length;
            _peakRecords = Math.Max(_peakRecords, _queue.Count); _peakBytes = Math.Max(_peakBytes, _queuedBytes);
            Monitor.PulseAll(_sync); return true;
        }
    }
    public void CompleteAdding() { lock (_sync) { _completeAdding = true; Monitor.PulseAll(_sync); } }
    public WriterOutcome Finish(TimeSpan timeout)
    {
        if (!_thread.Join(timeout)) { lock (_sync) { _failure ??= "not-drained-before-deadline"; _abort = true; Monitor.PulseAll(_sync); } }
        lock (_sync)
        {
            var success = _failure is null && _completeAdding && _stopped && _queue.Count == 0 &&
                _accepted == _expected && _written == _expected && _acceptedBytes == _writtenBytes && _flushCalls == 1;
            return new(success, success ? null : _failure ?? "not-completed-or-count-mismatch", _accepted, _written,
                _acceptedBytes, _writtenBytes, _peakRecords, _peakBytes, _capacity, _capacityBytes, _maxRecord,
                _overflows, _rejections, _writeCalls, _flushCalls, _writeTicks, _flushTicks, _wallTicks,
                _allocated, _stopped, _completeAdding);
        }
    }
    private void Run(string path)
    {
        var start = Stopwatch.GetTimestamp(); var allocation = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536,
                FileOptions.SequentialScan);
            _ready.Set();
            if (_testGate is not null && !_testGate.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException("test-gate");
            while (true)
            {
                FrozenRecord record;
                lock (_sync)
                {
                    while (_queue.Count == 0 && !_completeAdding && !_abort) Monitor.Wait(_sync, 3000);
                    if (_abort) throw new InvalidDataException("writer-aborted");
                    if (_queue.Count == 0 && _completeAdding) break;
                    record = _queue.Dequeue(); _queuedBytes -= record.Length;
                }
                if (_faultSequence == record.Sequence) throw new IOException("injected-owned-writer-fault");
                if (!_budget.Reserve(record.Length)) throw new InvalidDataException("output-budget-exceeded");
                var before = Stopwatch.GetTimestamp(); stream.Write(record.Bytes.Span);
                var elapsedWrite = Stopwatch.GetTimestamp() - before;
                lock (_sync)
                {
                    _writeTicks += elapsedWrite; _writeCalls++;
                    _written++; _writtenBytes += record.Length;
                }
            }
            var flush = Stopwatch.GetTimestamp(); stream.Flush(); // managed/OS flush, NOT a durable fsync assertion
            var elapsedFlush = Stopwatch.GetTimestamp() - flush;
            lock (_sync) { _flushTicks = elapsedFlush; _flushCalls = 1; }
        }
        catch (Exception ex) { lock (_sync) { _failure ??= "writer-" + ex.GetType().Name; } }
        finally
        {
            lock (_sync)
            {
                _allocated = GC.GetAllocatedBytesForCurrentThread() - allocation;
                _wallTicks = Stopwatch.GetTimestamp() - start; _stopped = true;
            }
            _ready.Set();
        }
    }
}
