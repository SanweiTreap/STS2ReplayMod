using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv007A;

[ModInitializer(nameof(Initialize))]
public static class ProbeEntryPoint
{
    public static void Initialize()
    {
        ProbeController.PrepareIdentity();
        var harmony = new Harmony("STS2ReplayMod.SPIKEINV007A.ScopeSafeOuterCloudGate");
        harmony.PatchAll();
        ProbeController.Activate();
    }
}

[HarmonyPatch(typeof(NGame), "InitializeGraphicsPreferences", new Type[] { })]
internal static class GraphicsPreferencesPatch
{
    [HarmonyPrefix]
    private static void Prefix(out ProbeController.GraphicsCallToken? __state)
    {
        if (!ProbeController.IsObserverRunningFirst()) { __state = null; return; }
        __state = ProbeController.OnGraphicsEntry();
    }

    [HarmonyPostfix]
    private static void Postfix(ProbeController.GraphicsCallToken? __state)
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        ProbeController.OnGraphicsNormalExit(__state);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(ProbeController.GraphicsCallToken? __state, Exception? __exception)
    {
        if (!ProbeController.IsObserverRunningFirst()) return __exception;
        ProbeController.OnGraphicsFinalizer(__state, __exception is not null);
        return __exception;
    }
}

[HarmonyPatch(typeof(NGame), "DoCloudSync", new Type[] { })]
internal static class OuterCloudPatch
{
    [HarmonyPostfix]
    private static void Postfix(Task __result)
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        ProbeController.OnOuterCloudReturned(__result);
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready), new Type[] { })]
internal static class MainMenuReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMainMenu __instance)
    {
        if (!ProbeController.IsObserverRunningFirst()) return;
        ProbeController.OnMainMenuReady(__instance);
    }
}

