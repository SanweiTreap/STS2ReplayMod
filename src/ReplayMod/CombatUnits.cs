using HarmonyLib;
using System.Globalization;
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Rooms;

namespace ReplayMod;

internal static class CombatUnits
{
    internal static bool IsOsty(Creature creature)=>LiveState.Format>=8&&creature.Monster is Osty&&ReferenceEquals(creature.PetOwner,LiveState.Player)&&
        creature.Side==CombatSide.Player;
    internal static bool Restorable(Creature creature)=>creature.IsAlive||IsOsty(creature);
    internal static bool CanGenerate(MonsterModel monster,CombatSide side)=>side==CombatSide.Player?
        LiveState.Format>=8&&monster is Osty:LiveState.Format>=10&&side==CombatSide.Enemy&&monster.IsMutable&&monster.GetType().Assembly==typeof(MonsterModel).Assembly;
    internal static EnemyUnitEntry Enemy(Creature creature)
    {
        var monster=creature.Monster??throw new InvalidDataException("enemy-without-monster");var rng=monster.Rng.ToSerializable();
        var mutable=new SortedDictionary<string,string?>(StringComparer.Ordinal);
        for(var type=monster.GetType();type!=typeof(MonsterModel);type=type.BaseType!)
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                if(field.FieldType.IsPrimitive||field.FieldType.IsEnum||field.FieldType==typeof(decimal)||field.FieldType==typeof(string))
                    mutable.Add(type.Name+"."+field.Name,Convert.ToString(field.GetValue(monster),CultureInfo.InvariantCulture));
        var ai=monster.MoveStateMachine;
        return new(creature.CombatId??throw new InvalidDataException("enemy-without-combat-identity"),monster.Id.ToString(),creature.SlotName,creature.IsAlive,
            creature.CurrentHp,creature.MaxHp,creature.Block,monster.NextMove?.Id,new(rng.counter,rng.state0,rng.state1,rng.state2,rng.state3),
            creature.Powers.Select(p=>new UnitPowerState(p.Id.ToString(),p.Amount)).ToArray(),
            ai is null?null:(AccessTools.Field(typeof(MonsterMoveStateMachine),"_currentState").GetValue(ai) as MonsterState)?.Id,
            ai is not null&&(bool)AccessTools.Field(typeof(MonsterMoveStateMachine),"_performedFirstMove").GetValue(ai)!,
            ai?.StateLog.Select(s=>s.Id).ToArray()??[],mutable);
    }
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
            player.PlayerCombatState.Pets.Select(c=>c.CombatId!.Value).ToArray(),LiveState.Format>=10?combat.Enemies.Select(Enemy).ToArray():null);
    }
}
