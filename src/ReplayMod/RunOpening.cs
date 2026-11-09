using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

// The actual inputs to the native NEW-run factory, captured before it reads
// unlocks or consumes RNG. Never a post-Neow combat checkpoint.
internal static class RunOpening
{
    internal static bool NewLeafArmed;
    internal static bool FullRunArmed;
    internal static bool RewardsTestArmed;
    internal static string? ChoicePathTest;
    internal static bool CombatStopTestArmed;
    private static RunStartInput? _captured,_playing;
    private static RunState? _bundleEnchantRoute;
    private static (RunState Run,string Option)? _ancientChoiceRoute;
    private static string? _recordedDebug;
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(NGame),nameof(NGame.StartNewSingleplayerRun)),
            prefix:new HarmonyMethod(typeof(RunOpening),nameof(Capture)));
        h.Patch(AccessTools.DeclaredMethod(typeof(AncientEventModel),"GenerateInitialOptionsWrapper"),
            prefix:new HarmonyMethod(typeof(RunOpening),nameof(PrepareOptions)));
        h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),nameof(RunManager.SetUpNewSingleplayer)),
            postfix:new HarmonyMethod(typeof(RunOpening),nameof(IsolateNativeWriters)));
    }
    internal static void ApplyPreparation(RunState state)
    {
        var input=PlayerLoop.Active?_playing:_captured;
        var ancientOption=AncientOption(input);
        _ancientChoiceRoute=null;
        if(ancientOption is not null||input?.Preparation is RunStartInput.IroncladBlockDeck or RunStartInput.IroncladRewardsTest or RunStartInput.IroncladBundleEnchantTest)
        {
            var player=state.Players.Single();
            RunBossPreparation.InstallBlockDeck(state,player);
            void AddRelic<T>() where T:RelicModel
            {
                var relic=(T)ModelDb.Relic<T>().ToMutable();
                player.AddRelicInternal(relic,silent:true);
                player.RelicGrabBag.Remove(relic);
                state.SharedRelicGrabBag.Remove(relic);
            }
            AddRelic<BagOfPreparation>();AddRelic<Lantern>();
            if(input!.Preparation==RunStartInput.IroncladRewardsTest)
            {
                AddRelic<WhiteBeastStatue>();
                for(var slot=0;slot<player.PotionSlots.Count;slot++)
                    Codec.Require(player.AddPotionInternal(ModelDb.Potion<EnergyPotion>().ToMutable(),slot,silent:true).success,
                        "reward-short-test-potion-belt-preparation-failed");
            }
            if(input.Preparation==RunStartInput.IroncladBundleEnchantTest)
            {
                Codec.Require(ScrollBoxes.CanGenerateBundles(player),"opening-bundle-test-unlock-requirements");
                for(var i=0;i<2;i++)player.Deck.AddInternal(state.CreateCard<DefendIronclad>(player),silent:true);
                _bundleEnchantRoute=state;
            }
            if(ancientOption is not null)
            {
                for(var i=0;i<2;i++)
                {
                    player.Deck.AddInternal(state.CreateCard<DefendIronclad>(player),silent:true);
                    Codec.Require(player.AddPotionInternal(ModelDb.Potion<EnergyPotion>().ToMutable(),i,silent:true).success,
                        "ancient-choice-test-potion-preparation-failed");
                }
                var secondActRooms=(RoomSet)AccessTools.Field(typeof(ActModel),"_rooms").GetValue(state.Acts[1])!;
                secondActRooms.Ancient=ModelDb.Event<Orobas>();
                _ancientChoiceRoute=(state,ancientOption);
            }
            Runtime.Log("run-opening-preparation-applied",new{input.Preparation,Playback=PlayerLoop.Active,BeforeFirstRoom=true,
                Cards=player.Deck.Cards.Select(c=>c.Id.ToString()).ToArray(),Relics=player.Relics.Select(r=>r.Id.ToString()).ToArray(),
                Potions=player.PotionSlots.Select(p=>p?.Id.ToString()).ToArray()});
        }
    }
    private static string? AncientOption(RunStartInput? input)=>input?.Preparation switch
    {
        RunStartInput.IroncladSeaGlassTest=>"SEA_GLASS",
        RunStartInput.IroncladAncientEnchantTest=>"ELECTRIC_SHRYMP",
        _=>null
    };
    internal static void BindBattleAnchor(RunState state,RunStartInput? input)
    {
        if(AncientOption(input) is not {} option||state.CurrentActIndex>1)return;
        Codec.Require(state.Players.Single().Character.Id.ToString()==input!.Character&&
            state.Rng.StringSeed==input.Seed&&state.Acts[1].Ancient is Orobas,"ancient-choice-test-anchor-mismatch");
        _ancientChoiceRoute=(state,option);
        Runtime.Log("run-opening-ancient-choice-anchor-bound",new{Option=option,AncientFromNativeSave=true,RecordedPreparation=true});
    }
    internal static void ConfigureShortRoute(ActMap map)
    {
        var state=RunManager.Instance.DebugOnlyGetState();
        if(state is null||state.CurrentActIndex!=0||state.VisitedMapCoords.Count!=0)return;
        if(_ancientChoiceRoute is {} ancient&&ReferenceEquals(ancient.Run,state))
        {
            Codec.Require(map.StartingMapPoint.PointType==MapPointType.Ancient,"ancient-choice-test-starting-point");
            foreach(var child in map.StartingMapPoint.Children.ToArray())map.StartingMapPoint.RemoveChildPoint(child);
            map.StartingMapPoint.AddChildPoint(map.BossMapPoint);
            Runtime.Log("run-opening-ancient-choice-route-prepared",new{ancient.Option,Boss=state.Act.BossEncounter.Id.ToString(),
                NativeBossPreserved=true,NativeActTransition=true,Playback=PlayerLoop.Active});
            return;
        }
        if(!ReferenceEquals(_bundleEnchantRoute,state))return;
        var rooms=(RoomSet)AccessTools.Field(typeof(ActModel),"_rooms").GetValue(state.Act)!;
        var eventIndex=map.StartingMapPoint.PointType==MapPointType.Ancient?1:0;
        Codec.Require(rooms.eventsVisited==0&&rooms.events.Count>eventIndex,"opening-bundle-test-event-list-not-ready");
        RoomSet.SwapToOrCreateAtIndex<EventModel,WoodCarvings>(rooms.events,eventIndex);
        var first=map.GetPointsInRow(1).Where(p=>p.PointType==MapPointType.Monster).MinBy(p=>p.coord.col)!;
        var next=first.Children.MinBy(p=>p.coord.col)!;
        next.PointType=MapPointType.Unknown;
        var odds=state.Odds.UnknownMapPoint;
        odds.MonsterOdds=-1;odds.EliteOdds=-1;odds.ShopOdds=-1;odds.TreasureOdds=-1;
        _bundleEnchantRoute=null;
        Runtime.Log("run-opening-bundle-enchant-route-prepared",new{First=first.coord.ToString(),Next=next.coord.ToString(),
            Event="EVENT.WOOD_CARVINGS",NativeRollPreserved=true,Playback=PlayerLoop.Active});
    }
    private static void IsolateNativeWriters()
    {
        if(!PlayerLoop.Active||_playing is null)return;
        RunManager.Instance.CombatStateSynchronizer.IsDisabled=true;RunManager.Instance.CombatReplayWriter.IsEnabled=false;
    }
    private static void Capture(CharacterModel character,IReadOnlyList<ActModel> acts,IReadOnlyList<ModifierModel> modifiers,
        string seed,GameMode gameMode,int ascensionLevel)=>Runtime.Guard(()=>
    {
        if(PlayerLoop.Active)return;
        _captured=null;_recordedDebug=null;
        if(gameMode!=GameMode.Standard||ascensionLevel!=0||modifiers.Count!=0||TestEncounter.Armed!=TestPreparation.None||RecordedState.Armed)return;
        var preparedCharacter=character.Id.ToString()=="CHARACTER.IRONCLAD"&&acts.Count==3;
        var pathTest=preparedCharacter?ChoicePathTest:null;
        var preparation=!preparedCharacter?null:RewardsTestArmed?RunStartInput.IroncladRewardsTest:
            pathTest=="SCROLL_BOXES"?RunStartInput.IroncladBundleEnchantTest:
            pathTest=="SEA_GLASS"?RunStartInput.IroncladSeaGlassTest:
            pathTest=="ELECTRIC_SHRYMP"?RunStartInput.IroncladAncientEnchantTest:
            FullRunArmed||pathTest is not null?RunStartInput.IroncladBlockDeck:null;
        if(preparation is not null){FullRunArmed=false;RewardsTestArmed=false;ChoicePathTest=null;}
        _recordedDebug=preparation is RunStartInput.IroncladSeaGlassTest or RunStartInput.IroncladAncientEnchantTest?"GOLDEN_PEARL":
            pathTest??(preparation==RunStartInput.IroncladRewardsTest?"NEOWS_BONES":preparation is null&&NewLeafArmed?"NEW_LEAF":null);
        NewLeafArmed=false;
        _captured=new(character.Id.ToString(),seed,acts.Select(a=>a.Id.ToString()).ToArray(),gameMode.ToString(),ascensionLevel,
            RunResources.Progress(),JsonSerializationUtility.ToJson(SaveManager.Instance.PrefsSave),_recordedDebug,preparation);
        Runtime.Log("run-opening-input-captured",new{Character=_captured.Character,Seed=seed,Acts=_captured.Acts,gameMode,ascensionLevel,
            BeforeNewRunFactory=true,BeforeRewards=true,Preparation=preparation??_recordedDebug,NeowDebugOption=_recordedDebug});
    });
    internal static RunStartInput? Take(){var input=_captured;_captured=null;return input;}
    internal static void TestCombatEntryStop(MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom room,MegaCrit.Sts2.Core.Combat.CombatState state)
    {
        if(!CombatStopTestArmed||!RunPlayback.Active||!PlayerLoop.Loading||!NativeSeek.Enabled||!NativeSeek.Opening||
            !ReferenceEquals(room,MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance)||
            !ReferenceEquals(state,MegaCrit.Sts2.Core.Combat.CombatManager.Instance.DebugOnlyGetState()))return;
        CombatStopTestArmed=false;
        Runtime.Log("run-combat-entry-stop-test-request",new{Phase="NCombatRoom.OnCombatSetUp",PlayerLoop.Loading,NativeSeek.Opening,
            OneShotConsumed=true,PublicControl="PlayerLoop.Control(stop)"});
        PlayerLoop.Control("stop");
        Runtime.Log("run-combat-entry-stop-test-accepted",new{Accepted=NativeSeek.Stopping&&PlayerLoop.Cursor?.StopRequested==true,
            CrossRoomNavigationEnabled=false});
    }
    private static void PrepareOptions(AncientEventModel __instance)
    {
        if(__instance is Neow)
        {
            var option=PlayerLoop.Active?_playing?.NeowDebugOption:_recordedDebug;
            if(option is not null)__instance.DebugOption=option;
        }
        else if(__instance is Orobas&&_ancientChoiceRoute is {} ancient&&
            ReferenceEquals(ancient.Run,RunManager.Instance.DebugOnlyGetState())&&ancient.Run.CurrentActIndex==1)
            __instance.DebugOption=ancient.Option;
    }
    internal static bool Available(RunDocument run)=>RunStore.HasNativeOpening(run);
    internal static async Task Load(RunStartInput input)
    {
        Codec.Require(ReferenceEquals(SaveManager.Instance,PlayerLoop.IsolatedManager),"new-run-opening-without-isolated-manager");
        Codec.Require(RunPlayback.NonCombat&&!ChoiceTape.Watching&&!ChoiceTape.Waiting,"new-run-opening-choice-owner");
        _playing=input;
        try
        {
            var character=ModelDb.GetById<CharacterModel>(ModelId.Deserialize(input.Character));
            var acts=input.Acts.Select(id=>ModelDb.GetById<ActModel>(ModelId.Deserialize(id))).ToArray();
            // Native character-select BeginRun stops menu music before the factory.
            // Replay enters the same factory directly, so keep that transition here.
            var audio=NAudioManager.Instance;audio?.StopMusic();
            var task=Runtime.Game.StartNewSingleplayerRun(character,false,acts,[],input.Seed,GameMode.Standard,input.Ascension);
            RunPlayback.Track(task);await task;RunPlayback.CheckStop();
            Codec.Require(LiveState.Run.CurrentRoom is MegaCrit.Sts2.Core.Rooms.EventRoom {CanonicalEvent:Neow},"new-run-did-not-reach-neow");
            RunManager.Instance.CombatStateSynchronizer.IsDisabled=true;RunManager.Instance.CombatReplayWriter.IsEnabled=false;
            Runtime.Log("run-opening-native-created",new{input.Character,input.Seed,NativeNewRunFactory=true,CheckpointLoaded=false,
                ShouldSave=RunManager.Instance.ShouldSave,IsolatedManager=true,Preparation=input.Preparation??input.NeowDebugOption,
                CombatChoiceTapeActive=ChoiceTape.Watching,RunChoicesOwnSelections=RunPlayback.NonCombat,
                MenuMusicStopRequested=audio is not null});
        }
        finally{_playing=null;}
    }
}
