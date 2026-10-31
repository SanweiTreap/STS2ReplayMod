using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReplayMod;

public sealed record RunDecision(string Kind,string[] Options,int[] Selected);
public sealed record RunArgument(int? Row=null,int? Col=null,int? RewardIndex=null,string? RewardKey=null,string? Option=null);
public sealed record RunOperation(int Sequence,int Visit,string Kind,RunArgument Argument,string? Before,string? After,
    string Status,RunDecision[] Decisions,string? Clip=null,BattleRecording? Battle=null,bool? Success=null,
    [property:System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? Progress=null);
public sealed record RoomVisit(int Id,int Act,int Floor,int Row,int Col,string MapType,string? RoomType,string? Model,
    int Entries,string Status,string? Gap,
    [property:System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string[]? Subrooms=null);
public sealed record RunContinuation(DateTimeOffset At,int Visit,bool PreFinished,string SaveKey,string Status);
public sealed record RunDocument(int Format,string Id,string GameHash,string ModHash,string Scope,string Character,
    DateTimeOffset Created,List<RoomVisit> Visits,List<RunOperation> Operations,List<RunContinuation> Continuations);
public sealed record RunEnvelope(string SHA256,RunDocument Data);

internal sealed class RunStore(string root)
{
    internal static int[] ResourceDiscontinuities(RunDocument run)=>run.Operations.Zip(run.Operations.Skip(1))
        .Where(pair=>pair.First is {Status:"complete",After:not null}&&pair.Second is {Status:"complete",Before:not null}&&pair.First.After!=pair.Second.Before)
        .Select(pair=>pair.Second.Sequence).ToArray();
    internal void Save(RunDocument run)
    {
        Validate(run);Directory.CreateDirectory(root);
        var bytes=Codec.Bytes(new RunEnvelope(Codec.Hash(Codec.Bytes(run)),run));
        Codec.Require(bytes.Length<=16*1024*1024,"run-file-size-limit");
        var path=Path.Combine(root,run.Id+".json");File.WriteAllBytes(path+".pending",bytes);File.Move(path+".pending",path,true);
    }
    internal RunDocument Read(string id)
    {
        Codec.Require(Regex.IsMatch(id,"^[a-f0-9]{32}$"),"invalid-run-id");
        var path=Path.Combine(root,id+".json");Codec.Require(new FileInfo(path).Length<=16*1024*1024,"run-file-size-limit");
        var envelope=JsonSerializer.Deserialize<RunEnvelope>(File.ReadAllBytes(path),Codec.Json)??throw new InvalidDataException("empty-run-file");
        Codec.Require(Codec.Hash(Codec.Bytes(envelope.Data))==envelope.SHA256,"run-file-checksum");Validate(envelope.Data);return envelope.Data;
    }
    internal IEnumerable<string> Ids()=>Directory.Exists(root)?Directory.EnumerateFiles(root,"*.json").Select(Path.GetFileNameWithoutExtension).OfType<string>().Where(s=>Regex.IsMatch(s,"^[a-f0-9]{32}$")):[];
    private static void Validate(RunDocument run)
    {
        Codec.Require(run.Format==1&&Regex.IsMatch(run.Id,"^[a-f0-9]{32}$")&&run.Visits.Count<=256&&run.Operations.Count<=2048,"unsupported-run-file");
        Codec.Require(run.Operations.Select(o=>o.Sequence).SequenceEqual(Enumerable.Range(1,run.Operations.Count)),"run-operation-order");
        Codec.Require(run.Operations.All(o=>run.Visits.Any(v=>v.Id==o.Visit)),"run-operation-visit-missing");
    }
}
