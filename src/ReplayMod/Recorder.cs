using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class Recorder
{
    internal static bool Active;
    internal static bool Attempted=>_attempted;
    private static bool _waiting,_attempted,_captureEntry,_release,_enabled;
    private static Clip? _clip;
    private static string? _continueSave;
    private static readonly List<Operation> Operations=[];
    private static Task? _entry;
    internal static bool CollectingChoices=>_waiting||Active;
    internal static int ChoiceBoundary=>_waiting?0:ActionMonitor.Pending is not null?Operations.Count+1:throw new InvalidDataException("choice-outside-initial-or-current-operation");

    private static void ResetSegment(bool preserveAuthority=false)
    {
        Active=false;_waiting=false;_captureEntry=false;_release=false;_clip=null;_entry=null;Operations.Clear();
        ActionMonitor.Reset();ChoiceTape.Clear();RecordingClock.Reset();OrbState.Reset();if(!preserveAuthority)RecordedState.Release();
    }
    internal static void NewRun(bool fresh=true)
    {
        if(PlayerLoop.Active)return;
        ResetSegment();_enabled=false;_attempted=false;_continueSave=null;
        if(fresh){Runtime.CheckEnvironment();Runtime.Journal.NewRun();RecordedState.NewRun();_enabled=true;}
        Runtime.Status="自动录制：五角色A0首幕普通战斗，每场胜利独立保存；支持保存退出后继续";
    }
    internal static void Continue(SerializableRun save)
    {
        if(PlayerLoop.Active)return;
        NewRun(false);Runtime.CheckEnvironment();_continueSave=JsonSerializationUtility.ToJson(save);Runtime.Journal.Continue(_continueSave);
        _enabled=true;
        Runtime.Log("recording-continue",new{RunId=Runtime.Journal.CurrentRun,MatchedObservedSave=Runtime.Journal.ContinueMatched,
            save.StartTime,save.CurrentActIndex,PreFinished=save.PreFinishedRoom is not null,ReusesOldOperations=false});
    }
    // BOTH normal travel and Continue's saveGame:false entrance pass here, before room creation.
    internal static void BeginEntry(MapCoord coord,bool preFinished,bool saveGame)
    {
        if(PlayerLoop.Active||!_enabled)return;
        if(Active||_waiting)Interrupt();
        ResetSegment(preserveAuthority:true);_attempted=false;
        var run=LiveState.Run;
        if(preFinished||run.CurrentActIndex!=0||run.AscensionLevel!=0||run.Modifiers.Count!=0||run.CurrentMapPoint?.PointType!=MapPointType.Monster)
        {
            _continueSave=null;Runtime.Status="本房间不录制；进入下一场受支持的首幕普通战斗时自动恢复";return;
        }
        _captureEntry=true;
        if(!saveGame)
        {
            var json=_continueSave;_continueSave=null;
            if(json is null){_captureEntry=false;Runtime.Status="当前Continue房间缺少可验证的房间前存档，本场未录；下一普通战斗恢复";return;}
            var parsed=JsonSerializationUtility.FromJson<SerializableRun>(json);
            Codec.Require(parsed.Success&&parsed.SaveData is not null&&parsed.SaveData.PreFinishedRoom is null&&
                parsed.SaveData.VisitedMapCoords.LastOrDefault().Equals(coord),"continue-checkpoint-not-current-unfinished-room");
            Capture(parsed.SaveData!,Runtime.Journal.ContinueMatched?"continue-known":"continue-unmatched");
        }
        else _continueSave=null;
    }
    internal static void Entry(Task task){if(!PlayerLoop.Active&&(_captureEntry||_waiting))_entry=task;}
    internal static void Checkpoint(SerializableRun save)
    {
        if(PlayerLoop.Active||!_enabled)return;
        Runtime.Journal.Observe(JsonSerializationUtility.ToJson(save));
        if(_captureEntry&&!_waiting&&save.PreFinishedRoom is null&&LiveState.Run.CurrentRoom is null)Capture(save,"map");
    }
    private static void Capture(SerializableRun save,string source)
    {
        _captureEntry=false;_attempted=true;LiveState.Format=RecordedState.Recording?3:10;LiveState.Supported();Runtime.CheckEnvironment();
        if(LiveState.Format>=5)RecordingClock.Start();else RecordingClock.Reset();
        var run=LiveState.Run;var coord=run.CurrentMapCoord!.Value;
        var battle=LiveState.Format>=9?Runtime.Journal.Begin(run.CurrentActIndex,coord.row,coord.col,coord.row+1,save.StartTime,source):null;
        _clip=new(LiveState.Format,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,Runtime.GameHash,Runtime.ModHash,Runtime.Scope,
            Runtime.Mods,LiveState.Player.Character.Id.ToString(),run.Rng.ToSerializable().Seed??"",coord.row,coord.col,"",
            JsonSerializationUtility.ToJson(save),"",[],TestEncounter.Preparation,RecordedState.Recording?"STATE001-first-Defend-v1":null,Battle:battle);
        _waiting=true;if(LiveState.Format>=4)ChoiceTape.Record();Runtime.Status="准备录制：等待本场可操作，初始选择将一并记录";
        Runtime.Log("recording-room-checkpoint",new{_clip.Id,_clip.Battle,coord.row,coord.col,save.StartTime,BeforeRoomCreation=true,Source=source});
    }
    internal static void Tick()
    {
        if(_release){ResetSegment();return;}
        if(_captureEntry&&_entry?.IsCompleted==true)
        {
            _captureEntry=false;_attempted=true;Runtime.Status="本场未取得房间前checkpoint，未录制；下一受支持普通战斗重试";
            Runtime.Log("recording-entry-missing-checkpoint",new{Faulted=_entry.IsFaulted,Cancelled=_entry.IsCanceled});return;
        }
        if(!_waiting)return;
        Codec.Require(_entry is null||!_entry.IsCanceled&&!_entry.IsFaulted,"normal-room-entry-failed");
        if(_entry?.IsCompletedSuccessfully!=true||ActionMonitor.StartTurnTask?.IsCompletedSuccessfully!=true||!LiveState.Playable)return;
        var ready=LiveState.Format>=5?new RecordingTiming("checkpoint-stopwatch-elapsed-realtime-v1",RecordingClock.Now,true,true,true):null;
        ChoiceTape.AssertBoundary(0);_clip=_clip! with{Encounter=((MegaCrit.Sts2.Core.Rooms.CombatRoom)LiveState.Run.CurrentRoom!).Encounter.Id.ToString(),Initial=LiveState.Capture(),Timing=ready};
        _waiting=false;Active=true;BattleEnd.Begin();RecordedState.Begin();
        Runtime.Status=RecordedState.Recording?"STATE录制中：请先打一张未升级的防御，再正常打赢首战":"本场录制中：0步。胜利自动保存，可正常领奖后继续下一场";
        Runtime.Log("recording-start",new{_clip.Id,_clip.Seed,_clip.Encounter,_clip.Preparation,_clip.Authority,_clip.Battle,Format=_clip.Format,InitialMechanics=LiveState.Mechanics()});
    }
    internal static void Append(Operation operation)
    {
        Codec.Require(Active&&Operations.Count<Codec.MaxOperations,"clip-operation-limit");Operations.Add(operation with{Sequence=Operations.Count+1});
        RecordedState.Append(_clip!,operation,Operations.Count);Runtime.Status=$"本场录制中：{Operations.Count}步。胜利或保存退出时封存";
        if(operation.Victory)Seal();else Codec.Require(Operations.Count<Codec.MaxOperations,"clip-operation-limit-before-victory");
    }
    internal static bool CanSeal=>Active&&!ActionMonitor.Outstanding&&!ChoiceTape.Waiting&&Operations.Count>0;
    internal static void Seal()
    {
        if(!Active)return;
        if(ActionMonitor.Pending is not null&&BattleEnd.Ready){ActionMonitor.Tick();if(!Active)return;}
        Codec.Require(!ActionMonitor.Outstanding&&!ChoiceTape.Waiting,"wait-before-finishing-clip");
        Publish(false);
    }
    internal static void Interrupt()
    {
        if(Active)
        {
            if(ActionMonitor.Pending is not null&&BattleEnd.Ready){ActionMonitor.Tick();if(!Active)return;}
            if(Operations.Count>0&&LiveState.Format>=9)Publish(true);
            else if(CanSeal)Seal();
            else Runtime.Log("recording-interrupted-without-playable-prefix",new{Id=_clip?.Id,_clip?.Battle,CompletedOperations=Operations.Count,InitialReady=Active,ChoiceInFlight=ChoiceTape.Waiting});
        }
        else if(_waiting)Runtime.Log("recording-opening-interrupted",new{Id=_clip?.Id,_clip?.Battle,NoCompleteInitialState=true});
        ResetSegment();
    }
    internal static void EndRun(){try{Interrupt();}finally{_enabled=false;_continueSave=null;}}
    private static void Publish(bool interrupted)
    {
        Codec.Require(Operations.Count>0,"no-completed-operations-to-seal");var victory=Operations[^1].Victory;
        var clip=_clip! with{Operations=Operations.ToArray(),Choices=LiveState.Format>=4?(interrupted?ChoiceTape.CompletedPrefix(Operations.Count):ChoiceTape.Snapshot()):null,
            Battle=_clip!.Battle is {} battle?battle with{Completion=victory?"victory":"interrupted"}:null};
        Codec.Validate(clip,Runtime.GameHash,Runtime.ModHash,Runtime.Scope);
        var at=System.Diagnostics.Stopwatch.GetTimestamp();RecordedState.Seal(clip);Runtime.Store.Seal(clip);
        var readback=Runtime.ReadClip(clip.Id);Codec.Require(Codec.Bytes(clip).SequenceEqual(Codec.Bytes(readback)),"sealed-production-readback-differs");
        Runtime.Log("clip-production-readback",new{clip.Id,clip.Format,clip.Battle,Timing=readback.Timing,TimedOperations=readback.Operations.Count(o=>o.Timing is not null),ExactPayloadMatched=true});
        Active=false;_waiting=false;_clip=null;_release=true;
        Runtime.Status=victory?$"战斗胜利 · 已保存 {clip.Id[..8]}：{clip.Operations.Length}步。可领奖后继续，下一普通战斗自动录制":$"已保存中断片段 {clip.Id[..8]}：{clip.Operations.Length}个完整操作；未完成的动作或选择不拼入";
        Runtime.Log("clip-sealed",new{clip.Id,clip.Format,clip.Battle,Count=clip.Operations.Length,Victory=victory,AutomaticVictorySeal=victory,Interrupted=interrupted,
            FinalOperation=clip.Operations[^1].Sequence,FinalStateHash=Codec.Hash(System.Text.Encoding.UTF8.GetBytes(clip.Operations[^1].After)),SealMilliseconds=System.Diagnostics.Stopwatch.GetElapsedTime(at).TotalMilliseconds});
    }
    internal static void Fail(string reason)
    {
        if(!Active&&!_waiting&&_clip is null)return;
        Runtime.Log("recording-unsupported-or-failed",new{Reason=reason,Id=_clip?.Id,_clip?.Battle,CompletedOperations=Operations.Count,NextSupportedRoomCanRecord=true});
        Active=false;_waiting=false;_captureEntry=false;_clip=null;_release=true;Runtime.Status="本段未封存，下一普通战斗会重试："+reason;
    }
}
