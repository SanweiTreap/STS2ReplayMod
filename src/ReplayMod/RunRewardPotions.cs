using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;

namespace ReplayMod;

internal static class RunRewardPotions
{
    internal const string Kind="reward-discard-potion";
    internal const string NonCombatKind=RunStore.NonCombatPotionDiscard;
    private static DiscardPotionGameAction? _action;
    private static RunOperation? _recorded;
    private static int? _drivenSlot;
    private static Task? _task;
    internal static void Clear(){_action=null;_recorded=null;_drivenSlot=null;_task=null;}
    internal static void Install(Harmony h)=>h.Patch(AccessTools.DeclaredMethod(typeof(NPotionPopup),"OnDiscardButtonPressed"),
        prefix:new HarmonyMethod(typeof(RunRewardPotions),nameof(PopupInput)));
    private static bool PopupInput()=>!RunPlayback.Active||!RunPlayback.NonCombat||_drivenSlot is not null&&RunPlayback.Driving;
    private static bool AtBattleRewards=>RunPatches.RewardsScreen is {} screen&&ReferenceEquals(NOverlayStack.Instance?.Peek(),screen)&&
        LiveState.Run.CurrentRoom is CombatRoom;
    private static string? NonCombatContext()=>LiveState.Run.CurrentRoom switch
    {
        MerchantRoom or EventRoom {CanonicalEvent:FakeMerchant}=>"merchant",
        EventRoom {CanonicalEvent:TheArchitect,IsVictoryRoom:true}=>RunStore.FinalEventPotionContext,
        _=>null
    };
    private static bool MatchesContext(RunOperation operation)=>operation.Kind==Kind?AtBattleRewards:
        operation.Kind==NonCombatKind&&operation.Argument.Option is {} context&&context==NonCombatContext();

    internal static bool Queue(DiscardPotionGameAction action)
    {
        if(!RunPlayback.Active&&!RunRecording.Enabled)return true;
        var slot=checked((int)((NetDiscardPotionGameAction)action.ToNetAction()).potionSlotIndex);
        if(RunPlayback.Active)
        {
            if(!RunPlayback.Driving||!RunPlayback.NonCombat||RunPlayback.Current is not {} operation||_drivenSlot!=slot||
                !MatchesContext(operation)||_action is not null||action.OwnerId!=LiveState.Player.NetId)return false;
            _action=action;
            return true;
        }
        if(action.OwnerId!=LiveState.Player.NetId)return true;
        var context=NonCombatContext();
        var kind=AtBattleRewards?Kind:NonCombatKind;
        if(kind==NonCombatKind&&context is null)return true;
        _recorded=RunPatches.Record(kind,()=>RunRecording.Begin(kind,new(RewardIndex:slot,Option:context,
            RewardKey:LiveState.Player.GetPotionAtSlotIndex(slot)?.Id.ToString()??throw new InvalidDataException("run-discard-potion-slot-empty"))));
        if(_recorded is not null)_action=action;
        return true;
    }
    internal static Task Observe(DiscardPotionGameAction action,Task task)
    {
        if(!ReferenceEquals(_action,action))return task;
        if(RunPlayback.Active){_task=task;RunPlayback.Track(task);return task;}
        var operation=_recorded;
        _action=null;_recorded=null;
        return RunRecording.Wrap(task,operation);
    }
    internal static async Task Play(RunOperation operation)
    {
        var slot=operation.Argument.RewardIndex??-1;
        var player=LiveState.Player;
        var potion=slot>=0&&slot<player.PotionSlots.Count?player.PotionSlots[slot]:null;
        Codec.Require(MatchesContext(operation)&&potion?.Id.ToString()==operation.Argument.RewardKey&&potion is not null,"run-discard-potion-identity-mismatch");
        _drivenSlot=slot;
        try
        {
            var popup=OpenPopup(slot,potion!);
            await Runtime.NextFrame();RunPlayback.CheckStop();
            RunPlayback.Native(()=>AccessTools.DeclaredMethod(typeof(NPotionPopup),"OnDiscardButtonPressed").Invoke(popup,[null]));
            await RunPlayback.Wait(()=>_task is not null,"reward-discard-potion-not-started");
            await RunPlayback.Await(_task!);
            Codec.Require(player.PotionSlots[slot] is null,"reward-discard-potion-not-removed");
        }
        finally{Clear();}
    }
    internal static NPotionPopup OpenPopup(int slot,PotionModel potion)
    {
        var container=NRun.Instance!.GlobalUi.TopBar.PotionContainer;
        var holders=(IReadOnlyList<NPotionHolder>)RunPatches.Field(container,"_holders")!;
        Codec.Require(slot>=0&&slot<holders.Count&&ReferenceEquals(holders[slot].Potion?.Model,potion),"run-potion-holder-mismatch");
        var holder=holders[slot];
        RunPlayback.Native(()=>AccessTools.DeclaredMethod(typeof(NPotionHolder),"OpenPotionPopup",[]).Invoke(holder,[]));
        var popup=RunPatches.Field(holder,"_popup") as NPotionPopup;
        Codec.Require(popup is not null&&GodotObject.IsInstanceValid(popup)&&popup.IsNodeReady(),"run-potion-popup-not-ready");
        return popup!;
    }
}
