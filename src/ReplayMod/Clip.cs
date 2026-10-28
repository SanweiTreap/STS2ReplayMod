using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ReplayMod;

public enum OperationKind { Card, EndTurn, Potion }
public enum TargetRole { Enemy, Player }
public sealed record TargetKey(int Index,string Model,uint CombatId,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] TargetRole Role=TargetRole.Enemy);
public sealed record CardKey(uint CombatIndex,int Upgrade,string Fingerprint,string TargetType);
public sealed record PotionKey(int Slot,string Model,string Fingerprint,string TargetType);
public sealed record OperationTiming(double AcceptedMilliseconds,double CompletedMilliseconds);
public sealed record RecordingTiming(string Basis,double InitialReadyMilliseconds,bool IncludesInitialLoading,bool IncludesMenuPause,bool IncludesChoiceWait);
public sealed record Operation(int Sequence,OperationKind Kind,int Turn,int? HandIndex,string? Card,TargetKey? Target,string After,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] bool Victory=false,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] CardKey? CardState=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] PotionKey? Potion=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] OperationTiming? Timing=null);
public sealed record Clip(int Format,string Id,DateTimeOffset CreatedUtc,string GameHash,string ModHash,string Scope,
    string[] Mods,string Character,string Seed,int Row,int Col,string Encounter,string Checkpoint,string Initial,Operation[] Operations,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? Preparation=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? Authority=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] ChoiceFrame[]? Choices=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] RecordingTiming? Timing=null);
public sealed record ClipEnvelope(string SHA256,Clip Data);

public static class Codec
{
    public static readonly JsonSerializerOptions Json=new(){Converters={new JsonStringEnumConverter()},MaxDepth=64};
    public static byte[] Bytes<T>(T value)=>JsonSerializer.SerializeToUtf8Bytes(value,Json);
    public static string Hash(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    public static void Require(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
    public const int MaxOperations=256;
    public static void Validate(Clip clip,string game,string mod,string scope,params string[] compatibleRecordedMods)
    {
        Require(clip.Format is 1 or 2 or 3 or 4 or 5 or 6&&Regex.IsMatch(clip.Id,"^[a-f0-9]{32}$"),"unsupported-format-or-id");
        Require(clip.GameHash==game&&(clip.ModHash==mod||clip.ModHash is not null&&compatibleRecordedMods.Contains(clip.ModHash,StringComparer.Ordinal))&&clip.Scope==scope&&clip.Mods.SequenceEqual(new[]{"ReplayMod:0.1.0"}),"different-build-or-profile-or-mods");
        Require((clip.Character=="CHARACTER.IRONCLAD"||clip.Format>=4&&clip.Character=="CHARACTER.SILENT"||clip.Format>=5&&clip.Character=="CHARACTER.REGENT")&&clip.Row==1&&clip.Col>=0&&clip.Encounter.Length>0&&
            clip.Checkpoint.Length>0&&clip.Initial.Length>0,"unsupported-scene");
        Require(clip.Operations.Length>0&&clip.Operations.Length<=(clip.Format==1?32:MaxOperations),"clip-operation-count");
        Require(clip.Format>=4?clip.Choices is not null:clip.Choices is null,"choice-format");
        if(clip.Choices is not null)ChoiceContract.Validate(clip.Choices,clip.Operations.Length);
        ValidateTiming(clip);
        Require(clip.Authority is null||clip.Character=="CHARACTER.IRONCLAD"&&clip.Format==3&&clip.Authority=="STATE001-first-Defend-v1"&&
            clip.Operations[0] is {Kind:OperationKind.Card,Card:"CARD.DEFEND_IRONCLAD",Turn:1,Victory:false,CardState.Upgrade:0},"unsupported-authority-point");
        var victory=clip.Operations[^1].Victory;
        Require(clip.Format!=1||!clip.Operations.Any(o=>o.Victory),"legacy-victory-marker");
        Require(victory||clip.Operations.Any(o=>o.Kind==OperationKind.EndTurn),"incomplete-short-clip");
        for(var i=0;i<clip.Operations.Length;i++)
        {
            var op=clip.Operations[i];Require(op.Sequence==i+1&&op.Turn>0&&op.After.Length>0&&Enum.IsDefined(op.Kind),"operation-sequence");
            Require(!op.Victory||i==clip.Operations.Length-1,"victory-must-be-last-operation");
            if(op.Victory)
            {
                using var result=JsonDocument.Parse(op.After);var end=result.RootElement;
                Require(end.GetProperty("Outcome").GetString()=="Victory"&&end.GetProperty("RoomPreFinished").GetBoolean()&&
                    !end.GetProperty("CombatInProgress").GetBoolean()&&end.GetProperty("State").GetString() is {Length:>0}&&
                    end.GetProperty("Defeated").GetArrayLength()>0,"incomplete-victory-result");
            }
            if(clip.Format<3)
            {
                Require(op.CardState is null&&op.Potion is null&&op.Target?.Role is null or TargetRole.Enemy,"legacy-operation-metadata");
                Require(op.Kind==OperationKind.EndTurn?op.Card is null&&op.HandIndex is null&&op.Target is null:
                    op.Kind==OperationKind.Card&&op.HandIndex is >=0 and <10&&op.Card is "CARD.STRIKE_IRONCLAD" or "CARD.DEFEND_IRONCLAD" or "CARD.BASH","unsupported-operation");
            }
            else
            {
                bool Model(string? value,string prefix)=>value is not null&&value.Length<=128&&Regex.IsMatch(value,"^"+prefix+"\\.[A-Z0-9_]+$");
                bool Hash(string? value)=>value is not null&&Regex.IsMatch(value,"^[A-F0-9]{64}$");
                bool TargetType(string value)=>value is "None" or "Self" or "AnyEnemy" or "AllEnemies" or "RandomEnemy" or "AnyPlayer" or "AllAllies";
                if(op.Target is {} target)Require(Enum.IsDefined(target.Role)&&target.Index>=0&&target.Index<128&&
                    Model(target.Model,target.Role==TargetRole.Player?"CHARACTER":"MONSTER"),"invalid-operation-target");
                Require(op.Kind switch
                {
                    OperationKind.EndTurn=>op.Card is null&&op.HandIndex is null&&op.Target is null&&op.CardState is null&&op.Potion is null,
                    OperationKind.Card=>op.HandIndex is >=0 and <128&&Model(op.Card,"CARD")&&op.Potion is null&&
                        op.CardState is {CombatIndex:<=65535,Upgrade:>=0 and <=20} key&&Hash(key.Fingerprint)&&TargetType(key.TargetType),
                    OperationKind.Potion=>op.Card is null&&op.HandIndex is null&&op.CardState is null&&op.Potion is {Slot:>=0 and <16} potion&&
                        Model(potion.Model,"POTION")&&Hash(potion.Fingerprint)&&TargetType(potion.TargetType),
                    _=>false
                },"unsupported-operation");
            }
        }
    }
    private static void ValidateTiming(Clip clip)
    {
        if(clip.Format<5){Require(clip.Timing is null&&clip.Operations.All(o=>o.Timing is null),"legacy-time-unknown");return;}
        Require(clip.Timing is {Basis:"checkpoint-stopwatch-elapsed-realtime-v1",IncludesInitialLoading:true,IncludesMenuPause:true,IncludesChoiceWait:true} timing&&
            double.IsFinite(timing.InitialReadyMilliseconds)&&timing.InitialReadyMilliseconds>=0,"recording-time-basis");
        var previous=clip.Timing!.InitialReadyMilliseconds;
        var previousAccepted=previous;
        foreach(var op in clip.Operations)
        {
            Require(op.Timing is {} time&&double.IsFinite(time.AcceptedMilliseconds)&&double.IsFinite(time.CompletedMilliseconds)&&
                time.AcceptedMilliseconds>=(clip.Format>=6?previousAccepted:previous)&&time.CompletedMilliseconds>=time.AcceptedMilliseconds&&
                time.CompletedMilliseconds>=previous,"operation-time-order");
            previousAccepted=op.Timing!.AcceptedMilliseconds;
            previous=op.Timing!.CompletedMilliseconds;
        }
        foreach(var choice in clip.Choices??[])
        {
            var start=choice.Boundary==0?0:clip.Operations[choice.Boundary-1].Timing!.AcceptedMilliseconds;
            var end=choice.Boundary==0?clip.Timing.InitialReadyMilliseconds:clip.Operations[choice.Boundary-1].Timing!.CompletedMilliseconds;
            Require(choice.BeganMilliseconds>=start&&choice.BeganMilliseconds+choice.DurationMilliseconds<=end+1,"choice-time-outside-host");
        }
    }
}

// Prototype files are private local clips, not a public/export format. The checkpoint contains native player identifiers.
public sealed class ClipStore(string root)
{
    public const int Cap=4*1024*1024;
    private string PathFor(string id){Codec.Require(Regex.IsMatch(id,"^[a-f0-9]{32}$"),"invalid-clip-id");return Path.Combine(root,id+".json");}
    public string? LatestId()
    {
        var path=Path.Combine(root,"latest.txt");return File.Exists(path)?File.ReadAllText(path):null;
    }
    public void Seal(Clip clip)
    {
        Directory.CreateDirectory(root);var path=PathFor(clip.Id);Codec.Require(!File.Exists(path),"clip-already-sealed");
        var bytes=Codec.Bytes(new ClipEnvelope(Codec.Hash(Codec.Bytes(clip)),clip));Codec.Require(bytes.Length<=Cap,"clip-too-large");
        var pending=path+".pending";File.WriteAllBytes(pending,bytes);File.Move(pending,path);
        var pointer=Path.Combine(root,"latest.txt");File.WriteAllText(pointer+".pending",clip.Id);File.Move(pointer+".pending",pointer,true);
    }
    public Clip Read(string id,string game,string mod,string scope,params string[] compatibleRecordedMods)
    {
        var path=PathFor(id);Codec.Require(new FileInfo(path).Length<=Cap,"clip-too-large");
        var envelope=JsonSerializer.Deserialize<ClipEnvelope>(File.ReadAllBytes(path),Codec.Json)??throw new InvalidDataException("empty-clip");
        Codec.Require(Codec.Hash(Codec.Bytes(envelope.Data))==envelope.SHA256&&envelope.Data.Id==id,"clip-integrity");
        Codec.Validate(envelope.Data,game,mod,scope,compatibleRecordedMods);return envelope.Data;
    }
}

// Controls change intent; an in-flight semantic action always reaches its stable boundary before pause/stop.
public sealed class PlaybackCursor(int count)
{
    public int Position {get;private set;}
    public bool Playing {get;private set;}
    public bool Busy {get;private set;}
    public bool Rebuilding {get;private set;}
    public int? RewindTarget {get;private set;}
    public bool StopRequested {get;private set;}
    private bool _step;
    private bool _choiceStep;
    public bool StepChoice(){if(StopRequested||Rebuilding||_choiceStep)return false;Playing=false;_choiceStep=true;return true;}
    public bool TakeChoiceStep(){if(StopRequested)return false;if(Playing)return true;var step=_choiceStep;_choiceStep=false;return step;}
    public void ChoiceOpened(){_choiceStep=false;}
    public bool CanRewind=>Position>0&&!Playing&&!Busy&&!StopRequested&&!_step;
    public bool Toggle(){if(StopRequested||Rebuilding||Finished)return false;Playing=!Playing;if(Playing)_choiceStep=false;return true;}
    public bool Step(){if(Busy||StopRequested||Finished||_step)return false;Playing=false;_step=true;return true;}
    // Reserve the cursor synchronously so two UI events in one frame cannot overlap dispatch/rebuild.
    public bool Rewind()
    {
        if(!CanRewind)return false;
        RewindTarget=Position-1;Rebuilding=true;Busy=true;return true;
    }
    public void CompleteRewind(int arrived)
    {
        Codec.Require(Rebuilding&&Busy&&!StopRequested&&arrived==RewindTarget,"invalid-rewind-arrival");
        Position=arrived;RewindTarget=null;Rebuilding=false;Busy=false;Playing=false;_step=false;
    }
    public void Stop(){StopRequested=true;Playing=false;_step=false;_choiceStep=false;}
    public bool TryBegin(bool alreadySubmitted=false)
    {
        if(Busy||StopRequested||Position>=count||(!Playing&&!_step&&!alreadySubmitted))return false;
        Busy=true;_step=false;return true;
    }
    public void Complete(){Codec.Require(Busy&&!Rebuilding,"no-active-operation");Position++;Busy=false;if(Position==count)Playing=false;}
    public bool Finished=>Position==count;
    public void ArriveFromFile(int position)
    {
        Codec.Require(Position==0&&!Busy&&!Playing&&!Rebuilding&&!StopRequested&&position>0&&position<count,"invalid-file-arrival");
        Position=position;
    }
}

// Display history and native execution have separate frontiers. Each visible edge is reversible.
public sealed class DecisionCursor
{
    public int Position {get;private set;}=-1;
    public int Count {get;private set;}
    public bool Playing {get;private set;}
    public bool Moving {get;private set;}
    public bool AwaitingEngine {get;private set;}=true;
    public bool Stopping {get;private set;}
    private int _intent;
    public bool AtFrontier=>Position==Count-1;
    public bool Ready=>Count>0&&!Moving&&!AwaitingEngine&&!Stopping;
    public bool HasIntent=>_intent!=0;
    public bool CanBack=>Ready&&Position>0;
    public bool Step(){if(!Ready||_intent!=0)return false;Playing=false;_intent=1;return true;}
    public bool Back(){if(!CanBack||_intent!=0)return false;Playing=false;_intent=-1;return true;}
    public bool Toggle(){if(Stopping)return false;Playing=!Playing;_intent=0;return true;}
    public int Take()
    {
        if(!Ready||_intent==0&&!Playing)return 0;
        var direction=_intent==0?1:_intent;_intent=0;Moving=true;return direction;
    }
    public void Append()
    {
        Codec.Require(!Stopping&&AtFrontier&&(AwaitingEngine||Moving),"route-invalid-append");
        Count++;Position=Count-1;AwaitingEngine=false;Moving=false;
    }
    public void Arrive(int target)
    {
        Codec.Require(Moving&&target>=0&&target<Count&&Math.Abs(target-Position)==1,"route-invalid-history-arrival");
        Position=target;Moving=false;
    }
    public void EngineWait(){Codec.Require(Moving&&AtFrontier,"route-invalid-engine-request");Moving=false;AwaitingEngine=true;}
    public void Pause(){Playing=false;_intent=0;}
    public void Stop(){Stopping=true;Playing=false;_intent=0;}
}
