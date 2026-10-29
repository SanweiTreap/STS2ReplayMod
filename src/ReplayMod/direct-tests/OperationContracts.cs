using System.Text.Json;
using ReplayMod;

internal static class OperationContracts
{
    internal static void Run(string output,string original)
    {
        var bytes=File.ReadAllBytes(original);var clip=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!.Data;
        var read=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(original))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","operation-contract",clip.Scope,
            "DC8D7075E181245D79E76435C158084A6A0BB2313490A1BAB5B69A69F90092B9");
        var passed=new List<string>();
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        void Test(string name,Action run){run();passed.Add(name);}
        ForwardPoint End(OperationRequests p,int host)=>p.Route.Single(n=>n.Complete&&n.Host==host);
        OperationRequests Manual(int host){var p=new OperationRequests(clip,27);p.Restore(host,PlaybackIntent.ForNavigation(false,false));return p;}
        Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(clip)),"original-production-read");
        Test("open-defaults-to-manual-wait-and-first-request-runs-without-play",()=>
        {
            var p=new OperationRequests(clip,27);Need(p.Running&&!p.Auto&&p.ManualWaiting&&p.CanRequestFine&&!p.Acquire(),"default-not-manual-wait");
            var fine=p.Add(ForwardKind.Fine)!;Need(fine.Target.Host==1&&p.Acquire()&&p.Active==fine,"first-fine-needed-play");
        });
        Test("fine-rejected-in-auto-or-busy-but-whole-stays-available",()=>
        {
            var p=Manual(0);p.ToggleAuto();Need(!p.CanRequestFine&&p.Add(ForwardKind.Fine) is null&&p.Pending.Length==0,"fine-accepted-in-auto");
            p.ToggleAuto();var a=p.Add(ForwardKind.Whole)!;Need(!p.CanRequestFine&&p.Add(ForwardKind.Fine) is null&&p.Pending.Single()==a,"busy-fine-created-budget");
            Need(p.Acquire()&&p.Add(ForwardKind.Fine) is null&&p.Add(ForwardKind.Whole) is not null,"active-fine-accepted-or-whole-disabled");
        });
        Test("choice-fine-arrival-is-manual-wait-without-completing-native-host",()=>
        {
            var p=Manual(20);var fine=p.Add(ForwardKind.Fine)!;Need(fine.Target is {Host:21,Complete:false}&&p.Acquire(),"choice-fine-setup");p.Arrive(fine.Target);
            Need(p.Running&&!p.Auto&&p.ManualWaiting&&p.CanRequestFine&&!p.Acquire()&&p.Stage==fine.Target.Stage,"choice-phase-not-usable-wait");
            var next=p.Add(ForwardKind.Fine)!;Need(next.Target.Host==21&&next.Target.Stage>fine.Target.Stage&&p.Acquire()&&p.Active==next,"next-choice-fine-unavailable");
            Need(p.Route.Where(n=>n.Complete).Select(n=>n.Host).SequenceEqual(Enumerable.Range(0,28)),"choice-exposed-as-seek-anchor");
        });
        Test("waiting-at-choice-never-invents-whole-continuation",()=>
        {
            var p=Manual(20);var fine=p.Add(ForwardKind.Fine)!;p.Acquire();p.Arrive(fine.Target);
            for(var i=0;i<4;i++)Need(!p.Acquire()&&p.Active is null&&p.Pending.Length==0&&p.ManualWaiting&&p.Stage==fine.Target.Stage,"wait-invented-whole-budget");
            var whole=p.Add(ForwardKind.Whole)!;Need(whole.Target is {Host:21,Complete:true}&&p.Acquire()&&p.Active==whole,"whole-did-not-complete-current-choice-owner");
            foreach(var phase in p.Route.Where(n=>n.Host==21&&!n.Complete&&n.Stage>fine.Target.Stage))Need(p.Arrive(phase) is null&&p.Active==whole,"whole-lost-owner-at-inner-choice");
            p.Arrive(End(p,21));Need(p.ManualWaiting&&!p.Acquire(),"whole-started-unrequested-next-card");
        });
        Test("complete-card-fine-arrives-ready-for-next-click-without-play",()=>
        {
            var p=Manual(4);var fine=p.Add(ForwardKind.Fine)!;Need(fine.Target is {Host:5,Complete:true,Label:"CARD.SHIV"}&&p.Acquire(),"fine-card-setup");p.Arrive(fine.Target);
            Need(p.ManualWaiting&&p.CanRequestFine&&!p.Acquire(),"fine-card-closed-internal-gate");Need(p.Add(ForwardKind.Fine)!.Target.Host==6,"next-fine-needs-play");
        });
        Test("whole-requests-accumulate-fifo-while-current-host-executes",()=>
        {
            var p=Manual(0);var a=p.Add(ForwardKind.Whole)!;Need(p.Acquire(),"whole-not-immediate");var b=p.Add(ForwardKind.Whole)!;var c=p.Add(ForwardKind.Whole)!;
            Need(new[]{a,b,c}.Select(r=>r.Target.Host).SequenceEqual(new[]{1,2,3})&&!p.CanRequestFine,"whole-fifo-binding");p.Arrive(End(p,1));
            Need(p.Acquire()&&p.Active==b,"second-whole-lost");foreach(var phase in p.Route.Where(n=>n.Host==2&&!n.Complete))Need(p.Arrive(phase) is null&&p.Active==b&&p.Pending.Single()==c,"choice-spent-next-whole");
            p.Arrive(End(p,2));Need(p.Acquire()&&p.Active==c,"third-whole-lost");p.Arrive(End(p,3));Need(p.ManualWaiting&&!p.Acquire(),"finite-wholes-became-auto");
        });
        Test("fine-then-whole-preserves-owner-completion-and-following-host",()=>
        {
            var p=Manual(20);var a=p.Add(ForwardKind.Fine)!;var b=p.Add(ForwardKind.Whole)!;var c=p.Add(ForwardKind.Whole)!;
            Need(a.Target.Host==21&&b.Target.Host==21&&c.Target.Host==22&&p.Acquire(),"mixed-target-binding");p.Arrive(a.Target);
            Need(!p.ManualWaiting&&p.Acquire()&&p.Active==b,"mixed-owner-lost");p.Arrive(End(p,21));Need(p.Acquire()&&p.Active==c,"mixed-next-lost");p.Arrive(End(p,22));Need(p.ManualWaiting,"mixed-did-not-wait");
        });
        Test("auto-on-from-choice-wait-finishes-current-owner-and-disables-fine",()=>
        {
            var p=Manual(20);var fine=p.Add(ForwardKind.Fine)!;p.Acquire();p.Arrive(fine.Target);p.ToggleAuto();
            Need(p.Add(ForwardKind.Fine) is null&&p.Acquire()&&p.Active is {Automatic:true,Continuation:false,Target.Host:21},"auto-did-not-finish-owner");
            p.Arrive(End(p,21));Need(p.Acquire()&&p.Active is {Automatic:true,Target.Host:22},"auto-next-host");
        });
        Test("auto-off-keeps-started-whole-and-explicit-queue-until-natural-end",()=>
        {
            var p=Manual(20);p.ToggleAuto();Need(p.Acquire(),"auto-whole-start");var current=p.Active;var next=p.Add(ForwardKind.Whole)!;p.ToggleAuto();
            Need(p.Active==current&&!p.Auto&&!p.CanRequestFine&&p.Pending.Single()==next,"auto-off-cleared-active-or-explicit-budget");
            foreach(var phase in p.Route.Where(n=>n.Host==21&&!n.Complete))Need(p.Arrive(phase) is null&&p.Acquire()&&p.Active==current,"auto-off-cut-started-whole-at-choice");
            p.Arrive(End(p,21));Need(p.Acquire()&&p.Active==next,"auto-off-lost-explicit-next");p.Arrive(End(p,22));Need(p.ManualWaiting&&!p.Acquire(),"auto-off-appended-unrequested-host");
        });
        Test("mode-changes-do-not-discard-legally-accepted-fine-or-whole",()=>
        {
            var p=Manual(0);var fine=p.Add(ForwardKind.Fine)!;var whole=p.Add(ForwardKind.Whole)!;var pending=p.Pending;p.ToggleAuto();p.ToggleAuto();
            Need(p.Pending.SequenceEqual(pending)&&p.Acquire()&&p.Active==fine,"mode-switch-discarded-or-rebound-budget");p.Arrive(fine.Target);Need(p.Acquire()&&p.Active==whole,"mode-switch-lost-following-whole");
        });
        Test("seek-preserves-auto-intent-and-clears-plans-even-from-ended-internal-gate",()=>
        {
            foreach(var auto in new[]{false,true})
            {
                var p=Manual(0);if(auto)p.ToggleAuto();p.Add(ForwardKind.Whole);p.Acquire();p.Add(ForwardKind.Whole);
                var intent=PlaybackIntent.ForNavigation(auto,false);p.Seek(0);p.Restore(19,intent);
                Need(p.Running&&p.Auto==auto&&p.Active is null&&p.Pending.Length==0,"seek-intent-or-budget");Need(p.Acquire()==auto,"seek-wrong-continuation-mode");
                p.Stop();p.Restore(19,intent);Need(p.Running&&p.Auto==auto,"seek-from-ended-gate-stayed-hidden-paused");
            }
        });
        Test("back-restores-complete-boundary-in-manual-mode-with-no-old-plan",()=>
        {
            var p=Manual(20);p.ToggleAuto();p.Acquire();p.Add(ForwardKind.Whole);var intent=PlaybackIntent.ForNavigation(p.Auto,true);p.Seek(20);p.Restore(19,intent);
            Need(p.Running&&!p.Auto&&p.ManualWaiting&&p.CanRequestFine&&p.Active is null&&p.Pending.Length==0&&!p.Acquire(),"back-did-not-return-to-manual");
        });
        Test("native-whole-window-stays-three-and-reservations-do-not-consume-navigation",()=>
        {
            var p=Manual(19);for(var i=0;i<4;i++)p.Add(ForwardKind.Whole);var pending=p.Pending;
            Need(p.NativeQueueCandidates(19).SequenceEqual(pending.Take(3))&&p.Pending.SequenceEqual(pending),"native-candidate-window-or-side-effect");
            Need(p.Acquire()&&p.Active==pending[0]&&p.NativeQueueCandidates(19).SequenceEqual(pending.Take(3)),"active-request-missing-from-native-window");
        });
        Test("stop-and-real-original-input-integrity-remain",()=>
        {
            var p=Manual(0);p.Add(ForwardKind.Whole);p.Acquire();p.Add(ForwardKind.Whole);p.ToggleAuto();p.Stop();Need(!p.Running&&!p.Auto&&p.Active is null&&p.Pending.Length==0&&!p.Acquire(),"stop-retained-controls");
            var ops=clip.Operations[19..22];Need(ops.Select(o=>o.Card).SequenceEqual(new[]{"CARD.BURST","CARD.SURVIVOR","CARD.STRIKE_SILENT"})&&ops.All(o=>o.Turn==5)&&clip.Choices!.Count(c=>c.Boundary==21)==2,"original-real-input-lost");
        });
        Need(File.ReadAllBytes(original).SequenceEqual(bytes),"original-mutated");Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_D131_TWO_MODE_REQUEST_POLICY_AND_ORIGINAL_FILE_ONLY",Tests=passed,
            OriginalSHA256=Codec.Hash(bytes),DefaultMode="ManualWaiting",FineEligibility="ManualWaiting plus native readiness",PublicPlayPause=false,
            FineArrival="WaitForExplicitTarget",AutoOff="FinishCurrentWholeAndExplicitFifo",Back="ManualAutoOff",Seek="PreserveAutoClearPlans",
            NativeRuntimeStillPending=true,OldPlayback003="UnknownNotRead"}));
        Console.WriteLine("PASS_D131_TWO_MODE_POLICY_NOT_NATIVE_RUNTIME");
    }
}