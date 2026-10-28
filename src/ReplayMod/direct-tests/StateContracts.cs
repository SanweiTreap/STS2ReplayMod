using System.Reflection;
using System.Text.Json;
using ReplayMod;

internal static class StateContracts
{
    internal static void VerifyRecording(string output,string clipPath,string statePath)
    {
        var clipBytes=File.ReadAllBytes(clipPath);var stateBytes=File.ReadAllBytes(statePath);
        var envelope=JsonSerializer.Deserialize<ClipEnvelope>(clipBytes,Codec.Json)!;var clip=envelope.Data;
        Codec.Require(Codec.Hash(Codec.Bytes(clip))==envelope.SHA256,"actual-clip-integrity");
        Codec.Validate(clip,"0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9","STATE001-build-002-compatibility-check",clip.Scope,
            "FCE1E395BB83D5518F1B08B57FA5C09AFD46939A7D7C32A802C186918463CF10");
        var state=StateFile.Decode(stateBytes,clip);var graph=state.Graph;
        Codec.Require(graph.Bindings>0&&graph.Nodes.Length>graph.Bindings&&clip.Operations[^1].Victory,"actual-authority-not-nonzero-complete");
        var pcs=graph.Nodes.Single(n=>n.Type=="MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState");
        var player=graph.Nodes[1];var creatureRef=player.Fields.Single(f=>f.Name.EndsWith("/<Creature>k__BackingField",StringComparison.Ordinal)).Value.Ref;
        var creature=graph.Nodes[creatureRef];
        var energy=pcs.Fields.Single(f=>f.Name.EndsWith("/_energy",StringComparison.Ordinal)).Value.Text;
        var block=creature.Fields.Single(f=>f.Name.EndsWith("/_block",StringComparison.Ordinal)).Value.Text;
        using var after=JsonDocument.Parse(clip.Operations[0].After);var expected=after.RootElement.GetProperty("Player");
        Codec.Require(energy==expected.GetProperty("Energy").GetRawText()&&block==expected.GetProperty("Block").GetRawText(),"actual-state-player-data-differs");
        foreach(var type in new[]{"EnergySpentEntry","CardPlayStartedEntry","BlockGainedEntry","CardPlayFinishedEntry","CardPlay"})
            Codec.Require(graph.Nodes.Skip(graph.Bindings).Any(n=>n.Type.EndsWith("."+type,StringComparison.Ordinal)),"actual-state-history-missing");
        void Check(StateValue v)
        {
            if(v.Kind=="ref")Codec.Require(v.Ref>=0&&v.Ref<graph.Nodes.Length,"actual-state-reference");
            foreach(var f in v.Fields??[])Check(f.Value);
        }
        foreach(var n in graph.Nodes){foreach(var f in n.Fields)Check(f.Value);foreach(var v in n.Items)Check(v);}
        Codec.Require(File.ReadAllBytes(clipPath).SequenceEqual(clipBytes)&&File.ReadAllBytes(statePath).SequenceEqual(stateBytes),"recorded-files-changed");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="ACTUAL_NORMAL_RECORDING_FILES_VERIFIED_NOT_COLD_RESTORE",clip.Id,Operations=clip.Operations.Length,
            clip.Seed,clip.Col,StatePosition=state.Position,state.RecordingRuntime,state.ProcessId,state.ProcessStartedUtc,
            ClipSHA256=Codec.Hash(clipBytes),StateSHA256=Codec.Hash(stateBytes),StateBytes=stateBytes.Length,Nodes=graph.Nodes.Length,
            graph.Bindings,graph.BindingShape,Rngs=graph.Nodes.Count(n=>n.Kind=="special"),Energy=int.Parse(energy),Block=int.Parse(block),
            OriginalFilesUnchanged=true,GameConstructorsExecuted=false,ColdRestoreExecuted=false}));
        Console.WriteLine("ACTUAL_RECORDING_FILES_VERIFIED");
    }
    internal static void Run(string output)
    {
        var passed=new List<string>();
        void Need(bool value){if(!value)throw new Exception("persistent-state-contract");}
        bool Field(FieldInfo f)=>!typeof(Delegate).IsAssignableFrom(f.FieldType)&&!typeof(Task).IsAssignableFrom(f.FieldType)&&
            !f.FieldType.FullName!.StartsWith("System.Threading.",StringComparison.Ordinal)&&f.Name!="Lifetime";
        StateGraph Bind(Arena a)=>new([a],o=>o is Arena or Unit or Entry,Field,t=>t==typeof(Entry),extraTypes:[typeof(Entry)]);
        var source=new Arena(17);var sourceBinding=Bind(source);
        source.Energy=2;source.Block=5;source.Hand.RemoveAt(0);source.Discard.Add(source.Card);
        source.History.Add(new(source.Card,new(){[42]=1},(1,1m),source.History));
        source.Card.Link=source.Card;
        var frozen=sourceBinding.Freeze();var bytes=Codec.Bytes(frozen);
        // Mutate and abandon the recording context after the freeze; no later encoding can read it.
        source.Energy=999;source.Card.Amount=999;source.History.Clear();
        Need(bytes.SequenceEqual(Codec.Bytes(frozen)));passed.Add("freeze-is-detached-from-recording-objects");
        var viewer=new Arena(91);var bindings=Bind(viewer);var viewerCard=viewer.Card;var pendingSignal=viewer.Signal;
        StateGraphData Read()=>JsonSerializer.Deserialize<StateGraphData>(bytes,Codec.Json)!;
        bindings.Apply(Read());
        Need(viewer.Energy==2&&viewer.Block==5&&viewer.Hand.Count==0&&viewer.Discard.Single()==viewerCard&&viewerCard.Amount==6&&viewerCard.Link==viewerCard);
        Need(viewer.Lifetime==91&&ReferenceEquals(viewer.Signal,pendingSignal)&&!pendingSignal.Task.IsCompleted);
        var entry=viewer.History.Single();Need(entry.Card==viewerCard&&entry.Turns[42]==1&&entry.Cost==(1,1m)&&ReferenceEquals(entry.History,viewer.History));
        Need(Codec.Bytes(bindings.Freeze()).SequenceEqual(bytes));
        Need(!ReferenceEquals(entry,source.History.FirstOrDefault())&&!ReferenceEquals(viewerCard,source.Card));
        passed.Add("new-context-binds-own-objects-restores-piles-cycles-readonly-history-and-struct-fields");
        passed.Add("fresh-context-task-and-lifecycle-id-retained");
        var oldEntry=entry;viewer.Energy+=1;viewer.Block+=2;viewer.History.Add(entry);bindings.Apply(Read());
        Need(viewer.Energy==2&&viewer.Block==5&&viewer.History.Count==1&&!ReferenceEquals(viewer.History[0],oldEntry));
        passed.Add("independent-decode-recovery-replaces-history-without-duplication");
        var loaded=new Arena(999);loaded.Card.Id=new("card");var loadedGraph=Bind(loaded);
        Need(loadedGraph.Shape!=frozen.BindingShape&&loadedGraph.BindingCount!=frozen.Bindings);
        var cardId=Array.FindIndex(frozen.Nodes[0].Fields,f=>f.Name.EndsWith("/Card",StringComparison.Ordinal));
        var logical=loadedGraph.BindLogical(Read(),new Dictionary<int,object>{[0]=loaded,[frozen.Nodes[0].Fields[cardId].Value.Ref]=loaded.Card});
        var verify=loadedGraph.Apply(Read(),logical);verify();
        Need(loaded.Energy==2&&loaded.Block==5&&loaded.Discard.Single()==loaded.Card&&loaded.Hand.Count==0&&loaded.Lifetime==999&&loaded.History.Single().Card==loaded.Card);
        loaded.Energy++;loaded.Block+=2;verify=loadedGraph.Apply(Read(),loadedGraph.BindLogical(Read(),new Dictionary<int,object>{[0]=loaded,[frozen.Nodes[0].Fields[cardId].Value.Ref]=loaded.Card}));verify();
        Need(loaded.Energy==2&&loaded.Block==5&&loaded.History.Count==1&&loaded.Card.Id==loaded.Id);
        loaded.Energy++;loaded.Block+=2;
        try{verify();throw new Exception("readback-difference-not-detected");}catch(InvalidDataException){}
        try{loadedGraph.BindLogical(Read(),new Dictionary<int,object>{[0]=new Token("wrong-anchor")});throw new Exception("wrong-anchor-accepted");}catch(InvalidDataException){}
        passed.Add("logical-anchors-restore-across-deserialization-alias-and-allocation-topology-differences");
        var listViewer=new Arena(1000){Modifiers=new List<Modifier>()};var listGraph=Bind(listViewer);
        var listBindings=listGraph.BindLogical(Read(),new Dictionary<int,object>{[0]=listViewer,[frozen.Nodes[0].Fields[cardId].Value.Ref]=listViewer.Card});
        listGraph.Apply(Read(),listBindings)();
        Need(listViewer.Modifiers is Modifier[] {Length:0}&&listViewer.Energy==2&&listViewer.Block==5&&listViewer.Discard.Single()==listViewer.Card);
        passed.Add("known-interface-empty-array-restores-over-native-deserialized-list-without-untrusted-type-loading");
        void Reject(StateGraphData data)
        {
            viewer.Energy=123;viewer.Block=456;
            try{bindings.Apply(data);throw new Exception("invalid-graph-accepted");}catch(InvalidDataException){}
            Need(viewer.Energy==123&&viewer.Block==456);
        }
        Reject(Read() with{BindingShape="bad"});
        var changed=Read();var nodes=changed.Nodes.ToArray();nodes[^1]=nodes[^1] with{Type="System.Diagnostics.Process"};Reject(changed with{Nodes=nodes});
        nodes=changed.Nodes.ToArray();nodes[^1]=nodes[^1] with{Type="System.Diagnostics.Process[]",Kind="array"};Reject(changed with{Nodes=nodes});
        var bad=Read();var members=bad.Nodes[0].Fields.ToArray();var cardField=Array.FindIndex(members,f=>f.Name.EndsWith("/Card",StringComparison.Ordinal));
        Need(cardField>=0);members[cardField]=members[cardField] with{Value=new("ref",Ref:999999)};
        nodes=bad.Nodes.ToArray();nodes[0]=nodes[0] with{Fields=members};Reject(bad with{Nodes=nodes});
        passed.Add("bad-binding-unknown-type-and-dangling-reference-rejected-before-live-writes");
        var clip=new Clip(3,new string('a',32),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],"CHARACTER.IRONCLAD","seed",1,0,"encounter","checkpoint","initial",
            [new(1,OperationKind.Card,1,0,"CARD.DEFEND_IRONCLAD",null,"after",CardState:new(0,0,new string('F',64),"Self")),new(2,OperationKind.EndTurn,1,null,null,null,"next")],Authority:"STATE001-first-Defend-v1");
        var authority=new RecordedStateData(1,clip.Id,1,clip.GameHash,clip.ModHash,clip.Scope,StateFile.HashProjection("after"),"recording-runtime",100,DateTimeOffset.UtcNow,frozen);
        var wire=StateFile.Encode(authority);Need(StateFile.Decode(wire,clip).Graph.Nodes.Length==frozen.Nodes.Length);
        void BadFile(byte[] payload,Clip owner){try{StateFile.Decode(payload,owner);throw new Exception("invalid-state-file-accepted");}catch(InvalidDataException){}}
        BadFile(Codec.Bytes(new RecordedStateEnvelope(new string('0',64),authority)),clip);
        BadFile(StateFile.Encode(authority with{Version=2}),clip);
        BadFile(wire,clip with{Id=new string('b',32)});
        BadFile(wire,clip with{ModHash="other-build"});
        BadFile(StateFile.Encode(authority with{Position=2}),clip);
        passed.Add("state-file-roundtrip-checksum-version-and-clip-position-build-association");
        var cursor=new PlaybackCursor(9);cursor.ArriveFromFile(1);Need(cursor.Position==1&&!cursor.Playing&&!cursor.Busy);
        cursor.Toggle();Need(cursor.TryBegin()&&cursor.Position==1&&cursor.Playing);cursor.Complete();Need(cursor.Position==2&&cursor.Playing);
        var step=new PlaybackCursor(9);step.ArriveFromFile(1);step.Step();Need(step.TryBegin()&&!step.Playing);step.Complete();Need(step.Position==2&&!step.Playing);
        passed.Add("cold-arrival-and-original-play-step-intents-survive-recovery-boundary");
        File.WriteAllText(output,JsonSerializer.Serialize(new{Result="STATE001_PURE_DATA_CONTRACTS_PASS_NOT_GAME_RUNTIME",Count=passed.Count,Tests=passed},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("STATE_CONTRACTS_PASS "+passed.Count);
    }
    private sealed class Arena
    {
        public int Energy=3,Block;
        public readonly int Lifetime;
        public readonly Unit Card=new(){Amount=6};
        public readonly Token Id=new("card");
        public IReadOnlyList<Modifier> Modifiers=Array.Empty<Modifier>();
        public readonly List<Unit> Hand=[],Discard=[];
        public readonly List<Entry> History=[];
        public readonly TaskCompletionSource Signal=new();
        public Arena(int lifetime){Lifetime=lifetime;Hand.Add(Card);Card.Id=Id;}
    }
    private sealed record Token(string Value);
    private abstract class Modifier;
    private sealed class Unit { public int Amount;public Unit? Link;public Token? Id; }
    private sealed class Entry(Unit card,Dictionary<ulong,int> turns,(int Energy,decimal Value) cost,List<Entry> history)
    {
        public readonly Unit Card=card;
        public readonly Dictionary<ulong,int> Turns=turns;
        public readonly (int Energy,decimal Value) Cost=cost;
        public readonly List<Entry> History=history;
    }
}
