using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Det001.Core;

namespace Det001;

// Preserve every original instruction, including the actual closed generic call.
// Only the non-generic caller is patched; shared generic methods are untouched.
public static class CallSiteInstrumentation
{
    public static IEnumerable<CodeInstruction> Around(IReadOnlyList<CodeInstruction> input,ILGenerator generator,MethodInfo target,MethodInfo before,MethodInfo after)
    {
        var args=target.GetParameters();var pre=before.GetParameters();var post=after.GetParameters();
        Contract.Require(args.Length==2 && pre.Length==2 && post.Length==2 && !target.ContainsGenericParameters && !args[0].ParameterType.IsValueType && !args[1].ParameterType.IsValueType && target.ReturnType==args[0].ParameterType && before.IsStatic && after.IsStatic && pre[0].ParameterType==args[0].ParameterType && pre[1].ParameterType==args[1].ParameterType && !before.ReturnType.IsValueType && before.ReturnType!=typeof(void) && post[0].ParameterType==target.ReturnType && post[1].ParameterType==before.ReturnType && after.ReturnType==typeof(void),"callsite-observer-signature");
        var sites=input.Where(i=>i.opcode==OpCodes.Call && i.operand is MethodInfo m && m==target).ToArray();
        Contract.Require(sites.Length==1 && sites[0].labels.Count==0 && sites[0].blocks.Count==0,"callsite-fixed-shape");
        var owner=generator.DeclareLocal(args[1].ParameterType);var scope=generator.DeclareLocal(before.ReturnType);var output=new List<CodeInstruction>();
        foreach(var original in input)
        {
            if(!ReferenceEquals(original,sites[0])){output.Add(original);continue;}
            output.Add(new(OpCodes.Stloc,owner)); // stack: list
            output.Add(new(OpCodes.Dup));       // keep exact original list for original call
            output.Add(new(OpCodes.Ldloc,owner));
            output.Add(new(OpCodes.Call,before));
            output.Add(new(OpCodes.Stloc,scope));
            output.Add(new(OpCodes.Ldloc,owner));
            output.Add(original);              // exact original call/operand, once
            output.Add(new(OpCodes.Dup));       // keep exact original result
            output.Add(new(OpCodes.Ldloc,scope));
            output.Add(new(OpCodes.Call,after));
        }
        Contract.Require(output.Count==input.Count+9 && input.All(i=>output.Count(o=>ReferenceEquals(o,i))==1),"callsite-original-preservation");
        return output;
    }
}
