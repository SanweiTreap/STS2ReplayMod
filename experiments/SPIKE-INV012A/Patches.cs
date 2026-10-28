using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpikeInv012A;

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready), new Type[] { })]
internal static class MainMenuPatch
{
    [HarmonyPostfix] private static void Postfix() => Probe.Safe("mainmenu.ready", Probe.MainMenuReady);
}
[HarmonyPatch(typeof(NGame), nameof(NGame._Input), new[] { typeof(InputEvent) })]
internal static class ArmPatch
{
    [HarmonyPostfix] private static void Postfix(InputEvent inputEvent) => Probe.Safe("input.arm", () =>
    {
        if (inputEvent is InputEventKey { Pressed: true, Echo: false } key &&
            (key.Keycode == Key.F8 || key.PhysicalKeycode == Key.F8)) Probe.Arm();
    });
}
[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRun), new[] { typeof(AbstractRoom), typeof(bool) })]
internal static class SavePatch
{
    [HarmonyPrefix] private static void Prefix(AbstractRoom? preFinishedRoom, bool saveProgress)
        => Probe.Safe("save.entry", () => Probe.SaveEntry(preFinishedRoom, saveProgress));
    [HarmonyPostfix] private static void Postfix(Task __result)
        => Probe.Safe("save.return", () => Probe.SaveReturn(__result));
}
[HarmonyPatch(typeof(NRun), nameof(NRun._Notification), new[] { typeof(int) })]
internal static class NRunPatch
{
    [HarmonyPrefix] private static void Prefix(int what) => Probe.Safe("nrun.entry", () => { if (what == 1006) Probe.Writer("NRun.Notification", true); });
    [HarmonyPostfix] private static void Postfix(int what) => Probe.Safe("nrun.return", () => { if (what == 1006) Probe.Writer("NRun.Notification", false); });
}
[HarmonyPatch(typeof(NGame), "Quit", new Type[] { })]
internal static class QuitPatch
{
    [HarmonyPrefix] private static void Prefix() => Probe.Safe("quit.entry", () => Probe.Writer("NGame.Quit", true));
    [HarmonyPostfix] private static void Postfix() => Probe.Safe("quit.return", () => Probe.Writer("NGame.Quit", false));
}
[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp), new[] { typeof(bool) })]
internal static class CleanupPatch
{
    [HarmonyPrefix] private static void Prefix(RunManager __instance) => Probe.Safe("cleanup.entry", () =>
        Probe.Writer("RunManager.CleanUp", true, __instance.CombatReplayWriter?.IsRecordingReplay));
    [HarmonyPostfix] private static void Postfix(RunManager __instance) => Probe.Safe("cleanup.return", () =>
        Probe.Writer("RunManager.CleanUp", false, __instance.CombatReplayWriter?.IsRecordingReplay));
}
[HarmonyPatch(typeof(RunManager), nameof(RunManager.WriteReplay), new[] { typeof(bool) })]
internal static class WriteReplayPatch
{
    [HarmonyPrefix] private static void Prefix([HarmonyArgument(0)] bool stop) => Probe.Safe("write-replay.entry", () => Probe.Writer("RunManager.WriteReplay", true, stop));
    [HarmonyPostfix] private static void Postfix([HarmonyArgument(0)] bool stop) => Probe.Safe("write-replay.return", () => Probe.Writer("RunManager.WriteReplay", false, stop));
}
[HarmonyPatch(typeof(SettingsSaveManager), nameof(SettingsSaveManager.SaveSettings), new Type[] { })]
internal static class SettingsPatch
{
    [HarmonyPrefix] private static void Prefix() => Probe.Safe("settings.entry", () => Probe.Writer("SettingsSaveManager.SaveSettings", true));
    [HarmonyPostfix] private static void Postfix() => Probe.Safe("settings.return", () => Probe.Writer("SettingsSaveManager.SaveSettings", false));
}
[HarmonyPatch(typeof(CombatReplayWriter), nameof(CombatReplayWriter.WriteReplay), new[] { typeof(string), typeof(bool) })]
internal static class NativeWriterPatch
{
    // The string path argument is deliberately absent from both patch callbacks.
    [HarmonyPrefix] private static void Prefix([HarmonyArgument(1)] bool stop) => Probe.Safe("native-writer.entry", () => Probe.Writer("CombatReplayWriter.WriteReplay", true, stop));
    [HarmonyPostfix] private static void Postfix([HarmonyArgument(1)] bool stop) => Probe.Safe("native-writer.return", () => Probe.Writer("CombatReplayWriter.WriteReplay", false, stop));
}
