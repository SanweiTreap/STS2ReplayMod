using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using Spike001B;
using Spike001B.Core;

public static class ActionObservationRegression
{
    public static int Run(string output,Binding binding,string[] states,AcceptedAction[] actions)
    {
        Directory.CreateDirectory(output);var count=0;
        void Test(string name,Action body){body();count++;Console.WriteLine("PASS action-diagnostics-r2-"+name);}
        Test("four-persisted-action-waits-distinct",()=>
        {
            using var queued=new Case(output,"queued-only");queued.Start();queued.Timeout();
            using var executed=new Case(output,"executed-no-effect");executed.Start();executed.Observe(ActionSite.BeforeExecuted,()=>executed.Adapter.Executed());executed.Timeout();
            using var effect=new Case(output,"effect-no-finished");effect.Start();effect.Observe(ActionSite.BeforeExecuted,()=>effect.Adapter.Executed());
            var task=new TaskCompletionSource();effect.Observe(ActionSite.CardEffect,()=>effect.Adapter.CardEffect(task.Task.Status));task.SetResult();effect.Timeout();
            using var end=new Case(output,"end-ready-no-finished");end.Start(Operation.EndTurn);end.Observe(ActionSite.BeforeExecuted,()=>end.Adapter.Executed());end.Observe(ActionSite.EndReady,()=>end.Adapter.EndReady());end.Timeout();
            var values=new[]{queued.Terminal(),executed.Terminal(),effect.Terminal(),end.Terminal()};
            Check(values.Select(Json.Serialize).Distinct().Count()==4 && values.All(e=>e.Reason=="deadline-Action"));
            Check(values[0].ActionBlocker=="BeforeExecuted" && values[0].Action is{Requested:true,Executed:false,Site:ActionSite.Queue,EffectTaskStatus:null});
            Check(values[1].ActionBlocker=="CardEffect" && values[1].Action is{Executed:true,CardEffectSeen:false,Site:ActionSite.BeforeExecuted});
            Check(values[2].ActionBlocker=="AfterFinished" && values[2].Action is{CardEffectSeen:true,Finished:false,Site:ActionSite.CardEffect,EffectTaskStatus:TaskStatus.WaitingForActivation});
            Check(values[3].ActionBlocker=="AfterFinished" && values[3].Action is{EndReadySeen:true,Finished:false,EffectTaskStatus:null,Site:ActionSite.EndReady});
            Check(values[2].Action!.EffectTaskStatus!=task.Task.Status); // A timer must not turn the last observation into live Task sampling.
            File.WriteAllText(Path.Combine(output,"four-waits-readback.json"),Json.Serialize(new{Source="PERSISTED_SYNTHETIC_CALLBACKS_NOT_RUNTIME",Values=values}));
        });
        foreach(var pair in new[]{(ActionSite.Queue,"wrong-card-instance",ActionReject.CardBinding),(ActionSite.CardEffect,"card-effect-origin",ActionReject.CardEffectOrigin),
            (ActionSite.EndReady,"end-not-accepted-transition",ActionReject.EndReadyTransition),(ActionSite.AfterFinished,"no-accepted-effect",ActionReject.MissingAcceptedEffect)})
        Test("controlled-rejection-"+pair.Item1,()=>
        {using var c=new Case(output,"reject-"+pair.Item1);c.Start();Check(!c.Adapter.Guard(pair.Item1,()=>ActionObservation.Require(false,pair.Item2)));var e=c.Terminal();Check(e.Action!.Site==pair.Item1 && e.Action.RejectionSite==pair.Item1 && e.Action.Rejection==pair.Item3 && e.ActionBlocker=="Rejected");Check(!c.Flow.ActionSucceeded(false));});
        Test("unknown-contract-and-unexpected-exception-redacted",()=>
        {
            foreach(var unexpected in new[]{false,true})
            {using var c=new Case(output,"private-rejection-"+unexpected);c.Start();c.Adapter.Guard(ActionSite.Queue,()=>{if(unexpected)throw new IOException("SECRET NetId path / stack / Action.ToString");throw new InvalidDataException("SECRET arbitrary contract text");});
                var e=c.Terminal();Check(!Json.Serialize(e).Contains("SECRET") && !Json.Serialize(e).Contains("IOException") && e.Action!.Rejection==ActionReject.UnexpectedObserver);}
        });
        Test("production-message-cannot-impersonate-controlled-code",()=>
        {using var c=new Case(output,"impostor-message");c.Start();c.Adapter.Guard(ActionSite.CardEffect,()=>throw new InvalidDataException("card-effect-origin"));Check(c.Terminal().Action!.Rejection==ActionReject.UnexpectedObserver);});
        Test("pure-Core-contract-has-explicit-trusted-code",()=>
        {using var c=new Case(output,"trusted-core");c.Start();c.Adapter.Guard(ActionSite.Queue,()=>ActionObservation.Core(()=>throw new InvalidDataException("PRIVATE text"),ActionReject.PreState));Check(c.Terminal().Action!.Rejection==ActionReject.PreState && !Json.Serialize(c.Terminal()).Contains("PRIVATE"));});
        Test("after-finished-fault-flags-retained-before-rejection",()=>
        {
            using var c=new Case(output,"finished-fault");c.Start();c.Observe(ActionSite.BeforeExecuted,()=>c.Adapter.Executed());c.Observe(ActionSite.CardEffect,()=>c.Adapter.CardEffect(TaskStatus.WaitingForActivation));
            c.Adapter.Guard(ActionSite.AfterFinished,()=>{c.Adapter.Finished(TaskStatus.Faulted,true);ActionObservation.Require(false,"action-completion-failed");});
            Check(c.Terminal().Action is{Finished:true,ProductionExceptionObserved:true,EffectTaskStatus:TaskStatus.Faulted,Rejection:ActionReject.ActionCompletion});
        });
        Test("cancel-and-choice-not-accepted",()=>
        {
            foreach(var choice in new[]{false,true}){using var c=new Case(output,"cancel-choice-"+choice);c.Start();c.Observe(choice?ActionSite.Choice:ActionSite.Cancel,()=>{if(choice)c.Adapter.Choice();else c.Adapter.Cancelled();c.Flow.Fail(choice?"unexpected-action-choice":"action-cancelled-not-accepted");});
                var d=c.Terminal().Action!;Check(d.Choice==choice && d.Cancelled==!choice && !d.Finished && !c.Flow.ActionSucceeded(false));}
        });
        Test("terminal-frozen-and-copy-does-not-alias",()=>
        {
            using var c=new Case(output,"frozen");c.Start();c.Observe(ActionSite.BeforeExecuted,()=>c.Adapter.Executed());c.Timeout();var before=Json.Serialize(c.Terminal());var copy=c.Flow.ActionDiagnostics!;
            Check(!ReferenceEquals(copy,c.Flow.ActionDiagnostics));copy=copy with{Finished=true};var writes=c.Attempts;bool ran=false;
            Check(!c.Adapter.Guard(ActionSite.AfterFinished,()=>ran=true) && !ran);Check(!c.Flow.ObserveAction(copy));c.Flow.Poll();c.Flow.Fail("second");
            Check(c.Attempts==writes && Json.Serialize(c.Terminal())==before && !c.Flow.ActionDiagnostics!.Finished);
        });
        Test("same-observation-duplicate-not-per-frame-IO",()=>
        {
            using var c=new Case(output,"duplicate");c.Start();c.Observe(ActionSite.BeforeExecuted,()=>c.Adapter.Executed());var writes=c.Attempts;
            for(var i=0;i<10000;i++)c.Adapter.Executed();Check(c.Attempts==writes);c.Timeout();Check(c.Terminal().Action!.ObservationIndex==3);
        });
        Test("count-byte-limits-and-one-terminal",()=>
        {
            using var c=new Case(output,"limits");c.Start();for(var i=0;i<100;i++)c.Adapter.CardEffect(i%2==0?TaskStatus.Running:TaskStatus.WaitingForActivation);
            var e=c.Terminal();Check(e.Reason=="action-diagnostic-limit" && e.Action!.ObservationIndex==Flow.MaxActionObservations);
            Check(c.Events().Count(e=>e.Name=="action-observed")==32 && c.Events().Count(e=>e.Name=="terminal")==1 && c.Sizes().All(b=>b<=Flow.MaxEventBytes));
        });
        Test("observation-writer-failure-terminal-has-last-facts",()=>
        {
            using var c=new Case(output,"writer-failure",e=>e.Name=="action-observed" && e.Action?.Site==ActionSite.CardEffect);c.Start();c.Observe(ActionSite.BeforeExecuted,()=>c.Adapter.Executed());c.Observe(ActionSite.CardEffect,()=>c.Adapter.CardEffect(TaskStatus.Running));
            var e=c.Terminal();Check(e.Reason=="evidence-write-failure" && e.Action is{CardEffectSeen:true,Site:ActionSite.CardEffect,EffectTaskStatus:TaskStatus.Running});var writes=c.Attempts;
            c.Adapter.Finished(TaskStatus.RanToCompletion,false);Check(c.Attempts==writes && !c.Flow.ActionSucceeded(false));
        });
        Test("terminal-writer-fails-no-retry-or-resume",()=>
        {
            using var c=new Case(output,"terminal-writer-failure",e=>e.Name=="terminal");c.Start();c.Observe(ActionSite.BeforeExecuted,()=>c.Adapter.Executed());c.Timeout();var writes=c.Attempts;
            Check(c.Flow.Stage==Stage.Terminal && c.Flow.ActionDiagnostics is{Executed:true,CardEffectSeen:false});c.Adapter.CardEffect(TaskStatus.RanToCompletion);c.Flow.Poll();c.Flow.Fail("again");
            Check(c.Attempts==writes && !c.Flow.ActionSucceeded(false));
        });
        Test("controlled-rejection-writer-failure-first-terminal",()=>
        {
            using var c=new Case(output,"reject-writer",e=>e.Name=="action-observed" && e.Action?.Rejection!=ActionReject.None);c.Start();
            c.Adapter.Guard(ActionSite.CardEffect,()=>ActionObservation.Require(false,"card-effect-origin"));
            Check(c.Terminal().Reason=="evidence-write-failure" && c.Terminal().Action!.Rejection==ActionReject.CardEffectOrigin);
        });
        Test("writer-deadline-callback-race-freezes-one-summary",()=>
        {
            using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
            using var c=new Case(output,"race",e=>{if(e.Name!="action-observed" || e.Action?.Site!=ActionSite.CardEffect)return false;entered.Set();Check(release.Wait(TimeSpan.FromSeconds(3)));return true;});
            c.Start();c.Observe(ActionSite.BeforeExecuted,()=>c.Adapter.Executed());var writer=Task.Run(()=>c.Adapter.CardEffect(TaskStatus.WaitingForActivation));Check(entered.Wait(TimeSpan.FromSeconds(3)));
            c.Clock.Now+=20000;var deadline=Task.Run(()=>c.Flow.Poll());var callback=Task.Run(()=>c.Adapter.Guard(ActionSite.AfterFinished,()=>c.Adapter.Finished(TaskStatus.RanToCompletion,false)));release.Set();Task.WaitAll(writer,deadline,callback);
            Check(c.Terminal().Reason=="evidence-write-failure" && c.Terminal().Action is{CardEffectSeen:true,Finished:false} && c.Events().Count(e=>e.Name=="terminal")==1);
        });
        Test("invalid-diagnostic-enum-never-exposes-free-data",()=>
        {
            using var c=new Case(output,"invalid-shape");c.Start();Check(!c.Flow.ObserveAction(c.Flow.ActionDiagnostics! with{Site=(ActionSite)999}));Check(c.Terminal().Reason=="action-diagnostic-shape" && c.Terminal().Action!.Site==ActionSite.Queue);
        });
        Test("DTO-no-live-or-free-text-reference-shape",()=>
        {
            foreach(var p in typeof(ActionDiagnostics).GetProperties().Where(p=>p.Name!="EqualityContract"))
            {var type=Nullable.GetUnderlyingType(p.PropertyType)??p.PropertyType;Check(type.IsEnum || type==typeof(int) || type==typeof(bool));}
        });
        Test("r1-activation-format-rejected-not-mixed",()=>
        {
            var root=Path.Combine(output,"old-format");var folder=Path.Combine(root,"SPIKE001B","phase0-r2","activations");Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"0001.json"),Json.Serialize(new{Schema=1,Revision="phase0-r1",Attempt=Guid.NewGuid().ToString("N")}));
            var rejected=false;try{new PrivateStore(root).Active();}catch(InvalidDataException e){rejected=e.Message=="active-schema-invalid";}Check(rejected);
        });
        Test("full-positive-A-and-fresh-B-with-real-diagnostic-adapter",()=>
        {
            foreach(var b in new[]{false,true})
            {
                using var c=new Case(output,"full-positive-"+(b?"B":"A"));var trajectory=new Trajectory(binding,b?states:null,b?actions:null);Check(trajectory.Capture(states[0]).Length==0);
                for(var i=0;i<2;i++)
                {
                    var action=trajectory.Planned(1);if(b)trajectory.Dispatch(action,1);trajectory.Requested(action,1,b);c.StartPending(action);
                    c.Observe(ActionSite.BeforeExecuted,()=>c.Adapter.Executed());
                    if(i==0)c.Observe(ActionSite.CardEffect,()=>c.Adapter.CardEffect(TaskStatus.RanToCompletion));else c.Observe(ActionSite.EndReady,()=>c.Adapter.EndReady());
                    c.Observe(ActionSite.AfterFinished,()=>c.Adapter.Finished(i==0?TaskStatus.RanToCompletion:null,false));
                    trajectory.Succeeded(action,true,true,false,false,1);Check(c.Flow.ActionSucceeded(b));c.Capture();Check(trajectory.Capture(states[i+1]).Length==0);
                }
                Check(trajectory.Done && trajectory.Accepted.Length==2 && c.Flow.Complete(()=>{}));var ev=c.Events();
                Check(ev.Count(e=>e.Name=="action-observed")==10 && ev.Last().Action is{Sequence:2,Type:Operation.EndTurn,Before:Step.PostCard,Round:1,PlayerTurn:1,EndReadySeen:true,Finished:true,EffectTaskStatus:null,ProductionExceptionObserved:false});
            }
        });
        return count;
    }
    public static async Task Dummy()
    {
        var h=new Harmony("SPIKE001B.r2.dummy.action-observation");h.Patch(AccessTools.Method(typeof(Target),nameof(Target.Run)),prefix:new HarmonyMethod(typeof(TargetPatch),nameof(TargetPatch.Prefix)),postfix:new HarmonyMethod(typeof(TargetPatch),nameof(TargetPatch.Postfix)));
        try
        {
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();var fault=new IOException("ORIGINAL_DUMMY_FAULT");
            foreach(var task in new[]{Task.CompletedTask,Task.FromException(fault),Task.FromCanceled(cancelled.Token)})
            {
                var flow=new Flow(new Clock(),_=>{});TargetPatch.Adapter=new(flow,reason=>flow.Fail(reason));Target.Value=task;var returned=Target.Run(53);
                Check(ReferenceEquals(task,returned) && Target.Argument==53 && flow.ActionDiagnostics is{Rejection:ActionReject.CardEffectOrigin,Site:ActionSite.CardEffect});
                try{await returned;}catch(IOException e){Check(ReferenceEquals(e,fault));}catch(OperationCanceledException e){Check(e.CancellationToken==cancelled.Token);}
                Check(task.Status==returned.Status);
            }
        }
        finally{h.UnpatchAll(h.Id);}Check(!h.GetPatchedMethods().Any());Console.WriteLine("PASS R2 ACTUAL HARMONY DUMMY diagnostics reject observers preserve exact Task/args/success/fault/cancel");
    }
    private static void Check(bool value){if(!value)throw new InvalidOperationException("action diagnostics regression assertion");}
    private sealed class Clock:IClock {public long Now;public long Milliseconds=>Now;}
    private sealed record Record(int Schema,string Revision,string Source,Event Event);
    private sealed class Case:IDisposable
    {
        public readonly Clock Clock=new();public readonly Flow Flow;public readonly ActionObservation Adapter;
        private readonly PrivateStore _store;private readonly string _id=Guid.NewGuid().ToString("N");private readonly List<string> _files=[];public int Attempts;
        public Case(string output,string name,Func<Event,bool>? reject=null)
        {
            _store=new(Path.Combine(output,name));Flow=new(Clock,e=>
            {
                Attempts++;if(reject?.Invoke(e)==true)throw new IOException("SYNTHETIC WRITER FAILURE");
                var filename=Attempts.ToString("D3")+".json";_store.WriteBytes(_id,filename,Encoding.UTF8.GetBytes(Json.Serialize(new Record(PrivateStore.Schema,PrivateStore.Revision,"SYNTHETIC_CALLBACK_DIAGNOSTICS",e))),Flow.MaxRecordBytes);_files.Add(filename);
            });Adapter=new(Flow,reason=>Flow.Fail(reason));Check(Flow.FirstFrame(false) && Flow.Arm() && Flow.SaveStarted() && Flow.BeginSeal() && Flow.Sealed());Capture();
        }
        public void Capture(){for(var i=0;i<30;i++){Clock.Now++;Check(Flow.Tick(i,Flow.FieldNames.Select(n=>new Field(n,FieldStatus.True)).ToArray(),"synthetic-local-refs")== (i==29));}Check(Flow.Captured());}
        public void Start(Operation type=Operation.CardPlay)=>StartPending(new(type==Operation.CardPlay?1:2,type,type==Operation.CardPlay?Step.Initial:Step.PostCard,1,1,null,new string('A',64)));
        public void StartPending(AcceptedAction a)=>Observe(ActionSite.Queue,()=>{Check(Adapter.Binding(a));Check(Flow.ActionStarted());Check(Adapter.Requested(a));});
        public void Observe(ActionSite site,Action body)=>Adapter.Guard(site,body);
        public void Timeout(){Clock.Now+=20000;Check(!Flow.Poll() && Flow.Stage==Stage.Terminal);}
        public Event[] Events()=>_files.Select(n=>_store.Read<Record>(_id,n).Event).ToArray();
        public Event Terminal()=>Events().Single(e=>e.Name=="terminal");
        public int[] Sizes()=>_files.Select(n=>Encoding.UTF8.GetByteCount(Json.Serialize(_store.Read<Record>(_id,n).Event))).ToArray();
        public void Dispose(){}
    }
    private static class Target {public static Task Value=Task.CompletedTask;public static int Argument;[MethodImpl(MethodImplOptions.NoInlining)]public static Task Run(int value){Argument=value;return Value;}}
    private static class TargetPatch
    {
        public static ActionObservation Adapter=null!;
        public static void Prefix()=>Adapter.Guard(ActionSite.CardEffect,()=>ActionObservation.Require(false,"card-effect-origin"));
        public static void Postfix(Task __result)=>Adapter.Guard(ActionSite.AfterFinished,()=>throw new IOException("SHOULD_NOT_ESCAPE"));
    }
}
