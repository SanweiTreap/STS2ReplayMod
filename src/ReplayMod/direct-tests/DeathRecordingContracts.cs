using System.Text.Json;
using System.Text.Json.Nodes;
using ReplayMod;

internal static class DeathRecordingContracts
{
    internal static void Run(string output,string original)
    {
        var bytes=File.ReadAllBytes(original);var sample=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!.Data;
        var store=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(original))!);
        var read=store.Read(sample.Id,sample.GameHash,"death-policy-build",sample.Scope,sample.ModHash);
        Codec.Require(sample.Format==10&&Codec.Bytes(read).SequenceEqual(Codec.Bytes(sample)),"old-native-recording-changed");
        var state=JsonNode.Parse(sample.Initial)!;
        state["Player"]!["CurrentHp"]=0;state["Mechanics"]!["Player"]!["CurrentHp"]=0;
        state["Mechanics"]!["CombatUnits"]!["Allies"]![0]!["Hp"]=0;
        state["Mechanics"]!["CombatUnits"]!["Allies"]![0]!["Alive"]=false;
        string Projection(string hpState,string resources)=>JsonSerializer.Serialize(new{Outcome="Death",RoomPreFinished=false,
            CombatInProgress=true,State=hpState,TerminalResources=resources},Codec.Json);
        const string deadResources="{\"players\":[{\"current_hp\":0}]}";
        var time=sample.Operations[^1].Timing;
        var fatal=new Operation(1,OperationKind.EndTurn,1,null,null,null,Projection(state.ToJsonString(),deadResources),Timing:time,Defeat:true);
        var death=sample with{Id=Guid.NewGuid().ToString("N"),Choices=[],Operations=[fatal],Battle=sample.Battle! with{Completion="death"}};
        var passed=new List<string>();
        void Test(string name,Action run){run();passed.Add(name);}
        void Valid(Clip clip)=>Codec.Validate(clip,sample.GameHash,sample.ModHash,sample.Scope);
        void Reject(Clip clip)
        {
            var refused=false;try{Valid(clip);}catch(InvalidDataException){refused=true;}catch(JsonException){refused=true;}
            catch(KeyNotFoundException){refused=true;}
            Codec.Require(refused,"invalid-death-recording-accepted");
        }
        Test("existing-format10-payload-and-checksum-remain-exact",()=>
            Codec.Require(Codec.Bytes(sample).AsSpan().IndexOf("\"Defeat\""u8)<0,"false-death-field-was-added"));
        Test("last-fatal-action-is-a-distinct-terminal-result",()=>Valid(death));
        Test("death-is-not-silently-opened-in-unimplemented-player",()=>
            Codec.Require(NativePlaybackSupport.Reason(death) is not null,"death-playback-unintentionally-enabled"));
        Test("victory-and-death-cannot-share-the-last-action",()=>Reject(death with{Operations=[fatal with{Victory=true}]}));
        Test("death-cannot-be-an-intermediate-operation",()=>Reject(death with{Operations=[fatal,fatal with{Sequence=2}]}));
        Test("death-cannot-be-labelled-an-interrupted-or-victory-clip",()=>
        {
            Reject(death with{Battle=death.Battle! with{Completion="interrupted"}});
            Reject(death with{Battle=death.Battle! with{Completion="victory"}});
        });
        Test("alive-player-cannot-be-certified-as-dead",()=>Reject(death with{Operations=[fatal with{After=Projection(sample.Initial,deadResources)}]}));
        Test("native-terminal-resources-must-also-show-death",()=>Reject(death with{Operations=[fatal with{After=Projection(state.ToJsonString(),"{\"players\":[{\"current_hp\":1}]}")}]}));
        Test("legacy-formats-cannot-carry-new-death-marker",()=>Reject(death with{Format=9}));
        Codec.Require(File.ReadAllBytes(original).SequenceEqual(bytes),"original-death-policy-input-mutated");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_DEATH_RECORDING_POLICY_ONLY",Tests=passed,
            OriginalSHA256=Codec.Hash(bytes),OriginalUnchanged=true,SyntheticDeathProjection=true,NativeDeathStillPending=true}));
        Console.WriteLine($"PASS_DEATH_RECORDING_POLICY_ONLY {passed.Count}");
    }
    internal static void Verify(string output,string original,string recordingMod)
    {
        var bytes=File.ReadAllBytes(original);var sample=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!.Data;
        var read=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(original))!).Read(sample.Id,sample.GameHash,"next-death-playback-build",sample.Scope,recordingMod);
        Codec.Require(read.Format==10&&read.ModHash==recordingMod&&read.Battle?.Completion=="death"&&read.Operations[^1] is {Defeat:true,Victory:false}&&
            Codec.Bytes(read).SequenceEqual(Codec.Bytes(sample)),"native-death-recording-production-readback");
        Codec.Require(NativePlaybackSupport.Reason(read) is not null,"recording-only-candidate-unexpectedly-offers-death-playback");
        Codec.Require(File.ReadAllBytes(original).SequenceEqual(bytes),"native-death-recording-was-mutated");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_ACTUAL_DEATH_RECORDING_PRODUCTION_READ_ONLY",read.Id,read.Format,
            Operations=read.Operations.Length,Completion=read.Battle!.Completion,FinalDefeat=read.Operations[^1].Defeat,
            OriginalSHA256=Codec.Hash(bytes),OriginalUnchanged=true,DeathPlaybackStillPending=true}));
        Console.WriteLine("PASS_ACTUAL_DEATH_RECORDING_PRODUCTION_READ_ONLY");
    }
}
