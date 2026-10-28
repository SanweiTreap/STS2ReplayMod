using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Rngs;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using Spike001B;
using Spike001B.Core;

var mode=args.FirstOrDefault()??"contracts";
if(mode=="patch") { PatchSmoke();return; }
if(mode=="dummy") { await DummySmoke();return; }
if(mode=="dummy-action-r2") { await ActionObservationRegression.Dummy();return; }
var output=Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
var publicA=File.ReadAllText(args[2]);
Assert(Json.Hash(publicA)=="DF34448C39CCEBC0DAE1F1C76006656D11BF00766A0B3A1EDB2A15EA506B4717");
var initial=Projection.Thaw(publicA); var states=Successors(initial); var initialFrozen=states[0];
var seed=initial.RunRng.Seed;
var beforeBytes=NativeFixture(false,seed);var sealedBytes=NativeFixture(true,seed);
var target=new Target(seed,initial.Player.Character,0,1,2,0,3,1);
var pred=NativeCheckpoint.Parse(beforeBytes).Facts;var saved=NativeCheckpoint.Parse(sealedBytes).Facts;
var bound=Contract.Seal(Contract.Arm(target,1,pred),pred,saved);
var candidate=new string('C',64);var attempt=Guid.NewGuid().ToString("N");
var actions=Log(bound,states);
var arm=new ArmData(PrivateStore.Schema,PrivateStore.Revision,candidate,"A:synthetic",target,pred);
var seal=new SealData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,bound);
var points=Enumerable.Range(0,3).Select(i=>new PointData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,"A",(Step)i,Json.Hash(states[i]),Window())).ToArray();
var records=actions.Select(a=>new ActionData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,"A",a)).ToArray();
var complete=new CompleteData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,attempt,saved.Hash,states.Select(Json.Hash).ToArray(),points.Select(p=>Json.Hash(Json.Serialize(p))).ToArray(),records.Select(a=>Json.Hash(Json.Serialize(a))).ToArray());
var ready=new ReadyData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,attempt,Json.Hash(Json.Serialize(complete)));
int count=0; void Test(string name,Action body) {body();count++;Console.WriteLine("PASS "+name);}
void Valid(PointData[]? p=null,ActionData[]? a=null,CompleteData? c=null,string[]? ss=null,byte[]? source=null,string process="B:synthetic",ReadyData? r=null)=>
    AttemptValidation.BInput(arm,seal,p??points,a??records,c??complete,r??ready,attempt,candidate,process,ss??states,sealedBytes,source??sealedBytes);
Test("public-A5-plus-synthetic-full-positive-seal-three-points-two-dispatch",()=>
{
    Valid(); var b=new Trajectory(bound,states,actions); Assert(b.Capture(states[0]).Length==0);
    for(var i=0;i<2;i++) {var planned=b.Planned(1);b.Dispatch(planned,1);b.Requested(planned,1,true);b.Succeeded(planned,true,true,false,false,1);Assert(b.Capture(states[i+1]).Length==0);}
    Assert(b.Done && b.Accepted.Length==2 && b.States.Length==3); Reject(()=>b.Dispatch(actions[1],1));
    var store=new PrivateStore(Path.Combine(output,"full-chain"));store.Activate(attempt);
    store.Write(attempt,"arm.json",arm);store.Write(attempt,"seal.json",seal);
    store.WriteBytes(attempt,"checkpoint.private.json",sealedBytes,PrivateStore.NativeCap);
    for(var i=0;i<3;i++){store.WriteBytes(attempt,"A.point-"+i+".semantic.json",Encoding.UTF8.GetBytes(states[i]));store.Write(attempt,"A.point-"+i+".capture.json",points[i]);}
    foreach(var r in records)store.Write(attempt,"A.action-"+r.Action.Sequence+".json",r);
    Assert(!AttemptValidation.ReadyB(store,attempt));store.Write(attempt,"A.complete.json",complete);Assert(!AttemptValidation.ReadyB(store,attempt));store.Write(attempt,"A.ready-B.json",ready);Assert(AttemptValidation.ReadyB(store,attempt));
    var readPoints=Enumerable.Range(0,3).Select(i=>store.Read<PointData>(attempt,"A.point-"+i+".capture.json")).ToArray();
    var readActions=Enumerable.Range(1,2).Select(i=>store.Read<ActionData>(attempt,"A.action-"+i+".json")).ToArray();
    Valid(readPoints,readActions,store.Read<CompleteData>(attempt,"A.complete.json"));
    File.WriteAllText(Path.Combine(output,"positive-chain.json"),Json.Serialize(new {Source="A5_PUBLIC_INITIAL_AND_SYNTHETIC_SUCCESSORS_NOT_RUNTIME",Bound=bound,Actions=actions,Complete=complete,States=states.Select(Projection.Thaw).ToArray()}));
});
Test("fresh-process-required",()=>Reject(()=>Valid(process:arm.Process)));
Test("source-changed-no-restore",()=>Reject(()=>Valid(source:beforeBytes)));
Test("candidate-mismatch",()=>Reject(()=>Valid(c:complete with{CandidateHash=new string('D',64)})));
Test("schema-mismatch",()=>Reject(()=>Valid(c:complete with{Schema=99})));
Test("revision-mismatch",()=>Reject(()=>Valid(c:complete with{Revision="old"})));
Test("attempt-mismatch",()=>Reject(()=>Valid(c:complete with{Attempt=Guid.NewGuid().ToString("N")})));
Test("partial-snapshots",()=>Reject(()=>Valid(ss:states.Take(2).ToArray())));
Test("partial-capture-log",()=>Reject(()=>Valid(p:points.Take(2).ToArray())));
Test("partial-actions",()=>Reject(()=>Valid(a:records.Take(1).ToArray())));
Test("commit-hash-capture-tamper",()=>{var p=points.ToArray();p[1]=p[1] with{Window=Window() with{EndMs=999}};Reject(()=>Valid(p:p));});
Test("action-hash-tamper",()=>{var a=records.ToArray();a[1]=a[1] with{Action=actions[1] with{PlayerTurn=9}};Reject(()=>Valid(a:a));});
Test("missing-commit-field-not-defaulted",()=>{var n=JsonNode.Parse(Json.Serialize(complete))!.AsObject();n.Remove("ActionHashes");Reject(()=>Json.Read<CompleteData>(n.ToJsonString()));});
Test("missing-semantic-energy-not-zero",()=>{var n=JsonNode.Parse(initialFrozen)!;n["Player"]!.AsObject().Remove("Energy");Reject(()=>Projection.Thaw(n.ToJsonString()));});
Test("wrong-floor-round1",()=>Reject(()=>Contract.Playable(bound,initial with{Position=initial.Position with{TotalFloor=1}},null,null)));
Test("postcard-wrong-round",()=>{var t=AtPost(bound,states,actions);var s=Projection.Thaw(states[1]);Reject(()=>t.Capture(Projection.Freeze(s with{Position=s.Position with{Round=2}})));});
Test("nextturn-requires-round2",()=>{var t=AtNext(bound,states,actions);var s=Projection.Thaw(states[2]);Reject(()=>t.Capture(Projection.Freeze(s with{Position=s.Position with{Round=1}})));});
Test("round3-not-round2",()=>Reject(()=>Contract.Playable(bound,initial with{Position=initial.Position with{Round=3}},null,null,2)));
Test("room-and-encounter-independent",()=>Reject(()=>Contract.Playable(bound,initial,"ROOM.BAD",initial.Position.Encounter)));
Test("pending-request-is-not-accepted",()=>{var t=new Trajectory(bound);t.Capture(states[0]);t.Requested(actions[0],1,false);Reject(()=>t.Capture(states[1]));Assert(t.Accepted.Length==0);});
foreach(var failure in new[]{"not-executed","not-effect-success","cancelled","choice"})Test("not-accepted-"+failure,()=>
{var t=new Trajectory(bound);t.Capture(states[0]);t.Requested(actions[0],1,false);Reject(()=>t.Succeeded(actions[0],failure!="not-executed",failure!="not-effect-success",failure=="cancelled",failure=="choice",1));Assert(t.Accepted.Length==0);});
Test("duplicate-request",()=>{var t=new Trajectory(bound);t.Capture(states[0]);t.Requested(actions[0],1,false);Reject(()=>t.Requested(actions[0],1,false));});
Test("duplicate-success",()=>{var t=AtPost(bound,states,actions);Reject(()=>t.Succeeded(actions[0],true,true,false,false,1));});
Test("success-cannot-substitute-another-identical-defend-slot",()=>{var t=new Trajectory(bound);t.Capture(states[0]);t.Requested(actions[0],1,false);var substituted=actions[0] with{Card=actions[0].Card! with{Index=3}};Reject(()=>t.Succeeded(substituted,true,true,false,false,1));Assert(t.Accepted.Length==0);});
Test("unknown-operation",()=>Reject(()=>Actions.Validate(actions[0] with{Type=(Operation)99},initial,1)));
Test("wrong-sequence",()=>Reject(()=>Actions.Validate(actions[0] with{Sequence=2},initial,1)));
Test("wrong-action-phase",()=>Reject(()=>Actions.Validate(actions[0] with{Before=Step.NextTurn},initial,1)));
Test("stale-player-turn",()=>Reject(()=>Actions.Validate(actions[1],Projection.Thaw(states[1]),2)));
Test("target-required",()=>Reject(()=>Actions.Resolve(actions[0].Card! with{Target=""},initial)));
Test("enemy-target-not-supported",()=>Reject(()=>Actions.Resolve(actions[0].Card! with{Target="Enemy:1"},initial)));
Test("model-only-not-a-locator",()=>Reject(()=>Actions.Resolve(actions[0].Card! with{HandHash="",Index=-1},initial)));
Test("card-order-changed",()=>{var c=Projection.Thaw(initialFrozen);Array.Reverse(c.Player.Hand);Reject(()=>Actions.Resolve(actions[0].Card!,c));});
Test("same-model-fingerprint-changed",()=>{var c=Projection.Thaw(initialFrozen);var i=actions[0].Card!.Index;c.Player.Hand[i]=c.Player.Hand[i] with{Upgrade=1};Reject(()=>Actions.Resolve(actions[0].Card!,c));});
Test("duplicate-cards-exact-slot-still-deterministic",()=>Assert(Actions.Resolve(actions[0].Card!,initial)==0 && initial.Player.Hand.Count(c=>c.Model==Actions.Defend)>1));
Test("out-of-range-slot",()=>Reject(()=>Actions.Resolve(actions[0].Card! with{Index=999},initial)));
Test("no-eligible-card-stop-no-blind-substitution",()=>Reject(()=>Actions.Select(initial with{Player=initial.Player with{Hand=initial.Player.Hand.Where(c=>c.Model!=Actions.Defend).ToArray()}})));
Test("insufficient-energy",()=>Reject(()=>Actions.Select(initial with{Player=initial.Player with{Energy=0}})));
Test("B-action-not-recorded-as-A",()=>{var b=new Trajectory(bound,states,actions);b.Capture(states[0]);b.Dispatch(actions[0],1);Reject(()=>b.Requested(actions[0],1,false));Assert(b.Accepted.Length==0);});
Test("A-dispatcher-origin-rejected",()=>{var a=new Trajectory(bound);a.Capture(states[0]);Reject(()=>a.Requested(actions[0],1,true));});
Test("B-without-reserved-dispatch",()=>{var b=new Trajectory(bound,states,actions);b.Capture(states[0]);Reject(()=>b.Requested(actions[0],1,true));});
Test("B-dispatch-at-most-once",()=>{var b=new Trajectory(bound,states,actions);b.Capture(states[0]);b.Dispatch(actions[0],1);Reject(()=>b.Dispatch(actions[0],1));});
Test("B-initial-difference-no-dispatch",()=>{var b=new Trajectory(bound,states,actions);var changed=Projection.Freeze(initial with{Player=initial.Player with{Hp=initial.Player.Hp-1}});Assert(b.Capture(changed).Contains("$.Player.Hp") && b.Done);Reject(()=>b.Dispatch(actions[0],1));});
foreach(var kind in new[]{"Hp","CardProperty","RngState"})Test("per-point-single-"+kind+"-difference",()=>
{
    var b=new Trajectory(bound,states,actions);b.Capture(states[0]);b.Dispatch(actions[0],1);b.Requested(actions[0],1,true);b.Succeeded(actions[0],true,true,false,false,1);
    var s=Projection.Thaw(states[1]);
    if(kind=="Hp")s=s with{Player=s.Player with{Hp=s.Player.Hp-1}};
    if(kind=="CardProperty")s.Player.Hand[0]=s.Player.Hand[0] with{EffectiveCost=2};
    if(kind=="RngState"){var e=s.RunRng.Entries.ToArray();e[0]=e[0] with{State2=e[0].State2^1};s=s with{RunRng=Projection.Rng(s.RunRng.Seed,e)};}
    Assert(b.Capture(Projection.Freeze(s)).Length>0 && b.Done && b.Accepted.Length==1);Reject(()=>b.Dispatch(actions[1],1));
});
Test("expected-detached-from-live-and-input-arrays",()=>{var b=new Trajectory(bound,states,actions);var c=Projection.Thaw(states[1]);c.Player.Hand[0].Variables[0]=c.Player.Hand[0].Variables[0] with{Base=999};Assert(Projection.Compare(states[1],Projection.Freeze(c)).Length>0);Assert(b.Capture(initialFrozen).Length==0);});
Test("partial-arm-fresh-A-same-binary-keeps-journal",()=>
{var s=new PrivateStore(Path.Combine(output,"interrupted"));var old=Guid.NewGuid().ToString("N");var next=Guid.NewGuid().ToString("N");s.Activate(old);s.Write(old,"arm.json",arm);var oldBytes=s.ReadBytes(old,"arm.json");Assert(!AttemptValidation.ReadyB(s,old));s.Activate(next);Assert(s.Active()==next && oldBytes.SequenceEqual(s.ReadBytes(old,"arm.json")));});
Test("claimed-B-cannot-reclaim",()=>{var s=new PrivateStore(Path.Combine(output,"claimed"));s.Write(attempt,"A.complete.json",complete);s.Write(attempt,"A.ready-B.json",ready);Assert(AttemptValidation.ReadyB(s,attempt));s.Write(attempt,"B.claim.json",new {Process="B"});Assert(!AttemptValidation.ReadyB(s,attempt));});
Test("complete-without-post-deadline-success-proof-not-readyB",()=>{var s=new PrivateStore(Path.Combine(output,"no-ready-proof"));s.Write(attempt,"A.complete.json",complete);Assert(!AttemptValidation.ReadyB(s,attempt));});
Test("ready-proof-must-bind-exact-complete",()=>Reject(()=>Valid(r:ready with{CompleteHash=new string('F',64)})));
Test("terminal-even-with-commit-not-readyB",()=>{var s=new PrivateStore(Path.Combine(output,"terminal"));s.Write(attempt,"A.complete.json",complete);s.Write(attempt,"terminal.json",new {Reason="synthetic"});Assert(!AttemptValidation.ReadyB(s,attempt));});
Test("immutable-artifact-create-new",()=>{var s=new PrivateStore(Path.Combine(output,"immutable"));s.Write(attempt,"value.json",new{Value=1});Reject(()=>s.Write(attempt,"value.json",new{Value=2}));Assert(s.ReadBytes(attempt,"value.json").SequenceEqual("{\"Value\":1}"u8.ToArray()));});
Test("native-required-RNG-state",()=>{var n=JsonNode.Parse(sealedBytes)!;n["rng"]!["rngs"]!["shuffle"]!.AsObject().Remove("s2");Reject(()=>NativeCheckpoint.Parse(Encoding.UTF8.GetBytes(n.ToJsonString())));});
Test("action-deadline-no-accepted-and-explicit-blocker",()=>{var clock=new FakeClock();var ev=new List<Event>();var f=CaptureFlow(clock,ev.Add);Thirty(f,clock);Assert(f.Captured() && f.ActionStarted());clock.Now+=20000;Assert(!f.Poll() && f.Stage==Stage.Terminal);Assert(ev.Last().ActionBlocker=="ActionAcceptedAndEffectComplete");});
Test("manual-ready-wait-unlimited",()=>{var c=new FakeClock();var f=CaptureFlow(c);Thirty(f,c);Assert(f.Captured());c.Now+=1000000;Assert(f.Poll() && f.ActionStarted());});
Test("independent-action-and-capture-deadlines",()=>{var c=new FakeClock();var f=CaptureFlow(c);Thirty(f,c);Assert(f.Captured() && f.ActionStarted());c.Now+=19999;Assert(f.ActionSucceeded(false));c.Now+=19999;Assert(f.Poll());c.Now++;Assert(!f.Poll() && f.TerminalReason=="deadline-CaptureA");});
Test("all-three-independent-windows-rounds",()=>{var c=new FakeClock();var f=CaptureFlow(c);for(var i=0;i<3;i++){Thirty(f,c);Assert(f.Window.Count==30 && f.Captured());if(i<2)Assert(f.ActionStarted() && f.ActionSucceeded(false));}bool wrote=false;Assert(f.Complete(()=>wrote=true) && wrote && f.Stage==Stage.Complete);});
Test("unsettled-current-action-no-quiet-window",()=>{var c=new FakeClock();var f=CaptureFlow(c);for(var i=0;i<50;i++)f.Tick(i,Good().Select(x=>x.Name=="currentActionIsNull"?x with{Status=FieldStatus.False}:x).ToArray(),"refs");Reject(()=>_=f.Window);});
Test("frame-gap-and-object-identity-reset",()=>{var c=new FakeClock();var f=CaptureFlow(c);for(int i=0;i<20;i++){c.Now++;f.Tick(i,Good(),"x");}c.Now+=251;Assert(!f.Tick(20,Good(),"x"));Assert(f.Diagnostics!.Consecutive==1);c.Now++;f.Tick(21,Good(),"y");Assert(f.Diagnostics.Consecutive==1);});
Test("complete-writer-failure-no-readyB",()=>{var c=new FakeClock();var f=CaptureFlow(c);Thirty(f,c);Assert(f.Captured());Assert(!f.Complete(()=>throw new IOException("synthetic")) && f.Stage==Stage.Terminal);});
Test("ready-proof-writer-failure-terminal-instead-of-silent-complete",()=>{var c=new FakeClock();var events=new List<Event>();var f=CaptureFlow(c,events.Add);Thirty(f,c);Assert(f.Captured());Assert(!f.Complete(()=>{},()=>throw new IOException("synthetic")) && f.Stage==Stage.Terminal && f.TerminalReason=="ready-proof-write-failure");Assert(events.Count(e=>e.Name=="terminal")==1);});
Test("slow-final-commit-does-not-publish-success",()=>{var c=new FakeClock();var f=CaptureFlow(c);Thirty(f,c);Assert(f.Captured());Assert(!f.Complete(()=>c.Now+=20000) && f.TerminalReason=="deadline-final-commit");});
Test("terminal-wins-before-complete-no-marker",()=>{var c=new FakeClock();var f=CaptureFlow(c);Thirty(f,c);f.Captured();f.Fail("first");bool wrote=false;Assert(!f.Complete(()=>wrote=true) && !wrote);});
Test("terminal-complete-race-single-winner",()=>{for(var i=0;i<30;i++){var c=new FakeClock();var f=CaptureFlow(c);Thirty(f,c);f.Captured();int wrote=0;Parallel.Invoke(()=>f.Fail("stop"),()=>f.Complete(()=>Interlocked.Increment(ref wrote)));Assert((f.Stage==Stage.Complete && wrote==1)||(f.Stage==Stage.Terminal && wrote==0));}});
count+=DiagnosticsRegression.Run(Path.Combine(output,"diagnostics"));
count+=ActionObservationRegression.Run(Path.Combine(output,"action-diagnostics-r2"),bound,states,actions);
File.WriteAllText(Path.Combine(output,"contracts-receipt.json"),Json.Serialize(new {Source="OFFLINE_SYNTHETIC_NOT_GAME_RUNTIME",Contracts=count,PublicInitialHash=Json.Hash(publicA),PrivatePlayerDataRead=false,GameStarted=false}));
Console.WriteLine("PASS CONTRACTS "+count+"/"+count);

static void Assert(bool value){if(!value)throw new Exception("assertion failed");}
static void Reject(Action body){try{body();}catch(Exception e) when(e is InvalidDataException or System.Text.Json.JsonException or IOException or IndexOutOfRangeException){return;}throw new Exception("expected rejection");}
static Field[] Good()=>Flow.FieldNames.Select(x=>new Field(x,FieldStatus.True)).ToArray();
static QuietWindow Window()=>new(11,40,100,600,30,34,Good());
static Flow CaptureFlow(FakeClock clock,Action<Event>? events=null){var f=new Flow(clock,events??(_=>{}));Assert(f.FirstFrame(false)&&f.Arm()&&f.SaveStarted()&&f.BeginSeal()&&f.Sealed());return f;}
static void Thirty(Flow f,FakeClock c){for(var i=0;i<30;i++){c.Now++;Assert(f.Tick(100+i,Good(),"local")== (i==29));}}
static string[] Successors(Semantic initial)
{
    var initialText=Projection.Freeze(initial);var post=Projection.Thaw(initialText);var index=Actions.Select(post).Index;var card=post.Player.Hand[index];
    post=post with{Player=post.Player with{Hand=post.Player.Hand.Where((_,i)=>i!=index).ToArray(),Discard=post.Player.Discard.Append(card).ToArray(),Energy=post.Player.Energy-1,Block=post.Player.Block+5}};
    var postText=Projection.Freeze(post);var next=Projection.Thaw(postText);var rng=next.RunRng.Entries.ToArray();rng[0]=rng[0] with{Counter=rng[0].Counter+1,State0=rng[0].State0^1};
    next=next with{Position=next.Position with{Round=2},Player=next.Player with{Hp=next.Player.Hp-3,Energy=3,Block=0,Hand=next.Player.Draw.Take(5).ToArray(),Draw=next.Player.Draw.Skip(5).ToArray(),Discard=next.Player.Discard.Concat(next.Player.Hand).ToArray()},RunRng=Projection.Rng(next.RunRng.Seed,rng)};
    return [initialText,postText,Projection.Freeze(next)];
}
static AcceptedAction[] Log(Binding binding,string[] states){var t=new Trajectory(binding);t.Capture(states[0]);for(var i=0;i<2;i++){var a=t.Planned(1);t.Requested(a,1,false);t.Succeeded(a,true,true,false,false,1);t.Capture(states[i+1]);}return t.Accepted;}
static Trajectory AtPost(Binding b,string[] s,AcceptedAction[] a){var t=new Trajectory(b);t.Capture(s[0]);t.Requested(a[0],1,false);t.Succeeded(a[0],true,true,false,false,1);return t;}
static Trajectory AtNext(Binding b,string[] s,AcceptedAction[] a){var t=AtPost(b,s,a);t.Capture(s[1]);t.Requested(a[1],1,false);t.Succeeded(a[1],true,true,false,false,1);return t;}
static byte[] NativeFixture(bool roomBefore,string seed)
{
    var history=new MapPointHistoryEntry {MapPointType=MapPointType.Ancient,PlayerStats=[new(){PlayerId=0,CurrentHp=80,MaxHp=80}],Rooms=[new(){RoomType=RoomType.Event}]};
    var save=new SerializableRun {SchemaVersion=1,CurrentActIndex=0,Players=[new(){CharacterId=new ModelId("CHARACTER","IRONCLAD"),CurrentHp=80,MaxHp=80,MaxEnergy=3,
        Rng=new(){Seed=42,Rngs=Enum.GetValues<PlayerRngType>().ToDictionary(x=>x,_=>new SerializableRng{counter=1,state0=1,state1=2,state2=3,state3=4})},Deck=[new(){Id=new ModelId("CARD","DEFEND_IRONCLAD")}]}],
        SerializableRng=new(){Seed=seed,Rngs=Enum.GetValues<RunRngType>().ToDictionary(x=>x,_=>new SerializableRng{counter=2,state0=1,state1=2,state2=3,state3=4})},
        VisitedMapCoords=roomBefore?[new(3,0),new(2,1)]:[new(3,0)],MapPointHistory=[[history]],PreFinishedRoom=roomBefore?null:new(){RoomType=RoomType.Event,EventId=new("EVENT","NEOW"),IsPreFinished=true},GameMode=GameMode.Standard};
    return Encoding.UTF8.GetBytes(JsonSerializationUtility.ToJson(save));
}
static void PatchSmoke()
{
    var assembly=typeof(Probe).Assembly;var original=Task.CompletedTask;
    Assert(ReferenceEquals(original,typeof(Probe).GetMethod("ObserveSave",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[original,null,true])));
    var h=new Harmony("SPIKE001B.offline.patch");
    try{h.PatchAll(assembly);var methods=h.GetPatchedMethods().ToArray();Assert(methods.Length==7);foreach(var m in methods)Console.WriteLine("PATCHED "+m);}
    finally{h.UnpatchAll(h.Id);}Assert(!h.GetPatchedMethods().Any());Console.WriteLine("PASS PATCH/UNPATCH 7/7; no production target invoked");
}
static async Task DummySmoke()
{
    // Install only a synthetic Flow and suppress the Godot marker sink in this .NET-only test process.
    // Calling GD.Print without the engine would enter native bindings; it is not a dummy dependency.
    var flowField=typeof(Probe).GetField("_flow",BindingFlags.NonPublic|BindingFlags.Static)!;
    var markerField=typeof(Probe).GetField("_announcedTerminal",BindingFlags.NonPublic|BindingFlags.Static)!;
    flowField.SetValue(null,new Flow(new FakeClock(),_=>{})); markerField.SetValue(null,1);
    var h=new Harmony("SPIKE001B.offline.dummy");h.Patch(AccessTools.Method(typeof(Dummy),nameof(Dummy.Save)),postfix:new HarmonyMethod(typeof(DummyPatch),nameof(DummyPatch.Save)));
    h.Patch(AccessTools.Method(typeof(Dummy),nameof(Dummy.Action)),prefix:new HarmonyMethod(typeof(DummyPatch),nameof(DummyPatch.Prefix)),postfix:new HarmonyMethod(typeof(DummyPatch),nameof(DummyPatch.Postfix)));
    try
    {
        DummyPatch.Match=false;var tcs=new TaskCompletionSource();Dummy.Original=tcs.Task;Assert(ReferenceEquals(Dummy.Save(),tcs.Task));tcs.SetResult();await tcs.Task;
        DummyPatch.Match=true;Dummy.Original=Task.CompletedTask;var result=Dummy.Save();Assert(!ReferenceEquals(result,Dummy.Original));await result;
        var fault=new InvalidOperationException("dummy");Dummy.Original=Task.FromException(fault);result=Dummy.Save();try{await result;}catch(InvalidOperationException e){Assert(ReferenceEquals(fault,e));}Assert(result.IsFaulted);
        using var cancel=new CancellationTokenSource();cancel.Cancel();Dummy.Original=Task.FromCanceled(cancel.Token);result=Dummy.Save();try{await result;}catch(OperationCanceledException e){Assert(e.CancellationToken==cancel.Token);}Assert(result.IsCanceled);
        foreach(var task in new[]{Task.CompletedTask,Task.FromException(fault),Task.FromCanceled(cancel.Token)})
        { Dummy.Original=task;var returned=Dummy.Action(37);Assert(ReferenceEquals(returned,task)&&Dummy.SeenArgument==37); }
    }
    finally{h.UnpatchAll(h.Id);flowField.SetValue(null,null);}Assert(!h.GetPatchedMethods().Any());Console.WriteLine("PASS DUMMY save wrapper success/fault/cancel; throwing action observers preserve exact Task/args/result");
}
sealed class FakeClock:IClock{public long Now;public long Milliseconds=>Now;}
static class Dummy {public static Task Original=Task.CompletedTask;public static int SeenArgument;[MethodImpl(MethodImplOptions.NoInlining)]public static Task Save()=>Original;[MethodImpl(MethodImplOptions.NoInlining)]public static Task Action(int value){SeenArgument=value;return Original;}}
static class DummyPatch
{
 public static bool Match;
 public static void Save(ref Task __result){if(Match)__result=ProductionObserver.Wrap(__result,()=>throw new IOException("observer"),_=>{});}
 public static void Prefix()=>InvokeSafe(); public static void Postfix(Task __result)=>InvokeSafe();
 private static void InvokeSafe()=>typeof(Probe).GetMethod("Safe",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[(Action)(()=>throw new IOException("observer"))]);
}
