using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal static class RunEndings
{
    internal static bool StopTestArmed;
    private static RunState? _owner;
    private static string? _kind,_state;
    private static int? _operation;
    private static bool _gameOver;
    private static TheArchitect? _openingModel;
    private static Task? _opening;
    private static string? _openingEntry;
    internal static void Clear(){_owner=null;_kind=null;_state=null;_operation=null;_gameOver=false;_openingModel=null;_opening=null;_openingEntry=null;}
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"OnEnded"),
            prefix:new HarmonyMethod(typeof(RunEndings),nameof(NativeEnd)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NRun),"ShowGameOverScreen"),
            postfix:new HarmonyMethod(typeof(RunEndings),nameof(GameOver)));
        h.Patch(AccessTools.DeclaredMethod(typeof(TheArchitect),"PlayCurrentLine"),
            postfix:new HarmonyMethod(typeof(RunEndings),nameof(DialogueTask)));
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"EnterRoom"),
            postfix:new HarmonyMethod(typeof(RunEndings),nameof(EventEntered)));
        foreach(var name in new[]{"AnimateIn","AnimateInQuote","AnimateRunSummary"})
            h.Patch(AccessTools.DeclaredMethod(typeof(NGameOverScreen),name),
                postfix:new HarmonyMethod(typeof(RunEndings),nameof(ScreenTask)));
        foreach(var name in new[]{"ReturnToMainMenuAfterRun","GoToTimelineAfterRun"})
            h.Patch(AccessTools.DeclaredMethod(typeof(NGame),name),
                prefix:new HarmonyMethod(typeof(RunEndings),nameof(ExitAfterRun)));
    }
    private static void DialogueTask(TheArchitect __instance,Task __result)
    {
        if(!RunPlayback.Active&&!RunRecording.Enabled)return;
        if(RunPlayback.Active)RunPlayback.Track(__result);
        var line=(int)AccessTools.Property(typeof(TheArchitect),"CurrentLineIndex").GetValue(__instance)!;
        if(line==0&&_opening is null)
        {_openingModel=__instance;_opening=__result;}
    }
    private static void EventEntered(AbstractRoom __0,ref Task __result)
    {
        if(!__0.IsVictoryRoom||!RunPlayback.Active&&!RunRecording.Enabled)return;
        var native=__result;__result=Capture();if(RunPlayback.Active)RunPlayback.Track(__result);
        async Task Capture()
        {
            await native;
            _openingEntry=RunResources.Capture();
            Runtime.Log("run-native-final-entry-snapshot",new{Playback=RunPlayback.Active,
                NativeEnterRoomCompleted=true,BeforeOuterFadeIn=true,FromThisRun=true,ExpectedStateInjected=false});
        }
    }
    internal static string OpeningEntry()=>_openingEntry??throw new InvalidDataException("native-final-entry-snapshot-missing");
    internal static async Task FinishOpening()
    {
        Codec.Require(LiveState.Run.CurrentRoom?.IsVictoryRoom==true&&_opening is not null&&
            ReferenceEquals(_openingModel,RunManager.Instance.EventSynchronizer.GetLocalEvent()),"native-final-opening-task-missing");
        await _opening!;
        Runtime.Log("run-native-final-opening-ready",new{Playback=RunPlayback.Active,NativeDialogueTaskCompleted=true,
            Options=_openingModel!.CurrentOptions.Count,CheckpointLoaded=false,ExpectedStateInjected=false});
    }
    private static void NativeEnd(RunManager __instance,bool isVictory)
    {
        // Native WinRun marks victory, then force-kills the player. OnEnded(false)
        // from that death is a duplicate, not a second loss or abandonment.
        if(!RunPlayback.Active&&!RunRecording.Enabled||RunPatches.Field(__instance,"_runHistoryWasUploaded") is true)return;
        Runtime.Guard(()=>
        {
            _owner=LiveState.Run;_kind=isVictory?"victory":__instance.IsAbandoned?"abandoned":"death";
            // Freeze at the same native boundary on record and playback, before
            // statistics/history/platform writes and WinRun's subsequent kill.
            _state=RunResources.Capture();_gameOver=false;
            if(RunPlayback.Active)
            {
                var operation=RunPlayback.Current;var fact=RunPlayback.TerminalFact();
                Codec.Require(!__instance.ShouldSave&&ReferenceEquals(MegaCrit.Sts2.Core.Saves.SaveManager.Instance,PlayerLoop.IsolatedManager),
                    "native-terminal-outside-isolated-context");
                Codec.Require(operation is {Terminal:"victory",Kind:"event-choice"}&&isVictory&&
                    _owner.CurrentRoom?.IsVictoryRoom==true&&fact is {Kind:"victory"}&&fact.State==_state&&operation.After==_state,
                    "native-terminal-kind-position-resources-or-rng-mismatch");
                _operation=operation!.Sequence;
                Runtime.Log("run-native-terminal-verified",new{operation.Sequence,operation.SourceSequence,Kind=_kind,
                    ResourcesAndRngMatched=true,NativeOnEnded=true,IsolatedManagerOnly=true,ExpectedStateInjected=false});
                if(StopTestArmed)
                {
                    StopTestArmed=false;
                    Runtime.Log("run-terminal-stop-test",new{Phase="native-OnEnded-before-WinRun-kill-and-game-over",
                        OneShotConsumed=true,PublicControl="PlayerLoop.Control(stop)"});
                    PlayerLoop.Control("stop");
                }
            }
            else
            {
                _operation=RunRecording.Pending?.Sequence;
                RunRecording.NoteLifecycle(_kind,isVictory?"native-OnEnded(true)":__instance.IsAbandoned?
                    "native-Abandon-OnEnded(false)":"native-death-OnEnded(false)",_state);
            }
        });
    }
    private static void GameOver()
    {
        if(_owner is null||!ReferenceEquals(_owner,RunManager.Instance.DebugOnlyGetState()))return;
        _gameOver=true;
        Runtime.Log("run-native-game-over-shown",new{Kind=_kind,Operation=_operation,Playback=RunPlayback.Active,
            NativeScreen=true,VictoryRoom=_owner.CurrentRoom?.IsVictoryRoom==true});
    }
    private static void ScreenTask(Task __result)
    {
        if(RunPlayback.Active&&ReferenceEquals(_owner,RunManager.Instance.DebugOnlyGetState()))RunPlayback.Track(__result);
    }
    private static bool ExitAfterRun(ref Task __result)
    {
        if(!RunPlayback.Active||PlayerLoop.TearingDown)return true;
        _=PlayerLoop.ExitFromMenu("native-game-over-exit",callerWaitsForTeardown:false);
        __result=Task.CompletedTask;return false;
    }
    internal static string? RecordingVictory(RunOperation operation)
    {
        if(_kind!="victory"||_operation!=operation.Sequence||!ReferenceEquals(_owner,LiveState.Run))return null;
        Codec.Require(_gameOver&&operation is {Kind:"event-choice"}&&_owner.CurrentRoom?.IsVictoryRoom==true&&_state is not null,
            "recording-native-victory-task-or-screen-incomplete");
        return _state;
    }
    internal static string PlaybackVictory(RunOperation operation)
    {
        Codec.Require(operation.Terminal=="victory"&&_kind=="victory"&&_operation==operation.Sequence&&
            ReferenceEquals(_owner,LiveState.Run)&&_gameOver&&_state is not null,"playback-native-victory-task-or-screen-incomplete");
        return _state!;
    }
}
