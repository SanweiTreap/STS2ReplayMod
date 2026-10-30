namespace ReplayMod;

// Only pure phase timers may enter this broker. It never receives a game action,
// hook, animation completion or turn-loop task, and never cancels the native timer.
internal sealed class RetiringPhaseWait(Action<Exception> lateFault)
{
    private TaskCompletionSource _release=new();
    internal bool Active {get;private set;}
    internal int Released {get;private set;}
    internal int Skipped {get;private set;}
    internal void Begin()
    {
        Codec.Require(!Active,"phase-retirement-already-active");
        Active=true;Released=0;Skipped=0;_release.TrySetResult();
    }
    internal void End(){Active=false;_release=new();}
    internal Task Wrap(Task timer)
    {
        if(timer.IsCompleted)return timer;
        if(Active){Skipped++;_ =Observe(timer);return Task.CompletedTask;}
        return Await(timer,_release.Task);
    }
    private async Task Await(Task timer,Task release)
    {
        var winner=await Task.WhenAny(timer,release);
        if(ReferenceEquals(winner,timer)||timer.IsCompleted){await timer;return;}
        Released++;_ =Observe(timer);
    }
    private async Task Observe(Task timer)
    {
        try{await timer;}catch(OperationCanceledException) { }
        catch(Exception error){lateFault(error);}
    }
}
