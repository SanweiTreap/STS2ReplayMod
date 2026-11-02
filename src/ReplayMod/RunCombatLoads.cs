using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace ReplayMod;

// A native victory save precedes progress/discovery updates. A recorded real
// Continue must consume that boundary, not the live post-progress RunState.
internal static class RunCombatLoads
{
    private static readonly Dictionary<int,string> Saves=[];
    internal static void Install(Harmony h)=>h.Patch(AccessTools.DeclaredMethod(typeof(RunSaveManager),"SaveRun",[typeof(AbstractRoom)]),
        prefix:new HarmonyMethod(typeof(RunCombatLoads),nameof(Capture)));
    internal static void Clear()=>Saves.Clear();
    private static void Capture(RunSaveManager __instance,AbstractRoom? __0)
    {
        if(!RunPlayback.Active||RunPlayback.Current is not {Kind:"battle",Battle:not null} op||op.Battle.Entry=="event"||
            __0 is not CombatRoom {IsPreFinished:true,ParentEventId:null} room||!LiveState.CombatRoomType(room.RoomType))return;
        var source=op.SourceSequence??op.Sequence;if(!RunPlayback.NeedsComputedSave(source))return;
        Runtime.Guard(()=>
        {
            Codec.Require(ReferenceEquals(SaveManager.Instance,PlayerLoop.IsolatedManager)&&
                ReferenceEquals(__instance,AccessTools.Field(typeof(SaveManager),"_runSaveManager").GetValue(PlayerLoop.IsolatedManager))&&
                ReferenceEquals(room,LiveState.Run.CurrentRoom)&&LiveState.Run.CurrentActIndex==op.Battle.Act,
                "computed-combat-save-owner-or-act");
            var save=RunManager.Instance.ToSave(room);save.SchemaVersion=SaveManager.Instance.GetLatestSchemaVersion<SerializableRun>();
            var json=JsonSerializationUtility.ToJson(save);Saves[source]=json;
            Runtime.Log("run-computed-combat-save-captured",new{SourceSequence=source,op.Visit,op.Battle.Act,
                RoomType=room.RoomType.ToString(),SnapshotSHA256=Codec.Hash(Encoding.UTF8.GetBytes(json)),FromThisPlayback=true,
                BeforeVictoryProgressUpdate=true,NativeSaveBoundary=true,OriginalSaveWritten=false});
        });
    }
    internal static async Task Restore(RunContinuation point,RunOperation battle)
    {
        var source=battle.SourceSequence??battle.Sequence;
        Codec.Require(RunPlayback.BattleVerified(source)&&Saves.ContainsKey(source),"computed-combat-save-missing-or-battle-unverified");
        var json=Saves[source];
        await PlayerLoop.LoadComputedContinuation(json,completedMap:true);
        Runtime.Log("run-computed-combat-continuation",new{point.Visit,point.Attempt,SourceSequence=source,
            SnapshotSHA256=Codec.Hash(Encoding.UTF8.GetBytes(json)),Source="native-ToSave-at-this-playback-save-boundary",
            BattlePreviouslyVerified=true,RecordedCheckpointLoaded=false,ExpectedStateInjected=false,
            NativeRunReconstructed=true,SameIsolatedSaveManager=true});
    }
}
