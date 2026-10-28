using Spike001A5.Core;

namespace Spike001A5;

public static class AttemptValidation
{
    public static void BInput(ArmData arm,SealData seal,CaptureData capture,CaptureData complete,string expectedCandidate,string processB,
        string semantic,byte[] privateBytes,byte[] currentBytes)
    {
        Contract.Text(arm.Process,"A-process"); Contract.Text(processB,"B-process");
        Contract.Require(expectedCandidate.Length==64,"candidate-hash-missing");
        Contract.Arm(arm.Target,arm.Target.ArmFloor,arm.Predecessor);
        Contract.Require(arm.Schema==PrivateStore.Schema && seal.Schema==PrivateStore.Schema && arm.Revision==PrivateStore.Revision && seal.Revision==PrivateStore.Revision &&
            arm.CandidateHash==expectedCandidate && seal.CandidateHash==expectedCandidate && seal.Process==arm.Process && seal.Binding.Target==arm.Target,"attempt-schema-identity");
        Contract.Require(capture.Schema==PrivateStore.Schema && capture.Revision==PrivateStore.Revision && capture.CandidateHash==expectedCandidate &&
            capture.Process==arm.Process && capture.Phase=="A" && capture.SemanticHash==Json.Hash(semantic),"A-capture-identity");
        Contract.Require(Json.Serialize(capture)==Json.Serialize(complete),"A-not-committed");
        Window(capture.Window); Contract.Playable(seal.Binding,Projection.Thaw(semantic),null,null);
        var facts=NativeCheckpoint.Parse(privateBytes).Facts;
        Contract.Require(facts==seal.Binding.Checkpoint && Json.Hash(privateBytes)==seal.Binding.Checkpoint.Hash,"private-checkpoint-identity");
        Contract.Seal(arm.Target,arm.Predecessor,facts);
        Contract.Require(currentBytes.AsSpan().SequenceEqual(privateBytes),"source-changed-before-B");
        Contract.Require(!string.IsNullOrWhiteSpace(processB) && processB!=arm.Process,"fresh-process-required");
    }
    public static void Window(QuietWindow w)
    {
        Contract.Require(w is not null && w.Count==30 && w.LastFrame-w.FirstFrame==29 && w.EndMs>=w.StartMs && w.MaxGapMs is >=0 and <=250 &&
            w.Fields is {Length:20} && w.Fields.Select(x=>x.Name).Order().SequenceEqual(Flow.FieldNames.Order()) &&
            w.Fields.All(x=>x.Status==FieldStatus.True),"capture-window-invalid");
    }
}
