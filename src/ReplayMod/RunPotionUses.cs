using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;

namespace ReplayMod;

internal static class RunPotionUses
{
    internal const string Kind="use-potion";
    private static UsePotionAction? _action;
    private static PotionModel? _potion,_driven;
    private static RunOperation? _recorded;
    private static Task? _task;

    internal static void Clear(){_action=null;_potion=null;_driven=null;_recorded=null;_task=null;}
    internal static void Install(Harmony harmony)=>harmony.Patch(AccessTools.DeclaredMethod(typeof(NPotionPopup),"OnUseButtonPressed"),
        prefix:new HarmonyMethod(typeof(RunPotionUses),nameof(PopupInput)));
    private static bool PopupInput()=>!RunPlayback.Active||!RunPlayback.NonCombat||_driven is not null&&RunPlayback.Driving;
    internal static bool CanDrive(PotionModel potion)=>RunPlayback.Active&&RunPlayback.NonCombat&&RunPlayback.Driving&&ReferenceEquals(_driven,potion);
    internal static bool OwnsEffect(PotionModel potion)=>ReferenceEquals(_potion,potion)&&_action is not null;
    internal static bool AtRewards(RunOperation operation)=>operation is {Kind:Kind,Argument.PotionUse.Context:"rewards"};

    private static string? Context()=>ActiveScreenContext.Instance.GetCurrentScreen() switch
    {
        NRewardsScreen screen when ReferenceEquals(screen,RunPatches.RewardsScreen)&&LiveState.Run.CurrentRoom is CombatRoom=>"rewards",
        NMapScreen when NOverlayStack.Instance?.Peek() is not NRewardsScreen=>"map",
        NMapRoom=>"map",
        NRestSiteRoom=>"rest",
        NTreasureRoom=>"treasure",
        NMerchantRoom=>"merchant",
        NMerchantInventory=>"merchant-inventory",
        NEventRoom=>"event",
        _=>null
    };
    private static bool Supported(PotionModel potion)=>potion.Usage==PotionUsage.AnyTime&&
        potion.TargetType is TargetType.AnyPlayer or TargetType.Self&&potion.IsValidTarget(potion.Owner.Creature);

    internal static bool Queue(UsePotionAction action)
    {
        if(RunPlayback.Active&&(!RunPlayback.NonCombat||!RunPlayback.Driving||_driven is null))return false;
        var slot=checked((int)action.PotionIndex);
        var potion=action.Player.GetPotionAtSlotIndex(slot);
        var net=(NetUsePotionAction)action.ToNetAction();
        // Out of combat the native action resolves the net player, with self
        // as the fallback for non-merchant actions. Ignore stale combat IDs.
        var context=Context();
        var use=potion is not null&&Supported(potion)&&ReferenceEquals(action.Player,LiveState.Player)&&
            (net.targetPlayerId is null||net.targetPlayerId==action.Player.NetId)&&context is not null?
            new RunPotionUse(slot,potion.Id.ToString(),0,context):null;
        if(RunPlayback.Active)
        {
            if(potion is null||!CanDrive(potion)||_action is not null||use is null||
                RunPlayback.Current is not {Kind:Kind} operation||use!=operation.Argument.PotionUse)return false;
            _action=action;_potion=potion;
            return true;
        }
        if(use is null||RunRecording.Pending is not null||_action is not null)
        {
            RunPatches.Record(Kind,()=>{RunRecording.MissingOperation(Kind,"unsupported-native-potion-target-or-screen");return null;});
            return true;
        }
        _recorded=RunPatches.Record(Kind,()=>RunRecording.Begin(Kind,new(PotionUse:use)));
        if(_recorded is not null){_action=action;_potion=potion;}
        return true;
    }

    internal static Task Observe(UsePotionAction action,Task task)
    {
        if(!ReferenceEquals(_action,action))return task;
        if(RunPlayback.Active){_task=task;RunPlayback.Track(task);return task;}
        return FinishRecording(action,task,_potion!,_recorded!);
    }
    private static async Task FinishRecording(UsePotionAction action,Task task,PotionModel potion,RunOperation operation)
    {
        try
        {
            await task;
            if(!ReferenceEquals(_action,action))return;
            Runtime.Guard(()=>
            {
                if(Consumed(potion))RunRecording.Complete(operation);
                else RunRecording.CancelOperation(operation,"native-potion-use-not-consumed");
            });
        }
        catch{if(ReferenceEquals(_action,action))Runtime.Guard(()=>RunRecording.CancelOperation(operation,"native-potion-use-failed"));throw;}
        finally{if(ReferenceEquals(_action,action))Clear();}
    }
    // Entropic Brew can fill the consumed slot with a different instance.
    private static bool Consumed(PotionModel potion)=>potion.HasBeenRemovedFromState&&!potion.Owner.PotionSlots.Contains(potion);
    internal static void Cancelled(UsePotionAction action)
    {
        if(RunPlayback.Active||!ReferenceEquals(_action,action)||_recorded is not {} operation)return;
        RunRecording.CancelOperation(operation,"native-potion-use-cancelled");Clear();
    }

    internal static async Task Play(RunOperation operation)
    {
        var use=operation.Argument.PotionUse??throw new InvalidDataException("run-potion-use-argument-missing");
        await RunPlayback.Wait(()=>Context()==use.Context,"run-potion-use-context-mismatch");
        var player=LiveState.Player;
        var potion=use.Slot>=0&&use.Slot<player.PotionSlots.Count?player.PotionSlots[use.Slot]:null;
        Codec.Require(use.PlayerIndex==0&&potion is not null&&Supported(potion)&&potion.Id.ToString()==use.Model,"run-potion-use-identity-mismatch");
        _driven=potion;
        try
        {
            var popup=RunRewardPotions.OpenPopup(use.Slot,potion!);
            await Runtime.NextFrame();RunPlayback.CheckStop();
            Codec.Require(GodotObject.IsInstanceValid(popup)&&RunPatches.Field(popup,"_useButton") is NClickableControl {IsEnabled:true},
                "run-potion-use-button-disabled");
            RunPlayback.Native(()=>AccessTools.DeclaredMethod(typeof(NPotionPopup),"OnUseButtonPressed").Invoke(popup,[null]));
            var action=_action??throw new InvalidDataException("run-potion-use-not-enqueued");
            var completed=action.CompletionTask;RunPlayback.Track(completed);
            // Once submitted, Stop lets this native action finish in its room.
            var started=Stopwatch.GetTimestamp();
            while(!completed.IsCompleted)
            {
                Codec.Require(Stopwatch.GetElapsedTime(started).TotalSeconds<45,"run-potion-use-did-not-settle");
                await Runtime.NextFrame();
            }
            await completed;
            await (_task??throw new InvalidDataException("run-potion-use-not-executed"));
            Codec.Require(Consumed(potion!),"run-potion-use-not-consumed");
            RunPlayback.CheckStop();
        }
        finally{Clear();}
    }
}
