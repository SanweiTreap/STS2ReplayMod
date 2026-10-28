using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReplayMod;

internal static class ChoicePatches
{
    internal static void Install(Harmony h)
    {
        foreach(var method in typeof(CardSelectCmd).GetMethods(BindingFlags.Public|BindingFlags.Static))
        {
            var ps=method.GetParameters();
            var leaf=method.Name switch
            {
                "FromChooseACardScreen" or "FromSimpleGridForRewards" or "FromSimpleGrid" or "FromDeckForUpgrade" or
                "FromDeckForTransformation" or "FromDeckGeneric" or "FromHand" or "FromHandForUpgrade" or "FromChooseABundleScreen"=>true,
                "FromCombatPile"=>ps.Length==5,
                "FromDeckForEnchantment"=>ps.Length==4&&ps[0].ParameterType==typeof(IReadOnlyList<CardModel>),_=>false
            };
            if(!leaf)continue;
            var one=method.ReturnType==typeof(Task<CardModel>);
            Codec.Require(one||method.ReturnType==typeof(Task<IEnumerable<CardModel>>),"choice-hook-return-type");
            h.Patch(method,new HarmonyMethod(typeof(ChoicePatches),nameof(Before)),new HarmonyMethod(typeof(ChoicePatches),one?nameof(AfterOne):nameof(AfterMany)));
        }
        h.Patch(AccessTools.Method(typeof(CardSelectCmd),"ShouldSelectLocalCard"),new HarmonyMethod(typeof(ChoicePatches),nameof(Local)));
        h.Patch(AccessTools.Method(typeof(PlayerChoiceSynchronizer),"ReserveChoiceId"),postfix:new HarmonyMethod(typeof(ChoicePatches),nameof(Reserved)));
        h.Patch(AccessTools.Method(typeof(PlayerChoiceSynchronizer),"SyncLocalChoice"),prefix:new HarmonyMethod(typeof(ChoicePatches),nameof(Synced)));
        h.Patch(AccessTools.Method(typeof(PlayerChoiceSynchronizer),"WaitForRemoteChoice"),prefix:new HarmonyMethod(typeof(ChoicePatches),nameof(Remote)));
        NativeChoice.Install(h);
    }
    private static void Before(MethodBase __originalMethod,object[] __args,out ChoiceScope? __state)
    {
        ChoiceScope? scope=null;Runtime.Guard(()=>scope=ChoiceTape.Begin(__originalMethod,__args));__state=scope;
    }
    private static void AfterMany(ref Task<IEnumerable<CardModel>> __result,ChoiceScope? __state)=>__result=ChoiceTape.Wrap(__result,__state);
    private static void AfterOne(ref Task<CardModel?> __result,ChoiceScope? __state)=>__result=ChoiceTape.Wrap(__result,__state);
    private static bool Local(ref bool __result)=>ChoiceTape.Local(ref __result);
    private static void Reserved(Player __0,uint __result)=>Runtime.Guard(()=>ChoiceTape.Reserved(__0,__result));
    private static void Synced(Player __0,uint __1,PlayerChoiceResult __2)=>Runtime.Guard(()=>ChoiceTape.Synced(__0,__1,__2));
    private static bool Remote(Player __0,uint __1,ref Task<PlayerChoiceResult> __result)=>ChoiceTape.Remote(__0,__1,ref __result);
}
