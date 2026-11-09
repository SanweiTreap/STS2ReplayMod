using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace ReplayMod;

// A real Continue consumes the exact native save boundary computed by this playback.
internal static class RunComputedLoads
{
    private static readonly Dictionary<int,string> Saves=[];
    internal static void Install(Harmony h)=>h.Patch(AccessTools.DeclaredMethod(typeof(RunSaveManager),"SaveRun",[typeof(AbstractRoom)]),
        prefix:new HarmonyMethod(typeof(RunComputedLoads),nameof(Capture)));
    internal static void Clear()=>Saves.Clear();
    private static void Capture(RunSaveManager __instance,AbstractRoom? __0)
    {
        if(__0 is null&&RunPlayback.CacheableRoomTravel is {} travel)
        {
            Runtime.Guard(()=>RunRoomEntryCache.Capture(__instance,travel));return;
        }
        if(!RunPlayback.Active||RunPlayback.Current is not {} op||__0 is not {IsPreFinished:true} room)return;
        var ancient=op.Kind=="event-choice"&&room is EventRoom {CanonicalEvent:AncientEventModel};
        var combat=op is {Kind:"battle",Battle:{Entry:not "event"}}&&room is CombatRoom {ParentEventId:null}&&LiveState.CombatRoomType(room.RoomType);
        if(!ancient&&!combat)return;
        var source=op.SourceSequence??op.Sequence;if(!RunPlayback.NeedsComputedSave(source))return;
        Runtime.Guard(()=>
        {
            Codec.Require(ReferenceEquals(SaveManager.Instance,PlayerLoop.IsolatedManager)&&
                ReferenceEquals(__instance,AccessTools.Field(typeof(SaveManager),"_runSaveManager").GetValue(PlayerLoop.IsolatedManager))&&
                ReferenceEquals(room,LiveState.Run.CurrentRoom)&&(!combat||LiveState.Run.CurrentActIndex==op.Battle!.Act),
                "computed-save-owner-or-act");
            var save=RunManager.Instance.ToSave(room);save.SchemaVersion=SaveManager.Instance.GetLatestSchemaVersion<SerializableRun>();
            var json=JsonSerializationUtility.ToJson(save);Saves[source]=json;
            Runtime.Log(ancient?"run-computed-ancient-save-captured":"run-computed-combat-save-captured",new{SourceSequence=source,op.Visit,Act=LiveState.Run.CurrentActIndex,
                RoomType=room.RoomType.ToString(),SnapshotSHA256=Codec.Hash(Encoding.UTF8.GetBytes(json)),FromThisPlayback=true,
                BeforeVictoryProgressUpdate=combat,NativeSaveBoundary=true,OriginalSaveWritten=false});
        });
    }
    internal static async Task Restore(RunContinuation point,RunOperation sourceOperation,PlayerLoop.ComputedLoadKind kind)
    {
        var source=sourceOperation.SourceSequence??sourceOperation.Sequence;
        var ancient=kind==PlayerLoop.ComputedLoadKind.Ancient;
        Codec.Require((ancient||RunPlayback.BattleVerified(source))&&Saves.TryGetValue(source,out _),"computed-save-missing-or-battle-unverified");
        var json=Saves[source];
        await PlayerLoop.LoadComputedContinuation(json,kind);
        Runtime.Log(ancient?"run-computed-ancient-continuation":"run-computed-combat-continuation",new{point.Visit,point.Attempt,SourceSequence=source,
            SnapshotSHA256=Codec.Hash(Encoding.UTF8.GetBytes(json)),Source="native-ToSave-at-this-playback-save-boundary",
            BattlePreviouslyVerified=!ancient,RecordedCheckpointLoaded=false,ExpectedStateInjected=false,
            NativeRunReconstructed=true,SameIsolatedSaveManager=true});
    }
}
