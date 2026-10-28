using System.Text.Json;
using ReplayMod;

internal static class ChoiceContracts
{
    internal static void VerifyRecording(string output,string path)
    {
        var bytes=File.ReadAllBytes(path);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;var clip=envelope.Data;
        Need(Codec.Hash(Codec.Bytes(clip))==envelope.SHA256,"recorded-envelope");
        Codec.Validate(clip,"0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9",
            "F1D9D349A5804D3A455F78D43B964320102C5117CCA3D9B9E5160D126DB63938",clip.Scope);
        var choices=clip.Choices!;Need(clip.Format==4&&clip.Operations.Length==16&&choices.Length==15&&clip.Operations[^1].Victory,"recorded-complete-content-sample");
        using var initial=JsonDocument.Parse(clip.Initial);Need(initial.RootElement.GetProperty("ChoicesConsumed").GetInt32()==1,"recorded-initial-choice");
        foreach(var op in clip.Operations)
        {
            using var document=JsonDocument.Parse(op.After);var root=document.RootElement;
            using var victory=op.Victory?JsonDocument.Parse(root.GetProperty("State").GetString()!):null;
            if(victory is not null)root=victory.RootElement;
            Need(root.GetProperty("ChoicesConsumed").GetInt32()==choices.Count(c=>c.Boundary<=op.Sequence),"recorded-consumption-boundary");
        }
        Need(choices.All(c=>!c.Automated&&c.NativeId.HasValue)&&choices.Count(c=>c.Boundary==7)==2&&
            choices.Count(c=>c.Boundary==11)==3&&choices.Count(c=>c.Boundary==14)==3&&choices.Count(c=>c.Chosen.Length==2)==3,"recorded-human-choice-coverage");
        int Paper(int operation)
        {
            using var document=JsonDocument.Parse(clip.Operations[operation-1].After);
            var relic=document.RootElement.GetProperty("Mechanics").GetProperty("Relics").EnumerateArray().Single(r=>r.GetProperty("Model").GetString()=="RELIC.JOSS_PAPER").GetProperty("Saved");
            if(!relic.TryGetProperty("props",out var props)||!props.TryGetProperty("ints",out var ints))return 0;
            return ints.EnumerateArray().Where(i=>i.GetProperty("name").GetString()=="CardsExhausted").Select(i=>i.GetProperty("value").GetInt32()).SingleOrDefault();
        }
        Need(Paper(7)==4&&Paper(8)==0&&File.ReadAllBytes(path).SequenceEqual(bytes),"recorded-paper-threshold-or-source-mutated");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="ACTUAL_CONTENT001_RECORDING_VERIFIED_NOT_PLAYBACK",clip.Id,clip.Format,clip.ModHash,clip.Seed,clip.Col,
            Operations=clip.Operations.Length,Choices=choices.Length,HumanChoices=choices.Count(c=>!c.Automated),InitialChoices=choices.Count(c=>c.Boundary==0),
            BurstChoices=2,MultiChoices=3,TurnStartChoices=6,PaperBefore=4,PaperAfter=0,FileBytes=bytes.Length,FileSHA256=Codec.Hash(bytes),EnvelopeSHA256=envelope.SHA256,
            ChoiceBoundaries=choices.Select(c=>c.Boundary).ToArray(),OriginalFileUnchanged=true,GameConstructorsExecuted=false,PlaybackExecuted=false}));
        Console.WriteLine("ACTUAL_CONTENT_RECORDING_VERIFIED");
    }
    private static void Need(bool ok,string reason){if(!ok)throw new InvalidDataException(reason);}
    internal static void Run(string output,string priorPath)
    {
        var tests=new List<string>();void Need(bool ok){if(!ok)throw new Exception("choice-contract");}
        void Reject(Action run){try{run();throw new Exception("bad-choice-accepted");}catch(InvalidDataException){}}
        ChoiceCard Card(uint id,string hash)=>new("CARD.DEFEND_IRONCLAD",0,true,id,null,new string(hash[0],64),"防御");
        var a=Card(7,"A");var b=Card(8,"A");
        var initial=new ChoiceFrame(1,0,1,"FromChooseACardScreen","RELIC.TOOLBOX",1,1,false,false,[[a],[b]],[1],40,1234,"Index",0,false);
        var multi=new ChoiceFrame(2,3,1,"FromHand","CARD.PREPARED",2,2,false,false,[[a],[b],[Card(9,"B")]],[2,0],2000,70000,"CombatCard",1,false);
        var turn=multi with{Sequence=3,Boundary=4,Turn=2,Source="POWER.TYRANNY",Min=1,Max=1,Chosen=[0]};
        ChoiceContract.Validate([initial,multi,turn],5);
        var parsed=JsonSerializer.Deserialize<ChoiceFrame[]>(Codec.Bytes(new[]{initial,multi,turn}),Codec.Json)!;
        Need(parsed[0].Boundary==0&&parsed[1].Chosen.SequenceEqual(new[]{2,0})&&parsed[1].DurationMilliseconds==70000&&parsed[2].Boundary==4);
        tests.Add("initial-operation-and-next-turn-choices-with-ordered-multiselect-and-human-time-roundtrip");
        ChoiceContract.Match(initial,initial with{NativeId=99,BeganMilliseconds=999,Options=[[a with{Label="Defend"}],[b]]});
        Reject(()=>ChoiceContract.Match(initial,initial with{Options=[[b],[a]]}));
        Reject(()=>ChoiceContract.Match(initial,initial with{Options=[[a with{Upgrade=1}],[b]]}));
        tests.Add("same-name-distinct-instances-and-upgrades-checked-labels-and-live-native-id-may-differ");
        ChoiceContract.Validate([initial with{Min=0,Cancelable=true,Chosen=[]}],5);
        ChoiceContract.Validate([multi with{Sequence=1,Min=0,Max=3,RequestedMax=999999999,Chosen=[2,0]}],5);
        Reject(()=>ChoiceContract.Validate([multi with{Sequence=1,Chosen=[0,0]}],5));
        Reject(()=>ChoiceContract.Validate([multi with{Sequence=1,Chosen=[99,0]}],5));
        Reject(()=>ChoiceContract.Validate([turn with{Sequence=1},initial with{Sequence=2}],5));
        Reject(()=>ChoiceContract.Validate([initial with{PlayerSlot=1}],5));
        tests.Add("legal-skip-empty-and-invalid-count-order-identity-boundaries-rejected");
        var clip4=new Clip(4,new string('a',32),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],"CHARACTER.IRONCLAD","seed",1,0,"test","checkpoint","initial",
            Enumerable.Range(1,5).Select(i=>new Operation(i,OperationKind.EndTurn,i,null,null,null,"after")).ToArray(),Choices:[initial,multi,turn]);
        Codec.Validate(clip4,"game","mod","scope");var wire=Codec.Bytes(new ClipEnvelope(Codec.Hash(Codec.Bytes(clip4)),clip4));
        var decoded=JsonSerializer.Deserialize<ClipEnvelope>(wire,Codec.Json)!;Need(Codec.Hash(Codec.Bytes(decoded.Data))==decoded.SHA256&&decoded.Data.Choices![1].Chosen.SequenceEqual(new[]{2,0}));
        Need(Codec.Hash(Codec.Bytes(decoded.Data with{Choices=[initial with{Chosen=[0]},multi,turn]}))!=decoded.SHA256);
        tests.Add("format4-envelope-covers-choice-results-and-retains-initial-and-operation-boundaries");
        var graph=new Battle();var first=new Piece{Id=7};graph.Pile.Add(first);graph.Db[7]=first;graph.Next=8;
        IdentityMemento Capture()=>new([graph],o=>o is Battle or Piece,f=>f.Name!="NativeChoiceId");
        var before=Capture();var generated=new Piece{Id=graph.Next++};graph.Db[generated.Id]=generated;graph.Pile.Add(generated);graph.ChoicePosition=3;graph.NativeChoiceId=10;
        before.Restore();Need(graph.Pile.Single()==first&&graph.Db.Count==1&&graph.Next==8&&graph.ChoicePosition==0&&graph.NativeChoiceId==10);
        var regenerated=new Piece{Id=graph.Next++};graph.Db[regenerated.Id]=regenerated;graph.Pile.Add(regenerated);graph.ChoicePosition=3;
        Need(regenerated.Id==generated.Id&&!ReferenceEquals(regenerated,generated));
        tests.Add("generated-card-registry-pile-and-logical-choice-position-restore-with-live-id-monotonic");
        var oldBytes=File.ReadAllBytes(priorPath);var prior=JsonSerializer.Deserialize<ClipEnvelope>(oldBytes,Codec.Json)!;
        Need(prior.Data.Format==3&&prior.Data.Choices is null&&Codec.Hash(Codec.Bytes(prior.Data))==prior.SHA256&&File.ReadAllBytes(priorPath).SequenceEqual(oldBytes));
        tests.Add("actual-STATE001-recording-retains-original-byte-checksum-with-null-choice-field-omitted");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="CONTENT001_CHOICE_DATA_CONTRACTS_PASS_NOT_NATIVE_RUNTIME",Count=tests.Count,Tests=tests,GameConstructorsExecuted=false}));
        Console.WriteLine("CHOICE_CONTRACTS_PASS "+tests.Count);
    }
    private sealed class Battle {public readonly List<Piece> Pile=[];public readonly Dictionary<uint,Piece> Db=[];public uint Next;public int ChoicePosition,NativeChoiceId;}
    private sealed class Piece {public uint Id;}
}
