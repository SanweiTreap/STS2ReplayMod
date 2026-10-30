using System.Text.Json;
using ReplayMod;

internal static class FullPlaybackContracts
{
    internal static void VerifyRecording(string output,string path,string recordingMod)
    {
        var bytes=File.ReadAllBytes(path);var clip=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!.Data;
        Codec.Require(clip.ModHash==recordingMod&&clip.Format is >=6 and <=7&&clip.Operations[^1].Victory,"new-native-recording-metadata");
        var read=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(path))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","next-playback-build",clip.Scope,recordingMod);
        Codec.Require(Codec.Bytes(read).SequenceEqual(Codec.Bytes(clip))&&NativePlaybackSupport.Reason(read) is null,"new-native-recording-production-readback");
        var plan=new OperationRequests(read,read.Operations.Length);
        Codec.Require(plan.Route[^1].Complete&&plan.Route[^1].Host==read.Operations.Length,"new-recording-route-incomplete");
        Codec.Require(File.ReadAllBytes(path).SequenceEqual(bytes),"new-native-recording-mutated");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_ACTUAL_RECORDING_PRODUCTION_READ_AND_COMPATIBILITY_ONLY",read.Id,read.Format,
            read.CreatedUtc,read.ModHash,OriginalSHA256=Codec.Hash(bytes),Operations=read.Operations.Length,Choices=read.Choices!.Length,
            FinalVictory=read.Operations[^1].Victory,ExactPayloadMatched=true,OriginalUnchanged=true,NativeRuntimeNotExecutedByThisCheck=true}));
        Console.WriteLine("PASS_NEW_RECORDING_PRODUCTION_READ_NOT_NATIVE_RUNTIME");
    }
    internal static void Run(string output,string original)
    {
        var bytes=File.ReadAllBytes(original);var clip=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!.Data;
        var actual=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(original))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","full-playback-contract",clip.Scope,
            "DC8D7075E181245D79E76435C158084A6A0BB2313490A1BAB5B69A69F90092B9");
        var passed=new List<string>();
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        void Test(string name,Action run){run();passed.Add(name);}
        Need(Codec.Bytes(actual).SequenceEqual(Codec.Bytes(clip)),"full-original-production-read");
        Test("all27-recorded-operations-remain-targets-including-real-victory",()=>
        {
            var p=new OperationRequests(clip,clip.Operations.Length);
            Need(clip.Operations.Length==27&&clip.Operations[^1].Victory&&NativePlaybackSupport.Reason(clip) is null,"original-not-supported-full-victory");
            Need(p.Route.Where(n=>n.Complete).Select(n=>n.Host).SequenceEqual(Enumerable.Range(0,28)),"old22-slice-survived");
            p.ToggleAuto();
            foreach(var op in clip.Operations)
            {
                Need(p.Acquire()&&p.Active!.Target.Host==op.Sequence,"whole-route-skipped-host");
                foreach(var phase in p.Route.Where(n=>n.Host==op.Sequence))p.Arrive(phase);
            }
            Need(!p.Running&&!p.Auto&&p.Pending.Length==0&&!p.Acquire(),"terminal-auto-not-off-or-extra-operation-granted");
            p.Restore(26,new(true,false));p.Add(ForwardKind.Whole);Need(p.Acquire()&&p.Active!.Target.Host==27,"victory-back-could-not-replay-last-operation");
        });
        Test("planning-and-support-do-not-require-reference-id-or-27-operations",()=>
        {
            var changed=clip with{Id=Guid.NewGuid().ToString("N"),Operations=clip.Operations.Take(3).ToArray(),Choices=clip.Choices!.Where(c=>c.Boundary<=3).ToArray()};
            Need(NativePlaybackSupport.Reason(changed) is null,"support-requires-reference-id");
            var p=new OperationRequests(changed,changed.Operations.Length);
            Need(p.Route[^1] is {Host:3,Complete:true},"planner-requires-reference-length");
        });
        Test("initial-choice-is-setup-not-an-addressable-extra-operation",()=>
        {
            var initial=clip.Choices![0] with{Boundary=0};
            var changed=clip with{Choices=new[]{initial}.Concat(clip.Choices.Select(c=>c with{Sequence=c.Sequence+1})).ToArray()};
            Need(NativePlaybackSupport.Reason(changed) is null,"initial-choice-still-rejected-by-support");
            var p=new OperationRequests(changed,changed.Operations.Length);
            Need(p.Route[0] is {Host:0,Choice:0,Complete:true}&&p.Route.Count(n=>n.Host==0)==1,"setup-choice-created-restore-target");
        });
        Test("unsupported-normal-input-is-explained-without-legacy-fallback",()=>
        {
            Need(NativePlaybackSupport.Reason(clip with{Format=3}) is not null,"old-state-format-silently-accepted");
            Need(NativePlaybackSupport.Reason(clip with{Character="CHARACTER.DEFECT"}) is not null,"new-character-unlocked");
            var deck=clip.Choices![0] with{Kind="FromDeckGeneric",Automated=false};
            Need(NativePlaybackSupport.Reason(clip with{Choices=[deck]}) is not null,"unsupported-manual-grid-silently-accepted");
        });
        Need(File.ReadAllBytes(original).SequenceEqual(bytes),"original-mutated");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_FULL_NATIVE_ROUTE_POLICY_AND_ORIGINAL_FILE_ONLY",Tests=passed,
            OriginalSHA256=Codec.Hash(bytes),OriginalOperations=clip.Operations.Length,LastOperationVictory=clip.Operations[^1].Victory,
            InMemoryDifferentIdPlannerOnly=true,FreshRecordingAndVictoryBackNativeRuntimeStillPending=true,OldPlayback003="UnknownNotRead"}));
        Console.WriteLine("PASS_FULL_ROUTE_POLICY_NOT_NATIVE_VICTORY_OR_FRESH_RECORDING");
    }
}
