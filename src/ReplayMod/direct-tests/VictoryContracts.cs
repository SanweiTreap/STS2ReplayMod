using System.Text;
using System.Text.Json;

namespace ReplayMod;

internal static class VictoryContracts
{
    internal static void VerifyExisting(string root,string path)
    {
        var original=File.ReadAllBytes(path);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(original,Codec.Json)!;var clip=envelope.Data;
        Codec.Require(Codec.Hash(Codec.Bytes(clip))==envelope.SHA256&&clip.Format==2&&clip.Operations[^1].Victory,"previous-victory-file-integrity");
        Codec.Validate(clip,"0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","current-build",clip.Scope,
            "46BF021546F6A5DB1B1B8D5A2B01D640E4D5BDC6EBB87C885DDE2DBADA28EC91","5F710B9FB7FE10B962F84D47698CE0C9B0151756A4BE0894FDD6B3834B4C65F7");
        Codec.Require(File.ReadAllBytes(path).SequenceEqual(original),"previous-victory-file-changed");Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root,"victory-compatibility.json"),Codec.Bytes(new{Result="PASS_ACTUAL_PREVIOUS_VICTORY_FILE_COMPATIBILITY",clip.Id,clip.Format,clip.ModHash,
            Operations=clip.Operations.Length,Victory=true,EnvelopeMatched=true,OriginalFileUnchanged=true,RealGameExecuted=false}));
        Console.WriteLine("PASS_ACTUAL_PREVIOUS_VICTORY_FILE_COMPATIBILITY");
    }
    internal static void Run(string root,string? legacyPath)
    {
        Directory.CreateDirectory(root);var results=new List<string>();
        void Need(bool ok){if(!ok)throw new Exception("victory-contract");}
        void Test(string name,Action action){action();results.Add(name);}
        void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("expected-reject");}
        var ending=JsonSerializer.Serialize(new{Outcome="Victory",RoomPreFinished=true,CombatInProgress=false,State="native-final-state",Defeated=new[]{new{CurrentHp=0,IsAlive=false}}});
        var clip=new Clip(2,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],
            "CHARACTER.IRONCLAD","seed",1,0,"ordinary","native-checkpoint","initial",
            [new(1,OperationKind.Card,1,0,"CARD.STRIKE_IRONCLAD",new(0,"enemy",1),ending,true)]);
        Test("real-terminal-operation-can-seal-without-any-EndTurn-and-reopen-with-result-intact",()=>
        {
            Codec.Validate(clip,"game","mod","scope");var store=new ClipStore(root);store.Seal(clip);
            var actual=store.Read(store.LatestId()!,"game","mod","scope");Need(actual.Operations.Length==1&&actual.Operations[0].Victory&&actual.Operations[0].After==ending);
        });
        Test("victory-only-at-last-operation-and-no-false-ended-or-legacy-terminal-claims",()=>
        {
            Reject(()=>Codec.Validate(clip with{Operations=[clip.Operations[0],clip.Operations[0] with{Sequence=2,Victory=false}]},"game","mod","scope"));
            Reject(()=>Codec.Validate(clip with{Format=1},"game","mod","scope"));
            Reject(()=>Codec.Validate(clip with{Format=4},"game","mod","scope"));
            Reject(()=>Codec.Validate(clip with{Operations=[clip.Operations[0] with{Victory=false}]},"game","mod","scope"));
            Reject(()=>Codec.Validate(clip with{Operations=[clip.Operations[0] with{After=ending.Replace("\"RoomPreFinished\":true","\"RoomPreFinished\":false")}]},"game","mod","scope"));
        });
        Test("longer-battle-not-auto-cut-at-old-32-and-explicit-cap-still-enforced",()=>
        {
            var ops=Enumerable.Range(1,40).Select(i=>clip.Operations[0] with{Sequence=i,Victory=i==40,After=i==40?ending:"ordinary-state"}).ToArray();
            Codec.Validate(clip with{Operations=ops},"game","mod","scope");
            Reject(()=>Codec.Validate(clip with{Operations=new Operation[257]},"game","mod","scope"));
        });
        Test("false-terminal-field-is-omitted-so-legacy-envelope-bytes-stay-identical",()=>
        {
            var op=new Operation(1,OperationKind.EndTurn,1,null,null,null,"after");
            Need(Encoding.UTF8.GetString(Codec.Bytes(op))=="{\"Sequence\":1,\"Kind\":\"EndTurn\",\"Turn\":1,\"HandIndex\":null,\"Card\":null,\"Target\":null,\"After\":\"after\"}");
        });
        if(legacyPath is not null)Test("actual-prior-12-operation-file-keeps-envelope-integrity-and-original-build-metadata",()=>
        {
            var bytes=File.ReadAllBytes(legacyPath);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;
            Need(envelope.Data.Format==1&&envelope.Data.Operations.Length==12&&Codec.Hash(Codec.Bytes(envelope.Data))==envelope.SHA256);
            Codec.Validate(envelope.Data,envelope.Data.GameHash,"new-build",envelope.Data.Scope,"8A7A03F9190EE81EFFD024013518B9697A7A9A12E44D1658353013F65984D6C7","C825AF5E111AC5C76DE2815EFEA40A9DC5AC4DE18B58C77B2F30CA0BF4992D1B");
            Need(File.ReadAllBytes(legacyPath).SequenceEqual(bytes));
        });
        File.WriteAllBytes(Path.Combine(root,"victory-contracts.json"),Codec.Bytes(new{Result="PASS_VICTORY_FORMAT_CONTRACTS",Count=results.Count,Tests=results,RealGameExecuted=false,GameConstructorsExecuted=false}));
        Console.WriteLine($"PASS_VICTORY_FORMAT_CONTRACTS {results.Count}");
    }
}
