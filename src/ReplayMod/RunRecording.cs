using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class RunResources
{
    internal static string Progress()
    {
        var manager=SaveManager.Instance;var progress=manager.Progress.ToSerializable();
        progress.SchemaVersion=manager.GetLatestSchemaVersion<SerializableProgress>();return JsonSerializationUtility.ToJson(progress);
    }
    internal static string Capture()
    {
        var save=JsonNode.Parse(JsonSerializationUtility.ToJson(RunManager.Instance.ToSave(null)))!.AsObject();
        var result=new JsonObject();
        // Compare native persistent resources and RNGs, not wall time, UI state
        // or a claim of general map/file authority. No expected value is applied.
        foreach(var key in new[]{"players","rng","odds","shared_relic_grab_bag","current_act_index","ascension","visited_map_coords","map_point_history"})
            result[key]=save[key]?.DeepClone();
        var acts=save["acts"]?.DeepClone();
        if(acts is JsonArray array)foreach(var act in array.OfType<JsonObject>())act.Remove("saved_map");
        result["acts"]=acts;return result.ToJsonString(Codec.Json);
    }
}

internal static class RunRecording
{
    internal static RunStore Store=>new(Path.Combine(Runtime.Root,"runs"));
    private static RunDocument? _run;
    private static int _visit;
    internal static RunOperation? Pending;
    private static int _battleSequence;
    private static string? _entryProgress;
    internal static bool Enabled=>_run is not null&&!PlayerLoop.Active;
    internal static bool Choosing=>Enabled&&Pending?.Kind is "reward" or "rest";
    internal static void Attach(bool continuation,SerializableRun? save=null)
    {
        if(PlayerLoop.Active||Runtime.Journal.CurrentRun is not {} id)return;
        Pending=null;_battleSequence=0;_visit=0;_entryProgress=null;
        var path=Path.Combine(Runtime.Root,"runs",id+".json");
        _run=File.Exists(path)?Store.Read(id):new(1,id,Runtime.GameHash,Runtime.ModHash,Runtime.Scope,
            save?.Players[0].CharacterId?.ToString()??LiveState.Player.Character.Id.ToString(),DateTimeOffset.UtcNow,[],[],[]);
        if(continuation)
        {
            _visit=_run.Visits.LastOrDefault()?.Id??0;
            var unfinished=_run.Operations.LastOrDefault();
            if(unfinished is {Status:"pending"})Replace(unfinished with{Status="missing"});
            _run.Continuations.Add(new(DateTimeOffset.UtcNow,_visit,save?.PreFinishedRoom is not null,
                save is null?"":RecordingJournal.SaveKey(JsonSerializationUtility.ToJson(save)),"native-continue",_run.Operations.Count+1));
            if(save?.PreFinishedRoom is null&&_run.Visits.LastOrDefault()?.RoomType=="Monster")
                Gap("未完战斗从原生开场重进；前次操作不拼接");
        }
        Save();
    }
    internal static void Entry(MapCoord coord,bool preFinished,bool saveGame)
    {
        if(!Enabled)return;var run=LiveState.Run;var last=_run!.Visits.LastOrDefault();
        if(!saveGame&&last is not null&&last.Act==run.CurrentActIndex&&last.Row==coord.row&&last.Col==coord.col)
        {_visit=last.Id;_run.Visits[^1]=last with{Entries=last.Entries+1};Save();return;}
        if(last is not null)_run.Visits[^1]=last with{Status=last.Gap is null?"left":"gap"};
        _visit=_run.Visits.Count+1;
        _run.Visits.Add(new(_visit,run.CurrentActIndex,coord.row+1,coord.row,coord.col,
            run.Map.GetPoint(coord)!.PointType.ToString(),null,null,1,"entering",null));Save();
    }
    internal static void Room(AbstractRoom room)
    {
        if(!Enabled||room.RoomType==RoomType.Map)return;
        if(_visit==0&&LiveState.Run.VisitedMapCoords.Count>0)Entry(LiveState.Run.VisitedMapCoords[^1],room.IsPreFinished,false);
        if(_visit==0)return;var index=_run!.Visits.FindIndex(v=>v.Id==_visit);var visit=_run.Visits[index];
        if(visit.RoomType is not null&&visit.RoomType!=room.RoomType.ToString())
        {
            _run.Visits[index]=visit with{Subrooms=(visit.Subrooms??[]).Append(room.RoomType+":"+room.ModelId).ToArray(),Gap="本轮未支持嵌套房间"};Save();return;
        }
        var supported=room.RoomType==RoomType.Monster&&visit.MapType=="Monster"||room.RoomType==RoomType.RestSite&&visit.MapType=="RestSite";
        _run.Visits[index]=visit with{RoomType=room.RoomType.ToString(),Model=room.ModelId?.ToString(),Status="entered",
            Gap=supported?visit.Gap:"本轮未支持的实际房间"};Save();
    }
    internal static RunOperation? Begin(string kind,RunArgument argument)
    {
        if(!Enabled||_visit==0||Recorder.CollectingChoices)return null;
        if(Pending is not null){Gap("尚未完成的房间操作被新的操作打断");return null;}
        var operation=new RunOperation(_run!.Operations.Count+1,_visit,kind,argument,RunResources.Capture(),null,"pending",[]);
        _run.Operations.Add(operation);Pending=operation;Save();return operation;
    }
    internal static async Task<T> Wrap<T>(Task<T> native,RunOperation? operation)
    {
        try{var value=await native;if(operation is not null)Runtime.Guard(()=>Complete(operation,value is bool success?success:null));return value;}
        catch{if(operation is not null)Runtime.Guard(()=>Missing(operation,"原生操作未完成"));throw;}
    }
    internal static async Task Wrap(Task native,RunOperation? operation,bool compareAfter=true)
    {
        try{await native;if(operation is not null)Runtime.Guard(()=>Complete(operation,null,compareAfter));}
        catch{if(operation is not null)Runtime.Guard(()=>Missing(operation,"原生操作未完成"));throw;}
    }
    internal static void Complete(RunOperation operation,bool? success=null,bool compareAfter=true)
    {
        if(!Enabled||Pending?.Sequence!=operation.Sequence)return;
        var current=Pending;Replace(current with{Status="complete",Success=success,After=compareAfter?RunResources.Capture():null});
        Pending=null;Save();Runtime.Log("run-operation-recorded",new{Run=_run!.Id,operation.Sequence,operation.Visit,operation.Kind,success});
    }
    internal static void Decision(RunDecision decision)
    {
        if(!Choosing)return;Pending=Pending! with{Decisions=Pending!.Decisions.Append(decision).ToArray()};Replace(Pending);Save();
    }
    internal static void BattleStarted(Clip clip)
    {
        if(!Enabled||_visit==0)return;
        var op=new RunOperation(_run!.Operations.Count+1,_visit,"battle",new(),null,null,"pending",[],clip.Id,clip.Battle,Progress:_entryProgress);
        _run.Operations.Add(op);_battleSequence=op.Sequence;Save();
    }
    internal static void BattleSealed(Clip clip)
    {
        if(!Enabled||_battleSequence==0)return;
        var op=_run!.Operations[_battleSequence-1];Replace(op with{Status="complete",Clip=clip.Id,Battle=clip.Battle});Save();
    }
    internal static void EntryContext(){if(Enabled)_entryProgress=RunResources.Progress();}
    internal static void MissingOperation(string kind,string reason)
    {
        if(!Enabled||_visit==0)return;
        Gap("记录"+kind+"失败："+reason);var index=_run!.Visits.FindIndex(v=>v.Id==_visit);
        _run.Visits[index]=_run.Visits[index] with{Gap="有未成功记录的房间操作",Status="gap"};Save();
    }
    internal static void Exit()
    {
        if(!Enabled)return;if(Pending is {} pending)Missing(pending,"退出时选择或转换尚未完成");Save();_run=null;Pending=null;
    }
    private static void Missing(RunOperation operation,string reason)
    {if(!Enabled)return;Replace(operation with{Status="missing"});if(Pending?.Sequence==operation.Sequence)Pending=null;Save();Runtime.Log("run-operation-missing",new{operation.Sequence,Reason=reason});}
    private static void Gap(string reason)
    {
        if(_run is null||_visit==0)return;
        _run.Operations.Add(new(_run.Operations.Count+1,_visit,"gap",new(),null,null,"missing",[]));
        Runtime.Log("run-recording-gap",new{Run=_run.Id,Visit=_visit,Reason=reason});
    }
    private static void Replace(RunOperation operation)=>_run!.Operations[operation.Sequence-1]=operation;
    private static void Save(){if(_run is not null)Store.Save(_run);}
}
