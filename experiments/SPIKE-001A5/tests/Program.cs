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
using MegaCrit.Sts2.Core.Saves.Runs;
using Spike001A5;
using Spike001A5.Core;

var mode=args.FirstOrDefault()??"contracts";
if(mode=="patch") { PatchSmoke(); return; }
if(mode=="dummy") { await DummySmoke(); return; }
var output=Path.GetFullPath(args.Length>1?args[1]:"evidence/spike-001a5"); Directory.CreateDirectory(output);
int count=0;
void Test(string name,Action test) { test(); count++; Console.WriteLine("PASS "+name); }
async Task AsyncTest(string name,Func<Task> test) { await test(); count++; Console.WriteLine("PASS "+name); }
void Reject(Action test) { try { test(); } catch(Exception e) when(e is InvalidDataException or System.Text.Json.JsonException or IOException) { return; } throw new Exception("unexpected acceptance"); }
var predecessor=NativeCheckpoint.Parse(NativeFixture(false)).Facts;
var sealedBytes=NativeFixture(true); var saved=NativeCheckpoint.Parse(sealedBytes).Facts;
var target=new Target(saved.Seed,saved.Character,0,saved.Row,saved.Col,predecessor.Row,predecessor.Col,1);
Binding? binding=null;
Test("native-schema-predecessor-nested-positive",()=> { Contract.Arm(target,1,predecessor); Assert(!predecessor.RoomNull && predecessor.Floor==1); });
Test("floor-unbound-before-successful-seal",()=>Assert(binding is null));
Test("native-schema-successful-seal-positive",()=> { binding=Contract.Seal(target,predecessor,saved); Assert(binding.PlayableFloor==2); });
var bound=binding!;
var semantic=SyntheticSemantic(); var frozen=Projection.Freeze(semantic);
Test("full-positive-A-seal-A-capture-fresh-B-compare",()=>
{
    var clock=new FakeClock(); var events=new List<Event>(); var a=new Flow(clock,events.Add);
    Assert(a.FirstFrame(false) && a.Arm() && a.SaveStarted() && a.BeginSeal() && a.Sealed());
    Contract.Playable(bound,semantic,null,null);
    Thirty(a,clock); Assert(a.Captured() && a.Stage==Stage.AComplete);
    Contract.Continue(bound,saved,"A","B",target.Seed,target.Character,0,1,target.Row,target.Col);
    var b=new Flow(clock,events.Add); Assert(b.FirstFrame(true) && b.ContinueStarted());
    var detached=Projection.Thaw(frozen); Contract.Playable(bound,detached,semantic.Position.RoomModel,semantic.Position.Encounter);
    Thirty(b,clock); Assert(b.Captured() && b.Stage==Stage.Complete && Projection.Compare(frozen,Projection.Freeze(detached)).Length==0);
    File.WriteAllText(Path.Combine(output,"synthetic-positive-trace.json"),Json.Serialize(new { Source="SYNTHETIC_FIXED_NATIVE_SCHEMA_NOT_RUNTIME",Events=events,Checkpoint=saved,Result="REACHABLE"}));
});
Test("floor-live-missing-rejected",()=>Reject(()=>Contract.Arm(target,null,predecessor)));
Test("floor-live-mismatch-rejected",()=>Reject(()=>Contract.Arm(target,0,predecessor)));
Test("floor-negative-rejected",()=>Reject(()=>Contract.Arm(target,-1,predecessor)));
Test("floor-save-mismatch-rejected",()=>Reject(()=>Contract.Seal(target,predecessor,saved with{Floor=0})));
Test("checkpoint-non-null-rejected",()=>Reject(()=>Contract.Seal(target,predecessor,saved with{RoomNull=false})));
Test("checkpoint-unmodified-predecessor-rejected",()=>Reject(()=>Contract.Seal(target,predecessor,saved with{Hash=predecessor.Hash})));
Test("checkpoint-multiple-players-rejected",()=>Reject(()=>Contract.Seal(target,predecessor,saved with{Players=2})));
Test("predecessor-null-room-rejected",()=>Reject(()=>Contract.Arm(target,1,predecessor with{RoomNull=true})));
Test("floor-playable-off-by-one-rejected",()=>Reject(()=>Contract.Playable(bound,semantic with{Position=semantic.Position with{TotalFloor=1}},null,null)));
Test("room-model-separated-from-encounter",()=>Reject(()=>Contract.Playable(bound,semantic,semantic.Position.Encounter,semantic.Position.Encounter)));
Test("encounter-mismatch-rejected",()=>Reject(()=>Contract.Playable(bound,semantic,semantic.Position.RoomModel,"ENCOUNTER.OTHER")));
Test("same-process-rejected",()=>Reject(()=>Contract.Continue(bound,saved,"A","A",target.Seed,target.Character,0,1,target.Row,target.Col)));
Test("source-changed-before-B-rejected",()=>Reject(()=>Contract.Continue(bound,saved with{Hash=new string('F',64)},"A","B",target.Seed,target.Character,0,1,target.Row,target.Col)));
Test("restored-pre-room-floor-missing-rejected",()=>Reject(()=>Contract.Continue(bound,saved,"A","B",target.Seed,target.Character,0,null,target.Row,target.Col)));
Test("coordinate-row-col-not-swapped",()=>Reject(()=>Contract.Continue(bound,saved,"A","B",target.Seed,target.Character,0,1,target.Col,target.Row)));
foreach(var key in new[]{"schema_version","game_mode","players","rng","visited_map_coords","map_point_history","current_act_index","pre_finished_room"})
    Test("native-missing-"+key,()=> { var node=JsonNode.Parse(sealedBytes)!.AsObject(); node.Remove(key); Reject(()=>NativeCheckpoint.Parse(Encoding.UTF8.GetBytes(node.ToJsonString()))); });
Test("native-coordinate-missing",()=> { var n=JsonNode.Parse(sealedBytes)!; n["visited_map_coords"]![0]!.AsObject().Remove("row"); Reject(()=>NativeCheckpoint.Parse(Encoding.UTF8.GetBytes(n.ToJsonString()))); });
Test("malformed-native-parse-rejected",()=>Reject(()=>NativeCheckpoint.Parse("{"u8.ToArray())));
Test("semantic-required-energy-missing-rejected",()=> {var n=JsonNode.Parse(frozen)!;n["Player"]!.AsObject().Remove("Energy");Reject(()=>Projection.Thaw(n.ToJsonString()));});
Test("semantic-null-pile-rejected",()=>Reject(()=>Projection.Freeze(semantic with {Player=semantic.Player with{Hand=null!}})));
Test("rng-one-key-missing-rejected",()=>Reject(()=>Projection.Freeze(semantic with{RunRng=Projection.Rng(semantic.RunRng.Seed,semantic.RunRng.Entries.Skip(1))})));
Test("rng-invalid-digest-rejected",()=>Reject(()=>Projection.Freeze(semantic with{RunRng=semantic.RunRng with{Digest="wrong"}})));
Test("detached-nested-oracle-no-alias",()=> {var b=Projection.Thaw(frozen);b.Player.Hand[0].Variables[0]=b.Player.Hand[0].Variables[0] with{Base=999};Assert(semantic.Player.Hand[0].Variables[0].Base==6 && Projection.Compare(frozen,Projection.Freeze(b)).Contains("$.Player.Hand[0].Variables[0].Base"));});
Test("pile-order-discriminates",()=> {var b=Projection.Thaw(frozen);Array.Reverse(b.Player.Hand);Assert(Projection.Compare(frozen,Projection.Freeze(b)).Length>0);});
Test("single-field-energy-discriminates",()=>Assert(Projection.Compare(frozen,Projection.Freeze(semantic with{Player=semantic.Player with{Energy=2}})).SequenceEqual(new[]{"$.Player.Energy"})));
Test("rng-state-discriminates",()=> {var b=Projection.Thaw(frozen);b.RunRng.Entries[0]=b.RunRng.Entries[0] with{State3=99};b=b with{RunRng=Projection.Rng(b.RunRng.Seed,b.RunRng.Entries)};Assert(Projection.Compare(frozen,Projection.Freeze(b)).Any(x=>x.EndsWith("State3")));});
Test("first-frame-evidence-failure-never-ready",()=> {var f=new Flow(new FakeClock(),_=>throw new IOException());Assert(!f.FirstFrame(false) && f.Stage==Stage.Terminal);});
Test("first-frame-evidence-exceeds-deadline-never-ready",()=> {var c=new FakeClock();var f=new Flow(c,e=>{if(e.Name=="first-tick")c.Now=10000;});Assert(!f.FirstFrame(false) && f.TerminalReason=="deadline-Startup");});
Test("startup-no-frame-deadline",()=> {var c=new FakeClock();var f=new Flow(c,_=>{});c.Now=10000;f.Poll();Assert(f.TerminalReason=="deadline-Startup");});
Test("ready-manual-wait-no-deadline",()=> {var c=new FakeClock();var f=new Flow(c,_=>{});Assert(f.FirstFrame(false));c.Now=999999;Assert(f.Poll() && f.Stage==Stage.ReadyArm && f.Arm());});
foreach(var stage in new[]{Stage.ProductionSave,Stage.Seal,Stage.CaptureA,Stage.CaptureB})
    Test("independent-deadline-"+stage,()=> {var c=new FakeClock();var f=At(stage,c);c.Now+=stage==Stage.ProductionSave?15000:stage==Stage.Seal?5000:20000;f.Poll();Assert(f.Stage==Stage.Terminal && f.TerminalReason=="deadline-"+stage && !f.Captured());});
Test("sealed-never-ready-deadline",()=> {var c=new FakeClock();var f=At(Stage.CaptureA,c);for(var i=0;i<20001;i+=10){c.Now=i;f.Tick(i,Good().Select(x=>x.Name=="handPresent"?x with{Status=FieldStatus.False}:x).ToArray(),"A");}c.Now=20000;f.Poll();Assert(f.Stage==Stage.Terminal);});
Test("all-20-fields-30-adjacent-frames",()=> {var c=new FakeClock();var f=At(Stage.CaptureA,c);for(var i=1;i<30;i++){c.Now++;Assert(!f.Tick(i,Good(),"A"));}c.Now++;Assert(f.Tick(30,Good(),"A") && f.Captured());});
foreach(var field in Flow.FieldNames)
    Test("false-field-resets-"+field,()=> {var c=new FakeClock();var f=At(Stage.CaptureA,c);for(var i=1;i<30;i++)f.Tick(i,Good(),"A");var bad=Good().Select(x=>x.Name==field?x with{Status=FieldStatus.False}:x).ToArray();Assert(!f.Tick(30,bad,"A"));for(var i=31;i<60;i++)Assert(!f.Tick(i,Good(),"A"));Assert(f.Tick(60,Good(),"A"));});
Test("frame-gap-resets",()=> {var c=new FakeClock();var f=At(Stage.CaptureA,c);for(var i=1;i<30;i++)f.Tick(i,Good(),"A");Assert(!f.Tick(31,Good(),"A"));for(var i=32;i<60;i++)Assert(!f.Tick(i,Good(),"A"));Assert(f.Tick(60,Good(),"A"));});
Test("wall-frame-gap-resets",()=> {var c=new FakeClock();var f=At(Stage.CaptureA,c);for(var i=1;i<30;i++)f.Tick(i,Good(),"A");c.Now=251;Assert(!f.Tick(30,Good(),"A"));});
Test("phase-local-reference-change-resets",()=> {var c=new FakeClock();var f=At(Stage.CaptureA,c);for(var i=1;i<30;i++)f.Tick(i,Good(),"refs1");Assert(!f.Tick(30,Good(),"refs2"));});
Test("readiness-schema-missing-terminal",()=> {var c=new FakeClock();var f=At(Stage.CaptureA,c);Assert(!f.Tick(1,Good().Skip(1).ToArray(),"A") && f.Stage==Stage.Terminal);});
Test("readiness-exception-terminal",()=> {var c=new FakeClock();var f=At(Stage.CaptureA,c);Assert(!f.Tick(1,Good().Select(x=>x.Name=="roomPresent"?x with{Status=FieldStatus.Exception}:x).ToArray(),"A") && f.Stage==Stage.Terminal);});
Test("terminal-first-wins-no-further-collection",()=> {var c=new FakeClock();var ev=new List<Event>();var f=At(Stage.CaptureA,c,ev.Add);Assert(f.Fail("first") && !f.Fail("second"));Assert(!f.Sealed() && !f.Tick(1,Good(),"A") && !f.Captured() && f.TerminalReason=="first" && ev.Count(x=>x.Name=="terminal")==1);});
Test("terminal-race-single-winner",()=> {var c=new FakeClock();var ev=new List<Event>();var f=At(Stage.CaptureA,c,ev.Add);Parallel.Invoke(()=>f.Fail("a"),()=>f.Fail("b"));Assert(ev.Count(x=>x.Name=="terminal")==1);});
Test("deadline-wins-during-seal-no-capture",()=> {var c=new FakeClock();var f=At(Stage.Seal,c);c.Now=5000;f.Poll();Assert(!f.Sealed() && f.Stage==Stage.Terminal);});
Test("seal-evidence-exceeds-deadline-no-capture",()=> {var c=new FakeClock();var f=At(Stage.Seal,c,e=>{if(e.Name=="checkpoint-sealed")c.Now=5000;});Assert(!f.Sealed() && f.TerminalReason=="deadline-Seal");});
Test("capture-evidence-failure-no-complete",()=> {var c=new FakeClock();var f=At(Stage.CaptureB,c,e=> {if(e.Name=="compare-complete")throw new IOException();});Thirty(f,c);Assert(!f.Captured() && f.Stage==Stage.Terminal);});
await AsyncTest("task-before-room-seal-awaited",async()=>
{
    var original=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    bool createdRoom=false,seal=false;
    var wrapper=ProductionObserver.Wrap(original.Task,async()=> {entered.SetResult();await gate.Task;seal=true;},_=>{});
    var caller=Caller(); original.SetResult();await entered.Task;Assert(!createdRoom && !wrapper.IsCompleted);gate.SetResult();await caller;Assert(createdRoom && seal);
    async Task Caller(){await wrapper;Assert(seal);createdRoom=true;}
});
await AsyncTest("task-original-fault-exception-identity",async()=> {var original=new InvalidOperationException("synthetic");var t=Task.FromException(original);var w=ProductionObserver.Wrap(t,()=>throw new Exception("must-not-run"),_=>{});try{await w;throw new Exception("accepted");}catch(InvalidOperationException e){Assert(ReferenceEquals(original,e) && w.IsFaulted);} });
await AsyncTest("task-faulted-OCE-stays-faulted",async()=> {var e=new OperationCanceledException();var w=ProductionObserver.Wrap(Task.FromException(e),()=>throw new Exception(),_=>{});try{await w;}catch(OperationCanceledException actual){Assert(ReferenceEquals(actual,e) && w.IsFaulted && !w.IsCanceled);} });
await AsyncTest("task-original-multiple-faults-preserved",async()=> {var t=new TaskCompletionSource();var es=new Exception[]{new InvalidOperationException(),new IOException()};t.SetException(es);var w=ProductionObserver.Wrap(t.Task,()=>throw new Exception(),_=>{});try{await w;}catch(InvalidOperationException){}Assert(w.Exception!.InnerExceptions.SequenceEqual(es));});
await AsyncTest("task-cancel-status-token-preserved",async()=> {using var token=new CancellationTokenSource();token.Cancel();var w=ProductionObserver.Wrap(Task.FromCanceled(token.Token),()=>throw new Exception(),_=>throw new IOException());try{await w;throw new Exception("accepted");}catch(OperationCanceledException e){Assert(w.IsCanceled && e.CancellationToken==token.Token);} });
await AsyncTest("task-observer-throw-does-not-fault-success",async()=> {var terminals=0;var w=ProductionObserver.Wrap(Task.CompletedTask,()=>throw new IOException(),_=>terminals++);await w;Assert(w.IsCompletedSuccessfully && terminals==1);});
await AsyncTest("task-terminal-writer-throw-does-not-fault-success",async()=> {var w=ProductionObserver.Wrap(Task.CompletedTask,()=>throw new IOException(),_=>throw new IOException());await w;Assert(w.IsCompletedSuccessfully);});
await AsyncTest("real-timer-no-tick-after-seal-terminal",async()=> {var c=new FakeClock();var ended=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var f=At(Stage.CaptureA,c,e=>{if(e.Name=="terminal")ended.TrySetResult();});c.Now=20000;using var timer=new Timer(_=>f.Poll(),null,5,5);await ended.Task.WaitAsync(TimeSpan.FromSeconds(2));Assert(f.TerminalReason=="deadline-CaptureA" && !f.Captured());});
Test("private-store-create-new-no-overwrite",()=> {var root=Path.Combine(output,"synthetic-store-"+Guid.NewGuid().ToString("N"));var store=new PrivateStore(root);var id=Guid.NewGuid().ToString("N");store.Write(id,"A.semantic.json",new{Source="SYNTHETIC"});Reject(()=>store.Write(id,"A.semantic.json",new{}));Assert(store.ReadBytes(id,"A.semantic.json").Length>0);});
Test("private-store-path-escape-rejected",()=> {var store=new PrivateStore(output);Reject(()=>store.Write(new string('a',32),"../current_run.save",new{}));});
Test("private-byte-writer-exact-roundtrip",()=> {var root=Path.Combine(output,"synthetic-store-"+Guid.NewGuid().ToString("N"));var store=new PrivateStore(root);var id=Guid.NewGuid().ToString("N");store.WriteBytes(id,"checkpoint.private.json",sealedBytes,PrivateStore.NativeCap);Assert(store.ReadBytes(id,"checkpoint.private.json",PrivateStore.NativeCap).AsSpan().SequenceEqual(sealedBytes));});
Test("persisted-native-A-fresh-B-complete-positive",()=>
{
    var root=Path.Combine(output,"synthetic-store-"+Guid.NewGuid().ToString("N"));var store=new PrivateStore(root);var id=Guid.NewGuid().ToString("N");var c=new FakeClock();var a=At(Stage.CaptureA,c);Thirty(a,c);
    var ad=new ArmData(PrivateStore.Schema,PrivateStore.Revision,new string('A',64),"A",target,predecessor);var sd=new SealData(PrivateStore.Schema,PrivateStore.Revision,ad.CandidateHash,"A",bound);
    var cd=new CaptureData(PrivateStore.Schema,PrivateStore.Revision,ad.CandidateHash,"A","A",Json.Hash(frozen),a.Window);
    store.Write(id,"arm.json",ad);store.Activate(id);store.WriteBytes(id,"checkpoint.private.json",sealedBytes,PrivateStore.NativeCap);store.Write(id,"seal.json",sd);
    store.WriteBytes(id,"A.semantic.json",Encoding.UTF8.GetBytes(frozen));store.Write(id,"A.capture.json",cd);Assert(a.Captured());store.Write(id,"A.complete.json",cd);
    var fresh=new PrivateStore(root);Assert(fresh.Active()==id);
    AttemptValidation.BInput(fresh.Read<ArmData>(id,"arm.json"),fresh.Read<SealData>(id,"seal.json"),fresh.Read<CaptureData>(id,"A.capture.json"),fresh.Read<CaptureData>(id,"A.complete.json"),ad.CandidateHash,"B",frozen,fresh.ReadBytes(id,"checkpoint.private.json",PrivateStore.NativeCap),sealedBytes);
    var b=new Flow(c,_=>{});Assert(b.FirstFrame(true));Contract.Continue(bound,saved,"A","B",target.Seed,target.Character,0,1,target.Row,target.Col);Assert(b.ContinueStarted());Thirty(b,c);Assert(b.Captured());
    var bDetached=Projection.Thaw(frozen);Assert(Projection.Compare(frozen,Projection.Freeze(bDetached)).Length==0);
});
var wc=new FakeClock();var wf=At(Stage.CaptureA,wc);Thirty(wf,wc);
var armData=new ArmData(PrivateStore.Schema,PrivateStore.Revision,new string('A',64),"A",target,predecessor);
var sealData=new SealData(PrivateStore.Schema,PrivateStore.Revision,armData.CandidateHash,"A",bound);
var capData=new CaptureData(PrivateStore.Schema,PrivateStore.Revision,armData.CandidateHash,"A","A",Json.Hash(frozen),wf.Window);
Test("B-gate-r1-schema-rejected",()=>Reject(()=>AttemptValidation.BInput(armData with{Schema=1},sealData,capData,capData,armData.CandidateHash,"B",frozen,sealedBytes,sealedBytes)));
Test("B-gate-r1-revision-rejected",()=>Reject(()=>AttemptValidation.BInput(armData with{Revision="phase0-r1"},sealData,capData,capData,armData.CandidateHash,"B",frozen,sealedBytes,sealedBytes)));
Test("B-gate-candidate-changed-rejected",()=>Reject(()=>AttemptValidation.BInput(armData,sealData,capData,capData,new string('B',64),"B",frozen,sealedBytes,sealedBytes)));
Test("B-gate-incomplete-A-rejected",()=>Reject(()=>AttemptValidation.BInput(armData,sealData,capData,capData with{SemanticHash="wrong"},armData.CandidateHash,"B",frozen,sealedBytes,sealedBytes)));
Test("B-gate-changed-native-bytes-rejected",()=>Reject(()=>AttemptValidation.BInput(armData,sealData,capData,capData,armData.CandidateHash,"B",frozen,sealedBytes,NativeFixture(false))));
Test("B-gate-same-process-rejected",()=>Reject(()=>AttemptValidation.BInput(armData,sealData,capData,capData,armData.CandidateHash,"A",frozen,sealedBytes,sealedBytes)));
Test("B-gate-semantic-hash-tamper-rejected",()=>Reject(()=>AttemptValidation.BInput(armData,sealData,capData,capData,armData.CandidateHash,"B",frozen.Replace("\"Energy\":3","\"Energy\":2"),sealedBytes,sealedBytes)));
Test("B-gate-window-incomplete-rejected",()=>Reject(()=>AttemptValidation.Window(wf.Window with{Count=29})));
Test("B-gate-missing-A-process-rejected",()=>Reject(()=>AttemptValidation.BInput(armData with{Process=null!},sealData,capData,capData,armData.CandidateHash,"B",frozen,sealedBytes,sealedBytes)));
Test("native-rng-missing-state-rejected",()=> {var n=JsonNode.Parse(sealedBytes)!;n["rng"]!["rngs"]!["shuffle"]!.AsObject().Remove("s3");Reject(()=>NativeCheckpoint.Parse(Encoding.UTF8.GetBytes(n.ToJsonString())));});
Test("native-player-rng-missing-rejected",()=> {var n=JsonNode.Parse(sealedBytes)!;n["players"]![0]!.AsObject().Remove("rng");Reject(()=>NativeCheckpoint.Parse(Encoding.UTF8.GetBytes(n.ToJsonString())));});
Test("native-zero-rng-counter-present-valid",()=> {var n=JsonNode.Parse(sealedBytes)!;n["rng"]!["rngs"]!["shuffle"]!["counter"]=0;Assert(NativeCheckpoint.Parse(Encoding.UTF8.GetBytes(n.ToJsonString())).Save.SerializableRng.Rngs[RunRngType.Shuffle].counter==0);});
count+=DiagnosticsRegression.Run(Path.Combine(output,"capture-diagnostics-r2"));
File.WriteAllBytes(Path.Combine(output,"synthetic-native-predecessor.json"),NativeFixture(false));
File.WriteAllBytes(Path.Combine(output,"synthetic-native-room-before.json"),sealedBytes);
File.WriteAllText(Path.Combine(output,"synthetic-semantic.json"),frozen);
File.WriteAllText(Path.Combine(output,"synthetic-fixture-provenance.json"),Json.Serialize(new{Source="SYNTHETIC; fixed DLL JsonSerializationUtility serializer/parser; no live checkpoint read",NativeShape="SerializableRun with existing nested MapPointHistory/Rooms/PlayerStats",GameHash="0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9",TestCount=count}));
Console.WriteLine($"PASS {count}/{count} offline contracts; SYNTHETIC, not runtime A/B");

static void Assert(bool value){if(!value)throw new Exception("assertion failed");}
static Field[] Good()=>Flow.FieldNames.Select(x=>new Field(x,FieldStatus.True)).ToArray();
static Flow At(Stage stage,IClock clock,Action<Event>? events=null)
{
    var f=new Flow(clock,events??(_=>{}));Assert(f.FirstFrame(stage==Stage.CaptureB));
    if(stage==Stage.CaptureB){Assert(f.ContinueStarted());return f;}
    Assert(f.Arm() && f.SaveStarted());if(stage==Stage.ProductionSave)return f;
    Assert(f.BeginSeal());if(stage==Stage.Seal)return f;Assert(f.Sealed());return f;
}
static void Thirty(Flow f,FakeClock clock){for(var i=1;i<=30;i++){clock.Now++;Assert(f.Tick(i,Good(),"phase-local")== (i==30));}}
static Semantic SyntheticSemantic()
{
    Card Card(string id)=>new(id,0,false,"{\"id\":\""+id+"\"}",1,1,false,null,0,1,false,[new("Damage",6,6)],1,[],[],null,null);
    var run=Projection.Rng("SYNTHETIC-SEED",Projection.RunKeys.Select(x=>new RngEntry(x,2,1,2,3,4)));
    var player=Projection.Rng("42",Projection.PlayerKeys.Select(x=>new RngEntry(x,1,5,6,7,8)));
    return new(new(0,1,2,"ROOM.MONSTER","Monster","ENCOUNTER.SYNTHETIC",2,1),
        new("CHARACTER.IRONCLAD",80,80,3,0,0,[],[Card("CARD.STRIKE"),Card("CARD.DEFEND")],[Card("CARD.BASH")],[],[]),
        [new("MONSTER.SYNTHETIC",1,20,20,0,true,false,false,"ATTACK",[],Projection.Rng("derived-instance;seed-not-serialized",[new("Instance",1,9,10,11,12)]))],run,player);
}
static byte[] NativeFixture(bool roomBefore)
{
    var history=new MapPointHistoryEntry {MapPointType=MapPointType.Ancient,PlayerStats=[new(){PlayerId=0,CurrentHp=80,MaxHp=80}],Rooms=[new(){RoomType=RoomType.Event}]};
    var save=new SerializableRun {SchemaVersion=1,CurrentActIndex=0,Players=[new(){CharacterId=new ModelId("CHARACTER","IRONCLAD"),CurrentHp=80,MaxHp=80,MaxEnergy=3,
        Rng=new(){Seed=42,Rngs=Enum.GetValues<PlayerRngType>().ToDictionary(x=>x,_=>new SerializableRng{counter=1,state0=1,state1=2,state2=3,state3=4})},
        Deck=[new(){Id=new ModelId("CARD","STRIKE"),CurrentUpgradeLevel=1}]}],
        SerializableRng=new(){Seed="SYNTHETIC-SEED",Rngs=Enum.GetValues<RunRngType>().ToDictionary(x=>x,_=>new SerializableRng{counter=2,state0=1,state1=2,state2=3,state3=4})},
        VisitedMapCoords=roomBefore?[new(3,0),new(2,1)]:[new(3,0)],MapPointHistory=[[history]],
        PreFinishedRoom=roomBefore?null:new(){RoomType=RoomType.Event,EventId=new("EVENT","NEOW"),IsPreFinished=true},
        GameMode=GameMode.Standard};
    return Encoding.UTF8.GetBytes(JsonSerializationUtility.ToJson(save));
}
static void PatchSmoke()
{
    var assembly=typeof(Probe).Assembly;
    var original=Task.CompletedTask;
    var observed=(Task)typeof(Probe).GetMethod("ObserveSave",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[original,null,true])!;
    Assert(ReferenceEquals(original,observed));
    Assert(typeof(Godot.SceneTree).GetEvent("ProcessFrame")!.EventHandlerType==typeof(Action));
    Assert(!assembly.GetTypes().Any(t=>t.IsSubclassOf(typeof(Godot.Node))));
    var h=new Harmony("SPIKE001A5.offline.patch");
    try{h.PatchAll(assembly);var methods=h.GetPatchedMethods().ToArray();Assert(methods.Length==4);foreach(var m in methods)Console.WriteLine("PATCHED "+m);}
    finally{h.UnpatchAll(h.Id);}Assert(!h.GetPatchedMethods().Any());
    Console.WriteLine("PASS PATCH/UNPATCH 4/4, no production target invoked; actual disarmed ObserveSave preserves Task; no custom Node ticker");
}
static async Task DummySmoke()
{
    var h=new Harmony("SPIKE001A5.offline.dummy");h.Patch(AccessTools.Method(typeof(Dummy),nameof(Dummy.Save)),postfix:new HarmonyMethod(typeof(DummyPatch),nameof(DummyPatch.Postfix)));
    try
    {
        DummyPatch.Match=false;var original=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);Dummy.Original=original.Task;
        Assert(ReferenceEquals(Dummy.Save(),original.Task));original.SetResult();await original.Task;
        DummyPatch.Match=true;Dummy.Original=Task.CompletedTask;var w=Dummy.Save();Assert(!ReferenceEquals(w,Dummy.Original));await w;Assert(w.IsCompletedSuccessfully);
        var fault=new InvalidOperationException("dummy");Dummy.Original=Task.FromException(fault);w=Dummy.Save();try{await w;}catch(InvalidOperationException e){Assert(ReferenceEquals(fault,e));}Assert(w.IsFaulted);
        using var cancel=new CancellationTokenSource();cancel.Cancel();Dummy.Original=Task.FromCanceled(cancel.Token);w=Dummy.Save();try{await w;}catch(OperationCanceledException e){Assert(e.CancellationToken==cancel.Token);}Assert(w.IsCanceled);
    }
    finally{h.UnpatchAll(h.Id);}Assert(!h.GetPatchedMethods().Any());Console.WriteLine("PASS DUMMY unchanged nonmatching Task; matched success/fault/cancel; throwing observer noninterference");
}
sealed class FakeClock:IClock{public long Now;public long Milliseconds=>Now;}
static class Dummy {public static Task Original=Task.CompletedTask;[MethodImpl(MethodImplOptions.NoInlining)]public static Task Save()=>Original;}
static class DummyPatch {public static bool Match;public static void Postfix(ref Task __result){if(Match)__result=ProductionObserver.Wrap(__result,()=>throw new IOException("observer"),_=>{});}}
