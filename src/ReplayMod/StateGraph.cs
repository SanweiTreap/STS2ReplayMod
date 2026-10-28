using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReplayMod;

// Detached wire data only. IDs refer to a deterministic initial-context binding table, never addresses.
internal sealed record StateValue(string Kind,string Type="",string Text="",int Ref=-1,StateMember[]? Fields=null);
internal sealed record StateMember(string Name,StateValue Value);
internal sealed record StateNode(string Type,string Kind,StateMember[] Fields,StateValue[] Items,string Special="");
internal sealed record StateGraphData(string BindingShape,int Bindings,StateNode[] Nodes);

internal sealed class StateGraph
{
    private readonly Func<object,bool> _data;
    private readonly Func<FieldInfo,bool> _field;
    private readonly Func<Type,bool> _newData;
    private readonly Func<object,string?> _special;
    private readonly Func<object,string,Action> _prepareSpecial;
    private readonly List<object> _bindings=[];
    private readonly Dictionary<string,Type> _types=new(StringComparer.Ordinal);
    internal readonly string Shape;
    internal int BindingCount=>_bindings.Count;
    private static string Key(Type t)=>t.FullName??throw new InvalidDataException("state-type-without-name");
    private FieldInfo[] Fields(Type t)=>IdentityMemento.Fields(t).Where(_field).OrderBy(f=>f.DeclaringType!.FullName,StringComparer.Ordinal).ThenBy(f=>f.Name,StringComparer.Ordinal).ToArray();
    private static string FieldKey(FieldInfo f)=>Key(f.DeclaringType!)+"/"+f.Name;
    private void Register(Type t)
    {
        if(!_types.TryAdd(Key(t),t))return;
        if(t.IsGenericType)foreach(var arg in t.GetGenericArguments())Register(arg);
        if(t.IsArray)Register(t.GetElementType()!);
        if((t.IsValueType&&!Atomic(t))||_newData(t))foreach(var f in Fields(t))Register(f.FieldType);
    }
    private Type ResolveType(string key)
    {
        if(_types.TryGetValue(key,out var type))return type;
        // A native IReadOnlyList<T> field can be Array.Empty<T>() in a new run and List<T> after load.
        // Only form a managed array of an element type already registered from trusted local metadata.
        if(key.EndsWith("[]",StringComparison.Ordinal)&&_types.TryGetValue(key[..^2],out var element))
        {type=element.MakeArrayType();Register(type);return type;}
        throw new InvalidDataException("state-unknown-type:"+key);
    }
    internal StateGraph(IEnumerable<object> roots,Func<object,bool> data,Func<FieldInfo,bool> field,Func<Type,bool> newData,
        Func<object,string?>? special=null,Func<object,string,Action>? prepareSpecial=null,IEnumerable<Type>? extraTypes=null)
    {
        _data=data;_field=field;_newData=newData;_special=special??(_=>null);_prepareSpecial=prepareSpecial??((_,_)=>throw new InvalidDataException("state-special-not-supported"));
        if(extraTypes is not null)foreach(var t in extraTypes)Register(t);
        var initial=Capture(roots.ToList());Shape=initial.BindingShape;
    }
    private static bool Atomic(Type t)=>t.IsPrimitive||t.IsEnum||t==typeof(decimal)||t==typeof(string);
    private string Kind(object obj)
    {
        if(_special(obj) is not null)return "special";
        if(obj is Array)return "array";
        if(obj is IDictionary)return "dictionary";
        if(obj is IList l)return l.IsReadOnly?"view":"list";
        var t=obj.GetType();if(t.IsGenericType&&t.GetGenericTypeDefinition()==typeof(HashSet<>))return "set";
        if(_data(obj))return "object";
        return obj is IEnumerable?"view":"binding";
    }
    internal StateGraphData Freeze()=>Capture(null);
    private StateGraphData Capture(List<object>? roots)
    {
        var objects=roots??new List<object>(_bindings);var ids=new Dictionary<object,int>(ReferenceEqualityComparer.Instance);
        // Initial roots may alias; process each exactly once.
        if(roots is not null){objects=[];foreach(var root in roots)if(ids.TryAdd(root,objects.Count))objects.Add(root);}
        else for(var i=0;i<objects.Count;i++)ids.Add(objects[i],i);
        var nodes=new List<StateNode>();
        StateValue Value(object? value)
        {
            if(value is null)return new("null");
            var t=value.GetType();Register(t);
            Codec.Require(value is not Delegate and not Type,"state-executable-reference");
            if(Atomic(t))return new("scalar",Key(t),value is string s?s:Convert.ToString(value,CultureInfo.InvariantCulture)!);
            if(t.IsValueType)return new("struct",Key(t),Fields:Fields(t).Select(f=>new StateMember(FieldKey(f),Value(f.GetValue(value)))).ToArray());
            if(!ids.TryGetValue(value,out var id)){id=objects.Count;ids.Add(value,id);objects.Add(value);}
            Codec.Require(objects.Count<=12000,"state-graph-node-limit");return new("ref",Ref:id);
        }
        for(var i=0;i<objects.Count;i++)
        {
            var obj=objects[i];var t=obj.GetType();Register(t);var kind=Kind(obj);
            var fields=kind=="object"?Fields(t).Select(f=>{Register(f.FieldType);return new StateMember(FieldKey(f),Value(f.GetValue(obj)));}).ToArray():[];
            StateValue[] items=kind switch
            {
                "dictionary"=>((IDictionary)obj).Keys.Cast<object>().SelectMany(key=>new[]{Value(key),Value(((IDictionary)obj)[key])}).ToArray(),
                "array" or "list" or "set" or "view"=>((IEnumerable)obj).Cast<object?>().Select(Value).ToArray(),_=>[]
            };
            Codec.Require(items.Length<=32768,"state-container-limit");
            if(roots is null&&i>=_bindings.Count)Codec.Require(NewAllowed(t,kind),"state-new-type-not-supported:"+Key(t));
            nodes.Add(new(Key(t),kind,fields,items,kind=="special"?_special(obj)!:""));
        }
        if(roots is not null)_bindings.AddRange(objects);
        // Only topology/type/field identities participate; runtime IDs and scalar values do not bind objects.
        StateValue Topology(StateValue v)=>v with{Text="",Fields=v.Fields?.Select(f=>f with{Value=Topology(f.Value)}).ToArray()};
        var shape=roots is null?Shape:Codec.Hash(Codec.Bytes(nodes.Select(n=>n with{Special="",Fields=n.Fields.Select(f=>f with{Value=Topology(f.Value)}).ToArray(),Items=n.Items.Select(Topology).ToArray()}).ToArray()));
        return new(shape,_bindings.Count,nodes.ToArray());
    }
    private bool NewAllowed(Type t,string kind)=>kind switch
    {
        "object"=>_newData(t),
        "array"=>t.GetArrayRank()==1,
        "list"=>t.IsGenericType&&t.GetGenericTypeDefinition()==typeof(List<>),
        "dictionary"=>t.IsGenericType&&t.GetGenericTypeDefinition()==typeof(Dictionary<,>),
        "set"=>t.IsGenericType&&t.GetGenericTypeDefinition()==typeof(HashSet<>),_=>false
    };
    // A loaded native run need not have the same allocation/alias topology as a newly created run.
    // Explicit logical anchors are supplied by the game adapter; children bind by named fields and dictionary keys.
    internal Dictionary<int,object> BindLogical(StateGraphData saved,IEnumerable<KeyValuePair<int,object>> anchors)
    {
        Codec.Require(saved.Bindings>0&&saved.Bindings<=saved.Nodes.Length&&saved.Nodes.Length<=12000,"state-binding-count");
        Codec.Require(saved.Nodes.All(n=>n.Fields.Length<=1024&&n.Items.Length<=32768),"state-node-size-limit");
        var map=new Dictionary<int,object>();var reverse=new Dictionary<object,int>(ReferenceEqualityComparer.Instance);var queue=new Queue<int>();
        bool Bind(int id,object? obj)
        {
            Codec.Require(id>=0&&id<saved.Nodes.Length,"state-invalid-reference");
            if(id>=saved.Bindings||obj is null||map.ContainsKey(id))return false;
            var node=saved.Nodes[id];if(Key(obj.GetType())!=node.Type||Kind(obj)!=node.Kind)return false;
            // Equal value IDs may be interned in one process and separately deserialized in another.
            // Mutable objects must keep distinct logical identities.
            if(node.Kind!="binding"&&reverse.TryGetValue(obj,out var prior)&&prior!=id)return false;
            map.Add(id,obj);reverse.TryAdd(obj,id);queue.Enqueue(id);Register(obj.GetType());return true;
        }
        void Match(StateValue savedValue,object? actual)
        {
            if(savedValue.Kind=="ref")Bind(savedValue.Ref,actual);
            else if(savedValue.Kind=="struct"&&actual is not null)
            {
                Codec.Require(Key(actual.GetType())==savedValue.Type,"state-struct-binding-type");
                var fields=Fields(actual.GetType());var members=savedValue.Fields??[];
                Codec.Require(fields.Select(FieldKey).SequenceEqual(members.Select(m=>m.Name)),"state-struct-fields");
                for(var i=0;i<fields.Length;i++)Match(members[i].Value,fields[i].GetValue(actual));
            }
        }
        object? KeyValue(StateValue value)
        {
            if(value.Kind=="ref")return map.GetValueOrDefault(value.Ref);
            if(value.Kind!="scalar"||!_types.TryGetValue(value.Type,out var t))return null;
            return t==typeof(string)?value.Text:t.IsEnum?Enum.Parse(t,value.Text):Convert.ChangeType(value.Text,t,CultureInfo.InvariantCulture);
        }
        foreach(var anchor in anchors)Codec.Require(Bind(anchor.Key,anchor.Value)||map.TryGetValue(anchor.Key,out var old)&&ReferenceEquals(old,anchor.Value),"state-logical-anchor-mismatch:"+anchor.Key);
        void Drain()
        {
            while(queue.TryDequeue(out var id))
            {
                var obj=map[id];var node=saved.Nodes[id];
                if(node.Kind=="object")
                {
                    var fields=Fields(obj.GetType());Codec.Require(fields.Select(FieldKey).SequenceEqual(node.Fields.Select(f=>f.Name)),"state-object-fields");
                    for(var j=0;j<fields.Length;j++)Match(node.Fields[j].Value,fields[j].GetValue(obj));
                }
                else if(node.Kind=="dictionary")
                {
                    Codec.Require(node.Items.Length%2==0,"state-dictionary-pairs");var d=(IDictionary)obj;
                    for(var j=0;j<node.Items.Length;j+=2){var key=KeyValue(node.Items[j]);if(key is not null&&d.Contains(key))Match(node.Items[j+1],d[key]);}
                }
                else if(node.Kind is "array" or "list" or "view" or "set")
                {
                    var items=((IEnumerable)obj).Cast<object?>().ToArray();
                    for(var j=0;j<Math.Min(items.Length,node.Items.Length);j++)Match(node.Items[j],items[j]);
                }
            }
        }
        Drain();
        // Retry dictionary key references after their model/player/card anchors have been discovered.
        var previous=-1;
        while(previous!=map.Count){previous=map.Count;foreach(var id in map.Keys.ToArray())if(saved.Nodes[id].Kind=="dictionary")queue.Enqueue(id);Drain();}
        var unresolved=new List<string>();
        for(var i=0;i<saved.Bindings;i++)if(!map.ContainsKey(i))
        {
            var node=saved.Nodes[i];Type t;
            try{t=ResolveType(node.Type);}catch(InvalidDataException){unresolved.Add(i+":"+node.Type);continue;}
            if(!NewAllowed(t,node.Kind)){unresolved.Add(i+":"+node.Type);continue;}
            // Detached pure containers/history may differ in native initialization. No new model/service/closure is made.
            var obj=Create(t,node);map.Add(i,obj);
        }
        Codec.Require(unresolved.Count==0,"state-logical-binding-unresolved:"+unresolved.Count+":"+string.Join("|",unresolved.Take(8)));
        return map;
    }
    private static object Create(Type t,StateNode node)=>node.Kind switch
    {
        "object"=>RuntimeHelpers.GetUninitializedObject(t),
        "array"=>Array.CreateInstance(t.GetElementType()!,node.Items.Length),
        _=>Activator.CreateInstance(t)??throw new InvalidDataException("state-container-construction")
    };
    internal Action Apply(StateGraphData saved,IReadOnlyDictionary<int,object>? logicalBindings=null)
    {
        Codec.Require(logicalBindings is not null||saved.BindingShape==Shape&&saved.Bindings==_bindings.Count,"state-initial-binding-shape-mismatch");
        Codec.Require(saved.Nodes.Length>=saved.Bindings&&saved.Nodes.Length<=12000,"state-graph-node-limit");
        var objects=new object[saved.Nodes.Length];var writes=new List<Action>();var checks=new List<Action>();
        Type TypeFor(string key)=>ResolveType(key);
        for(var i=0;i<objects.Length;i++)
        {
            var n=saved.Nodes[i];var t=TypeFor(n.Type);
            Codec.Require(n.Fields.Length<=1024&&n.Items.Length<=32768,"state-node-size-limit");
            if(i<saved.Bindings)
            {
                objects[i]=logicalBindings is null?_bindings[i]:logicalBindings.TryGetValue(i,out var bound)?bound:throw new InvalidDataException("state-logical-binding-missing");
                Codec.Require(objects[i].GetType()==t&&Kind(objects[i])==n.Kind,"state-binding-type-mismatch");
            }
            else
            {
                Codec.Require(NewAllowed(t,n.Kind),"state-new-type-not-supported:"+n.Type);
                objects[i]=Create(t,n);
            }
        }
        object? Value(StateValue v,Type expected,int depth=0)
        {
            Codec.Require(depth<32,"state-value-depth");object? result;
            if(v.Kind=="null")result=null;
            else if(v.Kind=="ref"){Codec.Require(v.Ref>=0&&v.Ref<objects.Length,"state-invalid-reference");result=objects[v.Ref];}
            else
            {
                var t=TypeFor(v.Type);
                if(v.Kind=="scalar")
                {
                    Codec.Require(Atomic(t)&&v.Text.Length<=65536,"state-invalid-scalar");
                    result=t==typeof(string)?v.Text:t.IsEnum?Enum.Parse(t,v.Text):t==typeof(char)?char.Parse(v.Text):Convert.ChangeType(v.Text,t,CultureInfo.InvariantCulture);
                }
                else
                {
                    Codec.Require(v.Kind=="struct"&&t.IsValueType&&!Atomic(t),"state-invalid-struct");
                    result=Activator.CreateInstance(t)!;var fields=Fields(t);var members=v.Fields??[];
                    Codec.Require(fields.Select(FieldKey).SequenceEqual(members.Select(f=>f.Name)),"state-struct-fields");
                    for(var j=0;j<fields.Length;j++)fields[j].SetValue(result,Value(members[j].Value,fields[j].FieldType,depth+1));
                }
            }
            Codec.Require(result is null?(!expected.IsValueType||Nullable.GetUnderlyingType(expected) is not null):
                (Nullable.GetUnderlyingType(expected)??expected).IsInstanceOfType(result),"state-field-type-mismatch");return result;
        }
        for(var i=0;i<objects.Length;i++)
        {
            var obj=objects[i];var n=saved.Nodes[i];var t=obj.GetType();
            Codec.Require((n.Kind=="object"||n.Fields.Length==0)&&(n.Kind is not ("object" or "binding" or "special")||n.Items.Length==0)&&
                (n.Kind=="special"||n.Special.Length==0),"state-node-shape");
            if(n.Kind=="object")
            {
                var fields=Fields(t);Codec.Require(fields.Select(FieldKey).SequenceEqual(n.Fields.Select(f=>f.Name)),"state-object-fields");
                for(var j=0;j<fields.Length;j++)
                {
                    var f=fields[j];var value=Value(n.Fields[j].Value,f.FieldType);
                    // Instance readonly data fields are supported by reflection, including newly allocated history entries.
                    writes.Add(()=>{if(!Equals(f.GetValue(obj),value))f.SetValue(obj,value);});
                    checks.Add(()=>Codec.Require(Equals(f.GetValue(obj),value),"state-readback-field:"+FieldKey(f)));
                }
            }
            else if(n.Kind=="dictionary")
            {
                Codec.Require(n.Items.Length%2==0,"state-dictionary-pairs");var args=t.GetGenericArguments();Codec.Require(args.Length==2,"state-dictionary-type");
                var pairs=Enumerable.Range(0,n.Items.Length/2).Select(k=>(Key:Value(n.Items[k*2],args[0]),Value:Value(n.Items[k*2+1],args[1]))).ToArray();
                Codec.Require(pairs.All(p=>p.Key is not null)&&pairs.Select(p=>p.Key).Distinct().Count()==pairs.Length,"state-dictionary-keys");
                writes.Add(()=>{var d=(IDictionary)obj;d.Clear();foreach(var p in pairs)d.Add(p.Key!,p.Value);});
                checks.Add(()=>{var d=(IDictionary)obj;Codec.Require(d.Count==pairs.Length&&pairs.All(p=>d.Contains(p.Key!)&&Equals(d[p.Key!],p.Value)),"state-readback-dictionary");});
            }
            else if(n.Kind is "array" or "list" or "set")
            {
                var element=n.Kind=="array"?t.GetElementType()!:t.GetGenericArguments()[0];var items=n.Items.Select(v=>Value(v,element)).ToArray();
                if(obj is Array a){Codec.Require(a.Rank==1&&a.Length==items.Length,"state-array-size");writes.Add(()=>{for(var j=0;j<items.Length;j++)a.SetValue(items[j],j);});}
                else if(obj is IList l)writes.Add(()=>{l.Clear();foreach(var item in items)l.Add(item);});
                else writes.Add(()=>{t.GetMethod("Clear")!.Invoke(obj,null);foreach(var item in items)t.GetMethod("Add")!.Invoke(obj,[item]);});
                checks.Add(()=>Codec.Require(((IEnumerable)obj).Cast<object?>().SequenceEqual(items),"state-readback-container"));
            }
            else if(n.Kind=="special")
            {
                writes.Add(_prepareSpecial(obj,n.Special));checks.Add(()=>Codec.Require(_special(obj)==n.Special,"state-readback-special"));
            }
            else if(n.Kind=="view")
            {
                var items=n.Items.Select(v=>Value(v,typeof(object))).ToArray();
                Codec.Require(((IEnumerable)obj).Cast<object?>().SequenceEqual(items),"state-readonly-view-changed");
            }
            else Codec.Require(n.Kind=="binding","state-invalid-node-kind");
        }
        // Validate the entire graph before touching any live data. A later application error exits playback.
        foreach(var write in writes)write();
        return ()=>{foreach(var check in checks)check();};
    }
}
