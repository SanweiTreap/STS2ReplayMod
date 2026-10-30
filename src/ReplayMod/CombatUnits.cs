using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace ReplayMod;

internal static class CombatUnits
{
    internal static bool IsOsty(Creature creature)=>LiveState.Format>=8&&creature.Monster is Osty&&ReferenceEquals(creature.PetOwner,LiveState.Player)&&
        creature.Side==CombatSide.Player;
    internal static bool Restorable(Creature creature)=>creature.IsAlive||IsOsty(creature);
    internal static CombatUnitState Capture()
    {
        var combat=((CombatRoom)LiveState.Run.CurrentRoom!).CombatState;var player=LiveState.Player;
        var allies=combat.Allies.Select(c=>
        {
            Codec.Require(ReferenceEquals(c,player.Creature)||IsOsty(c),"unsupported-combat-ally");
            Codec.Require(c.CombatId.HasValue,"ally-without-combat-identity");
            var rng=c.Monster?.Rng.ToSerializable();
            return new CombatUnitEntry(c.CombatId!.Value,c.ModelId.ToString(),c.PetOwner is null?null:0,c.IsAlive,c.CurrentHp,c.MaxHp,c.Block,
                c.Monster?.NextMove?.Id,rng is null?null:new(rng.counter,rng.state0,rng.state1,rng.state2,rng.state3),
                c.Powers.Select(p=>new UnitPowerState(p.Id.ToString(),p.Amount)).ToArray());
        }).ToArray();
        Codec.Require(player.PlayerCombatState!.Pets.All(IsOsty),"unsupported-combat-pet");
        return new((uint)AccessTools.Field(typeof(CombatState),"_nextCreatureId").GetValue(combat)!,allies,
            player.PlayerCombatState.Pets.Select(c=>c.CombatId!.Value).ToArray());
    }
}
