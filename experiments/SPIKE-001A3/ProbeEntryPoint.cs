using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace Spike001A3;

[ModInitializer(nameof(Initialize))]
public static class ProbeEntryPoint
{
    public static void Initialize()
    {
        new Harmony("STS2ReplayMod.SPIKE001A3.ReconstructionControl").PatchAll();
        ProbeController.Initialize();
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready), new Type[] { })]
internal static class MainMenuReadyPatch
{
    [HarmonyPostfix] private static void Postfix() => Callable.From(ProbeController.OnMainMenuReady).CallDeferred();
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
internal static class InputPatch
{
    [HarmonyPostfix] private static void Postfix([HarmonyArgument(0)] InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key) return;
        var code = key.Keycode != Key.None ? key.Keycode : key.PhysicalKeycode;
        if (code == Key.F8) ProbeController.ArmA();
    }
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRun), new[] { typeof(AbstractRoom), typeof(bool) })]
internal static class ProductionSaveObserverPatch
{
    [HarmonyPostfix]
    private static void Postfix(AbstractRoom? preFinishedRoom, bool saveProgress, ref Task __result)
    {
        if (ProbeController.ShouldWrapProductionSave(preFinishedRoom, saveProgress))
            __result = ProbeController.WrapProductionSaveAsync(__result);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedSingleplayer),
    new[] { typeof(RunState), typeof(SerializableRun) })]
internal static class ContinueObserverPatch
{
    [HarmonyPrefix]
    private static void Prefix(RunState state, SerializableRun save) => ProbeController.OnNormalContinueBegin(state, save);
}

internal sealed class ProbeTicker : Node
{
    public override void _EnterTree() => ProbeController.OnTickerLifecycle(this, "enter-tree");
    public override void _ExitTree() => ProbeController.OnTickerLifecycle(this, "exit-tree");
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; SetProcess(true);
        TreeExiting += () => ProbeController.OnTickerLifecycle(this, "tree-exiting");
        ProbeController.OnTickerLifecycle(this, "ready");
    }
    public override void _Process(double delta)
    {
        _ = delta; ProbeController.Tick(this);
    }
}
