using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class Recorder
{
    internal static bool Active;
    internal static bool Attempted=>_attempted;
    private static bool _waiting,_attempted,_fresh;
    private static Clip? _clip;
    private static readonly List<Operation> Operations=[];
    private static Task? _entry;
    internal static bool CollectingChoices=>_waiting||Active;
    internal static int ChoiceBoundary=>_waiting?0:ActionMonitor.Pending is not null?Operations.Count+1:throw new InvalidDataException("choice-outside-initial-or-current-operation");
    internal static void NewRun(bool fresh=true){if(PlayerLoop.Active)return;Active=false;_waiting=false;_attempted=false;_fresh=fresh;_clip=null;_entry=null;Operations.Clear();ActionMonitor.Reset();ChoiceTape.Clear();RecordingClock.Reset();if(fresh)RecordedState.NewRun();Runtime.Status=fresh?"自动录制：铁甲／猎人／储君A0首场普通战斗，出牌／用药／选择":"ReplayMod 就绪。新建支持的普通战斗将自动录制；Continue不录制";}
    internal static void Entry(MapCoord coord,Task task)
    {
        if(!PlayerLoop.Active&&coord.row==1)_entry=task;
    }
    internal static void Checkpoint(SerializableRun save)
    {
        if(PlayerLoop.Active||!_fresh||_attempted||LiveState.Run.CurrentMapCoord?.row!=1||LiveState.Run.CurrentMapPoint?.PointType!=MapPointType.Monster)return;
        _attempted=true;LiveState.Format=RecordedState.Recording?3:7;LiveState.Supported();Runtime.CheckEnvironment();
        if(LiveState.Format>=5)RecordingClock.Start();else RecordingClock.Reset();
        var run=LiveState.Run;var coord=run.CurrentMapCoord!.Value;
        _clip=new(LiveState.Format,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,Runtime.GameHash,Runtime.ModHash,Runtime.Scope,
            Runtime.Mods,LiveState.Player.Character.Id.ToString(),run.Rng.ToSerializable().Seed??"",coord.row,coord.col,"",
            JsonSerializationUtility.ToJson(save),"",[],TestEncounter.Preparation,RecordedState.Recording?"STATE001-first-Defend-v1":null);
        _waiting=true;if(LiveState.Format>=4)ChoiceTape.Record();Runtime.Status="准备录制：等待首场战斗可操作，初始选择将一并记录";
    }
    internal static void Tick()
    {
        if(!_waiting)return;
        Codec.Require(_entry is null||!_entry.IsCanceled&&!_entry.IsFaulted,"normal-room-entry-failed");
        if(_entry?.IsCompletedSuccessfully!=true||ActionMonitor.StartTurnTask?.IsCompletedSuccessfully!=true||!LiveState.Playable)return;
        var ready=LiveState.Format>=5?new RecordingTiming("checkpoint-stopwatch-elapsed-realtime-v1",RecordingClock.Now,true,true,true):null;
        ChoiceTape.AssertBoundary(0);_clip=_clip! with{Encounter=((MegaCrit.Sts2.Core.Rooms.CombatRoom)LiveState.Run.CurrentRoom!).Encounter.Id.ToString(),Initial=LiveState.Capture(),Timing=ready};
        _waiting=false;Active=true;BattleEnd.Begin();RecordedState.Begin();Runtime.Status=RecordedState.Recording?"STATE录制中：请先打一张未升级的防御，再正常打赢首战":"录制中：0步。首战胜利自动保存；也可提前结束片段";Runtime.Log("recording-start",new{_clip.Id,_clip.Seed,_clip.Encounter,_clip.Preparation,_clip.Authority,Format=_clip.Format,InitialMechanics=LiveState.Mechanics()});
    }
    internal static void Append(Operation operation)
    {
        Codec.Require(Active&&Operations.Count<Codec.MaxOperations,"clip-operation-limit");Operations.Add(operation with{Sequence=Operations.Count+1});
        RecordedState.Append(_clip!,operation,Operations.Count);
        Runtime.Status=$"录制中：{Operations.Count}步（已核对完成）。保存退出自动封存";
        if(operation.Victory)Seal();
        else Codec.Require(Operations.Count<Codec.MaxOperations,"clip-operation-limit-before-victory");
    }
    internal static bool CanSeal=>Active&&!ActionMonitor.Outstanding&&Operations.Any(o=>o.Kind==OperationKind.EndTurn||o.Victory);
    internal static void Seal()
    {
        if(!Active)return;
        // A save/quit event can arrive before the next frame's observer tick. Finish a ready victory first.
        if(ActionMonitor.Pending is not null&&BattleEnd.Ready){ActionMonitor.Tick();if(!Active)return;}
        Codec.Require(!ActionMonitor.Outstanding,"wait-before-finishing-clip");
        var clip=_clip! with{Operations=Operations.ToArray(),Choices=LiveState.Format>=4?ChoiceTape.Snapshot():null};Codec.Validate(clip,Runtime.GameHash,Runtime.ModHash,Runtime.Scope);
        var at=System.Diagnostics.Stopwatch.GetTimestamp();RecordedState.Seal(clip);Runtime.Store.Seal(clip);
        var readback=Runtime.ReadClip(clip.Id);Codec.Require(Codec.Bytes(clip).SequenceEqual(Codec.Bytes(readback)),"sealed-production-readback-differs");
        Runtime.Log("clip-production-readback",new{clip.Id,clip.Format,Timing=readback.Timing,TimedOperations=readback.Operations.Count(o=>o.Timing is not null),
            Times=readback.Operations.Select(o=>new{o.Sequence,o.Timing}).ToArray(),OldMissingTimeIsUnknown=true,ExactPayloadMatched=true});
        var sealMs=System.Diagnostics.Stopwatch.GetElapsedTime(at).TotalMilliseconds;Active=false;_clip=null;
        var victory=clip.Operations[^1].Victory;
        Runtime.Status=victory?$"战斗胜利 · 已自动保存 {clip.Id[..8]}：{clip.Operations.Length}步。可保存退出后从主菜单观看":$"已保存片段 {clip.Id[..8]}：{clip.Operations.Length}步。主菜单 Replay 可打开";
        Runtime.Log("clip-sealed",new{clip.Id,clip.Format,Count=clip.Operations.Length,Victory=victory,AutomaticVictorySeal=victory,FinalOperation=clip.Operations[^1].Sequence,FinalStateHash=Codec.Hash(System.Text.Encoding.UTF8.GetBytes(clip.Operations[^1].After)),SealMilliseconds=sealMs,SnapshotHashes=clip.Operations.Select(o=>Codec.Hash(System.Text.Encoding.UTF8.GetBytes(o.After))).ToArray()});
    }
    internal static void Fail(string reason){if(!Active&&!_waiting&&_clip is null)return;Active=false;_waiting=false;_clip=null;Runtime.Status="本段未封存："+reason;Runtime.Log("recording-unsupported-or-failed",new{Reason=reason,CompletedOperations=Operations.Count});}
}
