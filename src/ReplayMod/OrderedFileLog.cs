using System.Threading.Channels;

namespace ReplayMod;

// Only immutable serialized bytes cross this boundary. No game objects leave the main thread.
internal sealed class OrderedFileLog
{
    private sealed record Entry(string? Path,byte[]? Bytes,TaskCompletionSource? Barrier);
    private readonly Channel<Entry> _queue=Channel.CreateUnbounded<Entry>(new(){SingleReader=true,SingleWriter=false});
    private readonly Task _worker;
    private int _written,_failed;
    internal int Written=>Volatile.Read(ref _written);
    internal int Failed=>Volatile.Read(ref _failed);
    internal OrderedFileLog()=>_worker=Task.Run(async()=>
    {
        await foreach(var entry in _queue.Reader.ReadAllAsync())
        {
            if(entry.Barrier is {} barrier){barrier.TrySetResult();continue;}
            try{await File.WriteAllBytesAsync(entry.Path!,entry.Bytes!);Interlocked.Increment(ref _written);}
            catch{Interlocked.Increment(ref _failed);}
        }
    });
    internal void Write(string path,byte[] bytes)
    {
        if(!_queue.Writer.TryWrite(new(path,bytes,null)))Interlocked.Increment(ref _failed);
    }
    internal Task Drain()
    {
        var barrier=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return _queue.Writer.TryWrite(new(null,null,barrier))?barrier.Task:_worker;
    }
    internal Task Complete(){_queue.Writer.TryComplete();return _worker;}
}
