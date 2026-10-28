using ReplayMod;
using System.Text.Json;

internal static class CharacterContracts
{
    internal static void VerifyRecording(string output,string path,string? viewerMod)
    {
        void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
        const string recorded="DC8D7075E181245D79E76435C158084A6A0BB2313490A1BAB5B69A69F90092B9";
        var bytes=File.ReadAllBytes(path);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;var clip=envelope.Data;
        _=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(path))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9",viewerMod??recorded,clip.Scope,recorded);
        Need(clip.Character=="CHARACTER.SILENT"&&clip.Format==4&&clip.Authority is null&&clip.Operations[^1].Victory,"actual-silent-victory-format");
        using var checkpoint=JsonDocument.Parse(clip.Checkpoint);var player=checkpoint.RootElement.GetProperty("players")[0];
        Need(player.GetProperty("character_id").GetString()==clip.Character,"actual-native-character");
        var deck=player.GetProperty("deck").EnumerateArray().ToArray();var ids=deck.Select(c=>c.GetProperty("id").GetString()).ToArray();
        Need(deck.Length==14&&ids.Count(i=>i=="CARD.STRIKE_SILENT")==5&&ids.Count(i=>i=="CARD.DEFEND_SILENT")==5&&
            new[]{"CARD.SURVIVOR","CARD.NEUTRALIZE","CARD.BURST","CARD.BLADE_DANCE"}.All(i=>ids.Count(v=>v==i)==1),"actual-starter-deck-preserved");
        var enchanted=deck.Where(c=>c.TryGetProperty("enchantment",out _)).ToArray();
        Need(enchanted.Length==3&&enchanted.All(c=>c.GetProperty("enchantment").GetProperty("id").GetString()=="ENCHANTMENT.ROYALLY_APPROVED"),"actual-serialized-preparation");
        var states=new[]{clip.Initial}.Concat(clip.Operations.Select(o=>o.After)).Select(json=>
        {
            using var doc=JsonDocument.Parse(json);var root=doc.RootElement;
            if(root.TryGetProperty("Outcome",out _)){using var end=JsonDocument.Parse(root.GetProperty("State").GetString()!);return end.RootElement.Clone();}
            return root.Clone();
        }).ToArray();
        var initial=states[0];Need(initial.GetProperty("Piles")[0].GetArrayLength()==7,"actual-native-opening-seven-cards");
        var relics=initial.GetProperty("Player").GetProperty("Relics").EnumerateArray().Select(r=>r.GetProperty("id").GetString()).ToArray();
        Need(relics.Contains("RELIC.RING_OF_THE_SNAKE"),"actual-starter-relic-present");
        var choices=clip.Choices!;Need(choices.Length>=2&&choices.All(c=>c.Source=="CARD.SURVIVOR"&&!c.Automated&&c.Events is not null)&&
            choices.Select(c=>c.NativeId).Distinct().Count()==choices.Length,"actual-human-native-discard-decisions");
        foreach(var pair in choices.GroupBy(c=>c.Boundary))
        {
            var two=pair.ToArray();Need(two.Length==2&&clip.Operations[pair.Key-1].Card=="CARD.SURVIVOR","actual-two-choices-one-host");
            var discarded=two[0].Options[two[0].Chosen.Single()].Single().CombatIndex;
            Need(discarded.HasValue&&two[1].Options.SelectMany(o=>o).All(c=>c.CombatIndex!=discarded)&&
                two[1].Options.SelectMany(o=>o).Select(c=>c.CombatIndex).SequenceEqual(two[0].Options.SelectMany(o=>o).Where(c=>c.CombatIndex!=discarded).Select(c=>c.CombatIndex)),"actual-second-choice-candidates-preserve-surviving-instances");
        }
        Need(choices[0].Boundary==2&&states[2].GetProperty("Player").GetProperty("Block").GetInt32()==16,"actual-first-double-survivor-block");
        for(var position=0;position<states.Length;position++)Need(states[position].GetProperty("ChoicesConsumed").GetInt32()==choices.Count(c=>c.Boundary<=position),"actual-choice-cursor-at-each-host");
        var dance=clip.Operations.Single(o=>o.Card=="CARD.BLADE_DANCE");
        uint[] Shivs(int position,string pile)=>states[position].GetProperty("Mechanics").GetProperty("CombatCards").EnumerateArray()
            .Single(p=>p.GetProperty("Pile").GetString()==pile).GetProperty("Cards").EnumerateArray()
            .Where(c=>c.GetProperty("State").GetProperty("Base").GetProperty("Id").GetString()=="CARD.SHIV")
            .Select(c=>c.GetProperty("Key").GetProperty("CombatIndex").GetUInt32()).ToArray();
        var generated=Shivs(dance.Sequence,"Hand");var plays=clip.Operations.Where(o=>o.Card=="CARD.SHIV").ToArray();
        Need(dance.Turn>=2&&generated.Length==3&&generated.Distinct().Count()==3&&plays.Length==3&&
            plays.Select(o=>o.CardState!.CombatIndex).SequenceEqual(generated),"actual-distinct-generated-and-played-Shiv-identities");
        for(var i=0;i<plays.Length;i++)Need(Shivs(plays[i].Sequence,"Exhaust").SequenceEqual(generated.Take(i+1))&&
            Shivs(plays[i].Sequence,"Hand").SequenceEqual(generated.Skip(i+1)),"actual-Shiv-exhaust-and-hand-sequence");
        Need(clip.Operations.Any(o=>o.Card=="CARD.NEUTRALIZE")&&clip.Operations.Count(o=>o.Kind==OperationKind.EndTurn)>0,"actual-starter-and-turn-coverage");
        Need(File.ReadAllBytes(path).SequenceEqual(bytes),"actual-recording-was-mutated");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="ACTUAL_CONTENT003_RECORDING_VERIFIED_NOT_PLAYBACK",clip.Id,clip.Character,clip.ModHash,
            ViewerModHash=viewerMod,FileSHA256=Codec.Hash(bytes),FileBytes=bytes.Length,Operations=clip.Operations.Length,Victory=true,
            Turns=clip.Operations.Max(o=>o.Turn),Deck=deck.Length,SerializedEnchantments=enchanted.Length,Relics=relics,OpeningHand=7,
            Choices=choices.Length,ChoiceBoundaries=choices.Select(c=>c.Boundary).ToArray(),Candidates=choices.Select(c=>c.Options.Length).ToArray(),
            RawEvents=choices.Sum(c=>c.Events!.Length),GeneratedShivIds=generated,ShivHandAndExhaustIdentityVerified=true,
            NativeChoiceCandidatesAndCursorsVerified=true,OriginalBytesUnchanged=true,PlaybackExecuted=false,GameConstructorsExecuted=false}));
        Console.WriteLine("ACTUAL_SILENT_RECORDING_VERIFIED");
    }
    internal static void Run(string output)
    {
        var names=new List<string>();
        var clip=new Clip(4,new string('a',32),DateTimeOffset.UtcNow,"game","mod","scope",["ReplayMod:0.1.0"],
            "CHARACTER.SILENT","seed",1,0,"ordinary","native-checkpoint","initial",
            [new(1,OperationKind.EndTurn,1,null,null,null,"next-turn")],Choices:[]);
        void Check(string name,Clip value,bool accepted)
        {
            var passed=true;try{Codec.Validate(value,"game","mod","scope");}catch(InvalidDataException){passed=false;}
            if(passed!=accepted)throw new Exception(name);names.Add(name);
        }
        Check("Silent-format4-native-operations-accepted",clip,true);
        Check("Ironclad-format4-still-accepted",clip with{Character="CHARACTER.IRONCLAD"},true);
        Check("Silent-legacy-state-format-rejected",clip with{Format=3,Choices=null},false);
        Check("other-character-still-rejected",clip with{Character="CHARACTER.DEFECT"},false);
        var authority=clip with{Format=3,Choices=null,Authority="STATE001-first-Defend-v1",Operations=[
            new(1,OperationKind.Card,1,0,"CARD.DEFEND_IRONCLAD",null,"after",CardState:new(1,0,new string('A',64),"Self")),
            new(2,OperationKind.EndTurn,1,null,null,null,"next")]};
        Check("Silent-cannot-claim-old-Ironclad-file-authority",authority,false);
        Check("old-Ironclad-file-authority-kept",authority with{Character="CHARACTER.IRONCLAD"},true);
        Check("Silent-native-player-target-and-generated-Shiv-identity",clip with{Operations=[
            new(1,OperationKind.Card,1,0,"CARD.BLADE_DANCE",new(0,"CHARACTER.SILENT",0,TargetRole.Player),"generated",
                CardState:new(12,0,new string('A',64),"Self")),
            new(2,OperationKind.Card,1,0,"CARD.SHIV",new(0,"MONSTER.CORPSE_SLUG",1),"consumed",
                CardState:new(14,0,new string('B',64),"AnyEnemy")),
            new(3,OperationKind.EndTurn,1,null,null,null,"next")]},true);
        Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,"character-contracts.json"),Codec.Bytes(new{
            Result="PASS_CHARACTER_SCOPE_CONTRACTS",Tests=names,RealGameExecuted=false,
            Note="File scope and metadata only. Native Silent/Burst/Survivor/Shiv/Stop still require human runtime test."}));
        Console.WriteLine($"PASS_CHARACTER_SCOPE_CONTRACTS {names.Count}");
    }
}
