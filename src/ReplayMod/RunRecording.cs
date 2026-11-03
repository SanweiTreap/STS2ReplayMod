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
        result["acts"]=acts;
        if(RunManager.Instance.DebugOnlyGetState()?.CurrentRoom is MerchantRoom)
            result["merchant_inventory"]=JsonNode.Parse(RunMerchants.Offer());
        if(RunEvents.InSupportedEvent)result["event_state"]=JsonNode.Parse(RunEvents.State());
        return result.ToJsonString(Codec.Json);
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
    private static bool _restoredEntry,_unknownLoad;
    private static bool _saveExit;
    internal sealed record SaveLocation(string Run,int Visit,int Attempt,int[] Path);
    internal static bool Enabled=>_run is not null&&!PlayerLoop.Active;
    internal static bool Choosing=>Enabled&&Pending?.Kind is "reward" or "rest" or "treasure-pick" or "merchant-buy" or "merchant-remove" or "event-choice";
    internal static void Attach(bool continuation,SerializableRun? save=null,string? loadedJson=null)
    {
        if(PlayerLoop.Active||Runtime.Journal.CurrentRun is not {} id)return;
        Pending=null;_battleSequence=0;_visit=0;_entryProgress=null;_restoredEntry=false;_unknownLoad=false;_saveExit=false;
        RunEndings.Clear();
        var path=Path.Combine(Runtime.Root,"runs",id+".json");
        _run=File.Exists(path)?Store.Read(id):new(1,id,Runtime.GameHash,Runtime.ModHash,Runtime.Scope,
            save?.Players[0].CharacterId?.ToString()??LiveState.Player.Character.Id.ToString(),DateTimeOffset.UtcNow,[],[],[],new(),Opening:continuation?null:RunOpening.Take());
        if(continuation)
        {
            _visit=_run.Visits.LastOrDefault()?.Id??0;
            var unfinished=_run.Operations.LastOrDefault();
            if(unfinished is {Status:"pending"})Replace(unfinished with{Status="missing"});
            if(_run.Branches is {} branches)
            {
                RunSavePoint? anchor=null;
                try{if(loadedJson is not null)anchor=RunRoutes.Restore(_run,RecordingJournal.NativeFingerprint(loadedJson));}
                catch(InvalidDataException ex) when(ex.Message=="run-load-anchor-ambiguous")
                {Runtime.Log("run-load-anchor-unresolved",new{Run=_run.Id,Reason=ex.Message,OldAttemptsRetained=true});}
                if(anchor is not null){_visit=anchor.Visit;_restoredEntry=true;}
                else {_visit=0;_unknownLoad=true;branches.Selected.Clear();branches.ActiveAttempt=0;}
                Runtime.Log("run-attempt-restored",new{Run=_run.Id,Anchor=anchor?.Id,Visit=anchor?.Visit,
                    Attempt=branches.ActiveAttempt,MatchedSuccessfulNativeSave=anchor is not null,RawOperationsRetained=_run.Operations.Count});
            }
            _run.Continuations.Add(new(DateTimeOffset.UtcNow,_visit,save?.PreFinishedRoom is not null,
                save is null?"":RecordingJournal.SaveKey(JsonSerializationUtility.ToJson(save)),_unknownLoad?"unmatched-anchor":"native-continue",_run.Operations.Count+1,
                _run.Branches is {ActiveAttempt:>0} b?b.ActiveAttempt:null,Progress:RunResources.Progress()));
            if(_run.Branches is null&&save?.PreFinishedRoom is null&&_run.Visits.LastOrDefault()?.RoomType is "Monster" or "Elite" or "Boss")
                Gap("未完战斗从原生开场重进；前次操作不拼接");
        }
        NoteLifecycle(continuation?"continued":"in-progress",continuation?"native-SetUpSavedSingleplayer":"native-new-run",act:save?.CurrentActIndex);
    }
    internal static void SaveExitIntent(){if(Enabled)_saveExit=true;}
    internal static void NoteLifecycle(string kind,string source,string? state=null,int? act=null)
    {
        if(!Enabled)return;
        _run=_run! with{Lifecycle=_run!.Lifecycle??[]};
        var entry=new RunLifecycleEvent(kind,DateTimeOffset.UtcNow,source,_visit,
            act??RunManager.Instance.DebugOnlyGetState()?.CurrentActIndex??_run.Visits.LastOrDefault()?.Act??0,
            _run.Operations.Count+1,CurrentAttempt,state);
        if(!RunLifecycle.Append(_run.Lifecycle!,entry))return;
        Save();Runtime.Log("run-lifecycle-recorded",new{Run=_run.Id,entry.Kind,entry.Source,entry.Visit,entry.Act,entry.BeforeOperation,entry.Attempt,
            NativeTerminal=RunLifecycle.Terminal(kind),StateFrozen=state is not null});
    }
    internal static SaveLocation? SavePosition()=>Enabled&&_visit>0&&_run!.Branches is {ActiveAttempt:>0} b?
        new(_run.Id,_visit,b.ActiveAttempt,b.Selected.ToArray()):null;
    internal static void NativeSaved(SaveLocation? location,string raw,string native,bool preFinished)
    {
        if(location is null||!Enabled||_run!.Id!=location.Run||_run.Branches is null)return;
        var point=RunRoutes.Observe(_run,location.Visit,location.Attempt,location.Path,RecordingJournal.NativeFingerprint(raw),
            RecordingJournal.NativeFingerprint(native),RecordingJournal.SaveKey(native),preFinished);
        Save();Runtime.Log("run-save-point-observed",new{Run=_run.Id,point.Id,point.Visit,point.Attempt,point.PreFinished,Path=point.Path,
            SuccessfulNativeSave=true,ExactNativeLoadFingerprint=true});
    }
    internal static void Entry(MapCoord coord,bool preFinished,bool saveGame)
    {
        if(!Enabled)return;var run=LiveState.Run;var last=_run!.Visits.LastOrDefault();
        if(_restoredEntry)
        {
            var index=_run.Visits.FindIndex(v=>v.Id==_visit);var restored=_run.Visits[index];
            Codec.Require(!saveGame&&restored.Act==run.CurrentActIndex&&restored.Row==coord.row&&restored.Col==coord.col,"run-restored-entry-does-not-match-save");
            _run.Visits[index]=restored with{Entries=restored.Entries+1};_restoredEntry=false;Save();return;
        }
        if(_run.Branches is null&&!saveGame&&last is not null&&last.Act==run.CurrentActIndex&&last.Row==coord.row&&last.Col==coord.col)
        {_visit=last.Id;_run.Visits[^1]=last with{Entries=last.Entries+1};Save();return;}
        if(last is not null)_run.Visits[^1]=last with{Status=last.Gap is null?"left":"gap"};
        _visit=_run.Visits.Count+1;
        _run.Visits.Add(new(_visit,run.CurrentActIndex,coord.row+1,coord.row,coord.col,
            run.Map.GetPoint(coord)!.PointType.ToString(),null,null,1,"entering",null,
            ActModel:run.Act.Id.ToString(),ActFloor:run.ActFloor,TotalFloor:run.TotalFloor));
        if(_run.Branches is {} branch)RunRoutes.StartAttempt(_run,_visit,branch.ActiveAttempt>0?branch.ActiveAttempt:null,null,_unknownLoad?"unmatched-native-load":"entry");
        if(_unknownLoad)
        {
            var current=_run.Continuations[^1];_run.Continuations[^1]=current with{Visit=_visit,Attempt=_run.Branches!.ActiveAttempt};
            Gap("缺少成功原生存盘的恢复位置；保留独立尝试");_unknownLoad=false;
        }
        Save();
    }
    internal static void Room(AbstractRoom room)
    {
        if(!Enabled||room.RoomType==RoomType.Map)return;
        if(room.IsVictoryRoom&&_run!.Visits.LastOrDefault()?.MapType!="Victory")
        {
            var run=LiveState.Run;var previous=_run.Visits.LastOrDefault();
            Codec.Require(run.CurrentActIndex==2&&previous is {RoomType:"Boss"}&&run.CurrentMapCoord is not null,
                "final-event-without-native-final-boss");
            _run.Visits[^1]=previous! with{Status=previous!.Gap is null?"left":"gap"};
            var coord=run.CurrentMapCoord!.Value;_visit=_run.Visits.Count+1;
            _run.Visits.Add(new(_visit,run.CurrentActIndex,run.ActFloor,coord.row,coord.col,"Victory",null,null,1,"entering",null,
                ActModel:run.Act.Id.ToString(),ActFloor:run.ActFloor,TotalFloor:run.TotalFloor));
            if(_run.Branches is {} branches)RunRoutes.StartAttempt(_run,_visit,branches.ActiveAttempt,null,"native-final-event");
            Runtime.Log("run-final-event-visit-created",new{Visit=_visit,Act=run.CurrentActIndex,NativeVictoryRoom=true,
                SameMapCoord=true,SeparateFromBoss=true});
        }
        if(_visit==0&&LiveState.Run.VisitedMapCoords.Count>0)Entry(LiveState.Run.VisitedMapCoords[^1],room.IsPreFinished,false);
        if(_visit==0)return;var index=_run!.Visits.FindIndex(v=>v.Id==_visit);var visit=_run.Visits[index];
        visit=visit with{ActModel=LiveState.Run.Act.Id.ToString(),ActFloor=LiveState.Run.ActFloor,TotalFloor=LiveState.Run.TotalFloor};
        if(room is CombatRoom {ParentEventId:not null,ShouldResumeParentEventAfterCombat:true} nested&&
            visit.MapType=="Unknown"&&visit.Model==nested.ParentEventId.ToString())
        {
            _run.Visits[index]=visit with{Subrooms=(visit.Subrooms??[]).Append("Combat:"+nested.ModelId).ToArray()};Save();
            Runtime.Log("run-event-subroom-entered",new{Visit=_visit,Attempt=CurrentAttempt,Parent=nested.ParentEventId.ToString(),
                Encounter=nested.ModelId.ToString(),SameMapVisit=true,nested.IsPreFinished});return;
        }
        var firstRoomInAttempt=false;
        if(_run.Branches is {ActiveAttempt:>0} branch)
        {
            var at=branch.Attempts.FindIndex(a=>a.Id==branch.ActiveAttempt);var attempt=branch.Attempts[at];
            if(attempt.RoomType is null){firstRoomInAttempt=true;branch.Attempts[at]=attempt with{RoomType=room.RoomType.ToString(),Model=room.ModelId?.ToString()};}
        }
        if(visit.RoomType is not null&&visit.RoomType!=room.RoomType.ToString()&&!firstRoomInAttempt)
        {
            _run.Visits[index]=visit with{Subrooms=(visit.Subrooms??[]).Append(room.RoomType+":"+room.ModelId).ToArray(),Gap="本轮未支持嵌套房间"};Save();return;
        }
        var supported=room is CombatRoom&&LiveState.CombatRoomType(room.RoomType)&&
            (room.RoomType.ToString()==visit.MapType||room.RoomType==RoomType.Monster&&visit.MapType=="Unknown")||
            room.RoomType==RoomType.RestSite&&visit.MapType=="RestSite"||room.RoomType is RoomType.Treasure or RoomType.Shop||
            room.IsVictoryRoom&&visit.MapType=="Victory"||
            room is EventRoom {CanonicalEvent:not MegaCrit.Sts2.Core.Models.AncientEventModel}&&visit.MapType=="Unknown"||
            room is EventRoom {CanonicalEvent:MegaCrit.Sts2.Core.Models.AncientEventModel}&&visit.MapType=="Ancient"&&
                (visit.Act is 1 or 2||_run.Opening is not null);
        _run.Visits[index]=visit with{RoomType=room.RoomType.ToString(),Model=room.ModelId?.ToString(),Status="entered",
            Gap=supported?visit.Gap:"本轮未支持的实际房间"};Save();
    }
    internal static RunOperation? Begin(string kind,RunArgument argument)
    {
        if(!Enabled||_visit==0||Recorder.CollectingChoices)return null;
        if(Pending is not null){Gap("尚未完成的房间操作被新的操作打断");return null;}
        var operation=new RunOperation(_run!.Operations.Count+1,_visit,kind,argument,RunResources.Capture(),null,"pending",[],
            Progress:RunResources.Progress(),Attempt:CurrentAttempt);
        RunRoutes.Append(_run,operation);Pending=operation;Save();return operation;
    }
    internal static async Task<T> Wrap<T>(Task<T> native,RunOperation? operation)
    {
        var owner=_run?.Id;
        try{var value=await native;if(operation is not null&&_run?.Id==owner)Runtime.Guard(()=>Complete(operation,value is bool success?success:null));return value;}
        catch{if(operation is not null&&_run?.Id==owner)Runtime.Guard(()=>Missing(operation,"原生操作未完成"));throw;}
    }
    internal static async Task Wrap(Task native,RunOperation? operation,bool compareAfter=true)
    {
        var owner=_run?.Id;
        try{await native;if(operation is not null&&_run?.Id==owner)Runtime.Guard(()=>Complete(operation,null,compareAfter));}
        catch{if(operation is not null&&_run?.Id==owner)Runtime.Guard(()=>Missing(operation,"原生操作未完成"));throw;}
    }
    internal static void Complete(RunOperation operation,bool? success=null,bool compareAfter=true)
    {
        if(!Enabled||Pending?.Sequence!=operation.Sequence)return;
        var current=Pending;var terminal=RunEndings.RecordingTerminal(current);
        Replace(current with{Status="complete",Success=success,Terminal=terminal is null?null:current.Kind=="abandon"?"abandoned":"victory",
            After=terminal??(compareAfter?RunResources.Capture():null)});
        Pending=null;Save();Runtime.Log("run-operation-recorded",new{Run=_run!.Id,operation.Sequence,operation.Visit,operation.Kind,success});
        if(current.Kind=="abandon"&&terminal is not null)Runtime.Status="主动放弃已记录；已保留之前完成的战斗动作，可正常结算返回主菜单";
    }
    internal static void Decision(RunDecision decision)
    {
        if(!Choosing)return;Pending=Pending! with{Decisions=Pending!.Decisions.Append(decision).ToArray()};Replace(Pending);Save();
    }
    internal static void Describe(RunArgument argument)
    {if(Pending is not {} pending)return;Pending=pending with{Argument=argument};Replace(Pending);Save();}
    internal static void BattleStarted(Clip clip)
    {
        if(!Enabled||_visit==0)return;
        var op=new RunOperation(_run!.Operations.Count+1,_visit,"battle",new(),null,null,"pending",[],clip.Id,clip.Battle,Progress:_entryProgress,Attempt:CurrentAttempt);
        RunRoutes.Append(_run,op);_battleSequence=op.Sequence;Save();
    }
    internal static void BattleSealed(Clip clip)
    {
        if(!Enabled||_battleSequence==0)return;
        var op=_run!.Operations[_battleSequence-1];Replace(op with{Status="complete",Clip=clip.Id,Battle=clip.Battle});Save();
    }
    internal static void EntryContext(string? progress=null){if(Enabled)_entryProgress=progress??RunResources.Progress();}
    internal static void MissingOperation(string kind,string reason)
    {
        if(!Enabled||_visit==0)return;
        Gap("记录"+kind+"失败："+reason);var index=_run!.Visits.FindIndex(v=>v.Id==_visit);
        _run.Visits[index]=_run.Visits[index] with{Gap="有未成功记录的房间操作",Status="gap"};Save();
    }
    internal static void Unsupported(string kind,string reason)
    {if(Pending is {} pending)Missing(pending,reason);MissingOperation(kind,reason);}
    internal static void Exit()
    {
        if(!Enabled)return;if(Pending is {} pending)Missing(pending,"退出时选择或转换尚未完成");
        NoteLifecycle(_saveExit?"saved-exit":"interrupted",_saveExit?"native-pause-CloseToMenu":"native-CleanUp-without-terminal");
        Save();_run=null;Pending=null;_saveExit=false;
    }
    private static void Missing(RunOperation operation,string reason)
    {if(!Enabled)return;Replace(operation with{Status="missing"});if(Pending?.Sequence==operation.Sequence)Pending=null;Save();Runtime.Log("run-operation-missing",new{operation.Sequence,Reason=reason});}
    private static void Gap(string reason)
    {
        if(_run is null||_visit==0)return;
        RunRoutes.Append(_run,new(_run.Operations.Count+1,_visit,"gap",new(),null,null,"missing",[],Attempt:CurrentAttempt));
        Runtime.Log("run-recording-gap",new{Run=_run.Id,Visit=_visit,Reason=reason});
    }
    private static void Replace(RunOperation operation)=>_run!.Operations[operation.Sequence-1]=operation;
    private static int? CurrentAttempt=>_run?.Branches is {ActiveAttempt:>0} b?b.ActiveAttempt:null;
    private static void Save(){if(_run is not null)Store.Save(_run);}
}
