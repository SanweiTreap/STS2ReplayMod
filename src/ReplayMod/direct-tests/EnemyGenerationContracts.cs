using System.Text.Json;
using ReplayMod;

internal static class EnemyGenerationContracts
{
    private sealed class Unit(uint id,string model){internal uint Id=id;internal string Model=model;internal int Hp=20;internal int Ai;internal bool Reviving;}
    private sealed class Battle
    {
        internal uint Next=4;internal int Rng;internal readonly List<Unit> Units=[];
        internal readonly List<Unit> Escaped=[];
        internal readonly List<string> Deck=["stolen-card"];
        internal readonly List<string> Rewards=[];
    }
    internal static void Run(string output)
    {
        var tests=new List<string>();void Test(string name,Action body){body();tests.Add(name);}void Need(bool yes,string why)=>Codec.Require(yes,why);
        var battle=new Battle();var player=new Unit(0,"player");var a=new Unit(1,"rat");var b=new Unit(2,"rat");var c=new Unit(3,"rat");battle.Units.AddRange([player,a,b,c]);
        var ids=new CombatIdentityRegistry<Unit>(battle.Next,battle.Units,u=>u.Id);
        IdentityMemento Save()=>new([battle,ids],o=>o is Battle or Unit or CombatIdentityRegistry<Unit>,_=>true);
        bool Matches()=>ids.Matches(battle.Next,battle.Units,u=>u.Id,u=>u.Hp>0,battle.Escaped);
        Unit Spawn(){var unit=new Unit(battle.Next++,"rat");ids.Admit(unit,unit.Id);battle.Units.Insert(1,unit);battle.Rng++;foreach(var u in battle.Units)u.Ai++;return unit;}
        var before=Save();var first=Spawn();var after=Save();
        Test("native-slot-reordering-keeps-same-model-targets-bound-by-stable-id",()=>
        {
            Need(Matches()&&ReferenceEquals(ids.Resolve(battle.Units,2,u=>u.Model,"rat"),b)&&
                ReferenceEquals(ids.Resolve(battle.Units,4,u=>u.Model,"rat"),first),"reordered-same-model-target-wrong");
        });
        Test("cross-generation-memento-restores-members-ai-rng-and-future-allocator-before-native-new-object",()=>
        {
            for(var i=0;i<4;i++)
            {
                before.Restore();Need(Matches()&&battle.Next==4&&battle.Rng==0&&battle.Units.SequenceEqual(new[]{player,a,b,c})&&a.Ai==0&&!ids.Known.Contains(first),"before-generation-identity-or-data-drift");
                var other=Spawn();Need(other.Id==4&&!ReferenceEquals(other,first)&&Matches()&&ReferenceEquals(ids.Resolve(battle.Units,4,u=>u.Model,"rat"),other),"future-id-did-not-follow-new-native-instance");
                after.Restore();Need(Matches()&&battle.Next==5&&battle.Rng==1&&a.Ai==1&&ReferenceEquals(ids.Resolve(battle.Units,4,u=>u.Model,"rat"),first)&&!ids.Known.Contains(other),"visited-post-summon-point-lost-its-own-instance");
            }
        });
        Test("removed-generated-unit-does-not-rewind-allocator-until-earlier-point-is-restored",()=>
        {
            first.Hp=0;battle.Units.Remove(first);Need(Matches()&&battle.Next==5,"dead-generated-unit-invalidated-native-counter");
            var second=Spawn();Need(second.Id==5&&Matches(),"next-native-summon-reused-a-dead-id");
            before.Restore();Need(Matches()&&battle.Next==4&&!ids.Known.Contains(second),"earlier-point-kept-future-allocation");
        });
        Test("live-absence-requires-a-distinct-known-native-escape-and-reserves-its-id",()=>
        {
            before.Restore();
            battle.Units.Remove(b);
            Need(!Matches(),"unregistered-living-absence-accepted");
            battle.Escaped.Add(b);Need(Matches()&&battle.Next==4,"native-escape-lost-identity");
            battle.Escaped.Add(b);Need(!Matches(),"duplicate-escape-accepted");battle.Escaped.RemoveAt(1);
            battle.Units.Add(b);Need(!Matches(),"escaped-unit-still-targetable");battle.Units.Remove(b);
            battle.Escaped[0]=new(b.Id,b.Model);Need(!Matches(),"foreign-escape-with-same-id-accepted");
            battle.Escaped[0]=b;b.Hp=0;Need(!Matches(),"dead-unit-accepted-as-live-escape");
            before.Restore();
        });
        Test("escape-and-kill-branches-restore-their-own-deck-and-reward-contents",()=>
        {
            before.Restore();
            battle.Deck.Clear();var stolen=Save();
            battle.Units.Remove(b);battle.Escaped.Add(b);var escaped=Save();
            stolen.Restore();b.Hp=0;battle.Units.Remove(b);battle.Rewards.Add("returned-card");var killed=Save();
            for(var i=0;i<3;i++)
            {
                before.Restore();Need(Matches()&&battle.Units.Contains(b)&&battle.Deck.Count==1&&battle.Rewards.Count==0,"pre-theft-point-drift");
                escaped.Restore();Need(Matches()&&b.Hp>0&&battle.Escaped.SequenceEqual(new[]{b})&&battle.Deck.Count==0&&battle.Rewards.Count==0,"escape-point-drift");
                killed.Restore();Need(Matches()&&b.Hp==0&&battle.Escaped.Count==0&&battle.Rewards.SequenceEqual(new[]{"returned-card"}),"kill-reward-point-drift");
            }
            before.Restore();
        });
        Test("retained-knockout-and-revival-preserve-the-same-target-identity",()=>
        {
            before.Restore();b.Hp=0;b.Reviving=true;b.Ai=1;var waiting=Save();
            b.Hp=20;b.Reviving=false;b.Ai=2;var revived=Save();
            before.Restore();Need(Matches()&&b.Hp==20&&!b.Reviving&&b.Ai==0,"pre-knockout-point-drift");
            waiting.Restore();Need(Matches()&&b.Hp==0&&b.Reviving&&b.Ai==1&&battle.Units.Contains(b),"retained-knockout-lost");
            revived.Restore();Need(Matches()&&ReferenceEquals(ids.Resolve(battle.Units,b.Id,u=>u.Model,b.Model),b)&&b.Hp==20&&!b.Reviving&&b.Ai==2,"revival-point-drift");
        });
        UnitRngState Rng()=>new(0,1,2,3,4);
        EnemyUnitEntry Enemy(uint id,string slot)=>new(id,"MONSTER.TWO_TAILED_RAT",slot,true,20,20,0,"CALL_FOR_BACKUP_MOVE",Rng(),[],"CALL_FOR_BACKUP_MOVE",true,["SCRATCH_MOVE"],new(){{"TwoTailedRat._turnsUntilSummonable","0"},{"TwoTailedRat._callForBackupCount","1"}});
        var units=new CombatUnitState(6,[new(0,"CHARACTER.IRONCLAD",null,true,70,80,0,null,null,[])],[],[Enemy(5,"first"),Enemy(2,"second"),Enemy(3,"third")]);
        string Projection(CombatUnitState value)=>JsonSerializer.Serialize(new{Mechanics=new{OrbQueue=new OrbQueueState(0,0,[]),CombatUnits=value}},Codec.Json);
        var projection=Projection(units);var clip=new Clip(10,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],"CHARACTER.IRONCLAD","seed",5,0,"rats","checkpoint",projection,
            [new(1,OperationKind.EndTurn,1,null,null,null,projection,Timing:new(10,20))],Choices:[],Timing:new("checkpoint-stopwatch-elapsed-realtime-v1",0,true,true,true),
            Battle:new(Guid.NewGuid().ToString("N"),3,1,0,6,100,"map","interrupted"));
        void Validate(Clip value)=>Codec.Validate(value,"game","mod","scope");
        Test("format10-preserves-ordered-enemy-identities-slots-ai-counters-and-rng-through-production-reader",()=>
        {
            Validate(clip);var root=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"enemy-clips-"+Guid.NewGuid().ToString("N"));var store=new ClipStore(root);store.Seal(clip);
            var read=store.Read(clip.Id,"game","mod","scope");Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(clip)),"enemy-file-readback-differs");
        });
        Test("duplicate-enemy-ids-foreign-player-id-and-future-allocator-regression-are-rejected",()=>
        {
            foreach(var bad in new[]{units with{NextIdentity=5},units with{Enemies=[Enemy(2,"first"),Enemy(2,"second")]},units with{Enemies=[Enemy(0,"first")]}})
            {
                var rejected=false;try{Validate(clip with{Initial=Projection(bad)});}catch(InvalidDataException){rejected=true;}Need(rejected,"invalid-enemy-roster-accepted");
            }
        });
        File.WriteAllBytes(output,Codec.Bytes(new{Result="ENEMY_GENERATION_IDENTITY_MEMENTO_AND_FORMAT_CONTRACTS_PASS_NOT_NATIVE_RUNTIME",Tests=tests}));
    }
}
