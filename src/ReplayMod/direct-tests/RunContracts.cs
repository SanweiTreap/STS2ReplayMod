using System.Text.Json;
using System.Text.Json.Nodes;
using ReplayMod;

internal static class RunContracts
{
    internal static void Verify(string output,string root,string id,int? startSource=null)
    {
        var path=Path.Combine(root,id+".json");var before=File.ReadAllBytes(path);var raw=new RunStore(root).Read(id);var run=RunRoutes.Playback(raw);
        Codec.Require(run.Id==id,"run-id-mismatch");
        var gaps=RunStore.ResourceDiscontinuities(run);var anchor=run.Operations.FirstOrDefault(o=>o.Kind=="battle"&&o.Status=="complete"&&
            (startSource is null||(o.SourceSequence??o.Sequence)==startSource));
        Codec.Require(anchor is not null,"run-has-no-complete-battle-anchor");
        Codec.Require(!run.Operations.Any(o=>o.Sequence>=anchor!.Sequence&&(o.Kind=="gap"||o.Status!="complete")),"run-has-unrecorded-gap-after-anchor");
        Codec.Require(!gaps.Any(s=>s>anchor!.Sequence),"run-has-missing-resource-transition-after-anchor");
        var continuations=run.Continuations.Where(c=>c.PreFinished&&c.Visit>=anchor!.Visit&&c.BeforeOperation>=anchor.Sequence)
            .Select(c=>new{c.Visit,c.Attempt,BeforeOperation=RunStore.ContinueBefore(run,c),LegacyVisitBoundary=c.BeforeOperation is null,
                CompletedEventBattleSource=RunRoutes.CompletedEventBattle(run,c)?.SourceSequence,
                CompletedMapBattleSource=RunRoutes.CompletedMapBattle(run,c)?.SourceSequence}).ToArray();
        var destinations=run.Operations.Zip(run.Operations.Skip(1)).Where(p=>p.First.Kind=="map"&&p.First.Sequence>=anchor!.Sequence)
            .Select(p=>{var destination=RunRoutes.Destination(run,p.First,p.Second);
                return new{Operation=p.First.Sequence,destination.Visit.Id,destination.Visit.MapType,destination.Type,destination.Model};}).ToArray();
        Codec.Require(File.ReadAllBytes(path).SequenceEqual(before),"run-read-mutated-original");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        var result=startSource is null?"PASS_ACTUAL_RUN_PRODUCTION_READ_NOT_NATIVE_PLAYBACK":"PASS_ACTUAL_RUN_TAIL_PRODUCTION_READ_NOT_NATIVE_PLAYBACK";
        File.WriteAllBytes(output,Codec.Bytes(new{Result=result,run.Id,run.Format,
            OriginalSHA256=Codec.Hash(before),Visits=run.Visits.Count,Operations=run.Operations.Count,RecordedOperations=raw.Operations.Count,StartSequence=anchor!.Sequence,
            CoverageStartSource=anchor.SourceSequence??anchor.Sequence,ExplicitTail=startSource is not null,
            CoveredVisits=run.Operations.Where(o=>o.Sequence>=anchor.Sequence).Select(o=>o.Visit).Distinct().Count(),
            ResourceDiscontinuities=gaps,Continuations=continuations,Destinations=destinations,OriginalUnchanged=true,NativeRuntimeNotExecutedByThisCheck=true}));
        Console.WriteLine(result);
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
        Test("recorded-continue-position-is-preserved-instead-of-moving-it-to-first-reward",()=>
        {
            var point=new RunContinuation(DateTimeOffset.UtcNow,1,true,"native-save","native-continue",3);
            var continued=run with{Id=Guid.NewGuid().ToString("N"),Visits=[run.Visits[0] with{Entries=2}],
                Operations=run.Operations.Take(3).ToList(),Continuations=[point]};
            store.Save(continued);var read=store.Read(continued.Id);
            Need(read.Continuations.Single().BeforeOperation==3&&RunStore.ContinueBefore(read,read.Continuations.Single())==3,"explicit-continue-position-lost");
        });
        Test("legacy-continue-position-is-derived-only-for-one-completed-battle-and-one-reentry",()=>
        {
            var point=new RunContinuation(DateTimeOffset.UtcNow,1,true,"native-save","native-continue");
            var continued=run with{Id=Guid.NewGuid().ToString("N"),Visits=[run.Visits[0] with{Entries=2}],
                Operations=run.Operations.Take(3).ToList(),Continuations=[point]};
            store.Save(continued);var read=store.Read(continued.Id);
            Need(RunStore.ContinueBefore(read,point)==2&&!System.Text.Encoding.UTF8.GetString(Codec.Bytes(read)).Contains("BeforeOperation"),"legacy-continue-format-or-position-changed");
            var ambiguous=continued with{Visits=[continued.Visits[0] with{Entries=3}],Continuations=[point,point with{At=point.At.AddSeconds(1)}]};
            var rejected=false;try{_=RunStore.ContinueBefore(ambiguous,point);}catch(InvalidDataException){rejected=true;}
            Need(rejected,"ambiguous-legacy-continue-was-guessed");
        });
        Test("invalid-continue-positions-are-rejected-without-rejecting-unknown-legacy-origins",()=>
        {
            var rejected=0;
            foreach(var position in new[]{0,6,4})
            {
                var bad=run with{Id=Guid.NewGuid().ToString("N"),Continuations=[new(DateTimeOffset.UtcNow,1,true,"save","native-continue",position)]};
                try{store.Save(bad);}catch(InvalidDataException){rejected++;}
            }
            Need(rejected==3,"invalid-or-wrong-visit-continue-accepted");
            var unknown=run with{Id=Guid.NewGuid().ToString("N"),Visits=[],Operations=[],
                Continuations=[new(DateTimeOffset.UtcNow,0,false,"save","native-continue",1)]};
            store.Save(unknown);Need(store.Read(unknown.Id).Continuations.Single().Visit==0,"unknown-native-origin-was-invented-or-rejected");
        });
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
        Test("legacy-files-do-not-gain-invented-opening-inputs",()=>
        {
            store.Save(run);var read=store.Read(id);
            Need(read.Opening is null&&!JsonNode.Parse(Codec.Bytes(read))!.AsObject().ContainsKey("Opening"),"legacy-opening-invented");
        });
        Test("terminal-native-win-survives-forced-death-and-cleanup-while-save-exit-stays-resumable",()=>
        {
            var history=new List<RunLifecycleEvent>();
            RunLifecycleEvent Event(string kind)=>new(kind,DateTimeOffset.UtcNow,"actual-native-source",1,0,1,null,
                RunLifecycle.Terminal(kind)?"actual-native-state":null);
            Need(RunLifecycle.Append(history,Event("in-progress"))&&RunLifecycle.Append(history,Event("saved-exit"))&&
                !RunLifecycle.Terminal(history[^1].Kind),"save-exit-was-treated-as-terminal");
            RunLifecycle.Append(history,Event("continued"));RunLifecycle.Append(history,Event("victory"));
            Need(!RunLifecycle.Append(history,Event("death"))&&!RunLifecycle.Append(history,Event("interrupted"))&&
                RunLifecycle.Status(run with{Lifecycle=history})=="victory","victory-overwritten-by-forced-death-or-cleanup");
            foreach(var kind in new[]{"death","abandoned"})
            {
                var other=new List<RunLifecycleEvent>();RunLifecycle.Append(other,Event(kind));
                Need(RunLifecycle.Status(run with{Lifecycle=other})==kind,"native-death-and-abandon-confused");
            }
            var value=run with{Lifecycle=history};store.Save(value);Need(Codec.Bytes(store.Read(id)).SequenceEqual(Codec.Bytes(value)),"lifecycle-roundtrip-changed");
            Need(RunLifecycle.Status(run)=="unknown"&&!JsonNode.Parse(Codec.Bytes(run))!.AsObject().ContainsKey("Lifecycle"),"legacy-outcome-was-invented");
            var rejected=false;try{store.Save(run with{Lifecycle=[Event("victory") with{State=null}]});}
            catch(InvalidDataException ex) when(ex.Message=="run-lifecycle-invalid"){rejected=true;}
            Need(rejected,"terminal-kind-without-native-snapshot-accepted");
        });
        Test("final-event-result-requires-the-same-native-terminal-fact-and-remains-bound-to-raw-source-after-SL",()=>
        {
            var visit=new RoomVisit(1,2,16,15,3,"Victory","Event","EVENT.THE_ARCHITECT",1,"entered",null);
            var operation=new RunOperation(1,1,"event-choice",new(Option:"PROCEED"),"before","native-terminal-state","complete",[],Terminal:"victory");
            var fact=new RunLifecycleEvent("victory",DateTimeOffset.UtcNow,"native-OnEnded(true)",1,2,2,null,"native-terminal-state");
            var terminal=run with{Visits=[visit],Operations=[operation],Continuations=[],Lifecycle=[fact]};
            store.Save(terminal);Need(store.Read(id).Operations[0].Terminal=="victory","terminal-boundary-lost");
            Need(RunLifecycle.AtOperation(terminal,operation with{Sequence=42,SourceSequence=1})==fact,"terminal-matched-selected-instead-of-source-position");
            Need(RunLifecycle.AtOperation(terminal,operation with{SourceSequence=2}) is null&&
                RunLifecycle.AtOperation(terminal,operation with{Attempt=2}) is null,"terminal-from-another-position-or-attempt-used");
            void Reject(RunDocument value)
            {
                var rejected=false;try{store.Save(value);}catch(InvalidDataException ex) when(ex.Message=="run-terminal-operation-unsupported"){rejected=true;}
                Need(rejected,"unproven-final-result-accepted");
            }
            Reject(terminal with{Lifecycle=null});
            Reject(terminal with{Operations=[operation with{After="other-state"}]});
            Reject(terminal with{Visits=[visit with{RoomType="Boss",MapType="Boss"}]});
            Reject(terminal with{Lifecycle=[fact with{Kind="death"}]});
        });
        var opening=new RunStartInput(run.Character,"actual-seed",["ACT.UNDERDOCKS"],"Standard",0,"actual-progress","actual-prefs","NEW_LEAF");
        Test("new-run-inputs-roundtrip-with-preparation-and-account-inputs-without-changing-operations",()=>
        {
            var value=run with{Opening=opening};store.Save(value);var read=store.Read(id);
            Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(value))&&Codec.Bytes(read.Operations).SequenceEqual(Codec.Bytes(run.Operations)),"new-run-inputs-lost-or-operations-changed");
        });
        Test("invalid-new-run-inputs-do-not-become-playable-records",()=>
        {
            foreach(var bad in new[]{opening with{Character="CHARACTER.DEFECT"},opening with{Seed=""},opening with{Acts=[]},
                opening with{Acts=["CARD.STRIKE_IRONCLAD"]},opening with{GameMode="Daily"},opening with{Ascension=1},
                opening with{Progress=""},opening with{Prefs=""},opening with{NeowDebugOption="OTHER"}})
            {
                var rejected=false;try{store.Save(run with{Opening=bad});}catch(InvalidDataException){rejected=true;}
                Need(rejected,"invalid-opening-input-accepted");
            }
        });
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_RUN_METADATA_AND_FILE_CONTRACTS_NOT_NATIVE_RUNTIME",Tests=passed}));
        Console.WriteLine("PASS_RUN_METADATA_FILE_CONTRACTS_NOT_NATIVE_RUNTIME");
    }
}
