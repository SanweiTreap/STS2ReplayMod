using ReplayMod;

internal static class RunBranchContracts
{
    internal static void Run(string output)
    {
        var tests=new List<string>();
        void Test(string name,Action run){run();tests.Add(name);}
        void Need(bool value,string why)=>Codec.Require(value,why);
        RunDocument Fresh()=>new(1,Guid.NewGuid().ToString("N"),"game","mod","scope","CHARACTER.IRONCLAD",DateTimeOffset.UtcNow,
            [new(1,0,2,1,0,"Monster","Monster",null,1,"left",null),new(2,0,3,2,0,"Treasure","Treasure",null,1,"entered",null)],[],[],new());
        RunOperation Add(RunDocument run,int visit,string kind,string content)
        {
            var operation=new RunOperation(run.Operations.Count+1,visit,kind,new(Option:content),null,null,"complete",[],
                kind=="battle"?"clip":null,Attempt:run.Branches!.ActiveAttempt);
            RunRoutes.Append(run,operation);return operation;
        }
        var run=Fresh();RunRoutes.StartAttempt(run,1,null,null,"entry");Add(run,1,"battle","victory");Add(run,1,"map","chest");
        Test("act-transition-keeps-boss-cause-and-new-act-visit-through-observed-load",()=>
        {
            var sample=Fresh();
            sample.Visits[0]=sample.Visits[0] with{MapType="Boss",RoomType="Boss",Row=16,Floor=17,ActModel="ACT.UNDERDOCKS"};
            sample.Visits[1]=sample.Visits[1] with{Act=1,MapType="Ancient",RoomType="Event",Row=0,Floor=1,ActModel="ACT.HIVE",Model="EVENT.OROBAS"};
            RunRoutes.StartAttempt(sample,1,null,null,"entry");Add(sample,1,"battle","boss-victory");
            Add(sample,1,"reward","actual-native-claim");Add(sample,1,"act-proceed","native-vote-result");
            var travel=Add(sample,1,"map","next-act-start") with{Argument=new(0,0)};sample.Operations[3]=travel;
            RunRoutes.StartAttempt(sample,2,1,null,"entry");sample.Branches!.Attempts[1]=sample.Branches.Attempts[1] with{RoomType="Event",Model="EVENT.OROBAS"};
            var entry=RunRoutes.Observe(sample,2,2,[1,2,3,4],"act2-disk","act2-load","actual-save",false);
            Add(sample,2,"event-choice","old-choice");
            Need(RunRoutes.Restore(sample,"act2-load")?.Id==entry.Id,"act2-load-lost-actual-anchor");
            sample.Continuations.Add(new(DateTimeOffset.UtcNow,2,false,"actual-save","native-continue",6,sample.Branches.ActiveAttempt,"actual-progress"));
            Add(sample,2,"event-choice","new-choice");var view=RunRoutes.Playback(sample);
            var target=RunRoutes.Destination(view,view.Operations[3],view.Operations[4]);
            Need(view.Operations.Select(o=>o.SourceSequence).SequenceEqual(new int?[]{1,2,3,4,6})&&
                view.Operations.Count(o=>o.Kind=="act-proceed")==1&&view.Operations[2].Visit==1&&target.Visit.Act==1&&
                target.Visit.ActModel=="ACT.HIVE"&&target.Model=="EVENT.OROBAS"&&
                RunRoutes.ProgressBefore(view,view.Continuations.Single())==4,"act-cause-duplicated-or-entry-load-context-misplaced");
        });
        Test("completed-map-load-requires-observed-save-of-the-verified-battle-not-resource-similarity",()=>
        {
            foreach(var kind in new[]{"Monster","Elite","Boss"})
            {
                var sample=Fresh();sample.Visits[0]=sample.Visits[0] with{MapType=kind,RoomType=kind};
                RunRoutes.StartAttempt(sample,1,null,null,"entry");var battle=Add(sample,1,"battle","victory");
                sample.Operations[0]=battle with{Battle=new(sample.Id,1,1,0,2,100,"map","victory")};
                var saved=RunRoutes.Observe(sample,1,1,[1],"disk-before-progress","load-before-progress","key",true);
                Add(sample,1,"reward","old");RunRoutes.Restore(sample,"load-before-progress");
                var point=new RunContinuation(DateTimeOffset.UtcNow,1,true,"key","native-continue",3,sample.Branches!.ActiveAttempt);
                sample.Continuations.Add(point);Add(sample,1,"reward","actual-after-load");var view=RunRoutes.Playback(sample);
                Need(RunRoutes.CompletedMapBattle(view,view.Continuations.Single())?.SourceSequence==1&&
                    RunStore.ContinueBefore(view,view.Continuations.Single())==2&&saved.Path.SequenceEqual(new[]{1}),"completed-map-source-not-bound-to-actual-save");
                var altered=view with{Operations=view.Operations.Select(o=>o.Sequence==1?o with{Battle=o.Battle! with{Act=1}}:o).ToList()};
                var rejected=false;try{_=RunRoutes.CompletedMapBattle(altered,altered.Continuations.Single());}
                catch(InvalidDataException ex) when(ex.Message=="run-completed-map-load-anchor-missing"){rejected=true;}
                Need(rejected,"other-act-battle-used-as-load-boundary");
                Need(RunRoutes.CompletedMapBattle(view,view.Continuations.Single() with{PreFinished=false}) is null,"unfinished-load-guessed-as-completed");
            }
        });
        RunRoutes.StartAttempt(run,2,1,null,"entry");var point=RunRoutes.Observe(run,2,2,[1,2],"disk","load","semantic",false);
        Add(run,2,"treasure-open","offer");Add(run,2,"treasure-pick","A");
        var original=Codec.Bytes(run.Operations);
        Test("load-uses-successfully-observed-position-and-keeps-abandoned-A-as-raw-fact",()=>
        {
            Need(RunRoutes.Restore(run,"load")?.Id==point.Id,"native-load-point-not-matched");
            Need(run.Branches!.Selected.SequenceEqual(new[]{1,2})&&Codec.Bytes(run.Operations).SequenceEqual(original),"rolled-back-operation-deleted-or-still-selected");
            Add(run,2,"treasure-open","offer");Add(run,2,"treasure-pick","B");
            var view=RunRoutes.Playback(run);
            Need(view.Operations.Select(o=>o.SourceSequence).SequenceEqual(new int?[]{1,2,5,6})&&
                view.Operations.Last().Argument.Option=="B"&&run.Operations[3].Argument.Option=="A","final-route-executes-A-and-B-or-loses-old-A");
        });
        Test("repeating-identical-action-is-another-attempt-not-content-deduplication",()=>
        {
            var firstParent=run.Branches!.ActiveAttempt;
            RunRoutes.Observe(run,2,firstParent,[1,2],"disk","load","semantic",false);
            RunRoutes.Restore(run,"load");Add(run,2,"treasure-open","offer");Add(run,2,"treasure-pick","B");
            Need(run.Operations.Count==8&&run.Branches.Attempts.Count==4&&run.Branches.Attempts[^1].Parent==firstParent&&
                RunRoutes.Playback(run).Operations.Select(o=>o.SourceSequence).SequenceEqual(new int?[]{1,2,7,8}),"identical-repeat-was-merged-or-old-B-executed-twice");
        });
        Test("a-semantic-key-or-similar-resources-cannot-substitute-for-native-load-fingerprint",()=>
        {
            var selected=run.Branches!.Selected.ToArray();var count=run.Branches.Attempts.Count;
            Need(RunRoutes.Restore(run,"semantic") is null&&run.Branches.Selected.SequenceEqual(selected)&&run.Branches.Attempts.Count==count,"unobserved-save-guessed");
            const string a="{\"players\":[{\"gold\":99}],\"num_reloads\":1}";
            const string b="{\"players\":[{\"gold\":99}],\"num_reloads\":2}";
            Need(RecordingJournal.SaveKey(a)==RecordingJournal.SaveKey(b)&&RecordingJournal.NativeFingerprint(a)!=RecordingJournal.NativeFingerprint(b),"position-fingerprint-discarded-native-load-fields");
        });
        Test("identical-load-fingerprint-with-different-persisted-prefix-is-ambiguous",()=>
        {
            RunRoutes.Observe(run,2,run.Branches!.ActiveAttempt,[1,2,7,8],"other","load","semantic",false);
            var selected=run.Branches.Selected.ToArray();var rejected=false;
            try{RunRoutes.Restore(run,"load");}catch(InvalidDataException){rejected=true;}
            Need(rejected&&run.Branches.Selected.SequenceEqual(selected),"ambiguous-point-selected-by-time-or-content");
            run.Branches.Saves.RemoveAt(run.Branches.Saves.Count-1);
        });
        Test("selected-path-projection-is-read-only-and-keeps-raw-attempt-and-save-ownership",()=>
        {
            var before=Codec.Bytes(run);RunRoutes.Validate(run);_=RunRoutes.Playback(run);
            Need(Codec.Bytes(run).SequenceEqual(before)&&run.Branches!.Attempts[^1].RestoredSave==2,"projection-mutated-raw-evidence");
        });
        Test("saved-branch-envelope-keeps-raw-facts-and-final-selection-across-process-read",()=>
        {
            var folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"branch-store-"+Guid.NewGuid().ToString("N"));
            var store=new RunStore(folder);var before=Codec.Bytes(run);store.Save(run);var restored=store.Read(run.Id);
            Need(Codec.Bytes(restored).SequenceEqual(before)&&RunRoutes.Playback(restored).Operations.Select(o=>o.SourceSequence)
                .SequenceEqual(new int?[]{1,2,7,8}),"persisted-branch-or-raw-facts-lost");
        });
        Test("completed-result-continue-is-positioned-on-the-chosen-followup-not-rolled-back-reward",()=>
        {
            var complete=Fresh();RunRoutes.StartAttempt(complete,1,null,null,"entry");Add(complete,1,"battle","victory");
            RunRoutes.Observe(complete,1,1,[1],"won-disk","won-load","semantic",true);Add(complete,1,"reward","abandoned");
            RunRoutes.Restore(complete,"won-load");
            complete.Continuations.Add(new(DateTimeOffset.UtcNow,1,true,"semantic","native-continue",3,complete.Branches!.ActiveAttempt));
            Add(complete,1,"reward","continued");var view=RunRoutes.Playback(complete);
            Need(view.Operations.Count==2&&view.Continuations.Single().BeforeOperation==2&&view.Operations[1].SourceSequence==3,"continue-position-reindexed-incorrectly");
        });
        Test("load-progress-belongs-to-selected-attempt-and-precedes-native-room-generation",()=>
        {
            var sample=Fresh();RunRoutes.StartAttempt(sample,1,null,null,"entry");Add(sample,1,"battle","victory");
            Add(sample,1,"map","chest");sample.Operations[1]=sample.Operations[1] with{Argument=new(2,0)};
            RunRoutes.StartAttempt(sample,2,1,null,"entry");RunRoutes.Observe(sample,2,2,[1,2],"disk","load","key",false);
            Add(sample,2,"treasure-open","old");RunRoutes.Restore(sample,"load");
            sample.Continuations.Add(new(DateTimeOffset.UtcNow,2,false,"key","native-continue",4,sample.Branches!.ActiveAttempt,"old-context"));
            Add(sample,2,"treasure-open","old-again");RunRoutes.Restore(sample,"load");
            sample.Continuations.Add(new(DateTimeOffset.UtcNow,2,false,"key","native-continue",5,sample.Branches.ActiveAttempt,"actual-new-load-context"));
            Add(sample,2,"treasure-open","new");
            sample.Operations[3]=sample.Operations[3] with{Progress="abandoned-operation-context"};
            sample.Operations[4]=sample.Operations[4] with{Progress="actual-selected-operation-context"};
            var before=Codec.Bytes(sample);var view=RunRoutes.Playback(sample);
            Need(view.Continuations is [{Progress:"actual-new-load-context",BeforeOperation:3}]&&
                RunRoutes.ProgressBefore(view,view.Continuations.Single())==2&&view.Operations[^1].Progress=="actual-selected-operation-context"&&
                Codec.Bytes(sample).SequenceEqual(before),
                "load-context-lost-or-applied-after-room-generation-or-abandoned-context-replayed");
            var folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"context-store-"+Guid.NewGuid().ToString("N"));
            var store=new RunStore(folder);store.Save(sample);Need(Codec.Bytes(store.Read(sample.Id)).SequenceEqual(before),"load-context-envelope-roundtrip");
        });
        Test("missing-old-context-stays-absent-and-unplaced-new-context-is-rejected",()=>
        {
            var point=new RunContinuation(DateTimeOffset.UtcNow,2,false,"key","native-continue",2);
            Need(!System.Text.Encoding.UTF8.GetString(Codec.Bytes(point)).Contains("Progress"),"old-context-was-fabricated");
            var sample=Fresh();sample.Operations.Add(new(1,1,"battle",new(),null,null,"complete",[]));
            sample.Operations.Add(new(2,2,"treasure-open",new(),null,null,"complete",[]));
            var rejected=false;try{_=RunRoutes.ProgressBefore(sample,point with{Progress="observed"});}catch(InvalidDataException){rejected=true;}
            Need(rejected,"new-progress-context-silently-applied-after-native-entry");
        });
        Test("unknown-destination-uses-selected-attempt-type-and-model-without-rewriting-map-identity",()=>
        {
            var sample=Fresh();sample.Visits[1]=sample.Visits[1] with{MapType="Unknown",RoomType="Shop"};
            RunRoutes.StartAttempt(sample,1,null,null,"entry");Add(sample,1,"battle","won");
            var travel=Add(sample,1,"map","unknown") with{Argument=new(2,0)};sample.Operations[1]=travel;
            RunRoutes.StartAttempt(sample,2,1,null,"entry");sample.Branches!.Attempts[1]=sample.Branches.Attempts[1] with{RoomType="Shop"};
            RunRoutes.Observe(sample,2,2,[1,2],"disk","load","key",false);Add(sample,2,"merchant-open","old");
            RunRoutes.Restore(sample,"load");sample.Branches.Attempts[2]=sample.Branches.Attempts[2] with{RoomType="Monster",Model="ENCOUNTER.NEW"};
            var combat=Add(sample,2,"battle","new");var before=Codec.Bytes(sample);var target=RunRoutes.Destination(sample,travel,combat);
            Need(target.Visit.MapType=="Unknown"&&target.Type=="Monster"&&target.Model=="ENCOUNTER.NEW"&&
                Codec.Bytes(sample).SequenceEqual(before)&&sample.Branches.Attempts[1].RoomType=="Shop","unknown-destination-guessed-from-visit-or-overwrote-old-attempt");
            var rejected=false;try{_=RunRoutes.Destination(sample,travel with{Argument=new(9,9)},combat);}catch(InvalidDataException){rejected=true;}
            Need(rejected,"unknown-destination-ignored-recorded-coordinates");
        });
        Test("completed-event-load-is-bound-to-saved-child-battle-and-selected-return-not-parent-first-page",()=>
        {
            var sample=Fresh();sample.Visits[1]=sample.Visits[1] with{MapType="Unknown",RoomType="Event",Model="EVENT.DUMMY"};
            RunRoutes.StartAttempt(sample,1,null,null,"entry");Add(sample,1,"battle","first");Add(sample,1,"map","event");
            RunRoutes.StartAttempt(sample,2,1,null,"entry");Add(sample,2,"event-choice","fight");
            var child=Add(sample,2,"battle","child");sample.Operations[3]=child with{Battle=new(sample.Id,2,1,0,3,100,"event","victory","EVENT.DUMMY",true)};
            var saved=RunRoutes.Observe(sample,2,2,[1,2,3,4],"completed-disk","completed-load","key",true);
            Add(sample,2,"event-return","old-return");RunRoutes.Restore(sample,"completed-load");
            sample.Continuations.Add(new(DateTimeOffset.UtcNow,2,true,"key","native-continue",6,sample.Branches!.ActiveAttempt,"actual-load-progress"));
            Add(sample,2,"event-return","new-return");var before=Codec.Bytes(sample);var view=RunRoutes.Playback(sample);var context=view.Continuations.Single();
            Need(view.Operations.Select(o=>o.SourceSequence).SequenceEqual(new int?[]{1,2,3,4,6})&&
                RunStore.ContinueBefore(view,context)==5&&RunRoutes.ProgressBefore(view,context)==5&&
                RunRoutes.CompletedEventBattle(view,context)?.SourceSequence==4&&Codec.Bytes(sample).SequenceEqual(before),"event-load-return-or-source-battle-lost");
            var rejected=0;
            sample.Branches.Saves[0]=saved with{Path=[1,2,3]};
            try{_=RunStore.ContinueBefore(RunRoutes.Playback(sample),context);}catch(InvalidDataException){rejected++;}
            sample.Branches.Saves[0]=saved;
            var wrong=view with{Operations=view.Operations.Select(o=>o.Sequence==5?o with{Kind="event-proceed"}:o).ToList()};
            try{_=RunStore.ContinueBefore(wrong,context);}catch(InvalidDataException){rejected++;}
            Need(rejected==2,"event-completed-load-guessed-without-child-save-or-resume");
        });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_RUN_BRANCH_SELECTION_CONTRACTS_NOT_NATIVE_RUNTIME",Tests=tests,NativeRuntimeNotExecuted=true}));
        Console.WriteLine("PASS_RUN_BRANCH_SELECTION_CONTRACTS_NOT_NATIVE_RUNTIME");
    }
}
