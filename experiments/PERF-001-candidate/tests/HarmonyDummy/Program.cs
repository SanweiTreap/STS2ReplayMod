using System.Reflection;
using System.Text.Json;
using DummyFixture;
using HarmonyLib;
using Perf001Candidate;
using Perf001Candidate.Core;

if(args.Length!=1)throw new ArgumentException("new dummy output root");
var output=Path.GetFullPath(args[0]);var permitted=Path.GetFullPath(Path.Combine(Environment.CurrentDirectory,"evidence/perf-001/candidate-r1/validation"));
Identity.Require(Path.GetDirectoryName(output)==permitted&&!Directory.Exists(output),"new-package-output-only");Directory.CreateDirectory(output);
var assembly=typeof(Program).Assembly;
Identity.Require(!assembly.GetReferencedAssemblies().Any(x=>x.Name is "sts2" or "GodotSharp"),"dummy-must-not-reference-production");
var common=PatchCatalog.Plan(Mode.C0);var c1=PatchCatalog.Plan(Mode.C1);var c2=PatchCatalog.Plan(Mode.C2);
Identity.Require(common.Length==13&&c1.Length==16&&Identity.Bytes(c1).AsSpan().SequenceEqual(Identity.Bytes(c2)),"mode-patch-plan-difference");
File.WriteAllBytes(Path.Combine(output,"patch-plans.json"),Identity.Bytes(new{C0=common,C1=c1,C2=c2}));
var passed=new List<string>();
var foreign=new Harmony("PERF001-independent-dummy-owner");var foreignTarget=PatchCatalog.Resolve(assembly,common.First(x=>x.Key=="menu"));
foreign.Patch(foreignTarget,prefix:new HarmonyMethod(typeof(DummyCallbacks).GetMethod(nameof(DummyCallbacks.Foreign))!));
try
{
    foreach(var mode in Enum.GetValues<Mode>())
    {
        var own=new Harmony(Identity.Owner);DummyCallbacks.Mode=mode;DummyCallbacks.ThrowObservers=false;DummyCallbacks.CaptureCalls=0;
        try
        {
            PatchCatalog.Install(own,assembly,mode,typeof(DummyCallbacks));
            foreach(var spec in common)
            {
                var method=PatchCatalog.Resolve(assembly,spec);var instance=Activator.CreateInstance(method.DeclaringType!)!;
                object?[] Arguments()=>method.GetParameters().Select(p=>p.ParameterType==typeof(bool)?(object)false:
                    p.ParameterType==typeof(Func<Task>)?(Func<Task>)(()=>Task.CompletedTask):Nullable.GetUnderlyingType(p.ParameterType) is not null?null:Activator.CreateInstance(p.ParameterType,true)).ToArray();
                foreach(var task in new[]{Task.CompletedTask,Task.FromException(new ApplicationException("original-async")),Task.FromCanceled(new CancellationToken(true))})
                {
                    World.Next=task;World.SyncFault=null;World.Ready=false;var before=World.OriginalCalls;var values=World.Values;
                    DummyCallbacks.Order.Clear();DummyCallbacks.LastObserved=null;var returned=method.Invoke(instance,Arguments());
                    Identity.Require(World.OriginalCalls==before+1&&World.Values==values,"original-skipped-or-values-mutated");
                    if(method.ReturnType==typeof(Task))Identity.Require(ReferenceEquals(returned,task),"original-Task-replaced");
                    if(spec.Key is "map" or "map-move-action" or "card-action" or "end-action" or "card" or "turn")Identity.Require(ReferenceEquals(DummyCallbacks.LastObserved,task),"postfix-did-not-see-original-Task");
                    if(mode==Mode.C2&&spec.Key is "map" or "card" or "end-ready")
                        Identity.Require(DummyCallbacks.Order.First()=="common"&&DummyCallbacks.Order.IndexOf("candidate")>0&&
                            (spec.Key=="card"?DummyCallbacks.Order.Last()=="common":DummyCallbacks.Order.Last()=="candidate"),"common-candidate-priority-order");
                    if(task.IsFaulted)try{task.GetAwaiter().GetResult();}catch(ApplicationException ex){Identity.Require(ex.Message=="original-async","fault-replaced");}
                    if(task.IsCanceled)try{task.GetAwaiter().GetResult();}catch(TaskCanceledException){}
                }
                DummyCallbacks.ThrowObservers=true;World.Next=Task.CompletedTask;World.SyncFault=null;var n=World.OriginalCalls;
                var same=method.Invoke(instance,Arguments());Identity.Require(World.OriginalCalls==n+1,"observer-throw-skipped-original");
                if(method.ReturnType==typeof(Task))Identity.Require(ReferenceEquals(same,World.Next),"observer-throw-replaced-Task");
                var fault=new InvalidOperationException("original-sync");World.SyncFault=fault;
                try{method.Invoke(instance,Arguments());throw new InvalidDataException("sync fault suppressed");}
                catch(TargetInvocationException ex){Identity.Require(ReferenceEquals(ex.InnerException,fault),"sync original exception changed");}
                World.SyncFault=null;DummyCallbacks.ThrowObservers=false;
            }
            Identity.Require(mode==Mode.C2?DummyCallbacks.CaptureCalls>0:DummyCallbacks.CaptureCalls==0,"empty-observer-captured");
            var map=PatchCatalog.Resolve(assembly,common.Single(x=>x.Key=="map"));var captures=DummyCallbacks.CaptureCalls;
            World.Bootstrap=true;DummyCallbacks.LastObserved=null;
            map.Invoke(Activator.CreateInstance(map.DeclaringType!),new object[]{new MegaCrit.Sts2.Core.Map.MapCoord()});
            Identity.Require(DummyCallbacks.CaptureCalls==captures&&DummyCallbacks.LastObserved is null,"bootstrap-map-postfix-not-skipped");World.Bootstrap=false;
            passed.Add(mode+": exact catalog, common-before-candidate, originals once, values/Task success/fault/cancel/sync fault retained including observer failure");
        }
        finally{own.UnpatchAll(Identity.Owner);}
        Identity.Require(!Harmony.GetAllPatchedMethods().Any(m=>Harmony.GetPatchInfo(m)?.Owners.Contains(Identity.Owner)==true),"own-patches-remain");
        Identity.Require(Harmony.GetPatchInfo(foreignTarget)?.Owners.Contains(foreign.Id)==true,"foreign-owner-removed");
    }
    var saved=PatchCatalog.Specs[1];
    try
    {
        PatchCatalog.Specs[1]=saved with{Type="Missing.Typed.Target"};var partial=new Harmony(Identity.Owner);
        try{PatchCatalog.Install(partial,assembly,Mode.C0,typeof(DummyCallbacks));throw new InvalidDataException("missing target accepted");}
        catch(TypeLoadException){}finally{partial.UnpatchAll(Identity.Owner);}
    }
    finally{PatchCatalog.Specs[1]=saved;}
    Identity.Require(!Harmony.GetAllPatchedMethods().Any(m=>Harmony.GetPatchInfo(m)?.Owners.Contains(Identity.Owner)==true),"partial-install-cleanup");
    passed.Add("partial install failure unwinds only owner; foreign owner preserved");
}
finally{foreign.UnpatchAll(foreign.Id);}
File.WriteAllBytes(Path.Combine(output,"result.json"),Identity.Bytes(new{Result="PASS_REAL_HARMONY_HOMOMORPHIC_DUMMY",Tests=passed,
    HarmonyVersion=typeof(Harmony).Assembly.GetName().Version!.ToString(),CommonTargets=13,CandidateAdditionalHooks=3,
    ProductionAssembliesReferenced=false,ProductionMethodsExecuted=false,RealGodotLifecycle="Unknown",GameRuntimeAttempts=0,TimingBenchmarkRuns=0}));
Console.WriteLine("PASS actual Harmony typed dummy: C0/C1/C2, original Task/exception identity, exact-owner/partial cleanup; no production API");

public static class DummyCallbacks
{
    public static Mode Mode;public static int CaptureCalls;public static bool ThrowObservers;public static Task? LastObserved;
    public static List<string> Order=[];
    private static void Observe(Action? action=null)=>ObserverGuard.Run(()=>{Order.Add("common");if(ThrowObservers)throw new ApplicationException("observer");action?.Invoke();},_=>{});
    public static void Foreign(){}
    public static void Menu()=>Observe();public static void Fresh()=>Observe();public static void Saved()=>Observe();
    public static void MapBefore(out bool __state){__state=!World.Bootstrap;Observe();}public static void MapAfter(bool __state,Task __result){if(__state)Observe(()=>LastObserved=__result);}
    public static void Queued()=>Observe();public static void Execute()=>Observe();
    public static void ActionTask(Task __result)=>Observe(()=>LastObserved=__result);
    public static void MapMoveTask(Task __result)=>Observe(()=>LastObserved=__result);
    public static void CardBefore()=>Observe();public static void CardAfter(Task __result)=>Observe(()=>LastObserved=__result);
    public static void EndBefore(out bool __state){__state=World.Ready;Observe();}public static void EndAfter(bool __state)=>Observe();
    public static void TurnBefore(out int __state){__state=1;Observe();}public static void TurnAfter(int __state,Task __result)=>Observe(()=>LastObserved=__result);
    public static void Cancelled()=>Observe();public static void OriginalFault(Exception? __exception){if(__exception is not null)Observe();}
    private static void Capture(){if(Mode!=Mode.C2)return;ObserverGuard.Run(()=>{Order.Add("candidate");CaptureCalls++;if(ThrowObservers)throw new ApplicationException("candidate observer");},_=>{});}
    public static void CandidateMap(bool __state){if(__state)Capture();}public static void CandidateCard()=>Capture();public static void CandidateEnd(bool __state)=>Capture();
}
