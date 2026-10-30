using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class LiveState
{
    internal static int Format=3;
    private static readonly JsonSerializerOptions Values=new(JsonSerializationUtility.Options){TypeInfoResolver=
        JsonTypeInfoResolver.Combine(JsonSerializationUtility.Options.TypeInfoResolver??new DefaultJsonTypeInfoResolver(),new DefaultJsonTypeInfoResolver())};
    internal static RunState Run=>RunManager.Instance.DebugOnlyGetState()??throw new InvalidDataException("run-not-present");
    internal static Player Player=>Run.Players.Count==1?Run.Players[0]:throw new InvalidDataException("not-singleplayer");
    internal static bool Playable
    {
        get
        {
            var r=RunManager.Instance.DebugOnlyGetState();var p=r?.Players.FirstOrDefault();var c=CombatManager.Instance;var e=RunManager.Instance.ActionExecutor;
            return r?.CurrentRoom is CombatRoom {RoomType:RoomType.Monster}&&p?.PlayerCombatState is {Phase:PlayerTurnPhase.Play}&&
                c.IsInProgress&&!c.IsOverOrEnding&&!c.IsPaused&&!c.PlayerActionsDisabled&&c.IsPartOfPlayerTurn(p)&&
                !e.IsRunning&&!e.IsPaused&&e.CurrentlyRunningAction is null;
        }
    }
    internal static bool InputPhase
    {
        get
        {
            var r=RunManager.Instance.DebugOnlyGetState();var p=r?.Players.FirstOrDefault();var c=CombatManager.Instance;
            return r?.CurrentRoom is CombatRoom {RoomType:RoomType.Monster}&&p?.PlayerCombatState is {Phase:PlayerTurnPhase.Play}&&
                c.IsInProgress&&!c.IsOverOrEnding&&!c.IsPaused&&!c.PlayerActionsDisabled&&c.IsPartOfPlayerTurn(p);
        }
    }
    internal static int Turn=>Player.PlayerCombatState?.TurnNumber??0;
    internal static void Supported()
    {
        var r=Run;var p=Player;
        Codec.Require(RunManager.Instance.NetService.Type.ToString()=="Singleplayer"&&r.CurrentActIndex==0&&r.AscensionLevel==0&&r.Modifiers.Count==0&&(Format>=9?r.CurrentMapCoord?.row>=1:r.CurrentMapCoord?.row==1)&&
            (p.Character.Id.ToString()=="CHARACTER.IRONCLAD"||Format>=4&&p.Character.Id.ToString()=="CHARACTER.SILENT"||Format>=5&&p.Character.Id.ToString()=="CHARACTER.REGENT"||Format>=8&&p.Character.Id.ToString() is "CHARACTER.DEFECT" or "CHARACTER.NECROBINDER")&&p.Deck.Cards.Count is >0 and <=128&&
            p.Deck.Cards.All(c=>IsCard(c)),"only-act1-monster-five-characters-A0-supported-format");
    }
    internal static bool SimpleTarget(TargetType type)=>type is TargetType.None or TargetType.Self or TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy or TargetType.AnyPlayer or TargetType.AllAllies||Format>=8&&type==TargetType.Osty;
    internal static bool IsCard(CardModel c)=>c.IsMutable&&c.GetType().Assembly==typeof(CardModel).Assembly&&SimpleTarget(c.TargetType)&&c.CurrentUpgradeLevel is >=0 and <=20;
    private static object Vars(DynamicVarSet vars)=>vars.OrderBy(v=>v.Key,StringComparer.Ordinal).Select(v=>new{v.Key,v.Value.BaseValue,v.Value.EnchantedValue}).ToArray();
    internal static object CardDetails(CardModel card)
    {
        var basic=new{Id=card.Id.ToString(),card.CurrentUpgradeLevel,Persistent=card.ToSerializable(),
        TargetType=card.TargetType.ToString(),Energy=card.EnergyCost.GetWithModifiers(CostModifiers.Local),card.EnergyCost.CostsX,
        CapturedX=card.EnergyCost.CostsX?(int?)card.EnergyCost.CapturedXValue:null,card.CurrentStarCost,card.ExhaustOnNextPlay,Vars=Vars(card.DynamicVars)};
        if(Format<4)return basic;
        var content=new{Base=basic,Keywords=card.Keywords.OrderBy(k=>k).Select(k=>k.ToString()).ToArray(),Tags=card.Tags.OrderBy(t=>t).Select(t=>t.ToString()).ToArray(),
            card.ShouldRetainThisTurn,card.IsSlyThisTurn,card.BaseReplayCount,Replay=card.GetEnchantedReplayCount()};
        if(Format<5)return content;
        uint? original=card.DupeOf is {} source&&NetCombatCardDb.Instance.TryGetCardId(source,out var id)?id:null;
        return new{Content=content,card.IsDupe,DupeOfCombatIndex=original,
            Blade=card is MegaCrit.Sts2.Core.Models.Cards.SovereignBlade blade?new{blade.CreatedThroughForge,
                Damage=blade.DynamicVars.Damage.BaseValue,Repeats=blade.DynamicVars.Repeat.BaseValue}:null};
    }
    internal static object PotionDetails(PotionModel potion,int slot)=>new{Slot=slot,Model=potion.Id.ToString(),Persistent=potion.ToSerializable(slot),TargetType=potion.TargetType.ToString(),Vars=Vars(potion.DynamicVars)};
    internal static string Fingerprint(object value)=>Codec.Hash(JsonSerializer.SerializeToUtf8Bytes(value,Values));
    internal static CardKey CardIdentity(CardModel card)=>new(NetCombatCard.FromModel(card).CombatCardIndex,card.CurrentUpgradeLevel,Fingerprint(CardDetails(card)),card.TargetType.ToString());
    internal static PotionKey PotionIdentity(PotionModel potion,int slot)=>new(slot,potion.Id.ToString(),Fingerprint(PotionDetails(potion,slot)),potion.TargetType.ToString());
    internal static object Mechanics()
    {
        var basic=new{Player=new{Turn=Turn,Player.PlayerCombatState!.Energy,Player.PlayerCombatState.MaxEnergy,Player.PlayerCombatState.Stars,
            Player.Creature.CurrentHp,Player.Creature.Block,Powers=Player.Creature.Powers.Select(p=>new{Model=p.Id.ToString(),p.Amount}).ToArray()},
        RoomHistory=RoomHistory(),
        Relics=Player.Relics.Select(r=>new{Model=r.Id.ToString(),Saved=r.ToSerializable(),Status=r.Status.ToString(),Vars=Vars(r.DynamicVars)}).ToArray(),
        Potions=Player.PotionSlots.Select((p,i)=>p is null?null:PotionDetails(p,i)).ToArray(),
        CombatCards=Player.PlayerCombatState!.AllPiles.Select(p=>new{Pile=p.Type.ToString(),Cards=p.Cards.Select(CardStateEntry).ToArray()}).ToArray()};
        if(Format<8)return basic;
        var node=JsonSerializer.SerializeToNode(basic,Values)!.AsObject();node["OrbQueue"]=JsonSerializer.SerializeToNode(OrbState.Capture(),Values);
        node["CombatUnits"]=JsonSerializer.SerializeToNode(CombatUnits.Capture(),Values);return node;
    }
    private static object CardStateEntry(CardModel card)
    {
        var state=CardDetails(card);
        return new{Key=new CardKey(NetCombatCard.FromModel(card).CombatCardIndex,card.CurrentUpgradeLevel,Fingerprint(state),card.TargetType.ToString()),State=state};
    }
    private static object? RoomHistory()
    {
        var history=Run.CurrentMapPointHistoryEntry?.GetEntry(Player.NetId);
        return history is null?null:new{history.DamageTaken,history.HpHealed,history.GoldGained,history.GoldSpent,
            PotionsUsed=history.PotionUsed.Select(p=>p.ToString()).ToArray()};
    }
    internal static object RestoreSummary()=>new
    {
        Player=new{Turn,Player.PlayerCombatState!.Energy,Player.Creature.CurrentHp,Player.Creature.Block},
        Piles=Player.PlayerCombatState.AllPiles.Select(p=>new{Pile=p.Type.ToString(),Cards=p.Cards.Select(c=>new{
            Model=c.Id.ToString(),CombatIndex=NetCombatCardDb.Instance.TryGetCardId(c,out var id)?(uint?)id:null,c.CurrentUpgradeLevel}).ToArray()}).ToArray(),
        Potions=Player.PotionSlots.Select(p=>p?.Id.ToString()).ToArray(),
        Relics=Player.Relics.Select(r=>new{Model=r.Id.ToString(),r.DisplayAmount,Status=r.Status.ToString()}).ToArray()
    };
    internal static TargetKey? Target(Creature? creature)
    {
        if(creature is null)return null;
        if(CombatUnits.IsOsty(creature))return new(0,creature.ModelId.ToString(),creature.CombatId!.Value,TargetRole.Pet);
        if(creature.IsPlayer)
        {
            Codec.Require(ReferenceEquals(creature,Player.Creature)&&creature.CombatId.HasValue,"only-local-player-target-supported");
            return new(0,creature.ModelId.ToString(),creature.CombatId!.Value,TargetRole.Player);
        }
        var enemies=((CombatRoom)Run.CurrentRoom!).CombatState.Enemies.ToList();var i=enemies.FindIndex(c=>ReferenceEquals(c,creature));
        Codec.Require(i>=0&&creature.Monster is not null&&creature.CombatId.HasValue,"only-ordinary-enemy-target-supported");
        return new(i,creature.Monster!.Id.ToString(),creature.CombatId!.Value);
    }
    internal static Creature? Resolve(TargetKey? key)
    {
        if(key is null)return null;
        if(key.Role==TargetRole.Pet)
        {
            var pet=((CombatRoom)Run.CurrentRoom!).CombatState.Allies.SingleOrDefault(CombatUnits.IsOsty);
            Codec.Require(key.Index==0&&pet is {IsAlive:true}&&pet.CombatId==key.CombatId&&pet.ModelId.ToString()==key.Model,"pet-target-content-or-identity");return pet;
        }
        if(key.Role==TargetRole.Player)
        {
            var creature=Player.Creature;Codec.Require(key.Index==0&&creature.IsAlive&&creature.CombatId==key.CombatId&&creature.ModelId.ToString()==key.Model,"player-target-content-or-identity");return creature;
        }
        var enemies=((CombatRoom)Run.CurrentRoom!).CombatState.Enemies.ToList();
        Codec.Require(key.Index>=0&&key.Index<enemies.Count,"target-index");var e=enemies[key.Index];
        Codec.Require(e.IsAlive&&e.Monster?.Id.ToString()==key.Model&&e.CombatId==key.CombatId,"target-content-or-identity");return e;
    }
    internal static string Capture()
    {
        var r=Run;var p=Player;var pcs=p.PlayerCombatState??throw new InvalidDataException("no-player-combat-state");
        var room=r.CurrentRoom as CombatRoom??throw new InvalidDataException("no-combat-room");
        object Card(CardModel c)=>new{Id=c.Id.ToString(),c.CurrentUpgradeLevel,Persistent=c.ToSerializable(),c.CurrentStarCost,
            c.ExhaustOnNextPlay,Affliction=c.Affliction?.Id.ToString(),AfflictionAmount=c.Affliction?.Amount};
        object Powers(Creature c)=>c.Powers.Select(v=>new{Id=v.Id.ToString(),v.Amount}).ToArray();
        var snapshot=new{Seed=r.Rng.ToSerializable().Seed,r.CurrentActIndex,r.ActFloor,Row=r.CurrentMapCoord?.row,Col=r.CurrentMapCoord?.col,
            Encounter=room.Encounter.Id.ToString(),room.CombatState.RoundNumber,Side=room.CombatState.CurrentSide.ToString(),
            RunRng=r.Rng.ToSerializable(),PlayerRng=p.PlayerRng.ToSerializable(),
            Player=new{p.Creature.CurrentHp,p.Creature.MaxHp,p.Creature.Block,p.Gold,pcs.Energy,pcs.Stars,pcs.TurnNumber,Phase=pcs.Phase.ToString(),Powers=Powers(p.Creature),
                Potions=p.PotionSlots.Select((v,i)=>v?.ToSerializable(i)).ToArray(),Relics=p.Relics.Select(v=>v.ToSerializable()).ToArray()},
            Deck=p.Deck.Cards.Select(Card).ToArray(),Piles=new[]{pcs.Hand,pcs.DrawPile,pcs.DiscardPile,pcs.ExhaustPile}.Select(pile=>pile.Cards.Select(Card).ToArray()).ToArray(),
            Enemies=room.CombatState.Enemies.Select(e=>new{Id=e.Monster!.Id.ToString(),e.CombatId,e.CurrentHp,e.MaxHp,e.Block,e.IsAlive,Powers=Powers(e),Rng=e.Monster.Rng.ToSerializable(),
                Move=e.IsAlive?e.Monster.NextMove.Id:null,Intents=e.IsAlive?e.Monster.NextMove.Intents.Select(i=>new{Type=i.GetType().Name,
                    Damage=i is AttackIntent a?(int?)a.GetSingleDamage(r.Players.Select(v=>v.Creature),e):null,Repeats=i is AttackIntent b?(int?)b.Repeats:null}).ToArray():null}).ToArray()};
        if(Format<3)return JsonSerializer.Serialize(snapshot,Values);
        var node=JsonSerializer.SerializeToNode(snapshot,Values)!.AsObject();node["Mechanics"]=JsonSerializer.SerializeToNode(Mechanics(),Values);
        if(Format>=4)node["ChoicesConsumed"]=ChoiceTape.Position;
        return node.ToJsonString(Values);
    }
    // Initial choice UI may precede monster intent assignment. Browsing checks must not require a playable room.
    internal static string ChoiceProjection()=>Fingerprint(new{RunRng=Run.Rng.ToSerializable(),PlayerRng=Player.PlayerRng.ToSerializable(),
        Mechanics=Mechanics(),Deck=Player.Deck.Cards.Select(CardDetails).ToArray(),RoomPreFinished=Run.CurrentRoom!.IsPreFinished,
        Phase=Player.PlayerCombatState!.Phase.ToString(),CombatManager.Instance.IsPaused,CombatManager.Instance.IsInProgress,
        Creatures=Player.Creature.CombatState!.Creatures.Select(c=>new{c.CombatId,c.CurrentHp,c.Block,
            Powers=c.Powers.Select(p=>new{Id=p.Id.ToString(),p.Amount}).ToArray(),Rng=c.Monster?.Rng.ToSerializable()}).ToArray()});
}
