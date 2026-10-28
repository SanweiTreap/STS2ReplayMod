using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace SpikeInv004B;

[ModInitializer(nameof(Initialize))]
public static class ProbeEntryPoint
{
    public static void Initialize()
    {
        new Harmony("STS2ReplayMod.SPIKEINV004B.DeepCopyDiagnostic").PatchAll();
        ProbeController.Initialize();
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class MainMenuReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMainMenu __instance) => ProbeController.ObserveMainMenu(__instance);
}
