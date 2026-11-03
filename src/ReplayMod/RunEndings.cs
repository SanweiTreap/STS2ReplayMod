using HarmonyLib;
using Godot;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
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
    internal static bool GameOverActive=>PlayerLoop.Active&&_gameOver&&ReferenceEquals(_owner,RunManager.Instance.DebugOnlyGetState());
    internal static bool DeathActive=>PlayerLoop.Active&&_kind=="death"&&ReferenceEquals(_owner,RunManager.Instance.DebugOnlyGetState());
    private static TheArchitect? _openingModel;
    private static Task? _opening;
    private static string? _openingEntry;
    private static Task? _summary;
    private static bool _summaryAuto;
    private static readonly HashSet<Task> ClipTasks=[];
    internal static bool SummaryComplete=>_summary?.IsCompletedSuccessfully==true;
    internal static void Clear(){_owner=null;_kind=null;_state=null;_operation=null;_gameOver=false;_openingModel=null;_opening=null;_openingEntry=null;_summary=null;_summaryAuto=false;ClipTasks.Clear();}
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"OnEnded"),
            prefix:new HarmonyMethod(typeof(RunEndings),nameof(NativeEnd)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NRun),"ShowGameOverScreen"),
            prefix:new HarmonyMethod(typeof(RunEndings),nameof(GameOverBefore)),
            postfix:new HarmonyMethod(typeof(RunEndings),nameof(GameOver)));
        h.Patch(AccessTools.DeclaredMethod(typeof(TheArchitect),"PlayCurrentLine"),
            postfix:new HarmonyMethod(typeof(RunEndings),nameof(DialogueTask)));
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"EnterRoom"),
            postfix:new HarmonyMethod(typeof(RunEndings),nameof(EventEntered)));
        foreach(var name in new[]{"AnimateIn","AnimateInQuote","AnimateRunSummary"})
            h.Patch(AccessTools.DeclaredMethod(typeof(NGameOverScreen),name),
                postfix:new HarmonyMethod(typeof(RunEndings),nameof(ScreenTask)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NGameOverScreen),"AnimateRunSummary"),
            postfix:new HarmonyMethod(typeof(RunEndings),nameof(SummaryTask)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NGameOverScreen),"OpenSummaryScreen"),
            prefix:new HarmonyMethod(typeof(RunEndings),nameof(SummaryOnce)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NGameOverScreen),"OnMainMenuButtonPressed"),
            prefix:new HarmonyMethod(typeof(RunEndings),nameof(NativeExitBlocked)));
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
        if(!PlayerLoop.Active&&!RunRecording.Enabled||RunPatches.Field(__instance,"_runHistoryWasUploaded") is true)return;
        Runtime.Guard(()=>
        {
            _owner=LiveState.Run;_kind=isVictory?"victory":__instance.IsAbandoned?"abandoned":"death";
            // Freeze at the same native boundary on record and playback, before
            // statistics/history/platform writes and WinRun's subsequent kill.
            _state=RunResources.Capture();_gameOver=false;
            if(PlayerLoop.Active)
            {
                Codec.Require(!__instance.ShouldSave&&ReferenceEquals(MegaCrit.Sts2.Core.Saves.SaveManager.Instance,PlayerLoop.IsolatedManager),
                    "native-terminal-outside-isolated-context");
                if(_kind=="death")
                {
                    var expected=NativeSeek.ExpectedDeath??throw new InvalidDataException("unrecorded-native-death");
                    using var result=JsonDocument.Parse(expected.After);
                    Codec.Require(result.RootElement.GetProperty("TerminalResources").GetString()==_state,"native-death-resources-or-rng-mismatch");
                    if(RunPlayback.Active)RunPlayback.VerifyDeath(_state);
                    _operation=RunPlayback.Active?RunPlayback.Current!.Sequence:null;
                    BattleEnd.Lost(_state);
                    Runtime.Log("run-native-death-verified",new{Operation=_operation,Host=expected.Sequence,
                        ResourcesAndRngMatched=true,NativeOnEnded=true,IsolatedManagerOnly=true,ExpectedStateInjected=false});
                }
                else
                {
                    Codec.Require(RunPlayback.Active,"native-victory-without-run-route");
                    var operation=RunPlayback.Current;var fact=RunPlayback.TerminalFact();
                    Codec.Require(operation is {Terminal:"victory",Kind:"event-choice"}&&isVictory&&
                        _owner.CurrentRoom?.IsVictoryRoom==true&&fact is {Kind:"victory"}&&fact.State==_state&&operation.After==_state,
                        "native-terminal-kind-position-resources-or-rng-mismatch");
                    _operation=operation!.Sequence;
                    Runtime.Log("run-native-terminal-verified",new{operation.Sequence,operation.SourceSequence,Kind=_kind,
                        ResourcesAndRngMatched=true,NativeOnEnded=true,IsolatedManagerOnly=true,ExpectedStateInjected=false});
                }
                if(StopTestArmed)
                {
                    StopTestArmed=false;
                    Runtime.Log("run-terminal-stop-test",new{Phase=_kind=="death"?"native-death-OnEnded-before-game-over":"native-OnEnded-before-WinRun-kill-and-game-over",
                        OneShotConsumed=true,PublicControl="PlayerLoop.Control(stop)"});
                    PlayerLoop.Control("stop");
                }
            }
            else
            {
                _operation=RunRecording.Pending?.Sequence;
                RunRecording.NoteLifecycle(_kind,isVictory?"native-OnEnded(true)":__instance.IsAbandoned?
                    "native-Abandon-OnEnded(false)":"native-death-OnEnded(false)",_state);
                if(_kind=="death"&&Recorder.Active)BattleEnd.Lost(_state);
            }
        });
    }
    private static void GameOverBefore()
    {
        if(_kind=="death"&&ReferenceEquals(_owner,RunManager.Instance.DebugOnlyGetState()))GameOverVisuals.Capture("before-native-game-over");
    }
    private static void GameOver()
    {
        if(_owner is null||!ReferenceEquals(_owner,RunManager.Instance.DebugOnlyGetState()))return;
        _gameOver=true;
        if(_kind=="death"){BattleEnd.DeathScreen();GameOverVisuals.Capture("after-native-model-transfer");}
        if(PlayerLoop.Active&&NOverlayStack.Instance?.Peek() is NGameOverScreen screen)DisableNativeExit(screen,"game-over-entry");
        Runtime.Log("run-native-game-over-shown",new{Kind=_kind,Operation=_operation,Playback=PlayerLoop.Active,
            NativeScreen=true,VictoryRoom=_owner.CurrentRoom?.IsVictoryRoom==true});
    }
    private static void ScreenTask(MethodBase __originalMethod,Task __result)
    {
        if(!ReferenceEquals(_owner,RunManager.Instance.DebugOnlyGetState()))return;
        Track(__result);
        if(_kind=="death"&&__originalMethod.Name=="AnimateIn")_=GameOverVisuals.After(__result,_owner!,"native-game-over-animation-completed");
    }
    private static void SummaryTask(NGameOverScreen __instance,ref Task __result)
    {
        if(!GameOverActive)return;
        var native=__result;__result=Observe();_summary=__result;Track(__result);
        async Task Observe()
        {
            await native;
            DisableNativeExit(__instance,"summary-completed");
            Runtime.Log("run-native-summary-shown",new{NativeSummaryTaskCompleted=true,
                IsolatedManagerOnly=ReferenceEquals(MegaCrit.Sts2.Core.Saves.SaveManager.Instance,PlayerLoop.IsolatedManager),
                RecordedEndpoint=RunPlayback.Finished||NativeSeek.Finished,AdditionalRunDecision=false,AutoRequested=_summaryAuto});
        }
    }
    private static void DisableNativeExit(NGameOverScreen screen,string phase)
    {
        if(!GameOverActive||!GodotObject.IsInstanceValid(screen)||!screen.IsNodeReady()||
            RunPatches.Field(screen,"_mainMenuButton") is not NButton button)return;
        button.Disable();
        Runtime.Log("run-native-exit-disabled",new{Phase=phase,NativeButtonDisabled=!button.IsEnabled,OnlyStopExits=true});
    }
    private static bool NativeExitBlocked()
    {
        if(!GameOverActive)return true;
        Runtime.Log("run-native-game-over-exit-blocked",new{OnlyStopExits=true,NormalGameUnaffected=true});return false;
    }
    private static bool SummaryOnce(NGameOverScreen __instance)
    {
        if(!GameOverActive||_summary is null&&RunPatches.Field(__instance,"_isAnimatingSummary") is not true)return true;
        Runtime.Log("run-native-summary-repeat-ignored",new{RecordedEndpoint=RunPlayback.Finished,NativeSummaryAlreadyStarted=true});return false;
    }
    internal static async Task OpenSummaryAutomatically()
    {
        Codec.Require(GameOverActive&&(RunPlayback.Finished||NativeSeek.Finished),"auto-summary-outside-recorded-native-game-over");
        NGameOverScreen? screen=null;NButton? button=null;
        Runtime.Status="正在自动展开原生结算详情";
        await Wait(()=>
        {
            if(_summary is not null)return true; // A manual click may already have started it.
            screen=NOverlayStack.Instance?.Peek() as NGameOverScreen;
            if(screen is null||!GodotObject.IsInstanceValid(screen)||!screen.IsNodeReady())return false;
            button=RunPatches.Field(screen,"_continueButton") as NButton;
            return button?.IsEnabled==true;
        },"native-game-over-continue-not-ready");
        CheckStop();
        var startedHere=_summary is null;
        if(startedHere)
        {
            _summaryAuto=true;
            RunPlayback.Native(()=>AccessTools.Method(typeof(NGameOverScreen),"OpenSummaryScreen").Invoke(screen,[button]));
        }
        Codec.Require(_summary is not null,"native-summary-handler-did-not-start");
        Runtime.Log("run-native-summary-auto-started",new{NativeContinueHandlerCalled=startedHere,
            ReusedExistingNativeTask=!startedHere,AdditionalRunDecision=false,OnEndedRepeated=false});
        while(!_summary!.IsCompleted){CheckStop();await Runtime.NextFrame();}
        await _summary;
        Runtime.Log("run-native-summary-auto-completed",new{NativeSummaryTaskCompleted=true,SceneKeptOpen=true,AutomaticExit=false});
    }
    private static bool ExitAfterRun(ref Task __result)
    {
        if(!PlayerLoop.Active||PlayerLoop.TearingDown)return true;
        _=PlayerLoop.ExitFromMenu("native-game-over-exit",callerWaitsForTeardown:false);
        __result=Task.CompletedTask;return false;
    }
    private static void Track(Task task)
    {
        if(!PlayerLoop.Active)return;
        if(RunPlayback.Active){RunPlayback.Track(task);return;}
        if(ClipTasks.Add(task))_=task.ContinueWith(t=>{_=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
    }
    private static void CheckStop()
    {
        if(PlayerLoop.TearingDown||NativeSeek.Stopping||RunPlayback.Active&&RunPlayback.Stopping)throw new OperationCanceledException();
        PlayerLoop.RequireHealthy();
        if(ClipTasks.FirstOrDefault(t=>t.IsFaulted) is {} failed)throw new InvalidDataException("native-game-over-task-failed",failed.Exception);
        if(RunPlayback.Active)RunPlayback.CheckStop();
    }
    private static async Task Wait(Func<bool> ready,string reason)
    {
        var at=Stopwatch.GetTimestamp();
        while(!ready()){CheckStop();Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<45,reason);await Runtime.NextFrame();}
    }
    internal static async Task DrainAfterSceneExit()
    {
        if(ClipTasks.Count==0)return;
        var at=Stopwatch.GetTimestamp();
        while(ClipTasks.Any(t=>!t.IsCompleted))
        {Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds<5,"native-game-over-task-did-not-retire");await Runtime.NextFrame();}
        Runtime.Log("native-game-over-tasks-retired",new{Tasks=ClipTasks.Count,Canceled=ClipTasks.Count(t=>t.IsCanceled),
            Faulted=ClipTasks.Count(t=>t.IsFaulted),BeforeOriginalManagerRestored=true});
        ClipTasks.Clear();
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
