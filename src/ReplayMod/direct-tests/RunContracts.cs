using System.Text.Json;
using System.Text.Json.Nodes;
using ReplayMod;

internal static class RunContracts
{
    internal static void Verify(string output,string root,string id)
    {
        var path=Path.Combine(root,id+".json");var before=File.ReadAllBytes(path);var run=new RunStore(root).Read(id);
        Codec.Require(run.Id==id,"run-id-mismatch");
        var gaps=RunStore.ResourceDiscontinuities(run);var anchor=run.Operations.FirstOrDefault(o=>o.Kind=="battle"&&o.Status=="complete");
        Codec.Require(anchor is not null,"run-has-no-complete-battle-anchor");
        Codec.Require(!gaps.Any(s=>s>anchor!.Sequence),"run-has-missing-resource-transition-after-anchor");
        Codec.Require(File.ReadAllBytes(path).SequenceEqual(before),"run-read-mutated-original");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_ACTUAL_RUN_PRODUCTION_READ_NOT_NATIVE_PLAYBACK",run.Id,run.Format,
            OriginalSHA256=Codec.Hash(before),Visits=run.Visits.Count,Operations=run.Operations.Count,StartSequence=anchor!.Sequence,
            ResourceDiscontinuities=gaps,OriginalUnchanged=true,NativeRuntimeNotExecutedByThisCheck=true}));
        Console.WriteLine("PASS_ACTUAL_RUN_PRODUCTION_READ_NOT_NATIVE_PLAYBACK");
    }
    internal static void Run(string output)
    {
        var folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"runs-"+Guid.NewGuid().ToString("N"));
        var store=new RunStore(folder);var passed=new List<string>();
        void Test(string name,Action action){action();passed.Add(name);}
        void Need(bool value,string why)=>Codec.Require(value,why);
        var id=Guid.NewGuid().ToString("N");
        var run=new RunDocument(1,id,"game","mod","scope","CHARACTER.IRONCLAD",DateTimeOffset.UtcNow,
            [new(1,0,2,1,0,"Monster","Monster","ENCOUNTER.TEST",1,"left",null),new(2,0,3,2,0,"RestSite","RestSite",null,2,"entered",null)],
            [new(1,1,"battle",new(),null,null,"complete",[],"clip",new(id,1,1,0,2,100,"map","victory")),
             new(2,1,"reward",new(RewardIndex:1,RewardKey:"cards"),"before","after","complete",[new("reward-card",["same","same"],[1])]),
             new(3,1,"map",new(2,0),"after",null,"complete",[]),
             new(4,2,"rest",new(Option:"SMITH"),"after",null,"pending",[])],[]);
        Test("run-envelope-preserves-visit-versus-battle-identity-and-final-choice-of-duplicate-options",()=>
        {
            store.Save(run);var read=store.Read(id);
            Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(run))&&read.Visits[1].Entries==2&&read.Operations[1].Decisions[0].Selected.Single()==1,"run-roundtrip-lost-semantics");
        });
        Test("incomplete-operation-is-never-promoted-to-complete-on-read",()=>
        {var read=store.Read(id);Need(read.Operations[^1].Status=="pending"&&read.Operations[^1].After is null,"incomplete-operation-falsified");});
        Test("unrecorded-resource-change-is-diagnosed-without-inventing-a-choice-or-mutating-history",()=>
        {
            var steps=run.Operations.ToList();steps[2]=steps[2] with{Before="unrecorded-claim"};var gap=run with{Operations=steps};
            var before=Codec.Bytes(gap);
            Need(RunStore.ResourceDiscontinuities(gap).SequenceEqual(new[]{3})&&RunStore.ResourceDiscontinuities(run).Length==0&&Codec.Bytes(gap).SequenceEqual(before),"missing-transition-not-truthfully-diagnosed");
        });
        Test("altered-run-payload-is-rejected-while-another-good-run-remains-readable",()=>
        {
            var good=run with{Id=Guid.NewGuid().ToString("N")};store.Save(good);
            var path=Path.Combine(folder,id+".json");var node=JsonNode.Parse(File.ReadAllBytes(path))!;node["Data"]!["Character"]="changed";File.WriteAllText(path,node.ToJsonString());
            var rejected=false;try{store.Read(id);}catch(InvalidDataException){rejected=true;}
            Need(rejected&&store.Read(good.Id).Character==run.Character,"corrupt-run-not-isolated");
        });
        Test("missing-visit-and-noncontiguous-operation-order-are-rejected",()=>
        {
            var rejected=0;
            foreach(var bad in new[]{run with{Operations=[run.Operations[0] with{Sequence=2}]},run with{Visits=[],Operations=[run.Operations[0]]}})
                try{store.Save(bad);}catch(InvalidDataException){rejected++;}
            Need(rejected==2,"invalid-run-ownership-accepted");
        });
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_RUN_METADATA_AND_FILE_CONTRACTS_NOT_NATIVE_RUNTIME",Tests=passed}));
        Console.WriteLine("PASS_RUN_METADATA_FILE_CONTRACTS_NOT_NATIVE_RUNTIME");
    }
}
