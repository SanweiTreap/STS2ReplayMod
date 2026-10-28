using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Det001;
using HarmonyLib;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

static class CallSiteRegression
{
    static void Need(bool value){if(!value)throw new Exception("callsite-regression");}
    static Rng Detached()=>new(new SerializableRng{counter=46,state0=1,state1=2,state2=3,state3=4});
    public static void ActualUtility()
    {
        var harmony=new Harmony("DET001.r2.actual-utility");
        try
        {
            harmony.PatchAll(typeof(Probe).Assembly);Need(harmony.GetPatchedMethods().All(m=>m.Name!="StableShuffle"));
            var rng=Detached();var list=new List<string>{"c","a","b"};var result=Strings(list,rng);Need(ReferenceEquals(list,result) && result.SequenceEqual(new[]{"b","c","a"}) && rng.ToSerializable().counter==48);
            var numbers=new List<int>{3,1,2};rng=Detached();Need(ReferenceEquals(numbers,Numbers(numbers,rng)) && numbers.SequenceEqual(new[]{2,3,1}) && rng.ToSerializable().counter==48);
        }
        finally{harmony.UnpatchAll(harmony.Id);}Need(!harmony.GetPatchedMethods().Any());
    }
    [MethodImpl(MethodImplOptions.NoInlining)]static List<string> Strings(List<string> list,Rng owner)=>list.StableShuffle(owner);
    [MethodImpl(MethodImplOptions.NoInlining)]static List<int> Numbers(List<int> list,Rng owner)=>list.StableShuffle(owner);
    static Harmony Patched()
    {var h=new Harmony("DET001.r2.actual-callsite-dummy");h.Patch(typeof(Caller).GetMethod(nameof(Caller.Invoke))!,transpiler:new HarmonyMethod(typeof(CallSiteRegression),nameof(Transform)));return h;}
    static IEnumerable<CodeInstruction> Transform(IEnumerable<CodeInstruction> instructions,ILGenerator generator)=>CallSiteInstrumentation.Around(instructions.ToArray(),generator,Target(),typeof(Observe).GetMethod(nameof(Observe.Before))!,typeof(Observe).GetMethod(nameof(Observe.After))!);
    static MethodInfo Target()=>typeof(Original).GetMethod(nameof(Original.Stable))!.MakeGenericMethod(typeof(Value));
    public static void DummyNormal()
    {
        var h=Patched();try{Observe.Reset();var owner=new Owner();var list=new List<Value>{new(3),new(1),new(2)};var result=Caller.Invoke(list,owner);Need(ReferenceEquals(result,list) && owner.Counter==2 && result.Select(v=>v.Number).SequenceEqual(new[]{1,2,3}) && Observe.BeforeCount==1 && Observe.AfterCount==1 && ReferenceEquals(Observe.List,list) && ReferenceEquals(Observe.Owner,owner));var unrelated=new List<string>{"z","a"};Caller.Unrelated(unrelated,new());Need(Observe.BeforeCount==1 && unrelated.SequenceEqual(new[]{"a","z"}));}finally{h.UnpatchAll(h.Id);}
    }
    public static void DummyFaults()
    {
        var h=Patched();try
        {
            Observe.Reset();Observe.Throw=true;var owner=new Owner();var list=new List<Value>{new(2),new(1)};Need(ReferenceEquals(Caller.Invoke(list,owner),list) && owner.Counter==2 && Observe.BeforeCount==1 && Observe.AfterCount==1);
            Observe.Reset();var ex=new InvalidOperationException("original-identity");owner=new(){Fault=ex};try{Caller.Invoke(list,owner);throw new Exception("missing-original-fault");}catch(InvalidOperationException e){Need(ReferenceEquals(ex,e) && Observe.BeforeCount==1 && Observe.AfterCount==0 && owner.Counter==0);}
        }
        finally{h.UnpatchAll(h.Id);}
    }
    public static void Instructions()
    {
        var target=Target();var before=typeof(Observe).GetMethod(nameof(Observe.Before))!;var after=typeof(Observe).GetMethod(nameof(Observe.After))!;
        var original=new[]{new CodeInstruction(OpCodes.Ldarg_0),new(OpCodes.Ldarg_1),new(OpCodes.Call,target),new(OpCodes.Ret)};
        ILGenerator Generator()=>new DynamicMethod("instruction-test",typeof(void),Type.EmptyTypes).GetILGenerator();
        var result=CallSiteInstrumentation.Around(original,Generator(),target,before,after).ToArray();Need(result.Length==original.Length+9 && original.All(i=>result.Count(x=>ReferenceEquals(i,x))==1) && result.Where(x=>original.Contains(x)).SequenceEqual(original));
        Reject(()=>CallSiteInstrumentation.Around(original.Where(i=>i.opcode!=OpCodes.Call).ToArray(),Generator(),target,before,after));
        Reject(()=>CallSiteInstrumentation.Around(original.Concat(new[]{new CodeInstruction(OpCodes.Call,target)}).ToArray(),Generator(),target,before,after));
        var withLabel=new CodeInstruction(OpCodes.Call,target);withLabel.labels.Add(Generator().DefineLabel());Reject(()=>CallSiteInstrumentation.Around(new[]{withLabel},Generator(),target,before,after));
    }
    static void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("expected-callsite-rejection");}
    public sealed class Value(int number):IComparable<Value>{public int Number=number;public int CompareTo(Value? other)=>Number.CompareTo(other!.Number);}
    public sealed class Owner{public int Counter;public Exception? Fault;}
    public static class Original
    {[MethodImpl(MethodImplOptions.NoInlining)]public static List<T> Stable<T>(List<T> list,Owner owner){if(owner.Fault is not null)throw owner.Fault;list.Sort();owner.Counter+=2;return list;}}
    public static class Caller
    {[MethodImpl(MethodImplOptions.NoInlining)]public static List<Value> Invoke(List<Value> list,Owner owner)=>Original.Stable(list,owner);[MethodImpl(MethodImplOptions.NoInlining)]public static List<string> Unrelated(List<string> list,Owner owner)=>Original.Stable(list,owner);}
    public static class Observe
    {
        public static bool Throw;public static int BeforeCount,AfterCount;public static object? List,Owner;
        public static void Reset(){Throw=false;BeforeCount=AfterCount=0;List=Owner=null;}
        public static object Before(List<Value> list,Owner owner){try{BeforeCount++;List=list;Owner=owner;if(Throw)throw new IOException();}catch{}return owner;}
        public static void After(List<Value> list,object scope){try{AfterCount++;if(Throw)throw new IOException();}catch{}}
    }
}
