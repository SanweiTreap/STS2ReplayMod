using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;

// Fixed IL reviewed first: this path uses only a new detached List<string>, a new
// SerializableRng-backed Rng, List.Sort/ToList, private RNG kernel. No engine or run.
if(args.Length!=1 || args[0] is not ("baseline" or "r1-patch" or "r1-warm-patch"))throw new ArgumentException("mode");
var mode=args[0];var harmony=new Harmony("DET001.offline.shared-generic.repro");
var rng=new Rng(new SerializableRng{counter=46,state0=1,state1=2,state2=3,state3=4});
if(mode=="r1-warm-patch")Call(new Rng(new SerializableRng{state0=1,state1=2,state2=3,state3=4}));
try
{
 if(mode!="baseline")harmony.CreateClassProcessor(typeof(Det001.Probe).Assembly.GetType("Det001.PermutationObservationPatch",true)!).Patch();
 try{var result=Call(rng);Console.WriteLine(JsonSerializer.Serialize(new{Mode=mode,Result="SUCCESS",Permutation=result,Counter=rng.ToSerializable().counter,EngineStarted=false,DetachedRngOnly=true}));}
 catch(Exception ex){Console.WriteLine(JsonSerializer.Serialize(new{Mode=mode,Result="EXCEPTION",Type=ex.GetType().FullName,Frames=new System.Diagnostics.StackTrace(ex).GetFrames()?.Take(8).Select(f=>f.GetMethod()?.DeclaringType?.FullName+"::"+f.GetMethod()?.Name),Counter=rng.ToSerializable().counter,EngineStarted=false,DetachedRngOnly=true}));}
}
finally{harmony.UnpatchAll(harmony.Id);if(harmony.GetPatchedMethods().Any())throw new Exception("unpatch");}

[MethodImpl(MethodImplOptions.NoInlining)]static List<string> Call(Rng rng)=>new List<string>{"c","a","b"}.StableShuffle(rng);
