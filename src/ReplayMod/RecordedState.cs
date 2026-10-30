using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal sealed record RngData(int Counter,ulong State0,ulong State1,ulong State2,ulong State3);

// STATE-001 deliberately supports only the first completed, unupgraded Defend in player turn one.
internal static class RecordedState
{
    internal static bool Armed;
    internal static bool Recording;
    private static StateGraph? _recordGraph;
    private static Task? _write;
    private static bool _captured;
    private static readonly Type[] NewObjects=[typeof(EnergySpentEntry),typeof(CardPlayStartedEntry),typeof(CardPlayFinishedEntry),typeof(BlockGainedEntry),typeof(CardPlay),typeof(CardDrawnEntry),typeof(PowerReceivedEntry)];
    private static readonly Type[] AdditionalTypes=[..NewObjects,typeof(ResourceInfo),typeof(Dictionary<ulong,int>)];
    internal static bool Field(FieldInfo f)
    {
        if(!DirectCheckpoint.Field(f))return false;
        // A fresh turn-loop owns this ID. Localization caches are presentation data, not battle state.
        if(f.DeclaringType?.Name=="CombatTurnState"&&f.Name=="<Id>k__BackingField")return false;
        if(f.FieldType.Namespace=="MegaCrit.Sts2.Core.Localization")return false;
        return true;
    }
    private static string? Special(object value)
    {
        if(value is not Rng rng)return null;var r=rng.ToSerializable();
        return JsonSerializer.Serialize(new RngData(r.counter,r.state0,r.state1,r.state2,r.state3),Codec.Json);
    }
    private static Action PrepareSpecial(object value,string data)
    {
        var r=JsonSerializer.Deserialize<RngData>(data,Codec.Json)??throw new InvalidDataException("state-rng-empty");
        Codec.Require(value is Rng&&r.Counter>=0&&(r.State0|r.State1|r.State2|r.State3)!=0,"state-rng-invalid");
        return ()=>((Rng)value).LoadFromSerializable(new SerializableRng{counter=r.Counter,state0=r.State0,state1=r.State1,state2=r.State2,state3=r.State3});
    }
    internal static StateGraph Bind(CombatRoster roster)
    {
        // Deserialized deck cards can have fewer lazy cost caches than cards created through the new-run UI.
        // Materialize the same public data before binding; no cards are played and no RNG is consumed.
        foreach(var card in LiveState.Player.Deck.Cards.Concat(LiveState.Player.PlayerCombatState!.AllCards).Distinct())
        { _=card.EnergyCost;_=card.DynamicVars; }
        return new(DirectCheckpoint.Roots(roster),DirectCheckpoint.Data,Field,NewObjects.Contains,Special,PrepareSpecial,AdditionalTypes);
    }
    private static Dictionary<int,object> LogicalBindings(RecordedStateData state,StateGraph graph,CombatRoster roster)
    {
        var saved=state.Graph;var anchors=new Dictionary<int,object>();var roots=DirectCheckpoint.Roots(roster).ToArray();
        Codec.Require(roots.Length==8+roster.Initial.Length,"authority-root-layout");
        for(var i=0;i<roots.Length;i++)anchors.Add(i,roots[i]);
        int Ref(int owner,string suffix)
        {
            Codec.Require(owner>=0&&owner<saved.Nodes.Length,"authority-anchor-owner");
            var field=saved.Nodes[owner].Fields.SingleOrDefault(f=>f.Name.EndsWith("/"+suffix,StringComparison.Ordinal));
            Codec.Require(field?.Value.Kind=="ref"&&field.Value.Ref>=0&&field.Value.Ref<saved.Nodes.Length,"authority-anchor-field:"+suffix);
            return field!.Value.Ref;
        }
        void Card(int id,CardModel card)
        {
            Codec.Require(saved.Nodes[id].Type==card.GetType().FullName,"authority-card-model-anchor");
            Codec.Require(!anchors.TryGetValue(id,out var previous)||ReferenceEquals(previous,card),"authority-card-anchor-alias");anchors[id]=card;
        }
        var database=saved.Nodes[Ref(3,"_idToCard")];Codec.Require(database.Kind=="dictionary"&&database.Items.Length%2==0,"authority-card-db");
        for(var i=0;i<database.Items.Length;i+=2)
        {
            var key=database.Items[i];var value=database.Items[i+1];
            Codec.Require(key is {Kind:"scalar",Type:"System.UInt32"}&&value.Kind=="ref","authority-card-db-key");
            var card=NetCombatCardDb.Instance.GetCard(uint.Parse(key.Text,CultureInfo.InvariantCulture));Card(value.Ref,card);
        }
        var deck=saved.Nodes[Ref(Ref(1,"<Deck>k__BackingField"),"_cards")];var cards=LiveState.Player.Deck.Cards;
        Codec.Require(deck.Items.Length==cards.Count,"authority-deck-count");
        for(var i=0;i<cards.Count;i++){Codec.Require(deck.Items[i].Kind=="ref","authority-deck-card-reference");Card(deck.Items[i].Ref,cards[i]);}
        for(var i=0;i<roster.Initial.Length;i++)
        {
            var id=saved.Nodes[8+i].Fields.Single(f=>f.Name.EndsWith("/<CombatId>k__BackingField",StringComparison.Ordinal)).Value;
            Codec.Require(id.Kind=="scalar"&&id.Text==roster.Initial[i].CombatId!.Value.ToString(CultureInfo.InvariantCulture),"authority-creature-id-anchor");
        }
        var result=graph.BindLogical(saved,anchors);
        Runtime.Log("authority-logical-bindings",new{state.Clip,RecordedBindings=saved.Bindings,CurrentInitialBindings=graph.BindingCount,
            RecordedShape=saved.BindingShape,CurrentShape=graph.Shape,Anchors=anchors.Count,Bound=result.Count,
            Rule="fixed-roots-creature-ids-combat-card-ids-deck-order-then-fields-and-dictionary-keys",PrefixOperations=0});
        return result;
    }
    internal static void NewRun()
    {
        Recording=Armed;Armed=false;_recordGraph=null;_write=null;_captured=false;
        if(Recording)Codec.Require(LiveState.Player.Character.Id.ToString()=="CHARACTER.IRONCLAD","state-test-only-Ironclad");
    }
    internal static void Release(){Recording=false;_recordGraph=null;_write=null;_captured=false;_captureDiagnostics=null;}
    internal static void Begin()
    {
        if(!Recording)return;
        Codec.Require(LiveState.Turn==1&&LiveState.Player.Creature.Powers.Count==0,"state-test-requires-first-turn-no-powers");
        var at=Stopwatch.GetTimestamp();_recordGraph=Bind(new(((CombatRoom)LiveState.Run.CurrentRoom!).CombatState));
        Runtime.Log("authority-recording-bound",new{Bindings=_recordGraph.BindingCount,_recordGraph.Shape,Runtime.SessionId,Runtime.ProcessId,Runtime.ProcessStartedUtc,
            Milliseconds=Stopwatch.GetElapsedTime(at).TotalMilliseconds});
    }
    internal static void Append(Clip clip,Operation op,int position)
    {
        if(!Recording||_captured)return;
        Codec.Require(position==1&&op.Kind==OperationKind.Card&&op.Card=="CARD.DEFEND_IRONCLAD"&&op.CardState?.Upgrade==0&&op.Turn==1&&!op.Victory,
            "state-test-first-operation-must-be-unupgraded-Defend");
        Codec.Require(LiveState.Player.Creature.Powers.Count==0,"state-test-power-membership-changed");
        var at=Stopwatch.GetTimestamp();var frozen=_recordGraph!.Freeze();var freezeMs=Stopwatch.GetElapsedTime(at).TotalMilliseconds;
        var data=new RecordedStateData(1,clip.Id,position,clip.GameHash,clip.ModHash,clip.Scope,StateFile.HashProjection(op.After),Runtime.SessionId,Runtime.ProcessId,Runtime.ProcessStartedUtc,frozen);
        _captured=true;_recordGraph=null;
        // Everything captured above is detached data. The worker has no live objects or Godot references.
        var root=Runtime.Root;
        _write=Task.Run(()=>
        {
            var encodeAt=Stopwatch.GetTimestamp();var bytes=StateFile.Encode(data);
            var encodeMs=Stopwatch.GetElapsedTime(encodeAt).TotalMilliseconds;
            var dir=Path.Combine(root,"states");Directory.CreateDirectory(dir);var path=Path.Combine(dir,clip.Id+".state.json");
            var writeAt=Stopwatch.GetTimestamp();File.WriteAllBytes(path+".pending",bytes);File.Move(path+".pending",path,false);
            return (Bytes:bytes.Length,EncodeMs:encodeMs,WriteMs:Stopwatch.GetElapsedTime(writeAt).TotalMilliseconds);
        });
        // Completion diagnostics are emitted by Seal on the game thread, avoiding concurrent session log writes.
        _captureDiagnostics=new{clip.Id,Position=position,Nodes=frozen.Nodes.Length,Bindings=frozen.Bindings,FreezeMilliseconds=freezeMs,Origin="normal-recording"};
    }
    private static object? _captureDiagnostics;
    internal static void Seal(Clip clip)
    {
        if(!Recording)return;
        Codec.Require(_captured&&_write is not null,"authority-point-not-recorded");
        _write!.GetAwaiter().GetResult();
        var result=((Task<(int Bytes,double EncodeMs,double WriteMs)>)_write).Result;
        Runtime.Log("authority-state-written",new{Capture=_captureDiagnostics,result.Bytes,EncodeMilliseconds=result.EncodeMs,WriteMilliseconds=result.WriteMs,
            SourceRuntime=Runtime.SessionId,Runtime.ProcessId,Runtime.ProcessStartedUtc,IndependentFrozenData=true});
        // Read back and validate association before publishing the clip's latest pointer.
        Read(clip);Recording=false;
    }
    internal static RecordedStateData Read(Clip clip)
    {
        Codec.Require(clip.Authority=="STATE001-first-Defend-v1","clip-has-no-authority-state");
        var path=Path.Combine(Runtime.Root,"states",clip.Id+".state.json");
        Codec.Require(File.Exists(path)&&new FileInfo(path).Length is >0 and <=StateFile.Cap,"authority-file-missing-or-size");
        return StateFile.Decode(File.ReadAllBytes(path),clip);
    }
    internal static async Task Apply(Clip clip,StateGraph bindings,CombatRoster roster,string reason)
    {
        var at=Stopwatch.GetTimestamp();var state=Read(clip); // independent file read/parse for EVERY arrival or correction
        Codec.Require(state.RecordingRuntime!=Runtime.SessionId&&
            (state.ProcessId!=Runtime.ProcessId||state.ProcessStartedUtc!=Runtime.ProcessStartedUtc),"authority-test-requires-new-recording-process");
        Codec.Require(LiveState.Turn==1&&ActionMonitor.Pending is null,"authority-unsafe-context");
        var logical=LogicalBindings(state,bindings,roster);
        var ui=new DirectCheckpoint(state.Position,roster,captureData:false,projection:clip.Operations[0].After);
        var graphHash=Codec.Hash(Codec.Bytes(state.Graph));
        await ui.Restore(()=>
        {
            var verify=bindings.Apply(state.Graph,logical);verify();
            Runtime.Log("authority-data-verified",new{clip.Id,state.Position,Reason=reason,GraphHash=graphHash,DataFieldsAndContainersReadBackMatched=true,
                Rngs=state.Graph.Nodes.Count(n=>n.Kind=="special"),HistoryEntries=CombatManager.Instance.History.Entries.Select(e=>e.GetType().Name).ToArray(),
                StateReferencesBoundToCurrentContext=true,BeforeUiRefresh=true});
        });
        Codec.Require(LiveState.Capture()==clip.Operations[0].After,"authority-restored-state-mismatch");
        Runtime.Log("authority-file-applied",new{clip.Id,Position=state.Position,Reason=reason,FileSHA256=Codec.Hash(Codec.Bytes(state)),StateMatched=true,
            SourceRuntime=state.RecordingRuntime,SourceProcessId=state.ProcessId,SourceProcessStartedUtc=state.ProcessStartedUtc,
            Runtime.SessionId,Runtime.ProcessId,Runtime.ProcessStartedUtc,FreshDecode=true,InMemoryMementoUsed=false,PrefixOperations=0,RoomRebuilt=false,
            Nodes=state.Graph.Nodes.Length,Milliseconds=Stopwatch.GetElapsedTime(at).TotalMilliseconds});
    }
    internal static void Perturb()
    {
        Codec.Require(PlayerLoop.Active&&LiveState.Playable&&ActionMonitor.Pending is null,"authority-injection-unsafe-context");
        var pcs=LiveState.Player.PlayerCombatState!;var creature=LiveState.Player.Creature;
        var before=new{pcs.Energy,creature.Block};pcs.Energy+=1;
        var f=HarmonyLib.AccessTools.Field(creature.GetType(),"<Block>k__BackingField")??HarmonyLib.AccessTools.Field(creature.GetType(),"_block");
        Codec.Require(f is not null,"authority-injection-block-field");f!.SetValue(creature,creature.Block+2);
        Runtime.Log("authority-test-perturbed",new{Before=before,After=new{pcs.Energy,creature.Block},IndependentQuantities=new[]{"Energy","Block"},ExpectedUntouched=true});
    }
}
