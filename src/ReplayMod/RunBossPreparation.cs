using HarmonyLib;
using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReplayMod;

// One explicit developer preparation before room creation, never a playback
// correction. The actual native save is written/read back before recording it.
internal static class RunBossPreparation
{
    internal static bool Armed;
    private static bool _entering;
    private static RunState? _prepared;
    internal static string? Marker=>ReferenceEquals(_prepared,RunManager.Instance.DebugOnlyGetState())?
        "RUN005-explicit-act3-Boss-continue-BarricadePlus-2ImperviousPlus-EntrenchPlus-2BodySlamPlus-5-innate-retain-fullHP-native-save-prefix-gap":null;
    internal static void Install(Harmony h)=>h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"LoadIntoLatestMapCoord"),
        prefix:new HarmonyMethod(typeof(RunBossPreparation),nameof(Load)));
    private static bool Load(AbstractRoom? __0,ref Task __result)
    {
        if(!Armed||_entering||PlayerLoop.Active)return true;
        __result=PrepareAndEnter(__0);return false;
    }
    private static async Task PrepareAndEnter(AbstractRoom? preFinished)
    {
        var run=LiveState.Run;var manager=SaveManager.Instance;
        Codec.Require(RunRecording.Enabled&&RunManager.Instance.ShouldSave&&run.CurrentRoom is null&&preFinished is null&&
            run.CurrentActIndex==2&&run.CurrentMapPoint?.PointType==MapPointType.Boss&&run.Players.Count==1&&
            run.AscensionLevel==0&&run.Modifiers.Count==0&&LiveState.Player.Character.Id.ToString()=="CHARACTER.IRONCLAD",
            "boss-preparation-requires-unfinished-act3-Ironclad-native-Continue");
        var player=LiveState.Player;
        Codec.Require(player.Relics.Any(r=>r is BagOfPreparation)&&player.Relics.Any(r=>r is Lantern),"boss-preparation-opening-relics-missing");
        if(manager.CurrentRunSaveTask is {} prior)await prior;
        Armed=false;
        string Rng()=>JsonSerializer.Serialize(run.Rng.ToSerializable(),JsonSerializationUtility.GetTypeInfo<SerializableRunRngSet>());
        var rng=Rng();
        RunRecording.Unsupported("explicit-boss-preparation","终局短测加强：原生Boss战前替换六张升级先天保留牌并回满血；此前路线明确缺口");
        InstallBlockDeck(run,player);
        player.Creature.SetCurrentHpInternal(player.Creature.MaxHp);
        Codec.Require(Rng()==rng,"boss-preparation-rng-mismatch");
        _prepared=run;
        var requested=RunManager.Instance.ToSave(null);
        var expected=RecordingJournal.NativeFingerprint(JsonSerializationUtility.ToJson(requested));
        var nativeManager=(RunSaveManager)RunPatches.Field(manager,"_runSaveManager")!;
        // Save this exact native ToSave DTO; a second ToSave call would change
        // wall-clock fields and make exact readback needlessly ambiguous.
        await nativeManager.SaveRun(requested,isMultiplayer:false);
        Codec.Require(ReferenceEquals(manager,SaveManager.Instance)&&ReferenceEquals(run,LiveState.Run)&&run.CurrentRoom is null,
            "boss-preparation-save-owner-changed");
        var store=(ISaveStore)RunPatches.Field(nativeManager,"_saveStore")!;
        var path=(string)AccessTools.Property(typeof(RunSaveManager),"CurrentRunSavePath").GetValue(nativeManager)!;
        var actual=store.ReadFile(path)??throw new InvalidDataException("boss-preparation-native-save-missing");
        Codec.Require(RecordingJournal.NativeFingerprint(actual)==expected,"boss-preparation-native-save-readback-mismatch");
        var loaded=manager.LoadRunSave();
        Codec.Require(loaded.Success&&loaded.SaveData is not null,"boss-preparation-native-load-readback-failed");
        Recorder.PreparedContinue(loaded.SaveData!);
        Runtime.Log("run-boss-preparation-saved",new{Act=run.CurrentActIndex,Character=player.Character.Id.ToString(),
            Cards=player.Deck.Cards.Select(c=>new{Model=c.Id.ToString(),Upgrade=c.CurrentUpgradeLevel}).ToArray(),
            FullHp=true,NativeBossUnchanged=true,RunRngUnchanged=true,ExplicitPrefixGap=true,
            NativeSaveWrittenAndReadBack=true,BeforeRoomCreation=true,Preparation=Marker});
        _entering=true;
        try{await RunManager.Instance.LoadIntoLatestMapCoord(preFinished);}
        finally{_entering=false;}
    }

    internal static void InstallBlockDeck(RunState run,Player player)
    {
        foreach(var old in player.Deck.Cards.ToArray()){player.Deck.RemoveInternal(old,silent:true);run.RemoveCard(old);}
        void Add<T>() where T:CardModel
        {
            var card=run.CreateCard<T>(player);card.UpgradeInternal();card.FinalizeUpgradeInternal();
            var enchantment=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.RoyallyApproved>().ToMutable();
            if(enchantment.CanEnchantCardType(card.Type)){card.EnchantInternal(enchantment,1);enchantment.ModifyCard();}
            player.Deck.AddInternal(card,silent:true);
        }
        Add<Barricade>();Add<Impervious>();Add<Impervious>();Add<Entrench>();Add<BodySlam>();Add<BodySlam>();
        Codec.Require(player.Deck.Cards.Count==6&&player.Deck.Cards.All(c=>ReferenceEquals(c.Owner,player)&&run.ContainsCard(c)),
            "block-deck-preparation-registration-mismatch");
    }
}
