using System.Reflection;
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
    [HarmonyTargetMethod] private static MethodBase TargetMethod()=>typeof(ListExtensions).GetMethods(BindingFlags.Static|BindingFlags.Public)
        .Single(m=>m.Name=="StableShuffle" && m.IsGenericMethodDefinition).MakeGenericMethod(typeof(CardModel));
    [HarmonyPrefix] private static void Prefix(object __0,object __1,out ShuffleBridge.Scope? __state)
    { ShuffleBridge.Scope? scope=null;Probe.Safe(()=>scope=ShuffleBridge.PermutationPrefix(__0,__1));__state=scope; }
    [HarmonyPostfix] private static void Postfix(object __0,ShuffleBridge.Scope? __state)=>Probe.Safe(()=>ShuffleBridge.PermutationPostfix(__state,__0));
    [HarmonyFinalizer] private static Exception? Finalizer(ShuffleBridge.Scope? __state,Exception? __exception)
    { Probe.Safe(()=>ShuffleBridge.SynchronousException(__state,__exception));return __exception; }
}
[HarmonyPatch(typeof(Hook),nameof(Hook.ModifyShuffleOrder),new[]{typeof(MegaCrit.Sts2.Core.Combat.ICombatState),typeof(Player),typeof(List<CardModel>),typeof(bool)})]
internal static class ShuffleHookObservationPatch
{ [HarmonyPostfix] private static void Postfix(Player player,List<CardModel> cards,bool isInitialShuffle)=>Probe.Safe(()=>ShuffleBridge.AfterHook(player,cards,isInitialShuffle)); }
