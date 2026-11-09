using System.Collections;
using System.Reflection;
using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal sealed class DirectCheckpoint
{
    private readonly object _run=LiveState.Run;
    private readonly CombatState _combat=((CombatRoom)LiveState.Run.CurrentRoom!).CombatState;
    private readonly object _turnState=Get(CombatManager.Instance,"_turnState")!;
    private readonly IdentityMemento? _data;
    private readonly Action? _restoreDiscoveries;
    private readonly CombatRoster _roster;
    private readonly Creature[] _creatures;
    private readonly Creature[] _escaped;
    private readonly CombatUnitSubscriptions.ModelSubscription[] _modelSubscriptions;
    private readonly (Player Player,Reward[] Rewards)[] _rewardLists;
    private readonly CreaturePresentation[] _presentation;
    internal CombatMusic.Point? Music {get;}
    private readonly CombatLoops.Point? _loops;
    private readonly EnemyScenePresentation.CrabBackground? _crabBackground;
    private readonly RegentPresentation _regent=new();
    private readonly CombatHudTrace.Sample _combatHud=CombatHudTrace.Capture();
    private readonly object? _endTurnUiState=Get(NCombatRoom.Instance!.Ui.EndTurnButton,"_state");
    internal readonly int Position;
    internal readonly int Turn=LiveState.Turn;
    internal readonly string Projection;
    internal bool SameRoster=>_creatures.SequenceEqual(_combat.Creatures);
    private readonly bool _route;
    private readonly AbstractModel[] _extraModels;
    private readonly string? _extraProjection;
    private readonly PotionModel?[] _potions=LiveState.Player.PotionSlots.ToArray();
    private readonly int _choicePosition=ChoiceTape.Position;

    internal DirectCheckpoint(int position,CombatRoster roster,bool captureData=true,string? projection=null,bool route=false,IEnumerable<object>? extraRoots=null)
    {
        Position=position;_roster=roster;_route=route;Projection=projection??(route?LiveState.ChoiceProjection():LiveState.Capture());
        _extraModels=(extraRoots??[]).OfType<AbstractModel>().Where(v=>v.IsMutable).ToArray();_extraProjection=route?ExtraProjection():null;
        if(!route)RequireReady();LiveState.Supported();
        _creatures=_combat.Creatures.ToArray();
        if(roster.Reason(_combat) is {} reason)throw new InvalidDataException("direct-back-roster-unsupported: "+reason);
        Codec.Require(_creatures.All(CombatUnits.Restorable)&&LiveState.Player.PlayerCombatState!.Pets.All(CombatUnits.IsOsty),"direct-back-unsupported-death-or-pet");
        _escaped=_combat.EscapedCreatures.ToArray();
        _modelSubscriptions=LiveState.Format>=10?CombatUnitSubscriptions.CaptureModels(_creatures):[];
        _rewardLists=route?((CombatRoom)LiveState.Run.CurrentRoom!).ExtraRewards.Select(pair=>(pair.Key,pair.Value.ToArray())).ToArray():[];
        _presentation=_creatures.Select(c=>new CreaturePresentation(c)).ToArray();
        if(NativeSeek.Enabled)
        {
            Music=CombatMusic.Capture(_combat);
            _loops=CombatLoops.Capture(_combat,NCombatRoom.Instance!);
            _crabBackground=EnemyScenePresentation.CrabBackground.Capture(NCombatRoom.Instance!);
        }
        // Include detached enemies and the native room's pending reward lists.
        if(captureData)_data=new(Roots(roster).Concat(extraRoots??[]),v=>Data(v)||route&&v is AbstractRoom,
            f=>Field(f)&&(!route||f.FieldType.Namespace?.StartsWith("MegaCrit.Sts2.Core.GameActions",StringComparison.Ordinal)!=true),Special);
        if(captureData&&PlayerLoop.Active)_restoreDiscoveries=MemorySession.CaptureDiscoveryContext();
    }

    internal static IEnumerable<object> Roots(CombatRoster roster)
    {
        var run=LiveState.Run;
        return new object?[]{((CombatRoom)run.CurrentRoom!).CombatState,LiveState.Player,CombatManager.Instance,NetCombatCardDb.Instance,run.Rng,run.Odds,run.ExtraFields,run.CurrentMapPointHistoryEntry,
            LiveState.Format>=8?OrbState.Identities:null,roster.Identities}.Concat(roster.Anchors).Where(v=>v is not null).Cast<object>();
    }
    internal static bool Data(object value)
    {
        if(value is OrbIdentityMap<OrbModel> or CombatIdentityRegistry<Creature>)return true;
        if(value is GodotObject||value is Task||value is CancellationTokenSource)return false;
        if(value is AbstractModel model)return model.IsMutable&&model is not CharacterModel;
        var n=value.GetType().Namespace??"";
        // These are the data roots and their battle-owned children, not RunManager, scenes, network queues or saves.
        return n.StartsWith("MegaCrit.Sts2.Core.Entities.",StringComparison.Ordinal)||
            n.StartsWith("MegaCrit.Sts2.Core.Combat",StringComparison.Ordinal)||
            n.StartsWith("MegaCrit.Sts2.Core.MonsterMoves",StringComparison.Ordinal)||
            n.StartsWith("MegaCrit.Sts2.Core.Localization.DynamicVars",StringComparison.Ordinal)||
            n.StartsWith("MegaCrit.Sts2.Core.Models.",StringComparison.Ordinal)||
            n.StartsWith("MegaCrit.Sts2.Core.Runs.History",StringComparison.Ordinal)||n.StartsWith("MegaCrit.Sts2.Core.Saves",StringComparison.Ordinal)||
            n=="MegaCrit.Sts2.Core.Random"||value is PlayerMapPointHistoryEntry or RunRngSet or NetCombatCardDb||
            value.GetType().Name is "ExtraRunFields" or "RunOddsSet" or "SavedProperties";
    }
    internal static bool Field(FieldInfo f)
    {
        var type=f.FieldType;
        if(typeof(Delegate).IsAssignableFrom(type)||typeof(GodotObject).IsAssignableFrom(type)||
            typeof(Task).IsAssignableFrom(type)||type==typeof(CancellationTokenSource)||
            type.FullName?.StartsWith("System.Threading.",StringComparison.Ordinal)==true)return false;
        // A live turn loop is awaiting the CURRENT pending signals. Never restore a completed old signal/task.
        if(f.DeclaringType?.Name=="CombatTurnState"&&f.Name is "<EndTurnSignalSource>k__BackingField" or "<BeginEnemyTurnSignalSource>k__BackingField" or "<ReadyLock>k__BackingField")return false;
        if(f.DeclaringType==typeof(CombatManager)&&f.Name=="<StateTracker>k__BackingField")return false;
        if(f.DeclaringType==typeof(NetCombatCardDb)&&f.Name=="_subscriptions")return false;
        return true;
    }
    private static Action? Special(object value)
    {
        if(value is not Rng rng)return null;
        var saved=rng.ToSerializable() with{};return ()=>rng.LoadFromSerializable(saved);
    }
    private static object? Get(object value,string name)=>AccessTools.Field(value.GetType(),name).GetValue(value);
    private static void Invoke(object value,string name,params object?[] args)=>AccessTools.Method(value.GetType(),name).Invoke(value,args);

    private static void RequireReady()
    {
        Codec.Require(LiveState.Playable&&ActionMonitor.Pending is null,"direct-back-requires-stable-player-phase");
        var turn=Get(CombatManager.Instance,"_turnState")??throw new InvalidDataException("direct-back-no-turn-loop");
        var signal=AccessTools.Property(turn.GetType(),"EndTurnSignalSource").GetValue(turn);
        var enemySignal=AccessTools.Property(turn.GetType(),"BeginEnemyTurnSignalSource").GetValue(turn);
        Task? SignalTask(object? s)=>s is null?null:(Task?)AccessTools.Property(s.GetType(),"Task").GetValue(s);
        Codec.Require(SignalTask(signal) is {IsCompleted:false}&&SignalTask(enemySignal) is {IsCompleted:false},"direct-back-turn-loop-not-waiting");
    }

    internal bool CanSettleComputedTurn=>SameRoster&&_creatures.SequenceEqual(_roster.Initial)&&
        _creatures.All(c=>c.IsAlive&&c.PetOwner is null&&!c.HasPower<DoomPower>())&&
        _presentation.All(p=>p.SameNodes&&!p.RequiresEnemyDisplayRebuild)&&
        _crabBackground is null&&_combat.EscapedCreatures.Count==0&&LiveState.Player.PlayerCombatState!.OrbQueue.Orbs.Count==0;

    internal async Task<bool> SettleComputedTurn(int sourcePicture)
    {
        Codec.Require(NativeSeek.CanRestore&&CanSettleComputedTurn,"computed-turn-picture-not-stable");
        var room=NCombatRoom.Instance!;
        await NativeDisplayTrace.RetireSourceVisuals(replaceRoomLayers:true);
        EnemyScenePresentation.RetireTweens();
        CombatVisualLayers.Rotate(room,sourcePicture,NativeSeek.Generation);
        foreach(var presentation in _presentation)presentation.Restore();
        foreach(var enemy in _combat.Enemies)
        {
            var node=room.GetCreatureNode(enemy)!;
            if(Get(node,"_intentFadeTween") is Tween fade&&GodotObject.IsInstanceValid(fade))fade.Kill();
            node.IntentContainer.Modulate=Colors.White;
        }
        if(_loops is {} loops)CombatLoops.Restore(loops,room);
        var hand=NPlayerHand.Instance!;
        var cards=LiveState.Player.PlayerCombatState!.Hand.Cards;
        if(hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().Select(holder=>holder.CardModel).SequenceEqual(cards))
            HandPresentation.Snap(hand);
        else HandPresentation.Synchronize(hand,cards,HandPresentation.CaptureAfflictions(hand));
        RoutePresentation.Synchronize();
        CombatHudTrace.RestoreEnergy(_combatHud);
        CombatHudTrace.RestoreStars();
        CombatHudTrace.RefreshTurnCaption();
        HealthDisplayTail.SettleRestored(_creatures);
        CreatureHudTrace.SettleRestoredEnemyPowers(room,_creatures);
        ContentPresentation.SettleRestoredRelicActivations(LiveState.Player.Relics);
        ContentPresentation.RefreshRelics();
        Codec.Require(LiveState.ChoiceProjection()==Projection&&ExtraProjection()==_extraProjection,"computed-turn-state-mismatch");
        try
        {
            var potionsRebuilt=ContentPresentation.SettleComputedPotions(_potions);
            Codec.Require(HandPresentation.Matched(hand,cards)&&_presentation.All(p=>p.Matched)&&
                _combat.Enemies.All(e=>e.Monster?.NextMove is not {} move||
                    room.GetCreatureNode(e)!.IntentContainer.GetChildCount()==move.Intents.Count),
                "computed-turn-display-mismatch");
            Runtime.Log("computed-turn-picture-settled",new{Position,Turn,Display=RoutePresentation.Check(),
                PotionsRebuilt=potionsRebuilt,CombatDataReapplied=false,CreatureNodesRecreated=false});
            return true;
        }
        catch(InvalidDataException ex)
        {
            Runtime.Log("computed-turn-full-restore-needed",new{Position,Turn,Reason=ex.Message});return false;
        }
    }

    internal Task Restore(Action? fileData=null,int? sourcePicture=null)=>RestoreCore(fileData,sourcePicture);
    internal Task<bool> RestoreTurnAnchor(Func<bool> apply,int sourcePicture)=>RestoreCore(null,sourcePicture,apply);

    private async Task<bool> RestoreCore(Action? fileData,int? sourcePicture,Func<bool>? turnAnchor=null)
    {
        var started=Stopwatch.GetTimestamp();
        var fromVictory=NativeSeek.Enabled&&PlayerLoop.VictoryReached;
        if(!_route)RequireReady();else Codec.Require(NativeSeek.Enabled?NativeSeek.CanRestore:DecisionRoute.CanRestore,"route-data-restore-while-business-running");
        if(_route)await NativeDisplayTrace.RetireSourceVisuals(replaceRoomLayers:sourcePicture.HasValue);
        if(NativeSeek.Restoring)EnemyScenePresentation.RetireTweens();
        var retired=Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        Codec.Require(ReferenceEquals(_run,LiveState.Run)&&ReferenceEquals(_combat,((CombatRoom)LiveState.Run.CurrentRoom!).CombatState)&&
            ReferenceEquals(_turnState,Get(CombatManager.Instance,"_turnState")),"direct-back-context-changed");
        Codec.Require(_roster.Reason(_combat,fromVictory) is null,"direct-back-roster-or-visual-unsupported");
        Codec.Require(_presentation.Where((_,i)=>_creatures[i].IsPlayer).All(p=>p.SameNodes),"direct-back-player-visual-replaced");
        var before=_combat.Creatures.ToArray();var beforeSummary=CombatRoster.Summary(before);
        var escapedBefore=_combat.EscapedCreatures.Select(c=>c.CombatId).ToArray();
        var previousNodes=before.ToDictionary(c=>c,c=>NCombatRoom.Instance!.GetCreatureNode(c));
        var forwardHandoff=sourcePicture is null&&_route&&NativeDisplayTrace.Replaying;
        var rebuildEnemies=sourcePicture.HasValue||!_creatures.SequenceEqual(before)||_presentation.Any(p=>p.RequiresEnemyDisplayRebuild);
        var currentPowers=before.ToDictionary(c=>c,c=>c.Powers.ToArray());
        var previousAfflictions=HandPresentation.CaptureAfflictions(NPlayerHand.Instance!);
        // Drain and native-owned visual cleanup have finished. Replace both room
        // VFX layers and the old enemy picture before applying checkpoint data.
        if(sourcePicture is {} generation)CombatVisualLayers.Rotate(NCombatRoom.Instance!,generation,NativeSeek.Generation);
        var removedDisplays=rebuildEnemies?(forwardHandoff?_roster.RemoveForForwardTarget(_creatures):_roster.RemoveEnemyDisplays()):_roster.RemoveRetiredEnemyDisplays(_combat);
        Runtime.Log("checkpoint-display-cleanup",new{Position,RebuildLiveEnemies=rebuildEnemies,Removed=removedDisplays,WaitedForDeathAnimations=false});
        var keptPotions=_route&&ContentPresentation.CanKeepPotions(_potions);
        _roster.RemovePetDisplays();
        if(LiveState.Format>=3&&!keptPotions)ContentPresentation.RetirePotions();
        OrbPresentation.Retire();
        var cards=((Dictionary<uint,CardModel>)Get(NetCombatCardDb.Instance,"_idToCard")!).Values.ToArray();
        foreach(var card in cards)CombatManager.Instance.StateTracker.Unsubscribe(card);
        if(_route)
        {
            CombatManager.Instance.StateTracker.Unsubscribe(LiveState.Player.PlayerCombatState!);
            foreach(var pile in LiveState.Player.PlayerCombatState!.AllPiles)CombatManager.Instance.StateTracker.Unsubscribe(pile);
        }
        // Native removal unsubscribed the dead enemy. Reconcile explicitly without setup/spawn hooks.
        if(LiveState.Format>=10)CombatUnitSubscriptions.Retire(_roster.Anchors);
        else foreach(var creature in before)CombatManager.Instance.StateTracker.Unsubscribe(creature);
        var appliedAnchor=turnAnchor?.Invoke()==true;
        if(fileData is not null)fileData();
        else if(!appliedAnchor)(_data??throw new InvalidDataException("checkpoint-data-missing")).Restore();
        if(fileData is null)_restoreDiscoveries?.Invoke();
        if(sourcePicture.HasValue)ContentPresentation.SettleRestoredRelicActivations(LiveState.Player.Relics);
        var restored=Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        foreach(var removed in before.Except(_creatures))removed.CombatState=null;
        Codec.Require(_creatures.SequenceEqual(_combat.Creatures)&&_creatures.All(CombatUnits.Restorable),"direct-back-restored-membership-mismatch");
        Codec.Require(_creatures.All(c=>ReferenceEquals(c.CombatState,_combat)&&ReferenceEquals(_combat.GetCreature(c.CombatId),c)),"direct-back-restored-target-identity-mismatch");
        if(LiveState.Format>=10)
            Codec.Require(_escaped.SequenceEqual(_combat.EscapedCreatures)&&_roster.MatchesNativeIdentities(_combat),"direct-back-restored-escape-list-or-identity-mismatch");
        if(_route)
        {
            var rewards=((CombatRoom)LiveState.Run.CurrentRoom!).ExtraRewards;
            Codec.Require(rewards.Count==_rewardLists.Length&&_rewardLists.All(p=>rewards.TryGetValue(p.Player,out var actual)&&
                actual.SequenceEqual(p.Rewards)),"direct-back-restored-extra-rewards-mismatch");
        }
        if(LiveState.Format>=10)CombatUnitSubscriptions.Restore(_roster.Anchors,_creatures);
        else foreach(var creature in _creatures)CombatManager.Instance.StateTracker.Subscribe(creature);
        foreach(var card in LiveState.Player.PlayerCombatState!.AllCards.Distinct())CombatManager.Instance.StateTracker.Subscribe(card);
        if(_route)
        {
            CombatManager.Instance.StateTracker.Subscribe(LiveState.Player.PlayerCombatState);
            foreach(var pile in LiveState.Player.PlayerCombatState.AllPiles)CombatManager.Instance.StateTracker.Subscribe(pile);
        }
        CombatCardTracking.RestoreSubscriptions();
        OstySubscriptions.Restore(_roster.Anchors);
        if(LiveState.Format>=3)ContentPresentation.Restore(keptPotions,refreshRelics:!_route);
        OrbPresentation.Restore();
        if(rebuildEnemies)_roster.AddEnemyDisplays(_combat,onlyMissing:forwardHandoff);
        _roster.AddPetDisplays(_combat);
        for(var i=0;i<_creatures.Length;i++)if(!_creatures[i].IsPlayer)_presentation[i].BindEnemyDisplay();
        foreach(var presentation in _presentation)presentation.Restore(_route&&NativeDisplayTrace.Replaying);
        _crabBackground?.Restore(NCombatRoom.Instance!);
        if(_loops is {} loops)CombatLoops.Restore(loops,NCombatRoom.Instance!);
        _regent.Restore(_route&&NativeDisplayTrace.Replaying);
        Invoke(NCombatRoom.Instance!,"UpdateCreatureNavigation");
        var creatures=Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        // The normal turn-loop/synchronizer is still waiting in this same combat. IDs and tasks stay monotonic.
        if(!_route){ActionMonitor.Pending=null;ActionMonitor.Completed=null;ActionMonitor.Error=null;ActionMonitor.StartTurnTask=Task.CompletedTask;ChoiceTape.Restore(_choicePosition);}
        else ChoiceTape.ViewPosition(_choicePosition);
        if(_endTurnUiState is not null)Invoke(NCombatRoom.Instance!.Ui.EndTurnButton,"SetState",_endTurnUiState);
        await RefreshUi(currentPowers,rebuildEnemies,previousNodes,previousAfflictions);if(!_route)RequireReady();
        if(sourcePicture.HasValue)CreatureHudTrace.SettleRestoredEnemyPowers(NCombatRoom.Instance!,_combat.Enemies);
        if(LiveState.Format>=10)CombatUnitSubscriptions.RestoreModels(_roster.Anchors,_modelSubscriptions);
        if(fromVictory)
        {
            using var hud=new CombatHudTrace.View();hud.Prepare();hud.Handoff(_combatHud);
            Runtime.Log("native-victory-hud-restored",new{Position,HandEntrance=false,NativeHudSnapshot=true});
        }
        var ui=Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if(!appliedAnchor)
            Codec.Require((_route?LiveState.ChoiceProjection():LiveState.Capture())==Projection,"direct-back-restored-projection-differs");
        if(_route)Codec.Require(_extraProjection==ExtraProjection(),"route-owned-source-or-candidate-state-mismatch");
        Runtime.Log("checkpoint-roster-restored",new{Position,Before=beforeSummary,After=CombatRoster.Summary(_combat.Creatures),
            EscapedBefore=escapedBefore,EscapedAfter=_escaped.Select(c=>c.CombatId).ToArray(),
            AllOriginalAnchors=CombatRoster.Summary(_roster.Initial),
            OriginalObjectsRetained=_creatures.All(c=>_roster.Initial.Contains(c)&&ReferenceEquals(_combat.GetCreature(c.CombatId),c)),
            VisitedPointObjectsRetained=_creatures.All(c=>ReferenceEquals(_combat.GetCreature(c.CombatId),c)),
            NextNativeIdentity=Get(_combat,"_nextCreatureId"),RegisteredNextIdentity=_roster.Identities.NextIdentity,
            EnemyDisplaysRecreated=rebuildEnemies,StateSubscriptionsReconciled=true,SpawnHooksRun=false,RoomRebuilt=false,PrefixOperations=0});
        if(_route)Runtime.Log("checkpoint-restore-timing",new{Position,RetireMilliseconds=retired,DataMilliseconds=restored-retired,
            CreatureAndContentMilliseconds=creatures-restored,UiMilliseconds=ui-creatures,ValidationAndLogMilliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds-ui,
            TotalMilliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds,PotionsReused=keptPotions});
        return appliedAnchor;
    }
    private string ExtraProjection()=>LiveState.Fingerprint(_extraModels.Select(v=>v switch
    {
        CardModel card=>(object)LiveState.CardDetails(card),
        PotionModel potion=>new{Id=potion.Id.ToString(),potion.IsQueued,potion.HasBeenRemovedFromState},
        PowerModel power=>new{Id=power.Id.ToString(),power.Amount},
        _=>new{Id=v.Id.ToString()}
    }).ToArray());

    internal string? BlockReason()
    {
        if(!LiveState.Playable)return "战斗结束后暂不能后退";
        if(!ReferenceEquals(_run,LiveState.Run)||!ReferenceEquals(_combat,((CombatRoom)LiveState.Run.CurrentRoom!).CombatState))return "回放上下文已变化";
        if(_roster.Reason(_combat) is {} reason)return reason;
        if(_presentation.Where((_,i)=>_creatures[i].IsPlayer).Any(p=>!p.SameNodes))return "形态或模型节点变化暂不能后退";
        return null;
    }

    private async Task RefreshUi(Dictionary<Creature,PowerModel[]> previousPowers,bool rebuiltEnemies,Dictionary<Creature,NCreature?> previousNodes,
        IReadOnlyDictionary<NCard,AfflictionModel?> previousAfflictions)
    {
        var hand=NPlayerHand.Instance??throw new InvalidDataException("direct-back-hand-ui-missing");
        var handChanges=HandPresentation.Synchronize(hand,LiveState.Player.PlayerCombatState!.Hand.Cards,previousAfflictions);
        foreach(var pile in LiveState.Player.PlayerCombatState.AllPiles)pile.InvokeContentsChanged();
        IEnumerable<Node> Nodes(Node root)
        {
            // Display-only cards contain hundreds of controls but none of the game HUD endpoints
            // refreshed below. Never walk the display cache or a card's entire scene per restore.
            if(_route&&(DisplayAssets.IsCacheRoot(root)||root is NCard))yield break;
            // The scene tree can retain non-members during native death/fade cleanup. Their health,
            // powers and intents are not part of this checkpoint's active combat/UI state.
            if(root is NCreature creature&&(!_combat.ContainsCreature(creature.Entity)||
                !ReferenceEquals(NCombatRoom.Instance!.GetCreatureNode(creature.Entity),creature)))yield break;
            yield return root;foreach(var child in root.GetChildren())foreach(var node in Nodes(child))yield return node;
        }
        IEnumerable<Node> RouteTargets()
        {
            var ui=NCombatRoom.Instance!.Ui;var top=NRun.Instance!.GlobalUi.TopBar;
            yield return top.Hp;yield return top.Gold;
            yield return (Node)Get(ui,"_energyCounter")!;yield return (Node)Get(ui,"_starCounter")!;
            foreach(var model in _creatures)
            {
                var creature=NCombatRoom.Instance.GetCreatureNode(model)??throw new InvalidDataException("route-refresh-creature-node-missing");
                yield return creature;
                var display=Get(creature,"_stateDisplay")!;
                yield return (Node)Get(display,"_healthBar")!;yield return (Node)Get(display,"_powerContainer")!;
            }
        }
        foreach(var node in (_route?RouteTargets():Nodes(NRun.Instance!)).ToArray())
        {
            if(node is NHealthBar hp)hp.RefreshValues();
            else if(node is NTopBarHp topHp)Invoke(topHp,"UpdateHealth",0,0);
            else if(node is NTopBarGold gold)Invoke(gold,"UpdateGold");
            else if(node is NEnergyCounter energy){Invoke(energy,"RefreshLabel");CombatHudTrace.RestoreEnergy(_combatHud);}
            else if(node is NStarCounter){CombatHudTrace.RestoreStars();}
            else if(node is NRelic relic&&!_route)Invoke(relic,"Reload");
            else if(node is NPowerContainer container)
            {
                var powers=(IList)Get(container,"_powerNodes")!;
                var owner=(Creature?)Get(container,"_creature");
                var expectedPowers=owner?.Powers.Where(p=>p.IsVisible).ToArray()??[];
                var attached=container.GetChildren().OfType<NPower>();
                foreach(var old in powers.Cast<NPower>().Concat(attached).Distinct()
                    .Where(n=>!powers.Contains(n)||!expectedPowers.Contains(n.Model)).ToArray())
                {powers.Remove(old);old.GetParent()?.RemoveChild(old);old.QueueFree();}
                foreach(var power in expectedPowers)
                {
                    var existing=powers.Cast<NPower>().FirstOrDefault(n=>ReferenceEquals(n.Model,power));
                    if(existing is null)Invoke(container,"Add",power);else Invoke(existing,"Reload");
                }
                // Native order follows the owner's ordered power list, including after a removal undo.
                var ordered=expectedPowers.Select(p=>powers.Cast<NPower>().Single(n=>ReferenceEquals(n.Model,p))).ToArray();
                powers.Clear();foreach(var powerNode in ordered)powers.Add(powerNode);Invoke(container,"UpdatePositions");
            }
            else if(node is NCreature creature)
            {
                if(previousNodes.TryGetValue(creature.Entity,out var previousNode)&&ReferenceEquals(previousNode,creature))
                {
                    Codec.Require(previousPowers.TryGetValue(creature.Entity,out var previous),"direct-back-active-creature-power-anchor-missing");
                    foreach(var old in previous!.Except(creature.Entity.Powers))Invoke(creature,"UnsubscribeFromPower",old);
                    foreach(var power in creature.Entity.Powers.Except(previous!))Invoke(creature,"SubscribeToPower",power);
                }
                if(creature.Entity.Monster?.NextMove is not null)
                {
                    if(Get(creature,"_intentFadeTween") is Tween tween)tween.Kill();
                    await creature.UpdateIntent(_combat.PlayerCreatures);creature.IntentContainer.Modulate=Colors.White;
                }
            }
        }
        Invoke(CombatManager.Instance.StateTracker,"NotifyCombatStateChanged","ReplayMod.DirectRestore");
        await Runtime.NextFrame();
        // Deferred native UI notifications must not restart a hand entrance animation after the restore.
        HandPresentation.Snap(hand);
        if(_route){CombatHudTrace.RefreshTurnCaption();HealthDisplayTail.SettleRestored(_creatures);}
        if(_route){RoutePresentation.Synchronize(continuePresentation:NativeDisplayTrace.Replaying);Runtime.Log("route-restored-display-verified",new{Position,Display=RoutePresentation.Check()});}
        if(LiveState.Format>=3){ContentPresentation.RefreshRelics();Runtime.Log("checkpoint-content-restored",new{Position,Mechanics=_route?LiveState.RestoreSummary():LiveState.Mechanics(),Ui=ContentPresentation.CheckAndSummary(),CardSubscriptionsReconciled=true});}
        var expected=LiveState.Player.PlayerCombatState.Hand.Cards;
        var actual=hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().Select(h=>h.CardModel).ToArray();
        Codec.Require(actual.SequenceEqual(expected),"direct-back-ui-hand-order");
        Codec.Require(HandPresentation.Matched(hand,expected)&&_presentation.All(p=>p.Matched),"direct-back-presentation-mismatch");
        Runtime.Log("checkpoint-presentation-restored",new{Position,Creatures=_presentation.Select(p=>p.Summary()).ToArray(),CreatureScalesMatched=true,
            HandOrderAndLayoutMatched=true,HandEntranceAnimation=false,HandChanges=handChanges});
    }

    internal void LogCapture(double milliseconds)=>Runtime.Log("checkpoint-captured",new{Position,Turn,DataObjects=_data!.Objects,RestoreWrites=_data.Writes,
        ChoicePosition=_choicePosition,Presentation=_presentation.Select(p=>p.Summary()).ToArray(),Milliseconds=milliseconds,PlaybackOnly=true});
}
