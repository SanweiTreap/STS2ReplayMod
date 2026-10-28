using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv005A;

[ModInitializer(nameof(Initialize))]
public static class ProbeEntryPoint
{
    public static void Initialize()
    {
        var harmony = new Harmony("STS2ReplayMod.SPIKEINV005A.StartupBarrier");
        harmony.PatchAll();
        ProbeController.Initialize();
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class MainMenuReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMainMenu __instance) => ProbeController.ObserveMainMenu(__instance);
}

[HarmonyPatch(typeof(NGame), "InitializeGraphicsPreferences")]
internal static class GraphicsPreferencesPatch
{
    [HarmonyPrefix]
    private static void Prefix(out ProbeController.GraphicsCallState? __state)
    {
        try { __state = ProbeController.GraphicsEntry(); }
        catch (Exception ex) { __state = null; ProbeController.InstrumentationFailureObserved("graphics prefix", ex); }
    }

    [HarmonyPostfix]
    private static void Postfix(ProbeController.GraphicsCallState? __state)
    {
        try { if (__state is not null) ProbeController.GraphicsNormalExit(__state); }
        catch (Exception ex) { ProbeController.InstrumentationFailureObserved("graphics postfix", ex); }
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(ProbeController.GraphicsCallState? __state, Exception? __exception)
    {
        try { if (__state is not null) ProbeController.GraphicsFinalizer(__state, __exception); }
        catch (Exception ex) { ProbeController.InstrumentationFailureObserved("graphics finalizer", ex); }
        return __exception;
    }
}
