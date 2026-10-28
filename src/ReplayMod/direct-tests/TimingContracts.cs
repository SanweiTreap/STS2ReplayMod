using ReplayMod;

internal static class TimingContracts
{
    internal static void Run(string output)
    {
        var checks=new List<string>();
        var clip=new Clip(5,new string('b',32),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],
            "CHARACTER.REGENT","seed",1,0,"ordinary","checkpoint","initial",[
                new(1,OperationKind.Card,1,0,"CARD.REFINE_BLADE",null,"one",CardState:new(0,0,new string('A',64),"Self"),Timing:new(1000,1250)),
                new(2,OperationKind.Potion,1,null,null,null,"two",Potion:new(0,"POTION.STAR_POTION",new string('B',64),"AnyPlayer"),Timing:new(7250,7500)),
                new(3,OperationKind.EndTurn,1,null,null,null,"three",Timing:new(8000,9500))],Choices:[],
            Timing:new("checkpoint-stopwatch-elapsed-realtime-v1",500,true,true,true));
        void Check(string name,Clip data,bool accept)
        {
            var actual=true;try{Codec.Validate(data,"game","mod","scope");}catch(InvalidDataException){actual=false;}
            if(actual!=accept)throw new Exception(name);checks.Add(name);
        }
        Check("Regent-format5-card-potion-endturn-unified-monotonic-time",clip,true);
        Check("missing-recording-basis-rejected",clip with{Timing=null},false);
        Check("missing-operation-time-rejected",clip with{Operations=clip.Operations.Select((o,i)=>i==0?o with{Timing=null}:o).ToArray()},false);
        Check("initial-load-is-not-thought-time",clip with{Timing=clip.Timing! with{InitialReadyMilliseconds=1100}},false);
        Check("nonfinite-time-rejected",clip with{Operations=clip.Operations.Select((o,i)=>i==1?o with{Timing=new(double.NaN,7500)}:o).ToArray()},false);
        Check("negative-duration-rejected",clip with{Operations=clip.Operations.Select((o,i)=>i==1?o with{Timing=new(7600,7500)}:o).ToArray()},false);
        Check("overlapping-or-reversed-operations-rejected",clip with{Operations=clip.Operations.Select((o,i)=>i==1?o with{Timing=new(1200,7500)}:o).ToArray()},false);
        ChoiceCard card=new("CARD.SOVEREIGN_BLADE",0,true,13,null,new string('C',64),"blade");
        var choice=new ChoiceFrame(1,1,1,"FromHand","CARD.DUAL_WIELD",1,1,false,true,[[card]],[0],1050,100,"CombatCard",0,false);
        Check("choice-begin-duration-mapped-to-operation-clock",clip with{Choices=[choice]},true);
        Check("choice-outside-host-time-rejected",clip with{Choices=[choice with{BeganMilliseconds=1200,DurationMilliseconds=100}]},false);
        Check("initial-choice-mapped-before-initial-ready",clip with{Choices=[choice with{Boundary=0,BeganMilliseconds=100,DurationMilliseconds=200}]},true);
        var old=clip with{Character="CHARACTER.SILENT",Format=4,Timing=null,Operations=clip.Operations.Select(o=>o with{Timing=null}).ToArray()};
        Check("old-character-file-still-accepted-with-time-unknown",old,true);
        Check("old-file-cannot-claim-new-time-contract",old with{Timing=clip.Timing},false);
        Check("Regent-not-enabled-for-legacy-format",old with{Character="CHARACTER.REGENT"},false);
        Directory.CreateDirectory(output);
        var store=new ClipStore(Path.Combine(output,"timing-contract-fixture"));var id=Guid.NewGuid().ToString("N");store.Seal(clip with{Id=id});
        var loaded=store.Read(id,"game","mod","scope");
        if(!Codec.Bytes(loaded).SequenceEqual(Codec.Bytes(clip with{Id=id}))||loaded.Operations[1].Timing!.AcceptedMilliseconds-loaded.Operations[0].Timing!.CompletedMilliseconds!=6000)
            throw new Exception("timing-production-readback");
        checks.Add("production-store-preserves-accepted-completed-and-explicit-gap");
        File.WriteAllBytes(Path.Combine(output,"timing-contracts.json"),Codec.Bytes(new{Result="PASS_TIMING_AND_REGENT_FILE_CONTRACTS",Checks=checks,
            NativeGameExecuted=false,ActualHumanPauseVerified=false,FixtureGapMilliseconds=6000,OldMissingTimeNotInvented=true}));
        Console.WriteLine($"PASS_TIMING_CONTRACTS {checks.Count}");
    }
}
