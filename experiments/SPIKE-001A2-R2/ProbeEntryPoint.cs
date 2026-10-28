using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Spike001A2R2;

[ModInitializer(nameof(Initialize))]
public static class ProbeEntryPoint
{
    public static void Initialize()
    {
        new Harmony("STS2ReplayMod.SPIKE001A2R2.ReconstructionControl").PatchAll();
        ProbeController.Initialize();
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready), new Type[] { })]
internal static class MainMenuReadyPatch
{
    [HarmonyPostfix] private static void Postfix() => Callable.From(ProbeController.OnMainMenuReady).CallDeferred();
}

internal sealed class ProbeTicker : Node
{
    public override void _Process(double delta)
    {
        _ = delta;
        ProbeController.Tick();
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
internal static class InputPatch
{
    [HarmonyPostfix] private static void Postfix([HarmonyArgument(0)] InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key) return;
        var code = key.Keycode != Key.None ? key.Keycode : key.PhysicalKeycode;
        if (code == Key.F8) ProbeController.EstablishBaseline();
    }
}
