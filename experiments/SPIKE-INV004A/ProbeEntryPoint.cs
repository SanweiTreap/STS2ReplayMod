using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv004A;

[ModInitializer(nameof(Initialize))]
public static class ProbeEntryPoint
{
    public static void Initialize()
    {
        new Harmony("STS2ReplayMod.SPIKEINV004A.DirectWriteSentinel").PatchAll();
        ProbeController.Initialize();
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class MainMenuReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMainMenu __instance) => ProbeController.ObserveMainMenu(__instance);
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
internal static class PreflightHotkeyPatch
{
    [HarmonyPostfix]
    private static void Postfix([HarmonyArgument(0)] InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        var code = key.Keycode != Key.None ? key.Keycode : key.PhysicalKeycode;
        if (code == Key.F8)
            TaskHelper.RunSafely(ProbeController.RunPreflightAsync());
        else if (code == Key.F9)
            TaskHelper.RunSafely(ProbeController.ValidateNativeFixtureAsync());
        else if (code == Key.F10)
            ProbeController.PrintStatus();
    }
}
