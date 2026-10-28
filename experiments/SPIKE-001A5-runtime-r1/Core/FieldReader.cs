namespace Spike001A5.Core;

// Collect one game-thread sample. Exceptions become only a named whitelist status;
// no exception object, message, type, stack, live object, or reference is retained.
public sealed class FieldReader
{
    private readonly List<Field> _fields=new(20);
    public Field[] Fields=>_fields.Select(x=>x with{}).ToArray();
    public T? Object<T>(string name,Func<T?> getter,bool available=true) where T:class
    {
        if(!available){Add(name,FieldStatus.Missing);return null;}
        try { var value=getter();Add(name,value is null?FieldStatus.False:FieldStatus.True);return value; }
        catch { Add(name,FieldStatus.Exception);return null; }
    }
    public bool Bool(string name,Func<bool> getter,bool available=true)
    {
        if(!available){Add(name,FieldStatus.Missing);return false;}
        try { var value=getter();Add(name,value?FieldStatus.True:FieldStatus.False);return value; }
        catch { Add(name,FieldStatus.Exception);return false; }
    }
    private void Add(string name,FieldStatus status)
    { Contract.Require(Flow.FieldNames.Contains(name,StringComparer.Ordinal),"readiness-name-not-whitelisted");_fields.Add(new(name,status)); }
}
