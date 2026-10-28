using System.Diagnostics;

namespace Perf001Candidate.Core;

public sealed record WriterReport(bool Complete,string? Failure,int Accepted,int Written,long EnqueuedBytes,long WrittenBytes,
    int PeakRecords,long PeakBytes,int CapacityRecords,long CapacityBytes,int MaximumRecordBytes,int Overflows,int Rejections,
    int AbortedQueuedRecords,int WriteCalls,int FlushCalls,long WriteTicks,long FlushTicks,long WallTicks,long AllocatedBytes,bool ThreadStopped);
public sealed class WriterTestOptions
{
    public int CapacityRecords {get;init;}=128;
    public long CapacityBytes {get;init;}=2*1024*1024;
    public int MaximumRecordBytes {get;init;}=16384;
    public ManualResetEventSlim? Gate {get;init;}
    public int? FaultBeforeWrite {get;init;}
    public bool FailOpen {get;init;}
}
public sealed class RecordWriter
{
    private readonly object _sync=new();private readonly Queue<(int Sequence,byte[] Bytes)> _queue=new();
    private readonly Thread _thread;private readonly ManualResetEventSlim _ready=new();private readonly ManualResetEventSlim _abortSignal=new();
    private readonly WriterTestOptions _options;private readonly OwnedStore _store;
    private bool _addingComplete,_stopped,_aborted;private string? _failure;
    private int _accepted,_written,_peakRecords,_overflows,_rejections,_abortedRecords,_writeCalls,_flushCalls;
    private long _queuedBytes,_enqueuedBytes,_writtenBytes,_peakBytes,_writeTicks,_flushTicks,_wallTicks,_allocated;
    public RecordWriter(OwnedStore store,WriterTestOptions? test=null)
    {
        _store=store;_options=test??new();
        Identity.Require(_options.CapacityRecords>0&&_options.CapacityBytes>0&&_options.MaximumRecordBytes>0,"writer-limits");
        _thread=new(Run){IsBackground=true,Name="PERF001-owned-finite-writer"};_thread.Start();
        if(!_ready.Wait(TimeSpan.FromSeconds(1))) {Abort("writer-open-timeout");_thread.Join(250);throw new TimeoutException("writer-open-timeout");}
    }
    public string? Failure {get{lock(_sync)return _failure;}}
    // Bytes already owned/frozen; no model, engine object, delegate or Task crosses to the writer.
    public bool Enqueue(int sequence,ReadOnlySpan<byte> serialized)
    {
        lock(_sync)
        {
            if(_failure is not null||_addingComplete||_aborted||sequence!=_accepted+1||serialized.Length+1>_options.MaximumRecordBytes)
            {_rejections++;_failure??="writer-state-sequence-or-size";return false;}
            var length=serialized.Length+1;
            if(_queue.Count>=_options.CapacityRecords||_queuedBytes>_options.CapacityBytes-length)
            {_overflows++;_failure??="writer-queue-overflow";return false;}
            var frozen=new byte[length];serialized.CopyTo(frozen);frozen[^1]=10;
            _queue.Enqueue((sequence,frozen));_accepted++;_queuedBytes+=length;_enqueuedBytes+=length;
            _peakRecords=Math.Max(_peakRecords,_queue.Count);_peakBytes=Math.Max(_peakBytes,_queuedBytes);Monitor.PulseAll(_sync);return true;
        }
    }
    public void Abort(string reason)
    {
        lock(_sync){_failure??=reason;_aborted=true;_addingComplete=true;_abortedRecords+=_queue.Count;_queue.Clear();_queuedBytes=0;Monitor.PulseAll(_sync);}
        _abortSignal.Set();
    }
    public WriterReport Stop(int expected,bool observationSucceeded,TimeSpan? timeout=null)
    {
        if(!observationSucceeded)Abort("observation-ended-with-failure");
        else lock(_sync){_addingComplete=true;Monitor.PulseAll(_sync);}
        if(!_thread.Join(timeout??TimeSpan.FromSeconds(1))) {Abort("writer-drain-timeout");_thread.Join(250);}
        lock(_sync)
        {
            var good=observationSucceeded&&_failure is null&&_stopped&&_addingComplete&&_queue.Count==0&&
                _accepted==expected&&_written==expected&&_enqueuedBytes==_writtenBytes&&_flushCalls==1;
            return new(good,good?null:_failure??"writer-count-or-not-stopped",_accepted,_written,_enqueuedBytes,_writtenBytes,
                _peakRecords,_peakBytes,_options.CapacityRecords,_options.CapacityBytes,_options.MaximumRecordBytes,_overflows,_rejections,
                _abortedRecords,_writeCalls,_flushCalls,_writeTicks,_flushTicks,_wallTicks,_allocated,_stopped);
        }
    }
    private void Run()
    {
        var begin=Stopwatch.GetTimestamp();var allocation=GC.GetAllocatedBytesForCurrentThread();
        try
        {
            if(_options.FailOpen)throw new IOException("injected-owned-open");
            using var file=new FileStream(_store.PathFor("events.ndjson"),FileMode.CreateNew,FileAccess.Write,FileShare.Read,65536,FileOptions.SequentialScan);
            _ready.Set();
            if(_options.Gate is not null)WaitHandle.WaitAny(new[]{_options.Gate.WaitHandle,_abortSignal.WaitHandle},1000);
            while(true)
            {
                (int Sequence,byte[] Bytes) item;
                lock(_sync)
                {
                    while(_queue.Count==0&&!_addingComplete&&!_aborted)Monitor.Wait(_sync);
                    if(_aborted)throw new InvalidDataException("writer-aborted");
                    if(_queue.Count==0&&_addingComplete)break;
                    item=_queue.Dequeue();_queuedBytes-=item.Bytes.Length;
                }
                if(_options.FaultBeforeWrite==item.Sequence)throw new IOException("injected-owned-write");
                _store.Reserve(item.Bytes.Length);
                var start=Stopwatch.GetTimestamp();file.Write(item.Bytes);var elapsed=Stopwatch.GetTimestamp()-start;
                lock(_sync){_writeTicks+=elapsed;_writeCalls++;_written++;_writtenBytes+=item.Bytes.Length;}
            }
            var flush=Stopwatch.GetTimestamp();file.Flush();var duration=Stopwatch.GetTimestamp()-flush;
            lock(_sync){_flushTicks=duration;_flushCalls++;}
        }
        catch(Exception ex){lock(_sync){_failure??="writer-"+ex.GetType().Name;}}
        finally {lock(_sync){_wallTicks=Stopwatch.GetTimestamp()-begin;_allocated=GC.GetAllocatedBytesForCurrentThread()-allocation;_stopped=true;}_ready.Set();}
    }
}
public sealed class Recorder
{
    private readonly IClock _clock;private readonly RecordWriter _writer;
    private readonly List<RecordedEvent> _expected=[];private readonly List<RecorderSample> _samples=[];
    private static readonly EventKind[] Order=Enum.GetValues<EventKind>();
    public IReadOnlyList<RecordedEvent> Expected=>_expected;public IReadOnlyList<RecorderSample> Samples=>_samples;
    public Recorder(IClock clock,RecordWriter writer){_clock=clock;_writer=writer;}
    public void Record(EventKind kind,long tick,Func<Payload> capture)
    {
        Identity.Require(_expected.Count<Order.Length&&Order[_expected.Count]==kind,"candidate-event-order");
        var allocation=GC.GetAllocatedBytesForCurrentThread();var start=_clock.Now;
        var payload=capture();var independent=payload.Detached();var frozen=_clock.Now-start;
        var value=new RecordedEvent(_expected.Count+1,kind,tick,payload);
        var before=_clock.Now;var bytes=Identity.Bytes(value);var encoding=_clock.Now-before;
        before=_clock.Now;var accepted=_writer.Enqueue(value.Sequence,bytes);var enqueuing=_clock.Now-before;
        Identity.Require(accepted,"candidate-enqueue-refused");
        _expected.Add(value with{Payload=independent});
        _samples.Add(new(value.Sequence,frozen,encoding,enqueuing,GC.GetAllocatedBytesForCurrentThread()-allocation));
    }
}
