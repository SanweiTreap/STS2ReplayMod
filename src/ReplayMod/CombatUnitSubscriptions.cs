using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ReplayMod;

internal static class CombatUnitSubscriptions
{
    private static readonly (FieldInfo Field,string Handler)[] Events=new[]{
        ("BlockChanged","OnCreatureValueChanged"),("CurrentHpChanged","OnCreatureValueChanged"),("MaxHpChanged","OnCreatureValueChanged"),
        ("PowerApplied","OnPowerAppliedOrRemoved"),("PowerIncreased","OnPowerIncreased"),("PowerDecreased","OnPowerDecreased"),
        ("PowerRemoved","OnPowerAppliedOrRemoved"),("Died","OnCreatureChanged")}
        .Select(p=>(AccessTools.Field(typeof(Creature),p.Item1),p.Item2)).ToArray();
    private static readonly (FieldInfo Field,MethodInfo Handler)[] ModelEvents=new (string Event,Type Model,string Handler)[]{
        ("Died",typeof(SlumberingBeetle),"AfterDeath"),("Died",typeof(SoulNexus),"AfterDeath"),
        ("CurrentHpChanged",typeof(CubexConstruct),"OnHpChanged"),
        ("PowerApplied",typeof(TestSubject),"AfterPowerApplied"),("PowerRemoved",typeof(TestSubject),"AfterPowerRemoved")}
        .Select(p=>(AccessTools.Field(typeof(Creature),p.Event),AccessTools.DeclaredMethod(p.Model,p.Handler))).ToArray();

    internal sealed record ModelSubscription(Creature Unit,FieldInfo Field,MethodInfo Handler,Delegate[] Order);
    internal static ModelSubscription[] CaptureModels(IEnumerable<Creature> units)=>units.SelectMany(unit=>ModelEvents
        .Where(binding=>binding.Handler.DeclaringType!.IsInstanceOfType(unit.Monster))
        .Select(binding=>new ModelSubscription(unit,binding.Field,binding.Handler,
            (binding.Field.GetValue(unit) as Delegate)?.GetInvocationList()??[]))).ToArray();

    internal static void RestoreModels(IEnumerable<Creature> anchors,ModelSubscription[] snapshots)
    {
        foreach(var unit in anchors)
            foreach(var (field,handler) in ModelEvents)
            {
                if(!handler.DeclaringType!.IsInstanceOfType(unit.Monster))continue;
                bool IsOwned(Delegate callback)=>ReferenceEquals(callback.Target,unit.Monster)&&callback.Method==handler;
                var before=(field.GetValue(unit) as Delegate)?.GetInvocationList()??[];
                var order=snapshots.FirstOrDefault(s=>ReferenceEquals(s.Unit,unit)&&s.Field==field&&s.Handler==handler)?.Order??[];
                var saved=Array.FindIndex(order,IsOwned);
                var current=before.Where(callback=>!IsOwned(callback)).ToList();
                if(saved>=0)
                {
                    // Only reattach the audited model callback. Keep the order of
                    // live observers, and never reinstall a retired scene delegate.
                    int Anchor(Delegate old)
                    {
                        var index=current.IndexOf(old);
                        if(index>=0||old.Target is not (NCreature or NCreatureStateDisplay or NPowerContainer))return index;
                        return current.FindIndex(candidate=>candidate.Method==old.Method&&candidate.Target?.GetType()==old.Target.GetType());
                    }
                    var next=order.Skip(saved+1).Select(Anchor).FirstOrDefault(index=>index>=0,-1);
                    var prior=order.Take(saved).Reverse().Select(Anchor).FirstOrDefault(index=>index>=0,-1);
                    current.Insert(next>=0?next:prior>=0?prior+1:current.Count,order[saved]);
                }
                if(!before.SequenceEqual(current))
                {
                    field.SetValue(unit,Delegate.Combine(current.ToArray()));
                    Runtime.Log("native-creature-model-subscription-restored",new{unit.CombatId,Event=field.Name,Handler=handler.Name,Attached=saved>=0});
                }
            }
    }
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
