using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;

namespace ReplayMod;

internal static class RunRewardPotions
{
    internal const string Kind="reward-discard-potion";
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

    internal static bool Queue(DiscardPotionGameAction action)
    {
        if(!RunPlayback.Active&&!RunRecording.Enabled)return true;
        var slot=checked((int)((NetDiscardPotionGameAction)action.ToNetAction()).potionSlotIndex);
        if(RunPlayback.Active)
        {
            if(!RunPlayback.Driving||!RunPlayback.NonCombat||RunPlayback.Current?.Kind!=Kind||_drivenSlot!=slot||
                !AtBattleRewards||_action is not null||action.OwnerId!=LiveState.Player.NetId)return false;
            _action=action;
            return true;
        }
        if(!AtBattleRewards||action.OwnerId!=LiveState.Player.NetId)return true;
        _recorded=RunPatches.Record(Kind,()=>RunRecording.Begin(Kind,new(RewardIndex:slot,
            RewardKey:LiveState.Player.GetPotionAtSlotIndex(slot)?.Id.ToString()??throw new InvalidDataException("reward-discard-potion-slot-empty"))));
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
        Codec.Require(AtBattleRewards&&slot>=0&&slot<player.PotionSlots.Count&&player.PotionSlots[slot] is {} potion&&
            potion.Id.ToString()==operation.Argument.RewardKey,"reward-discard-potion-identity-mismatch");
        var container=NRun.Instance!.GlobalUi.TopBar.PotionContainer;
        var holders=(IReadOnlyList<NPotionHolder>)RunPatches.Field(container,"_holders")!;
        Codec.Require(slot<holders.Count&&ReferenceEquals(holders[slot].Potion?.Model,player.PotionSlots[slot]),"reward-discard-potion-holder-mismatch");
        var holder=holders[slot];
        _drivenSlot=slot;
        try
        {
            RunPlayback.Native(()=>AccessTools.DeclaredMethod(typeof(NPotionHolder),"OpenPotionPopup").Invoke(holder,[]));
            var popup=RunPatches.Field(holder,"_popup") as NPotionPopup;
            Codec.Require(popup is not null&&GodotObject.IsInstanceValid(popup)&&popup.IsNodeReady(),"reward-discard-potion-popup-not-ready");
            await Runtime.NextFrame();RunPlayback.CheckStop();
            RunPlayback.Native(()=>AccessTools.DeclaredMethod(typeof(NPotionPopup),"OnDiscardButtonPressed").Invoke(popup,[null]));
            await RunPlayback.Wait(()=>_task is not null,"reward-discard-potion-not-started");
            await RunPlayback.Await(_task!);
            Codec.Require(player.PotionSlots[slot] is null,"reward-discard-potion-not-removed");
        }
        finally{Clear();}
    }
}
