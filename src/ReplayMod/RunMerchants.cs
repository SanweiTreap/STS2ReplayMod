using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;

namespace ReplayMod;

internal static class RunMerchants
{
    private static MerchantEntry? _entry;
    private static bool _purchaseStarted;
    private static Task<bool>? _purchase;
    private static Task? _slotTask,_opening;
    private static NMerchantRoom Room=>NMerchantRoom.Instance??throw new InvalidDataException("merchant-room-missing");
    private static MerchantInventory Inventory=>((MerchantRoom)LiveState.Run.CurrentRoom!).GetLocalInventory();
    private static bool Input()=>!RunPlayback.Active||RunPlayback.Driving;
    internal static void Install(Harmony h)
    {
        void Patch(Type type,string method,string? before=null,string? after=null)=>h.Patch(AccessTools.DeclaredMethod(type,method),
            before is null?null:new HarmonyMethod(typeof(RunMerchants),before),after is null?null:new HarmonyMethod(typeof(RunMerchants),after));
        Patch(typeof(NMerchantRoom),"OpenInventory",nameof(OpenBefore),nameof(UiAfter));
        Patch(typeof(NMerchantRoom),"HideScreen",nameof(ProceedBefore),nameof(ProceedAfter));
        Patch(typeof(NMerchantInventory),"Open",nameof(Input));
        Patch(typeof(NMerchantInventory),"Close",nameof(CloseBefore),nameof(UiAfter));
        Patch(typeof(NMerchantInventory),"DoOpenAnimation",after:nameof(Opening));
        Patch(typeof(NMerchantSlot),"OnMouseReleased",nameof(Input));
        Patch(typeof(NMerchantSlot),"OnSelected",nameof(SlotBefore),nameof(SlotAfter));
        Patch(typeof(MerchantEntry),"OnTryPurchaseWrapper",nameof(PurchaseBefore),nameof(PurchaseAfter));
        Patch(typeof(MerchantCardRemovalEntry),"OnTryPurchaseWrapper",nameof(RemovalBefore),nameof(PurchaseAfter));
    }
    // Prices and stock are native room state, absent from the room-entry save.
    // Include them in strict resource snapshots; no inventory is copied in.
    internal static string Offer()=>JsonSerializer.Serialize(Inventory.AllEntries.Select(e=>new{
        Kind=e.GetType().Name,e.Cost,Stocked=e.IsStocked,Content=e switch{
            MerchantCardEntry c=>c.CreationResult is {} card?RunChoices.CardKey(card.Card):null,
            MerchantRelicEntry r=>r.Model?.Id.ToString(),MerchantPotionEntry p=>p.Model?.Id.ToString(),
            MerchantCardRemovalEntry=>"remove-card",_=>throw new InvalidDataException("merchant-entry-type-unsupported")}}).ToArray(),Codec.Json);
    internal static void Clear(){_entry=null;_purchaseStarted=false;_purchase=null;_slotTask=null;_opening=null;}
    private static RunOperation? Begin(string kind,RunArgument? argument=null)=>RunPatches.Record(kind,()=>
        RunRecording.Begin(kind,argument??new(RewardKey:Offer())));
    private static bool OpenBefore(NMerchantRoom __instance,out RunOperation? __state)
    {__state=null;if(!Input())return false;if(RunRecording.Enabled&&!__instance.Inventory.IsOpen)__state=Begin("merchant-open");return true;}
    private static bool CloseBefore(NMerchantInventory __instance,out RunOperation? __state)
    {__state=null;if(!Input())return false;if(RunRecording.Enabled&&__instance.IsOpen)__state=Begin("merchant-close");return true;}
    private static void UiAfter(RunOperation? __state){if(__state is not null)Runtime.Guard(()=>RunRecording.Complete(__state));}
    private static bool ProceedBefore(out RunOperation? __state)
    {__state=null;if(!Input())return false;if(RunRecording.Enabled)__state=Begin("merchant-proceed");return true;}
    private static void ProceedAfter(RunOperation? __state)
    {
        if(__state is null)return;
        Runtime.Guard(()=>{if(NMapScreen.Instance is {IsOpen:true})RunRecording.Complete(__state);
            else RunRecording.Unsupported("merchant-proceed","商人首次教程阻断离开，尚未支持");});
    }
    private static void Opening(Task __result){if(RunPlayback.Active){_opening=__result;RunPlayback.Track(__result);}}
    private static bool SlotBefore(ref Task __result)
    {if(Input())return true;__result=Task.CompletedTask;return false;}
    private static void SlotAfter(NMerchantSlot __instance,Task __result)
    {if(RunPlayback.Active&&ReferenceEquals(__instance.Entry,_entry)){_slotTask=__result;RunPlayback.Track(__result);}}
    private static bool PurchaseBefore(MerchantEntry __instance,MerchantInventory? __0,bool __1,
        ref Task<bool> __result,out RunOperation? __state)
    {
        __state=null;
        if(RunPlayback.Active)
        {
            var owned=false;Runtime.Guard(()=>
            {
                Codec.Require(RunPlayback.NonCombat&&ReferenceEquals(__instance,_entry)&&ReferenceEquals(__0,Inventory)&&
                    !_purchaseStarted&&!__1&&ReferenceEquals(Inventory.Player,LiveState.Player),"merchant-purchase-outside-operation");
                _purchaseStarted=true;owned=true;
            });
            if(!owned)__result=Task.FromResult(false);return owned;
        }
        if(!RunRecording.Enabled||LiveState.Run.CurrentRoom is not MerchantRoom)return true;
        if(RunRecording.Pending is {Kind:"merchant-buy" or "merchant-remove"})
        {Runtime.Guard(()=>RunRecording.Unsupported("merchant-derived-purchase","嵌套自动购买尚未支持"));return true;}
        RunOperation? operation=null;Runtime.Guard(()=>
        {
            Codec.Require(ReferenceEquals(__0,Inventory)&&!__1,"merchant-purchase-context-unsupported");
            var index=Inventory.AllEntries.ToList().FindIndex(e=>ReferenceEquals(e,__instance));
            Codec.Require(index>=0,"merchant-entry-not-in-inventory");
            operation=Begin(__instance is MerchantCardRemovalEntry?"merchant-remove":"merchant-buy",new(RewardIndex:index,RewardKey:Offer()));
        });
        __state=operation;return true;
    }
    private static bool RemovalBefore(MerchantCardRemovalEntry __instance,MerchantInventory? __0,bool __1,bool __2,
        ref Task<bool> __result,out RunOperation? __state)
    {
        if(!__2){__state=null;Runtime.Guard(()=>RunRecording.Unsupported("merchant-removal","非可取消删牌尚未支持"));
            if(RunPlayback.Active){__result=Task.FromResult(false);return false;}return true;}
        return PurchaseBefore(__instance,__0,__1,ref __result,out __state);
    }
    private static void PurchaseAfter(ref Task<bool> __result,RunOperation? __state)
    {
        __result=RunRecording.Wrap(__result,__state);
        if(RunPlayback.Active&&_purchaseStarted){_purchase=__result;RunPlayback.Track(__result);}
    }
    internal static bool OwnsRelicAdd(Player player,RelicModel relic)=>RunPlayback.Active&&RunPlayback.NonCombat&&
        RunPlayback.Current?.Kind=="merchant-buy"&&_purchaseStarted&&_entry is MerchantRelicEntry entry&&
        ReferenceEquals(entry.Model,relic)&&ReferenceEquals(Inventory.Player,player)&&ReferenceEquals(player,LiveState.Player);
    private static async Task Ready()
    {
        await RunPlayback.Wait(()=>NMerchantRoom.Instance is {} room&&GodotObject.IsInstanceValid(room)&&room.IsNodeReady()&&
            ReferenceEquals(room.Room,LiveState.Run.CurrentRoom),"merchant-room-not-ready");
        await RunPlayback.Wait(()=>RunPatches.Field(Room.Inventory,"_inventoryTween") is not Tween tween||
            !GodotObject.IsInstanceValid(tween)||!tween.IsRunning(),"merchant-inventory-animation-not-complete");
        await RunPlayback.Wait(()=>!Room.Inventory.IsOpen||RunPatches.Field(Room.Inventory,"_isInputBlocked") is not true,
            "merchant-input-still-blocked");
    }
    internal static async Task Play(RunOperation operation)
    {
        await Ready();Codec.Require(Offer()==operation.Argument.RewardKey,"merchant-offer-or-price-mismatch");
        switch(operation.Kind)
        {
            case "merchant-open":
                Codec.Require(!Room.Inventory.IsOpen,"merchant-already-open");_opening=null;
                RunPlayback.Native(()=>Room.OpenInventory());
                if(_opening is not null)await RunPlayback.Await(_opening);
                Codec.Require(Room.Inventory.IsOpen,"merchant-open-failed");break;
            case "merchant-close":
                Codec.Require(Room.Inventory.IsOpen,"merchant-close-while-closed");
                RunPlayback.Native(()=>AccessTools.Method(typeof(NMerchantInventory),"Close").Invoke(Room.Inventory,[]));
                await Ready();Codec.Require(!Room.Inventory.IsOpen,"merchant-close-failed");break;
            case "merchant-proceed":
                Codec.Require(!Room.Inventory.IsOpen&&Room.ProceedButton.IsEnabled,"merchant-proceed-while-open");
                RunPlayback.Native(()=>AccessTools.Method(typeof(NMerchantRoom),"HideScreen").Invoke(Room,[null]));
                Codec.Require(NMapScreen.Instance is {IsOpen:true},"merchant-proceed-did-not-open-map");break;
            case "merchant-buy":case "merchant-remove":
                Codec.Require(Room.Inventory.IsOpen,"merchant-purchase-while-closed");
                var entries=Inventory.AllEntries.ToArray();var index=operation.Argument.RewardIndex??-1;
                Codec.Require(index>=0&&index<entries.Length,"merchant-entry-index");
                var entry=entries[index];Codec.Require((entry is MerchantCardRemovalEntry)==(operation.Kind=="merchant-remove"),"merchant-entry-kind-mismatch");
                var slot=Room.Inventory.GetAllSlots().Single(s=>ReferenceEquals(s.Entry,entry));
                _entry=entry;_purchaseStarted=false;_purchase=null;_slotTask=null;
                try
                {
                    RunPlayback.Native(()=>AccessTools.Method(typeof(NMerchantSlot),"OnSelected").Invoke(slot,[]));
                    Codec.Require(_purchase is not null&&_slotTask is not null,"merchant-native-purchase-not-started");
                    await RunPlayback.Await(_purchase!);
                    Codec.Require(_purchase!.Result==operation.Success,"merchant-purchase-result-mismatch");
                    await RunPlayback.Await(_slotTask!);
                }
                finally{_entry=null;_purchaseStarted=false;}
                break;
            default:throw new InvalidDataException("merchant-operation-unsupported");
        }
    }
}
