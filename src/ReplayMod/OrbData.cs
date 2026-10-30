namespace ReplayMod;

// Replay-local identities follow actual native queue admissions, not visual nodes or model names.
internal sealed class OrbIdentityMap<T> where T:class
{
    private readonly Dictionary<T,uint> _ids=new(ReferenceEqualityComparer.Instance);
    internal uint NextId {get;private set;}
    internal IEnumerable<T> Known=>_ids.Keys;
    internal uint Admit(T instance)
    {
        if(_ids.TryGetValue(instance,out var id))return id;
        id=++NextId;_ids.Add(instance,id);return id;
    }
    internal uint Require(T instance)=>_ids.TryGetValue(instance,out var id)?id:throw new InvalidDataException("orb-instance-not-registered");
}

public sealed record OrbEntry(uint Identity,string Model,int Owner,bool Removed,decimal Passive,decimal Evoke,SortedDictionary<string,string?> Mutable);
public sealed record OrbQueueState(int Capacity,uint NextIdentity,OrbEntry[] Orbs);
