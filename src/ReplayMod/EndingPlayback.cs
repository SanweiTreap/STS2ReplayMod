using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

namespace ReplayMod;

// Viewer navigation through the native information -> summary action. These are
// presentation nodes, not invented operations appended to an old recording.
internal static class EndingPlayback
{
    private static NGameOverScreen? _screen;
    private static Task? _entrance;
    private static bool _active,_auto,_advance,_busy;
    internal static bool Active=>_active&&RunEndings.GameOverActive;
    internal static bool Auto=>_auto;
    internal static bool CanStep=>Active&&!_busy&&!_advance&&!RunEndings.SummaryStarted;
    internal static bool CanAuto=>Active&&(!RunEndings.SummaryComplete||_auto);
    internal static string Display=>Active?"\n结局动作 · "+(_busy?"正在继续到结算详情":RunEndings.SummaryComplete?"1/1 · 结算详情":"0/1 · 原生继续")+" · 待播"+(_advance?1:0):"";
    internal static void Clear(){_screen=null;_entrance=null;_active=false;_auto=false;_advance=false;_busy=false;}
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
        else if(command=="auto"&&CanAuto){_auto=!_auto;if(!_busy)_advance=_auto;accepted=true;}
        Runtime.Log("ending-control",new{Command=command,Accepted=accepted,Auto=_auto,Pending=_advance?1:0,
            Position=RunEndings.SummaryComplete?1:0,NativeButtonsClickable=false});
    }
    internal static async Task Run(bool auto)
    {
        Codec.Require(RunEndings.GameOverActive&&_screen is not null&&_entrance is not null,"ending-screen-not-owned");
        _active=true;_auto=auto;_advance=auto;_busy=false;Status();
        Runtime.Log("ending-playback-ready",new{Auto=auto,NativeContinueIsAction=true,InformationNode=0,SummaryNode=1,OnlyStopExits=true});
        try
        {
            while(true)
            {
                RunEndings.CheckStop();
                if(PlayerLoop.HistoryMenuOpen){await Runtime.NextFrame();continue;}
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
        finally{_active=false;_auto=false;_advance=false;}
    }
    private static void Status()
    {
        var exit=PlayerLoop.FromHistory?"从菜单退出回放":"点Stop退出";
        Runtime.Status=_busy?"正在执行原生继续，展开结算详情":
            RunEndings.SummaryComplete?$"结算详情 · {exit}":$"结局信息 · > / >> 执行继续，{exit}";
    }
}
