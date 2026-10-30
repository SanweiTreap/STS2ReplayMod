using System.Text.Json;
using ReplayMod;

internal static class OrbContracts
{
    private sealed record Probe(string Model){internal decimal Stored=6;internal bool Removed;}
    private sealed class Queue {internal int Capacity=3;internal readonly List<Probe> Orbs=[];}
    internal static void Run(string output)
    {
        var tests=new List<string>();
        void Need(bool yes,string reason)=>Codec.Require(yes,reason);
        void Test(string name,Action action){action();tests.Add(name);}
        Test("same-kind-and-equal-values-still-have-distinct-native-instance-identities",()=>
        {
            var ids=new OrbIdentityMap<Probe>();var a=new Probe("ORB.DARK_ORB");var b=new Probe("ORB.DARK_ORB");
            Need(a==b&&!ReferenceEquals(a,b),"probe-must-be-value-equal");
            Need(ids.Admit(a)==1&&ids.Admit(b)==2&&ids.Admit(a)==1&&ids.NextId==2,"orb-identity-collapsed-or-reassigned");
        });
        Test("queue-order-capacity-dark-value-removal-flag-and-future-id-restore-together",()=>
        {
            var ids=new OrbIdentityMap<Probe>();var queue=new Queue();var dark=new Probe("ORB.DARK_ORB");var frost=new Probe("ORB.FROST_ORB");
            queue.Orbs.AddRange([dark,frost]);ids.Admit(dark);ids.Admit(frost);
            IdentityMemento Save()=>new([queue,ids],o=>o is Queue or Probe or OrbIdentityMap<Probe>,_=>true);
            var before=Save();dark.Stored=20;dark.Removed=true;queue.Orbs.Remove(dark);queue.Capacity=5;
            var generated=new Probe("ORB.LIGHTNING_ORB");Need(ids.Admit(generated)==3,"third-orb-id");queue.Orbs.Add(generated);var after=Save();
            for(var attempt=0;attempt<3;attempt++)
            {
                before.Restore();Need(queue.Capacity==3&&queue.Orbs.SequenceEqual(new[]{dark,frost})&&dark.Stored==6&&!dark.Removed&&
                    ids.NextId==2&&ids.Known.SequenceEqual(new[]{dark,frost}),"orb-back-data-or-counter-drift");
                Need(ids.Admit(new("ORB.LIGHTNING_ORB"))==3,"orb-id-not-reusable-in-new-native-branch");
                after.Restore();Need(queue.Capacity==5&&queue.Orbs.SequenceEqual(new[]{frost,generated})&&dark.Stored==20&&dark.Removed&&
                    ids.NextId==3&&ids.Require(generated)==3,"orb-later-checkpoint-lost-original-instance");
            }
        });
        var state=new OrbQueueState(3,2,[new(1,"ORB.DARK_ORB",0,false,8,20,new(){{"DarkOrb._evokeVal","20"}}),
            new(2,"ORB.FROST_ORB",0,false,4,7,new())]);
        string Projection(OrbQueueState queue)=>JsonSerializer.Serialize(new{Mechanics=new{OrbQueue=queue,
            CombatUnits=new CombatUnitState(3,[new(0,"CHARACTER.DEFECT",null,true,75,75,0,null,null,[])],[])}},Codec.Json);
        var projection=Projection(state);
        var clip=new Clip(8,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],
            "CHARACTER.DEFECT","seed",1,0,"ordinary","checkpoint",projection,
            [new(1,OperationKind.EndTurn,1,null,null,null,projection,Timing:new(10,20))],Choices:[],
            Timing:new("checkpoint-stopwatch-elapsed-realtime-v1",0,true,true,true));
        void Validate(Clip c)=>Codec.Validate(c,"game","mod","scope");
        void Reject(Clip c)
        {
            var rejected=false;try{Validate(c);}catch(InvalidDataException){rejected=true;}
            Need(rejected,"invalid-orb-state-accepted");
        }
        Test("format8-preserves-orb-data-in-original-file-and-production-reader",()=>
        {
            Validate(clip);var root=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"orb-clips");var store=new ClipStore(root);
            store.Seal(clip);var read=store.Read(clip.Id,"game","next-mod","scope","mod");
            Need(Codec.Bytes(clip).SequenceEqual(Codec.Bytes(read))&&NativePlaybackSupport.Reason(read) is null,"orb-production-readback");
            using var doc=JsonDocument.Parse(read.Operations[0].After);
            var actual=JsonSerializer.Deserialize<OrbQueueState>(doc.RootElement.GetProperty("Mechanics").GetProperty("OrbQueue").GetRawText(),Codec.Json)!;
            Need(actual.Orbs[0].Mutable["DarkOrb._evokeVal"]=="20"&&actual.Orbs[0].Evoke==20&&actual.Orbs[1].Identity==2,"orb-private-state-not-persisted");
        });
        Test("old-format-cannot-claim-Defect-and-orb-identities-capacity-owner-are-validated",()=>
        {
            Reject(clip with{Format=7});
            Reject(clip with{Initial=Projection(state with{Capacity=1})});
            Reject(clip with{Initial=Projection(state with{Orbs=[state.Orbs[0],state.Orbs[0]]})});
            Reject(clip with{Initial=Projection(state with{NextIdentity=1})});
            Reject(clip with{Initial=Projection(state with{Orbs=[state.Orbs[0] with{Owner=1}]})});
        });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_ORB_IDENTITY_MEMENTO_AND_PRODUCTION_FORMAT_ONLY",Tests=tests,
            NativeOrbCmdExecuted=false,NativeHudAndSubscriptionsExecuted=false,FileColdStateRestorationExecuted=false}));
        Console.WriteLine($"PASS_ORB_IDENTITY_MEMENTO_AND_PRODUCTION_FORMAT_ONLY {tests.Count}");
    }
}
