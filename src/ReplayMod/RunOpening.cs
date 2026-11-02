using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

// The actual inputs to the native NEW-run factory, captured before it reads
// unlocks or consumes RNG. Never a post-Neow combat checkpoint.
internal static class RunOpening
{
    internal static bool NewLeafArmed;
    internal static bool CombatStopTestArmed;
    private static RunStartInput? _captured,_playing;
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
        _recordedDebug=NewLeafArmed?"NEW_LEAF":null;NewLeafArmed=false;
        _captured=new(character.Id.ToString(),seed,acts.Select(a=>a.Id.ToString()).ToArray(),gameMode.ToString(),ascensionLevel,
            RunResources.Progress(),JsonSerializationUtility.ToJson(SaveManager.Instance.PrefsSave),_recordedDebug);
        Runtime.Log("run-opening-input-captured",new{Character=_captured.Character,Seed=seed,Acts=_captured.Acts,gameMode,ascensionLevel,
            BeforeNewRunFactory=true,BeforeRewards=true,Preparation=_recordedDebug});
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
        if(__instance is not Neow)return;
        var option=PlayerLoop.Active?_playing?.NeowDebugOption:_recordedDebug;
        if(option is not null)__instance.DebugOption=option;
    }
    internal static bool Available(RunDocument run)=>run.Opening is not null&&run.Operations.FirstOrDefault() is
        {Kind:"event-choice",Status:"complete"} op&&run.Visits.Any(v=>v.Id==op.Visit&&v.Model=="EVENT.NEOW"&&v.MapType=="Ancient")&&
        RunPlayback.StartIndex(run)>=0;
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
                ShouldSave=RunManager.Instance.ShouldSave,IsolatedManager=true,Preparation=input.NeowDebugOption,
                CombatChoiceTapeActive=ChoiceTape.Watching,RunChoicesOwnSelections=RunPlayback.NonCombat,
                MenuMusicStopRequested=audio is not null});
        }
        finally{_playing=null;}
    }
}
