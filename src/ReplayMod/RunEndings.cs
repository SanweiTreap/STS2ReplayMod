using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal static class RunEndings
{
    internal static void Install(Harmony h)=>h.Patch(AccessTools.DeclaredMethod(typeof(RunManager),"OnEnded"),
        prefix:new HarmonyMethod(typeof(RunEndings),nameof(NativeEnd)));
    private static void NativeEnd(RunManager __instance,bool isVictory)
    {
        // Native WinRun marks victory, then force-kills the player. OnEnded(false)
        // from that death is a duplicate, not a second loss or abandonment.
        if(RunPlayback.Active||!RunRecording.Enabled||RunPatches.Field(__instance,"_runHistoryWasUploaded") is true)return;
        Runtime.Guard(()=>RunRecording.NoteLifecycle(isVictory?"victory":__instance.IsAbandoned?"abandoned":"death",
            isVictory?"native-OnEnded(true)":__instance.IsAbandoned?"native-Abandon-OnEnded(false)":"native-death-OnEnded(false)",
            RunResources.Capture()));
    }
}
