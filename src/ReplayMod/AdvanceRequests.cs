namespace ReplayMod;

// Counts nodes not yet submitted. The node currently moving was already consumed once.
internal sealed class AdvanceRequests
{
    internal long Count {get;private set;}
    internal void Add()=>Count=checked(Count+1);
    internal bool Take(){if(Count==0)return false;Count--;return true;}
    internal void Clear()=>Count=0;
}
