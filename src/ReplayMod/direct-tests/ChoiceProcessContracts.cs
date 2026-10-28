using System.Text.Json;
using ReplayMod;

internal static class ChoiceProcessContracts
{
    internal static void VerifyRecording(string output,string path,string? viewerMod=null)
    {
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        var bytes=File.ReadAllBytes(path);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;var clip=envelope.Data;
        Need(Codec.Hash(Codec.Bytes(clip))==envelope.SHA256,"actual-recorded-envelope");
        var recordedMod="0C6FEC6168D75F73C53388FBE0112E9681497159CA7E6556E4BA04BBD28B6DA0";
        viewerMod??=recordedMod;
        _=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(path))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9",viewerMod,clip.Scope,recordedMod);
        var choices=clip.Choices!;
        Need(choices.All(c=>!c.Automated&&c.Events is not null&&c.NativeId.HasValue),"actual-human-process-missing");
        Need(choices.Any(c=>c.Events!.Any(e=>e.Kind=="Deselect")),"actual-no-deselect");
        Need(choices.Single(c=>c.Source=="CARD.SECRET_WEAPON").Options.Length==25&&
            choices.Single(c=>c.Source=="CARD.SEEKER_STRIKE").Options.Length==3&&
            choices.Single(c=>c.Source=="CARD.HOLOGRAM").Options.Length==18,"actual-pile-sample");
        var preparation=JsonDocument.Parse(clip.Checkpoint);var deck=preparation.RootElement.GetProperty("players")[0].GetProperty("deck").EnumerateArray().ToArray();
        var enchanted=deck.Where(c=>c.TryGetProperty("enchantment",out _)).ToArray();
        Need(deck.Length==34&&enchanted.Length==6&&enchanted.All(c=>c.GetProperty("enchantment").GetProperty("id").GetString()=="ENCHANTMENT.ROYALLY_APPROVED"),"actual-checkpoint-enchantments");
        void State(string json,int count)
        {
            using var document=JsonDocument.Parse(json);var root=document.RootElement;
            using var end=root.TryGetProperty("Outcome",out _)?JsonDocument.Parse(root.GetProperty("State").GetString()!):null;
            if(end is not null)root=end.RootElement;
            Need(root.GetProperty("ChoicesConsumed").GetInt32()==count,"actual-choice-cursor-boundary");
        }
        State(clip.Initial,choices.Count(c=>c.Boundary==0));
        foreach(var op in clip.Operations)State(op.After,choices.Count(c=>c.Boundary<=op.Sequence));
        Need(clip.Operations.Length==15&&choices.Length==8&&choices.Sum(c=>c.Events!.Length)==41&&clip.Operations[^1].Victory,"actual-complete-victory-process-sample");
        Need(File.ReadAllBytes(path).SequenceEqual(bytes),"actual-source-mutated");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="ACTUAL_CONTENT002_RECORDING_DATA_VERIFIED_NOT_PLAYBACK",clip.Id,clip.ModHash,ViewerModHash=viewerMod,ExplicitKnownBuildReadPassed=true,clip.Seed,Operations=clip.Operations.Length,
            Choices=choices.Length,Events=choices.Sum(c=>c.Events!.Length),Deselects=choices.Sum(c=>c.Events!.Count(e=>e.Kind=="Deselect")),
            Boundaries=choices.Select(c=>c.Boundary).ToArray(),CandidateCounts=choices.Select(c=>c.Options.Length).ToArray(),Deck=deck.Length,SerializedNativeEnchantments=enchanted.Length,
            FileBytes=bytes.Length,FileSHA256=Codec.Hash(bytes),EnvelopeSHA256=envelope.SHA256,OriginalFileUnchanged=true,GameConstructorsExecuted=false,PlaybackExecuted=false}));
        Console.WriteLine("ACTUAL_CHOICE_PROCESS_RECORDING_VERIFIED");
    }
    internal static void Run(string output,string oldPath)
    {
        var checks=new List<string>();
        void Need(bool ok){if(!ok)throw new Exception("choice-process-contract");}
        void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("invalid-trace-accepted");}
        ChoiceCard C(uint id)=>new("CARD.STRIKE_IRONCLAD",0,true,id,null,new string('A',64),"打击");
        ChoiceEvent E(string kind,int? option=null,double time=0)=>new(kind,option,time);
        var f=new ChoiceFrame(1,0,1,"FromHand","RELIC.GAMBLING_CHIP",0,3,true,false,[[C(1)],[C(2)],[C(3)]],[2,0],0,70000,"CombatCard",0,false,
            Events:[E("Open"),E("Select",0,2),E("Deselect",0,3),E("Select",2,4),E("Select",0,5),E("Confirm",null,69999),E("Close",null,70000)]);
        ChoiceContract.Validate([f],1);
        var copy=JsonSerializer.Deserialize<ChoiceFrame>(Codec.Bytes(f),Codec.Json)!;
        Need(copy.Events!.SequenceEqual(f.Events!)&&copy.Chosen.SequenceEqual(f.Chosen));
        checks.Add("deselect-reselect-ordered-hand-confirm-and-long-pause-roundtrip");
        Reject(()=>ChoiceContract.Validate([f with{Events=[E("Open"),E("Deselect",0),E("Close")]}],1));
        Reject(()=>ChoiceContract.Validate([f with{Events=[E("Open"),E("Select",3),E("Close")]}],1));
        Reject(()=>ChoiceContract.Validate([f with{Events=[E("Open"),E("Select",0,2),E("Deselect",0,1),E("Close")]}],1));
        Reject(()=>ChoiceContract.Validate([f with{Chosen=[0,2]}],1));
        Reject(()=>ChoiceContract.Validate([f with{Events=[E("Open"),E("Confirm"),E("Select",0),E("Close")]}],1));
        checks.Add("bad-deselect-identity-time-result-order-and-post-submit-click-rejected");
        ChoiceContract.Validate([f with{Chosen=[],Events=[E("Open"),E("Confirm"),E("Close")]}],1);
        ChoiceContract.Validate([f with{Kind="FromChooseACardScreen",Max=1,Chosen=[],NativeType="Index",Events=[E("Open"),E("Skip"),E("Close")]}],1);
        var two=f with{Kind="FromChooseACardScreen",Min=1,Max=1,Options=[[C(1)],[C(2)]],Chosen=[1],NativeType="Index",Events=[E("Open"),E("Select",1),E("Close")]};
        ChoiceContract.Validate([two],1);
        checks.Add("legal-empty-confirm-and-two-candidate-auto-submit-data-supported");
        var generated=two with{Manual=true};var demo=ChoiceTrace.ResultDemo(generated);
        Need(demo.Select(e=>e.Kind).SequenceEqual(new[]{"Open","Select","Close"}));ChoiceTrace.Validate(generated with{Events=demo});
        checks.Add("result-demo-small-native-choice-auto-submits-even-with-manual-command-metadata");
        ChoiceContract.Validate([f with{Kind="FromCombatPile",Chosen=[0,2]}],1);
        checks.Add("grid-final-membership-with-native-hashset-slot-reuse");
        var cursor=new PlaybackCursor(3);
        Need(cursor.StepChoice()&&cursor.TakeChoiceStep()&&!cursor.TakeChoiceStep()&&cursor.Position==0&&!cursor.Busy);
        Need(cursor.Step()&&cursor.TryBegin()&&cursor.Busy);
        cursor.ChoiceOpened();Need(cursor.StepChoice()&&cursor.TakeChoiceStep()&&!cursor.TakeChoiceStep()&&!cursor.CanRewind&&cursor.Position==0);
        Need(cursor.Toggle()&&cursor.TakeChoiceStep());Need(cursor.Toggle()&&!cursor.TakeChoiceStep());
        Need(cursor.StepChoice()&&cursor.Toggle()&&cursor.Toggle()&&!cursor.TakeChoiceStep());
        cursor.Complete();Need(cursor.Position==1&&cursor.CanRewind&&cursor.Rewind());cursor.CompleteRewind(0);
        cursor.ChoiceOpened();Need(!cursor.TakeChoiceStep());cursor.Stop();Need(!cursor.StepChoice()&&!cursor.TakeChoiceStep());
        checks.Add("initial-and-busy-choice-step-play-pause-preserve-host-cursor-and-back-boundary");
        var bytes=File.ReadAllBytes(oldPath);var old=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;
        Need(old.Data.Format==4&&old.Data.Choices!.All(c=>c.Events is null));
        Need(Codec.Hash(Codec.Bytes(old.Data))==old.SHA256&&bytes.SequenceEqual(File.ReadAllBytes(oldPath)));
        ChoiceContract.Validate(old.Data.Choices!,old.Data.Operations.Length);
        foreach(var c in old.Data.Choices!.Where(c=>!c.Automated))ChoiceTrace.Validate(c with{Events=ChoiceTrace.ResultDemo(c)});
        checks.Add("actual-CONTENT001-result-only-clip-checksum-unchanged");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="CONTENT002_DATA_AND_CURSOR_CONTRACTS_PASS_NOT_NATIVE_RUNTIME",Count=checks.Count,Checks=checks,NativeUiInstantiated=false,GameConstructorsExecuted=false}));
        Console.WriteLine("CHOICE_PROCESS_CONTRACTS_PASS "+checks.Count);
    }
}
