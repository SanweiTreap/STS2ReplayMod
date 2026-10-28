using ReplayMod;
using System.Text.Json;

internal static class QueueContracts
{
    internal static void VerifyRecording(string output,string path)
    {
        void Need(bool ok,string why){if(!ok)throw new Exception(why);}
        var original=File.ReadAllBytes(path);var env=JsonSerializer.Deserialize<ClipEnvelope>(original,Codec.Json)!;var clip=env.Data;
        const string recorder="6A4B1730418177CEC01CCDAE0A4D5FDE72541D34DEBD3BC2A83ED521F654EC10";
        var loaded=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(path))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","queue-direct-reader",clip.Scope,recorder);
        Need(clip.Format==6&&clip.ModHash==recorder&&Codec.Bytes(loaded).SequenceEqual(Codec.Bytes(clip)),"actual-queue-production-read");
        Need(clip.Operations.Length==13&&clip.Operations[^1].Victory,"actual-queue-victory");
        var a=clip.Operations[0];var b=clip.Operations[1];
        Need(a.Card=="CARD.DEFEND_IRONCLAD"&&b.Card==a.Card&&a.CardState!.CombatIndex!=b.CardState!.CombatIndex,"distinct-native-card-inputs");
        Need(b.Timing!.AcceptedMilliseconds<a.Timing!.CompletedMilliseconds,"no-actual-overlapping-input");
        var prior=clip.Initial;
        foreach(var op in clip.Operations)
        {
            using var before=JsonDocument.Parse(prior);
            if(op.CardState is {} key)
            {
                var hand=before.RootElement.GetProperty("Mechanics").GetProperty("CombatCards").EnumerateArray().Single(p=>p.GetProperty("Pile").GetString()=="Hand").GetProperty("Cards");
                var card=hand[op.HandIndex!.Value].GetProperty("Key").Deserialize<CardKey>(Codec.Json);
                Need(card==key,"execution-index-or-identity-does-not-match-prior-state");
            }
            prior=op.After;
        }
        using var first=JsonDocument.Parse(a.After);using var second=JsonDocument.Parse(b.After);
        var one=first.RootElement.GetProperty("Player");var two=second.RootElement.GetProperty("Player");
        Need(one.GetProperty("Energy").GetInt32()==2&&one.GetProperty("Block").GetInt32()==5&&
            two.GetProperty("Energy").GetInt32()==1&&two.GetProperty("Block").GetInt32()==10,"queued-results-mixed-or-double-applied");
        Need(original.SequenceEqual(File.ReadAllBytes(path)),"original-queue-file-changed");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="ACTUAL_QUEUED_RECORDING_PRODUCTION_READ_AND_BOUNDARIES_VERIFIED",clip.Id,clip.Format,
            OriginalSHA256=Codec.Hash(original),Operations=clip.Operations.Length,FirstCard=a.CardState!.CombatIndex,SecondCard=b.CardState!.CombatIndex,
            InputOverlapMilliseconds=a.Timing!.CompletedMilliseconds-b.Timing!.AcceptedMilliseconds,
            ExecutionKeysMatchPriorState=true,IndependentEnergyAndBlock=true,Victory=true,NativePlaybackVerified=false}));
        Console.WriteLine("ACTUAL_QUEUED_RECORDING_PRODUCTION_READ_AND_BOUNDARIES_VERIFIED");
    }
    internal static void Run(string output)
    {
        void Need(bool ok,string reason){if(!ok)throw new Exception(reason);}
        var requests=new AdvanceRequests();
        requests.Add();Need(requests.Take(),"first-step-not-submitted");
        requests.Add();requests.Add();Need(requests.Count==2,"two-clicks-during-first-move-not-retained");
        Need(requests.Take()&&requests.Take()&&!requests.Take()&&requests.Count==0,"three-clicks-not-exactly-three-nodes");
        var ahead=new AdvanceRequests();for(var i=0;i<3;i++)ahead.Add();
        Need(ahead.Take()&&ahead.Take()&&ahead.Count==1,"current-and-native-lookahead-not-counted-once");
        // A checkpoint publication consumes no second request for the already submitted lookahead.
        Need(ahead.Count==1&&ahead.Take()&&!ahead.Take(),"lookahead-checkpoint-double-consumed-request");
        foreach(var command in new[]{"pause","back","stop","terminal","new-session"})
        {requests.Add();requests.Add();Need(requests.Take(),command);requests.Clear();Need(!requests.Take()&&requests.Count==0,"stale-request-after-"+command);}
        // One choice-open, one selection and one confirm are three existing nodes, not one host.
        var cursor=new DecisionCursor();cursor.Append();
        for(var i=0;i<3;i++)requests.Add();
        for(var i=0;i<3;i++){Need(requests.Take()&&cursor.Step()&&cursor.Take()==1,"choice-node-request");cursor.Append();}
        Need(cursor.Position==3&&!requests.Take(),"choice-host-collapsed-or-duplicate-step");
        Need(cursor.Back()&&cursor.HasIntent&&cursor.Take()==-1,"back-reservation-not-visible");cursor.Arrive(2);
        var clip=new Clip(6,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],
            "CHARACTER.IRONCLAD","seed",1,0,"ordinary","checkpoint","initial",[
                new(1,OperationKind.Card,1,0,"CARD.DEFEND_IRONCLAD",null,"one",CardState:new(0,0,new string('A',64),"Self"),Timing:new(1000,1500)),
                new(2,OperationKind.Card,1,0,"CARD.DEFEND_IRONCLAD",null,"two",CardState:new(1,0,new string('B',64),"Self"),Timing:new(1100,1800)),
                new(3,OperationKind.EndTurn,1,null,null,null,"three",Timing:new(2000,3000))],Choices:[],
            Timing:new("checkpoint-stopwatch-elapsed-realtime-v1",500,true,true,true));
        Codec.Validate(clip,"game","mod","scope");
        bool Reject(Clip value){try{Codec.Validate(value,"game","mod","scope");return false;}catch(InvalidDataException){return true;}}
        Need(Reject(clip with{Format=5}),"old-format-overlap-contract-weakened");
        Need(Reject(clip with{Operations=clip.Operations.Select((o,i)=>i==1?o with{Timing=new(900,1800)}:o).ToArray()}),"accepted-order-reversed");
        Need(Reject(clip with{Operations=clip.Operations.Select((o,i)=>i==1?o with{Timing=new(1100,1400)}:o).ToArray()}),"completed-order-reversed");
        var folder=Path.GetDirectoryName(Path.GetFullPath(output))!;Directory.CreateDirectory(folder);
        var store=new ClipStore(Path.Combine(folder,"queue-file-fixture"));store.Seal(clip);var loaded=store.Read(clip.Id,"game","mod","scope");
        Need(Codec.Bytes(clip).SequenceEqual(Codec.Bytes(loaded)),"production-queue-time-readback");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_CUMULATIVE_REQUEST_AND_FORMAT6_OVERLAP_CONTRACTS",NativeQueueExecuted=false,
            Checks=new[]{"three clicks include current move once","cancel and generation clear unsent work","choice nodes remain distinct",
                "format6 accepts overlapping input with ordered completion","format5 serial time unchanged","reversed input/completion rejected","production readback preserves timestamps"}}));
        Console.WriteLine("PASS_CUMULATIVE_REQUEST_AND_FORMAT6_OVERLAP_CONTRACTS");
    }
}
