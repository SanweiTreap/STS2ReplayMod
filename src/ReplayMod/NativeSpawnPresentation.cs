using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace ReplayMod;

// The two native helpers run only after the creatures have actually been added.
// Keep their presentation bound to the original nodes and navigation generation.
internal static class NativeSpawnPresentation
{
    private sealed record Reveal(CombatState State,int Generation,bool ElitePhase,(Creature Unit,NCreature? Node)[] Units);
    private static readonly HashSet<Reveal> Pending=[];

    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(StockPower),"RevealReplacementAfterDeathAnim"),
            prefix:new HarmonyMethod(typeof(NativeSpawnPresentation),nameof(StockBefore)));
        h.Patch(AccessTools.DeclaredMethod(typeof(InfestedPower),"RevealWrigglersAfterDeathAnim"),
            prefix:new HarmonyMethod(typeof(NativeSpawnPresentation),nameof(InfestedBefore)));
    }

    internal static void Clear()=>Pending.Clear();
    internal static bool PendingElitePhase(CombatState state)=>Pending.Any(reveal=>reveal.ElitePhase&&
        reveal.Generation==NativeSeek.Generation&&ReferenceEquals(reveal.State,state));
    internal static bool Reveals(Creature unit)=>unit.Monster is Axebot {ShouldPlaySpawnAnimation:true} or Wriggler {StartStunned:true};
    internal static bool WillReveal(Creature unit)=>Pending.Any(reveal=>reveal.Generation==NativeSeek.Generation&&
        reveal.Units.Any(entry=>ReferenceEquals(entry.Unit,unit)));

    private static bool StockBefore(Creature __0,float __1,ref Task __result)=>Begin([__0],__1,elitePhase:false,ref __result);
    private static bool InfestedBefore(List<Creature> __0,float __1,ref Task __result)=>Begin(__0,__1,elitePhase:true,ref __result);
    private static bool Begin(IReadOnlyList<Creature> units,float delay,bool elitePhase,ref Task result)
    {
        if(!NativeSeek.Enabled||TestMode.IsOn||CombatManager.Instance.DebugOnlyGetState() is not {} state)return true;
        var reveal=new Reveal(state,NativeSeek.Generation,elitePhase,
            units.Select(unit=>(unit,NCombatRoom.Instance?.GetCreatureNode(unit))).ToArray());
        result=Play(reveal,delay);
        return false;
    }

    private static async Task Play(Reveal reveal,float delay)
    {
        Pending.Add(reveal);
        try
        {
            await Cmd.CustomScaledWait(delay,delay);
            if(!Pending.Contains(reveal)||!NativeSeek.Enabled||NativeSeek.RetiringPresentation||NativeSeek.Stopping||
                reveal.Generation!=NativeSeek.Generation||!ReferenceEquals(reveal.State,CombatManager.Instance.DebugOnlyGetState()))
            {
                Runtime.Log("native-enemy-reveal-retired",new{reveal.Generation,CurrentGeneration=NativeSeek.Generation,reveal.ElitePhase,
                    Units=reveal.Units.Select(entry=>entry.Unit.CombatId).ToArray()});
                return;
            }
            var room=NCombatRoom.Instance;
            var ready=reveal.Units.Where(entry=>entry.Unit.IsAlive&&ReferenceEquals(entry.Unit.CombatState,reveal.State)&&
                entry.Node is { } node&&GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion()&&
                ReferenceEquals(node,room?.GetCreatureNode(entry.Unit))).ToArray();
            if(ready.Length==0)return;
            if(reveal.ElitePhase)NRunMusicController.Instance?.TriggerEliteSecondPhase();
            foreach(var (unit,node) in ready)
            {
                node!.Show();
                if(!reveal.ElitePhase)await CreatureCmd.TriggerAnim(unit,Axebot.respawnTrigger,0f);
            }
            Runtime.Log("native-enemy-reveal-completed",new{reveal.Generation,reveal.ElitePhase,Units=ready.Select(entry=>entry.Unit.CombatId).ToArray()});
        }
        finally{Pending.Remove(reveal);}
    }
}
