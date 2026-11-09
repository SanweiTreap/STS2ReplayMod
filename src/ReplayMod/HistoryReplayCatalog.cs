using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReplayMod;

internal sealed record HistoryReplayKey(long StartTime,string Fingerprint)
{
    internal static HistoryReplayKey? Read(string file,JsonObject history)
    {
        if(history["start_time"]?.GetValue<long>() is not >0L||history["players"] is not JsonArray {Count:1} players||
            players[0]?["id"]?.GetValue<ulong>()!=1)return null;
        var start=history["start_time"]!.GetValue<long>();
        if(file!=FileName(start))return null;
        var identity=history.DeepClone().AsObject();identity.Remove("schema_version");
        return new(start,RecordingJournal.NativeFingerprint(identity.ToJsonString()));
    }

    internal static string FileName(long start)=>FormattableString.Invariant($"{start}.run");
}

internal sealed record HistoryFileStamp(long Bytes,long LastWriteTicks)
{
    internal static HistoryFileStamp? Read(string path)
    {
        var file=new FileInfo(path);
        return file.Exists?new(file.Length,file.LastWriteTimeUtc.Ticks):null;
    }
}

internal sealed record HistoryReplayLink(string Run,string Clip,int Start,string Game,string RecordedMod,
    HistoryFileStamp RunFile,HistoryFileStamp ClipFile);

// The history UI reads a single small index entry and two file stamps. The
// original run and clip are read only when preparing the index or starting replay.
internal sealed class HistoryReplayCatalog(string root,string scope,string? indexRoot=null)
{
    private string StartDirectory(long start)=>Path.Combine(indexRoot??Path.Combine(root,"history-index"),scope,
        FormattableString.Invariant($"{start}"));
    private string Entry(HistoryReplayKey key)=>Path.Combine(StartDirectory(key.StartTime),key.Fingerprint+".json");
    private string RunPath(string id)=>Path.Combine(root,"runs",id+".json");
    private string ClipPath(string id)=>Path.Combine(root,"clips",id+".json");
    internal bool HasStart(long start)=>Directory.Exists(StartDirectory(start));

    internal HistoryReplayLink? Find(HistoryReplayKey key,string game,Func<string,bool> compatible)
    {
        var link=Read(Entry(key));
        return link is {RunFile:not null,ClipFile:not null}&&link.Game==game&&compatible(link.RecordedMod)&&link.Start>=0&&
            Guid.TryParseExact(link.Run,"N",out _)&&Guid.TryParseExact(link.Clip,"N",out _)&&
            link.RunFile==HistoryFileStamp.Read(RunPath(link.Run))&&link.ClipFile==HistoryFileStamp.Read(ClipPath(link.Clip))?link:null;
    }

    internal HistoryReplayLink? Publish(HistoryReplayKey key,JsonObject history,RunDocument raw,Func<string,Clip> readClip)
    {
        if(raw.Scope!=scope||!TerminalMatches(raw,history))return null;
        var route=RunRoutes.Playback(raw);
        var anchor=RunStore.FirstBattleIndex(route);
        if(anchor<0)return null;
        var opening=RunStore.HasNativeOpening(route);
        var start=opening?1:route.Operations[anchor].Sequence;
        if(RunStore.HasUnrecordedAfter(route,start)||RunStore.ResourceDiscontinuities(route).Any(n=>n>start))return null;
        var op=route.Operations[anchor];
        if(op.Battle is not {} battle||battle.RunId!=raw.Id||battle.RunStartTime!=key.StartTime||
            raw.Operations.Any(o=>o.Battle is {} b&&b.RunStartTime!=key.StartTime))return null;
        var runFile=HistoryFileStamp.Read(RunPath(raw.Id));
        var clipFile=HistoryFileStamp.Read(ClipPath(op.Clip!));
        if(runFile is null||clipFile is null)return null;
        var clip=readClip(op.Clip!);
        if(clip.Battle!=battle||clip.Scope!=scope||clip.GameHash!=raw.GameHash||clip.Character!=raw.Character||NativePlaybackSupport.Reason(clip) is not null||
            !CheckpointMatches(clip,key,history))return null;
        if(runFile!=HistoryFileStamp.Read(RunPath(raw.Id))||clipFile!=HistoryFileStamp.Read(ClipPath(clip.Id)))return null;
        var link=new HistoryReplayLink(raw.Id,clip.Id,opening?0:op.SourceSequence??op.Sequence,raw.GameHash,raw.ModHash,runFile,clipFile);
        var path=Entry(key);
        if(File.Exists(path)&&Read(path)?.Run is {} previous&&previous!=raw.Id)
        {
            // Distinct recordings with identical native evidence are ambiguous.
            Write(path,Codec.Bytes(link with{Run=""}));return null;
        }
        Write(path,Codec.Bytes(link));return link;
    }

    private static HistoryReplayLink? Read(string path)=>HistoryFileStamp.Read(path) is {Bytes:>0 and <=4096}?
        JsonSerializer.Deserialize<HistoryReplayLink>(File.ReadAllBytes(path),Codec.Json):null;

    private static void Write(string path,byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var pending=path+"."+Guid.NewGuid().ToString("N")+".pending";
        try{File.WriteAllBytes(pending,bytes);File.Move(pending,path,true);}
        finally{if(File.Exists(pending))File.Delete(pending);}
    }

    private static bool CheckpointMatches(Clip clip,HistoryReplayKey key,JsonObject history)
    {
        var saved=JsonNode.Parse(clip.Checkpoint);
        return saved?["start_time"]?.GetValue<long>()==key.StartTime&&
            JsonNode.DeepEquals(saved["rng"]?["seed"],history["seed"])&&
            JsonNode.DeepEquals(saved["game_mode"],history["game_mode"])&&
            JsonNode.DeepEquals(saved["ascension"],history["ascension"])&&
            saved["players"] is JsonArray {Count:1} players&&
            JsonNode.DeepEquals(players[0]?["net_id"],history["players"]?[0]?["id"])&&
            JsonNode.DeepEquals(players[0]?["character_id"],history["players"]?[0]?["character"]);
    }

    private static bool TerminalMatches(RunDocument raw,JsonObject history)
    {
        var kind=history["win"]?.GetValue<bool>()==true?"victory":history["was_abandoned"]?.GetValue<bool>()==true?"abandoned":"death";
        if(raw.Lifecycle?.LastOrDefault() is not {State:{} state} terminal||terminal.Kind!=kind)return false;
        var saved=JsonNode.Parse(state);
        var player=saved?["players"]?[0];var actual=history["players"]?[0];
        if(player is null||actual is null||!JsonNode.DeepEquals(saved!["rng"]?["seed"],history["seed"])||
            !JsonNode.DeepEquals(saved["ascension"],history["ascension"])||
            !JsonNode.DeepEquals(player["net_id"],actual["id"])||!JsonNode.DeepEquals(player["character_id"],actual["character"])||
            !new[]{"deck","relics","max_potion_slot_count"}.All(field=>JsonNode.DeepEquals(player[field],actual[field]))||
            !JsonNode.DeepEquals(player["potions"]??new JsonArray(),actual["potions"]??new JsonArray())||
            saved["acts"] is not JsonArray acts||history["acts"] is not JsonArray historyActs||
            !acts.Select(a=>a?["id"]?.GetValue<string>()).SequenceEqual(historyActs.Select(a=>a?.GetValue<string>())))return false;
        if(saved["map_point_history"] is not JsonArray route||history["map_point_history"] is not JsonArray nativeRoute)return false;
        var expected=route.DeepClone().AsArray();var observed=nativeRoute.DeepClone().AsArray();
        // OnEnded writes these final-room facts after Replay freezes its terminal state.
        var last=LastMapPoint(expected);var nativeLast=LastMapPoint(observed);
        if(last?["player_stats"] is JsonArray {Count:1} stats&&stats[0] is JsonObject finalStats)
            foreach(var field in new[]{"current_hp","max_hp","current_gold"})
                finalStats[field]=player[field=="current_gold"?"gold":field]?.DeepClone();
        LastRoom(last)?.Remove("turns_taken");LastRoom(nativeLast)?.Remove("turns_taken");
        return JsonNode.DeepEquals(expected,observed);
    }

    private static JsonObject? LastMapPoint(JsonArray route)=>(route.LastOrDefault() as JsonArray)?.LastOrDefault() as JsonObject;
    private static JsonObject? LastRoom(JsonObject? point)=>(point?["rooms"] as JsonArray)?.LastOrDefault() as JsonObject;
}
