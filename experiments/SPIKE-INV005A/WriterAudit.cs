using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpikeInv005A;

internal static class WriterAudit
{
    internal sealed class CallState
    {
        internal required long EntrySequence;
        internal required string Layer;
        internal required string Operation;
        internal required object Target;
        internal required string? LogicalPath;
        internal required long? PayloadLength;
        internal required string? PayloadSha256;
        internal required bool IsAsync;
        internal bool Returned;
    }

    private static readonly object Gate = new();
    private static readonly List<WriterEvent> Events = new();
    private static readonly Dictionary<long, string> AsyncTasks = new();
    private static int _activeComponentWriters;
    private static int _activeSyncStoreOperations;
    private static bool _enabled;
    private static int? _originalManagerIdentity;
    private static HashSet<object> _originalStores = new(ReferenceEqualityComparer.Instance);

    internal static int ActiveComponentWriters => Volatile.Read(ref _activeComponentWriters);
    internal static int ActiveSyncStoreOperations => Volatile.Read(ref _activeSyncStoreOperations);
    internal static long LastWriterEntrySequence { get { lock (Gate) return Events.Where(x => x.Phase == "entry").Select(x => x.Sequence).DefaultIfEmpty().Max(); } }
    internal static IReadOnlyList<WriterEvent> Snapshot() { lock (Gate) return Events.ToList(); }
    internal static IReadOnlyDictionary<long, string> AsyncTaskSnapshot() { lock (Gate) return new Dictionary<long, string>(AsyncTasks); }
    internal static bool AllAsyncTerminalSuccessfully { get { lock (Gate) return AsyncTasks.Values.All(x => x == "RanToCompletion"); } }
    internal static bool HasAsyncFaultOrCancellation { get { lock (Gate) return AsyncTasks.Values.Any(x => x is "Faulted" or "Canceled"); } }
    internal static bool HasPendingAsync { get { lock (Gate) return AsyncTasks.Values.Any(x => x is "WaitingForActivation" or "WaitingToRun" or "Running" or "WaitingForChildrenToComplete"); } }

    internal static void Start(SaveManager original)
    {
        _originalManagerIdentity = RuntimeHelpers.GetHashCode(original);
        _originalStores = CollectStores(original);
        _enabled = true;
    }

    internal static void Stop() => _enabled = false;

    internal static void ComponentEntry(MethodBase method, object target)
    {
        if (!_enabled) return;
        Interlocked.Increment(ref _activeComponentWriters);
        Add("component", "entry", method.Name, target, null, false, null, null, null, null);
        ProbeController.WriterEntryObserved();
    }

    internal static void ComponentExit(MethodBase method, object target, Exception? exception)
    {
        if (!_enabled) return;
        Add("component", "exit", method.Name, target, null, false, null, null, null, exception?.ToString());
        Interlocked.Decrement(ref _activeComponentWriters);
    }

    internal static CallState StoreEntry(MethodBase method, object target, object[] args, bool isAsync)
    {
        var (path, length, hash) = SummarizeArguments(method, args);
        var state = new CallState
        {
            EntrySequence = ProbeController.NextSequence(), Layer = "store", Operation = method.Name, Target = target,
            LogicalPath = path, PayloadLength = length, PayloadSha256 = hash, IsAsync = isAsync
        };
        if (!_enabled) return state;
        if (!isAsync) Interlocked.Increment(ref _activeSyncStoreOperations);
        AddWithSequence(state.EntrySequence, "store", "entry", method.Name, target, path, isAsync, isAsync ? "ReturnedTaskPending" : null, length, hash, null);
        if (!_originalStores.Contains(target)) ProbeController.InstrumentationFailureObserved("store identity", new InvalidOperationException("Observed store is not reachable from original SaveManager: " + target.GetType().FullName));
        ProbeController.WriterEntryObserved();
        return state;
    }

    internal static void StoreSyncExit(CallState state, Exception? exception)
    {
        if (!_enabled) return;
        Add("store", "exit", state.Operation, state.Target, state.LogicalPath, false, null, state.PayloadLength, state.PayloadSha256, exception?.ToString());
        Interlocked.Decrement(ref _activeSyncStoreOperations);
    }

    internal static void StoreAsyncReturned(CallState state, Task task)
    {
        if (!_enabled) return;
        state.Returned = true;
        lock (Gate) AsyncTasks[state.EntrySequence] = NormalizeTaskStatus(task);
        Add("store", "method-return", state.Operation, state.Target, state.LogicalPath, true, NormalizeTaskStatus(task), state.PayloadLength, state.PayloadSha256, null);
        _ = task.ContinueWith(completed =>
        {
            var status = NormalizeTaskStatus(completed);
            lock (Gate) AsyncTasks[state.EntrySequence] = status;
            Add("store", "task-terminal", state.Operation, state.Target, state.LogicalPath, true, status, state.PayloadLength, state.PayloadSha256, completed.Exception?.ToString());
            ProbeController.AsyncTaskTerminalObserved(status);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    internal static void StoreAsyncFinalizer(CallState state, Exception? exception)
    {
        if (!_enabled || exception is null || state.Returned) return;
        lock (Gate) AsyncTasks[state.EntrySequence] = "FaultedBeforeTaskReturn";
        Add("store", "throw-before-task-return", state.Operation, state.Target, state.LogicalPath, true, "Faulted", state.PayloadLength, state.PayloadSha256, exception.ToString());
        ProbeController.AsyncTaskTerminalObserved("Faulted");
    }

    private static string NormalizeTaskStatus(Task task) => task.IsCanceled ? "Canceled" : task.IsFaulted ? "Faulted" : task.IsCompletedSuccessfully ? "RanToCompletion" : task.Status.ToString();

    private static (string? Path, long? Length, string? Hash) SummarizeArguments(MethodBase method, object[] args)
    {
        string? path = null;
        if (args.FirstOrDefault() is string first) path = SanitizeLogicalPath(first);
        if (!method.Name.StartsWith("WriteFile", StringComparison.Ordinal) || args.Length < 2) return (path, null, null);
        return args[1] switch
        {
            byte[] bytes => (path, bytes.LongLength, JsonSupport.Sha256Bytes(bytes)),
            string text => (path, text.Length, JsonSupport.Sha256Text(text)),
            _ => (path, null, null)
        };
    }

    private static string SanitizeLogicalPath(string value)
    {
        var normalized = value.Replace('\\', '/');
        if (Path.IsPathRooted(value) || normalized.Contains(":/", StringComparison.Ordinal)) return "<rooted>/" + Path.GetFileName(value);
        return normalized.Length <= 240 ? normalized : normalized[^240..];
    }

    private static void Add(string layer, string phase, string operation, object target, string? path, bool async, string? taskStatus, long? length, string? hash, string? exception)
        => AddWithSequence(ProbeController.NextSequence(), layer, phase, operation, target, path, async, taskStatus, length, hash, exception);

    private static void AddWithSequence(long sequence, string layer, string phase, string operation, object target, string? path, bool async, string? taskStatus, long? length, string? hash, string? exception)
    {
        var item = new WriterEvent(sequence, DateTimeOffset.UtcNow.ToString("O"), ProbeController.ProcessFrame(), Environment.CurrentManagedThreadId,
            layer, phase, operation, target.GetType().FullName ?? target.GetType().Name, RuntimeHelpers.GetHashCode(target),
            layer == "store" ? (_originalStores.Contains(target) ? "original-manager-store" : "unknown-store") : "original-manager-component",
            _originalManagerIdentity,
            path, async, taskStatus, length, hash, exception);
        lock (Gate) Events.Add(item);
    }

    private static HashSet<object> CollectStores(SaveManager manager)
    {
        var result = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var field in manager.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var value = field.GetValue(manager);
            if (value is ISaveStore direct) AddStoreRecursive(direct, result);
            if (value is null || field.FieldType.Namespace?.Contains("Saves.Managers", StringComparison.Ordinal) != true) continue;
            foreach (var childField in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (childField.GetValue(value) is ISaveStore store) AddStoreRecursive(store, result);
        }
        return result;
    }

    private static void AddStoreRecursive(ISaveStore store, HashSet<object> result)
    {
        if (!result.Add(store)) return;
        foreach (var field in store.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.GetValue(store) is ISaveStore child) AddStoreRecursive(child, result);
    }
}

[HarmonyPatch]
internal static class ComponentWriterPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        AccessTools.Method(typeof(SettingsSaveManager), "SaveSettings"),
        AccessTools.Method(typeof(PrefsSaveManager), "SavePrefs"),
        AccessTools.Method(typeof(ProgressSaveManager), "SaveProgress")
    }.Where(x => x is not null)!;

    private static void Prefix(MethodBase __originalMethod, object __instance, out bool __state)
    {
        try { WriterAudit.ComponentEntry(__originalMethod, __instance); __state = true; }
        catch (Exception ex) { __state = false; ProbeController.InstrumentationFailureObserved("component writer prefix", ex); }
    }
    private static Exception? Finalizer(MethodBase __originalMethod, object __instance, bool __state, Exception? __exception)
    {
        try { if (__state) WriterAudit.ComponentExit(__originalMethod, __instance, __exception); }
        catch (Exception ex) { ProbeController.InstrumentationFailureObserved("component writer finalizer", ex); }
        return __exception;
    }
}

[HarmonyPatch]
internal static class SyncStoreWriterPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => StorePatchTargets.All().Where(x => !typeof(Task).IsAssignableFrom(((MethodInfo)x).ReturnType));
    private static void Prefix(MethodBase __originalMethod, object __instance, object[] __args, out WriterAudit.CallState? __state)
    {
        try { __state = WriterAudit.StoreEntry(__originalMethod, __instance, __args, false); }
        catch (Exception ex) { __state = null; ProbeController.InstrumentationFailureObserved("sync store prefix", ex); }
    }
    private static Exception? Finalizer(WriterAudit.CallState? __state, Exception? __exception)
    {
        try { if (__state is not null) WriterAudit.StoreSyncExit(__state, __exception); }
        catch (Exception ex) { ProbeController.InstrumentationFailureObserved("sync store finalizer", ex); }
        return __exception;
    }
}

[HarmonyPatch]
internal static class AsyncStoreWriterPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => StorePatchTargets.All().Where(x => typeof(Task).IsAssignableFrom(((MethodInfo)x).ReturnType));
    private static void Prefix(MethodBase __originalMethod, object __instance, object[] __args, out WriterAudit.CallState? __state)
    {
        try { __state = WriterAudit.StoreEntry(__originalMethod, __instance, __args, true); }
        catch (Exception ex) { __state = null; ProbeController.InstrumentationFailureObserved("async store prefix", ex); }
    }
    private static void Postfix(WriterAudit.CallState? __state, Task __result)
    {
        try { if (__state is not null) WriterAudit.StoreAsyncReturned(__state, __result); }
        catch (Exception ex) { ProbeController.InstrumentationFailureObserved("async store postfix", ex); }
    }
    private static Exception? Finalizer(WriterAudit.CallState? __state, Exception? __exception)
    {
        try { if (__state is not null) WriterAudit.StoreAsyncFinalizer(__state, __exception); }
        catch (Exception ex) { ProbeController.InstrumentationFailureObserved("async store finalizer", ex); }
        return __exception;
    }
}

internal static class StorePatchTargets
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "WriteFile", "WriteFileAsync", "DeleteFile", "DeleteDirectory", "RenameFile", "SetLastModifiedTime", "SetLastModified",
        "BeginSaveBatch", "EndSaveBatch", "OverwriteCloudWithLocal", "OverwriteCloudWithLocalDirectory", "ForgetFile",
        "SyncCloudToLocal", "SyncCloudToLocalDirectory"
    };

    internal static IEnumerable<MethodBase> All() => typeof(SaveManager).Assembly.GetTypes()
        .Where(type => typeof(ISaveStore).IsAssignableFrom(type) && !type.IsInterface)
        .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        .Where(method => Names.Contains(method.Name) && !method.ContainsGenericParameters);
}

[HarmonyPatch]
internal static class ForbiddenOverrideAuditPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        AccessTools.Method(typeof(SaveManager), nameof(SaveManager.MockInstanceForTesting)),
        AccessTools.Method(typeof(SaveManager), nameof(SaveManager.ClearInstanceForTesting))
    }.Where(x => x is not null)!;

    private static void Prefix(MethodBase __originalMethod) => ProbeController.ForbiddenOperationObserved("SaveManager." + __originalMethod.Name);
}
