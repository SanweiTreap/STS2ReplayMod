using System.Text.Json;
using ReplayMod;

internal static class OpeningContracts
{
    internal static void Run(string output,string original)
    {
        var bytes=File.ReadAllBytes(original);var clip=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!.Data;
        var passed=new List<string>();
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        void Test(string name,Action run){run();passed.Add(name);}
        OperationRequests New()=>new(clip,clip.Operations.Length,opening:true);
        ForwardPoint End(OperationRequests p,int host)=>p.Route.Single(n=>n.Complete&&n.Host==host);
        var choice=clip.Choices!.First(c=>c.Boundary==0&&!c.Automated);
        ForwardPoint Open(OperationRequests p)=>p.Route.First(n=>n.Choice==choice.Sequence);
        Test("natural-initial-choice-waits-without-consuming-operation1",()=>
        {
            var p=New();Need(p.Route[0] is {Host:-1,Complete:true},"no-pre-opening-target");
            p.Arrive(Open(p));Need(p.ManualWaiting&&!p.Acquire(),"opening-choice-auto-submitted");
            Need(p.Route.Where(n=>n.Complete).Select(n=>n.Host).SequenceEqual(Enumerable.Range(-1,clip.Operations.Length+2)),"choice-became-seek-point");
        });
        Test("fine-opening-advances-only-next-decision-phase",()=>
        {
            var p=New();p.Arrive(Open(p));var next=p.Route.First(n=>n.Stage>p.Stage);
            var fine=p.Add(ForwardKind.Fine)!;Need(fine.Target==next&&fine.Target.Host==0&&p.Acquire(),"fine-not-next-opening-phase");
            p.Arrive(next);Need(p.ManualWaiting&&!p.Acquire(),"fine-invented-continuation");
        });
        Test("one-whole-finishes-opening-at-zero-and-never-requests-operation1",()=>
        {
            foreach(var fromStart in new[]{true,false})
            {
                var p=New();if(!fromStart)p.Arrive(Open(p));var whole=p.Add(ForwardKind.Whole)!;
                Need(whole.Target==End(p,0)&&p.Acquire(),"whole-opening-target-not-zero");
                foreach(var phase in p.Route.Where(n=>n.Host==0&&n.Stage>p.Stage).ToArray())p.Arrive(phase);
                Need(p.Stage==End(p,0).Stage&&p.ManualWaiting&&!p.Acquire()&&p.Pending.Length==0,"whole-spilled-into-operation1");
                p.Add(ForwardKind.Whole);Need(p.Acquire()&&p.Active!.Target.Host==1,"next-whole-not-operation1");
            }
        });
        Test("auto-finishes-opening-before-operation1",()=>
        {
            var p=New();p.ToggleAuto();Need(p.Acquire()&&p.Active!.Target.Host==0,"auto-skipped-opening");
            foreach(var phase in p.Route.Where(n=>n.Host==0))p.Arrive(phase);
            Need(p.Auto&&p.Acquire()&&p.Active!.Target.Host==1,"auto-did-not-continue-after-zero");
        });
        Test("back-to-opening-clears-fifo-and-auto-and-restores-requestable-start",()=>
        {
            var p=New();p.ToggleAuto();p.Add(ForwardKind.Whole);p.Add(ForwardKind.Whole);p.Acquire();
            p.Restore(-1,PlaybackIntent.ForNavigation(p.Auto,back:true));
            Need(p.ManualWaiting&&!p.Auto&&p.Active is null&&p.Pending.Length==0&&!p.Acquire()&&p.Stage==0,"back-retained-opening-plans");
            var fine=p.Add(ForwardKind.Fine)!;Need(fine.Target==Open(p)&&p.Acquire(),"reentry-cannot-fine-to-choice");
        });
        Test("no-choice-natural-open-still-arrives-zero-and-seek-preserves-auto",()=>
        {
            var p=new OperationRequests(clip with{Choices=[]},clip.Operations.Length,opening:true);
            p.Arrive(End(p,0));Need(p.ManualWaiting&&!p.Acquire(),"no-choice-open-needs-request-or-starts-op1");
            p.Restore(-1,PlaybackIntent.ForNavigation(true,back:false));Need(p.Auto&&p.Acquire()&&p.Active!.Target.Host==0,"opening-seek-lost-auto");
        });
        Test("multiple-opening-choices-share-one-whole-target",()=>
        {
            var choices=new[]{choice with{Sequence=1},choice with{Sequence=2}};
            var p=new OperationRequests(clip with{Choices=choices},clip.Operations.Length,opening:true);
            p.Add(ForwardKind.Whole);p.Acquire();
            foreach(var phase in p.Route.Where(n=>n.Host==0&&!n.Complete))
            {p.Arrive(phase);Need(p.Active?.Target.Host==0,"whole-released-between-opening-choices");}
            p.Arrive(End(p,0));Need(p.ManualWaiting&&!p.Acquire(),"whole-not-finished-at-playable-zero");
        });
        Need(File.ReadAllBytes(original).SequenceEqual(bytes),"opening-original-mutated");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_OPENING_REQUEST_POLICY_ONLY",Tests=passed,
            OriginalSHA256=Codec.Hash(bytes),RealGameExecuted=false,NativeSceneReentryAndTimingVerified=false}));
        Console.WriteLine($"PASS_OPENING_REQUEST_POLICY_ONLY {passed.Count}");
    }
}
