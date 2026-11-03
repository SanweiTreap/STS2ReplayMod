using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// Viewer navigation through the native information -> summary action. These are
// presentation nodes, not invented operations appended to an old recording.
internal static class EndingPlayback
{
    private static NGameOverScreen? _screen;
    private static Task? _entrance;
    private static bool _active,_auto,_advance,_back,_busy,_restoring;
    internal static bool Active=>_active&&RunEndings.GameOverActive;
    internal static bool Auto=>_auto;
    internal static bool Restoring=>_restoring;
    internal static bool CanStep=>Active&&!_busy&&!_restoring&&!_advance&&!RunEndings.SummaryStarted;
    internal static bool CanBack=>Active&&!_busy&&!_restoring&&!_back&&RunEndings.SummaryComplete;
    internal static bool CanAuto=>Active&&!_restoring&&(!RunEndings.SummaryComplete||_auto);
    internal static string Display=>Active?"\n结局动作 · "+(_restoring?"返回信息页":_busy?"正在继续到结算详情":RunEndings.SummaryComplete?"1/1 · 结算详情":"0/1 · 原生继续")+" · 待播"+(_advance?1:0):"";
    internal static void Clear(){_screen=null;_entrance=null;_active=false;_auto=false;_advance=false;_back=false;_busy=false;_restoring=false;}
    internal static void ScreenOpened(NGameOverScreen screen,Task entrance)
    {
        if(!PlayerLoop.Active)return;
        _screen=screen;_entrance=entrance;
        _=DisableAfterEntrance();
        async Task DisableAfterEntrance()
        {
            try
            {
                await entrance;
                if(ReferenceEquals(_screen,screen)&&RunEndings.GameOverActive)RunEndings.DisableNativeControls(screen,"information-ready");
            }
            catch(OperationCanceledException){}
            catch(Exception ex){Runtime.Log("ending-entrance-failed",new{Reason=ex.GetBaseException().Message});}
        }
    }
    internal static void Control(string command)
    {
        var accepted=false;
        if(command is "step" or "fine"&&CanStep){_advance=true;accepted=true;}
        else if(command=="back"&&CanBack){_auto=false;_back=true;accepted=true;}
        else if(command=="auto"&&CanAuto){_auto=!_auto;if(!_busy)_advance=_auto;accepted=true;}
        Runtime.Log("ending-control",new{Command=command,Accepted=accepted,Auto=_auto,Pending=_advance?1:0,
            Position=RunEndings.SummaryComplete?1:0,NativeButtonsClickable=false});
    }
    internal static async Task Run(bool auto)
    {
        Codec.Require(RunEndings.GameOverActive&&_screen is not null&&_entrance is not null,"ending-screen-not-owned");
        _active=true;_auto=auto;_advance=auto;_back=false;_busy=false;_restoring=false;Status();
        Runtime.Log("ending-playback-ready",new{Auto=auto,NativeContinueIsAction=true,InformationNode=0,SummaryNode=1,OnlyStopExits=true});
        try
        {
            while(true)
            {
                RunEndings.CheckStop();
                if(_back){_back=false;await Back();Status();}
                if(_advance)
                {
                    _advance=false;_busy=true;Status();
                    try
                    {
                        await RunEndings.Wait(()=>_entrance?.IsCompleted==true,"ending-information-not-ready");
                        await _entrance!;
                        await RunEndings.Continue(_auto);
                        _auto=false;
                    }
                    finally{_busy=false;}
                    Status();
                    Runtime.Log("ending-action-completed",new{Action="native-continue",Position=1,Auto=false,
                        NativeSummaryCompleted=RunEndings.SummaryComplete,SceneKeptOpen=true,OnlyStopExits=true});
                }
                await Runtime.NextFrame();
            }
        }
        finally{_active=false;_auto=false;_advance=false;_back=false;}
    }
    private static void Status()=>Runtime.Status=_restoring?"正在返回原生结局信息页":_busy?"正在执行原生继续，展开结算详情":
        RunEndings.SummaryComplete?"结算详情 · 可Back回信息页，点Stop退出":"结局信息 · > / >> 执行继续，点Stop退出";
    private static async Task Back()
    {
        _restoring=true;Status();var before=RunResources.Capture();var owner=LiveState.Run;
        try
        {
            var screen=_screen!;
            var stack=NOverlayStack.Instance??throw new InvalidDataException("ending-overlay-stack-missing");
            var room=NCombatRoom.Instance??throw new InvalidDataException("ending-combat-display-missing");
            Codec.Require(GodotObject.IsInstanceValid(screen)&&ReferenceEquals(stack.Peek(),screen)&&
                RunEndings.SummaryComplete,"ending-back-without-completed-owned-summary");
            // The native overlay owns the existing creature visuals. Return them to
            // their original creatures before disposing that OVERLAY, preserving the
            // current world transform and death animation. No combat is reloaded.
            foreach(var creature in room.CreatureNodes)
                if(screen.IsAncestorOf(creature.Visuals))creature.Visuals.Reparent(creature);
            stack.Remove(screen);
            _screen=null;_entrance=null;RunEndings.ResetSummary();
            await Runtime.NextFrame();RunEndings.CheckStop();
            await RunEndings.Wait(()=>!GodotObject.IsInstanceValid(screen)||!screen.IsInsideTree(),"ending-old-overlay-not-retired");
            var nativeRun=NRun.Instance??throw new InvalidDataException("ending-native-run-display-missing");
            nativeRun.ShowGameOverScreen(RunEndings.ActualTerminalRun);
            Codec.Require(ReferenceEquals(owner,LiveState.Run)&&RunResources.Capture()==before,"ending-back-changed-run-resources-or-rng");
            Runtime.Log("ending-back-arrived",new{Position=0,SameRunState=true,ResourcesAndRngUnchanged=true,
                NativeOverlayRecreated=true,OnEndedRepeated=false,CombatReplayed=false,RecordingExpectedInjected=false});
        }
        finally{_restoring=false;}
    }
}
