using System.Text.Json;
using ReplayMod;

internal static class CombatUnitContracts
{
    private sealed class Owner {internal readonly List<Pet> Pets=[];}
    private sealed class Pet {internal Owner? Owner;internal int Hp=1,MaxHp=1;internal bool Alive=true;}
    private sealed class Combat {internal uint Next=4;internal readonly List<Pet> Allies=[];}
    internal static void Run(string output)
    {
        var tests=new List<string>();
        void Test(string name,Action action){action();tests.Add(name);}
        void Need(bool yes,string why)=>Codec.Require(yes,why);
        var player=new CombatUnitEntry(0,"CHARACTER.NECROBINDER",null,true,66,66,0,null,null,[]);
        var osty=new CombatUnitEntry(3,"MONSTER.OSTY",0,true,6,6,0,"NOTHING_MOVE",new(0,1,2,3,4),[new("POWER.DIE_FOR_YOU_POWER",1)]);
        var units=new CombatUnitState(4,[player,osty],[3]);
        string Projection(CombatUnitState value)=>JsonSerializer.Serialize(new{Mechanics=new{OrbQueue=new OrbQueueState(0,0,[]),CombatUnits=value}},Codec.Json);
        var initial=Projection(units);
        var terminal=JsonSerializer.Serialize(new{Outcome="Victory",RoomPreFinished=true,CombatInProgress=false,State=initial,
            Defeated=new[]{new{Model="MONSTER.CORPSE_SLUG",CombatId=1}}},Codec.Json);
        var clip=new Clip(8,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],
            "CHARACTER.NECROBINDER","seed",1,0,"ordinary","checkpoint",initial,
            [new(1,OperationKind.EndTurn,1,null,null,null,terminal,Victory:true,Timing:new(10,20))],Choices:[],
            Timing:new("checkpoint-stopwatch-elapsed-realtime-v1",0,true,true,true));
        void Validate(Clip value)=>Codec.Validate(value,"game","mod","scope");
        void Reject(Clip value)
        {
            var rejected=false;try{Validate(value);}catch(InvalidDataException){rejected=true;}
            Need(rejected,"invalid-unit-state-accepted");
        }
        Test("format8-keeps-pet-ownership-roster-hp-rng-powers-and-real-terminal-operation-kind",()=>
        {
            Validate(clip);var store=new ClipStore(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"unit-clips"));store.Seal(clip);
            var read=store.Read(clip.Id,"game","next","scope","mod");
            Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(clip))&&NativePlaybackSupport.Reason(read) is null&&
                read.Operations[0] is {Kind:OperationKind.EndTurn,Victory:true},"unit-production-roundtrip");
        });
        Test("foreign-owner-unknown-pet-duplicate-id-future-counter-and-old-format-are-rejected",()=>
        {
            Reject(clip with{Format=7});
            foreach(var invalid in new[]{units with{Allies=[player,osty with{Owner=1}]},units with{Allies=[player,osty with{Model="MONSTER.TOUGH_EGG"}]},
                units with{Allies=[player,osty with{Identity=0}]},units with{NextIdentity=3},units with{Pets=[99]}})
                Reject(clip with{Initial=Projection(invalid)});
        });
        Test("pet-death-revive-growth-owner-and-membership-restore-as-the-same-object",()=>
        {
            var owner=new Owner();var pet=new Pet{Owner=owner};var combat=new Combat();owner.Pets.Add(pet);combat.Allies.Add(pet);
            IdentityMemento Save()=>new([owner,combat],o=>o is Owner or Pet or Combat,_=>true);
            var alive=Save();pet.Alive=false;pet.Hp=0;var dead=Save();pet.Alive=true;pet.MaxHp=7;pet.Hp=7;var grown=Save();
            for(var i=0;i<3;i++)
            {
                alive.Restore();Need(pet.Alive&&pet.Hp==1&&pet.MaxHp==1&&owner.Pets.Single()==pet&&pet.Owner==owner,"pet-initial-restore");
                dead.Restore();Need(!pet.Alive&&pet.Hp==0&&pet.MaxHp==1&&combat.Allies.Single()==pet&&owner.Pets.Single()==pet,"pet-dead-but-present-restore");
                grown.Restore();Need(pet.Alive&&pet.Hp==7&&pet.MaxHp==7&&combat.Allies.Single()==pet&&pet.Owner==owner,"pet-grown-restore");
            }
        });
        Test("before-first-summon-memento-removes-pet-membership-and-restores-future-allocation",()=>
        {
            var owner=new Owner();var combat=new Combat();
            var before=new IdentityMemento([owner,combat],o=>o is Owner or Pet or Combat,_=>true);
            var generated=new Pet{Owner=owner};var firstId=combat.Next++;combat.Allies.Add(generated);owner.Pets.Add(generated);
            before.Restore();Need(combat.Next==firstId&&combat.Allies.Count==0&&owner.Pets.Count==0,"first-summon-membership-or-counter-remains");
            var next=new Pet{Owner=owner};Need(!ReferenceEquals(generated,next)&&combat.Next++==firstId,"future-native-unit-identity-drift");
        });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_COMBAT_UNIT_FORMAT_AND_MEMENTO_ONLY",Tests=tests,
            NativeOstyCmdExecuted=false,NativeDoomTurnLoopExecuted=false,NativeSceneAndSubscriptionsExecuted=false,ColdFileStateRestorationExecuted=false}));
        Console.WriteLine($"PASS_COMBAT_UNIT_FORMAT_AND_MEMENTO_ONLY {tests.Count}");
    }
}
