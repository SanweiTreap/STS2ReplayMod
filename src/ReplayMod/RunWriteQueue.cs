using System.Diagnostics;

namespace ReplayMod;

// A snapshot contains plain records and immutable payloads. Copy the lists
// the recorder mutates, and keep at most one pending snapshot per active run.
internal sealed class RunWriteQueue(RunStore store)
{
    private readonly object _gate=new();
    private RunDocument? _pending;
    private Task? _worker;
    private Exception? _failure;
    private int _queued,_written;
    private double _maxWriteMilliseconds;

    internal void Enqueue(RunDocument run)
    {
        var branches=run.Branches;
        var snapshot=run with
        {
            Visits=[..run.Visits],Operations=[..run.Operations],Continuations=[..run.Continuations],
            Lifecycle=run.Lifecycle is null?null:[..run.Lifecycle],
            Branches=branches is null?null:new RunBranches
            {
                ActiveAttempt=branches.ActiveAttempt,Selected=[..branches.Selected],
                Attempts=[..branches.Attempts],Saves=[..branches.Saves]
            }
        };
        lock(_gate)
        {
            _pending=snapshot;
            _queued++;
            _worker??=Task.Run(WritePending);
        }
    }

    private void WritePending()
    {
        while(true)
        {
            RunDocument next;
            lock(_gate)
            {
                if(_pending is null){_worker=null;return;}
                next=_pending;
                _pending=null;
            }
            var at=Stopwatch.GetTimestamp();
            try{store.Save(next);_failure=null;_written++;}
            catch(Exception ex){_failure=ex;}
            _maxWriteMilliseconds=Math.Max(_maxWriteMilliseconds,Stopwatch.GetElapsedTime(at).TotalMilliseconds);
        }
    }

    // Called at normal run exit on the producer thread, before the library or
    // a subsequent Continue can read the final atomic file.
    internal object Drain()
    {
        Task? worker;
        lock(_gate)worker=_worker;
        worker?.GetAwaiter().GetResult();
        if(_failure is not null)throw new IOException("run-background-write-failed",_failure);
        return new{Queued=_queued,Written=_written,Coalesced=_queued-_written,MaxWriteMilliseconds=_maxWriteMilliseconds};
    }
}
