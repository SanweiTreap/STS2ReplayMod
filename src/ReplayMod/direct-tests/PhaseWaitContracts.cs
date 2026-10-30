using ReplayMod;

internal static class PhaseWaitContracts
{
    internal static async Task Run(string output)
    {
        var passed=new List<string>();var faults=new List<Exception>();
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        async Task Test(string name,Func<Task> run){await run();passed.Add(name);}
        await Test("normal-play-keeps-the-original-timer-and-error",async ()=>
        {
            var p=new RetiringPhaseWait(faults.Add);var timer=new TaskCompletionSource();var wait=p.Wrap(timer.Task);
            Need(!wait.IsCompleted&&!p.Active,"normal-timer-skipped");timer.SetResult();await wait;
            var bad=new TaskCompletionSource();wait=p.Wrap(bad.Task);bad.SetException(new InvalidOperationException("native-timer"));
            try{await wait;throw new Exception("timer-fault-hidden");}catch(InvalidOperationException e){Need(e.Message=="native-timer","wrong-timer-fault");}
        });
        await Test("back-releases-current-pure-wait-without-cancelling-native-task",async ()=>
        {
            var p=new RetiringPhaseWait(faults.Add);var timer=new TaskCompletionSource();var wait=p.Wrap(timer.Task);var business=0;
            async Task Continue(){await wait;business++;}
            var continuation=Continue();p.Begin();await continuation;
            Need(business==1&&!timer.Task.IsCompleted&&p.Released==1,"native-task-cancelled-or-continuation-repeated");
            timer.SetResult();await timer.Task;Need(business==1,"late-timer-repeated-business");p.End();
        });
        await Test("discarded-next-phase-can-skip-but-the-next-navigation-cannot",async ()=>
        {
            var p=new RetiringPhaseWait(faults.Add);p.Begin();var timer=new TaskCompletionSource();await p.Wrap(timer.Task);
            Need(p.Skipped==1&&!timer.Task.IsCompleted,"discarded-phase-wait-not-skipped");p.End();
            var next=new TaskCompletionSource();var wait=p.Wrap(next.Task);Need(!wait.IsCompleted,"old-release-leaked-to-new-forward");
            timer.SetResult();Need(!wait.IsCompleted,"old-timer-completion-released-new-phase");next.SetResult();await wait;
        });
        await Test("normal-cancellation-is-preserved-and-late-pure-timer-fault-is-observed",async ()=>
        {
            var p=new RetiringPhaseWait(faults.Add);var timer=new TaskCompletionSource();var wait=p.Wrap(timer.Task);timer.SetCanceled();
            try{await wait;throw new Exception("timer-cancellation-hidden");}catch(OperationCanceledException) { }
            timer=new();wait=p.Wrap(timer.Task);p.Begin();await wait;p.End();
            timer.SetException(new InvalidOperationException("retired-pure-timer"));
            Need(faults.Count==1&&faults[0].Message=="retired-pure-timer","late-timer-fault-unobserved");
        });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_PURE_PHASE_WAIT_RETIREMENT_ONLY",Tests=passed,
            NativeGameplayTasksExecuted=false,NativeCallerBindingAndStateRngAccelerationVerified=false}));
        Console.WriteLine($"PASS_PURE_PHASE_WAIT_RETIREMENT_ONLY {passed.Count}");
    }
}
