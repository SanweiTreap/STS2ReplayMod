using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class RunEvents
{
    private static EventOption? _option;
    private static EventModel? _model;
    private static RunOperation? _recorded;
    private static Task? _task;
    private static readonly AsyncLocal<EventOption?> NativeChoice=new();
    private static RelicModel? _addingRelic;
    private static Player? _addingPlayer;
    private sealed record ChoiceCall(EventOption? Prior,bool Owned);
    private static bool Input()=>!RunPlayback.Active||RunPlayback.Driving;
    private static EventModel Model=>RunManager.Instance.EventSynchronizer.GetLocalEvent();
    internal static bool InOrdinaryEvent=>LiveState.Run.CurrentRoom is EventRoom {CanonicalEvent:not AncientEventModel};
    internal static void Install(Harmony h)
    {
        void Patch(Type type,string method,string? before=null,string? after=null)=>h.Patch(AccessTools.DeclaredMethod(type,method),
            before is null?null:new HarmonyMethod(typeof(RunEvents),before),after is null?null:new HarmonyMethod(typeof(RunEvents),after));
        Patch(typeof(NEventOptionButton),"OnRelease",nameof(Input));
        Patch(typeof(NEventRoom),"OptionButtonClicked",nameof(Clicked));
        Patch(typeof(EventSynchronizer),"ChooseLocalOption",nameof(Choose));
        Patch(typeof(EventOption),"Chosen",nameof(ChosenBefore),nameof(ChosenAfter));
        Patch(typeof(NEventRoom),"SetupLayout",after:nameof(Owned));
        Patch(typeof(EventModel),"BeginEvent",after:nameof(Owned));
        Patch(typeof(EventCombatSynchronizer),"ReadyToEnterCombat",nameof(UnsupportedCombat));
        h.Patch(AccessTools.DeclaredMethod(typeof(RelicCmd),"Obtain",[typeof(RelicModel),typeof(Player),typeof(int)]),
            new HarmonyMethod(typeof(RunEvents),nameof(ObtainBefore)),new HarmonyMethod(typeof(RunEvents),nameof(ObtainAfter)));
    }
    private static JsonNode RngState(Rng rng)=>JsonSerializer.SerializeToNode(rng.ToSerializable(),JsonSerializationUtility.GetTypeInfo<SerializableRng>())!;
    internal static string State()
    {
        var model=Model;var sync=RunManager.Instance.EventSynchronizer;
        return JsonSerializer.Serialize(new{Model=model.Id.ToString(),model.IsFinished,
            Page=model.Description?.LocEntryKey,
            Options=model.CurrentOptions.Select(o=>new{o.TextKey,o.IsLocked,o.IsProceed,o.WasChosen,Relic=o.Relic?.Id.ToString()}).ToArray(),
            Vars=model.DynamicVars.OrderBy(v=>v.Key,StringComparer.Ordinal).Select(v=>new{v.Key,v.Value.BaseValue,v.Value.EnchantedValue,
                Text=v.Value is StringVar text?text.StringValue:null}).ToArray(),
            Rng=RngState(model.Rng),SharedPage=RunPatches.Field(sync,"_pageIndex"),
            SharedRng=model.IsShared?RngState((Rng)RunPatches.Field(sync,"_multiplayerOptionSelectionRng")!):null},Codec.Json);
    }
    internal static void Clear(){_option=null;_model=null;_recorded=null;_task=null;_addingRelic=null;_addingPlayer=null;NativeChoice.Value=null;}
    private static bool Prepare(EventOption option,string kind,int index)
    {
        if(!InOrdinaryEvent)return !RunPlayback.Active;
        var model=Model;
        if(RunPlayback.Active)
        {
            var owned=false;Runtime.Guard(()=>
            {
                Codec.Require(Input()&&RunPlayback.NonCombat&&RunPlayback.Current?.Kind==kind&&
                    ReferenceEquals(_option,option)&&ReferenceEquals(_model,model),"event-option-outside-operation");owned=true;
            });return owned;
        }
        if(!RunRecording.Enabled)return true;
        if(NEventRoom.Instance?.Layout is null||model.LayoutType==EventLayoutType.Combat)
        {Runtime.Guard(()=>RunRecording.Unsupported("event-custom-layout","自定义事件控件尚未支持"));return true;}
        _model=model;_option=option;
        _recorded=RunPatches.Record(kind,()=>RunRecording.Begin(kind,new(RewardIndex:index,RewardKey:State(),Option:option.TextKey)));
        return true;
    }
    private static bool Clicked(EventOption __0,int __1)
    {
        if(!Input())return false;
        return !__0.IsProceed||Prepare(__0,"event-proceed",__1);
    }
    private static bool Choose(int __0)
    {
        if(!Input())return false;if(!InOrdinaryEvent)return !RunPlayback.Active;
        var options=Model.CurrentOptions;
        if(__0<0||__0>=options.Count){Runtime.Guard(()=>throw new InvalidDataException("event-choice-index"));return !RunPlayback.Active;}
        return Prepare(options[__0],"event-choice",__0);
    }
    private static bool ChosenBefore(EventOption __instance,ref Task __result,out ChoiceCall __state)
    {
        var owned=ReferenceEquals(_option,__instance)&&_model is not null;
        __state=new(NativeChoice.Value,owned);
        if(owned){NativeChoice.Value=__instance;return true;}
        if(!RunPlayback.Active)return true;
        Runtime.Guard(()=>throw new InvalidDataException("event-unowned-option-task"));__result=Task.CompletedTask;return false;
    }
    private static void ChosenAfter(EventOption __instance,ref Task __result,ChoiceCall __state)
    {
        if(!__state.Owned)return;
        NativeChoice.Value=__state.Prior;
        var operation=_recorded;var option=__instance;var native=__result;var boundary=Boundary();
        __result=RunRecording.Wrap(boundary,operation);
        if(RunPlayback.Active){_task=__result;RunPlayback.Track(__result);}
        async Task Boundary()
        {
            try
            {
                await native;
                // ChooseOptionForEvent appends history after calling Chosen.
                // Even a synchronous option must finish that native call stack.
                await Runtime.NextFrame();
            }
            finally{if(ReferenceEquals(_option,option)){_option=null;_model=null;_recorded=null;}}
        }
    }
    private static void Owned(Task __result)=>RunPlayback.Track(__result);
    private static void ObtainBefore(RelicModel __0,Player __1,out bool __state)
    {
        __state=RunPlayback.Active&&RunPlayback.NonCombat&&RunPlayback.Current?.Kind=="event-choice"&&
            _option is not null&&ReferenceEquals(NativeChoice.Value,_option)&&ReferenceEquals(_model?.Owner,__1)&&
            ReferenceEquals(__1,LiveState.Player)&&_addingRelic is null;
        if(__state){_addingRelic=__0;_addingPlayer=__1;}
    }
    private static void ObtainAfter(Task<RelicModel> __result,bool __state)
    {if(__state){RunPlayback.Track(__result);_addingRelic=null;_addingPlayer=null;}}
    internal static bool OwnsRelicAdd(Player player,RelicModel relic)=>RunPlayback.Active&&RunPlayback.NonCombat&&
        RunPlayback.Current?.Kind=="event-choice"&&ReferenceEquals(NativeChoice.Value,_option)&&_option is not null&&
        ReferenceEquals(_addingPlayer,player)&&ReferenceEquals(_addingRelic,relic)&&ReferenceEquals(player,LiveState.Player);
    private static bool UnsupportedCombat()
    {
        if(RunPlayback.Active){Runtime.Guard(()=>throw new InvalidDataException("event-internal-combat-not-yet-supported"));return false;}
        if(RunRecording.Enabled)Runtime.Guard(()=>RunRecording.Unsupported("event-internal-combat","事件内部战斗仍待接入"));
        return true;
    }
    internal static async Task Play(RunOperation operation)
    {
        await RunPlayback.Wait(()=>NEventRoom.Instance is {} room&&GodotObject.IsInstanceValid(room)&&room.IsNodeReady()&&
            room.Layout is not null,"event-layout-not-ready");
        Codec.Require(InOrdinaryEvent&&State()==operation.Argument.RewardKey,"event-page-options-or-rng-mismatch");
        var index=operation.Argument.RewardIndex??-1;
        NEventOptionButton? button=null;
        await RunPlayback.Wait(()=>
        {
            button=NEventRoom.Instance!.Layout!.OptionButtons.SingleOrDefault(b=>AccessTools.Property(typeof(NEventOptionButton),"Index").GetValue(b) is int i&&i==index);
            return button is not null&&GodotObject.IsInstanceValid(button)&&button.IsNodeReady()&&
                button.MouseFilter==Control.MouseFilterEnum.Stop;
        },"event-option-not-presented");
        Codec.Require(button!.Option.TextKey==operation.Argument.Option&&!button.Option.IsLocked&&
            button.Option.IsProceed==(operation.Kind=="event-proceed"),"event-option-identity-or-availability-mismatch");
        _option=button.Option;_model=Model;_recorded=null;_task=null;
        RunPlayback.Native(()=>AccessTools.Method(typeof(NEventOptionButton),"OnRelease").Invoke(button,[]));
        Codec.Require(_task is not null,"event-native-option-task-not-started");
        await RunPlayback.Await(_task!);
    }
}
