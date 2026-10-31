using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal static class RunTreasures
{
    private static NTreasureRoom? _room;
    private static Task? _open,_awards,_obtain,_pickTask;
    private static PickRelicAction? _pick;
    private static string? _pickedModel;
    private static bool _obtainStarted;
    private static RelicModel? _addingRelic;
    private static Player? _addingPlayer;
    private sealed record ObtainScope(RunOperation? Operation,bool Playback);
    private static NTreasureRoomRelicCollection Collection=>(NTreasureRoomRelicCollection)RunPatches.Field(_room!,"_relicCollection")!;
    private static string Offer()=>JsonSerializer.Serialize(RunManager.Instance.TreasureRoomRelicSynchronizer.CurrentRelics?.Select(r=>r.Id.ToString()).ToArray()??[],Codec.Json);
    internal static void Install(Harmony h)
    {
        void Patch(Type type,string method,string? before=null,string? after=null)=>h.Patch(AccessTools.DeclaredMethod(type,method),
            before is null?null:new HarmonyMethod(typeof(RunTreasures),before),after is null?null:new HarmonyMethod(typeof(RunTreasures),after));
        Patch(typeof(NTreasureRoom),"_Ready",after:nameof(Ready));
        Patch(typeof(NTreasureRoom),"_ExitTree",before:nameof(Exit));
        Patch(typeof(NTreasureRoom),"OnChestButtonReleased",before:nameof(Input));
        Patch(typeof(NTreasureRoom),"OnProceedButtonPressed",before:nameof(Input));
        Patch(typeof(NTreasureRoom),"OpenChest",before:nameof(OpenBefore),after:nameof(OpenAfter));
        Patch(typeof(NTreasureRoom),"EnableSkipAfterDelay",before:nameof(DelayBefore),after:nameof(DelayAfter));
        Patch(typeof(NTreasureRoom),"RelicFtueCheck",after:nameof(Owned));
        Patch(typeof(NTreasureRoomRelicCollection),"InitializeRelics",after:nameof(Offered));
        Patch(typeof(NTreasureRoomRelicCollection),"AnimateRelicAwards",after:nameof(Awards));
        Patch(typeof(TreasureRoomRelicSynchronizer),"PickRelicLocally",before:nameof(PickBefore));
        h.Patch(AccessTools.DeclaredMethod(typeof(RelicCmd),"Obtain",[typeof(RelicModel),typeof(Player),typeof(int)]),
            new HarmonyMethod(typeof(RunTreasures),nameof(ObtainBefore)),new HarmonyMethod(typeof(RunTreasures),nameof(ObtainAfter)));
        Patch(typeof(PickRelicAction),"ExecuteAction",after:nameof(PickTask));
    }
    private static bool Input()=>!RunPlayback.Active||RunPlayback.Driving;
    private static void Ready(NTreasureRoom __instance){if(RunRecording.Enabled||RunPlayback.Active)_room=__instance;}
    private static void Exit(NTreasureRoom __instance)
    {
        if(!ReferenceEquals(_room,__instance))return;
        var collection=RunPatches.Field(__instance,"_relicCollection");
        if(RunPlayback.Active&&collection is not null)
        {
            // The native chest task waits on collection TCSs even when leaving
            // without a relic. Retire only this spectator scene's waiters.
            foreach(var name in new[]{"_relicPickingBeganTaskCompletionSource","_relicPickingCompleteTaskCompletionSource"})
                if(RunPatches.Field(collection,name) is TaskCompletionSource source)source.TrySetCanceled();
        }
        _room=null;
    }
    private static void OpenBefore()=>Runtime.Guard(()=>{if(RunRecording.Enabled)RunRecording.Begin("treasure-open",new());});
    private static void OpenAfter(Task __result){_open=__result;RunPlayback.Track(__result);}
    private static void Offered()=>Runtime.Guard(()=>
    {
        if(RunRecording.Pending is not {Kind:"treasure-open"} pending)return;
        RunRecording.Describe(pending.Argument with{Option=Offer()});RunRecording.Complete(pending);
    });
    private static void Awards(Task __result){_awards=__result;RunPlayback.Track(__result);}
    private static void Owned(Task __result)=>RunPlayback.Track(__result);
    private static void DelayBefore(ref CancellationToken __1,out CancellationTokenSource? __state)
    {
        __state=null;if(!RunPlayback.Active||_room is null)return;
        var scene=(CancellationTokenSource)RunPatches.Field(_room,"_cts")!;
        __state=CancellationTokenSource.CreateLinkedTokenSource(__1,scene.Token);__1=__state.Token;
    }
    private static void DelayAfter(ref Task __result,CancellationTokenSource? __state)
    {
        if(__state is null)return;var native=__result;__result=Finish();RunPlayback.Track(__result);
        async Task Finish(){try{await native;}finally{__state.Dispose();}}
    }
    internal static void Clear(){_room=null;_open=null;_awards=null;_obtain=null;_pickTask=null;_pick=null;_pickedModel=null;_obtainStarted=false;_addingRelic=null;_addingPlayer=null;}
    private static bool PickBefore(int? __0)
    {
        if(!Input())return false;
        if(__0 is not {} index)return true; // Skip belongs to the native Proceed action.
        Runtime.Guard(()=>
        {
            var relics=RunManager.Instance.TreasureRoomRelicSynchronizer.CurrentRelics;
            Codec.Require(relics is not null&&index>=0&&index<relics.Count,"treasure-pick-index");
            _pickedModel=relics![index].Id.ToString();_obtainStarted=false;
            if(RunRecording.Enabled)RunRecording.Begin("treasure-pick",new(RewardIndex:index,RewardKey:Offer()));
        });return true;
    }
    private static void ObtainBefore(RelicModel __0,Player __1,out ObtainScope? __state)
    {
        __state=null;
        if(_obtainStarted||_pickedModel!=__0.Id.ToString()||!ReferenceEquals(__1,LiveState.Player))return;
        if(RunPlayback.Active&&RunPlayback.NonCombat&&RunPlayback.Current?.Kind=="treasure-pick"&&_pick is not null)
        {_obtainStarted=true;_addingRelic=__0;_addingPlayer=__1;__state=new(null,true);}
        else if(RunRecording.Pending is {Kind:"treasure-pick"} pending){_obtainStarted=true;__state=new(pending,false);}
    }
    private static void ObtainAfter(RelicModel __0,ref Task<RelicModel> __result,ObtainScope? __state)
    {
        if(__state is null)return;
        if(__state.Operation is {} operation)__result=RunRecording.Wrap(__result,operation);
        if(__state.Playback){_obtain=__result;RunPlayback.Track(__result);}
        if(ReferenceEquals(_addingRelic,__0)){_addingRelic=null;_addingPlayer=null;}
    }
    internal static bool OwnsRelicAdd(Player player,RelicModel relic)=>RunPlayback.Active&&RunPlayback.NonCombat&&
        RunPlayback.Current?.Kind=="treasure-pick"&&_pick is not null&&ReferenceEquals(player,_addingPlayer)&&
        ReferenceEquals(relic,_addingRelic)&&ReferenceEquals(player,LiveState.Player);
    internal static bool OwnsPick(PickRelicAction action)
    {
        Codec.Require(RunPlayback.NonCombat&&RunPlayback.Driving&&_room is not null&&_pick is null&&action.OwnerId==LiveState.Player.NetId&&
            RunPlayback.Current is {Kind:"treasure-pick" or "treasure-proceed"},"treasure-action-outside-operation");
        var expected=RunPlayback.Current!.Kind=="treasure-pick"?RunPlayback.Current.Argument.RewardIndex:null;
        Codec.Require(action.ToNetAction() is NetPickRelicAction net&&net.relicIndex==expected,"treasure-action-choice-mismatch");
        _pick=action;return true;
    }
    private static void PickTask(PickRelicAction __instance,Task __result)
    {if(RunPlayback.Active&&ReferenceEquals(_pick,__instance)){_pickTask=__result;RunPlayback.Track(__result);}}
    internal static async Task Play(RunOperation operation)
    {
        await RunPlayback.Wait(()=>_room is not null&&GodotObject.IsInstanceValid(_room)&&_room.IsNodeReady(),"treasure-room-not-ready");
        if(operation.Kind=="treasure-open")
        {
            _open=null;RunPlayback.Native(()=>AccessTools.Method(typeof(NTreasureRoom),"OnChestButtonReleased").Invoke(_room,[null]));
            await RunPlayback.Wait(()=>RunPatches.Field(_room!,"_hasChestBeenOpened") is true,"treasure-opening-not-complete");
            Codec.Require(Offer()==operation.Argument.Option,"treasure-offer-mismatch");return;
        }
        _pick=null;_pickTask=null;
        if(operation.Kind=="treasure-pick")
        {
            Codec.Require(Offer()==operation.Argument.RewardKey,"treasure-pick-offer-mismatch");
            var holders=((IEnumerable<NTreasureRoomRelicHolder>)RunPatches.Field(Collection,"_holdersInUse")!).ToArray();
            var holder=holders.Single(h=>h.Index==operation.Argument.RewardIndex);
            await RunPlayback.Wait(()=>holder.IsEnabled&&holder.MouseFilter==Control.MouseFilterEnum.Stop,"treasure-relic-not-presented");
            _awards=null;_obtain=null;
            RunPlayback.Native(()=>AccessTools.Method(typeof(NTreasureRoomRelicCollection),"PickRelic").Invoke(Collection,[holder]));
            await RunPlayback.Wait(()=>_obtain is not null,"treasure-native-obtain-not-started");await RunPlayback.Await(_obtain!);
            if(_awards is not null)await RunPlayback.Await(_awards);
            if(_open is not null)await RunPlayback.Await(_open);return;
        }
        await RunPlayback.Wait(()=>_room!.ProceedButton.IsEnabled,"treasure-proceed-not-ready");
        RunPatches.ProceedTask=null;
        RunPlayback.Native(()=>AccessTools.Method(typeof(NTreasureRoom),"OnProceedButtonPressed").Invoke(_room,[null]));
        await RunPlayback.Wait(()=>RunPatches.ProceedTask is not null,"treasure-native-proceed-not-started");
        await RunPlayback.Await(RunPatches.ProceedTask!);
        if(_pick is not null){await RunPlayback.Wait(()=>_pickTask is not null,"treasure-skip-action-not-started");await RunPlayback.Await(_pickTask!);}
    }
}
