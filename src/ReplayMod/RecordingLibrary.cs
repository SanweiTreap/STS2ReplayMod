using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ReplayMod;

public sealed record BattleRecording(string RunId,int Battle,int Attempt,int Act,int Floor,long RunStartTime,string Entry,string Completion);
public sealed record ClipListing(string Id,DateTimeOffset CreatedUtc,string Character,string Encounter,int Row,int Operations,bool Victory,BattleRecording? Battle,string? Problem=null);

// This journal belongs to Replay. No identifier is written into the native run save.
// A Continue must match an observed save, rather than a seed/character/coordinate guess.
public sealed class RecordingJournal(string path)
{
    public sealed record RunEntry(string Id,Dictionary<string,int> Battles,Dictionary<string,int> Attempts);
    public sealed record RunCorrection(string CanonicalRun,int BattleOffset,string Evidence);
    public sealed record Data(Dictionary<string,string> Saves,Dictionary<string,RunEntry> Runs,
        [property:System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] Dictionary<string,RunCorrection>? Corrections=null);
    private Data _data=Load(path);
    public string? CurrentRun {get;private set;}
    public bool ContinueMatched {get;private set;}
    private static Data Load(string path)=>File.Exists(path)?JsonSerializer.Deserialize<Data>(File.ReadAllBytes(path),Codec.Json)??throw new InvalidDataException("empty-recording-journal"):new([],[]);
    public static string SaveKey(string json)
    {
        var node=JsonNode.Parse(json)?.AsObject()??throw new InvalidDataException("invalid-native-run-save");
        foreach(var key in new[]{"save_time","run_time","num_reloads"})node.Remove(key);
        return Codec.Hash(Canonical(node));
    }
    private static byte[] Canonical(JsonNode node)
    {
        using var stream=new MemoryStream();using(var writer=new Utf8JsonWriter(stream))Write(node,writer);
        return stream.ToArray();
    }
    private static void Write(JsonNode? node,Utf8JsonWriter writer)
    {
        if(node is JsonObject obj){writer.WriteStartObject();foreach(var p in obj.OrderBy(p=>p.Key,StringComparer.Ordinal)){writer.WritePropertyName(p.Key);Write(p.Value,writer);}writer.WriteEndObject();}
        else if(node is JsonArray array){writer.WriteStartArray();foreach(var v in array)Write(v,writer);writer.WriteEndArray();}
        else if(node is null)writer.WriteNullValue();else node.WriteTo(writer);
    }
    public void NewRun(){CurrentRun=Guid.NewGuid().ToString("N");ContinueMatched=false;_data.Runs.Add(CurrentRun,new(CurrentRun,[],[]));Save();}
    public void Continue(string actualSave)
    {
        ContinueMatched=_data.Saves.TryGetValue(SaveKey(actualSave),out var id)&&_data.Runs.ContainsKey(id);
        if(ContinueMatched)CurrentRun=CanonicalRun(id!);else NewRun();
        Observe(actualSave); // Unknown old runs start an explicitly unmatched group.
    }
    public void Observe(string save)
    {
        if(CurrentRun is null)return;
        ObserveFor(CurrentRun,save);
    }
    public void ObserveFor(string runId,string save)
    {
        Codec.Require(_data.Runs.ContainsKey(runId),"recording-observed-save-run-missing");
        runId=CanonicalRun(runId);
        var key=SaveKey(save);
        // Exact collisions between two separately observed new runs are ambiguous, never merged.
        _data.Saves[key]=_data.Saves.TryGetValue(key,out var old)&&CanonicalRun(old)!=runId?"ambiguous":runId;Save();
    }
    private string CanonicalRun(string run)=>_data.Corrections?.GetValueOrDefault(run)?.CanonicalRun??run;
    public BattleRecording Display(BattleRecording battle)=>_data.Corrections?.GetValueOrDefault(battle.RunId) is {} correction?
        battle with{RunId=correction.CanonicalRun,Battle=battle.Battle+correction.BattleOffset}:battle;
    // Explicit, evidence-backed correction of an old split after completed rooms.
    // Never infer this link from a seed; the original clips and catalog stay immutable.
    public void CorrectContinuation(string alias,string canonical,string evidence)
    {
        Codec.Require(alias!=canonical&&_data.Runs.ContainsKey(alias)&&_data.Runs.ContainsKey(canonical)&&evidence.Length>=20,"invalid-run-correction");
        if(_data.Corrections?.GetValueOrDefault(alias) is {} existing)
        {Codec.Require(existing.CanonicalRun==canonical,"conflicting-run-correction");return;}
        Codec.Require(CanonicalRun(canonical)==canonical&&!(_data.Corrections?.Values.Any(c=>c.CanonicalRun==alias)??false),"nested-run-correction-unsupported");
        var source=_data.Runs[alias];var target=_data.Runs[canonical];
        Codec.Require(!source.Battles.Keys.Intersect(target.Battles.Keys).Any()&&
            source.Battles.Values.Order().SequenceEqual(Enumerable.Range(1,source.Battles.Count))&&
            target.Battles.Values.Order().SequenceEqual(Enumerable.Range(1,target.Battles.Count)),"run-correction-overlap-or-order");
        var offset=target.Battles.Count;
        foreach(var p in source.Battles)target.Battles.Add(p.Key,p.Value+offset);
        foreach(var p in source.Attempts)target.Attempts.Add(p.Key,p.Value);
        _data=_data with{Corrections=_data.Corrections??new()};_data.Corrections.Add(alias,new(canonical,offset,evidence));
        if(CurrentRun==alias)CurrentRun=canonical;Save();
    }
    public BattleRecording Begin(int act,int row,int col,int floor,long start,string entry)
    {
        Codec.Require(CurrentRun is not null,"recording-run-identity-missing");var run=_data.Runs[CurrentRun!];var key=$"{act}:{row}:{col}";
        if(!run.Battles.ContainsKey(key))run.Battles[key]=run.Battles.Count+1;
        var attempt=run.Attempts.GetValueOrDefault(key)+1;run.Attempts[key]=attempt;Save();
        return new(run.Id,run.Battles[key],attempt,act,floor,start,entry,"recording");
    }
    private void Save(){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);File.WriteAllBytes(path+".pending",Codec.Bytes(_data));File.Move(path+".pending",path,true);}
}

public static class RecordingLibrary
{
    // Read metadata once per menu attachment, never on the gameplay/frame path.
    // Each file is isolated: one damaged clip cannot hide the rest of the library.
    public static ClipListing[] List(string root,Func<string,Clip> read,Func<BattleRecording,BattleRecording>? display=null)
    {
        if(!Directory.Exists(root))return [];
        var cache=Path.Combine(root,"catalog");Directory.CreateDirectory(cache);var rows=new List<ClipListing>();
        foreach(var file in Directory.EnumerateFiles(root,"*.json"))
        {
            var id=Path.GetFileNameWithoutExtension(file);if(!Regex.IsMatch(id,"^[a-f0-9]{32}$"))continue;
            var meta=Path.Combine(cache,id+".json");
            try
            {
                ClipListing? row=null;
                if(File.Exists(meta)&&File.GetLastWriteTimeUtc(meta)>=File.GetLastWriteTimeUtc(file))try
                {
                    row=JsonSerializer.Deserialize<ClipListing>(File.ReadAllBytes(meta),Codec.Json);
                    Codec.Require(row?.Id==id&&row.Character is not null&&row.Encounter is not null&&row.Operations>0&&
                        (row.Battle is null||Regex.IsMatch(row.Battle.RunId??"","^[a-f0-9]{32}$")),"catalog-metadata-invalid");
                }
                catch{row=null;}
                if(row is null){row=Describe(read(id));File.WriteAllBytes(meta,Codec.Bytes(row));}
                rows.Add(row);
            }
            catch(Exception ex){rows.Add(new(id,File.GetLastWriteTimeUtc(file),"未知","读取失败",0,0,false,null,ex is InvalidDataException?ex.Message:ex.GetType().Name));}
        }
        return rows.Select(r=>display is not null&&r.Battle is {} battle?r with{Battle=display(battle)}:r).OrderByDescending(r=>r.CreatedUtc).ToArray();
    }
    public static ClipListing Describe(Clip clip)=>new(clip.Id,clip.CreatedUtc,clip.Character,clip.Encounter,clip.Row,clip.Operations.Length,clip.Operations.LastOrDefault()?.Victory==true,clip.Battle,NativePlaybackSupport.Reason(clip));
}
