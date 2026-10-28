using System.Text.Json;
using Perf001Candidate.Core;

if(args.Length!=1)throw new ArgumentException("new synthetic validation root required");
var output=Path.GetFullPath(args[0]);
var permitted=Path.GetFullPath(Path.Combine(Environment.CurrentDirectory,"evidence/perf-001/candidate-r1/validation"));
Identity.Require(Path.GetDirectoryName(output)==permitted,"synthetic-validation-package-only");
if(Directory.Exists(output)||File.Exists(output))throw new InvalidDataException("new validation root only");
OwnedStore.NoLinks(output);Directory.CreateDirectory(output);
var passed=new List<string>();
void Need(bool value,string why="assertion"){if(!value)throw new InvalidDataException(why);}
void Test(string name,Action action){action();passed.Add(name);}
void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new InvalidDataException("expected rejection");}
var h=new Harness(output);
Test("game-sourcegen-only-options-own-envelope-fallback-without-global-mutation",()=>
{
    var source=SyntheticGeneratedJson.GameLikeContext.Default.Options;
    var resolver=source.TypeInfoResolver;var known=new SyntheticGeneratedJson.KnownDto(17);
    var unknown=new{Frozen=known,Raw=new[]{1,2},Nested=new{Hp=80}};
    var rejected=false;try{JsonSerializer.Serialize(unknown,source);}catch(NotSupportedException){rejected=true;}
    Need(rejected,"generated-only source unexpectedly accepted anonymous envelope");
    var own=LocalJson.ValueOptions(source);var json=JsonSerializer.Serialize(unknown,own);
    using var parsed=JsonDocument.Parse(json);Need(parsed.RootElement.GetProperty("frozen").GetProperty("value").GetInt32()==17);
    Need(parsed.RootElement.GetProperty("nested").GetProperty("hp").GetInt32()==80&&ReferenceEquals(resolver,source.TypeInfoResolver));
    Need(JsonSerializer.Serialize(known,source)=="{\"value\":17}");
});
Fixture? baseline=null;
Test("typed-C1-baseline-through-normal-Custom-map-Defend-EndTurn-drain",()=>
{
    baseline=h.Create("c1-positive",Mode.C1);baseline.Complete();
    Need(baseline.Flow.Stage==Stage.Complete&&baseline.View.CaptureCalls==0&&baseline.Flow.RecorderSamples.Count==0);
    Need(baseline.Flow.CommonEvents.SequenceEqual(Enum.GetValues<EventKind>()));
    Need(!File.Exists(Path.Combine(baseline.StorePath,"events.ndjson")));
});
h.Expected=baseline!.Flow.Scenes.Select(x=>x.Detached()).ToArray();
foreach(var mode in new[]{Mode.C0,Mode.C2})Test("same-typed-path-"+mode,()=>
{
    var f=h.Create(mode.ToString().ToLowerInvariant()+"-positive",mode);f.Complete();
    Need(f.Flow.Stage==Stage.Complete);Need(f.Flow.Scenes.Count==4&&f.Flow.CommonEvents.SequenceEqual(baseline.Flow.CommonEvents));
    Need(f.Flow.Boundaries.Select(x=>x.Name).SequenceEqual(baseline.Flow.Boundaries.Select(x=>x.Name)),"common boundary order diverged");
    Need(f.Flow.FrameCount==baseline.Flow.FrameCount);
    Need(f.View.CaptureCalls==(mode==Mode.C2?5:0));
    if(mode==Mode.C2){Need(f.Flow.WriterResult is{Complete:true,ThreadStopped:true,Accepted:5,Written:5,FlushCalls:1,Overflows:0});
        Need(f.Flow.RecorderSamples.Count==5);Need(File.ReadAllLines(Path.Combine(f.StorePath,"events.ndjson")).Length==5);}
});
Test("configuration-and-published-gates-reject-before-store",()=>
{
    var c=h.Config("not-created",Mode.C1);var p=Harness.Published();
    foreach(var bad in new[]{c with{Schema=2},c with{Mode=(Mode)9},c with{RuntimeAuthorized=false},c with{UserConfirmedCloudOff=false},
        c with{Mode=Mode.C0},c with{Seed="old/seed"}})Reject(()=>new Session(bad,p,output,new FakeClock(),new FakeView()));
    foreach(var bad in new[]{p with{Profile=2},p with{RunningModded=false},p with{ModdedTree=false},p with{ProfileScopeBinding=new string('F',64)},
        p with{LoadedIds=["old-probe"]},p with{CandidateSHA=new string('F',64)}})Reject(()=>new Session(c,bad,output,new FakeClock(),new FakeView()));
    Need(!Directory.Exists(Path.Combine(output,"not-created")));
});
Test("no-first-frame-deadline-stops-owned-writer",()=>
{var f=h.Create("no-first-frame",Mode.C2);f.Clock.Value=5001;f.Flow.PollDeadlines();f.AssertFailed();Need(f.Flow.WriterResult?.ThreadStopped==true);});
Test("manual-ready-wait-does-not-use-action-deadline",()=>
{var f=h.Create("manual-wait",Mode.C1);f.ToMapReady();f.Clock.Value+=600000;f.Flow.PollDeadlines();Need(f.Flow.Stage==Stage.ReadyMapNode);f.Flow.OwnerExit();f.AssertFailed();});
Test("Continue-and-wrong-fresh-origin-stop",()=>
{
    var f=h.Create("continue",Mode.C2);f.Step();f.Flow.ContinueObserved();f.AssertFailed();
    var g=h.Create("wrong-origin",Mode.C1);g.Step();g.Flow.NewRun("Standard",Harness.Seed,true,1,0,0);g.AssertFailed();
});
Test("early-map-input-during-warmup-stop",()=>
{var f=h.Create("early-map",Mode.C2);f.Start();f.Flow.MapInputRequested(true);f.AssertFailed();});
Test("automatic-initial-starting-entry-is-not-measured-monster",()=>
{
    var f=h.Create("initial-starting-entry",Mode.C1);f.Step();f.Flow.NewRun("Custom",Harness.Seed,true,1,0,0);
    f.Flow.InitialStartingMapObserved();Need(f.Flow.Stage==Stage.AwaitMap&&f.Flow.CommonEvents.Count==0);
    f.Light=f.Light with{MapVisible=true};f.Step();Need(f.Flow.Stage==Stage.Warmup);
    f.Step(10000);f.Step(30000);Need(f.Flow.Stage==Stage.ReadyMapNode);f.Flow.OwnerExit();f.AssertFailed();
});
Test("common-qualification-detects-dynamic-enemy-content-difference",()=>
{var f=h.Create("changed-content",Mode.C2);f.ToMapReady();f.View.ChangeEnemy=true;f.LoadCombat();f.AssertFailed();});
Test("load-does-not-complete-on-Task-object-return",()=>
{
    var f=h.Create("map-task-wait",Mode.C1);f.ToMapReady();f.BeginMap();f.Step();Need(f.Flow.Stage==Stage.Loading);
    f.Clock.Value+=20001;f.Flow.PollDeadlines();f.AssertFailed();
});
Test("duplicate-map-return-is-boundary-race-failure",()=>
{var f=h.Create("map-race",Mode.C2);f.ToMapReady();f.BeginMap();f.Flow.MapReturned(Task.CompletedTask);f.AssertFailed();});
Test("map-vote-move-and-original-travel-task-required",()=>
{
    var f=h.Create("map-travel-fault",Mode.C2);f.ToMapReady();f.BeginMap();f.Map.SetResult();f.InitialTurn.SetResult();
    f.MapTravel.SetException(new ApplicationException("original travel fault"));f.Light=new(true,false,false,true,1,true);f.Step();f.AssertFailed();
    try{f.MapTravel.Task.GetAwaiter().GetResult();}catch(ApplicationException ex){Need(ex.Message=="original travel fault");}
    Need(f.Flow.Boundaries.Any(b=>b.Name=="map-vote-input-request")&&f.Flow.Boundaries.Any(b=>b.Name=="normal-map-host-move-queued"));
});
Test("same-tick-internal-boundaries-allowed-frame-duplicates-rejected",()=>
{
    Need(baseline.Flow.Boundaries.Zip(baseline.Flow.Boundaries.Skip(1)).Any(x=>x.First.Tick==x.Second.Tick&&x.First.Sequence<x.Second.Sequence));
    var f=h.Create("frame-duplicate",Mode.C2);f.Step();f.Flow.Frame(++f.FrameNumber,new(true,false,false,false,0,false));f.AssertFailed();
});
Test("missed-engine-frame-and-reversed-clock-reject",()=>
{
    var f=h.Create("missed-frame",Mode.C1);f.Step();f.Clock.Value++;f.FrameNumber+=2;f.Flow.Frame(f.FrameNumber,f.Light);f.AssertFailed();
    var g=h.Create("reversed-frame",Mode.C1);g.Step();g.Clock.Value--;g.Flow.Frame(++g.FrameNumber,g.Light);g.AssertFailed();
});
Test("interrupted-idle-retained-and-rearmed-without-new-game",()=>
{
    var f=h.Create("focus",Mode.C2);f.Start();f.Step(10000);Need(f.Flow.Stage==Stage.MapIdle);var before=f.Flow.FrameCount;
    f.Light=f.Light with{Focused=false};f.Step();Need(f.Flow.Stage==Stage.AwaitWarmupFocus&&f.Flow.FrameCount==before+1);
    f.Light=f.Light with{Focused=true};f.Step();f.Step(Session.WarmupSeconds*1000);f.Step(Session.IdleSeconds*1000);
    Need(f.Flow.Stage==Stage.ReadyMapNode&&f.Flow.Boundaries.Any(x=>x.Name=="map-idle-invalid-focus-window-retained"));f.Flow.OwnerExit();f.AssertFailed();
});
Test("untimed-warmup-restarts-on-focus-and-manual-ready-can-wait",()=>
{
    var f=h.Create("warmup-focus",Mode.C1);f.Start();f.Light=f.Light with{Focused=false};f.Step(5000);
    Need(f.Flow.Stage==Stage.AwaitWarmupFocus);var before=f.Flow.FrameCount;f.Step(60000);Need(!f.Flow.Terminal&&f.Flow.FrameCount==before+1);
    f.Light=f.Light with{Focused=true};f.Step();Need(f.Flow.Stage==Stage.Warmup);f.Step(10000);Need(f.Flow.Stage==Stage.MapIdle);
    f.Step(30000);f.Light=f.Light with{Focused=false};f.Step();Need(f.Flow.Stage==Stage.ReadyMapNode);
    Need(f.Flow.Boundaries.Any(b=>b.Name=="untimed-warmup-interrupted-focus-or-pause"));f.Flow.OwnerExit();f.AssertFailed();
});
Test("frame-array-overflow-does-not-complete",()=>
{var f=h.Create("frame-full",Mode.C2,capacity:2);f.Start();f.Step();f.Step();f.AssertFailed();});
Test("unlimited-main-menu-and-ready-wait-do-not-fill-sampling-buffer",()=>
{
    var f=h.Create("menu-afk",Mode.C1,capacity:64);f.Step();
    for(var i=0;i<100000;i++)f.Step(1000);
    Need(f.Flow.Stage==Stage.AwaitFreshCustom&&f.Flow.FrameCount==0);
    f.Flow.NewRun("Custom",Harness.Seed,true,1,0,0);f.Flow.InitialStartingMapObserved();f.Light=f.Light with{MapVisible=true};f.Step();
    f.Step(Session.WarmupSeconds*1000);f.Step(Session.IdleSeconds*1000);Need(f.Flow.Stage==Stage.ReadyMapNode);
    f.Step();var before=f.Flow.FrameCount;
    for(var i=0;i<100000;i++)f.Step(1000);
    Need(f.Flow.Stage==Stage.ReadyMapNode&&f.Flow.FrameCount==before);f.Flow.OwnerExit();f.AssertFailed();
});
Test("original-effect-fault-cancel-and-never-complete",()=>
{
    foreach(var kind in new[]{"fault","cancel","pending"})
    {
        var f=h.Create("effect-"+kind,Mode.C2);f.ToCombat();f.BeginCard();
        if(kind=="fault")f.CardEffect.SetException(new ApplicationException("original effect"));
        if(kind=="cancel")f.CardEffect.SetCanceled();
        f.Step();if(kind=="pending"){Need(f.Flow.Stage==Stage.CardEffect);f.Clock.Value+=20001;f.Flow.PollDeadlines();}
        f.AssertFailed();Need(f.Flow.WriterResult?.ThreadStopped==true);
        if(kind=="fault")try{f.CardEffect.Task.GetAwaiter().GetResult();}catch(ApplicationException ex){Need(ex.Message=="original effect");}
    }
});
Test("next-turn-requires-new-original-turn-task-and-increment",()=>
{
    var f=h.Create("turn-task-wait",Mode.C1);f.ToCardDone();f.BeginEnd();f.EndAction.SetResult();f.Light=f.Light with{Turn=2};f.Step();Need(f.Flow.Stage==Stage.EndTurn);
    f.Clock.Value+=20001;f.Flow.PollDeadlines();f.AssertFailed();
});
Test("missing-candidate-event-never-commits",()=>
{var f=h.Create("missing-event",Mode.C2);f.ToCombat();f.BeginCard(candidate:false);f.CardEffect.SetResult();f.CardAction.SetResult();f.Step();f.AssertFailed();});
Test("all-writer-failure-paths-stop-and-no-success",()=>
{
    var open=h.Create("open-fail",Mode.C2,options:new(){FailOpen=true});open.Flow.PollDeadlines();open.AssertFailed();
    var write=h.Create("write-fail",Mode.C2,options:new(){FaultBeforeWrite=1});write.ToMapReady();write.BeginMap();
    SpinWait.SpinUntil(()=>write.Flow.WriterResult is not null||Directory.GetFiles(write.StorePath,"events.ndjson").Length>0,100);
    write.Flow.OwnerExit();write.AssertFailed();Need(write.Flow.WriterResult?.ThreadStopped==true);
    using var gate=new ManualResetEventSlim();var overflow=h.Create("queue-full",Mode.C2,options:new(){Gate=gate,CapacityRecords=1});
    overflow.ToCombat();overflow.BeginCard();overflow.AssertFailed();Need(overflow.Flow.WriterResult is{ThreadStopped:true,Complete:false});
});
Test("writer-undrained-budget-and-size-failures-are-sticky",()=>
{
    using var gate=new ManualResetEventSlim();var store=new OwnedStore(output,"writer-undrained");var writer=new RecordWriter(store,new(){Gate=gate});
    Need(writer.Enqueue(1,new byte[]{1}));var result=writer.Stop(1,true,TimeSpan.Zero);Need(!result.Complete&&result.ThreadStopped);
    var budgetStore=new OwnedStore(output,"writer-budget");budgetStore.Reserve((int)OwnedStore.Limit);var budgetWriter=new RecordWriter(budgetStore);
    Need(budgetWriter.Enqueue(1,new byte[]{1}));Need(budgetWriter.Stop(1,true).Complete==false);
    var sizeWriter=new RecordWriter(new OwnedStore(output,"writer-size"),new(){MaximumRecordBytes=1});Need(!sizeWriter.Enqueue(1,new byte[]{1}));Need(!sizeWriter.Stop(0,true).Complete);
});
Test("payload-buffer-independent-and-terminal-no-append",()=>
{
    var f=h.Create("freeze-terminal",Mode.C2);f.Complete();var eventsPath=Path.Combine(f.StorePath,"events.ndjson");var before=File.ReadAllBytes(eventsPath);
    f.View.PayloadHand[0]="MUTATED_AFTER_FREEZE";f.Flow.Candidate(EventKind.MapCommit);f.Flow.OwnerExit();f.Step();
    Need(before.SequenceEqual(File.ReadAllBytes(eventsPath)));Need(f.Flow.Stage==Stage.Complete&&f.View.CaptureCalls==5);
});
Test("observer-guard-never-propagates-including-failure-handler",()=>
{ObserverGuard.Run(()=>throw new ApplicationException("observer"),_=>throw new ApplicationException("failure handler"));});
File.WriteAllBytes(Path.Combine(output,"result.json"),Identity.Bytes(new{Result="PASS_TYPED_OFFLINE_CANDIDATE_CONTRACTS",Count=passed.Count,Tests=passed,
    Modes=new[]{"C1 baseline","C0 matched","C2 matched"},RealEngineExecuted=false,ProductionConstructorsExecuted=false,RealTimeWait40Seconds=false,TimingBenchmarkRuns=0,GameRuntimeAttempts=0}));
Console.WriteLine("PASS "+passed.Count+" candidate typed path/negative contracts; no game/benchmark");

sealed class FakeClock:IClock {public long Value;public long Now=>Value;public long Frequency=>1000;}
sealed class FakeView:ILiveView
{
    public int CaptureCalls;public bool ChangeEnemy;public string[] PayloadHand=["CARD.DEFEND_IRONCLAD","CARD.STRIKE_IRONCLAD"];
    public Payload Capture(EventKind kind){CaptureCalls++;return new(1,"player:0",kind.ToString(),0,[],80,0,3,PayloadHand,5,0,0);}
    public SceneFacts Qualify(string point)=>new(point,"Custom",Harness.Seed,"CHARACTER.IRONCLAD",0,0,point=="map"?0:1,0,point=="map"?"Event":"Monster",
        point=="map"?"":"SYNTHETIC_MONSTER","SYNTHETIC_NEOW",[],["BURNING_BLOOD","SYNTHETIC_NEOW"],Enumerable.Repeat("synthetic-card-value",10).ToArray(),
        point=="map"?[]:["synthetic-hand"],[],[],[],"synthetic-player-values-"+point,point=="map"?[]:[ChangeEnemy?"changed-hp-intent":"synthetic-enemy-hp-intent-powers"]);
}
sealed class Harness(string root)
{
    public const string Seed="NEWPERF001SEED";public SceneFacts[] Expected=[];
    public static PublishedIdentity Published()=>new(Identity.GameSHA,new string('A',64),new string('B',64),"2.4.2.0",true,true,1,new string('C',64),["PERF001"],["0.1.0"]);
    public Configuration Config(string id,Mode mode)=>new(1,Identity.Revision,mode,id,Seed,true,true,mode==Mode.C1,new string('A',64),new string('B',64),new string('C',64),mode==Mode.C1?[]:Expected);
    public Fixture Create(string id,Mode mode,int capacity=65536,WriterTestOptions? options=null)=>new(root,Config(id,mode),capacity,options);
}
sealed class Fixture
{
    public FakeClock Clock=new();public FakeView View=new();public Session Flow;public long FrameNumber;public string StorePath;
    public LightState Light=new(true,false,false,false,0,false);
    public TaskCompletionSource Map=new(),MapTravel=new(),InitialTurn=new(),CardEffect=new(),CardAction=new(),EndAction=new(),NextTurn=new();
    public Fixture(string root,Configuration c,int capacity,WriterTestOptions? options){StorePath=Path.Combine(root,c.RunId);Flow=new(c,Harness.Published(),root,Clock,View,capacity,options);}
    public void Step(long delta=1){Clock.Value+=delta;Flow.Frame(++FrameNumber,Light);}
    public void Start(){Step();Flow.NewRun("Custom",Harness.Seed,true,1,0,0);Flow.InitialStartingMapObserved();Light=Light with{MapVisible=true};Step();}
    public void ToMapReady(){Start();Step(Session.WarmupSeconds*1000);Step(Session.IdleSeconds*1000);}
    public void BeginMap(){Flow.MapInputRequested(true);Flow.MapMoveQueued(true);Flow.MapRequested(true);Flow.MapReturned(Map.Task);Flow.Candidate(EventKind.MapCommit);Flow.MapTravelTaskReturned(MapTravel.Task);var generation=Flow.TurnStarted(true);Flow.TurnTaskReturned(generation,InitialTurn.Task);}
    public void LoadCombat(){BeginMap();Step();Map.SetResult();MapTravel.SetResult();InitialTurn.SetResult();Light=new(true,false,false,true,1,true);Step();}
    public void ToCombat(){ToMapReady();LoadCombat();}
    public void BeginCard(bool candidate=true){Flow.ActionRequested(true,1);Flow.CardEntered(true,false);if(candidate)Flow.Candidate(EventKind.CardAccepted);Flow.CardTaskReturned(CardEffect.Task);Flow.ActionTaskReturned(CardAction.Task);}
    public void ToCardDone(){ToCombat();BeginCard();Step();CardEffect.SetResult();CardAction.SetResult();Step();}
    public void BeginEnd(){Flow.ActionRequested(false,1);Flow.EndAccepted(false,true,true);Flow.Candidate(EventKind.EndTurnAccepted);Flow.ActionTaskReturned(EndAction.Task);}
    public void Complete(){ToCardDone();BeginEnd();var gen=Flow.TurnStarted(true);Flow.TurnTaskReturned(gen,NextTurn.Task);Step();EndAction.SetResult();NextTurn.SetResult();Light=Light with{Turn=2};Step();Step();}
    public void AssertFailed(){if(Flow.Stage!=Stage.Failed||File.Exists(Path.Combine(StorePath,"complete.json")))throw new InvalidDataException("expected failed without commit: "+Flow.Stage+" "+Flow.Failure);}
}
