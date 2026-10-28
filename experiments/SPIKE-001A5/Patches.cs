using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Spike001A5;

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
