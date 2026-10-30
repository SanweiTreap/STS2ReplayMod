using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReplayMod;

internal static class OstySubscriptions
{
    internal static void Restore(IEnumerable<Creature> known)
    {
        if(LiveState.Format<8)return;
        var pcs=LiveState.Player.PlayerCombatState!;var all=known.Where(CombatUnits.IsOsty).ToArray();
        Set(pcs,all,true);
        Runtime.Log("osty-subscriptions-restored",new{Known=all.Length,Pets=pcs.Pets.Count,NativePetDiedCallback=true,OldDelegatesRestored=false});
    }
    internal static void Cleanup(PlayerCombatState pcs,IEnumerable<Creature> known)
    {
        var all=known.Where(c=>c.Monster is MegaCrit.Sts2.Core.Models.Monsters.Osty).ToArray();Set(pcs,all,false);
        Runtime.Log("osty-subscriptions-cleaned",new{Known=all.Length,NativePetDiedCallbacksRemaining=0});
    }
    private static void Set(PlayerCombatState pcs,IEnumerable<Creature> all,bool restore)
    {
        var method=AccessTools.Method(typeof(PlayerCombatState),"OnPetDied");
        var handler=(Action<Creature>)Delegate.CreateDelegate(typeof(Action<Creature>),pcs,method);
        foreach(var pet in all)
        {
            var field=AccessTools.Field(typeof(Creature),"Died");
            var existing=(field.GetValue(pet) as Delegate)?.GetInvocationList()??[];
            foreach(var previous in existing.Where(d=>ReferenceEquals(d.Target,pcs)&&d.Method==method))pet.Died-=(Action<Creature>)previous;
            var expected=restore&&pcs.Pets.Contains(pet)?1:0;
            if(expected==1)pet.Died+=handler;
            var actual=(field.GetValue(pet) as Delegate)?.GetInvocationList()??[];
            Codec.Require(actual.Count(d=>ReferenceEquals(d.Target,pcs)&&d.Method==method)==expected,"osty-pet-death-subscription-mismatch");
        }
    }
}
