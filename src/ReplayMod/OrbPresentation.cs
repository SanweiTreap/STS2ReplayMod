using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ReplayMod;

internal static class OrbPresentation
{
    private static object? Get(object owner,string field)=>AccessTools.Field(owner.GetType(),field).GetValue(owner);
    private static NOrbManager Manager=>NCombatRoom.Instance!.GetCreatureNode(LiveState.Player.Creature)?.OrbManager??
        throw new InvalidDataException("orb-manager-missing");
    private static Delegate[] Handlers(OrbModel orb,string name)=>(Get(orb,name) as Delegate)?.GetInvocationList()??[];
    internal static int Retire()
    {
        if(LiveState.Format<8)return 0;
        var manager=Manager;var container=(Control)Get(manager,"_orbContainer")!;
        if(Get(manager,"_curTween") is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
        var nodes=container.GetChildren().OfType<NOrb>().ToArray();
        foreach(var node in nodes)
        {
            if(Get(node,"_curTween") is Tween entrance&&GodotObject.IsInstanceValid(entrance))entrance.Kill();
            // ExitTree runs native NOrbVfx unsubscribe while its old owner/model are intact.
            container.RemoveChild(node);node.QueueFree();
        }
        ((List<NOrb>)Get(manager,"_orbs")!).Clear();
        Codec.Require(OrbState.Identities.Known.All(orb=>Handlers(orb,"PassiveActivated").Concat(Handlers(orb,"EvokeActivated")).All(d=>d.Target is not NOrbVfx)),
            "retired-orb-display-subscription-remains");
        Runtime.Log("orb-displays-retired",new{Nodes=nodes.Length,NativeVfxListenersRemaining=0,LogicalQueueChanged=false});return nodes.Length;
    }
    internal static void Restore()
    {
        if(LiveState.Format<8)return;
        var manager=Manager;var queue=LiveState.Player.PlayerCombatState!.OrbQueue;
        manager.AddSlotAnim(queue.Capacity);
        var slots=(List<NOrb>)Get(manager,"_orbs")!;
        Codec.Require(slots.Count==queue.Capacity,"orb-restored-slot-count");
        for(var i=0;i<queue.Orbs.Count;i++)
        {
            slots[i].ReplaceOrb(queue.Orbs[i]);
            // Native entrance/layout tweens are display-only. Back arrives immediately.
            if(Get(slots[i],"_curTween") is Tween tween&&GodotObject.IsInstanceValid(tween)){tween.CustomStep(1);tween.Kill();}
        }
        if(Get(manager,"_curTween") is Tween layout&&GodotObject.IsInstanceValid(layout)){layout.CustomStep(1);layout.Kill();}
        manager.UpdateVisuals(OrbEvokeType.None);
        Verify();
        Runtime.Log("orb-displays-restored",new{State=OrbState.Capture(),Slots=slots.Count,NativeNodesAndVfx=true,EntranceAnimation=false,OriginalChannelOrEvokeRun=false});
    }
    private static void Verify()
    {
        var queue=LiveState.Player.PlayerCombatState!.OrbQueue;var slots=(List<NOrb>)Get(Manager,"_orbs")!;
        Codec.Require(slots.Count==queue.Capacity&&slots.Take(queue.Orbs.Count).Select(n=>n.Model).SequenceEqual(queue.Orbs)&&
            slots.Skip(queue.Orbs.Count).All(n=>n.Model is null),"orb-restored-node-order-or-identity");
        foreach(var orb in OrbState.Identities.Known)
        {
            var wanted=queue.Orbs.Contains(orb)?1:0;
            foreach(var name in new[]{"PassiveActivated","EvokeActivated"})
                Codec.Require(Handlers(orb,name).Count(d=>d.Target is NOrbVfx)==wanted,"orb-restored-vfx-listener-count");
        }
    }
}
