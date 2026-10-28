using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using Player=MegaCrit.Sts2.Core.Entities.Players.Player;

namespace Det001;

[HarmonyPatch(typeof(CardPileCmd),nameof(CardPileCmd.Draw),new[]{typeof(PlayerChoiceContext),typeof(decimal),typeof(Player),typeof(bool)})]
internal static class DrawObservationPatch
{
    [HarmonyPrefix] private static void Prefix(Player player,decimal count,bool fromHandDraw,out ShuffleBridge.Scope? __state)
    { ShuffleBridge.Scope? scope=null;Probe.Safe(()=>scope=ShuffleBridge.DrawPrefix(player,count,fromHandDraw));__state=scope; }
    [HarmonyPostfix] private static void Postfix(ShuffleBridge.Scope? __state,Task __result)=>Probe.Safe(()=>ShuffleBridge.DrawPostfix(__state,__result));
    [HarmonyFinalizer] private static Exception? Finalizer(ShuffleBridge.Scope? __state,Exception? __exception)
    { Probe.Safe(()=>ShuffleBridge.SynchronousException(__state,__exception));return __exception; }
}
[HarmonyPatch(typeof(CardPileCmd),nameof(CardPileCmd.Shuffle),new[]{typeof(PlayerChoiceContext),typeof(Player)})]
internal static class NaturalShuffleObservationPatch
{
    [HarmonyPrefix] private static void Prefix(Player player,out ShuffleBridge.Scope? __state)
    { ShuffleBridge.Scope? scope=null;Probe.Safe(()=>scope=ShuffleBridge.ShufflePrefix(player));__state=scope; }
    [HarmonyPostfix] private static void Postfix(ShuffleBridge.Scope? __state,Task __result)=>Probe.Safe(()=>ShuffleBridge.ShufflePostfix(__state,__result));
    [HarmonyFinalizer] private static Exception? Finalizer(ShuffleBridge.Scope? __state,Exception? __exception)
    { Probe.Safe(()=>ShuffleBridge.SynchronousException(__state,__exception));return __exception; }
}
[HarmonyPatch]
internal static class PermutationObservationPatch
{
    [HarmonyTargetMethod] private static MethodBase TargetMethod()=>typeof(CardPileCmd).GetMethod(nameof(CardPileCmd.Shuffle),[typeof(PlayerChoiceContext),typeof(Player)])!
        .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType.GetMethod("MoveNext",BindingFlags.NonPublic|BindingFlags.Instance)!;
    [HarmonyTranspiler] private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,ILGenerator generator)
    {
        var target=typeof(ListExtensions).GetMethods(BindingFlags.Static|BindingFlags.Public).Single(m=>m.Name=="StableShuffle" && m.IsGenericMethodDefinition).MakeGenericMethod(typeof(CardModel));
        return CallSiteInstrumentation.Around(instructions.ToArray(),generator,target,
            typeof(PermutationObservationPatch).GetMethod(nameof(Before),BindingFlags.NonPublic|BindingFlags.Static)!,
            typeof(PermutationObservationPatch).GetMethod(nameof(After),BindingFlags.NonPublic|BindingFlags.Static)!);
    }
    private static ShuffleBridge.Scope? Before(List<CardModel> list,MegaCrit.Sts2.Core.Random.Rng rng)
    { ShuffleBridge.Scope? scope=null;Probe.Safe(()=>scope=ShuffleBridge.PermutationPrefix(list,rng));return scope; }
    private static void After(List<CardModel> list,ShuffleBridge.Scope? scope)=>Probe.Safe(()=>ShuffleBridge.PermutationPostfix(scope,list));
}
[HarmonyPatch(typeof(Hook),nameof(Hook.ModifyShuffleOrder),new[]{typeof(MegaCrit.Sts2.Core.Combat.ICombatState),typeof(Player),typeof(List<CardModel>),typeof(bool)})]
internal static class ShuffleHookObservationPatch
{ [HarmonyPostfix] private static void Postfix(Player player,List<CardModel> cards,bool isInitialShuffle)=>Probe.Safe(()=>ShuffleBridge.AfterHook(player,cards,isInitialShuffle)); }
