using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// Native room-stack transitions, owned by the event option and the same visit.
// These battle snapshots describe an opening; they are never cold-load entries.
internal static class RunEventCombat
{
    private static string? _parent,_encounter;
    private static bool _reenter;
    private static Task? _entry,_ready;
    private static readonly List<Task> ResumeTasks=[];
    internal static bool Returning;
    internal static void Clear(){_parent=null;_encounter=null;_entry=null;_ready=null;_reenter=false;Returning=false;ResumeTasks.Clear();}
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(EventCombatSynchronizer),"ReadyToEnterCombat"),prefix:new HarmonyMethod(typeof(RunEventCombat),nameof(Ready)));
        h.Patch(AccessTools.Method(typeof(RunManager),"EnterRoomWithoutExitingCurrentRoom"),
            prefix:new HarmonyMethod(typeof(RunEventCombat),nameof(EnterBefore)),postfix:new HarmonyMethod(typeof(RunEventCombat),nameof(EnterAfter)));
        foreach(var type in typeof(EventModel).Assembly.GetTypes().Where(t=>t.IsSubclassOf(typeof(EventModel))))
            if(AccessTools.DeclaredMethod(type,"Resume",[typeof(AbstractRoom)]) is {IsAbstract:false} method)
                h.Patch(method,postfix:new HarmonyMethod(typeof(RunEventCombat),nameof(Resumed)));
    }
    private static bool Ready(EncounterModel __0,Player __1,IReadOnlyList<Reward> __2,bool __3)
    {
        if(!RunPlayback.Active&&!RunRecording.Enabled)return true;
        var supported=RunEvents.InOrdinaryEvent&&RunEvents.OwnsNativeChoice&&__3&&__2.Count==0&&
            __0.RoomType==RoomType.Monster&&ReferenceEquals(__1,LiveState.Player)&&
            LiveState.Run.CurrentRoom is EventRoom {CanonicalEvent.LayoutType:not EventLayoutType.Combat};
        if(!supported)
        {
            if(RunPlayback.Active){Runtime.Guard(()=>throw new InvalidDataException("event-combat-layout-rewards-or-owner-unsupported"));return false;}
            Runtime.Guard(()=>RunRecording.Unsupported("event-combat","尚未支持的内部战斗布局、额外奖励或选项归属"));return true;
        }
        _parent=LiveState.Run.CurrentRoom!.ModelId!.ToString();_encounter=__0.Id.ToString();_entry=null;_ready=null;
        Runtime.Log("run-event-combat-requested",new{Parent=_parent,Encounter=_encounter,ResumeParent=true,NativeSynchronizer=true});return true;
    }
    private static bool EnterBefore(RunManager __instance,AbstractRoom __0,bool __1,ref Task __result)
    {
        if(_reenter||_parent is null||__0 is not CombatRoom room)return true;
        var valid=false;
        Runtime.Guard(()=>
        {
            Codec.Require(room.ParentEventId?.ToString()==_parent&&room.ModelId.ToString()==_encounter&&
                room.ShouldCreateCombat&&room.ShouldResumeParentEventAfterCombat,"event-combat-room-stack-mismatch");
            if(!RunPlayback.Active)_ready=Recorder.BeginEventBattle(room);
            valid=true;
        });
        if(!valid){if(RunPlayback.Active)__result=Task.CompletedTask;return !RunPlayback.Active;}
        if(!RunPlayback.Active)return true;
        var clip=RunPlayback.EventBattle(_parent,_encounter!);
        __result=PlayerLoop.LoadEnteredBattle(clip,()=>
        {
            _reenter=true;
            try{return __instance.EnterRoomWithoutExitingCurrentRoom(room,__1);}
            finally{_reenter=false;}
        });
        RunPlayback.Track(__result);return false;
    }
    private static void EnterAfter(AbstractRoom __0,Task __result)
    {
        if(_parent is null||__0 is not CombatRoom room||room.ParentEventId?.ToString()!=_parent)return;
        _entry=__result;if(!RunPlayback.Active)Recorder.EventEntryTask(__result);else RunPlayback.Track(__result);
    }
    internal static async Task ChoiceBoundary()
    {
        if(_parent is null)return;
        var entry=_entry??throw new InvalidDataException("event-combat-native-entry-not-started");
        if(RunPlayback.Active)await RunPlayback.Await(entry);else
        {
            await entry;
            var at=System.Diagnostics.Stopwatch.GetTimestamp();
            while(_ready is {IsCompleted:false})
            {Codec.Require(System.Diagnostics.Stopwatch.GetElapsedTime(at).TotalSeconds<45,"event-combat-recording-opening-not-ready");await Runtime.NextFrame();}
            if(_ready is {} ready)await ready;
        }
        Runtime.Log("run-event-combat-entered",new{Parent=_parent,Encounter=_encounter,NativeEntry=true,ColdCheckpointLoaded=false});
        _parent=null;_encounter=null;_entry=null;_ready=null;
    }
    internal static bool IsReturn=>LiveState.Run.CurrentRoom is CombatRoom {ParentEventId:not null,ShouldResumeParentEventAfterCombat:true};
    internal static void BeginReturn()
    {
        Returning=true;ResumeTasks.Clear();
        if(!PlayerLoop.Active&&Recorder.Active)
        {
            // A no-reward encounter advances automatically. Preserve the complete
            // winning action before the native stack pop removes its state.
            if(ActionMonitor.Pending is not null&&BattleEnd.Ready)ActionMonitor.Tick();
            if(Recorder.Active)Runtime.Guard(()=>RunRecording.Unsupported("event-return","战斗终态未封存前开始返回"));
        }
    }
    private static void Resumed(Task __result)
    {if(Returning){ResumeTasks.Add(__result);RunPlayback.Track(__result);}}
    internal static Task ReturnBoundary(Task native)
    {
        if(!Returning)return native;
        return Finish();
        async Task Finish()
        {
            try
            {
                await native;await Task.WhenAll(ResumeTasks.ToArray());await Runtime.NextFrame();
                Codec.Require(RunEvents.InOrdinaryEvent,"event-return-did-not-resume-parent");
                Runtime.Log("run-event-parent-resumed",new{Parent=LiveState.Run.CurrentRoom!.ModelId!.ToString(),NativeResume=true,
                    ResumeTasks=ResumeTasks.Count,SameMapVisit=true,Playback=RunPlayback.Active});
            }
            finally{Returning=false;ResumeTasks.Clear();}
        }
    }
    internal static async Task PlayReturn()
    {
        Codec.Require(IsReturn&&LiveState.Run.CurrentRoom is CombatRoom {Encounter.ShouldGiveRewards:false},"event-auto-return-room-mismatch");
        PlayerLoop.ReleaseBattleForTravel();
        Task? native=null;
        RunPlayback.Native(()=>native=NCombatRoom.Instance!.Ui.ProceedWithoutRewards());
        await RunPlayback.Await(native!);
    }
}
