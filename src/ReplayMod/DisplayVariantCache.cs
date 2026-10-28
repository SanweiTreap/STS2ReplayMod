namespace ReplayMod;

// Two equal-looking combat cards still have different identities. Revisiting an earlier content
// fingerprint must recover its frozen display asset instead of mutating the asset for newer state.
internal sealed class DisplayVariantCache<TSource,TValue> where TSource:class
{
    private readonly Dictionary<TSource,Dictionary<string,TValue>> _sources=new(ReferenceEqualityComparer.Instance);
    internal TValue Get(TSource source,string fingerprint,Func<TValue> create)
    {
        if(!_sources.TryGetValue(source,out var variants))_sources.Add(source,variants=[]);
        if(variants.TryGetValue(fingerprint,out var value))return value;
        value=create();variants.Add(fingerprint,value);return value;
    }
    internal void Clear()=>_sources.Clear();
}
