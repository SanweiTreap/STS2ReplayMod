using System.Text.Json;
using ReplayMod;

internal static class RecordingContracts
{
    internal static void Run(string output)
    {
        var tests=new List<string>();var dir=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"library-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        void Need(bool value,string reason)=>Codec.Require(value,reason);
        void Test(string name,Action body){body();tests.Add(name);}
        var path=Path.Combine(dir,"journal.json");var journal=new RecordingJournal(path);
        const string save="{\"start_time\":100,\"seed\":\"same-seed\",\"save_time\":101,\"run_time\":5,\"num_reloads\":0,\"players\":[{\"hp\":70}],\"coords\":[1]}";
        string? first=null;
        Test("continue-matches-observed-actual-save-across-new-process-and-volatile-fields",()=>
        {
            journal.NewRun();first=journal.CurrentRun;journal.Observe(save);
            journal=new(path);journal.Continue("{\"coords\":[1],\"players\":[{\"hp\":70}],\"num_reloads\":1,\"run_time\":8,\"save_time\":200,\"seed\":\"same-seed\",\"start_time\":100}");
            Need(journal.ContinueMatched&&journal.CurrentRun==first,"continue-lost-real-run");
        });
        Test("three-independent-battles-and-reentry-have-stable-order-and-new-attempt",()=>
        {
            var a=journal.Begin(0,1,0,2,100,"map");var b=journal.Begin(0,3,1,4,100,"map");var c=journal.Begin(0,5,1,6,100,"map");
            journal=new(path);journal.Continue(save);var retry=journal.Begin(0,5,1,6,100,"continue-known");
            Need(a.Battle==1&&b.Battle==2&&c.Battle==3&&retry.Battle==3&&retry.Attempt==2&&retry.RunId==first,"battle-or-attempt-drift");
        });
        Test("different-new-runs-never-merge-on-seed-or-ambiguous-exact-save",()=>
        {
            journal.NewRun();Need(journal.CurrentRun!=first,"new-run-reused-guid");journal.Observe(save);
            journal=new(path);journal.Continue(save);Need(!journal.ContinueMatched&&journal.CurrentRun!=first,"ambiguous-runs-merged");
            journal.Continue(save.Replace("70","69"));Need(!journal.ContinueMatched,"resource-mismatch-guessed-from-seed");
        });
        Test("stored-save-readback-and-frozen-load-origin-survive-dto-mutation-before-setup",()=>
        {
            var bound=new RecordingJournal(Path.Combine(dir,"boundary-journal.json"));bound.NewRun();var run=bound.CurrentRun!;
            bound.Observe(save);var stored=save.Replace("70","68");bound.ObserveFor(run,stored);
            // The native writer can serialize a state differing from an earlier ToSave observation.
            // Freeze the actual load before later reconstruction mutates its source DTO.
            var frozen=stored;var laterDto=stored.Replace("68","66");
            Need(RecordingJournal.SaveKey(frozen)!=RecordingJournal.SaveKey(laterDto),"probe-does-not-distinguish-mutated-dto");
            bound=new(Path.Combine(dir,"boundary-journal.json"));bound.Continue(frozen);
            Need(bound.ContinueMatched&&bound.CurrentRun==run,"actual-stored-load-was-not-associated");
        });
        Test("completed-native-save-is-associated-with-captured-run-not-a-later-current-run",()=>
        {
            var bound=new RecordingJournal(Path.Combine(dir,"captured-run-journal.json"));bound.NewRun();var run=bound.CurrentRun!;
            bound.NewRun();var later=bound.CurrentRun;bound.ObserveFor(run,save);
            Need(bound.CurrentRun==later,"save-observer-changed-active-run");bound.Continue(save);
            Need(bound.ContinueMatched&&bound.CurrentRun==run,"save-observer-bound-wrong-run");
        });
        var projection=JsonSerializer.Serialize(new{Mechanics=new{OrbQueue=new OrbQueueState(0,0,[]),CombatUnits=new CombatUnitState(3,[new(0,"CHARACTER.IRONCLAD",null,true,70,80,0,null,null,[])],[])}},Codec.Json);
        var clip=new Clip(9,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],"CHARACTER.IRONCLAD","seed",5,0,"encounter","checkpoint",projection,
            [new(1,OperationKind.Card,1,0,"CARD.DEFEND_IRONCLAD",null,projection,CardState:new(1,0,new string('A',64),"Self"),Timing:new(10,20))],Choices:[],
            Timing:new("checkpoint-stopwatch-elapsed-realtime-v1",0,true,true,true),Battle:new(Guid.NewGuid().ToString("N"),3,2,0,6,100,"continue-known","interrupted"));
        var root=Path.Combine(dir,"clips");var store=new ClipStore(root);
        Test("later-floor-completed-prefix-is-truthful-fragment-without-forced-endturn",()=>
        {
            Codec.Validate(clip,"game","mod","scope");store.Seal(clip);var read=store.Read(clip.Id,"game","new-mod","scope","mod");
            Need(Codec.Bytes(read).SequenceEqual(Codec.Bytes(clip))&&read.Battle?.Completion=="interrupted"&&!read.Operations[^1].Victory,"fragment-falsified");
            var bad=clip with{Battle=clip.Battle! with{Completion="victory"}};var rejected=false;
            try{Codec.Validate(bad,"game","mod","scope");}catch(InvalidDataException){rejected=true;}Need(rejected,"false-victory-accepted");
        });
        Test("library-isolates-bad-file-and-reuses-metadata-without-reparsing-good-clips",()=>
        {
            File.WriteAllText(Path.Combine(root,Guid.NewGuid().ToString("N")+".json"),"broken");var reads=0;
            Clip Read(string id){reads++;return store.Read(id,"game","mod","scope");}
            var rows=RecordingLibrary.List(root,Read);Need(rows.Length==2&&rows.Count(r=>r.Problem is not null)==1&&rows.Single(r=>r.Id==clip.Id).Battle==clip.Battle,"bad-file-blocked-good-file");
            reads=0;rows=RecordingLibrary.List(root,Read);Need(reads==1&&rows.Length==2,"good-file-parsed-again-for-list");
        });
        File.WriteAllBytes(output,Codec.Bytes(new{Result="CONTINUOUS_RECORDING_JOURNAL_FORMAT_LIBRARY_CONTRACTS_PASS_NOT_NATIVE_RUNTIME",Tests=tests}));
    }
}
