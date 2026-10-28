namespace Det001.Core;

// Only the creating game thread may bind/sample. Timer receives Flow DTOs only.
// No await, continuation, result, exception retrieval or replacement Task.
public sealed class OriginalTaskScope
{
    private readonly int _thread=Environment.CurrentManagedThreadId;
    private Task? _draw,_shuffle;private bool _closed;
    private void Thread(){Contract.Require(Environment.CurrentManagedThreadId==_thread,"shuffle-task-thread");}
    public void BindDraw(Task task){Thread();Contract.Require(!_closed && _draw is null && task is not null,"shuffle-draw-task-bind");_draw=task;}
    public void BindShuffle(Task task){Thread();Contract.Require(!_closed && _shuffle is null && task is not null,"shuffle-task-bind");_shuffle=task;}
    public (TaskStatus? Shuffle,TaskStatus? Draw) LastOnGameThread()
    { Thread();return(_shuffle?.Status,_draw?.Status); }
    public bool FinishedSuccessfully()
    {
        Thread();Contract.Require(!_closed,"shuffle-task-scope-closed");
        var state=LastOnGameThread();
        Contract.Require(state.Shuffle is not (TaskStatus.Faulted or TaskStatus.Canceled) && state.Draw is not (TaskStatus.Faulted or TaskStatus.Canceled),"shuffle-original-task-fault-or-cancel");
        return state.Shuffle==TaskStatus.RanToCompletion && state.Draw==TaskStatus.RanToCompletion;
    }
    public void Close(){Thread();_closed=true;_draw=null;_shuffle=null;}
}
