using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class Patches
{
    internal static void Install(Harmony h)
    {
        void Patch(Type type,string method,string? prefix=null,string? postfix=null,Type[]? parameters=null)
        {
            var target=AccessTools.DeclaredMethod(type,method,parameters)??throw new MissingMethodException(type.FullName,method);
            HarmonyMethod? Handler(string? name)=>name is null?null:new(AccessTools.Method(typeof(Patches),name));
            h.Patch(target,Handler(prefix),Handler(postfix));
        }
        Patch(typeof(NMainMenu),"_Ready",postfix:nameof(Menu),parameters:[]);
        Patch(typeof(RunManager),"SetUpNewSingleplayer",prefix:nameof(FreshBefore),postfix:nameof(Fresh));
        Patch(typeof(RunManager),"SetUpSavedSingleplayer",prefix:nameof(Saved));
        Patch(typeof(RunManager),"ToSave",postfix:nameof(Checkpoint));
        Patch(typeof(RunManager),"EnterMapCoord",postfix:nameof(Entry));
        Patch(typeof(ActionQueueSynchronizer),"RequestEnqueue",prefix:nameof(Queue));
        Patch(typeof(PlayCardAction),"ExecuteAction",postfix:nameof(ActionTask),parameters:[]);
        Patch(typeof(EndPlayerTurnAction),"ExecuteAction",postfix:nameof(ActionTask),parameters:[]);
        Patch(typeof(UsePotionAction),"ExecuteAction",postfix:nameof(ActionTask),parameters:[]);
        Patch(typeof(DiscardPotionGameAction),"ExecuteAction",postfix:nameof(ActionTask),parameters:[]);
        Patch(typeof(CardModel),"OnPlayWrapper",prefix:nameof(CardBefore),postfix:nameof(CardTask));
        Patch(typeof(PotionModel),"OnUseWrapper",postfix:nameof(PotionTask));
        Patch(typeof(PotionModel),"EnqueueManualUse",prefix:nameof(ManualPotion));
        Patch(typeof(PlayerChoiceSynchronizer),"ReserveChoiceId",prefix:nameof(Choice));
        Patch(typeof(CombatState),"CreateCreature",prefix:nameof(Spawn));
        Patch(typeof(OrbQueue),"TryEnqueue",prefix:nameof(OrbAdmitted));
        Patch(typeof(CardPileCmd),"AddGeneratedCardsToCombat",prefix:nameof(GeneratedCards),postfix:nameof(GeneratedAfter));
        Patch(typeof(ForgeCmd),"Forge",postfix:nameof(ForgedAfter));
        Patch(typeof(DivineRight),"AfterRoomEntered",postfix:nameof(DivineAfter));
        Patch(typeof(NCombatCardPile),"OnRelease",postfix:nameof(PileBrowsed));
        NativeDisplayTrace.Install(h);
        NativePhaseRetirement.Install(h);
        DisplayAssets.Install(h);
        var transform=typeof(CardCmd).GetMethods().Single(m=>m.Name=="Transform"&&m.ReturnType==typeof(Task<IEnumerable<CardPileAddResult>>));
        h.Patch(transform,postfix:new HarmonyMethod(typeof(Patches),nameof(TransformedAfter)));
        Patch(typeof(Player),"AddRelicInternal",prefix:nameof(RelicMembership));
        Patch(typeof(Player),"RemoveRelicInternal",prefix:nameof(RelicMembership));
        Patch(typeof(HappyFlower),"DoActivateVisuals",prefix:nameof(FlowerVisuals));
        Patch(typeof(JossPaper),"DoActivateVisuals",prefix:nameof(PaperVisuals));
        Patch(typeof(CardModel),"TryManualPlay",prefix:nameof(Manual));
        ChoicePatches.Install(h);
        Patch(typeof(CombatManager),"StartTurn",prefix:nameof(TurnBefore),postfix:nameof(TurnAfter));
        Patch(typeof(NCombatStartBanner),"_Ready",postfix:nameof(BannerReady),parameters:[]);
        Patch(typeof(NPlayerTurnBanner),"_Ready",postfix:nameof(BannerReady),parameters:[]);
        Patch(typeof(NPlayerTurnBanner),"Create",prefix:nameof(TurnBanner),parameters:[typeof(int)]);
        Patch(typeof(NEnemyTurnBanner),"Create",prefix:nameof(EnemyBanner),parameters:[]);
        Patch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom),"OnCombatSetUp",postfix:nameof(OpeningRoomVisible),parameters:[typeof(CombatState)]);
        Patch(typeof(NCreature),"StartReviveAnim",postfix:nameof(OstyReviveHud),parameters:[]);
        var turnState=AccessTools.TypeByName("MegaCrit.Sts2.Core.Combat.CombatTurnState")??throw new MissingMemberException("CombatTurnState");
        Patch(typeof(CombatManager),"EndCombatInternal",prefix:nameof(EndBefore),postfix:nameof(EndTask),parameters:[turnState]);
        Patch(typeof(NCombatUi),"OnCombatWon",prefix:nameof(RewardUi),parameters:[typeof(MegaCrit.Sts2.Core.Rooms.CombatRoom)]);
        Patch(typeof(SaveManager),"UpdateProgressAfterCombatWon",prefix:nameof(ProgressOwner),postfix:nameof(ProgressWon));
        Patch(typeof(SaveManager),"SaveProgressFile",prefix:nameof(ProgressOwner),parameters:[]);
        Patch(typeof(GameAction),"Cancel",prefix:nameof(Cancel),postfix:nameof(CancelSettled),parameters:[]);
        Patch(typeof(ActionQueueSet),"GetReadyAction",postfix:nameof(ReadyAction));
        Patch(typeof(GameAction),"Execute",prefix:nameof(ActionStarted),parameters:[]);
        Patch(typeof(RunManager),"CleanUp",prefix:nameof(Cleanup));
        Patch(typeof(NGame),"ReturnToMainMenu",prefix:nameof(Return),parameters:[]);
        Patch(typeof(NHotkeyManager),"_UnhandledInput",prefix:nameof(Hotkey));
        CombatManager.Instance.CombatWon+=room=>Runtime.Guard(()=>BattleEnd.Won(room));
    }
    private static void Menu(NMainMenu __instance)=>Runtime.Menu(__instance);
    private static void FreshBefore(RunState __0)=>Runtime.Guard(()=>TestEncounter.BeforeNew(__0));
    private static void Fresh()=>Runtime.Guard(()=>{Recorder.NewRun();TestEncounter.AfterNew();});
    private static void Saved(){if(!PlayerLoop.Active)Runtime.Guard(()=>Recorder.NewRun(false));}
    private static void Checkpoint(object? __0,SerializableRun __result){if(__0 is null&&!PlayerLoop.Active)Runtime.Guard(()=>Recorder.Checkpoint(__result));}
    private static void Entry(MapCoord __0,Task __result){if(!PlayerLoop.Active)Runtime.Guard(()=>Recorder.Entry(__0,__result));}
    private static bool Queue(GameAction __0)
    {
        if(DecisionRoute.Enabled&&DecisionRoute.Historical&&__0 is not (PlayCardAction or EndPlayerTurnAction or UsePotionAction or DiscardPotionGameAction))
        {
            Runtime.Guard(()=>throw new InvalidDataException("route-business-action-during-history-"+__0.GetType().Name));return false;
        }
        if(PlayerLoop.Active&&!PlayerLoop.Dispatching)
        {
            if(__0 is GenericHookGameAction or ReadyToBeginEnemyTurnAction)return true;
            if(__0 is PlayCardAction or EndPlayerTurnAction or UsePotionAction or DiscardPotionGameAction)
            {Runtime.Log("spectator-decision-blocked",new{Action=__0.GetType().Name,ChoiceUnchanged=true});return false;}
            Runtime.Guard(()=>throw new InvalidDataException("incomplete-unexpected-replay-action-"+__0.GetType().Name));return false;
        }
        if(!Recorder.Active&&!PlayerLoop.Active)return true;
        if(__0 is not (PlayCardAction or EndPlayerTurnAction or UsePotionAction or DiscardPotionGameAction))
        {
            if(__0 is not (GenericHookGameAction or ReadyToBeginEnemyTurnAction))Runtime.Guard(()=>throw new InvalidDataException("unsupported-action-"+__0.GetType().Name));
            return true;
        }
        Runtime.Guard(()=>ActionMonitor.Queue(__0));return true;
    }
    private static void ActionTask(GameAction __instance,Task __result)=>Runtime.Guard(()=>ActionMonitor.ActionTask(__instance,__result));
    private static void ActionStarted(GameAction __instance)=>Runtime.Guard(()=>ActionMonitor.Started(__instance));
    private static void CardTask(CardModel __instance,bool isAutoPlay,Task __result)=>Runtime.Guard(()=>ActionMonitor.CardTask(__instance,isAutoPlay,__result));
    private static bool Observing=>Recorder.Active||PlayerLoop.Active&&!PlayerLoop.Loading&&!PlayerLoop.TearingDown;
    private static void CardBefore(CardModel __instance,bool isAutoPlay)
    {
        if(Observing&&LiveState.Format<4)Runtime.Guard(()=>Codec.Require(!isAutoPlay&&ReferenceEquals(__instance,ActionMonitor.Pending?.Card),"incomplete-unsupported-autoplay-chain"));
        else if(Observing&&isAutoPlay)Runtime.Log("derived-auto-card",new{Model=__instance.Id.ToString(),OwnerOperation=ActionMonitor.Pending?.Template.Kind.ToString(),RecordedAsManualInput=false});
    }
    private static void PotionTask(PotionModel __instance,Task __result){if(Observing)Runtime.Guard(()=>ActionMonitor.PotionTask(__instance,__result));}
    private static bool ManualPotion()=>!PlayerLoop.Active||PlayerLoop.Dispatching;
    private static void Choice(){if(Observing&&LiveState.Format<4)Runtime.Guard(()=>throw new InvalidDataException("incomplete-unsupported-player-choice"));}
    private static void Spawn(CombatState __instance,MonsterModel __0,CombatSide __1)
    {
        if(Observing)Runtime.Guard(()=>Codec.Require(LiveState.Format>=8&&__0 is MegaCrit.Sts2.Core.Models.Monsters.Osty&&
            __1==CombatSide.Player&&ReferenceEquals(__instance,LiveState.Player.Creature.CombatState),"incomplete-unsupported-creature-generation"));
    }
    private static void OrbAdmitted(OrbQueue __instance,OrbModel __0)=>Runtime.Guard(()=>OrbState.Admit(__instance,__0));
    private static void GeneratedCards(){if(Observing&&LiveState.Format<4)Runtime.Guard(()=>throw new InvalidDataException("incomplete-unsupported-card-generation"));}
    private static void PileBrowsed(NCombatCardPile __instance)=>Runtime.Guard(()=>RoutePresentation.Browsed(__instance));
    private static bool ContentObserving=>LiveState.Format>=4&&(Recorder.CollectingChoices||PlayerLoop.Active&&!PlayerLoop.TearingDown);
    private static void ForgedAfter(ref Task<IEnumerable<MegaCrit.Sts2.Core.Models.Cards.SovereignBlade>> __result)
    {if(ContentObserving)__result=ObserveForge(__result);}
    private static async Task<IEnumerable<MegaCrit.Sts2.Core.Models.Cards.SovereignBlade>> ObserveForge(Task<IEnumerable<MegaCrit.Sts2.Core.Models.Cards.SovereignBlade>> task)
    {
        var result=await task;Runtime.Guard(()=>Runtime.Log("native-forge-completed",new{Mode=PlayerLoop.Active?"playback":"recording",
            Blades=LiveState.Player.PlayerCombatState!.AllCards.OfType<MegaCrit.Sts2.Core.Models.Cards.SovereignBlade>().Select(c=>new{
                CombatIndex=NetCombatCardDb.Instance.TryGetCardId(c,out var id)?(uint?)id:null,c.IsDupe,c.CreatedThroughForge,Pile=c.Pile?.Type.ToString(),
                Damage=c.DynamicVars.Damage.BaseValue,Repeats=c.DynamicVars.Repeat.BaseValue,Energy=c.EnergyCost.GetWithModifiers(MegaCrit.Sts2.Core.Entities.Cards.CostModifiers.Local)}).ToArray(),
            BusinessExecutedByOriginalForgeCmd=true}));return result;
    }
    private static void DivineAfter(DivineRight __instance,ref Task __result)
    {if(ContentObserving)__result=ObserveDivine(__instance,__result);}
    private static async Task ObserveDivine(DivineRight relic,Task task)
    {
        await task;if(relic.Owner.PlayerCombatState is not null)Runtime.Guard(()=>Runtime.Log("native-resource-relic-completed",new{
            Mode=PlayerLoop.Active?"playback":"recording",Model=relic.Id.ToString(),Stars=relic.Owner.PlayerCombatState.Stars,OriginalHookCompleted=true}));
    }
    private static void GeneratedAfter(ref Task<IReadOnlyList<CardPileAddResult>> __result){if(ContentObserving)__result=ObserveGenerated(__result);}
    private static async Task<IReadOnlyList<CardPileAddResult>> ObserveGenerated(Task<IReadOnlyList<CardPileAddResult>> task)
    {
        var results=await task;Runtime.Guard(()=>LogAdded("card-generation-completed",results));return results;
    }
    private static void TransformedAfter(ref Task<IEnumerable<CardPileAddResult>> __result){if(ContentObserving)__result=ObserveTransformed(__result);}
    private static async Task<IEnumerable<CardPileAddResult>> ObserveTransformed(Task<IEnumerable<CardPileAddResult>> task)
    {
        var results=(await task).ToArray();Runtime.Guard(()=>LogAdded("card-transformation-completed",results));return results;
    }
    private static void LogAdded(string kind,IEnumerable<CardPileAddResult> results)=>Runtime.Log(kind,new{Mode=PlayerLoop.Active?"playback":"recording",Initial=PlayerLoop.Loading||!Recorder.Active&&!PlayerLoop.Active,
        Cards=results.Where(r=>r.cardAdded is not null).Select(r=>new{r.success,Model=r.cardAdded.Id.ToString(),r.cardAdded.CurrentUpgradeLevel,
            CombatIndex=NetCombatCardDb.Instance.TryGetCardId(r.cardAdded,out var id)?(uint?)id:null,
            TargetPile=r.targetPile.ToString(),ActualPile=r.cardAdded.Pile?.Type.ToString(),Fingerprint=LiveState.Fingerprint(LiveState.CardDetails(r.cardAdded))}).ToArray(),
        NativeFactoriesAndEffectsUnchanged=true});
    private static void RelicMembership(){if(Observing)Runtime.Guard(()=>throw new InvalidDataException("incomplete-unsupported-relic-membership-change"));}
    private static bool FlowerVisuals(HappyFlower __instance,ref Task __result)
    {
        if(!PlayerLoop.Active)return true;
        // This native task ONLY flashes and temporarily replaces the counter with 3 for one second.
        // Keep the real AfterSideTurnStart/GainEnergy, but no old delayed cosmetic setter across restores.
        __instance.Flash();__result=Task.CompletedTask;
        Runtime.Log("playback-relic-visual",new{Model=__instance.Id.ToString(),__instance.TurnsSeen,DelayedCounterOverride=false,NativeEnergyHookUnaffected=true});return false;
    }
    private static bool PaperVisuals(JossPaper __instance,ref Task __result)
    {
        if(!PlayerLoop.Active)return true;
        // The actual exhaust threshold/draw/reset hooks remain native; retire only this delayed display flag.
        AccessTools.Property(typeof(JossPaper),"IsActivating").SetValue(__instance,false);__instance.Flash();__result=Task.CompletedTask;
        Runtime.Log("playback-relic-visual",new{Model=__instance.Id.ToString(),__instance.CardsExhausted,DelayedCounterOverride=false,NativeExhaustAndDrawUnaffected=true});return false;
    }
    private static bool Manual(ref bool __result){if(!PlayerLoop.Active||PlayerLoop.Dispatching)return true;__result=false;return false;}
    private static void TurnBefore(object __0,out bool __state)
    {
        __state=AccessTools.Property(__0.GetType(),"State").GetValue(__0) is CombatState state&&
            ReferenceEquals(state,CombatManager.Instance.DebugOnlyGetState())&&state.CurrentSide==CombatSide.Player;
    }
    private static void TurnAfter(bool __state,Task __result){if(__state){ActionMonitor.StartTurnTask=__result;ActionMonitor.Generation++;}}
    private static void BannerReady(Godot.Control __instance)=>Runtime.Guard(()=>NativeOpening.BannerReady(__instance));
    private static bool TurnBanner(int __0,ref NPlayerTurnBanner? __result)=>NativeOpening.TurnBanner(__0,ref __result);
    private static bool EnemyBanner(ref NEnemyTurnBanner? __result)
    {if(!NativeSeek.Enabled||!NativePhaseRetirement.Active)return true;__result=null;return false;}
    private static void OpeningRoomVisible(MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom __instance,CombatState __0)=>Runtime.Guard(()=>NativeSeek.OpeningRoomVisible(__instance,__0));
    private static void OstyReviveHud(NCreature __instance)=>Runtime.Guard(()=>CreaturePresentation.VerifyNativeOstyRevive(__instance));
    private static void EndBefore(){if(Recorder.Active||PlayerLoop.Active)Runtime.Guard(BattleEnd.EndStarted);}
    private static void EndTask(Task __result){if(Recorder.Active||PlayerLoop.Active)Runtime.Guard(()=>BattleEnd.Track(__result));}
    private static bool RewardUi()
    {
        if(!PlayerLoop.Active)return true;
        Runtime.Log("playback-rewards-blocked",new{Reason="victory-is-replay-endpoint",NormalRecordingUnaffected=true});return false;
    }
    private static void ProgressOwner(SaveManager __instance)
    {
        // Abort before a write if the victory chain ever escaped the isolated manager.
        if(PlayerLoop.Active&&BattleEnd.Ending)Codec.Require(ReferenceEquals(__instance,PlayerLoop.IsolatedManager),"victory-progress-outside-memory-context");
    }
    private static void ProgressWon(SaveManager __instance)
    {
        if(PlayerLoop.Active&&BattleEnd.Ending)Runtime.Log("playback-victory-progress",new{IsolatedManager=ReferenceEquals(__instance,PlayerLoop.IsolatedManager),NativeVictoryUpdateExecuted=true});
    }
    private static void Cancel(GameAction __instance)
    {
        if(NativeSeek.Enabled){Runtime.Guard(()=>ActionMonitor.Cancelled(__instance));return;}
        if(!PlayerLoop.ChoiceAbort&&ReferenceEquals(__instance,ActionMonitor.Pending?.Action))Runtime.Guard(()=>throw new InvalidDataException("normal-operation-cancelled"));
    }
    private static void CancelSettled(GameAction __instance){if(NativeSeek.Enabled)Runtime.Guard(()=>NativeSeek.CancelSettled(__instance));}
    private static void ReadyAction(ref GameAction? __result)
    {
        if(!NativeSeek.Enabled)return;
        var candidate=__result;GameAction? filtered=null;Runtime.Guard(()=>filtered=NativeSeek.FilterReady(candidate));__result=filtered;
    }
    private static bool Cleanup()
    {
        if(PlayerLoop.Active&&!PlayerLoop.TearingDown&&!PlayerLoop.CleaningRoom){PlayerLoop.Control("stop");return false;}
        if(!PlayerLoop.Active)Runtime.Guard(Recorder.Seal);return true;
    }
    private static bool Return(ref Task __result)
    {
        if(!PlayerLoop.Active||PlayerLoop.TearingDown)return true;PlayerLoop.Control("stop");__result=Task.CompletedTask;return false;
    }
    private static bool Hotkey()=>!PlayerLoop.Active||(DecisionRoute.Enabled?DecisionRoute.Ready:ChoiceTape.Controllable);
}
