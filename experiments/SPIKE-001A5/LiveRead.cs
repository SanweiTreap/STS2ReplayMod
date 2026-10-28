using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Spike001A5.Core;
using P=Spike001A5.Core.Player;

namespace Spike001A5;

internal static class LiveRead
{
    private static object?[]? _previous;
    private static long _generation;
    internal static void ResetIdentity() { _previous=null; _generation++; }
    internal static (Field[] Fields,string Identity) Read(Binding binding,string? expectedRoom,string? expectedEncounter)
    {
        var f=new FieldReader();
        var manager=f.Object("managerPresent",()=>RunManager.Instance);
        var state=f.Object("statePresent",()=>manager!.DebugOnlyGetState(),manager is not null);
        var player=f.Object("singlePlayerPresent",()=>state!.Players.Count==1?state.Players[0]:null,state is not null);
        var pcs=f.Object("playerCombatStatePresent",()=>player!.PlayerCombatState,player is not null);
        var hand=f.Object("handPresent",()=>pcs!.Hand,pcs is not null);
        var combat=f.Object("combatPresent",()=>CombatManager.Instance);
        var executor=f.Object("executorPresent",()=>manager!.ActionExecutor,manager is not null);
        var room=f.Object("roomPresent",()=>state!.CurrentRoom as CombatRoom,state is not null);
        f.Bool("combatIsInProgress",()=>combat!.IsInProgress,combat is not null);
        f.Bool("combatIsNotOverOrEnding",()=>!combat!.IsOverOrEnding,combat is not null);
        f.Bool("combatIsNotPaused",()=>!combat!.IsPaused,combat is not null);
        f.Bool("runIsNotPaused",()=>!manager!.IsPaused,manager is not null);
        f.Bool("playerActionsEnabled",()=>!combat!.PlayerActionsDisabled,combat is not null);
        f.Bool("isPartOfPlayerTurn",()=>combat!.IsPartOfPlayerTurn(player!),combat is not null && player is not null);
        f.Bool("phaseIsPlay",()=>pcs!.Phase==PlayerTurnPhase.Play,pcs is not null);
        f.Bool("executorIsNotRunning",()=>!executor!.IsRunning,executor is not null);
        f.Bool("executorIsNotPaused",()=>!executor!.IsPaused,executor is not null);
        f.Bool("currentActionIsNull",()=>executor!.CurrentlyRunningAction is null,executor is not null);
        var node=f.Object("currentRunNodePresent",()=>NGame.Instance?.CurrentRunNode);
        var identity="";
        f.Bool("targetIdentityStable",()=>
        {
            var coord=state!.CurrentMapCoord;
            var stable=coord.HasValue && state.TotalFloor==binding.PlayableFloor && state.CurrentActIndex==binding.Target.Act &&
                coord.Value.row==binding.Target.Row && coord.Value.col==binding.Target.Col &&
                state.Rng.ToSerializable().Seed==binding.Target.Seed && player!.Character.Id.ToString()==binding.Target.Character &&
                room!.RoomType.ToString()=="Monster" && !string.IsNullOrWhiteSpace(room.ModelId.ToString()) &&
                !string.IsNullOrWhiteSpace(room.CombatState.Encounter?.Id.ToString()) &&
                (expectedRoom is null || room.ModelId.ToString()==expectedRoom) &&
                (expectedEncounter is null || room.CombatState.Encounter.Id.ToString()==expectedEncounter) &&
                ReferenceEquals(room.CombatState,combat!.DebugOnlyGetState()) && room.CombatState.RoundNumber==1;
            object?[] objects=[manager,state,player,pcs,combat,executor,room,room!.CombatState,room.CombatState.Encounter,
                hand,pcs!.DrawPile,pcs.DiscardPile,pcs.ExhaustPile,node];
            if(_previous is null || !objects.Zip(_previous).All(x=>ReferenceEquals(x.First,x.Second)))
            { _generation++; _previous=objects; }
            identity=_generation.ToString();return stable;
        },state is not null && player is not null && room is not null && combat is not null && pcs is not null);
        return(f.Fields,identity);
    }
    private static readonly FieldInfo CostField=typeof(CardModel).GetField("_energyCost",BindingFlags.NonPublic|BindingFlags.Instance)??throw new MissingFieldException("_energyCost");
    private static readonly FieldInfo VarsField=typeof(CardModel).GetField("_dynamicVars",BindingFlags.NonPublic|BindingFlags.Instance)??throw new MissingFieldException("_dynamicVars");
    private static readonly PropertyInfo CanonicalCost=typeof(CardModel).GetProperty("CanonicalEnergyCost",BindingFlags.NonPublic|BindingFlags.Instance)??throw new MissingMemberException("CanonicalEnergyCost");
    private static readonly PropertyInfo HasX=typeof(CardModel).GetProperty("HasEnergyCostX",BindingFlags.NonPublic|BindingFlags.Instance)??throw new MissingMemberException("HasEnergyCostX");
    private static readonly FieldInfo BaseCost=typeof(CardEnergyCost).GetField("_base",BindingFlags.NonPublic|BindingFlags.Instance)??throw new MissingFieldException("_base");
    private static readonly FieldInfo CapturedX=typeof(CardEnergyCost).GetField("_capturedXValue",BindingFlags.NonPublic|BindingFlags.Instance)??throw new MissingFieldException("_capturedXValue");
    private static readonly FieldInfo LocalCosts=typeof(CardEnergyCost).GetField("_localModifiers",BindingFlags.NonPublic|BindingFlags.Instance)??throw new MissingFieldException("_localModifiers");
    private static readonly FieldInfo Keywords=typeof(CardModel).GetField("_keywords",BindingFlags.NonPublic|BindingFlags.Instance)??throw new MissingFieldException("_keywords");
    internal static string Capture()
    {
        var state=RunManager.Instance.DebugOnlyGetState()??throw new InvalidDataException("state-missing");
        Contract.Require(state.Players.Count==1,"player-count");
        var player=state.Players[0]; var pcs=player.PlayerCombatState??throw new InvalidDataException("pcs-missing");
        var room=state.CurrentRoom as CombatRoom??throw new InvalidDataException("room-missing");
        var before=Guard(state,player,room);
        var result=Build(state,player,room);
        var frozen=Projection.Freeze(result);
        Contract.Require(before==Guard(state,player,room),"oracle-mutated-live-state");
        // A second detached capture detects changes caused by projection getters/hooks.
        Contract.Require(frozen==Projection.Freeze(Build(state,player,room)),"oracle-not-stable");
        Contract.Require(before==Guard(state,player,room),"oracle-mutated-live-state");
        return frozen;
    }
    private static string Guard(RunState state,MegaCrit.Sts2.Core.Entities.Players.Player player,CombatRoom room)
    {
        var pcs=player.PlayerCombatState!;
        var piles=new[]{pcs.Hand,pcs.DrawPile,pcs.DiscardPile,pcs.ExhaustPile};
        return Json.Canonical(Json.Serialize(new {
            RunRng=state.Rng.ToSerializable(),PlayerRng=player.PlayerRng.ToSerializable(),player.Creature.CurrentHp,player.Creature.MaxHp,
            pcs.Energy,pcs.Stars,player.Creature.Block, Powers=Powers(player.Creature),
            Piles=piles.Select(p=>p.Cards.Select(c=>new { Ref=RuntimeHelpers.GetHashCode(c),Persist=Persistent(c),
                CostRef=CostField.GetValue(c) is {} cost?RuntimeHelpers.GetHashCode(cost):(int?)null,
                CostState=CostField.GetValue(c) is {} energy?new { Base=BaseCost.GetValue(energy),X=CapturedX.GetValue(energy),Local=LocalCosts.GetValue(energy) }:null,
                Keywords=Keywords.GetValue(c),Affliction=c.Affliction?.Id.ToString(),AfflictionAmount=c.Affliction?.Amount,
                VarsRef=VarsField.GetValue(c) is {} vars?RuntimeHelpers.GetHashCode(vars):(int?)null,
                VarsState=VarsField.GetValue(c) is MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVarSet vs?
                    vs.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new Variable(x.Key,x.Value.BaseValue,x.Value.EnchantedValue)).ToArray():null }).ToArray()).ToArray(),
            Enemies=room.CombatState.Enemies.Select(e=> { Contract.Require(e.Monster is {IsMutable:true},"enemy-model-not-mutable");
                return new { e.CombatId,e.CurrentHp,e.MaxHp,e.Block,Powers=Powers(e),Rng=e.Monster.Rng.ToSerializable() }; }).ToArray()
        }));
    }
    private static Semantic Build(RunState state,MegaCrit.Sts2.Core.Entities.Players.Player player,CombatRoom room)
    {
        var coord=state.CurrentMapCoord??throw new InvalidDataException("coord-missing"); var pcs=player.PlayerCombatState!;
        var run=state.Rng.ToSerializable(); var prng=player.PlayerRng.ToSerializable();
        return new(new(state.CurrentActIndex,coord.row,coord.col,Contract.Text(room.ModelId.ToString(),"room-model"),room.RoomType.ToString(),
            Contract.Text(room.CombatState.Encounter?.Id.ToString(),"encounter"),state.TotalFloor,room.CombatState.RoundNumber),
            new P(player.Character.Id.ToString(),player.Creature.CurrentHp,player.Creature.MaxHp,pcs.Energy,player.Creature.Block,pcs.Stars,
                Powers(player.Creature),Cards(pcs.Hand),Cards(pcs.DrawPile),Cards(pcs.DiscardPile),Cards(pcs.ExhaustPile)),
            room.CombatState.Enemies.OrderBy(e=>e.CombatId).Select(e=>new Enemy(e.ModelId.ToString(),e.CombatId??throw new InvalidDataException("enemy-id-missing"),
                e.CurrentHp,e.MaxHp,e.Block,e.IsAlive,e.IsDead,room.CombatState.EscapedCreatures.Contains(e),
                Contract.Text(e.Monster?.NextMove.Id,"next-move"),Powers(e),Projection.Rng("derived-instance;seed-not-serialized",[Rng("Instance",e.Monster!.Rng.ToSerializable())]))).ToArray(),
            Projection.Rng(Contract.Text(run.Seed,"run-seed"),run.Rngs.Select(x=>Rng(x.Key.ToString(),x.Value))),
            Projection.Rng(prng.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture),prng.Rngs.Select(x=>Rng(x.Key.ToString(),x.Value))));
    }
    private static Power[] Powers(Creature c)=>c.Powers.Select(p=>new Power(p.Id.ToString(),p.Amount)).ToArray();
    private static RngEntry Rng(string key,SerializableRng r)=>new(key,r.counter,r.state0,r.state1,r.state2,r.state3);
    private static Card[] Cards(CardPile p)=>p.Cards.Select(Card).ToArray();
    private static string Persistent(CardModel c)=>Json.Canonical(System.Text.Json.JsonSerializer.Serialize(c.ToSerializable(),JsonSerializationUtility.Options));
    private static Card Card(CardModel c)
    {
        // Avoid the lazy EnergyCost/DynamicVars getters initializing production caches.
        var cost=(CardEnergyCost?)CostField.GetValue(c)??new CardEnergyCost(c,(int)CanonicalCost.GetValue(c)!,(bool)HasX.GetValue(c)!);
        var variables=(MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVarSet?)VarsField.GetValue(c)
            ??throw new InvalidDataException("card-dynamic-vars-uninitialized");
        return new(c.Id.ToString(),c.CurrentUpgradeLevel,c.IsUpgraded,Persistent(c),cost.Canonical,cost.GetWithModifiers(CostModifiers.All),
            cost.CostsX,cost.CostsX?cost.CapturedXValue:null,c.CurrentStarCost,c.BaseReplayCount,c.ExhaustOnNextPlay,
            variables.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new Variable(x.Key,x.Value.BaseValue,x.Value.EnchantedValue)).ToArray(),
            (int)BaseCost.GetValue(cost)!,((IEnumerable<LocalCostModifier>)LocalCosts.GetValue(cost)!).Select(x=>new CostModifier(x.Amount,x.Type.ToString(),x.Expiration.ToString(),x.IsReduceOnly)).ToArray(),
            c.GetKeywordsWithSources(KeywordSources.All).Select(x=>x.ToString()).Order(StringComparer.Ordinal).ToArray(),c.Affliction?.Id.ToString(),c.Affliction?.Amount);
    }
}
