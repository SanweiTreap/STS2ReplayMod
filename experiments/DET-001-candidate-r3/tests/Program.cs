using System.Numerics;
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
using Det001;
using Det001.Core;
using Json=Det001.Core.Json;
using Card=Det001.Core.Card;

if(args.Length!=2)throw new ArgumentException("workspace new-output-directory");
var workspace=Path.GetFullPath(args[0]);var output=Path.GetFullPath(args[1]);
Check(output.StartsWith(Path.Combine(workspace,"evidence","det-001","candidate-r3")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) && !Directory.Exists(output));
Directory.CreateDirectory(output);int count=0;var names=new List<string>();
void Test(string name,Action body){body();names.Add(name);count++;Console.WriteLine("PASS "+name);}
void Save(string name,object? value)=>File.WriteAllText(Path.Combine(output,name),Json.Serialize(value),new UTF8Encoding(false));
var publicPath=Path.Combine(workspace,"evidence/spike-001b/runtime/records-4f958fdc207f4b9ba83ede826848f9ed/A.point-0.semantic.json");
var source=File.ReadAllText(publicPath);Check(Json.Hash(source)=="BBBD2D4C1B71FCB0CBF8747224717A888137C377569ACD654074BF54E58E8E08");
var actual=Projection.Thaw(source);var node=JsonNode.Parse(source)!;
// Detached synthetic mutation only; original actual Stomp remains sealed and ineligible.
node["Player"]!["Hand"]![3]=node["Player"]!["Hand"]![0]!.DeepClone();
var s0=Projection.Thaw(node.ToJsonString());var seed=s0.RunRng.Seed;
var arbitrary=Independent(s0.RunRng.Entries.Single(e=>e.Key=="Shuffle"),37); // starts counter 46, not 9
s0=s0 with{RunRng=Projection.Rng(seed,s0.RunRng.Entries.Select(e=>e.Key=="Shuffle"?arbitrary:e))};
var h=s0.Player.Hand;var d=s0.Player.Draw;var one=Array.FindIndex(h,Actions.Simple);var two=Array.FindIndex(d,Actions.Simple);Check(one>=0 && two>=0);
var s1=s0 with{Player=s0.Player with{Hand=h.Where((_,i)=>i!=one).ToArray(),Discard=[h[one]],Energy=2,Block=5}};
var discard2=s1.Player.Discard.Concat(s1.Player.Hand).ToArray();
var s2=s0 with{Position=s0.Position with{Round=2},Player=s0.Player with{Hand=d,Draw=[],Discard=discard2,Energy=3,Block=0}};
var s3=s2 with{Player=s2.Player with{Hand=d.Where((_,i)=>i!=two).ToArray(),Discard=discard2.Append(d[two]).ToArray(),Energy=2,Block=5}};
var all=s3.Player.Discard.Concat(s3.Player.Hand).ToArray();
var input=all.Select((c,i)=>new TraceCard(i+1,c)).ToArray();var reordered=input.Reverse().ToArray();
var after=Independent(arbitrary,9);
var s4=s3 with{Position=s3.Position with{Round=3},RunRng=Projection.Rng(seed,s3.RunRng.Entries.Select(e=>e.Key=="Shuffle"?after:e)),Player=s3.Player with{Hand=reordered.Take(5).Select(c=>c.Value).ToArray(),Draw=reordered.Skip(5).Select(c=>c.Value).ToArray(),Discard=[],Energy=3,Block=0}};
var states=new[]{s0,s1,s2,s3,s4}.Select(Projection.Freeze).ToArray();
var cards=h.Concat(d).ToArray();var qual=new ArmEligibility(true,["RELIC.BURNING_BLOOD",Eligibility.NeowRelic],[],true,cards.Select(c=>new DeckCardFact(c.Model,c.Upgrade,c.Upgraded,c.Persistent,true,c.Affliction is null,c.Replays,c.ExhaustNext,c.Keywords)).ToArray(),249,Neow(Eligibility.NeowRelic));
var cpBytes=Native(true,seed,cards);var predecessor=NativeCheckpoint.Parse(Native(false,seed,cards)).Facts;var cp=NativeCheckpoint.Parse(cpBytes).Facts;
var target=new Target(seed,s0.Player.Character,0,s0.Position.Row,s0.Position.Col,0,3,1);var binding=Contract.Seal(Contract.Arm(target,1,predecessor),predecessor,cp);
Check(cp.Row==target.Row && cp.Col==target.Col);
var trace=new ShuffleTrace(new(new(10,100),"Draw.Hand/NaturalShuffle",3,3,binding.PlayableFloor,s0.Position.Encounter,0,10,0),new(new(11,100),input,StreamSample.From(seed,arbitrary)),new(new(12,100),reordered,StreamSample.From(seed,after)),new(new(13,100),reordered),new(new(14,101),TaskStatus.RanToCompletion,TaskStatus.RanToCompletion));
var candidate=Json.Hash(File.ReadAllBytes(typeof(Probe).Assembly.Location));var attempt="00112233445566778899aabbccddeeff";
var arm=new ArmData(PrivateStore.Schema,PrivateStore.Revision,candidate,"synthetic-A",target,predecessor,qual);
var seal=new SealData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,binding);
var trajectory=new Trajectory(binding);trajectory.Capture(states[0]);var actions=new List<AcceptedAction>();
for(var i=0;i<4;i++){var round=Stages.ActionRound(i+1);var a=trajectory.Planned(round);trajectory.Requested(a,round,false);trajectory.Succeeded(a,true,true,false,false,round);actions.Add(a);trajectory.Capture(states[i+1]);}
var points=Enumerable.Range(0,5).Select(i=>new PointData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,"A",(Step)i,Json.Hash(states[i]),Window(),new(qual,true,Stages.RoundAt(i)))).ToArray();
var records=actions.Select(a=>new ActionData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,"A",a)).ToArray();
var shuffle=new ShuffleData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,attempt,cp.Hash,trace);
var complete=new CompleteData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,attempt,cp.Hash,states.Select(Json.Hash).ToArray(),points.Select(p=>Json.Hash(Json.Serialize(p))).ToArray(),records.Select(a=>Json.Hash(Json.Serialize(a))).ToArray(),Json.Hash(Json.Serialize(shuffle)));
var ready=new ReadyData(PrivateStore.Schema,PrivateStore.Revision,candidate,arm.Process,attempt,Json.Hash(Json.Serialize(complete)));
void Valid(ArmData? a=null,PointData[]? p=null,ActionData[]? acts=null,CompleteData? c=null,ReadyData? r=null,ShuffleData? sh=null,byte[]? current=null,string process="synthetic-B")=>AttemptValidation.BInput(a??arm,seal,p??points,acts??records,c??complete,r??ready,attempt,candidate,process,states,cpBytes,current??cpBytes,sh??shuffle);
Test("typed-seal-five-points-four-accepted-E0-E4-commit-ReadyB-freshB",()=>
{
 Valid();var store=new PrivateStore(Path.Combine(output,"synthetic-private"));store.Write(attempt,"arm.json",arm);store.Write(attempt,"seal.json",seal);store.WriteBytes(attempt,"checkpoint.private.json",cpBytes);store.Activate(attempt);
 for(var i=0;i<5;i++){store.WriteBytes(attempt,$"A.point-{i}.semantic.json",Encoding.UTF8.GetBytes(states[i]));store.Write(attempt,$"A.point-{i}.capture.json",points[i]);}
 for(var i=0;i<4;i++)store.Write(attempt,$"A.action-{i+1}.json",records[i]);store.Write(attempt,"A.shuffle.json",shuffle);store.Write(attempt,"A.complete.json",complete);store.Write(attempt,"A.ready-B.json",ready);
 Check(store.Active()==attempt && AttemptValidation.ReadyB(store,attempt));
 var b=new Trajectory(binding,states,actions.ToArray());Check(b.Capture(states[0]).Length==0);
 for(var i=0;i<4;i++){int round=Stages.ActionRound(i+1);var a=b.Planned(round);b.Dispatch(a,round);b.Requested(a,round,true);b.Succeeded(a,true,true,false,false,round);Check(b.Capture(states[i+1]).Length==0);}
 Check(b.Done && b.States.Length==5 && b.Accepted.Length==4);Reject(()=>b.Planned(3));Reject(()=>b.Dispatch(actions[3],2));
 store.Write(attempt,"B.claim.json",new{Process="synthetic-B"});Check(!AttemptValidation.ReadyB(store,attempt));
});
Test("actual-Stomp-is-not-eligible",()=>Reject(()=>Eligibility.Point(new(qual,true,1),qual,actual,0)));
Test("no-hardcoded-HP-or-gold",()=>{Eligibility.Arm(qual with{Gold=399});Eligibility.Point(new(qual,true,1),qual,s0 with{Player=s0.Player with{Hp=79}},0);});
foreach(var seq in new[]{3,4})
{
 var a=actions[seq-1];var state=Projection.Thaw(states[seq-1]);
 Test($"seq{seq}-correct-Round2-PlayerTurn2",()=>Actions.Validate(a,state,2));
 Test($"seq{seq}-wrong-round-turn",()=>Reject(()=>Actions.Validate(a with{Round=1,PlayerTurn=1},state,1)));
}
Test("round3-required-at-final-point",()=>Reject(()=>Contract.Playable(binding,s4 with{Position=s4.Position with{Round=2}},s0.Position.RoomModel,s0.Position.Encounter,3)));
Test("point-turn3-required",()=>Reject(()=>Eligibility.Point(new(qual,true,2),qual,s4,4)));
Test("four-operation-hard-cap",()=>{Reject(()=>Stages.ActionRound(5));Reject(()=>trajectory.Planned(3));});
Test("missing-FTUE-rejected-at-deserialization",()=>{var n=JsonNode.Parse(Json.Serialize(qual))!;n.AsObject().Remove("ShuffleFtueSeen");Reject(()=>Json.Read<ArmEligibility>(n.ToJsonString()));});
Test("false-FTUE",()=>Reject(()=>Eligibility.Arm(qual with{ShuffleFtueSeen=false})));
Test("missing-relics",()=>Reject(()=>Eligibility.Arm(qual with{Relics=null!})));
Test("unlisted-relic",()=>Reject(()=>Eligibility.Arm(qual with{Relics=["RELIC.BURNING_BLOOD","RELIC.CURSED_PEARL"]})));
Test("run-modifier",()=>Reject(()=>Eligibility.Arm(qual with{RunModifiers=["MODIFIER.TEST"]})));
Test("debug-forced-top",()=>Reject(()=>Eligibility.Arm(qual with{DebugTopAbsent=false})));
Test("PlayPile-nonempty",()=>Reject(()=>Eligibility.Point(new(qual,false,1),qual,s0,0)));
Test("missing-required-projection",()=>{var n=JsonNode.Parse(states[0])!;n["Player"]!.AsObject().Remove("Hp");Reject(()=>Projection.Thaw(n.ToJsonString()));});
Test("no-reshuffle-artifact",()=>Reject(()=>Valid(sh:shuffle with{Trace=null!})));
Test("initial-shuffle-wrong-origin",()=>Reject(()=>TraceContract.Complete(trace with{E0=trace.E0 with{Origin="InitialPopulate"}},binding,s0.Position.Encounter)));
Test("wrong-owner",()=>Reject(()=>ShuffleOracle.Validate(trace.E1.Rng with{Owner="Player"},trace.E2.Rng)));
Test("wrong-stream",()=>Reject(()=>ShuffleOracle.Validate(trace.E1.Rng with{Entry=arbitrary with{Key="CombatCardGeneration"}},trace.E2.Rng)));
Test("wrong-local-member",()=>Reject(()=>TraceContract.Complete(trace with{E2=trace.E2 with{Cards=reordered.Select((c,i)=>i==0?c with{LocalToken=999}:c).ToArray()}},binding,s0.Position.Encounter)));
Test("same-sort-key-distinct-full-fingerprint",()=>Reject(()=>TraceContract.Input(trace.E1 with{Cards=input.Select((c,i)=>i==Array.FindIndex(all,Actions.Simple)?c with{Value=c.Value with{EffectiveCost=2}}:c).ToArray()})));
// Independent state oracle uses BigInteger masking/shift arithmetic, not candidate implementation.
Test("nine-full-state-steps-from-counter46",()=>{Check(arbitrary.Counter==46 && after.Counter==55);ShuffleOracle.Validate(trace.E1.Rng,trace.E2.Rng);Check(ShuffleOracle.Advance(arbitrary,9)==after);});
Test("known-independent-state-vector",()=>Check(Independent(new("Shuffle",0,1,2,3,4),1)==new RngEntry("Shuffle",1,7,0,262146,211106232532992)));
Test("counter-plus9-wrong-state-consistent-digest",()=>Reject(()=>ShuffleOracle.Validate(trace.E1.Rng,StreamSample.From(seed,after with{State2=after.State2^1}))));
Test("extra-consumption",()=>Reject(()=>ShuffleOracle.Validate(trace.E1.Rng,StreamSample.From(seed,Independent(arbitrary,10)))));
Test("stale-digest",()=>Reject(()=>(trace.E2.Rng with{Digest="bad"}).Validate()));
Test("same-millisecond-events-accepted",()=>TraceContract.Complete(trace,binding,s0.Position.Encounter));
Test("reverse-event-sequence",()=>Reject(()=>TraceContract.Complete(trace with{E2=trace.E2 with{Stamp=new(10,100)}},binding,s0.Position.Encounter)));
Test("decreasing-monotonic-time",()=>Reject(()=>TraceContract.Complete(trace with{E2=trace.E2 with{Stamp=new(12,99)}},binding,s0.Position.Encounter)));
Test("cross-process-local-tokens-normalize",()=>{TraceCard[] Shift(TraceCard[] c)=>c.Select(t=>t with{LocalToken=t.LocalToken+1000}).ToArray();var other=trace with{E1=trace.E1 with{Cards=Shift(input)},E2=trace.E2 with{Cards=Shift(reordered)},E3=trace.E3 with{Cards=Shift(reordered)}};TraceContract.Complete(other,binding,s0.Position.Encounter);Check(TraceContract.Normalize(trace)==TraceContract.Normalize(other));});
foreach(var status in new[]{TaskStatus.WaitingForActivation,TaskStatus.Faulted,TaskStatus.Canceled})Test("trace-original-task-"+status,()=>Reject(()=>TraceContract.Complete(trace with{E4=trace.E4 with{Draw=status}},binding,s0.Position.Encounter)));
Test("original-task-scope-pending-then-success",()=>{var scope=new OriginalTaskScope();var pending=new TaskCompletionSource();scope.BindDraw(pending.Task);scope.BindShuffle(Task.CompletedTask);Check(!scope.FinishedSuccessfully());pending.SetResult();Check(scope.FinishedSuccessfully());scope.Close();Reject(()=>scope.FinishedSuccessfully());});
Test("original-task-fault-preserved",()=>{var task=Task.FromException(new InvalidOperationException());var scope=new OriginalTaskScope();scope.BindDraw(task);scope.BindShuffle(Task.CompletedTask);Reject(()=>scope.FinishedSuccessfully());Check(task.IsFaulted);scope.Close();});
Test("original-task-cancel-preserved",()=>{var task=Task.FromCanceled(new CancellationToken(true));var scope=new OriginalTaskScope();scope.BindDraw(task);scope.BindShuffle(Task.CompletedTask);Reject(()=>scope.FinishedSuccessfully());Check(task.IsCanceled);scope.Close();});
Test("task-offthread-blocked-before-sampling",()=>{var scope=new OriginalTaskScope();Task.Run(()=>Reject(()=>scope.LastOnGameThread())).GetAwaiter().GetResult();scope.Close();});
Test("scope-nesting-leak-cleanup",()=>{var slot=new ScopeSlot<object>();slot.Bind(new());Reject(()=>slot.Bind(new()));slot.Clear();slot.Bind(new());slot.Clear();Check(slot.Current is null);});
Test("closed-generic-actual-CardModel-list-owner-guard",()=>{var owner=new object();var list=new List<CardModel>();Check(ReferenceScope.Matches<CardModel>(list,owner,owner));Check(!ReferenceScope.Matches<CardModel>(new List<string>(),owner,owner));Check(!ReferenceScope.Matches<CardModel>(list,new object(),owner));Check(!ReferenceScope.Matches<CardModel>(list,owner,owner,new List<CardModel>()));});
Test("boundary-first-difference-frozen-inflight-explicit",()=>{var ledger=new BoundaryLedger(trace);var values=BoundaryLedger.Values(trace);Check(ledger.Observe("E0",values[0],new(20,200),4,4,4,(null,null)));Check(!ledger.Observe("E1","different",new(21,200),4,4,4,(null,null)));var first=Json.Serialize(ledger.Difference);Check(!ledger.Observe("E2","later",new(22,200),5,4,4,(TaskStatus.RanToCompletion,TaskStatus.RanToCompletion)));Check(first==Json.Serialize(ledger.Difference) && ledger.Difference!.StablePoints==4 && ledger.Difference.OriginalDrawStatus=="UnknownNotReturned" && ledger.Count==2);Save("early-boundary-difference.json",ledger.Difference);});
Test("all-normalized-boundaries-compare",()=>{var ledger=new BoundaryLedger(trace);var vals=BoundaryLedger.Values(trace);for(var i=0;i<5;i++)Check(ledger.Observe("E"+i,vals[i],new(100+i,1000),4,4,4,(null,null)));Check(ledger.Count==5 && ledger.Difference is null);Reject(()=>ledger.Observe("E5","x",new(105,1000),4,4,4,(null,null)));});
for(var point=0;point<5;point++){int p=point;Test("stable-difference-stops-next-dispatch-at-"+p,()=>{var b=new Trajectory(binding,states,actions.ToArray());for(var i=0;i<=p;i++){var ss=Projection.Thaw(states[i]);var diff=b.Capture(i==p?Projection.Freeze(ss with{Player=ss.Player with{Hp=ss.Player.Hp-1}}):states[i]);if(i==p){Check(diff.Length>0 && b.Done && b.Accepted.Length==p);Reject(()=>b.Planned(Stages.RoundAt(p)));break;}var a=b.Planned(Stages.ActionRound(i+1));b.Dispatch(a,a.PlayerTurn);b.Requested(a,a.PlayerTurn,true);b.Succeeded(a,true,true,false,false,a.PlayerTurn);}});}
Test("fresh-process-required",()=>Reject(()=>Valid(process:arm.Process)));
Test("changed-CP-rejected",()=>Reject(()=>Valid(current:cpBytes.Append((byte)32).ToArray())));
Test("wrong-candidate-rejected",()=>Reject(()=>Valid(a:arm with{CandidateHash=new string('F',64)})));
Test("wrong-ready-hash",()=>Reject(()=>Valid(r:ready with{CompleteHash="bad"})));
Test("missing-fifth-capture",()=>Reject(()=>Valid(p:points.Take(4).ToArray())));
Test("wrong-shuffle-hash",()=>Reject(()=>Valid(c:complete with{ShuffleHash="bad"})));
Test("checkpoint-deck-qualification-bound",()=>{var parsed=NativeCheckpoint.Parse(cpBytes).Save;parsed.Players[0].Deck.RemoveAt(0);Reject(()=>NativeCheckpoint.MatchArm(parsed,qual));});
Test("four-action-observation-sequences-and-cap",()=>{var clock=new Clock();var f=Start(clock,_=>{});var observer=new ActionObservation(f,_=>{});foreach(var a in actions){Check(observer.Binding(a)&&observer.Requested(a)&&observer.Executed());Check(a.Type==Operation.CardPlay?observer.CardEffect(TaskStatus.RanToCompletion):observer.EndReady());Check(observer.Finished(a.Type==Operation.CardPlay?TaskStatus.RanToCompletion:null,false));}Check(f.Stage!=Stage.Terminal && f.ActionDiagnostics!.Sequence==4 && f.ActionDiagnostics.ObservationIndex==20);});
Test("adjacent30-20fields-gap250-Flow",()=>{var clock=new Clock();var events=new List<Det001.Core.Event>();var f=Start(clock,events.Add);for(var i=0;i<30;i++){clock.Now=i*10;Check(f.Tick(i,Fields(),"identity")== (i==29));}AttemptValidation.Window(f.Window);Check(f.Captured());Check(events.Select(e=>e.Sequence).SequenceEqual(Enumerable.Range(1,events.Count).Select(i=>(long)i)));});
Test("gap-breaks-window",()=>{var clock=new Clock();var f=Start(clock,_=>{});f.Tick(1,Fields(),"x");clock.Now=251;f.Tick(2,Fields(),"x");Check(f.Diagnostics!.Consecutive==1);});
Test("evidence-write-failure-terminal-no-ready",()=>{var clock=new Clock();var f=new Flow(clock,_=>throw new IOException());Check(!f.FirstFrame(false) && f.Stage==Stage.Terminal);Check(!f.Complete(()=>throw new Exception()));});
Test("terminal-wins-completion-race",()=>{var clock=new Clock();var f=Start(clock,_=>{});for(var i=0;i<30;i++)f.Tick(i,Fields(),"x");Check(f.Captured());f.Fail("first");bool committed=false;Check(!f.Complete(()=>committed=true) && !committed && f.TerminalReason=="first");});
Test("terminal-wins-owned-dispatch-race",()=>{var clock=new Clock();var f=Start(clock,_=>{});for(var i=0;i<30;i++)f.Tick(i,Fields(),"x");Check(f.Captured());f.Fail("first");bool dispatched=false;Check(!f.RunDispatch(()=>dispatched=true) && !dispatched);});
Test("ready-write-failure-no-publication",()=>{var clock=new Clock();var f=Start(clock,_=>{});for(var i=0;i<30;i++)f.Tick(i,Fields(),"x");Check(f.Captured());Check(!f.Complete(()=>{},()=>throw new IOException()) && f.Stage==Stage.Terminal);});
Test("capture-deadline-no-late-commit",()=>{var clock=new Clock();var f=Start(clock,_=>{});clock.Now=20000;Check(!f.Poll() && f.Stage==Stage.Terminal);});
Test("real-production-patch-unpatch-metadata-only",PatchSmoke);
Test("real-Harmony-dummy-Task-exception-generic-isolation",DummySmoke);
Test("actual-finalizers-preserve-exact-exception",()=>{var ex=new InvalidOperationException("identity");foreach(var name in new[]{"DrawObservationPatch","NaturalShuffleObservationPatch"}){var method=typeof(Probe).Assembly.GetType("Det001."+name)!.GetMethod("Finalizer",BindingFlags.NonPublic|BindingFlags.Static)!;Check(ReferenceEquals(ex,method.Invoke(null,[null,ex])));Check(method.Invoke(null,[null,null]) is null);}});
Test("r2-actual-reference-and-value-utility-isolation",CallSiteRegression.ActualUtility);
Test("r2-real-callsite-dummy-original-context-result-owner",CallSiteRegression.DummyNormal);
Test("r2-real-callsite-dummy-throwing-observers-and-original-fault",CallSiteRegression.DummyFaults);
Test("r2-callsite-original-IL-and-rejected-shapes",CallSiteRegression.Instructions);
Test("preparation-rejection-is-distinct",()=>{try{Eligibility.Arm(qual with{ShuffleFtueSeen=false});throw new Exception();}catch(PreparationException e){Check(e.Code=="eligibility-ftue-debug-gold");}});
foreach(var id in new[]{Eligibility.NeowRelic,"RELIC.SILKEN_TRESS","RELIC.BOOMING_CONCH","RELIC.LAVA_ROCK","RELIC.WINGED_BOOTS","RELIC.FISHING_ROD"})
{
    var q=qual with{Relics=["RELIC.BURNING_BLOOD",id],Gold=id=="RELIC.SILKEN_TRESS"?0:99,Neow=Neow(id)};
    var bytes=Native(true,seed,cards,id,q.Gold);var prior=NativeCheckpoint.Parse(Native(false,seed,cards,id,q.Gold)).Facts;
    var bound=Contract.Seal(Contract.Arm(target,1,prior),prior,NativeCheckpoint.Parse(bytes).Facts);
    var ar=arm with{Predecessor=prior,Eligibility=q};var se=seal with{Binding=bound};
    var ps=points.Select(p=>p with{Eligibility=p.Eligibility with{Arm=q}}).ToArray();
    var sh=shuffle with{CheckpointHash=bound.Checkpoint.Hash};
    var co=complete with{CheckpointHash=bound.Checkpoint.Hash,CaptureHashes=ps.Select(p=>Json.Hash(Json.Serialize(p))).ToArray(),ShuffleHash=Json.Hash(Json.Serialize(sh))};
    var re=ready with{CompleteHash=Json.Hash(Json.Serialize(co))};
    Test(id+"-full-typed-qualification-seal-5points-4actions-shuffle-ready-freshB",()=>
    {
        Eligibility.Arm(Json.Read<ArmEligibility>(Json.Serialize(q)));
        AttemptValidation.BInput(ar,se,ps,records,co,re,attempt,candidate,"synthetic-fresh-B",states,bytes,bytes,sh);
        var b=new Trajectory(bound,states,actions.ToArray());Check(b.Capture(states[0]).Length==0);
        for(var i=0;i<4;i++){var a=b.Planned(Stages.ActionRound(i+1));b.Dispatch(a,a.PlayerTurn);b.Requested(a,a.PlayerTurn,true);b.Succeeded(a,true,true,false,false,a.PlayerTurn);Check(b.Capture(states[i+1]).Length==0);}
        Check(b.Done && b.Accepted.Length==4);Reject(()=>b.Planned(3));
        Save(id.Replace('.','-')+"-synthetic-chain.json",new{Classification="SYNTHETIC_NOT_RUNTIME",Arm=ar,Seal=se,Points=ps,Actions=records,Shuffle=sh,Complete=co,Ready=re});
    });
    Test(id+"-wrong-sidecar-id",()=>Reject(()=>Eligibility.Arm(q with{Neow=q.Neow with{Model="RELIC.UNKNOWN"}})));
    Test(id+"-mixed-extra-relic",()=>Reject(()=>Eligibility.Arm(q with{Relics=q.Relics.Append("RELIC.LAVA_ROCK").ToArray()})));
    Test(id+"-two-Neow-without-BurningBlood",()=>Reject(()=>Eligibility.Arm(q with{Relics=[id,id]})));
    Test(id+"-unknown-relic",()=>Reject(()=>Eligibility.Arm(q with{Relics=["RELIC.BURNING_BLOOD","RELIC.UNKNOWN"]})));
    Test(id+"-checkpoint-other-id",()=>{var parsed=NativeCheckpoint.Parse(bytes).Save;parsed.Players[0].Relics[1].Id=new("RELIC","UNKNOWN");Reject(()=>NativeCheckpoint.MatchArm(parsed,q));});
    Test(id+"-checkpoint-instance-state-binding",()=>{var parsed=NativeCheckpoint.Parse(bytes).Save;parsed.Players[0].Relics[1].FloorAddedToDeck=99;Reject(()=>NativeCheckpoint.MatchArm(parsed,q));});
    Test(id+"-sidecar-persistent-id",()=>Reject(()=>Eligibility.Arm(q with{Neow=q.Neow with{Persistent=q.Neow.Persistent.Replace(id,"RELIC.UNKNOWN",StringComparison.Ordinal)}})));
    Test(id+"-used-counter-or-flag",()=>Reject(()=>Eligibility.Arm(q with{Neow=q.Neow.Counter.HasValue?q.Neow with{Counter=1}:q.Neow with{Flag=true}})));
    Test(id+"-persistent-props-cannot-lie",()=>
    {
        var n=JsonNode.Parse(q.Neow.Persistent)!;n["props"]=JsonNode.Parse("{\"ints\":[{\"name\":\"TimesUsed\",\"value\":1}]}");
        Reject(()=>Eligibility.Arm(q with{Neow=q.Neow with{Persistent=n.ToJsonString()}}));
    });
    Test(id+"-point-relic-instance-changed",()=>
    {
        var n=JsonNode.Parse(q.Neow.Persistent)!;n["floor_added_to_deck"]=99;
        Reject(()=>Eligibility.Point(new(q with{Neow=q.Neow with{Persistent=n.ToJsonString()}},true,1),q,s0,0));
    });
}
Test("r3-missing-Neow-field",()=>{var n=JsonNode.Parse(Json.Serialize(qual))!;n.AsObject().Remove("Neow");Reject(()=>Json.Read<ArmEligibility>(n.ToJsonString()));});
foreach(var field in new[]{"Model","Counter","Flag","Persistent"})Test("r3-required-Neow-"+field,()=>{var n=JsonNode.Parse(Json.Serialize(qual))!;n["Neow"]!.AsObject().Remove(field);Reject(()=>Json.Read<ArmEligibility>(n.ToJsonString()));});
Test("r3-null-Neow",()=>Reject(()=>Eligibility.Arm(qual with{Neow=null!})));
Save("synthetic-chain.json",new{Classification="INDEPENDENT_SYNTHETIC_NOT_RUNTIME",PublicInputHash=Json.Hash(source),Candidate=candidate,Arm=arm,Seal=seal,Points=points,Actions=records,Shuffle=shuffle,Complete=complete,Ready=ready,SnapshotHashes=states.Select(Json.Hash),CounterBefore=arbitrary.Counter,CounterAfter=after.Counter});
Save("summary.json",new{Classification="OFFLINE_ONLY",Passed=count,Cases=names,GameStarted=false,CurrentAccountRead=false,RuntimeAttempt=0});
Check(Json.Hash(File.ReadAllText(publicPath))==Json.Hash(source));Console.WriteLine($"PASS TOTAL {count}; actual runtime effects NOT invoked");

static void Check(bool value){if(!value)throw new Exception("assertion");}
static void Reject(Action action){try{action();}catch(Exception e) when(e is InvalidDataException or System.Text.Json.JsonException or PreparationException){return;}throw new Exception("expected-rejection");}
static Field[] Fields()=>Flow.FieldNames.Select(n=>new Field(n,FieldStatus.True)).ToArray();
static QuietWindow Window()=>new(1,30,0,290,30,10,Fields());
static Flow Start(Clock clock,Action<Det001.Core.Event> write){var f=new Flow(clock,write);Check(f.FirstFrame(false)&&f.Arm()&&f.SaveStarted()&&f.BeginSeal()&&f.Sealed());return f;}
static RngEntry Independent(RngEntry e,int n)
{BigInteger mask=(BigInteger.One<<64)-1,a=e.State0,b=e.State1,c=e.State2,d=e.State3;for(int i=0;i<n;i++){var shift=(b<<17)&mask;c^=a;d^=b;b^=c;a^=d;c^=shift;d=((d<<45)|(d>>19))&mask;}return new(e.Key,e.Counter+n,(ulong)a,(ulong)b,(ulong)c,(ulong)d);}
static NeowState Neow(string id)
{
    int? counter=id is "RELIC.WINGED_BOOTS" or "RELIC.FISHING_ROD"?0:null;
    bool? flag=id is "RELIC.SILKEN_TRESS" or "RELIC.LAVA_ROCK"?false:null;
    return new(id,counter,flag,Json.Canonical(System.Text.Json.JsonSerializer.Serialize(Relic(id),JsonSerializationUtility.Options)));
}
static SerializableRelic Relic(string id)
{
    var r=new SerializableRelic{Id=new("RELIC",id.Split('.')[1])};
    if(id is "RELIC.WINGED_BOOTS" or "RELIC.FISHING_ROD")r.Props=new(){ints=[new(id=="RELIC.WINGED_BOOTS"?"TimesUsed":"CombatsSeen",0)]};
    if(id is "RELIC.SILKEN_TRESS" or "RELIC.LAVA_ROCK")r.Props=new(){bools=[new(id=="RELIC.SILKEN_TRESS"?"IsUsed":"HasTriggered",false)]};
    return r;
}
static byte[] Native(bool roomBefore,string seed,Card[] deck,string neow=Eligibility.NeowRelic,int gold=249)
{
 var history=new MapPointHistoryEntry{MapPointType=MapPointType.Ancient,PlayerStats=[new(){PlayerId=0,CurrentHp=80,MaxHp=80}],Rooms=[new(){RoomType=RoomType.Event}]};
 var save=new SerializableRun{SchemaVersion=1,CurrentActIndex=0,Players=[new(){CharacterId=new ModelId("CHARACTER","IRONCLAD"),CurrentHp=80,MaxHp=80,MaxEnergy=3,Gold=gold,Relics=[Relic("RELIC.BURNING_BLOOD"),Relic(neow)],Rng=new(){Seed=42,Rngs=Enum.GetValues<PlayerRngType>().ToDictionary(x=>x,_=>new SerializableRng{counter=1,state0=1,state1=2,state2=3,state3=4})},Deck=deck.Select(c=>System.Text.Json.JsonSerializer.Deserialize<MegaCrit.Sts2.Core.Saves.Runs.SerializableCard>(c.Persistent,JsonSerializationUtility.Options)!).ToList()}],SerializableRng=new(){Seed=seed,Rngs=Enum.GetValues<RunRngType>().ToDictionary(x=>x,_=>new SerializableRng{counter=2,state0=1,state1=2,state2=3,state3=4})},VisitedMapCoords=roomBefore?[new(3,0),new(1,1)]:[new(3,0)],MapPointHistory=[[history]],PreFinishedRoom=roomBefore?null:new(){RoomType=RoomType.Event,EventId=new("EVENT","NEOW"),IsPreFinished=true},GameMode=GameMode.Standard};return Encoding.UTF8.GetBytes(JsonSerializationUtility.ToJson(save));
}
static void PatchSmoke()
{var h=new Harmony("DET001.offline.actual-targets");try{h.PatchAll(typeof(Probe).Assembly);var targets=h.GetPatchedMethods().ToArray();foreach(var m in targets)Console.WriteLine("PATCHED "+m);Check(targets.Length==11 && targets.All(m=>m.Name!="StableShuffle"));Check(targets.Count(m=>m.Name=="MoveNext" && m.DeclaringType!.Name.StartsWith("<Shuffle>"))==1);}finally{h.UnpatchAll(h.Id);}Check(!h.GetPatchedMethods().Any());}
static void DummySmoke()
{
 var flow=typeof(Probe).GetField("_flow",BindingFlags.NonPublic|BindingFlags.Static)!;var announced=typeof(Probe).GetField("_announcedTerminal",BindingFlags.NonPublic|BindingFlags.Static)!;
 flow.SetValue(null,new Flow(new Clock(),_=>{}));announced.SetValue(null,1);
 var h=new Harmony("DET001.offline.same-shape");
 h.Patch(AccessTools.Method(typeof(Dummy),nameof(Dummy.Draw)),prefix:new HarmonyMethod(typeof(DummyPatch),nameof(DummyPatch.Prefix)),postfix:new HarmonyMethod(typeof(DummyPatch),nameof(DummyPatch.Postfix)),finalizer:new HarmonyMethod(typeof(DummyPatch),nameof(DummyPatch.Finalizer)));
 var generic=typeof(Dummy).GetMethod(nameof(Dummy.Stable))!.MakeGenericMethod(typeof(DummyCard));h.Patch(generic,prefix:new HarmonyMethod(typeof(DummyPatch),nameof(DummyPatch.GenericPrefix)));
 try
 {
  var fault=new InvalidOperationException("dummy");var pending=new TaskCompletionSource();
  foreach(var task in new[]{Task.CompletedTask,pending.Task,Task.FromException(fault),Task.FromCanceled(new CancellationToken(true))}){Dummy.Original=task;Dummy.Throw=null;var result=Dummy.Draw(37);Check(ReferenceEquals(result,task)&&Dummy.Argument==37);}
  Dummy.Throw=fault;try{Dummy.Draw(19);throw new Exception("no-fault");}catch(InvalidOperationException e){Check(ReferenceEquals(e,fault));}Dummy.Throw=null;
  var owner=new object();DummyPatch.Owner=owner;DummyPatch.Seen=0;Dummy.Stable(new List<DummyCard>(),owner);Check(DummyPatch.Seen==1);Dummy.Stable(new List<string>(),owner);Dummy.Stable(new List<DummyCard>(),new object());Check(DummyPatch.Seen==1);
 }
 finally{h.UnpatchAll(h.Id);flow.SetValue(null,null);}Check(!h.GetPatchedMethods().Any());
}
sealed class Clock:IClock{public long Now;public long Milliseconds=>Now;}
sealed class DummyCard{}
static class Dummy
{public static Task Original=Task.CompletedTask;public static Exception? Throw;public static int Argument;[MethodImpl(MethodImplOptions.NoInlining)]public static Task Draw(int value){Argument=value;if(Throw is not null)throw Throw;return Original;}[MethodImpl(MethodImplOptions.NoInlining)]public static void Stable<T>(List<T> list,object owner){GC.KeepAlive(list);GC.KeepAlive(owner);}}
static class DummyPatch
{public static object? Owner;public static int Seen;public static void Prefix()=>typeof(Probe).GetMethod("Safe",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[new Action(()=>throw new IOException())]);public static void Postfix(Task __result)=>Prefix();public static Exception? Finalizer(Exception? __exception){Prefix();return __exception;}public static void GenericPrefix(object __0,object __1){if(ReferenceScope.Matches<DummyCard>(__0,__1,Owner!))Seen++;}}
