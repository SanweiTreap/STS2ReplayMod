using System.Text.Json;
using ReplayMod;

internal static class OperationContracts
{
    internal static void Run(string output,string original)
    {
        var bytes=File.ReadAllBytes(original);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;var clip=envelope.Data;
        var read=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(original))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","operation-contract",clip.Scope,
            "DC8D7075E181245D79E76435C158084A6A0BB2313490A1BAB5B69A69F90092B9");
        var passed=new List<string>();
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        void Test(string name,Action run){run();passed.Add(name);}
        ForwardPoint End(OperationRequests p,int host)=>p.Route.Single(n=>n.Complete&&n.Host==host);
        Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(clip)),"operation-original-production-read");
        Test("three-whole-requests-bind-distinct-hosts-and-include-internal-choices",()=>
        {
            var p=new OperationRequests(clip,22);for(var i=0;i<3;i++)p.Add(ForwardKind.Whole);
            Need(p.Pending.Select(r=>r.Target.Host).SequenceEqual(new[]{1,2,3})&&!p.Acquire(),"paused-whole-binding");
            p.ToggleRunning();Need(p.Acquire()&&p.Active!.Target.Host==1,"first-whole-grant");p.Arrive(End(p,1));
            Need(p.Acquire()&&p.Active!.Target.Host==2&&p.Pending.Length==1,"second-whole-grant");
            foreach(var phase in p.Route.Where(n=>n.Host==2&&!n.Complete))
            {Need(p.Arrive(phase) is null&&p.Active!.Target.Host==2&&p.Pending.Length==1,"internal-choice-spent-another-request");}
            p.Arrive(End(p,2));Need(p.Acquire()&&p.Active!.Target.Host==3,"third-whole-lost");p.Arrive(End(p,3));
            Need(p.Running&&p.Pending.Length==0&&!p.Acquire(),"finite-whole-became-infinite-or-closed-gate");
        });
        Test("mixed-fine-whole-whole-fifo-completes-current-then-next-host",()=>
        {
            var p=new OperationRequests(clip,22);p.Seek(20);var fine=p.Add(ForwardKind.Fine)!;var b=p.Add(ForwardKind.Whole)!;var c=p.Add(ForwardKind.Whole)!;
            Need(!fine.Target.Complete&&fine.Target.Host==21&&b.Target is {Complete:true,Host:21}&&c.Target.Host==22,"mixed-request-targets");
            p.ToggleRunning();Need(p.Acquire(),"mixed-fine-grant");p.Arrive(fine.Target);
            Need(p.Running&&p.Acquire()&&p.Active==b,"fine-swallowed-following-explicit-whole");
            p.Arrive(End(p,21));Need(p.Acquire()&&p.Active==c,"mixed-wholes-bound-to-same-host");p.Arrive(End(p,22));
        });
        Test("fine-observation-pauses-with-auto-preserved-and-never-adds-seek-anchor",()=>
        {
            var p=new OperationRequests(clip,22);p.Seek(20);p.ToggleAuto();var fine=p.Add(ForwardKind.Fine)!;p.ToggleRunning();Need(p.Acquire(),"fine-auto-grant");
            p.Arrive(fine.Target);Need(!p.Running&&p.Auto&&!p.Acquire()&&p.Active is null,"auto-swallowed-fine-stop");
            Need(p.Route.Where(n=>n.Complete).Select(n=>n.Host).SequenceEqual(Enumerable.Range(0,23)),"choice-exposed-as-boundary");
            var whole=p.Add(ForwardKind.Whole)!;Need(whole.Target.Host==21,"partial-host-not-completed-first");
            p.ToggleRunning();Need(p.Acquire()&&p.Active==whole,"play-did-not-continue-partial-native-host");
        });
        Test("native-ahead-reservations-do-not-consume-or-rebind-pending-requests",()=>
        {
            var p=new OperationRequests(clip,22);p.Seek(19);for(var i=0;i<3;i++)p.Add(ForwardKind.Whole);p.ToggleRunning();Need(p.Acquire(),"ahead-first-grant");
            var reserved=p.Pending;Need(reserved.Select(r=>r.Target.Host).SequenceEqual(new[]{21,22}),"ahead-reservations");
            p.ToggleRunning();Need(!p.Acquire()&&p.Pending.SequenceEqual(reserved),"pause-lost-native-reservations");
            // Enqueue/cancellation changes native acceptance only. The production request FIFO
            // is untouched; B and C become grants only when allowed to START their operation.
            p.ToggleRunning();p.Arrive(End(p,20));Need(p.Acquire()&&p.Active==reserved[0],"requeued-b-consumed-twice");
            p.Arrive(End(p,21));Need(p.Acquire()&&p.Active==reserved[1]&&p.Pending.Length==0,"requeued-c-lost-or-consumed-twice");
            p.Arrive(End(p,22));Need(!p.Acquire(),"unrequested-after-c");
        });
        Test("play-directly-from-fine-stop-finishes-current-only-without-another-step",()=>
        {
            var p=new OperationRequests(clip,22);p.Seek(20);var fine=p.Add(ForwardKind.Fine)!;p.ToggleRunning();Need(p.Acquire(),"fine-setup");p.Arrive(fine.Target);
            Need(!p.Running&&!p.Auto&&p.Pending.Length==0&&p.Active is null,"fine-stop-setup");
            p.ToggleRunning();Need(p.Acquire()&&p.Active is {Continuation:true,Target.Host:21}&&p.Pending.Length==0,"direct-play-stalled-at-choice-or-spent-step");
            foreach(var phase in p.Route.Where(n=>n.Host==21&&!n.Complete&&n.Stage>fine.Target.Stage))
                Need(p.Arrive(phase) is null&&p.Active is {Continuation:true},"direct-play-needed-more-fine-steps");
            p.Arrive(End(p,21));Need(p.Running&&!p.Auto&&!p.Acquire()&&p.Pending.Length==0,"direct-play-started-unrequested-next-card");
            p.Seek(20);p.ToggleRunning();Need(!p.Acquire()&&p.Active is null,"play-invented-card-at-whole-boundary");
        });
        Test("seek-clears-active-and-pending-but-keeps-auto-stop-clears-all",()=>
        {
            var p=new OperationRequests(clip,22);p.ToggleAuto();p.ToggleRunning();p.Add(ForwardKind.Whole);p.Acquire();p.Add(ForwardKind.Fine);
            p.Seek(0);Need(!p.Running&&p.Auto&&p.Active is null&&p.Pending.Length==0,"seek-carried-request");
            p.Stop();Need(!p.Running&&!p.Auto&&p.Pending.Length==0&&!p.Acquire(),"stop-carried-controls");
        });
        Test("explicit-seek-restores-captured-gate-and-auto-clears-route-in-all-four-states",()=>
        {
            foreach(var running in new[]{false,true})foreach(var auto in new[]{false,true})
            {
                var p=new OperationRequests(clip,22);if(auto)p.ToggleAuto();if(running)p.ToggleRunning();
                p.Add(ForwardKind.Whole);var intent=p.Intent;
                // Internal retirement may close the gate and clear requests; final restore
                // must use the intent captured at REQUEST time, not this temporary state.
                p.Seek(0);p.Restore(19,intent);
                Need(p.Running==running&&p.Auto==auto&&p.Pending.Length==0&&p.Active is null,"seek-lost-request-time-intent-or-carried-old-route");
                Need(p.Acquire()==(running&&auto),"seek-started-unrequested-card-or-failed-to-resume-auto");
                p.Seek(18);Need(!p.Running&&p.Auto==auto&&p.Pending.Length==0&&p.Active is null,"back-no-longer-pauses");
            }
        });
        Test("paused-native-acceptance-does-not-grant-or-consume-navigation",()=>
        {
            var p=new OperationRequests(clip,22);p.Seek(19);for(var i=0;i<3;i++)p.Add(ForwardKind.Whole);
            var reservations=p.Pending;for(var i=0;i<4;i++)Need(!p.Acquire()&&p.Pending.SequenceEqual(reservations)&&p.Active is null,"native-acceptance-opened-business-gate");
            p.ToggleRunning();Need(p.Acquire()&&p.Active==reservations[0]&&p.Pending.Length==2,"play-consumed-reservation-more-than-once");
        });
        Test("paused-fine-no-choice-card-is-a-native-queue-candidate-without-starting-or-losing-its-stop",()=>
        {
            var p=new OperationRequests(clip,22);p.Seek(4);p.ToggleAuto();var fine=p.Add(ForwardKind.Fine)!;
            Need(fine.Target is {Host:5,Complete:true,Label:"CARD.SHIV"},"sample-fine-not-no-choice-shiv");
            for(var i=0;i<3;i++)Need(p.NativeQueueCandidates(4).SequenceEqual(new[]{fine})&&!p.Acquire()&&
                p.Pending.Single()==fine&&p.Stage==End(p,4).Stage,"fine-not-reserved-or-reservation-started-business");
            p.ToggleRunning();Need(p.Acquire()&&p.Active==fine,"fine-native-candidate-lost-fifo-grant");
            p.Arrive(fine.Target);Need(!p.Running&&p.Auto&&p.Active is null&&p.Pending.Length==0&&!p.Acquire(),"fine-card-lost-observation-stop");
            var next=p.Add(ForwardKind.Fine)!;Need(p.NativeQueueCandidates(5).Single()==next&&next.Target.Host==6,"next-paused-fine-card-lost");
        });
        Test("native-candidate-prefix-keeps-three-card-window-across-explicit-complete-fine-and-blocks-internal-choice",()=>
        {
            var p=new OperationRequests(clip,22);p.Seek(3);for(var i=0;i<4;i++)p.Add(ForwardKind.Whole);
            Need(p.NativeQueueCandidates(3).Select(r=>r.Target.Host).SequenceEqual(new[]{4,5,6})&&p.Pending.Length==4,"whole-window-changed");
            p.Seek(3);var whole=p.Add(ForwardKind.Whole)!;var fine=p.Add(ForwardKind.Fine)!;var following=p.Add(ForwardKind.Whole)!;
            Need(p.NativeQueueCandidates(3).SequenceEqual(new[]{whole,fine,following})&&p.Pending.Length==3,"fine-blocked-explicit-following-host");
            p.ToggleRunning();Need(p.Acquire(),"mixed-prefix-active-setup");
            Need(p.NativeQueueCandidates(3).SequenceEqual(new[]{whole,fine,following}),"active-request-lost-from-native-prefix");
            p.Seek(1);var choice=p.Add(ForwardKind.Fine)!;p.Add(ForwardKind.Whole);
            Need(!choice.Target.Complete&&p.NativeQueueCandidates(1).Length==0&&p.Pending.Length==2,"internal-choice-fine-incorrectly-treated-as-complete-card");
        });
        Test("paused-fine-whole-fine-reserves-three-explicit-cards-and-stops-only-at-last-fine-with-auto-preserved",()=>
        {
            var p=new OperationRequests(clip,22);p.Seek(4);p.ToggleAuto();
            var a=p.Add(ForwardKind.Fine)!;var b=p.Add(ForwardKind.Whole)!;var c=p.Add(ForwardKind.Fine)!;
            var requests=new[]{a,b,c};
            Need(requests.Select(r=>r.Target.Host).SequenceEqual(new[]{5,6,7})&&requests.All(r=>r.Target.Complete),"mixed-complete-card-targets");
            for(var i=0;i<3;i++)Need(p.NativeQueueCandidates(4).SequenceEqual(requests)&&p.Pending.SequenceEqual(requests)&&
                !p.Running&&!p.Acquire(),"mixed-prequeue-opened-gate-consumed-request-or-truncated-at-fine");
            p.ToggleRunning();Need(p.Acquire()&&p.Active==a,"mixed-first-fine-grant");p.Arrive(a.Target);
            Need(p.Running&&p.Acquire()&&p.Active==b,"first-fine-stopped-before-explicit-whole");p.Arrive(b.Target);
            Need(p.Running&&p.Acquire()&&p.Active==c,"whole-lost-final-fine");p.Arrive(c.Target);
            Need(!p.Running&&p.Auto&&p.Pending.Length==0&&p.Active is null&&!p.Acquire()&&
                p.NativeQueueCandidates(7).Length==0,"last-fine-lost-stop-or-invented-auto-prequeue-target");
        });
        Test("original-recording-contains-burst-survivor-strike-in-same-turn",()=>
        {
            var ops=clip.Operations[19..22];Need(ops.Select(o=>o.Card).SequenceEqual(new[]{"CARD.BURST","CARD.SURVIVOR","CARD.STRIKE_SILENT"})&&ops.All(o=>o.Turn==5),"sample-sequence-invented");
            Need(clip.Choices!.Count(c=>c.Boundary==21)==2,"sample-missing-hand-choice");
            using var before=JsonDocument.Parse(clip.Operations[18].After);
            var hand=before.RootElement.GetProperty("Mechanics").GetProperty("CombatCards").EnumerateArray().Single(p=>p.GetProperty("Pile").GetString()=="Hand").GetProperty("Cards");
            foreach(var op in ops)Need(hand.EnumerateArray().Any(c=>JsonSerializer.Deserialize<CardKey>(c.GetProperty("Key"),Codec.Json)==op.CardState),"ahead-card-not-already-in-original-hand");
        });
        Need(File.ReadAllBytes(original).SequenceEqual(bytes),"original-mutated");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_OPERATION_REQUEST_PLANNER_AND_ORIGINAL_FILE_ONLY",Tests=passed,
            OriginalSHA256=Codec.Hash(bytes),NativeAcceptanceCancellationReentryExecuted=false,NativeRuntimeStillPending=true,OldPlayback003="UnknownNotRead"}));
        Console.WriteLine("PASS_OPERATION_REQUESTS_AND_ORIGINAL_FILE_NOT_NATIVE_QUEUE_EXECUTION");
    }
}
