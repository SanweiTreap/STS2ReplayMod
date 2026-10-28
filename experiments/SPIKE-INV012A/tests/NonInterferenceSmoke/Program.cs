using HarmonyLib;

var harmony = new Harmony("STS2ReplayMod.SPIKEINV012A.NonInterferenceSmoke");
try
{
    harmony.PatchAll(typeof(Program).Assembly);
    var success = Task.CompletedTask;
    if (!ReferenceEquals(success, Dummy.Work(success))) throw new Exception("success Task replaced");
    await Dummy.Work(success);
    var fault = Task.FromException(new InvalidOperationException("production fault"));
    if (!ReferenceEquals(fault, Dummy.Work(fault))) throw new Exception("fault Task replaced");
    try { await fault; throw new Exception("fault suppressed"); }
    catch (InvalidOperationException ex) { if (ex.Message != "production fault") throw; }
    var canceled = Task.FromCanceled(new CancellationToken(true));
    if (!ReferenceEquals(canceled, Dummy.Work(canceled))) throw new Exception("cancel Task replaced");
    try { await canceled; throw new Exception("cancel suppressed"); }
    catch (TaskCanceledException) { }
    try { Dummy.ThrowSynchronously(); throw new Exception("synchronous fault suppressed"); }
    catch (ApplicationException ex) { if (ex.Message != "original") throw; }
    if (Dummy.Calls != 5) throw new Exception("original skipped");
    Console.WriteLine("PASS dummy observer throws: original invoked, Task identity/success/fault/cancel and synchronous throw preserved");
}
finally { harmony.UnpatchAll(harmony.Id); }
if (harmony.GetPatchedMethods().Any()) throw new Exception("unpatch failed");

internal static class Dummy
{
    internal static int Calls;
    public static Task Work(Task task) { Calls++; return task; }
    public static void ThrowSynchronously() { Calls++; throw new ApplicationException("original"); }
}
[HarmonyPatch(typeof(Dummy), nameof(Dummy.Work), new[] { typeof(Task) })]
internal static class DummyTaskPatch
{
    [HarmonyPrefix] private static void Prefix() => Safe(() => throw new InvalidOperationException("observer"));
    [HarmonyPostfix] private static void Postfix(Task __result) => Safe(() =>
    {
        _ = __result.ContinueWith(_ => Safe(() => throw new InvalidOperationException("observer continuation")),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    });
    private static void Safe(Action action) { try { action(); } catch (Exception) { } }
}
[HarmonyPatch(typeof(Dummy), nameof(Dummy.ThrowSynchronously), new Type[] { })]
internal static class DummyThrowPatch
{
    [HarmonyPrefix] private static void Prefix() { try { throw new InvalidOperationException("observer"); } catch (Exception) { } }
}
