using System.Text.Json;
using ReplayMod;

internal static class RegentRecordingContracts
{
    private sealed record Blade(uint Id,string Pile,decimal Damage,decimal Repeats,int Energy,bool IsDupe,bool CreatedThroughForge);
    internal static void Run(string output,string path)
    {
        void Need(bool condition,string reason){if(!condition)throw new InvalidDataException(reason);}
        var bytes=File.ReadAllBytes(path);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;var clip=envelope.Data;
        const string recorded="EBB189A1D9982A631E7810313224B729B6F5BC1AD860F0BF637D2ECF392C5A88";
        var loaded=new ClipStore(Path.GetDirectoryName(Path.GetFullPath(path))!).Read(clip.Id,
            "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9",recorded,clip.Scope);
        Need(Codec.Bytes(loaded).SequenceEqual(Codec.Bytes(clip))&&clip.Character=="CHARACTER.REGENT"&&clip.Format==5&&clip.Authority is null,"actual-Regent-production-read");
        Need(clip.Operations.Length==13&&clip.Operations[^1].Victory,"actual-complete-victory-recording");
        var pattern=new[]{"POTION.STAR_POTION","CARD.FALLING_STAR","CARD.REFINE_BLADE","CARD.DUAL_WIELD","CARD.ENLIGHTENMENT","CARD.SOVEREIGN_BLADE","EndTurn","POTION.KINGS_COURAGE","CARD.SOVEREIGN_BLADE"};
        Need(clip.Operations.Take(9).Select(o=>o.Card??o.Potion?.Model??o.Kind.ToString()).SequenceEqual(pattern),"actual-mechanism-order");
        using var checkpoint=JsonDocument.Parse(clip.Checkpoint);var player=checkpoint.RootElement.GetProperty("players")[0];
        var deck=player.GetProperty("deck").EnumerateArray().ToArray();var relics=player.GetProperty("relics").EnumerateArray().Select(r=>r.GetProperty("id").GetString()).ToArray();
        Need(player.GetProperty("character_id").GetString()==clip.Character&&deck.Length==13&&relics.Contains("RELIC.DIVINE_RIGHT"),"actual-character-starter-checkpoint");
        var enchanted=deck.Where(c=>c.TryGetProperty("enchantment",out _)).ToArray();
        Need(enchanted.Length==4&&enchanted.All(c=>c.GetProperty("enchantment").GetProperty("id").GetString()=="ENCHANTMENT.ROYALLY_APPROVED"),"actual-serialized-enchantments");
        var states=new[]{clip.Initial}.Concat(clip.Operations.Select(o=>o.After)).Select(json=>
        {
            using var doc=JsonDocument.Parse(json);var root=doc.RootElement;
            if(root.TryGetProperty("Outcome",out _)){using var end=JsonDocument.Parse(root.GetProperty("State").GetString()!);return end.RootElement.Clone();}
            return root.Clone();
        }).ToArray();
        int Resource(int position,string name)=>states[position].GetProperty("Player").GetProperty(name).GetInt32();
        IEnumerable<(string Pile,JsonElement Card)> Cards(int position)=>states[position].GetProperty("Mechanics").GetProperty("CombatCards").EnumerateArray()
            .SelectMany(p=>p.GetProperty("Cards").EnumerateArray().Select(c=>(p.GetProperty("Pile").GetString()!,c)));
        Blade[] Blades(int position)=>Cards(position).Where(c=>c.Card.GetProperty("State").TryGetProperty("Blade",out var b)&&b.ValueKind!=JsonValueKind.Null).Select(c=>
        {
            var state=c.Card.GetProperty("State");var blade=state.GetProperty("Blade");
            return new Blade(c.Card.GetProperty("Key").GetProperty("CombatIndex").GetUInt32(),c.Pile,blade.GetProperty("Damage").GetDecimal(),
                blade.GetProperty("Repeats").GetDecimal(),state.GetProperty("Content").GetProperty("Base").GetProperty("Energy").GetInt32(),
                state.GetProperty("IsDupe").GetBoolean(),blade.GetProperty("CreatedThroughForge").GetBoolean());
        }).OrderBy(b=>b.Id).ToArray();
        Need(Resource(0,"Stars")==3&&Resource(1,"Stars")==6&&Resource(2,"Stars")==4,"actual-stars-gain-and-payment");
        Need(states[0].GetProperty("Piles")[0].GetArrayLength()==5&&Blades(0).Length==0,"actual-native-opening-no-injected-blade");
        var first=Blades(3);var copies=Blades(4);Need(first is [var original]&&original.CreatedThroughForge&&original.Damage==18&&original.Energy==2,"actual-first-native-forge");
        Need(copies.Length==2&&copies.Select(b=>b.Id).Distinct().Count()==2&&copies.All(b=>b.Damage==18&&!b.IsDupe)&&
            copies.Count(b=>b.CreatedThroughForge)==1&&copies.Any(b=>b.Id==first[0].Id),"actual-distinct-native-clone-not-dupe");
        Need(Blades(5).All(b=>b.Energy==1)&&Resource(5,"Energy")==1&&Resource(6,"Energy")==0,"actual-reduced-cost-payment");
        var played=clip.Operations[5].CardState!.CombatIndex;
        Need(Blades(6).Single(b=>b.Id==played).Pile=="Discard"&&Blades(6).Single(b=>b.Id!=played).Pile=="Hand","actual-blade-left-hand");
        Need(Resource(7,"TurnNumber")==2&&Resource(7,"Energy")==4&&Blades(7).All(b=>b.Energy==2),"actual-next-turn-cost-reset-and-energy-power");
        var reforged=Blades(8);Need(reforged.Length==2&&reforged.Select(b=>b.Id).SequenceEqual(copies.Select(b=>b.Id))&&
            reforged.All(b=>b.Damage==33&&b.Repeats==1&&b.Energy==2&&!b.IsDupe)&&reforged.Single(b=>b.Id==played).Pile=="Discard","actual-native-reforge-updates-hand-and-discard-instances");
        Need(Resource(9,"Energy")==Resource(8,"Energy")-2,"actual-next-turn-blade-payment");
        foreach(var op in clip.Operations.Where(o=>o.Kind==OperationKind.Card))
        {
            var before=Cards(op.Sequence-1).Where(c=>c.Pile=="Hand").Select(c=>c.Card).ToArray();var input=before[op.HandIndex!.Value];
            Need(input.GetProperty("State").GetProperty("Content").GetProperty("Base").GetProperty("Id").GetString()==op.Card&&
                JsonSerializer.Deserialize<CardKey>(input.GetProperty("Key").GetRawText(),Codec.Json)==op.CardState,"actual-input-card-instance-fingerprint");
        }
        var choices=clip.Choices!;Need(choices.Length==1&&choices[0].Boundary==4&&choices[0].Source=="CARD.DUAL_WIELD"&&
            choices[0].Automated&&choices[0].Options.Length==1&&choices[0].Chosen.SequenceEqual([0]),"actual-only-candidate-native-automatic-choice");
        for(var p=0;p<states.Length;p++)Need(states[p].GetProperty("ChoicesConsumed").GetInt32()==choices.Count(c=>c.Boundary<=p),"actual-choice-cursor");
        var times=clip.Operations.Select(o=>new{o.Sequence,o.Kind,o.Timing,GapSincePreviousCompletion=o.Timing!.AcceptedMilliseconds-
            (o.Sequence==1?clip.Timing!.InitialReadyMilliseconds:clip.Operations[o.Sequence-2].Timing!.CompletedMilliseconds)}).ToArray();
        var pauseGap=times[3].GapSincePreviousCompletion;Need(pauseGap>=5000,"actual-requested-pause-interval-present");
        Need(bytes.SequenceEqual(File.ReadAllBytes(path)),"actual-original-recording-mutated");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="ACTUAL_CONTENT004_RECORDING_DATA_AND_TIME_VERIFIED_NOT_PLAYBACK",clip.Id,clip.ModHash,
            FileSHA256=Codec.Hash(bytes),FileBytes=bytes.Length,Operations=clip.Operations.Length,Turns=clip.Operations.Max(o=>o.Turn),Victory=true,
            Deck=deck.Length,EnchantedCards=enchanted.Length,Relics=relics,InitialStars=3,AfterStarPotion=6,AfterStarPayment=4,
            FirstForge=first,Copies=copies,PlayedBladeId=played,AfterOffhandForge=reforged,NativeCopyIsDupe=false,
            Choices=choices.Length,AutomatedChoices=1,ManualChoiceUiObserved=false,TimedOperations=times.Length,RecordingTiming=clip.Timing,
            RequestedPauseHostGapMilliseconds=pauseGap,ExactFiveSecondPauseSeparatelyIdentifiable=false,
            PauseMeaning="All real elapsed waiting between first forge completion and DualWield acceptance; includes the reported pause, not a separately tagged interval",
            OperationTimes=times,ProductionReadbackPassed=true,AllCardInputFingerprintsMatchedPriorRecordedState=true,OriginalFileUnchanged=true,
            PlaybackExecuted=false,GameConstructorsExecuted=false,RngRestorationExecuted=false}));
        Console.WriteLine("ACTUAL_REGENT_RECORDING_AND_TIMING_VERIFIED");
    }
}
