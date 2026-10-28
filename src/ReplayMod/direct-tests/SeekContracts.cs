using ReplayMod;
using System.Text.Json;

internal static class SeekContracts
{
    internal static void Run(string output,string original)
    {
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        var before=File.ReadAllBytes(original);var clip=JsonSerializer.Deserialize<ClipEnvelope>(before,Codec.Json)!.Data;
        var read=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(original))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","seek-contract",clip.Scope,
            "DC8D7075E181245D79E76435C158084A6A0BB2313490A1BAB5B69A69F90092B9");
        Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(clip)),"seek-original-production-read");
        var nodes=SeekPlan.Build(clip,4);
        Need(nodes.Length==9&&nodes.Select(n=>n.Index).SequenceEqual(Enumerable.Range(0,9)),"seek-index-sequence");
        Need(nodes.Where(n=>n.Stable).Select(n=>n.Host).SequenceEqual(new[]{0,1,2,3,4}),"seek-stable-hosts");
        Need(nodes[2] is {Host:2,Choice:1,Stage:0,Anchor:1}&&nodes[3] is {Choice:1,Stage:1,Anchor:1}&&
            nodes[4] is {Host:2,Choice:2,Stage:0,Anchor:1}&&nodes[5] is {Choice:2,Stage:1,Anchor:1},"seek-choice-local-anchors");
        Need(nodes[6] is {Host:2,Choice:0}&&nodes[7] is {Host:3,Choice:0}&&clip.Operations[2].Kind==OperationKind.EndTurn&&
            clip.Operations[3].Turn==2,"seek-cross-turn-index");
        Need(nodes.Where(n=>!n.Stable).All(n=>nodes[n.Anchor].Stable&&nodes[n.Anchor].Host==n.Host-1),"seek-choice-rebuild-replays-more-than-own-host");
        var hand=clip.Choices![0];Need(SeekPlan.Events(hand).Select(e=>e.Kind).SequenceEqual(new[]{"Select","Confirm"}),"native-hand-confirm-node");
        var autoSubmit=hand with{Kind="FromChooseACardScreen",Manual=true};
        Need(SeekPlan.Events(autoSubmit).Select(e=>e.Kind).SequenceEqual(new[]{"Select"}),"invented-confirm-after-native-autosubmit");
        var requests=new AdvanceRequests();for(var i=0;i<3;i++)requests.Add();
        var position=0;while(requests.Take())position++;Need(position==3,"seek-three-clicks-three-visible-nodes");
        foreach(var command in new[]{"Pause","Back","Seek","Stop"})
        {requests.Add();requests.Add();requests.Clear();Need(!requests.Take(),"seek-stale-next-host-after-"+command);}
        Need(File.ReadAllBytes(original).SequenceEqual(before),"seek-original-file-mutated");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_SEEK_INDEX_NATIVE_CHOICE_PROJECTION_AND_ORIGINAL_READ_CONTRACTS",clip.Id,
            OriginalSHA256=Codec.Hash(before),SliceHosts=4,Nodes=nodes,BoundedChoiceRebuildHosts=1,
            NativeTasksExecuted=false,NativeContinuationAndRngEqualityStillPending=true,Old003TestResult="UnknownNotRead"}));
        Console.WriteLine("PASS_SEEK_INDEX_AND_ORIGINAL_FILE_CONTRACTS_NOT_NATIVE_EXECUTION");
    }
}
