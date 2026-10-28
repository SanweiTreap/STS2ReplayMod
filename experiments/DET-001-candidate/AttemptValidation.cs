using System.Text.Json.Serialization;
using Det001.Core;

namespace Det001;

public sealed record PointData(int Schema,string Revision,string CandidateHash,string Process,string Phase,Step Step,string SemanticHash,QuietWindow Window,PointEligibility Eligibility);
public sealed record ActionData(int Schema,string Revision,string CandidateHash,string Process,string Phase,AcceptedAction Action);
public sealed record ShuffleData(int Schema,string Revision,string CandidateHash,string Process,string Attempt,string CheckpointHash,ShuffleTrace Trace);
public sealed record ReadyData(int Schema,string Revision,string CandidateHash,string Process,string Attempt,string CompleteHash);
public sealed record CompleteData([property:JsonRequired]int Schema,[property:JsonRequired]string Revision,
    [property:JsonRequired]string CandidateHash,[property:JsonRequired]string Process,[property:JsonRequired]string Attempt,
    [property:JsonRequired]string CheckpointHash,[property:JsonRequired]string[] SnapshotHashes,[property:JsonRequired]string[] CaptureHashes,
    [property:JsonRequired]string[] ActionHashes,[property:JsonRequired]string ShuffleHash);
public static class AttemptValidation
{
    public static bool ReadyB(PrivateStore store,string attempt)=>!store.Exists(attempt,"terminal.json") &&
        !store.Exists(attempt,"B.claim.json") && store.Exists(attempt,"A.complete.json") && store.Exists(attempt,"A.ready-B.json");
    public static void BInput(ArmData arm,SealData seal,PointData[] captures,ActionData[] actions,CompleteData complete,ReadyData ready,
        string attempt,string candidate,string processB,string[] snapshots,byte[] original,byte[] current,ShuffleData shuffle)
    {
        Contract.Require(captures is {Length:5} && actions is {Length:4} && snapshots is {Length:5} && complete.SnapshotHashes is {Length:5} && complete.CaptureHashes is {Length:5} && complete.ActionHashes is {Length:4},"A-incomplete");
        Contract.Require(candidate.Length==64 && arm.Schema==PrivateStore.Schema && arm.Revision==PrivateStore.Revision &&
            seal.Schema==PrivateStore.Schema && seal.Revision==PrivateStore.Revision && arm.CandidateHash==candidate &&
            seal.CandidateHash==candidate && seal.Process==arm.Process && seal.Binding.Target==arm.Target,"attempt-identity");
        Contract.Arm(arm.Target,arm.Target.ArmFloor,arm.Predecessor);
        Eligibility.Arm(arm.Eligibility);
        Contract.Require(complete.Schema==PrivateStore.Schema && complete.Revision==PrivateStore.Revision && complete.CandidateHash==candidate &&
            complete.Attempt==attempt && complete.Process==arm.Process && complete.CheckpointHash==seal.Binding.Checkpoint.Hash,"complete-identity");
        Contract.Require(ready.Schema==PrivateStore.Schema && ready.Revision==PrivateStore.Revision && ready.CandidateHash==candidate && ready.Process==arm.Process &&
            ready.Attempt==attempt && ready.CompleteHash==Json.Hash(Json.Serialize(complete)),"A-ready-proof");
        for(var i=0;i<5;i++)
        {
            var c=captures[i]; Window(c.Window);
            Contract.Require(c.Schema==PrivateStore.Schema && c.Revision==PrivateStore.Revision && c.CandidateHash==candidate && c.Process==arm.Process &&
                c.Phase=="A" && c.Step==(Step)i && c.SemanticHash==Json.Hash(snapshots[i]) && complete.SnapshotHashes[i]==c.SemanticHash &&
                complete.CaptureHashes[i]==Json.Hash(Json.Serialize(c)),"capture-identity");
            Eligibility.Point(c.Eligibility,arm.Eligibility,Projection.Thaw(snapshots[i]),i);
        }
        for(var i=0;i<4;i++)
        {
            var a=actions[i]; Contract.Require(a.Schema==PrivateStore.Schema && a.Revision==PrivateStore.Revision && a.CandidateHash==candidate &&
                a.Process==arm.Process && a.Phase=="A" && a.Action.Sequence==i+1 && complete.ActionHashes[i]==Json.Hash(Json.Serialize(a)),"action-identity");
        }
        Trajectory.ValidateA(seal.Binding,snapshots,actions.Select(a=>a.Action).ToArray());
        Contract.Require(shuffle is not null && shuffle.Schema==PrivateStore.Schema && shuffle.Revision==PrivateStore.Revision && shuffle.CandidateHash==candidate && shuffle.Process==arm.Process && shuffle.Attempt==attempt && shuffle.CheckpointHash==seal.Binding.Checkpoint.Hash && complete.ShuffleHash==Json.Hash(Json.Serialize(shuffle)),"shuffle-artifact-identity");
        Shuffle(shuffle!.Trace,seal.Binding,snapshots);
        var parsed=NativeCheckpoint.Parse(original);var facts=parsed.Facts;NativeCheckpoint.MatchArm(parsed.Save,arm.Eligibility);Contract.Seal(arm.Target,arm.Predecessor,facts);
        Contract.Require(facts==seal.Binding.Checkpoint && Json.Hash(original)==complete.CheckpointHash,"private-checkpoint-identity");
        Contract.Require(current.AsSpan().SequenceEqual(original),"source-changed-before-B");
        Contract.Require(!string.IsNullOrWhiteSpace(processB) && !string.IsNullOrWhiteSpace(arm.Process) && processB!=arm.Process,"fresh-process-required");
    }
    public static void Shuffle(ShuffleTrace trace,Binding binding,string[] snapshots)
    {
        Contract.Require(snapshots is {Length:5},"shuffle-five-points-required");
        var first=Projection.Thaw(snapshots[0]);TraceContract.Complete(trace,binding,first.Position.Encounter);
        var before=Projection.Thaw(snapshots[3]);var after=Projection.Thaw(snapshots[4]);
        Contract.Require(before.RunRng.Entries.Single(e=>e.Key=="Shuffle")==trace.E1.Rng.Entry && after.RunRng.Entries.Single(e=>e.Key=="Shuffle")==trace.E2.Rng.Entry,"shuffle-stable-stream-link");
        Contract.Require(trace.E1.Cards.Select(c=>Actions.Fingerprint(c.Value)).Order(StringComparer.Ordinal).SequenceEqual(before.Player.Hand.Concat(before.Player.Draw).Concat(before.Player.Discard).Concat(before.Player.Exhaust).Select(Actions.Fingerprint).Order(StringComparer.Ordinal)),"shuffle-stable-member-link");
        // DrawInternal removes from the head. S4 records both resulting ordered piles.
        Contract.Require(trace.E3.Cards.Take(5).Select(c=>Actions.Fingerprint(c.Value)).SequenceEqual(after.Player.Hand.Select(Actions.Fingerprint)) && trace.E3.Cards.Skip(5).Select(c=>Actions.Fingerprint(c.Value)).SequenceEqual(after.Player.Draw.Select(Actions.Fingerprint)),"shuffle-final-pile-link");
    }
    public static void Window(QuietWindow w)
    {
        Contract.Require(w is not null && w.Count==30 && w.LastFrame-w.FirstFrame==29 && w.EndMs>=w.StartMs && w.MaxGapMs is >=0 and <=250 &&
            w.Fields is {Length:20} && w.Fields.Select(x=>x.Name).Order().SequenceEqual(Flow.FieldNames.Order()) &&
            w.Fields.All(x=>x.Status==FieldStatus.True),"capture-window-invalid");
    }
}
