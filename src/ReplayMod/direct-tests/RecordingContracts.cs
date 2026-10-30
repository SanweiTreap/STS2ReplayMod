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
        var repairPath=Path.Combine(dir,"corrected-journal.json");var corrected=new RecordingJournal(repairPath);
        corrected.NewRun();var originalRun=corrected.CurrentRun!;corrected.Observe(save);corrected.Begin(0,1,0,2,100,"map");
        corrected.NewRun();var splitRun=corrected.CurrentRun!;var splitSave=save.Replace("70","65");corrected.Observe(splitSave);
        corrected.Begin(0,3,0,4,100,"map");var oldThird=corrected.Begin(0,5,0,6,100,"map");
        Test("explicit-user-confirmed-old-split-displays-three-battles-and-continues-canonical-counter",()=>
        {
            corrected.CorrectContinuation(splitRun,originalRun,"user-confirmed-same-run-with-save-load-at-question-room");
            Need(corrected.Display(oldThird).RunId==originalRun&&corrected.Display(oldThird).Battle==3,"old-split-display-not-corrected");
            corrected=new(repairPath);corrected.Continue(splitSave);
            Need(corrected.ContinueMatched&&corrected.CurrentRun==originalRun,"continue-did-not-resolve-explicit-correction");
            Need(corrected.Begin(0,7,0,8,100,"map").Battle==4,"future-battle-counter-restarted-after-correction");
            corrected.ObserveFor(splitRun,splitSave);corrected=new(repairPath);corrected.Continue(splitSave);
            Need(corrected.ContinueMatched&&corrected.CurrentRun==originalRun,"alias-save-became-falsely-ambiguous");
            corrected.CorrectContinuation(splitRun,originalRun,"repeat-same-user-confirmed-correction");
            Need(corrected.Display(oldThird).Battle==3,"repeated-correction-shifted-old-battles-again");
        });
        Test("library-correction-never-rewrites-sealed-clip-or-its-original-recorded-group",()=>
        {
            var raw=clip with{Battle=oldThird with{Completion="interrupted"}};var folder=Path.Combine(dir,"corrected-clips");var immutable=new ClipStore(folder);immutable.Seal(raw);
            var before=File.ReadAllBytes(Path.Combine(folder,raw.Id+".json"));
            var view=RecordingLibrary.List(folder,id=>immutable.Read(id,"game","mod","scope"),corrected.Display).Single();
            Need(view.Battle?.RunId==originalRun&&view.Battle.Battle==3&&
                immutable.Read(raw.Id,"game","mod","scope").Battle==raw.Battle&&File.ReadAllBytes(Path.Combine(folder,raw.Id+".json")).SequenceEqual(before),"view-repair-rewrote-original-recording");
        });
        Test("correction-refuses-overlapping-room-attempts-and-keeps-distinct-new-runs",()=>
        {
            var j=new RecordingJournal(Path.Combine(dir,"overlapping-journal.json"));j.NewRun();var firstId=j.CurrentRun!;j.Begin(0,1,0,2,100,"map");
            j.NewRun();var secondId=j.CurrentRun!;j.Begin(0,1,0,2,100,"map");var rejected=false;
            try{j.CorrectContinuation(secondId,firstId,"explicit-but-unsupported-overlapping-attempt-correction");}catch(InvalidDataException){rejected=true;}
            Need(rejected&&j.CurrentRun==secondId&&firstId!=secondId,"ambiguous-overlap-was-silently-merged");
        });
        Test("raw-save-and-native-load-default-representation-are-both-explicitly-observed-not-field-ignored",()=>
        {
            var jpath=Path.Combine(dir,"load-defaults.json");var j=new RecordingJournal(jpath);j.NewRun();var id=j.CurrentRun!;
            var raw="{\"start_time\":100,\"map\":[{\"coord\":1}]}";var loaded="{\"start_time\":100,\"map\":[{\"coord\":1,\"can_modify\":true}]}";
            Need(RecordingJournal.SaveKey(raw)!=RecordingJournal.SaveKey(loaded),"gameplay-field-was-ignored");
            j.ObserveFor(id,raw);j.ObserveFor(id,loaded);j=new(jpath);j.Continue(loaded);
            Need(j.ContinueMatched&&j.CurrentRun==id,"native-read-representation-not-bound");
            j.Continue(loaded.Replace("true","false"));Need(!j.ContinueMatched,"unobserved-gameplay-change-was-guessed");
        });
        Test("explicit-single-room-reentry-correction-preserves-attempt-order-and-next-counter-across-reload",()=>
        {
            var jpath=Path.Combine(dir,"same-room-repair.json");var j=new RecordingJournal(jpath);j.NewRun();var firstId=j.CurrentRun!;
            var firstBattle=j.Begin(0,1,4,2,100,"map");j.NewRun();var alias=j.CurrentRun!;j.Observe(save);
            var raw=j.Begin(0,1,4,2,100,"continue-unmatched");
            j.CorrectReentry(alias,firstId,"explicit-runtime-two-processes-same-native-checkpoint-and-load-default-diff");
            j.CorrectReentry(alias,firstId,"idempotent-explicit-runtime-reentry-correction");j=new(jpath);j.Continue(save);
            var displayed=j.Display(raw);Need(displayed.RunId==firstId&&displayed.Battle==1&&displayed.Attempt==2&&j.Display(firstBattle)==firstBattle,"same-room-attempt-repair-wrong");
            Need(j.ContinueMatched&&j.CurrentRun==firstId&&j.Begin(0,1,4,2,100,"continue-known").Attempt==3,"next-reentry-did-not-inherit-count");
            Need(j.Begin(0,3,4,4,100,"map").Battle==2,"next-room-count-wrong");
        });
        Test("explicit-reentry-correction-refuses-different-rooms-and-multi-room-alias",()=>
        {
            var j=new RecordingJournal(Path.Combine(dir,"wrong-room-repair.json"));j.NewRun();var firstId=j.CurrentRun!;j.Begin(0,1,0,2,100,"map");
            j.NewRun();var alias=j.CurrentRun!;j.Begin(0,2,0,3,100,"map");var rejected=false;
            try{j.CorrectReentry(alias,firstId,"explicit-but-different-room-must-not-merge");}catch(InvalidDataException){rejected=true;}
            Need(rejected&&j.CurrentRun==alias,"different-room-reentry-was-accepted");j.Begin(0,1,0,2,100,"map");rejected=false;
            try{j.CorrectReentry(alias,firstId,"explicit-but-multi-room-alias-is-not-supported");}catch(InvalidDataException){rejected=true;}
            Need(rejected,"multi-room-alias-reentry-was-accepted");
        });
        File.WriteAllBytes(output,Codec.Bytes(new{Result="CONTINUOUS_RECORDING_JOURNAL_FORMAT_LIBRARY_CONTRACTS_PASS_NOT_NATIVE_RUNTIME",Tests=tests}));
    }
}
