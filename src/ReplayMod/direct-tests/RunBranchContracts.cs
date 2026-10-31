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
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_RUN_BRANCH_SELECTION_CONTRACTS_NOT_NATIVE_RUNTIME",Tests=tests,NativeRuntimeNotExecuted=true}));
        Console.WriteLine("PASS_RUN_BRANCH_SELECTION_CONTRACTS_NOT_NATIVE_RUNTIME");
    }
}
