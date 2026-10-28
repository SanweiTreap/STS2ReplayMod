using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv003A;

internal sealed record AuditEvent(string Utc, string Boundary, string Method, string Target, string[] Arguments);

internal static class TransactionAudit
{
    private static readonly object Gate = new();
    private static readonly List<AuditEvent> Events = new();
    private static HashSet<object> _originalStores = new(ReferenceEqualityComparer.Instance);
    private static SaveManager? _originalManager;
    private static SaveManager? _isolatedManager;
    private static MockGodotFileIo? _memoryStore;
    private static bool _active;
    private static bool _restoring;
    private static string? _violation;

    internal static bool Active => _active;
    internal static string? Violation => _violation;

    internal static void Start(SaveManager original, SaveManager isolated, MockGodotFileIo memoryStore)
    {
        lock (Gate)
        {
            Events.Clear();
            _originalManager = original;
            _isolatedManager = isolated;
            _memoryStore = memoryStore;
            _originalStores = CollectStores(original);
            _violation = null;
            _restoring = false;
            _active = true;
            Add("transaction", "start", isolated, Array.Empty<object>());
        }
    }

    internal static void BeginRestore()
    {
        _restoring = true;
        Add("transaction", "begin-restore", _isolatedManager, Array.Empty<object>());
    }

    internal static void Stop() => _active = false;

    internal static bool HasEvent(string boundary, string method)
    {
        lock (Gate) return Events.Any(x => x.Boundary == boundary && x.Method == method);
    }

    internal static bool HasIsolatedStoreWriteContaining(string pathFragment)
    {
        lock (Gate)
        {
            return Events.Any(x => x.Boundary == "isolated-store"
                                   && (x.Method == "WriteFile" || x.Method == "WriteFileAsync")
                                   && x.Arguments.Any(argument => argument.Contains(pathFragment, StringComparison.OrdinalIgnoreCase)));
        }
    }

    internal static void RecordSaveManager(MethodBase method, SaveManager manager, object[] args)
    {
        if (!_active) return;
        var boundary = ReferenceEquals(manager, _originalManager) ? "original-manager" :
            ReferenceEquals(manager, _isolatedManager) ? "isolated-manager" : "unknown-manager";
        Add(boundary, method.Name, manager, args);
        if (method.Name == nameof(SaveManager.SwitchProfileId))
            Violate("profile change attempted during replay transaction");
        else if (boundary is "original-manager" or "unknown-manager")
            Violate($"{boundary} call: {method.DeclaringType?.Name}.{method.Name}");
    }

    internal static void RecordStore(MethodBase method, object store, object[] args)
    {
        if (!_active) return;
        var boundary = _originalStores.Contains(store) ? "original-store" :
            ReferenceEquals(store, _memoryStore) ? "isolated-store" : "unknown-store";
        Add(boundary, method.Name, store, args);
        if (boundary is "original-store" or "unknown-store")
            Violate($"{boundary} call: {method.DeclaringType?.Name}.{method.Name}");
    }

    internal static void RecordExternal(MethodBase method, object[] args)
    {
        if (!_active) return;
        Add("external-submit", method.DeclaringType?.FullName + "." + method.Name, null, args);
        Violate($"external submit boundary: {method.DeclaringType?.FullName}.{method.Name}");
    }

    internal static void RecordOverride(MethodBase method, object[] args)
    {
        if (!_active) return;
        Add("manager-override", method.Name, null, args);
        if (!_restoring && method.Name == nameof(SaveManager.ClearInstanceForTesting))
            Violate("unexpected SaveManager override clear");
    }

    internal static object Snapshot()
    {
        lock (Gate)
        {
            var calls = _memoryStore?.Calls.Select(call => new
            {
                method = call.Method,
                arguments = call.Args.Select(FormatArgument).ToArray(),
            }).ToList() ?? new();
            return new
            {
                violation = _violation,
                originalManagerIdentity = _originalManager is null ? (int?)null : RuntimeHelpers.GetHashCode(_originalManager),
                isolatedManagerIdentity = _isolatedManager is null ? (int?)null : RuntimeHelpers.GetHashCode(_isolatedManager),
                originalStoreCount = _originalStores.Count,
                events = Events.ToList(),
                isolatedStoreCalls = calls,
                counters = Events.GroupBy(x => x.Boundary).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal),
            };
        }
    }

    private static void Violate(string reason)
    {
        if (_violation is not null) return;
        _violation = reason;
        ProbeController.RequestFailClosed(reason);
    }

    private static void Add(string boundary, string method, object? target, object[] args)
    {
        lock (Gate)
        {
            Events.Add(new AuditEvent(
                DateTimeOffset.UtcNow.ToString("O"),
                boundary,
                method,
                target is null ? "static" : $"{target.GetType().FullName}#{RuntimeHelpers.GetHashCode(target)}",
                args.Take(4).Select(FormatArgument).ToArray()));
        }
    }

    private static string FormatArgument(object? value)
    {
        if (value is null) return "null";
        if (value is string text) return text.Length <= 160 ? text : text[..160] + "…";
        if (value is byte[] bytes) return $"byte[{bytes.Length}]";
        return value.GetType().IsPrimitive || value is Enum ? value.ToString() ?? "" : value.GetType().FullName ?? value.GetType().Name;
    }

    private static HashSet<object> CollectStores(SaveManager manager)
    {
        var result = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var field in manager.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var value = field.GetValue(manager);
            if (value is ISaveStore directStore) AddStoreRecursive(directStore, result);
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

[HarmonyPatch(typeof(NGame), nameof(NGame.Quit))]
internal static class QuitDuringTransactionGuardPatch
{
    private static bool Prefix()
    {
        if (!TransactionAudit.Active) return true;
        ProbeController.RequestFailClosed("Quit attempted during replay transaction");
        return false;
    }
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SwitchProfileId))]
internal static class ProfileChangeDuringTransactionGuardPatch
{
    private static bool Prefix()
    {
        if (!TransactionAudit.Active) return true;
        ProbeController.RequestFailClosed("Profile change attempted during replay transaction");
        return false;
    }
}

[HarmonyPatch]
internal static class MainMenuRunStartDuringTransactionGuardPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new[] { "OnContinueButtonPressed", "SingleplayerButtonPressed", "OpenMultiplayerSubmenu" };
        return typeof(NMainMenu).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => names.Contains(method.Name, StringComparer.Ordinal));
    }

    private static bool Prefix(MethodBase __originalMethod)
    {
        if (!TransactionAudit.Active) return true;
        ProbeController.RequestFailClosed($"Main-menu run action attempted during replay transaction: {__originalMethod.Name}");
        return false;
    }
}

[HarmonyPatch]
internal static class SaveManagerAuditPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(SaveManager.SaveRun), nameof(SaveManager.SaveProgressFile), nameof(SaveManager.UpdateProgressAfterCombatWon),
            nameof(SaveManager.SaveRunHistory), nameof(SaveManager.SavePrefsFile), nameof(SaveManager.SaveSettings),
            nameof(SaveManager.SaveProfile), nameof(SaveManager.DeleteCurrentRun), nameof(SaveManager.DeleteCurrentMultiplayerRun),
            nameof(SaveManager.DeleteDirectoryRecursive), "DeleteInDirectoryRecursive", nameof(SaveManager.BeginSaveBatch),
            nameof(SaveManager.EndSaveBatch), nameof(SaveManager.SwitchProfileId)
        };
        return typeof(SaveManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => names.Contains(method.Name));
    }

    private static void Prefix(MethodBase __originalMethod, SaveManager __instance, object[] __args) =>
        TransactionAudit.RecordSaveManager(__originalMethod, __instance, __args);
}

[HarmonyPatch]
internal static class SaveStoreAuditPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "WriteFile", "WriteFileAsync", "DeleteFile", "DeleteDirectory", "RenameFile", "BeginSaveBatch", "EndSaveBatch",
            "OverwriteCloudWithLocal", "OverwriteCloudWithLocalDirectory", "ForgetFile", "SyncCloudToLocal", "SyncCloudToLocalDirectory"
        };
        var assembly = typeof(SaveManager).Assembly;
        return assembly.GetTypes()
            .Where(type => typeof(ISaveStore).IsAssignableFrom(type) && !type.IsInterface)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(method => names.Contains(method.Name));
    }

    private static void Prefix(MethodBase __originalMethod, object __instance, object[] __args) =>
        TransactionAudit.RecordStore(__originalMethod, __instance, __args);
}

[HarmonyPatch]
internal static class SaveOverrideAuditPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        AccessTools.Method(typeof(SaveManager), nameof(SaveManager.MockInstanceForTesting)),
        AccessTools.Method(typeof(SaveManager), nameof(SaveManager.ClearInstanceForTesting)),
    }.Where(x => x is not null)!;

    private static void Prefix(MethodBase __originalMethod, object[] __args) => TransactionAudit.RecordOverride(__originalMethod, __args);
}

[HarmonyPatch]
internal static class ExternalSubmitAuditPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(SaveManager).Assembly.GetTypes()
            .Where(type => type.Name.Contains("Metric", StringComparison.OrdinalIgnoreCase)
                           || type.Name.Contains("Leaderboard", StringComparison.OrdinalIgnoreCase)
                           || type.Name.Contains("Achievement", StringComparison.OrdinalIgnoreCase)
                           || type.Name.Contains("Daily", StringComparison.OrdinalIgnoreCase))
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(method => method.Name.StartsWith("Upload", StringComparison.OrdinalIgnoreCase)
                             || method.Name.StartsWith("Submit", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(method.Name, "Unlock", StringComparison.OrdinalIgnoreCase))
            .Where(method => !method.ContainsGenericParameters && method.GetMethodBody() is not null);
    }

    private static void Prefix(MethodBase __originalMethod, object[] __args) => TransactionAudit.RecordExternal(__originalMethod, __args);
}
