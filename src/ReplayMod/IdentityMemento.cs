using System.Collections;
using System.Reflection;

namespace ReplayMod;

// An in-process memento of explicitly selected data objects, not a clone or serializer.
// Object identity is retained; field values and container contents are copied at capture time.
internal sealed class IdentityMemento
{
    private readonly List<Action> _restore=[];
    private readonly HashSet<object> _seen=new(ReferenceEqualityComparer.Instance);
    internal int Objects=>_seen.Count;
    internal int Writes=>_restore.Count;
    private static readonly Dictionary<Type,FieldInfo[]> FieldCache=[];
    internal static IEnumerable<FieldInfo> Fields(Type type)
    {
        if(FieldCache.TryGetValue(type,out var cached))return cached;
        var fields=new List<FieldInfo>();
        for(var t=type;t is not null&&t!=typeof(object);t=t.BaseType)
            fields.AddRange(t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly));
        var result=fields.ToArray();FieldCache.Add(type,result);return result;
    }
    internal IdentityMemento(IEnumerable<object?> roots,Func<object,bool> data,Func<FieldInfo,bool> field,Func<object,Action?>? special=null)
    {
        void Visit(object? value)
        {
            if(value is null||value is string||value is Delegate||value is Type)return;
            var type=value.GetType();
            if(type.IsPrimitive||type.IsEnum||type==typeof(decimal))return;
            if(type.IsValueType)
            {
                // The field/container already holds a boxed copy. Preserve any referenced children too.
                foreach(var f in Fields(type))if(!f.FieldType.IsPointer)Visit(f.GetValue(value));return;
            }
            if(!_seen.Add(value))return;
            Codec.Require(_seen.Count<=12000,"direct-checkpoint-data-graph-limit");
            if(special?.Invoke(value) is {} action){_restore.Add(action);return;}
            if(value is Array array)
            {
                Codec.Require(array.Rank==1,"direct-checkpoint-array-rank");var saved=(Array)array.Clone();
                _restore.Add(()=>Array.Copy(saved,array,saved.Length));foreach(var item in saved)Visit(item);return;
            }
            if(value is IDictionary dictionary)
            {
                var saved=dictionary.Keys.Cast<object>().Select(key=>new DictionaryEntry(key,dictionary[key])).ToArray();
                _restore.Add(()=>{dictionary.Clear();foreach(var item in saved)dictionary.Add(item.Key,item.Value);});
                foreach(var item in saved){Visit(item.Key);Visit(item.Value);}return;
            }
            if(value is IList list)
            {
                if(list.IsReadOnly){foreach(var item in list)Visit(item);return;}
                var saved=list.Cast<object?>().ToArray();
                _restore.Add(()=>{list.Clear();foreach(var item in saved)list.Add(item);});foreach(var item in saved)Visit(item);return;
            }
            if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(HashSet<>))
            {
                var saved=((IEnumerable)value).Cast<object?>().ToArray();var clear=type.GetMethod("Clear")!;var add=type.GetMethod("Add")!;
                _restore.Add(()=>{clear.Invoke(value,null);foreach(var item in saved)add.Invoke(value,[item]);});foreach(var item in saved)Visit(item);return;
            }
            if(!data(value))
            {
                // Read-only compiler-generated intent lists still contain mutable intent objects.
                if(value is IEnumerable items)foreach(var item in items)Visit(item);
                return;
            }
            foreach(var f in Fields(type).Where(field))
            {
                var saved=f.GetValue(value);
                if(!f.IsInitOnly)_restore.Add(()=>f.SetValue(value,saved));
                Visit(saved);
            }
        }
        foreach(var root in roots)Visit(root);
    }
    internal void Restore(){foreach(var write in _restore)write();}
}
