using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Perf001Candidate.Core;

namespace Perf001Candidate;

internal sealed class LiveView:ILiveView
{
    internal RunState? FreshState;internal GameAction? PendingAction,PendingMapMove;internal CardModel? PendingCard;internal int PendingHandIndex;
    internal MapCoord? Target;private string? _neow;
    private static readonly FieldInfo Cost=AccessTools.Field(typeof(CardModel),"_energyCost");
    private static readonly FieldInfo Variables=AccessTools.Field(typeof(CardModel),"_dynamicVars");
    internal RunState State=>RunManager.Instance.DebugOnlyGetState()??throw new InvalidDataException("live-run-missing");
    internal Player Player=>State.Players.Count==1?State.Players[0]:throw new InvalidDataException("single-player-missing");
    internal LightState Light()
    {
        var manager=RunManager.Instance;var state=manager.DebugOnlyGetState();var combat=CombatManager.Instance;
        var player=state?.Players.Count==1?state.Players[0]:null;var pcs=player?.PlayerCombatState;
        var map=NMapScreen.Instance;
        var mapReady=state is not null&&ReferenceEquals(state,FreshState)&&map?.IsVisibleInTree()==true&&!map.IsTraveling&&
            state.CurrentActIndex==0&&(state.CurrentRoom is MapRoom||state.CurrentRoom is EventRoom e&&e.CanonicalEvent.Id.ToString()=="EVENT.NEOW"&&e.LocalMutableEvent.IsFinished);
        var executor=manager.ActionExecutor;
        var playable=state?.CurrentRoom is CombatRoom room&&room.RoomType==RoomType.Monster&&pcs is not null&&
            combat.IsInProgress&&!combat.IsOverOrEnding&&!combat.IsPaused&&!combat.PlayerActionsDisabled&&
            pcs.Phase==PlayerTurnPhase.Play&&combat.IsPartOfPlayerTurn(player!)&&
            executor is not null&&!executor.IsRunning&&!executor.IsPaused&&executor.CurrentlyRunningAction is null;
        return new(DisplayServer.WindowIsFocused(),manager.IsPaused,mapReady,playable,pcs?.TurnNumber??0,
            combat.IsInProgress&&combat.DebugOnlyGetState()?.CurrentSide==CombatSide.Player);
    }
    internal bool IsTarget(MapCoord coordinate)
    {
        if(Target is null)return false;
        return Target.Value.Equals(coordinate)&&State.Map.GetPoint(coordinate)?.PointType==MapPointType.Monster;
    }
    internal bool IsInitialStartingCoordinate(MapCoord coordinate)
        =>ReferenceEquals(State,FreshState)&&State.CurrentActIndex==0&&State.Map.StartingMapPoint.coord.Equals(coordinate);
    internal bool IsExpectedMapQueue(GameAction action)
    {
        if(action.OwnerId!=Player.NetId)return false;
        var destination=AccessTools.Field(action.GetType(),"_destination").GetValue(action);
        return destination is MapVote vote?IsTarget(vote.coord):destination is MapCoord coord&&IsTarget(coord);
    }
    internal bool IsExpectedCard(CardModel card,bool auto)
    {
        if(auto||PendingAction is not PlayCardAction action||!ReferenceEquals(action.Player,Player)||
            !ReferenceEquals(action.NetCombatCard.ToCardModelOrNull(),card)||action.TargetId is not null)return false;
        var index=Player.PlayerCombatState!.Hand.Cards.ToList().FindIndex(c=>ReferenceEquals(c,card));
        if(index<0||card.Id.ToString()!="CARD.DEFEND_IRONCLAD")return false;
        PendingCard=card;PendingHandIndex=index;return true;
    }
    public Payload Capture(EventKind kind)
    {
        var player=Player;var pcs=player.PlayerCombatState;
        return new(pcs?.TurnNumber??0,"player:0",kind is EventKind.CardAccepted or EventKind.CardResolved?PendingCard?.Id.ToString():null,
            kind is EventKind.CardAccepted or EventKind.CardResolved?PendingHandIndex:null,[],player.Creature.CurrentHp,player.Creature.Block,
            pcs?.Energy??0,pcs?.Hand.Cards.Select(c=>c.Id.ToString()).ToArray()??[],pcs?.DrawPile.Cards.Count??0,
            pcs?.DiscardPile.Cards.Count??0,pcs?.ExhaustPile.Cards.Count??0);
    }
    private static readonly JsonSerializerOptions ValueJson=LocalJson.ValueOptions(JsonSerializationUtility.Options);
    private static string Value<T>(T value)=>JsonSerializer.Serialize(value,ValueJson);
    private static string Card(CardModel card)
    {
        // Read initialized caches only. Do not call lazy EnergyCost/DynamicVars getters or construct production objects.
        var cost=(CardEnergyCost?)Cost.GetValue(card);var vars=(DynamicVarSet?)Variables.GetValue(card);
        return Value(new{Id=card.Id.ToString(),card.CurrentUpgradeLevel,Persistent=card.ToSerializable(),
            CostCacheInitialized=cost is not null,Cost=cost is null?(int?)null:cost.GetWithModifiers(CostModifiers.All),
            CostsX=cost?.CostsX,card.CurrentStarCost,card.BaseReplayCount,card.ExhaustOnNextPlay,
            VariablesCacheInitialized=vars is not null,Variables=vars?.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new{x.Key,x.Value.BaseValue,x.Value.EnchantedValue}).ToArray(),
            Affliction=card.Affliction?.Id.ToString(),AfflictionAmount=card.Affliction?.Amount});
    }
    public SceneFacts Qualify(string point)
    {
        Probe.CheckPublishedIdentity();
        var state=State;Identity.Require(ReferenceEquals(state,FreshState),"fresh-run-object-changed");var player=Player;var pcs=player.PlayerCombatState;
        var coord=state.CurrentMapCoord;var before=GuardValues();
        if(point=="map")
        {
            Identity.Require(Light().MapVisible,"neow-finished-visible-map");
            var start=state.CurrentMapPoint??state.Map.StartingMapPoint;
            var target=start.Children.Where(x=>x.PointType==MapPointType.Monster).OrderBy(x=>x.coord.col).FirstOrDefault();
            Identity.Require(target is not null,"direct-first-row-ordinary-monster-missing");Target=target!.coord;
            _neow=string.Join("|",player.Relics.Select(r=>r.Id.ToString()).Where(id=>id!="RELIC.BURNING_BLOOD").Order(StringComparer.Ordinal));
            if(_neow.Length==0&&state.CurrentRoom is MapRoom)_neow="NO_NEOW_CUSTOM_MAP";
        }
        var combat=state.CurrentRoom as CombatRoom;
        string[] Pile(CardPile? pile)=>pile?.Cards.Select(Card).ToArray()??[];
        var powers=player.Creature.Powers.Select(p=>new{Id=p.Id.ToString(),p.Amount}).ToArray();
        var playerValue=Value(new{player.Creature.CurrentHp,player.Creature.MaxHp,player.Creature.Block,player.Gold,
            Energy=pcs?.Energy,Stars=pcs?.Stars,Turn=pcs?.TurnNumber,Powers=powers,
            ShuffleFtueSeen=SaveManager.Instance.SeenFtue("shuffle_ftue"),
            Potions=player.PotionSlots.Select((p,i)=>p is null?"Empty":Value(p.ToSerializable(i))).ToArray(),
            TargetRow=Target?.row,TargetCol=Target?.col,DebugForcedCard=CombatManager.Instance.DebugForcedTopCardOnNextShuffle?.Id.ToString()});
        var enemies=combat?.CombatState.Enemies.Select(e=>
        {
            var monster=e.Monster??throw new InvalidDataException("enemy-model-missing");
            return Value(new{Id=monster.Id.ToString(),e.CombatId,e.CurrentHp,e.MaxHp,e.Block,
                Powers=e.Powers.Select(p=>new{Id=p.Id.ToString(),p.Amount}).ToArray(),
                Intents=monster.NextMove.Intents.Select(i=>new{Type=i.GetType().FullName,
                    SingleDamage=i is AttackIntent attack?(int?)attack.GetSingleDamage(state.Players.Select(p=>p.Creature),e):null,
                    Repeats=i is AttackIntent repeats?(int?)repeats.Repeats:null}).ToArray(),MoveId=monster.NextMove.Id,
                Rng=monster.Rng.ToSerializable()});
        }).ToArray()??[];
        var facts=new SceneFacts(point,state.GameMode.ToString(),state.Rng.ToSerializable().Seed??throw new InvalidDataException("live-seed-missing"),player.Character.Id.ToString(),state.AscensionLevel,state.CurrentActIndex,
            coord?.row??-1,coord?.col??-1,state.CurrentRoom?.RoomType.ToString()??"None",combat?.CombatState.Encounter?.Id.ToString()??"",
            _neow??throw new InvalidDataException("neow-selection-not-qualified"),state.Modifiers.Select(m=>m.Id.ToString()).ToArray(),
            player.Relics.Select(r=>Value(r.ToSerializable())).ToArray(),Pile(player.Deck),Pile(pcs?.Hand),Pile(pcs?.DrawPile),Pile(pcs?.DiscardPile),Pile(pcs?.ExhaustPile),playerValue,enemies);
        Identity.Require(before==GuardValues(),"qualification-query-changed-live-guard");return facts;
    }
    private string GuardValues()
    {
        var state=State;var p=Player;var pcs=p.PlayerCombatState;
        return Value(new{RunRng=state.Rng.ToSerializable(),PlayerRng=p.PlayerRng.ToSerializable(),p.Creature.CurrentHp,p.Creature.Block,p.Gold,
            Energy=pcs?.Energy,Stars=pcs?.Stars,Deck=p.Deck.Cards.Select(c=>c.Id.ToString()).ToArray(),
            Piles=pcs is null?[]:new[]{pcs.Hand,pcs.DrawPile,pcs.DiscardPile,pcs.ExhaustPile}.Select(pile=>pile.Cards.Select(c=>c.Id.ToString()).ToArray()).ToArray(),
            Enemies=(state.CurrentRoom as CombatRoom)?.CombatState.Enemies.Select(e=>new{e.CurrentHp,e.Block,
                Powers=e.Powers.Select(x=>new{Id=x.Id.ToString(),x.Amount}).ToArray(),Rng=e.Monster!.Rng.ToSerializable()}).ToArray()});
    }
}
