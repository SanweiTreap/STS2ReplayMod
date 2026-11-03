using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal static class RunAbandon
{
    private static NAbandonRunConfirmPopup? _confirming;
    private static RunOperation? _recorded;
    private static Task? _native;
    internal static void Clear(){_confirming=null;_recorded=null;_native=null;}
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(NAbandonRunConfirmPopup),"OnYesButtonPressed"),
            prefix:new HarmonyMethod(typeof(RunAbandon),nameof(ConfirmBefore)),postfix:new HarmonyMethod(typeof(RunAbandon),nameof(ConfirmAfter)));
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"Abandon"),prefix:new HarmonyMethod(typeof(RunAbandon),nameof(Record)));
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"AbandonInternal"),postfix:new HarmonyMethod(typeof(RunAbandon),nameof(TaskStarted)));
    }
    private static void ConfirmBefore(NAbandonRunConfirmPopup __instance)=>_confirming=__instance;
    private static void ConfirmAfter()=>_confirming=null;
    private static void Record()
    {
        if(PlayerLoop.Active||!RunRecording.Enabled||!Recorder.Active)return;
        Runtime.Guard(()=>
        {
            if(_confirming is null||!Recorder.CanSeal||LiveState.Run.CurrentRoom is not CombatRoom {ParentEventId:null})
            {RunRecording.Unsupported("abandon","当前放弃缺少已完成、无在途选择/动作的战斗前缀或原生确认输入");return;}
            // The completed battle prefix remains its own immutable interrupted clip.
            // No forced-kill effect is misrecorded as an extra card/end-turn action.
            Recorder.Interrupt();
            _recorded=RunRecording.Begin("abandon",new(Option:"native-confirm-popup"));
            Codec.Require(_recorded is not null,"abandon-operation-not-started");
            Runtime.Log("run-abandon-recording-started",new{_recorded!.Sequence,NativeConfirmedInput=true,CompletedBattlePrefixSealed=true});
        });
    }
    private static void TaskStarted(ref Task __result)
    {
        if(RunPlayback.Active&&RunPlayback.Current?.Kind=="abandon")
        {_native=__result;RunPlayback.Track(__result);return;}
        if(_recorded is not {} operation)return;
        _recorded=null;__result=RunRecording.Wrap(__result,operation);
    }
    internal static async Task Play(RunOperation operation)
    {
        Codec.Require(operation is {Kind:"abandon",Terminal:"abandoned",Argument.Option:"native-confirm-popup"}&&
            LiveState.Run.CurrentRoom is CombatRoom,"unsupported-abandon-playback-input");
        var popup=NAbandonRunConfirmPopup.Create(null)??throw new InvalidDataException("native-abandon-popup-unavailable");
        _native=null;
        try
        {
            RunPlayback.Native(()=>NModalContainer.Instance!.Add(popup));
            await RunPlayback.Wait(()=>popup.IsNodeReady()&&
                (RunPatches.Field(popup,"_tween") is not Tween tween||!GodotObject.IsInstanceValid(tween)||!tween.IsRunning()),"native-abandon-popup-not-ready");
            await RunPlayback.TakePhase("确认主动放弃");
            RunPlayback.Native(()=>AccessTools.Method(typeof(NAbandonRunConfirmPopup),"OnYesButtonPressed").Invoke(popup,[null]));
            await RunPlayback.Wait(()=>_native is not null,"native-abandon-task-not-started");
            await RunPlayback.Await(_native!);
            Runtime.Log("run-abandon-native-completed",new{operation.Sequence,NativePopupConfirmed=true,
                NativeAbandonTaskSucceeded=true,NativeIsAbandoned=RunManager.Instance.IsAbandoned,ExpectedStateInjected=false});
        }
        finally
        {
            if(GodotObject.IsInstanceValid(popup)&&!popup.IsQueuedForDeletion())
                RunPlayback.Native(()=>AccessTools.Method(typeof(NAbandonRunConfirmPopup),"OnNoButtonPressed").Invoke(popup,[null]));
        }
    }
}
