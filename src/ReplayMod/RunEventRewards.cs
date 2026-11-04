using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// A custom offer is part of the native event option's unfinished task.
internal static class RunEventRewards
{
    private static RewardsSet? _set;
    private static RewardsSet? _retiring;
    private static NRewardsScreen? _screen;
    private static int? _operation;
    internal static void Clear(){_set=null;_retiring=null;_screen=null;_operation=null;}
    internal static void Install(Harmony h)=>h.Patch(AccessTools.DeclaredMethod(typeof(RewardsSet),nameof(RewardsSet.Offer),[]),
        postfix:new HarmonyMethod(typeof(RunEventRewards),nameof(OfferStarted)));
    private static void OfferStarted(RewardsSet __instance,ref Task __result)
    {
        if(RunPlayback.Active&&RunPlayback.NonCombat&&RunPlayback.Current?.Kind=="event-choice"&&RunEvents.OwnsNativeChoice&&
            ReferenceEquals(__instance.Player,LiveState.Player))
            __result=FinishOffer(__instance,__result);
    }
    private static async Task FinishOffer(RewardsSet set,Task native)
    {
        await native;
        if(!ReferenceEquals(_retiring,set)&&!RunPlayback.Stopping)return;
        // Cancel the caller's await before the event can apply its post-offer effects.
        throw new OperationCanceledException();
    }
    private static bool OnTop=>_screen is not null&&ReferenceEquals(NOverlayStack.Instance?.Peek(),_screen);
    private static string[] Offer(RewardsSet set)=>set.Rewards.Select(RunPatches.RewardKey).ToArray();
    internal static void Open(RewardsSet set,bool terminal,NRewardsScreen screen)
    {
        if(terminal||!RunEvents.OwnsNativeChoice||RunEventCombat.Returning||_screen is not null&&
            GodotObject.IsInstanceValid(_screen)&&_screen.IsInsideTree()||
            !ReferenceEquals(set,RunPatches.Rewards)||set.Rewards.Any(reward=>reward is not (RelicReward or PotionReward or CardReward)))
        {
            if(RunPlayback.Active)throw new InvalidDataException("event-extra-reward-screen-unsupported");
            RunRecording.Unsupported("event-rewards","当前事件的领奖形式尚未支持");return;
        }
        _set=set;_screen=screen;_operation=(RunPlayback.Active?RunPlayback.Current:RunRecording.Pending)!.Sequence;
        if(RunRecording.Enabled)RunRecording.Decision(new("event-rewards-open",Offer(set),[]));
        else Codec.Require(RunPlayback.Current!.Decisions.Any(d=>d.Kind=="event-rewards-open"),"event-extra-reward-screen-not-recorded");
    }
    internal static bool RecordReward(Reward reward,int index)
    {
        if(_set is null||!ReferenceEquals(_set,RunPatches.Rewards)||!OnTop||RunRecording.Pending?.Sequence!=_operation)return false;
        Runtime.Guard(()=>RunRecording.Decision(new("event-reward",[RunPatches.RewardKey(reward)],[index])));
        return true;
    }
    internal static async Task<bool> RecordResult(Task<bool> native,RewardsSet set)
    {
        var operation=RunRecording.Pending;
        var result=await native;
        if(ReferenceEquals(_set,set)&&operation is not null&&RunRecording.Pending?.Sequence==operation.Sequence)
            Runtime.Guard(()=>RunRecording.Decision(new("event-reward-result",[result?"claimed":"declined"],[])));
        return result;
    }
    internal static void RecordClose(NRewardsScreen screen)
    {
        if(RunRecording.Enabled&&ReferenceEquals(_screen,screen)&&OnTop&&RunRecording.Pending?.Sequence==_operation)
            Runtime.Guard(()=>RunRecording.Decision(new("event-rewards-close",[],[])));
    }
    internal static async Task Play(Task option,int sequence)
    {
        try
        {
            while(!option.IsCompleted)
            {
                await RunPlayback.Wait(()=>option.IsCompleted||_operation==sequence&&OnTop,"event-option-or-reward-not-ready");
                if(option.IsCompleted)break;
                var screen=_screen!;var set=_set!;
                _=RunPlayback.Decision("event-rewards-open",Offer(set));
                await RunPlayback.RewardsReady(requireVisible:true);
                while(ReferenceEquals(NOverlayStack.Instance?.Peek(),screen))
                {
                    var action=RunPlayback.NextDecision;
                    if(action is {Kind:"event-rewards-close"})
                    {
                        _=RunPlayback.Decision("event-rewards-close",[]);
                        await RunPlayback.TakePhase("关闭事件领奖页");
                        await RunPlayback.Wait(()=>Proceed(screen).IsEnabled,"event-reward-proceed-disabled");
                        Close(screen);
                        break;
                    }
                    Codec.Require(action is {Kind:"event-reward",Options.Length:1,Selected.Length:1},"event-reward-action-missing");
                    var index=action!.Selected[0];
                    Codec.Require(index>=0&&index<set.Rewards.Count,"event-reward-index");
                    var key=RunPatches.RewardKey(set.Rewards[index]);
                    _=RunPlayback.Decision("event-reward",[key]);
                    await RunPlayback.TakePhase("领取事件奖励");
                    var result=await RunPatches.PlayReward(index,key);
                    _=RunPlayback.Decision("event-reward-result",[result?"claimed":"declined"]);
                }
            }
            await RunPlayback.Await(option);
        }
        finally
        {
            if(_operation==sequence&&!option.IsCompleted)
            {
                var set=_set!;
                var sync=RunManager.Instance.RewardsSetSynchronizer;
                if(ReferenceEquals(RunPatches.Rewards,set)&&!sync.IsRewardsSetCompleted(set))
                {
                    _retiring=set;
                    // Settle the native waiter, keeping the overlay in place until teardown.
                    sync.SkipLocalRewardsSet();
                }
                var start=Stopwatch.GetTimestamp();
                while(!option.IsCompleted&&Stopwatch.GetElapsedTime(start).TotalSeconds<5)await Runtime.NextFrame();
                if(ReferenceEquals(_retiring,set))
                    Runtime.Log("run-event-rewards-retired",new{Operation=sequence,OptionCanceled=option.IsCanceled,
                        NativeSetSettled=sync.IsRewardsSetCompleted(set),ProceedInvoked=false});
            }
            Clear();
        }
    }
    private static NProceedButton Proceed(NRewardsScreen screen)=>(NProceedButton)RunPatches.Field(screen,"_proceedButton")!;
    private static void Close(NRewardsScreen screen)=>RunPlayback.Native(()=>
        AccessTools.Method(typeof(NRewardsScreen),"OnProceedButtonPressed").Invoke(screen,[null]));
}
