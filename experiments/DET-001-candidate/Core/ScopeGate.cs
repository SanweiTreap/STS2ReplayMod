namespace Det001.Core;

public static class ReferenceScope
{
    public static bool Matches<T>(object list,object rng,object expectedOwner,object? expectedList=null)=>
        list is List<T> && ReferenceEquals(rng,expectedOwner) && (expectedList is null || ReferenceEquals(list,expectedList));
}
public sealed class ScopeSlot<T> where T:class
{
    public T? Current {get;private set;}
    public void Bind(T scope){Contract.Require(Current is null,"shuffle-scope-nested-or-leaked");Current=scope;}
    public void Clear()=>Current=null;
}
