using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;

namespace Spike001A;

[ModInitializer(nameof(Initialize))]
public static class ProbeEntryPoint
{
    public static void Initialize()
    {
        new Harmony("STS2ReplayMod.SPIKE001A").PatchAll();
        ProbeController.Initialize();
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
internal static class ProbeHotkeyPatch
{
    [HarmonyPostfix]
    private static void Postfix([HarmonyArgument(0)] InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        var code = key.Keycode != Key.None ? key.Keycode : key.PhysicalKeycode;
        if (code == Key.F8)
            TaskHelper.RunSafely(ProbeController.CaptureFixtureAsync());
        else if (code == Key.F9)
            TaskHelper.RunSafely(ProbeController.RestoreFixtureAsync());
        else if (code == Key.F10)
            ProbeController.PrintStatus();
    }
}
