using System.Text;
using System.Text.Json;
namespace ReplayMod;

internal static class ContentContracts
{
    internal static void VerifyExisting(string root,string path)
    {
        var bytes=File.ReadAllBytes(path);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;var clip=envelope.Data;
        Codec.Require(Codec.Hash(Codec.Bytes(clip))==envelope.SHA256&&clip.Format==3&&clip.Operations[^1].Victory,"actual-content-file-integrity");
        Codec.Require(clip.Operations.Any(o=>o.Kind==OperationKind.Potion&&o.Target?.Role==TargetRole.Player)&&
            clip.Operations.Any(o=>o.CardState?.Upgrade>0),"actual-content-file-mechanisms");
        Codec.Validate(clip,"0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","current-build",clip.Scope,
            "E428FC791BB7EA22E8CCAD37DF90A2FA10F0B5DC756033F4B7450E4C8D2B686A");
        Codec.Require(File.ReadAllBytes(path).SequenceEqual(bytes),"actual-content-file-changed");Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root,"content-compatibility-r1.json"),Codec.Bytes(new{Result="PASS_ACTUAL_CONTENT_CLIP_COMPATIBILITY",clip.Id,clip.Format,clip.ModHash,
            Operations=clip.Operations.Length,EnvelopeMatched=true,OriginalFileUnchanged=true,RealGameExecuted=false}));
        Console.WriteLine("PASS_ACTUAL_CONTENT_CLIP_COMPATIBILITY");
    }
    internal static void Run(string root,string? legacyPath)
    {
        Directory.CreateDirectory(root);var tests=new List<string>();
        void Need(bool ok){if(!ok)throw new Exception("content-contract");}
        void Test(string name,Action action){action();tests.Add(name);}
        void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("expected-reject");}
        var hash=new string('A',64);var player=new TargetKey(0,"CHARACTER.IRONCLAD",0,TargetRole.Player);
        var ending=JsonSerializer.Serialize(new{Outcome="Victory",RoomPreFinished=true,CombatInProgress=false,State="final",Defeated=new[]{new{IsAlive=false}}});
        var shrug=new Operation(1,OperationKind.Card,1,12,"CARD.SHRUG_IT_OFF",null,"after-shrug",CardState:new(11,1,hash,"Self"));
        var potion=new Operation(2,OperationKind.Potion,1,null,null,player,"after-potion",Potion:new(0,"POTION.ENERGY_POTION",hash,"AnyPlayer"));
        var endTurn=new Operation(3,OperationKind.EndTurn,1,null,null,null,"third-turn-with-native-relic-trigger");
        var lethal=new Operation(4,OperationKind.Card,2,0,"CARD.THUNDERCLAP",null,ending,true,new(7,0,hash,"AllEnemies"));
        var clip=new Clip(3,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],
            "CHARACTER.IRONCLAD","seed",1,0,"ordinary","actual-prepared-checkpoint","initial",[shrug,potion,endTurn,lethal],"explicit-content-preparation");
        Test("format3-generic-upgraded-card-potion-self-target-and-derived-turn-effect-roundtrip",()=>
        {
            Codec.Validate(clip,"game","mod","scope");var store=new ClipStore(Path.Combine(root,"format3"));store.Seal(clip);
            var read=store.Read(clip.Id,"game","mod","scope");Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(clip))&&read.Operations.Length==4);
            Need(read.Operations[0].CardState!.Upgrade==1&&read.Operations[1].Target!.Role==TargetRole.Player);
            Need(read.Operations[0].CardState!=read.Operations[0].CardState! with{CombatIndex=12});
        });
        Test("invalid-mixed-or-missing-input-identities-and-invalid-targets-are-rejected",()=>
        {
            void Bad(Operation op)=>Reject(()=>Codec.Validate(clip with{Operations=[op,lethal with{Sequence=2}]},"game","mod","scope"));
            Bad(shrug with{CardState=null});Bad(shrug with{Potion=potion.Potion});Bad(potion with{Sequence=1,Potion=null});
            Bad(potion with{Sequence=1,Card="CARD.BASH"});Bad(potion with{Sequence=1,Potion=potion.Potion! with{Slot=-1}});
            Bad(potion with{Sequence=1,Target=player with{Role=(TargetRole)99}});Bad(potion with{Sequence=1,Target=player with{Index=-1}});
            Bad(shrug with{CardState=shrug.CardState! with{Fingerprint="short"}});Bad(endTurn with{Sequence=1,Target=player});
            Reject(()=>Codec.Validate(clip with{Format=2},"game","mod","scope"));
        });
        Test("checksum-protects-instance-upgrade-potion-slot-and-target-role-metadata",()=>
        {
            var dir=Path.Combine(root,"corrupt");var store=new ClipStore(dir);store.Seal(clip);
            var path=Path.Combine(dir,clip.Id+".json");var envelope=JsonSerializer.Deserialize<ClipEnvelope>(File.ReadAllBytes(path),Codec.Json)!;
            var ops=envelope.Data.Operations.ToArray();ops[1]=ops[1] with{Target=player with{CombatId=2},Potion=potion.Potion! with{Slot=1}};
            File.WriteAllBytes(path,Codec.Bytes(envelope with{Data=envelope.Data with{Operations=ops}}));Reject(()=>store.Read(clip.Id,"game","mod","scope"));
        });
        Test("new-optional-fields-and-default-enemy-role-do-not-change-old-json",()=>
        {
            Need(Encoding.UTF8.GetString(Codec.Bytes(new TargetKey(0,"MONSTER.CORPSE_SLUG",1)))=="{\"Index\":0,\"Model\":\"MONSTER.CORPSE_SLUG\",\"CombatId\":1}");
            Need(Encoding.UTF8.GetString(Codec.Bytes(endTurn with{Sequence=1,After="after"}))=="{\"Sequence\":1,\"Kind\":\"EndTurn\",\"Turn\":1,\"HandIndex\":null,\"Card\":null,\"Target\":null,\"After\":\"after\"}");
        });
        Test("memento-restores-consumed-potion-same-slot-owner-and-queued-flags-with-relic-counter",()=>
        {
            var state=new Items();var potionObject=new Bottle{Owner=state};state.Slots[1]=potionObject;
            IdentityMemento Save()=>new(new object[]{state,potionObject},o=>o is Items or Bottle or Flower,_=>true);
            var before=Save();state.Slots[1]=null;potionObject.Removed=true;potionObject.Queued=true;state.Energy=5;state.Flower.TurnsSeen=0;state.History.Add("ENERGY_POTION");var after=Save();
            for(var i=0;i<3;i++)
            {
                before.Restore();Need(ReferenceEquals(state.Slots[1],potionObject)&&ReferenceEquals(potionObject.Owner,state)&&!potionObject.Removed&&!potionObject.Queued&&state.Energy==3&&state.Flower.TurnsSeen==2&&state.History.Count==0);
                after.Restore();Need(state.Slots[1] is null&&potionObject.Removed&&potionObject.Queued&&state.Energy==5&&state.Flower.TurnsSeen==0&&state.History.Single()=="ENERGY_POTION");
            }
        });
        if(legacyPath is not null)Test("actual-DEMO004-file-retains-original-envelope-build-and-preparation",()=>
        {
            var bytes=File.ReadAllBytes(legacyPath);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;var prior=envelope.Data;
            Need(prior.Format==2&&prior.Operations.Length==11&&prior.Preparation=="DEMO004-directed-Underdocks-CorpseSlugsWeak"&&Codec.Hash(Codec.Bytes(prior))==envelope.SHA256);
            Codec.Validate(prior,"0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","new-build",prior.Scope,
                "ECC7F2516BCD8337968CB8D2798F3D81FC57F68B4B9DADE3D15A86D8A9D97987");
            Need(File.ReadAllBytes(legacyPath).SequenceEqual(bytes));
        });
        File.WriteAllBytes(Path.Combine(root,"content-contracts.json"),Codec.Bytes(new{Result="PASS_CONTENT_INPUT_AND_STATE_CONTRACTS",Count=tests.Count,Tests=tests,RealGameExecuted=false,GameConstructorsExecuted=false}));
        Console.WriteLine($"PASS_CONTENT_INPUT_AND_STATE_CONTRACTS {tests.Count}");
    }
    private sealed class Items {public readonly Bottle?[] Slots=new Bottle?[3];public readonly Flower Flower=new();public readonly List<string> History=[];public int Energy=3;}
    private sealed class Bottle {public Items? Owner;public bool Removed,Queued;}
    private sealed class Flower {public int TurnsSeen=2;}
}
