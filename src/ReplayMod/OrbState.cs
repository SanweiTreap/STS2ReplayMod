using System.Globalization;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Models;

namespace ReplayMod;

internal static class OrbState
{
    internal static OrbIdentityMap<OrbModel> Identities {get;private set;}=new();
    internal static void Reset()=>Identities=new();
    internal static void Admit(OrbQueue queue,OrbModel orb)
    {
        if(LiveState.Format<8||!(Recorder.CollectingChoices||PlayerLoop.Active&&!PlayerLoop.TearingDown))return;
        Codec.Require(ReferenceEquals(queue,LiveState.Player.PlayerCombatState!.OrbQueue)&&ReferenceEquals(orb.Owner,LiveState.Player),"foreign-orb-queue-or-owner");
        if(queue.Capacity<=0||queue.Orbs.Count>=queue.Capacity)return;
        var identity=Identities.Admit(orb);
        Runtime.Log("orb-native-admitted",new{Identity=identity,Model=orb.Id.ToString(),queue.Capacity,OriginalQueueUnchanged=true});
    }
    internal static OrbQueueState Capture()
    {
        var queue=LiveState.Player.PlayerCombatState!.OrbQueue;
        return new(queue.Capacity,Identities.NextId,queue.Orbs.Select(orb=>
        {
            Codec.Require(ReferenceEquals(orb.Owner,LiveState.Player)&&orb.IsMutable,"orb-owner-or-mutability");
            var mutable=new SortedDictionary<string,string?>(StringComparer.Ordinal);
            for(var type=orb.GetType();type!=typeof(OrbModel);type=type.BaseType!)
                foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                {
                    Codec.Require(field.FieldType.IsPrimitive||field.FieldType.IsEnum||field.FieldType==typeof(decimal)||field.FieldType==typeof(string),"unsupported-orb-mutable-field");
                    mutable.Add(type.Name+"."+field.Name,Convert.ToString(field.GetValue(orb),CultureInfo.InvariantCulture));
                }
            return new OrbEntry(Identities.Require(orb),orb.Id.ToString(),0,orb.HasBeenRemovedFromState,orb.PassiveVal,orb.EvokeVal,mutable);
        }).ToArray());
    }
}
