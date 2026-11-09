using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace ReplayMod;

// Detached native saves computed in an isolated viewing session. Publish a candidate
// only when native entry reached the recorded room and its first stable projection.
internal static class RunRoomEntryCache
{
    private const int Capacity=24;
    private static readonly Dictionary<RoomEntryCacheKey,RoomEntrySave> Verified=[];
    private static (RunOperation Travel,RoomEntrySave Value)? _pending;

    internal static void Clear(){Verified.Clear();_pending=null;}
    internal static RoomEntrySave? Find(RoomEntryCacheKey? key)=>key is not null?
        Verified.GetValueOrDefault(key)??RoomEntryCacheFile.Read(Runtime.Root,key):null;
    internal static void Capture(RunSaveManager manager,RunOperation travel)
    {
        if(!ReferenceEquals(SaveManager.Instance,PlayerLoop.IsolatedManager)||
            !ReferenceEquals(manager,AccessTools.Field(typeof(SaveManager),"_runSaveManager").GetValue(PlayerLoop.IsolatedManager)))return;
        var save=RunManager.Instance.ToSave(null);
        save.SchemaVersion=SaveManager.Instance.GetLatestSchemaVersion<SerializableRun>();
        _pending=(travel,new(JsonSerializationUtility.ToJson(save),RunResources.Progress()));
    }
    internal static void Complete(RunOperation travel,RunOperation next,RoomEntryCacheKey key)
    {
        if(_pending is not {} candidate||candidate.Travel!=travel)return;
        _pending=null;
        if(next.Before is not {} expected||!RunStore.ResourcesMatch(expected,RunResources.Capture()))return;
        if(Verified.Count==Capacity&&!Verified.ContainsKey(key))Verified.Remove(Verified.Keys.First());
        Verified[key]=candidate.Value;
        var persisted=RoomEntryCacheFile.Write(Runtime.Root,key,candidate.Value);
        Runtime.Log("run-room-entry-cache-captured",new{SourceSequence=key.Travel,next.Visit,next.Attempt,
            StateAndRngMatched=true,Persisted=persisted,ReplayOriginalChanged=false});
    }
}
