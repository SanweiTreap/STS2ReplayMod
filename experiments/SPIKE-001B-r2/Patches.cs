using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Spike001B;

[HarmonyPatch(typeof(NMainMenu),nameof(NMainMenu._Ready),new Type[]{})]
internal static class MainMenuPatch { [HarmonyPostfix] private static void Postfix()=>Probe.Safe(Probe.MainMenu); }
[HarmonyPatch(typeof(NGame),nameof(NGame._Input),new[]{typeof(InputEvent)})]
internal static class InputPatch
{
    [HarmonyPostfix] private static void Postfix(InputEvent inputEvent)=>Probe.Safe(()=>
    { if(inputEvent is InputEventKey {Pressed:true,Echo:false} key && (key.Keycode==Key.F8 || key.PhysicalKeycode==Key.F8))Probe.Arm(); });
}
[HarmonyPatch(typeof(SaveManager),nameof(SaveManager.SaveRun),new[]{typeof(AbstractRoom),typeof(bool)})]
internal static class SavePatch
{
    [HarmonyPostfix] private static void Postfix(AbstractRoom? preFinishedRoom,bool saveProgress,ref Task __result)
        =>__result=Probe.ObserveSave(__result,preFinishedRoom,saveProgress);
}
[HarmonyPatch(typeof(RunManager),nameof(RunManager.SetUpSavedSingleplayer),new[]{typeof(RunState),typeof(SerializableRun)})]
internal static class ContinuePatch
{
    [HarmonyPrefix] private static void Prefix(RunState state,SerializableRun save)=>Probe.Safe(()=>Probe.Continue(state,save));
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.GameActions.Multiplayer.ActionQueueSynchronizer),"RequestEnqueue",new[]{typeof(MegaCrit.Sts2.Core.GameActions.GameAction)})]
internal static class QueuePatch
{ [HarmonyPrefix] private static void Prefix(MegaCrit.Sts2.Core.GameActions.GameAction action)=>Probe.Safe(()=>ActionBridge.Queue(action)); }

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Models.CardModel),"OnPlayWrapper",new[]{typeof(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext),typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature),typeof(bool),typeof(MegaCrit.Sts2.Core.Entities.Cards.ResourceInfo),typeof(bool)})]
internal static class CardEffectPatch
{ [HarmonyPostfix] private static void Postfix(MegaCrit.Sts2.Core.Models.CardModel __instance,MegaCrit.Sts2.Core.Entities.Creatures.Creature? target,bool isAutoPlay,Task __result)
    =>Probe.Safe(()=>ActionBridge.CardEffect(__instance,target,isAutoPlay,__result)); }

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Combat.CombatManager),"SetReadyToEndTurn",new[]{typeof(MegaCrit.Sts2.Core.Entities.Players.Player),typeof(bool),typeof(Func<Task>)})]
internal static class EndReadyPatch
{
    [HarmonyPrefix] private static void Prefix(MegaCrit.Sts2.Core.Entities.Players.Player player,out bool? __state)
    { bool? state=null; Probe.Safe(()=> { if(Probe.CurrentFlow?.Stage==Spike001B.Core.Stage.Action)state=ActionBridge.BeforeEnd(player); }); __state=state; }
    [HarmonyPostfix] private static void Postfix(MegaCrit.Sts2.Core.Entities.Players.Player player,bool? __state)
    { if(__state.HasValue)Probe.Safe(()=>ActionBridge.EndReady(player,__state.Value)); }
}
