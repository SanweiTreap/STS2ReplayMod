using System.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace ReplayMod;

internal static class CombatCardTracking
{
    internal static void Cleanup(CombatRoom room)
    {
        var database=NetCombatCardDb.Instance;
        var subscriptions=(IList)AccessTools.Field(typeof(NetCombatCardDb),"_subscriptions").GetValue(database)!;
        var before=subscriptions.Count;
        AccessTools.Method(typeof(NetCombatCardDb),"OnCombatEnded").CreateDelegate<Action<CombatRoom>>(database)(room);
        Codec.Require(subscriptions.Count==0,"playback-card-tracking-cleanup-incomplete");
        Runtime.Log("combat-card-tracking-cleanup",new{Before=before,Remaining=subscriptions.Count,NativeCombatEndCleanup=true});
    }
    internal static void RestoreSubscriptions()
    {
        var database=NetCombatCardDb.Instance;
        var subscriptions=(IList)AccessTools.Field(typeof(NetCombatCardDb),"_subscriptions").GetValue(database)!;
        var type=AccessTools.Inner(typeof(NetCombatCardDb),"Subscription")!;
        var pileField=AccessTools.Field(type,"pile");var actionField=AccessTools.Field(type,"action");
        var nextField=AccessTools.Field(typeof(NetCombatCardDb),"_nextId");var next=(uint)nextField.GetValue(database)!;
        var changed=AccessTools.Method(typeof(NetCombatCardDb),"OnPileContentsChanged").CreateDelegate<Action<CardPile>>(database);
        var piles=LiveState.Player.PlayerCombatState!.AllPiles.ToArray();
        var attached=PileSubscriptionRepair.AttachMissing(piles,subscriptions.Cast<object>().Select(s=>(CardPile)pileField.GetValue(s)!),pile=>
        {
            // Recreate only the native registration subscription. StartCombat would reset
            // the restored ID maps/counter; restoring old delegates would revive stale owners.
            Action action=()=>changed(pile);
            var subscription=Activator.CreateInstance(type)!;
            pileField.SetValue(subscription,pile);actionField.SetValue(subscription,action);
            subscriptions.Add(subscription);pile.ContentsChanged+=action;
        });
        if(attached>0)
        {
            var ended=AccessTools.Method(typeof(NetCombatCardDb),"OnCombatEnded").CreateDelegate<Action<CombatRoom>>(database);
            CombatManager.Instance.CombatEnded-=ended;
            CombatManager.Instance.CombatEnded+=ended;
        }
        Codec.Require((uint)nextField.GetValue(database)! == next,"restore-card-tracking-changed-id-counter");
        var cards=LiveState.Player.PlayerCombatState.AllCards.Distinct().ToArray();
        Codec.Require(cards.All(c=>database.TryGetCardId(c,out var id)&&database.TryGetCard(id,out var mapped)&&ReferenceEquals(c,mapped)),
            "restore-card-tracking-existing-map-mismatch");
        Runtime.Log("combat-card-tracking-restored",new{Piles=piles.Length,Attached=attached,ExistingCards=cards.Length,
            NextId=next,ExistingIdsUnchanged=true,NativePileCallback=true,NativeCombatEndCleanup=true,StartCombatCalled=false});
    }
}
