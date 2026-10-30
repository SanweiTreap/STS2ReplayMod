using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ReplayMod;

internal static class CombatUnitSubscriptions
{
    private static readonly (FieldInfo Field,string Handler)[] Events=new[]{
        ("BlockChanged","OnCreatureValueChanged"),("CurrentHpChanged","OnCreatureValueChanged"),("MaxHpChanged","OnCreatureValueChanged"),
        ("PowerApplied","OnPowerAppliedOrRemoved"),("PowerIncreased","OnPowerIncreased"),("PowerDecreased","OnPowerDecreased"),
        ("PowerRemoved","OnPowerAppliedOrRemoved"),("Died","OnCreatureChanged")}
        .Select(p=>(AccessTools.Field(typeof(Creature),p.Item1),p.Item2)).ToArray();
    private static void Set(CombatStateTracker tracker,IEnumerable<Creature> known,ISet<Creature> active)
    {
        var units=known.Distinct().ToArray();
        foreach(var unit in units)
            foreach(var (field,handler) in Events)
            {
                var value=(Delegate?)field.GetValue(unit);
                foreach(var callback in value?.GetInvocationList()??[])
                    if(ReferenceEquals(callback.Target,tracker)&&callback.Method.DeclaringType==typeof(CombatStateTracker)&&callback.Method.Name==handler)
                        value=Delegate.Remove(value,callback);
                field.SetValue(unit,value);
            }
        foreach(var unit in active)tracker.Subscribe(unit);
        foreach(var unit in units)
            foreach(var (field,handler) in Events)
            {
                var count=((Delegate?)field.GetValue(unit))?.GetInvocationList().Count(d=>ReferenceEquals(d.Target,tracker)&&
                    d.Method.DeclaringType==typeof(CombatStateTracker)&&d.Method.Name==handler)??0;
                Codec.Require(count==(active.Contains(unit)?1:0),"combat-unit-state-tracker-subscription-count");
            }
        Runtime.Log("combat-unit-subscriptions-reconciled",new{Known=units.Length,Active=active.Count,CallbacksPerActiveUnit=Events.Length,
            RetiredCallbacksRemaining=0,OldDelegatesRestored=false});
    }
    internal static void Retire(IEnumerable<Creature> known)=>Set(CombatManager.Instance.StateTracker,known,new HashSet<Creature>());
    internal static void Restore(IEnumerable<Creature> known,IEnumerable<Creature> active)=>Set(CombatManager.Instance.StateTracker,known,active.ToHashSet());
    internal static void Cleanup(CombatStateTracker tracker,IEnumerable<Creature> known)=>Set(tracker,known,new HashSet<Creature>());
}
