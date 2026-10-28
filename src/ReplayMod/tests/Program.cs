using System.Text.Json;
using ReplayMod;

var results=new List<string>();
void Test(string name,Action test){test();results.Add(name);}
void Need(bool value){if(!value)throw new Exception("assertion");}
void Reject(Action test){try{test();}catch(InvalidDataException){return;}throw new Exception("should reject");}
var root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
var clip=new Clip(1,Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],
    "CHARACTER.IRONCLAD","user-seed",1,2,"first-monster","captured-checkpoint","initial-live-state",
    [new(1,OperationKind.Card,1,3,"CARD.STRIKE_IRONCLAD",new(1,"enemy-model",7),"after-actual-targeted-strike"),
     new(2,OperationKind.EndTurn,1,null,null,null,"after-actual-next-turn")]);
Test("fresh-file-reopened-with-actual-operation-order-target-and-states",()=>
{
    new ClipStore(root).Seal(clip);var reader=new ClipStore(root);var actual=reader.Read(reader.LatestId()!,"game","mod","scope");
    Need(Codec.Bytes(actual).AsSpan().SequenceEqual(Codec.Bytes(clip))&&actual.Operations[0].HandIndex==3&&actual.Operations[0].Target!.Index==1);
});
Test("same-id-not-overwritten-and-invalid-pointer-path-rejected",()=>{Reject(()=>new ClipStore(root).Seal(clip));Reject(()=>new ClipStore(root).Read("../latest","game","mod","scope"));});
Test("wrong-build-environment-scope-or-incomplete-format-rejected",()=>
{
    Reject(()=>Codec.Validate(clip,"other","mod","scope"));Reject(()=>Codec.Validate(clip,"game","other","scope"));Reject(()=>Codec.Validate(clip,"game","mod","other"));
    Reject(()=>Codec.Validate(clip with{Mods=["another-mod"]},"game","mod","scope"));Reject(()=>Codec.Validate(clip with{Format=3},"game","mod","scope"));
    Reject(()=>Codec.Validate(clip with{Operations=clip.Operations.Take(1).ToArray()},"game","mod","scope"));
});
Test("changed-payload-checksum-rejected",()=>
{
    var path=Path.Combine(root,clip.Id+".json");var envelope=JsonSerializer.Deserialize<ClipEnvelope>(File.ReadAllBytes(path),Codec.Json)!;
    File.WriteAllBytes(path,Codec.Bytes(envelope with{Data=clip with{Initial="tampered"}}));Reject(()=>new ClipStore(root).Read(clip.Id,"game","mod","scope"));
});
Test("pause-and-step-stop-at-semantic-boundary-no-concurrent-dispatch",()=>
{
    var cursor=new PlaybackCursor(3);Need(!cursor.TryBegin());cursor.Step();Need(cursor.TryBegin()&&!cursor.TryBegin());
    cursor.Step();cursor.Complete();Need(cursor.Position==1&&!cursor.TryBegin());cursor.Toggle();Need(cursor.TryBegin());cursor.Toggle();
    Need(cursor.Busy);cursor.Complete();Need(cursor.Position==2&&!cursor.TryBegin());cursor.Step();Need(cursor.TryBegin());cursor.Stop();
    cursor.Complete();Need(cursor.Finished&&cursor.StopRequested&&!cursor.TryBegin());
});
Test("reopen-has-independent-cursor-and-no-fixed-Defend-plan",()=>
{
    var a=new PlaybackCursor(clip.Operations.Length);a.Step();Need(a.TryBegin());a.Complete();
    var b=new PlaybackCursor(clip.Operations.Length);Need(b.Position==0&&!b.Busy&&!b.Playing&&clip.Operations[0].Card=="CARD.STRIKE_IRONCLAD");
});
Test("back-rebuild-is-exclusive-paused-and-resumes-from-arrival-including-finished",()=>
{
    var cursor=new PlaybackCursor(3);Need(!cursor.CanRewind&&!cursor.Rewind());
    for(var i=0;i<2;i++){Need(cursor.Step()&&cursor.TryBegin());cursor.Complete();}
    Need(cursor.Position==2&&cursor.CanRewind&&cursor.Rewind()&&cursor.RewindTarget==1);
    Need(cursor.Busy&&cursor.Rebuilding&&!cursor.Step()&&!cursor.Toggle()&&!cursor.Rewind()&&!cursor.TryBegin());
    Reject(()=>cursor.Complete());Reject(()=>cursor.CompleteRewind(0));
    cursor.CompleteRewind(1);Need(cursor.Position==1&&!cursor.Busy&&!cursor.Playing&&!cursor.TryBegin());
    Need(cursor.Step()&&cursor.TryBegin());cursor.Complete();Need(cursor.Position==2);
    Need(cursor.Toggle()&&cursor.TryBegin());cursor.Complete();Need(cursor.Finished&&!cursor.Playing);
    Need(!cursor.Step()&&!cursor.Toggle()&&cursor.Rewind()&&cursor.RewindTarget==2);
    cursor.CompleteRewind(2);Need(!cursor.Finished&&!cursor.Playing&&cursor.Toggle()&&cursor.TryBegin());
    cursor.Complete();Need(cursor.Finished);
});
Test("repeated-back-to-zero-and-pending-forward-or-active-play-cannot-overlap",()=>
{
    var cursor=new PlaybackCursor(3);
    for(var cycle=0;cycle<3;cycle++)
    {
        Need(cursor.Step()&&!cursor.Rewind()&&cursor.TryBegin());
        Need(cursor.Toggle()&&!cursor.Rewind());Need(cursor.Toggle()&&!cursor.Rewind());
        cursor.Complete();Need(cursor.Rewind()&&cursor.RewindTarget==0);cursor.CompleteRewind(0);
        Need(cursor.Position==0&&!cursor.CanRewind&&!cursor.Rewind()&&!cursor.TryBegin());
    }
});
Test("unfinished-stop-and-stop-during-back-never-resume-dispatch",()=>
{
    var cursor=new PlaybackCursor(3);Need(cursor.Step()&&cursor.TryBegin());cursor.Complete();
    cursor.Stop();Need(!cursor.Finished&&cursor.Position==1&&!cursor.Rewind()&&!cursor.Step()&&!cursor.TryBegin());
    var back=new PlaybackCursor(3);Need(back.Step()&&back.TryBegin());back.Complete();Need(back.Rewind());back.Stop();
    Need(back.StopRequested&&!back.Playing&&!back.Step()&&!back.Toggle()&&!back.Rewind()&&!back.TryBegin());
    Reject(()=>back.CompleteRewind(0));
    var reopened=new PlaybackCursor(3);Need(reopened.Position==0&&!reopened.StopRequested&&!reopened.Rebuilding);
});
File.WriteAllBytes(Path.Combine(root,"result.json"),Codec.Bytes(new{Result="PASS_DEMO_CORE_CONTRACTS",Count=results.Count,Tests=results,RealGameExecuted=false,GameConstructorsExecuted=false}));
Console.WriteLine(JsonSerializer.Serialize(new{Result="PASS",Count=results.Count}));
