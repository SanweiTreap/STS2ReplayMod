using System.Text.Json;
using ReplayMod;

internal static class DiscardPotionContracts
{
    internal static void Run(string output,string regentPath)
    {
        var tests=new List<string>();
        void Test(string name,Action action){action();tests.Add(name);}
        var bytes=File.ReadAllBytes(regentPath);
        var old=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!.Data;
        var store=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(regentPath))!);
        Test("existing-Regent-format5-production-read-remains-byte-equivalent-and-supported",()=>
        {
            var read=store.Read(old.Id,old.GameHash,"content005-candidate",old.Scope,old.ModHash);
            Codec.Require(old.Character=="CHARACTER.REGENT"&&old.Operations.Length==13&&old.Format==5&&
                Codec.Bytes(read).SequenceEqual(Codec.Bytes(old))&&NativePlaybackSupport.Reason(read) is null,"old-regent-changed");
        });
        var potion=old.Operations[0].Potion!;
        var discard=new Operation(1,OperationKind.DiscardPotion,1,null,null,null,"discarded",Potion:potion,Timing:new(10,20));
        var sample=old with{Format=7,Id=Guid.NewGuid().ToString("N"),Initial="initial",Choices=[],Authority=null,
            Timing=new("checkpoint-stopwatch-elapsed-realtime-v1",0,true,true,true),
            Operations=[discard,new(2,OperationKind.EndTurn,1,null,null,null,"next-turn",Timing:new(30,40))]};
        void Validate(Clip clip)=>Codec.Validate(clip,old.GameHash,old.ModHash,old.Scope);
        void Reject(Clip clip)
        {
            var rejected=false;try{Validate(clip);}catch(InvalidDataException){rejected=true;}
            Codec.Require(rejected,"invalid-discard-accepted");
        }
        Test("format7-discard-retains-slot-model-fingerprint-and-time-in-production-file",()=>
        {
            Validate(sample);var directory=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"discard-clips");
            var scratch=new ClipStore(directory);scratch.Seal(sample);
            var read=scratch.Read(sample.Id,old.GameHash,old.ModHash,old.Scope);
            Codec.Require(Codec.Bytes(read).SequenceEqual(Codec.Bytes(sample))&&read.Operations[0].Potion==potion&&
                read.Operations[0].Kind==OperationKind.DiscardPotion&&NativePlaybackSupport.Reason(read) is null,"discard-roundtrip");
        });
        Test("discard-is-not-valid-in-old-formats-and-cannot-carry-use-target-or-card",()=>
        {
            Reject(sample with{Format=6});
            Reject(sample with{Operations=[discard with{Target=new(0,"CHARACTER.REGENT",0,TargetRole.Player)},sample.Operations[1]]});
            Reject(sample with{Operations=[discard with{Card="CARD.STRIKE_REGENT"},sample.Operations[1]]});
            Reject(sample with{Operations=[discard with{Potion=potion with{Slot=16}},sample.Operations[1]]});
            Reject(sample with{Operations=[discard with{Potion=potion with{Fingerprint="unknown"}},sample.Operations[1]]});
        });
        Test("discard-is-one-whole-boundary-and-restored-manual-plan-can-request-it-again",()=>
        {
            var plan=new OperationRequests(sample,2);plan.Add(ForwardKind.Fine);
            Codec.Require(plan.Acquire()&&plan.Active!.Target is {Host:1,Complete:true},"discard-not-single-fine");
            plan.Arrive(plan.Active!.Target);plan.Restore(0,PlaybackIntent.ForNavigation(false,true));plan.Add(ForwardKind.Whole);
            Codec.Require(plan.Acquire()&&plan.Active!.Target is {Host:1,Complete:true}&&!plan.Auto,"discard-after-back-plan");
        });
        Codec.Require(File.ReadAllBytes(regentPath).SequenceEqual(bytes),"old-regent-file-mutated");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_DISCARD_FORMAT_AND_ROUTE_ONLY",Tests=tests,
            OldRegentSHA256=Codec.Hash(bytes),OldFileUnchanged=true,NativeDiscardAndBackRuntimeExecuted=false}));
        Console.WriteLine($"PASS_DISCARD_FORMAT_AND_ROUTE_ONLY {tests.Count}");
    }
}
