namespace ReplayMod;

// The active branch is memento data. Historical scene anchors are kept separately.
// Rewinding removes future admissions and restores the native allocator value; a new
// native object can then receive the same logical ID without reviving the old object.
internal sealed class CombatIdentityRegistry<T> where T:class
{
    private readonly Dictionary<T,uint> _ids=new(ReferenceEqualityComparer.Instance);
    internal uint NextIdentity {get;private set;}
    internal IEnumerable<T> Known=>_ids.Keys;
    internal CombatIdentityRegistry(uint next,IEnumerable<T> initial,Func<T,uint> identity)
    {
        NextIdentity=next;foreach(var item in initial)_ids.Add(item,identity(item));
        Codec.Require(_ids.Values.Distinct().Count()==_ids.Count&&_ids.Values.All(id=>id<next),"initial-unit-identities-invalid");
    }
    internal void Admit(T unit,uint identity)
    {
        Codec.Require(!_ids.ContainsKey(unit)&&identity==NextIdentity&&NextIdentity<uint.MaxValue,"native-unit-allocation-not-next");
        _ids.Add(unit,identity);NextIdentity++;
    }
    internal bool Contains(T unit,uint? id)=>id.HasValue&&_ids.TryGetValue(unit,out var known)&&known==id.Value;
    internal bool Matches(uint nativeNext,IEnumerable<T> current,Func<T,uint?> identity,Func<T,bool> alive)
    {
        var units=current.ToArray();var present=new HashSet<T>(units,ReferenceEqualityComparer.Instance);
        return units.Length<=128&&nativeNext==NextIdentity&&units.All(c=>Contains(c,identity(c)))&&
            units.Select(identity).Distinct().Count()==units.Length&&!Known.Any(c=>!present.Contains(c)&&alive(c));
    }
    internal T Resolve(IEnumerable<T> current,uint identity,Func<T,string> model,string expectedModel)
    {
        var matches=current.Where(c=>Contains(c,identity)).ToArray();
        Codec.Require(matches.Length==1&&model(matches[0])==expectedModel,"unit-target-content-or-stable-identity");return matches[0];
    }
}
