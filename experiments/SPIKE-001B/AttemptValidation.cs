using System.Text.Json.Serialization;
using Spike001B.Core;

namespace Spike001B;

public sealed record PointData(int Schema,string Revision,string CandidateHash,string Process,string Phase,Step Step,string SemanticHash,QuietWindow Window);
public sealed record ActionData(int Schema,string Revision,string CandidateHash,string Process,string Phase,AcceptedAction Action);
public sealed record ReadyData(int Schema,string Revision,string CandidateHash,string Process,string Attempt,string CompleteHash);
public sealed record CompleteData([property:JsonRequired]int Schema,[property:JsonRequired]string Revision,
    [property:JsonRequired]string CandidateHash,[property:JsonRequired]string Process,[property:JsonRequired]string Attempt,
    [property:JsonRequired]string CheckpointHash,[property:JsonRequired]string[] SnapshotHashes,[property:JsonRequired]string[] CaptureHashes,
    [property:JsonRequired]string[] ActionHashes);
public static class AttemptValidation
{
    public static bool ReadyB(PrivateStore store,string attempt)=>!store.Exists(attempt,"terminal.json") &&
        !store.Exists(attempt,"B.claim.json") && store.Exists(attempt,"A.complete.json") && store.Exists(attempt,"A.ready-B.json");
    public static void BInput(ArmData arm,SealData seal,PointData[] captures,ActionData[] actions,CompleteData complete,ReadyData ready,
        string attempt,string candidate,string processB,string[] snapshots,byte[] original,byte[] current)
    {
        Contract.Require(captures is {Length:3} && actions is {Length:2} && snapshots is {Length:3} && complete.SnapshotHashes is {Length:3} && complete.CaptureHashes is {Length:3} && complete.ActionHashes is {Length:2},"A-incomplete");
        Contract.Require(candidate.Length==64 && arm.Schema==PrivateStore.Schema && arm.Revision==PrivateStore.Revision &&
            seal.Schema==PrivateStore.Schema && seal.Revision==PrivateStore.Revision && arm.CandidateHash==candidate &&
            seal.CandidateHash==candidate && seal.Process==arm.Process && seal.Binding.Target==arm.Target,"attempt-identity");
        Contract.Arm(arm.Target,arm.Target.ArmFloor,arm.Predecessor);
        Contract.Require(complete.Schema==PrivateStore.Schema && complete.Revision==PrivateStore.Revision && complete.CandidateHash==candidate &&
            complete.Attempt==attempt && complete.Process==arm.Process && complete.CheckpointHash==seal.Binding.Checkpoint.Hash,"complete-identity");
        Contract.Require(ready.Schema==PrivateStore.Schema && ready.Revision==PrivateStore.Revision && ready.CandidateHash==candidate && ready.Process==arm.Process &&
            ready.Attempt==attempt && ready.CompleteHash==Json.Hash(Json.Serialize(complete)),"A-ready-proof");
        for(var i=0;i<3;i++)
        {
            var c=captures[i]; Window(c.Window);
            Contract.Require(c.Schema==PrivateStore.Schema && c.Revision==PrivateStore.Revision && c.CandidateHash==candidate && c.Process==arm.Process &&
                c.Phase=="A" && c.Step==(Step)i && c.SemanticHash==Json.Hash(snapshots[i]) && complete.SnapshotHashes[i]==c.SemanticHash &&
                complete.CaptureHashes[i]==Json.Hash(Json.Serialize(c)),"capture-identity");
        }
        for(var i=0;i<2;i++)
        {
            var a=actions[i]; Contract.Require(a.Schema==PrivateStore.Schema && a.Revision==PrivateStore.Revision && a.CandidateHash==candidate &&
                a.Process==arm.Process && a.Phase=="A" && a.Action.Sequence==i+1 && complete.ActionHashes[i]==Json.Hash(Json.Serialize(a)),"action-identity");
        }
        Trajectory.ValidateA(seal.Binding,snapshots,actions.Select(a=>a.Action).ToArray());
        var facts=NativeCheckpoint.Parse(original).Facts; Contract.Seal(arm.Target,arm.Predecessor,facts);
        Contract.Require(facts==seal.Binding.Checkpoint && Json.Hash(original)==complete.CheckpointHash,"private-checkpoint-identity");
        Contract.Require(current.AsSpan().SequenceEqual(original),"source-changed-before-B");
        Contract.Require(!string.IsNullOrWhiteSpace(processB) && !string.IsNullOrWhiteSpace(arm.Process) && processB!=arm.Process,"fresh-process-required");
    }
    public static void Window(QuietWindow w)
    {
        Contract.Require(w is not null && w.Count==30 && w.LastFrame-w.FirstFrame==29 && w.EndMs>=w.StartMs && w.MaxGapMs is >=0 and <=250 &&
            w.Fields is {Length:20} && w.Fields.Select(x=>x.Name).Order().SequenceEqual(Flow.FieldNames.Order()) &&
            w.Fields.All(x=>x.Status==FieldStatus.True),"capture-window-invalid");
    }
}
