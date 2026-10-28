using System.Text;
using Spike001B;
using Spike001B.Core;

public static class DiagnosticsRegression
{
    public static int Run(string output)
    {
        Directory.CreateDirectory(output);int count=0;
        void Test(string name,Action body){body();count++;Console.WriteLine("PASS inherited-diagnostics-"+name);}
        Test("no-tick-versus-never-ready",()=>
        {
            using var none=new Case(output,"no-tick");none.Start();
            for(var t=1;t<=20000;t++){none.Clock.Now=t;none.Flow.Poll();}
            using var blocked=new Case(output,"never-ready-false");blocked.Start();
            for(var i=1;i<2000;i++){blocked.Clock.Now=i*10;blocked.Flow.Tick(i,Bad("handPresent",FieldStatus.False),"refs");blocked.Flow.Poll();}
            blocked.Clock.Now=20000;blocked.Flow.Poll();
            var no=none.Terminal();var yes=blocked.Terminal();
            Require(no.Diagnostics!.Callbacks==0 && no.Diagnostics.SampleStatus==SampleStatus.NoSample && no.Diagnostics.LastSample is null && no.Frame is null);
            Require(yes.Diagnostics!.Callbacks==1999 && yes.Diagnostics.SampleStatus==SampleStatus.Sampled && yes.Frame==1999);
            Require(yes.Diagnostics.LastSample!.Monotonic==19990 && yes.Diagnostics.LastSample.BlockingField=="handPresent" &&
                yes.Diagnostics.LastSample.Fields.Single(x=>x.Name=="handPresent").Status==FieldStatus.False);
            Require(no.Reason==yes.Reason && no.Reason=="deadline-CaptureA" && Json.Serialize(no)!=Json.Serialize(yes));
            File.WriteAllText(Path.Combine(output,"no-tick-versus-never-ready.json"),Json.Serialize(new {Source="SYNTHETIC_PERSISTED_EVENT_READBACK",NoTick=no,NeverReady=yes}));
        });
        Test("missing-field-blocker-retained",()=>
        {
            using var c=new Case(output,"missing");c.Start();
            for(var i=1;i<=20;i++){c.Clock.Now=i*100;c.Flow.Tick(i,Bad("phaseIsPlay",FieldStatus.Missing),"refs");c.Flow.Poll();}
            c.Clock.Now=20000;c.Flow.Poll();var d=c.Terminal().Diagnostics!;
            Require(d.Callbacks==20 && d.LastSample!.BlockingField=="phaseIsPlay" && d.LastSample.Fields.Single(x=>x.Name=="phaseIsPlay").Status==FieldStatus.Missing);
        });
        Test("exception-field-not-generic-reason-only",()=>
        {
            using var c=new Case(output,"exception");c.Start();c.Clock.Now=5;
            var fields=Bad("managerPresent",FieldStatus.False);fields=fields.Select(x=>x.Name=="phaseIsPlay"?x with{Status=FieldStatus.Exception}:x).ToArray();
            Require(!c.Flow.Tick(41,fields,"refs"));var e=c.Terminal();var d=e.Diagnostics!;
            Require(e.Reason=="readiness-getter-exception" && d.Callbacks==1 && e.Frame==41 && d.LastSample!.Monotonic==5 &&
                d.LastSample.BlockingField=="managerPresent" && d.LastSample.ExceptionField=="phaseIsPlay" && d.LastSample.Fields.Length==20);
        });
        Test("actual-field-reader-redacts-getter-failure",()=>
        {
            var reader=new FieldReader();
            foreach(var name in Flow.FieldNames)reader.Bool(name,()=>name=="targetIdentityStable"?throw new IOException("SENSITIVE synthetic native path / model value / stack"):true);
            using var c=new Case(output,"field-reader");c.Start();c.Flow.Tick(9,reader.Fields,"refs");
            var raw=Json.Serialize(c.Terminal());Require(c.Terminal().Diagnostics!.LastSample!.ExceptionField=="targetIdentityStable" && !raw.Contains("SENSITIVE") && !raw.Contains("IOException"));
        });
        Test("object-getter-failure-named-and-dependent-missing",()=>
        {
            var reader=new FieldReader();var value=reader.Object<object>("statePresent",()=>throw new InvalidOperationException("private"));
            reader.Bool("phaseIsPlay",()=>true,value is not null);
            Require(value is null && reader.Fields[0].Status==FieldStatus.Exception && reader.Fields[1].Status==FieldStatus.Missing && !Json.Serialize(reader.Fields).Contains("private"));
        });
        Test("deadline-preserves-existing-quiet-count",()=>
        {
            using var c=new Case(output,"quiet-before-deadline");c.Start();
            for(var i=1;i<=17;i++){c.Clock.Now=i;c.Flow.Tick(i,Good(),"refs");}
            c.Clock.Now=20000;c.Flow.Poll();var e=c.Terminal();
            Require(e.Frame==17 && e.Consecutive==17 && e.Diagnostics!.Callbacks==17 && e.Diagnostics.Consecutive==17 &&
                e.Diagnostics.LastSample!.QuietBefore==16 && e.Diagnostics.LastSample.QuietAfter==17 && e.Diagnostics.LastSample.Fields.All(x=>x.Status==FieldStatus.True));
        });
        Test("false-reset-keeps-last-sample-and-prior-count",()=>
        {
            using var c=new Case(output,"reset-sample");c.Start();for(var i=1;i<=12;i++)c.Flow.Tick(i,Good(),"refs");
            c.Flow.Tick(13,Bad("currentActionIsNull",FieldStatus.False),"refs");c.Flow.Fail("synthetic-stop");var e=c.Terminal();
            Require(e.Consecutive==0 && e.Frame==13 && e.Diagnostics!.Callbacks==13 && e.Diagnostics.LastSample!.QuietBefore==12 &&
                e.Diagnostics.LastSample.QuietAfter==0 && e.Diagnostics.LastSample.BlockingField=="currentActionIsNull");
        });
        Test("deadline-tick-keeps-last-before-deadline-sample",()=>
        {
            using var c=new Case(output,"deadline-tick");c.Start();c.Clock.Now=19999;c.Flow.Tick(7,Good(),"refs");
            c.Clock.Now=20000;Require(!c.Flow.Tick(8,Bad("handPresent",FieldStatus.Exception),"refs"));var e=c.Terminal();
            Require(e.Reason=="deadline-CaptureA" && e.Frame==7 && e.Diagnostics!.Callbacks==1 &&
                e.Diagnostics.LastSample!.Monotonic==19999 && e.Diagnostics.LastSample.ExceptionField is null);
        });
        Test("heartbeat-rate-count-and-byte-caps-A-and-B",()=>
        {
            foreach(var b in new[]{false,true})
            {
                using var c=new Case(output,b?"heartbeat-B":"heartbeat-A");c.Start(b);
                for(var t=1;t<=20000;t++){c.Clock.Now=t;if(t%10==0 && t<20000)c.Flow.Tick(t/10,Bad("executorIsNotRunning",FieldStatus.False),"refs");c.Flow.Poll();}
                var beats=c.Events().Where(x=>x.Name=="capture-heartbeat").ToArray();
                Require(beats.Length==19 && beats.Length<=Flow.MaxHeartbeats && beats.Zip(beats.Skip(1)).All(x=>x.Second.Monotonic-x.First.Monotonic>=1000));
                var files=c.RecordBytes();Require(files.All(x=>x<=Flow.MaxRecordBytes));
                Require(beats.Sum(x=>Encoding.UTF8.GetByteCount(Json.Serialize(x)))<=Flow.MaxHeartbeatBytes &&
                    c.Terminal().Diagnostics!.Phase==(b?Stage.CaptureB:Stage.CaptureA));
            }
        });
        Test("heartbeat-no-catchup-burst",()=>
        {
            using var c=new Case(output,"no-burst");c.Start();c.Clock.Now=9000;for(var i=0;i<100;i++)c.Flow.Poll();
            Require(c.Events().Count(x=>x.Name=="capture-heartbeat")==1);
            c.Clock.Now=9999;c.Flow.Poll();Require(c.Events().Count(x=>x.Name=="capture-heartbeat")==1);
            c.Clock.Now=10000;c.Flow.Poll();Require(c.Events().Count(x=>x.Name=="capture-heartbeat")==2);
        });
        Test("tick-performs-no-per-frame-write",()=>
        {
            using var c=new Case(output,"no-frame-IO");c.Start();var before=c.Writes;
            for(var i=1;i<=1000;i++){c.Clock.Now=i;c.Flow.Tick(i,Bad("handPresent",FieldStatus.False),"refs");}
            Require(c.Writes==before);c.Flow.Poll();Require(c.Writes==before+1);
        });
        Test("ready-and-await-save-have-no-heartbeat",()=>
        {
            using var c=new Case(output,"ready-wait");Require(c.Flow.FirstFrame(false));var before=c.Writes;
            c.Clock.Now=999999;c.Flow.Poll();Require(c.Writes==before);Require(c.Flow.Arm());before=c.Writes;
            c.Clock.Now+=999999;c.Flow.Poll();Require(c.Writes==before);
            using var b=new Case(output,"ready-B-wait");Require(b.Flow.FirstFrame(true));before=b.Writes;b.Clock.Now=999999;b.Flow.Poll();Require(b.Writes==before);
        });
        Test("normal-30-frame-capture-keeps-window",()=>
        {
            using var c=new Case(output,"positive-window");c.Start();
            for(var i=1;i<=30;i++){c.Clock.Now=i;Require(c.Flow.Tick(i,Good(),"refs")==(i==30));}
            var window=c.Flow.Window;Require(window.Count==30 && window.LastFrame-window.FirstFrame==29 && c.Flow.Captured());
            var e=c.Events().Single(x=>x.Name=="capture-30-frames");Require(e.Diagnostics!.Callbacks==30 && e.Consecutive==30 && e.Diagnostics.LastSample!.QuietAfter==30);
        });
        Test("terminal-freeze-and-no-further-writes",()=>
        {
            using var c=new Case(output,"terminal-freeze");c.Start();c.Flow.Tick(1,Bad("handPresent",FieldStatus.False),"refs");Require(c.Flow.Fail("first"));
            var expected=Json.Serialize(c.Flow.Diagnostics);var writes=c.Writes;
            for(var i=0;i<100;i++){c.Clock.Now+=1000;Require(!c.Flow.Poll() && !c.Flow.Tick(i,Good(),"new") && !c.Flow.Captured() && !c.Flow.Sealed() && !c.Flow.Fail("second"));}
            Require(c.Writes==writes && expected==Json.Serialize(c.Flow.Diagnostics) && c.Events().Count(x=>x.Name=="terminal")==1 && c.Terminal().Reason=="first");
        });
        Test("terminal-cannot-expose-ready-window-after-30",()=>
        {
            using var c=new Case(output,"terminal-window");c.Start();for(var i=1;i<=30;i++)c.Flow.Tick(i,Good(),"refs");c.Flow.Fail("first");
            var rejected=false;try{_=c.Flow.Window;}catch(InvalidDataException){rejected=true;}Require(rejected && c.Terminal().Consecutive==30);
        });
        Test("samples-detached-from-caller-and-writer",()=>
        {
            using var c=new Case(output,"detached");c.Start();var fields=Bad("handPresent",FieldStatus.False);c.Flow.Tick(1,fields,"refs");
            fields[4]=fields[4] with{Status=FieldStatus.True};var publicCopy=c.Flow.Diagnostics!;publicCopy.LastSample!.Fields[4]=publicCopy.LastSample.Fields[4] with{Status=FieldStatus.True};
            c.Flow.Fail("first");Require(c.Terminal().Diagnostics!.LastSample!.Fields[4].Status==FieldStatus.False);
        });
        Test("malformed-names-not-exported",()=>
        {
            using var c=new Case(output,"name-whitelist");c.Start();var fields=Good();fields[0]=new(new string('X',10000),FieldStatus.Exception);c.Flow.Tick(2,fields,"SENSITIVE reference token");
            var e=c.Terminal();var raw=Json.Serialize(e);Require(e.Reason=="readiness-field-set" && e.Diagnostics!.LastSample!.Fields.Select(x=>x.Name).SequenceEqual(Flow.FieldNames) &&
                !raw.Contains(new string('X',50)) && !raw.Contains("SENSITIVE") && Encoding.UTF8.GetByteCount(raw)<=Flow.MaxEventBytes);
        });
        Test("heartbeat-write-failure-fails-closed",()=>
        {
            using var c=new Case(output,"heartbeat-failure",e=>e.Name=="capture-heartbeat");c.Start();c.Flow.Tick(1,Bad("handPresent",FieldStatus.False),"refs");
            c.Clock.Now=1000;Require(!c.Flow.Poll());var e=c.Terminal();Require(e.Reason=="evidence-write-failure" && e.Diagnostics!.Callbacks==1 && e.Diagnostics.LastSample!.BlockingField=="handPresent");
            var attempts=c.WriteAttempts;Require(!c.Flow.Captured() && !c.Flow.Poll() && c.WriteAttempts==attempts);
        });
        Test("terminal-writer-failure-only-once-no-recursion",()=>
        {
            using var c=new Case(output,"terminal-write-failure",e=>e.Name=="terminal");c.Start();c.Flow.Tick(1,Good(),"refs");var attempts=c.WriteAttempts;
            Require(c.Flow.Fail("first") && c.WriteAttempts==attempts+1 && c.Flow.Stage==Stage.Terminal && c.Flow.Diagnostics!.Callbacks==1);
            c.Flow.Fail("second");c.Flow.Poll();c.Flow.Tick(2,Good(),"refs");Require(c.WriteAttempts==attempts+1 && c.Flow.TerminalReason=="first");
        });
        Test("startup-and-final-commit-writer-failure",()=>
        {
            using var c=new Case(output,"startup-failure",e=>true);Require(!c.Flow.FirstFrame(false) && c.Flow.Stage==Stage.Terminal && c.WriteAttempts==2);
            using var b=new Case(output,"commit-failure",e=>e.Name=="point-captured");b.Start(true);for(var i=1;i<=30;i++)b.Flow.Tick(i,Good(),"refs");
            Require(!b.Flow.Captured());var e=b.Terminal();Require(e.Consecutive==30 && e.Diagnostics!.Callbacks==30 && e.Reason=="evidence-write-failure");
        });
        Test("terminal-tick-and-writer-race",()=>
        {
            using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
            using var c=new Case(output,"writer-race",e=>
            {if(e.Name!="capture-heartbeat")return false;entered.Set();Require(release.Wait(TimeSpan.FromSeconds(2)));return true;});c.Start();c.Clock.Now=1000;
            var writer=Task.Run(()=>c.Flow.Poll());Require(entered.Wait(TimeSpan.FromSeconds(2)));
            var tick=Task.Run(()=>c.Flow.Tick(1,Good(),"refs"));var terminal=Task.Run(()=>c.Flow.Fail("external-terminal"));release.Set();
            Task.WaitAll(writer,tick,terminal);var e=c.Terminal();Require(e.Reason=="evidence-write-failure" && c.Events().Count(x=>x.Name=="terminal")==1 && e.Diagnostics!.Callbacks==0 && !tick.Result);
        });
        return count;
    }
    private static Field[] Good()=>Flow.FieldNames.Select(x=>new Field(x,FieldStatus.True)).ToArray();
    private static Field[] Bad(string name,FieldStatus status)=>Good().Select(x=>x.Name==name?x with{Status=status}:x).ToArray();
    private static void Require(bool value){if(!value)throw new InvalidOperationException("r2 diagnostic regression assertion");}
    private sealed class Clock:IClock {public long Now;public long Milliseconds=>Now;}
    private sealed record Record(int Schema,string Revision,string Source,Event Event);
    private sealed class Case:IDisposable
    {
        public readonly Clock Clock=new();public readonly Flow Flow;
        private readonly PrivateStore _store;private readonly string _id=Guid.NewGuid().ToString("N");private readonly List<string> _files=[];
        public int Writes=>_files.Count;public int WriteAttempts;
        public Case(string root,string name,Func<Event,bool>? reject=null)
        {
            _store=new(Path.Combine(root,name));Flow=new(Clock,e=>
            {
                WriteAttempts++;if(reject?.Invoke(e)==true)throw new IOException("SYNTHETIC writer failure");
                var filename=WriteAttempts.ToString("D3")+".json";var bytes=Encoding.UTF8.GetBytes(Json.Serialize(new Record(PrivateStore.Schema,PrivateStore.Revision,"SYNTHETIC",e)));
                _store.WriteBytes(_id,filename,bytes,Flow.MaxRecordBytes);_files.Add(filename);
            });
            File.WriteAllText(Path.Combine(root,name+"-attempt.json"),Json.Serialize(new {Source="SYNTHETIC",Attempt=_id}));
        }
        public void Start(bool b=false)
        {Require(Flow.FirstFrame(b));if(b)Require(Flow.ContinueStarted());else Require(Flow.Arm() && Flow.SaveStarted() && Flow.BeginSeal() && Flow.Sealed());}
        public Event[] Events()=>_files.Select(x=>_store.Read<Record>(_id,x).Event).ToArray();
        public Event Terminal()=>Events().Single(x=>x.Name=="terminal");
        public int[] RecordBytes()=>_files.Select(x=>_store.ReadBytes(_id,x).Length).ToArray();
        public void Dispose(){}
    }
}
