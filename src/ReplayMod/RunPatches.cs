using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReplayMod;

internal static class RunPatches
{
    internal static RewardsSet? Rewards;
    internal static NRewardsScreen? RewardsScreen;
    internal static Task? Travel;
    internal static Task? UiTask,ProceedTask,RestAnimation;
    internal static Task<bool>? ResultTask;
    internal static RunOperation? Record(string kind,Func<RunOperation?> begin)
    {
        try{return begin();}
        catch(Exception ex)
        {
            Runtime.Guard(()=>RunRecording.MissingOperation(kind,ex.GetType().Name));
            Runtime.Guard(()=>System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw());return null;
        }
    }
    internal static object? Field(object instance,string name)=>AccessTools.Field(instance.GetType(),name)?.GetValue(instance);
    internal static string RewardKey(Reward reward)
    {
        // Native CardReward.ToSerializable explicitly rejects combat creation
        // flags. Describe the already-generated native offer, not a save DTO.
        if(reward is CardReward cards)
        {
            var options=AccessTools.Property(typeof(CardReward),"Options").GetValue(cards)!;
            string? Value(string name)=>AccessTools.Property(options.GetType(),name)?.GetValue(options)?.ToString()??
                AccessTools.Field(options.GetType(),name)?.GetValue(options)?.ToString();
            return "CardReward-v2:"+JsonSerializer.Serialize(new{Cards=cards.Cards.Select(RunChoices.CardKey).ToArray(),
                cards.CanSkip,cards.CanReroll,Source=Value("Source"),Flags=Value("Flags")},Codec.Json);
        }
        if(reward is PotionReward potion)
        {
            Codec.Require(potion.Potion is not null,"run-reward-potion-not-populated");
            return "PotionReward-v2:"+potion.Potion!.Id;
        }
        return reward.GetType().Name+":"+JsonSerializer.Serialize(reward.ToSerializable(),JsonSerializationUtility.GetTypeInfo<SerializableReward>());
    }
    internal static void Install(Harmony h)
    {
        void Patch(Type type,string name,string? before=null,string? after=null)
        {var method=AccessTools.DeclaredMethod(type,name)??throw new MissingMethodException(type.Name,name);
            h.Patch(method,before is null?null:new HarmonyMethod(typeof(RunPatches),before),after is null?null:new HarmonyMethod(typeof(RunPatches),after));}
        Patch(typeof(RunManager),"EnterRoomInternal",after:nameof(Room));
        Patch(typeof(RunManager),"RollRoomTypeFor",after:nameof(RoomTypeChosen));
        Patch(typeof(NMapScreen),"SetMap",before:nameof(GeneratedMap));
        Patch(typeof(NMapScreen),"OnMapPointSelectedLocally",before:nameof(Map));
        Patch(typeof(RunManager),"EnterMapCoord",after:nameof(EnteredMap));
        Patch(typeof(RewardsSetSynchronizer),"BeginRewardsSet",before:nameof(RewardsBegan));
        Patch(typeof(NRewardsScreen),"ShowScreen",after:nameof(RewardsShown));
        Patch(typeof(NCombatUi),"ShowRewards",after:nameof(OwnedTask));
        Patch(typeof(NCombatUi),"ProceedWithoutRewards",after:nameof(OwnedTask));
        Patch(typeof(NRewardButton),"GetReward",after:nameof(ButtonTask));
        Patch(typeof(NRestSiteButton),"SelectOption",after:nameof(ButtonTask));
        Patch(typeof(NRestSiteRoom),"AfterSelectingOptionAsync",after:nameof(RestAnimationTask));
        Patch(typeof(RewardsSetSynchronizer),"SelectLocalReward",before:nameof(RewardBefore),after:nameof(RewardAfter));
        Patch(typeof(RunManager),"ProceedFromTerminalRewardsScreen",before:nameof(ProceedBefore),after:nameof(ProceedAfter));
        Patch(typeof(RestSiteSynchronizer),"ChooseLocalOption",before:nameof(RestBefore),after:nameof(RestAfter));
        Patch(typeof(NRestSiteRoom),"OnProceedButtonReleased",before:nameof(RestProceedBefore),after:nameof(RestProceedAfter));
        RunChoices.Install(h);
        RunTreasures.Install(h);
        RunMerchants.Install(h);
        RunEvents.Install(h);
    }
    private static void RoomTypeChosen(MapPointType __0,RoomType __result)=>Runtime.Guard(()=>
    {
        if(__0==MapPointType.Unknown)Runtime.Log("run-unknown-room-resolved",new{MapType=__0.ToString(),ActualRoomType=__result.ToString(),
            Playback=RunPlayback.Active,NativeRollExecuted=true,RoomTypeForced=false});
        Recorder.RoomTypeChosen(__0,__result);
    });
    private static void GeneratedMap(ActMap __0)=>Runtime.Guard(()=>TestEncounter.ConfigureRunRoute(__0));
    private static void Room(AbstractRoom __0,ref Task __result)
    {var native=__result;__result=Finish();async Task Finish(){await native;Runtime.Guard(()=>RunRecording.Room(__0));}}
    private static bool Map(NMapPoint __0)
    {
        if(RunPlayback.Active)return RunPlayback.Driving;
        _=Record("map",()=>RunRecording.Begin("map",new(__0.Point.coord.row,__0.Point.coord.col)));return true;
    }
    private static void EnteredMap(ref Task __result)
    {
        Travel=__result;
        if(RunPlayback.Active)RunPlayback.Track(__result);
        if(RunRecording.Pending is {Kind:"map"} operation)__result=RunRecording.Wrap(__result,operation,compareAfter:false);
    }
    private static void RewardsBegan(RewardsSet __0)
    {if((RunRecording.Enabled||RunPlayback.Active)&&ReferenceEquals(__0.Player,LiveState.Player))Rewards=__0;}
    private static void RewardsShown(NRewardsScreen __result)
    {
        if(RunRecording.Enabled||RunPlayback.Active)RewardsScreen=__result;
        if(RunPlayback.Active&&RunPlayback.Current?.Kind=="treasure-open")Runtime.Guard(()=>throw new InvalidDataException("treasure-extra-reward-screen-not-yet-supported"));
        if(RunRecording.Pending is {Kind:"event-choice"})Runtime.Guard(()=>RunRecording.Unsupported("event-rewards","事件内额外领奖页仍待接入"));
        if(RunPlayback.Active&&RunPlayback.Current?.Kind=="event-choice")Runtime.Guard(()=>throw new InvalidDataException("event-extra-reward-screen-not-yet-supported"));
    }
    private static void RewardBefore(Reward __0,out RunOperation? __state)
    {
        __state=null;if(!RunRecording.Enabled)return;
        if(RunRecording.Pending is {Kind:"treasure-open"}){Runtime.Guard(()=>RunRecording.Unsupported("treasure-extra-reward","附加奖励页尚未接入"));return;}
        var index=Rewards?.Rewards.IndexOf(__0)??-1;
        if(index<0)return;
        __state=Record("reward",()=>RunRecording.Begin("reward",new(RewardIndex:index,RewardKey:RewardKey(__0))));
    }
    private static void RewardAfter(ref Task<bool> __result,RunOperation? __state)
    {__result=RunRecording.Wrap(__result,__state);if(RunPlayback.Active){ResultTask=__result;RunPlayback.Track(__result);}}
    private static void ProceedBefore(out RunOperation? __state)=>__state=Record("proceed",()=>RunRecording.Begin(LiveState.Run.CurrentRoom is TreasureRoom?"treasure-proceed":"proceed",new()));
    private static void ProceedAfter(ref Task __result,RunOperation? __state)
    {__result=RunRecording.Wrap(__result,__state);if(RunPlayback.Active){ProceedTask=__result;RunPlayback.Track(__result);}}
    private static void RestBefore(RestSiteSynchronizer __instance,int __0,out RunOperation? __state)
    {
        __state=null;if(!RunRecording.Enabled)return;
        var option=__instance.GetLocalOptions()[__0];
        __state=Record("rest",()=>RunRecording.Begin("rest",new(Option:option.OptionId)));
    }
    private static void RestAfter(ref Task<bool> __result,RunOperation? __state)
    {__result=RunRecording.Wrap(__result,__state);if(RunPlayback.Active){ResultTask=__result;RunPlayback.Track(__result);}}
    private static void ButtonTask(Task __result)
    {if(RunPlayback.Active){UiTask=__result;RunPlayback.Track(__result);}}
    private static void RestAnimationTask(Task __result)
    {if(RunPlayback.Active){RestAnimation=__result;RunPlayback.Track(__result);}}
    private static void OwnedTask(Task __result)=>RunPlayback.Track(__result);
    private static void RestProceedBefore(out RunOperation? __state)=>__state=Record("rest-proceed",()=>RunRecording.Begin("rest-proceed",new()));
    private static void RestProceedAfter(RunOperation? __state){if(__state is not null)Runtime.Guard(()=>RunRecording.Complete(__state));}
    internal static void Clear(){Rewards=null;RewardsScreen=null;Travel=null;UiTask=null;ProceedTask=null;ResultTask=null;RestAnimation=null;}
}
