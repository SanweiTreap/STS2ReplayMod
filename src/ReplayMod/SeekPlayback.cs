namespace ReplayMod;

// Playback permission, continuous mode and finite navigation requests are independent.
// A node has one dispatcher; Auto can satisfy it without consuming a second Step.
internal sealed class SeekPlayback
{
    private readonly AdvanceRequests _steps=new();
    internal bool Running {get;private set;}
    internal bool Auto {get;private set;}
    internal long Pending=>_steps.Count;
    internal bool WantsAdvance=>Running&&(Auto||Pending>0);
    internal void Step()=>_steps.Add();
    internal void ToggleRunning()=>Running=!Running;
    internal void ToggleAuto()=>Auto=!Auto;
    internal bool Take()
    {
        if(!WantsAdvance)return false;
        _steps.Take();return true;
    }
    internal void Seek(){Running=false;_steps.Clear();}
    internal void End()=>Seek();
    internal void Stop(){Seek();Auto=false;}
}
