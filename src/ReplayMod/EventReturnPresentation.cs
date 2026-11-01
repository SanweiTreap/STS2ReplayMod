using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// Only the presentation of an evidenced completed-event Load is delayed. The
// event's original Resume, RNG, upgrades and native VFX implementation stay intact.
internal static class EventReturnPresentation
{
    private static RunState? _run;
    private static TaskCompletionSource? _revealed;
    private static readonly List<Task> Upgrades=[];
    private static Task? _layout;
    private static NEventRoom? _room;
    private static bool _nativeAnimation;
    private static int _started,_finished;
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(NCardUpgradeVfx),"PlayAnimation"),prefix:new HarmonyMethod(typeof(EventReturnPresentation),nameof(Upgrade)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NEventRoom),"SetupLayout"),postfix:new HarmonyMethod(typeof(EventReturnPresentation),nameof(Layout)));
    }
    internal static void Begin(RunState run)
    {
        Codec.Require(_run is null&&RunPlayback.Active&&PlayerLoop.Loading,"event-return-presentation-owner");
        _run=run;_revealed=new(TaskCreationOptions.RunContinuationsAsynchronously);_layout=null;_room=null;_started=0;_finished=0;Upgrades.Clear();
    }
    private static void Layout(NEventRoom __instance,Task __result)
    {
        if(_run is null||!ReferenceEquals(_run,RunManager.Instance.DebugOnlyGetState()))return;
        _room=__instance;_layout=__result;RunPlayback.Track(__result);
    }
    private static bool Upgrade(NCardUpgradeVfx __instance,ref Task __result)
    {
        if(_nativeAnimation||_run is null||_revealed is null||
            AccessTools.Field(typeof(NCardUpgradeVfx),"_card").GetValue(__instance) is not CardModel card||
            !ReferenceEquals(card.Owner.RunState,_run))return true;
        var gate=_revealed;__result=PlayWhenVisible(__instance,gate);Upgrades.Add(__result);RunPlayback.Track(__result);return false;
    }
    private static async Task PlayWhenVisible(NCardUpgradeVfx node,TaskCompletionSource gate)
    {
        while(!gate.Task.IsCompleted)
        {
            RunPlayback.CheckStop();if(!GodotObject.IsInstanceValid(node)||node.IsQueuedForDeletion())return;
            await Runtime.NextFrame();
        }
        await gate.Task;RunPlayback.CheckStop();
        if(!GodotObject.IsInstanceValid(node)||node.IsQueuedForDeletion())return;
        Task native;
        _nativeAnimation=true;
        try{native=(Task)AccessTools.Method(typeof(NCardUpgradeVfx),"PlayAnimation").Invoke(node,[])!;}
        finally{_nativeAnimation=false;}
        _started++;await native;_finished++;
    }
    internal static async Task Finish()
    {
        Codec.Require(_run is not null&&_revealed is not null,"event-return-presentation-not-started");
        await RunPlayback.Wait(()=>ReferenceEquals(_run,RunManager.Instance.DebugOnlyGetState())&&
            _run!.CurrentRoom is EventRoom&&_room is not null&&GodotObject.IsInstanceValid(_room)&&
            ReferenceEquals(_room,NEventRoom.Instance)&&_room.IsNodeReady()&&_room.Layout is not null&&
            _layout?.IsCompletedSuccessfully==true&&!Runtime.Game.Transition.InTransition,"event-return-page-not-visible");
        // Native ResumePreviousRoom has completed its fade-in. Let that visible
        // page draw before starting the ORIGINAL scale-up/upgrade/fly-to-deck task.
        await Runtime.NextFrame();RunPlayback.CheckStop();
        Runtime.Log("run-event-return-presentation-revealed",new{NativeLayoutComplete=true,NativeFadeComplete=true,UpgradeTasks=Upgrades.Count});
        _revealed!.TrySetResult();await Runtime.NextFrame();
        await RunPlayback.Await(Task.WhenAll(Upgrades.ToArray()));
        Codec.Require(_finished==Upgrades.Count,"event-upgrade-animation-disappeared-before-reveal");
        Runtime.Log("run-event-return-presentation-complete",new{UpgradeTasks=Upgrades.Count,Started=_started,Finished=_finished,OriginalAnimationsCompleted=true});
    }
    internal static void End()
    {
        _revealed?.TrySetCanceled();_revealed=null;_run=null;_layout=null;_room=null;_nativeAnimation=false;Upgrades.Clear();
    }
}
