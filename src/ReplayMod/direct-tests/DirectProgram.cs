using System.Reflection;
using ReplayMod;
if(args.Length>2&&args[1]=="operations"){OperationContracts.Run(args[0],args[2]);return;}
if(args.Length>2&&args[1]=="seek"){SeekContracts.Run(args[0],args[2]);return;}
if(args.Length>1&&args[1]=="queue"){QueueContracts.Run(args[0]);return;}
if(args.Length>2&&args[1]=="verify-queue"){QueueContracts.VerifyRecording(args[0],args[2]);return;}
if(args.Length>1&&args[1]=="log-writer"){await LogWriterContracts.Run(args[0]);return;}

if(args.Length>1&&args[1]=="presentation"){PresentationContracts.Run(args[0]);return;}
if(args.Length>1&&args[1]=="characters"){CharacterContracts.Run(args[0]);return;}
if(args.Length>1&&args[1]=="timing"){TimingContracts.Run(args[0]);return;}
if(args.Length>2&&args[1]=="verify-regent"){RegentRecordingContracts.Run(args[0],args[2]);return;}
if(args.Length>2&&args[1]=="verify-silent"){CharacterContracts.VerifyRecording(args[0],args[2],args.ElementAtOrDefault(3));return;}
if(args.Length>2&&args[1]=="decision-route"){DecisionRouteContracts.Run(args[0],args[2]);return;}

if(args.Length>2&&args[1]=="choice-process"){ChoiceProcessContracts.Run(args[0],args[2]);return;}
if(args.Length>2&&args[1]=="verify-choice-process"){ChoiceProcessContracts.VerifyRecording(args[0],args[2],args.ElementAtOrDefault(3));return;}

if(args.Length>1&&args[1]=="state"){StateContracts.Run(args[0]);return;}
if(args.Length>2&&args[1]=="choices"){ChoiceContracts.Run(args[0],args[2]);return;}
if(args.Length>2&&args[1]=="verify-choices"){ChoiceContracts.VerifyRecording(args[0],args[2]);return;}
if(args.Length>3&&args[1]=="verify-state"){StateContracts.VerifyRecording(args[0],args[2],args[3]);return;}
if(args.Length>1&&args[1]=="victory"){VictoryContracts.Run(args[0],args.ElementAtOrDefault(2));return;}
if(args.Length>1&&args[1]=="content"){ContentContracts.Run(args[0],args.ElementAtOrDefault(2));return;}
if(args.Length>2&&args[1]=="verify-content"){ContentContracts.VerifyExisting(args[0],args[2]);return;}
if(args.Length>2&&args[1]=="verify-victory"){VictoryContracts.VerifyExisting(args[0],args[2]);return;}

var tests=new List<string>();
void Need(bool ok){if(!ok)throw new Exception("direct-memento-assertion");}
void Test(string name,Action run){run();tests.Add(name);}
IdentityMemento Capture(Graph state)=>new([state],o=>o is Graph or Value or Generator,
    f=>!typeof(Delegate).IsAssignableFrom(f.FieldType)&&!f.FieldType.FullName!.StartsWith("System.Threading."),
    o=>o is Generator rng?RngRestore(rng):null);
Action RngRestore(Generator rng){var counter=rng.Counter;return ()=>rng.Counter=counter;}

Test("ordered-piles-and-shared-card-power-identities-survive-removal-and-field-replacement",()=>
{
    var state=new Graph();var strike=new Value{Amount=6};var power=new Value{Amount=2};
    state.Hand.Add(strike);state.Discard.Add(power);state.Alias=state.Hand;state.Powers["strength"]=power;power.Link=strike;strike.Link=power;
    var before=Capture(state);state.Hand.Clear();state.Discard.Reverse();state.Discard.Add(strike);state.Powers.Clear();power.Amount=9;strike.Amount=99;
    state.Alias=new List<Value>{new(){Amount=100}};before.Restore();
    Need(state.Hand.SequenceEqual(new[]{strike})&&state.Discard.SequenceEqual(new[]{power})&&ReferenceEquals(state.Hand,state.Alias));
    Need(state.Powers["strength"]==power&&power.Amount==2&&strike.Amount==6&&strike.Link==power&&power.Link==strike);
});
Test("independent-checkpoints-do-not-drift-after-repeated-restores-and-new-mutations",()=>
{
    var state=new Graph();var card=new Value{Amount=1};state.Hand.Add(card);state.Rng.Counter=1;var a=Capture(state);
    state.Hand.Clear();state.Discard.Add(card);card.Amount=2;state.Rng.Counter=2;var b=Capture(state);
    for(var i=0;i<3;i++)
    {
        a.Restore();Need(state.Hand.Single()==card&&state.Discard.Count==0&&card.Amount==1&&state.Rng.Counter==1);
        card.Amount=100;state.Hand.Clear();state.Rng.Counter=100;
        b.Restore();Need(state.Hand.Count==0&&state.Discard.Single()==card&&card.Amount==2&&state.Rng.Counter==2);
    }
});
Test("sets-arrays-and-struct-contained-reference-data-are-copied",()=>
{
    var state=new Graph();state.Set.UnionWith(new[]{1,2});var value=new Value{Amount=3};state.Array[0]=value;state.Wrapped=(4,new List<Value>{value});
    var before=Capture(state);state.Set.Clear();state.Set.Add(99);state.Array[0]=new(){Amount=99};state.Wrapped.Values.Clear();state.Wrapped=(99,new());value.Amount=99;
    before.Restore();Need(state.Set.SetEquals(new[]{1,2})&&state.Array[0]==value&&value.Amount==3&&state.Wrapped.Id==4&&state.Wrapped.Values.Single()==value);
});
Test("current-pending-turn-signal-is-kept-while-old-logical-turn-and-rng-are-restored",()=>
{
    var state=new Graph{Turn=1};state.Rng.Counter=10;var old=state.Signal;var before=Capture(state);
    old.SetResult();state.Signal=new();var current=state.Signal;state.Turn=2;state.Rng.Counter=22;
    before.Restore();Need(state.Turn==1&&state.Rng.Counter==10&&ReferenceEquals(current,state.Signal)&&!state.Signal.Task.IsCompleted);
    state.Signal.SetResult();Need(current.Task.IsCompletedSuccessfully&&old.Task.IsCompletedSuccessfully);
});
Test("removed-same-name-enemy-restores-original-identity-order-ai-and-survivor-death-effects",()=>
{
    var battle=new Battle();var front=new Foe{Id=1,Hp=26,Combat=battle,Move=new(){Amount=1}};
    var back=new Foe{Id=2,Hp=25,Combat=battle,Move=new(){Amount=2}};
    battle.Enemies.AddRange([front,back]);
    IdentityMemento Save()=>new(new object[]{battle,front,back},o=>o is Battle or Foe or Value,
        f=>!typeof(Delegate).IsAssignableFrom(f.FieldType));
    var before=Save();var firstMove=front.Move;
    void Kill()
    {
        front.Hp=0;front.Combat=null;front.Move=null;battle.Enemies.Remove(front);
        back.Strength=4;back.Ravenous=true;back.Move=new(){Amount=3};battle.AiCounter++;
    }
    Kill();var after=Save();
    for(var i=0;i<3;i++)
    {
        before.Restore();Need(battle.Enemies.SequenceEqual(new[]{front,back})&&ReferenceEquals(front.Combat,battle)&&front.Hp==26&&
            ReferenceEquals(front.Move,firstMove)&&back.Strength==0&&!back.Ravenous&&back.Move!.Amount==2&&battle.AiCounter==0);
        Kill();Need(battle.Enemies.Single()==back&&back.Id==2&&battle.AiCounter==1);
        front.Hp=99;after.Restore();Need(front.Hp==0&&front.Combat is null&&front.Move is null&&battle.Enemies.Single()==back&&back.Ravenous&&back.Strength==4);
    }
});
var root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
Test("explicit-known-recording-build-can-be-read-with-original-metadata-and-other-builds-stay-rejected",()=>
{
    var clip=new Clip(1,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","known-r1","scope",["ReplayMod:0.1.0"],
        "CHARACTER.IRONCLAD","seed",1,0,"ordinary","checkpoint","initial",[new(1,OperationKind.EndTurn,1,null,null,null,"next-turn")]);
    var store=new ClipStore(Path.Combine(root,"compatibility"));store.Seal(clip);var reopened=store.Read(clip.Id,"game","direct","scope","known-r1");
    Need(Codec.Bytes(clip).SequenceEqual(Codec.Bytes(reopened))&&reopened.ModHash=="known-r1");
    var rejected=false;try{store.Read(clip.Id,"game","direct","scope","another-build");}catch(InvalidDataException){rejected=true;}Need(rejected);
    rejected=false;try{Codec.Validate(clip with{ModHash=null!},"game","direct","scope");}catch(InvalidDataException){rejected=true;}Need(rejected);
});
File.WriteAllBytes(Path.Combine(root,"direct-contracts.json"),Codec.Bytes(new{Result="PASS_DIRECT_MEMENTO_CONTRACTS",Count=tests.Count,Tests=tests,RealGameExecuted=false,GameConstructorsExecuted=false}));
Console.WriteLine($"PASS_DIRECT_MEMENTO_CONTRACTS {tests.Count}");

sealed class Graph
{
    public readonly List<Value> Hand=[];
    public readonly List<Value> Discard=[];
    public List<Value> Alias=[];
    public readonly Dictionary<string,Value> Powers=[];
    public readonly HashSet<int> Set=[];
    public readonly Value?[] Array=new Value?[2];
    public (int Id,List<Value> Values) Wrapped=(0,new());
    public TaskCompletionSource Signal=new();
    public int Turn=1;
    public readonly Generator Rng=new();
}
sealed class Value {public int Amount;public Value? Link;}
sealed class Generator {public int Counter;}
sealed class Battle {public readonly List<Foe> Enemies=[];public int AiCounter;}
sealed class Foe
{
    public readonly string Model="same-model";
    public int Id,Hp,Strength;public bool Ravenous;
    public Battle? Combat;public Value? Move;
}
