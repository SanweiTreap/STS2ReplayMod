namespace ReplayMod;

// One command can wait for an atomic data/UI handoff. It never interrupts native business.
internal sealed class PresentationNavigation
{
    private int _direction;
    internal bool Pending=>_direction!=0;
    // Forward navigation has the same settlement rule in native and historical execution.
    internal bool InterruptsDisplay=>_direction is -1 or 2;
    internal bool Request(string command)
    {
        var direction=command switch{"step"=>1,"back"=>-1,"pause"=>2,_=>0};
        if(direction==0||Pending)return false;
        _direction=direction;return true;
    }
    internal int Take(){var direction=_direction;_direction=0;return direction;}
    internal void Clear()=>_direction=0;
}
