using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Det001;
using Spike001B.Core;
using OldJson=Spike001B.Core.Json;

if(args.Length!=2)throw new ArgumentException("usage: Det001Offline <workspace> <new-output-directory>");
var workspace=Path.GetFullPath(args[0]);var output=Path.GetFullPath(args[1]);
if(!output.StartsWith(Path.Combine(workspace,"evidence","det-001")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || Directory.Exists(output))throw new InvalidDataException("new-output-scope");
Directory.CreateDirectory(output);
const string approved="F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7";
var comparator=typeof(Projection).Assembly.Location;
Assert(Hash(File.ReadAllBytes(comparator))==approved,"comparator-identity");
var records=Path.Combine(workspace,"evidence","spike-001b","runtime","records-4f958fdc207f4b9ba83ede826848f9ed");
string[] golden=["BBBD2D4C1B71FCB0CBF8747224717A888137C377569ACD654074BF54E58E8E08","0C61F1495F3139352A2ECB13B9FA01AA8E78B01B007141B1C1907E297C850D64","E37A7DC38A021526253415C28B832165A6916962269C55F0125113B096D44023"];
var inputFiles=(from phase in new[]{"A","B"} from i in Enumerable.Range(0,3) select Path.Combine(records,$"{phase}.point-{i}.semantic.json")).ToArray();
var inputHashes=inputFiles.ToDictionary(p=>p,p=>Hash(File.ReadAllBytes(p)));
var original=File.ReadAllText(inputFiles[0]);var actualCases=new List<object>();
for(var i=0;i<3;i++)
{
    var a=File.ReadAllText(inputFiles[i]);var b=File.ReadAllText(inputFiles[i+3]);
    Assert(Hash(Encoding.UTF8.GetBytes(a))==golden[i] && Hash(Encoding.UTF8.GetBytes(b))==golden[i],"public-fixture-hash");
    Check($"actual-public-equal-point-{i}",Wrap(a),Wrap(b),"Equal",[]);
}
var expectedNode=JsonNode.Parse(original)!;
var hp=Clone();hp["Player"]!["Hp"]=79;
Mutation("single-hp",hp,["$.Player.Hp"],"Hp");
var swapped=Clone();var hand=swapped["Player"]!["Hand"]!.AsArray();
var first=hand[0]!.DeepClone();var second=hand[1]!.DeepClone();
var fingerprint0=Hash(Encoding.UTF8.GetBytes(Canonical(first)));var fingerprint1=Hash(Encoding.UTF8.GetBytes(Canonical(second)));
Assert(fingerprint0!=fingerprint1,"swap-different-full-fingerprints");
hand[0]=second;hand[1]=first;
var swapPaths=(from index in new[]{0,1} from field in new[]{"Model","Persistent","Variables[0].Base","Variables[0].Enchanted","Variables[0].Name"} select $"$.Player.Hand[{index}].{field}").ToArray();
Mutation("ordered-full-card-swap-distinct-fingerprints",swapped,swapPaths,"Hand");
var property=Clone();property["Player"]!["Hand"]![1]!["Variables"]![0]!["Enchanted"]=4;
Mutation("single-effective-card-block-variable",property,["$.Player.Hand[1].Variables[0].Enchanted"],"Hand");
var rng=Clone();var entries=rng["RunRng"]!["Entries"]!.AsArray();var shuffleIndex=entries.Select((v,i)=>(v,i)).Single(x=>x.v!["Key"]!.GetValue<string>()=="Shuffle").i;
var entry=entries[shuffleIndex]!;
var state=new RandomState(entry["State0"]!.GetValue<ulong>(),entry["State1"]!.GetValue<ulong>(),entry["State2"]!.GetValue<ulong>(),entry["State3"]!.GetValue<ulong>());
var next=state.Next();entry["Counter"]=entry["Counter"]!.GetValue<int>()+1;
entry["State0"]=next.State.S0;entry["State1"]=next.State.S1;entry["State2"]=next.State.S2;entry["State3"]=next.State.S3;
var rngSet=rng["RunRng"]!;var digestInput=new JsonObject{["Seed"]=rngSet["Seed"]!.DeepClone(),["Entries"]=rngSet["Entries"]!.DeepClone()};
rngSet["Digest"]=Hash(Encoding.UTF8.GetBytes(Canonical(digestInput)));
var rngPaths=new[]{"Counter","State0","State1","State2","State3"}.Select(k=>$"$.RunRng.Entries[{shuffleIndex}].{k}").Append("$.RunRng.Digest").ToArray();
Mutation("legal-projected-rng-state-counter-and-consistent-digest",rng,rngPaths,"RunRng");
Save("valid-rng-mutation.json",new {Source="INDEPENDENT_SYNTHETIC_MUTANT_NOT_RUNTIME_RESHUFFLE",Stream="Run.Shuffle",Before=state,After=next.State,NextUnsignedLongOutput=next.Value,BeforeCounter=9,AfterCounter=10,RecomputedDigest=rngSet["Digest"]!.GetValue<string>(),ExpectedDifferencePaths=rngPaths});
var missing=Clone();missing["Player"]!.AsObject().Remove("Hp");Check("missing-required-hp",Wrap(original),Wrap(missing.ToJsonString()),"InputRejected",[]);
var wrongSchema=new SnapshotEnvelope(99,SnapshotEnvelope.Revision,original);Check("wrong-envelope-schema",Wrap(original),JsonSerializer.Serialize(wrongSchema),"InputRejected",[]);
var badDigest=Clone();badDigest["RunRng"]!["Entries"]![shuffleIndex]!["Counter"]=10;Check("counter-mutant-with-stale-digest-is-corruption",Wrap(original),Wrap(badDigest.ToJsonString()),"InputRejected",[]);
var missingState=Clone();missingState["RunRng"]!["Entries"]![shuffleIndex]!.AsObject().Remove("State2");Check("missing-required-rng-state",Wrap(original),Wrap(missingState.ToJsonString()),"InputRejected",[]);
Assert(expectedNode.ToJsonString()==JsonNode.Parse(original)!.ToJsonString(),"expected-not-mutated");
var vector=new RandomState(1,2,3,4).Next();Assert(vector.Value==11520 && vector.State==new RandomState(7,0,262146,211106232532992),"independent-known-state-vector");
// Existing actual fixture has a Neow replacement Stomp, so it is NOT presented
// as an eligible unchanged starter. Build the plan-positive independently.
var realPlan=StarterPreparation.Plan(Projection.Thaw(original));Assert(realPlan.Classification=="FIXTURE_NOT_PLAIN_TEN_CARD_STARTER","actual-fixture-not-mislabelled");
var plain=Clone();plain["Player"]!["Hand"]![3]=plain["Player"]!["Hand"]![0]!.DeepClone();
var plainState=Projection.Thaw(plain.ToJsonString());var plan=StarterPreparation.Plan(plainState);
Assert(plan==new StarterPlan("SYNTHETIC_PREPARATION_ELIGIBLE",3,2,4,5),"derive-first-reshuffle-and-bounds");
var noSecondDefend=JsonNode.Parse(plain.ToJsonString())!;
foreach(var pair in new[]{(Hand:0,Draw:0),(Hand:2,Draw:2),(Hand:3,Draw:4)}){var card=noSecondDefend["Player"]!["Hand"]![pair.Hand]!.DeepClone();noSecondDefend["Player"]!["Hand"]![pair.Hand]=noSecondDefend["Player"]!["Draw"]![pair.Draw]!.DeepClone();noSecondDefend["Player"]!["Draw"]![pair.Draw]=card;}
Assert(StarterPreparation.Plan(Projection.Thaw(noSecondDefend.ToJsonString())).Classification=="FIXTURE_DEFEND_DISTRIBUTION","no-round2-defend-stop");
Assert(StarterPreparation.Plan(plainState,false).Classification=="FIXTURE_UNSUPPORTED_PASSIVE_INPUT" && StarterPreparation.Plan(plainState,true,false).Classification=="FIXTURE_FTUE_NOT_READY","passive-and-FTUE-stops");
var distinctTie=JsonNode.Parse(plain.ToJsonString())!;distinctTie["Player"]!["Hand"]![1]!["Variables"]![0]!["Enchanted"]=4;
Assert(StarterPreparation.Plan(Projection.Thaw(distinctTie.ToJsonString())).Classification=="FIXTURE_DISTINCT_SORT_TIE","different-fingerprints-same-sort-key-rejected");
Save("preparation-cases.json",new{Source="ACTUAL_PUBLIC_FIXTURE_ADMISSIBILITY_AND_INDEPENDENT_SYNTHETIC_STARTER",Actual001B=realPlan,SyntheticPlainStarter=plan,SyntheticMutation="Replace Stomp with independently copied full ordinary Strike; no source file changes",NoRound2Defend="FIXTURE_DEFEND_DISTRIBUTION",UnsupportedPassives="FIXTURE_UNSUPPORTED_PASSIVE_INPUT",UnseenFTUE="FIXTURE_FTUE_NOT_READY",DistinctFingerprintSortTie="FIXTURE_DISTINCT_SORT_TIE"});
var flowCases=new List<object>();var baseline=new ReshuffleEvidence();
Flow("full-synthetic-contract",baseline,"SYNTHETIC_CONTRACT_READY_NOT_RUNTIME_PASS");
Flow("no-natural-event",baseline with{Natural=false},"TARGET_RESHUFFLE_NOT_PROVEN");
Flow("initial-shuffle-only",baseline with{InitialShuffle=true},"TARGET_RESHUFFLE_NOT_PROVEN");
Flow("wrong-stream",baseline with{Stream="CombatCardGeneration"},"RNG_ATTRIBUTION_REJECTED");
Flow("interleaved-target-rng-use",baseline with{NoOtherTargetRngUse=false},"RNG_ATTRIBUTION_REJECTED");
Flow("before-after-order-invalid",baseline with{RandomReturned=99},"SAMPLE_ORDER_REJECTED");
Flow("wrong-round",baseline with{Round=2},"PHASE_IDENTITY_REJECTED");
Flow("wrong-player-turn",baseline with{PlayerTurn=4},"PHASE_IDENTITY_REJECTED");
Flow("floor-not-derived-from-checkpoint",baseline with{Floor=1},"PHASE_IDENTITY_REJECTED");
Flow("not-settled",baseline with{AdjacentFrames=29},"NOT_SETTLED_OR_COMBAT_ENDED");
Flow("target-capture-timeout",baseline with{DrawStable=20100},"TARGET_CAPTURE_DEADLINE");
Flow("missing-original-draw-completion",baseline with{OriginalDrawTaskSucceeded=false},"ORIGINAL_COMPLETION_NOT_PROVEN");
Flow("no-rng-advance",baseline with{CounterAfter=9,StateChanged=false},"RNG_ADVANCE_NOT_PROVEN");
Flow("nonempty-draw-pile-not-target",baseline with{DrawAtEntry=1},"FIXTURE_NOT_ELIGIBLE");
Flow("earlier-difference",baseline with{Round2Equal=false},"SEMANTIC_DIFFERENT_STOP_NO_NEXT_DISPATCH");
Assert(ReshuffleContract.NextDispatch(false,false,1)=="STOP_DIFFERENCE" && ReshuffleContract.NextDispatch(true,false,4)=="STOP_OPERATION_BOUND" && ReshuffleContract.NextDispatch(true,true,0)=="STOP_TERMINAL","dispatch-stop-gates");
foreach(var path in inputFiles)Assert(Hash(File.ReadAllBytes(path))==inputHashes[path],"original-fixture-unchanged");
Save("comparison-cases.json",new {Source="REAL_PUBLIC_001B_EQUALS_AND_INDEPENDENT_SYNTHETIC_MUTANTS",Cases=actualCases,SwapFingerprint0=fingerprint0,SwapFingerprint1=fingerprint1,MutableNodeReferencesSharedWithExpected=0,ExpectedOriginalHash=Hash(Encoding.UTF8.GetBytes(original))});
Save("flow-cases.json",new {Source="SYNTHETIC_CONTRACT_ONLY_NOT_GAME_RESHUFFLE",Cases=flowCases,DispatchStopGates="PASS",IndependentStateVector="PASS"});
Save("receipt.json",new {Schema=1,Date="2026-10-23",Status="OFFLINE_PASS_RUNTIME_HOLD",ComparatorSHA256=Hash(File.ReadAllBytes(comparator)),ComparisonCases=actualCases.Count,FlowCases=flowCases.Count,PreparationPlan="PASS_SYNTHETIC_ROUND3_TWO_ENDS_FOUR_OPERATIONS_FIVE_POINTS",Inputs=inputFiles.Select(p=>new{Path=Path.GetRelativePath(workspace,p).Replace('\\','/'),SHA256=inputHashes[p],AfterSHA256=Hash(File.ReadAllBytes(p))}),OriginalFixtureHashesPreserved=true,ProductionEffectsCalled=false,RuntimeProbeBuilt=false,PlayerPrivateLogConfigOrLiveModsAccessed=false,FormalAttempts=0,Result="READY_FIRST_RESHUFFLE_CONTRACT"});
Console.WriteLine($"PASS {actualCases.Count} detector cases + {flowCases.Count} synthetic flow cases; independent RNG vector/alias/stop gates; all6 public hashes unchanged; RUNTIME_HOLD");

JsonNode Clone()=>JsonNode.Parse(original)!;
void Mutation(string name,JsonNode candidate,string[] paths,string reset)
{
    var expectedRefs=Nodes(expectedNode).ToHashSet(ReferenceEqualityComparer.Instance);
    Assert(!Nodes(candidate).Any(expectedRefs.Contains),"no-alias-"+name);
    var restored=candidate.DeepClone();if(reset=="RunRng")restored["RunRng"]=expectedNode["RunRng"]!.DeepClone();else restored["Player"]![reset]=expectedNode["Player"]![reset]!.DeepClone();
    Assert(JsonNode.DeepEquals(restored,expectedNode),"unselected-fields-unchanged-"+name);
    Check(name,Wrap(original),Wrap(candidate.ToJsonString()),"SemanticDifferent",paths);
}
void Check(string name,string expected,string candidate,string classification,string[] paths)
{
    var result=Detector.Compare(expected,candidate);Assert(result.Classification==classification,"class-"+name);
    Assert(result.Paths.Order(StringComparer.Ordinal).SequenceEqual(paths.Order(StringComparer.Ordinal)),"paths-"+name);
    actualCases.Add(new{Name=name,ExpectedClass=classification,Actual=result.Classification,ExpectedPaths=paths,ActualPaths=result.Paths,Rejection=result.Rejection,ExpectedHash=Hash(Encoding.UTF8.GetBytes(expected)),CandidateHash=Hash(Encoding.UTF8.GetBytes(candidate))});
}
void Flow(string name,ReshuffleEvidence evidence,string expected){var actual=ReshuffleContract.Check(evidence);Assert(actual==expected,"flow-"+name);flowCases.Add(new{Name=name,Expected=expected,Actual=actual,Evidence=evidence});}
void Save(string name,object value){var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true})+Environment.NewLine);using var s=new FileStream(Path.Combine(output,name),FileMode.CreateNew);s.Write(bytes);}
static string Wrap(string semantic)=>JsonSerializer.Serialize(new SnapshotEnvelope(1,SnapshotEnvelope.Revision,semantic));
static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
static void Assert(bool condition,string code){if(!condition)throw new InvalidDataException(code);}
static IEnumerable<JsonNode> Nodes(JsonNode n){yield return n;foreach(var child in n is JsonObject o?o.Select(p=>p.Value):n is JsonArray a?a:[]){if(child is not null)foreach(var nested in Nodes(child))yield return nested;}}
static string Canonical(JsonNode node)=>Sorted(node).ToJsonString();
static JsonNode Sorted(JsonNode n)=>n switch{JsonObject o=>new JsonObject(o.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new KeyValuePair<string,JsonNode?>(p.Key,p.Value is null?null:Sorted(p.Value)))),JsonArray a=>new JsonArray(a.Select(v=>v is null?null:Sorted(v)).ToArray()),_=>n.DeepClone()};

public sealed record SnapshotEnvelope([property:JsonRequired]int Schema,[property:JsonRequired]string Projection,[property:JsonRequired]string Semantic)
{public const string Revision="SPIKE001B.phase0-r2.Core.Projection";}
public sealed record Detection(string Classification,string[] Paths,string? Rejection=null);
public static class Detector
{
    public static Detection Compare(string expected,string candidate)
    {
        try
        {
            var a=Read(expected);var b=Read(candidate);var paths=Projection.Compare(a,b);
            return new(paths.Length==0?"Equal":"SemanticDifferent",paths);
        }
        catch(Rejected ex){return new("InputRejected",[],ex.Code);}
    }
    private static string Read(string value)
    {
        SnapshotEnvelope envelope;
        try{envelope=JsonSerializer.Deserialize<SnapshotEnvelope>(value,new JsonSerializerOptions{MaxDepth=64})??throw new JsonException();}
        catch(JsonException){throw new Rejected("EnvelopeMissingOrMalformed");}
        if(envelope.Schema!=1 || envelope.Projection!=SnapshotEnvelope.Revision)throw new Rejected("EnvelopeSchemaOrProjection");
        try{Projection.Thaw(envelope.Semantic);return envelope.Semantic;}
        catch(JsonException){throw new Rejected("ProjectionMissingOrMalformed");}
        catch(InvalidDataException){throw new Rejected("ProjectionContractOrDigest");}
    }
    private sealed class Rejected(string code):Exception {internal string Code {get;}=code;}
}
