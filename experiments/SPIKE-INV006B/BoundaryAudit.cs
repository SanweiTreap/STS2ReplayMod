using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv006B;

internal static class BoundaryAudit
{
    private static readonly object Gate = new();
    private static HashSet<object> _originalStores = new(ReferenceEqualityComparer.Instance);
    private static SaveManager? _original;
    private static SaveManager? _candidate;
    private static MockGodotFileIo? _memory;
    private static string? _expectedPayloadCanonical;
    private static int _enabled;
    private static int _sealed;
    private static BoundaryFailure _failure;
    private static bool _originalManagerSeen, _unknownManagerSeen, _originalStoreSeen, _unknownStoreSeen, _externalSeen;
    private static int _updates, _saves, _writes;
    private static bool _syncWrite, _exactName, _payloadEqual;

    internal static bool IsObserverRunningFirst() => Volatile.Read(ref _sealed) == 0 && Volatile.Read(ref _enabled) != 0;

    internal static void Start(SaveManager original, SaveManager candidate, MockGodotFileIo memory)
    {
        var stores = CollectStores(original);
        lock (Gate)
        {
            _original = original; _candidate = candidate; _memory = memory; _originalStores = stores;
            _expectedPayloadCanonical = null; _failure = BoundaryFailure.None;
            _originalManagerSeen = _unknownManagerSeen = _originalStoreSeen = _unknownStoreSeen = _externalSeen = false;
            _updates = _saves = _writes = 0; _syncWrite = _exactName = _payloadEqual = false;
            Volatile.Write(ref _sealed, 0); Volatile.Write(ref _enabled, 1);
        }
    }

    internal static void SetExpectedPayloadCanonical(string value)
    {
        if (!IsObserverRunningFirst()) return;
        lock (Gate) { if (!IsObserverRunningFirst()) return; _expectedPayloadCanonical = value; }
    }

    internal static bool ManagerEntry(MethodBase method, SaveManager instance)
    {
        if (!IsObserverRunningFirst()) return true;
        try
        {
            lock (Gate)
            {
                if (!IsObserverRunningFirst()) return true;
                if (ReferenceEquals(instance, _original)) { _originalManagerSeen = true; Fail(BoundaryFailure.OriginalManager); return false; }
                if (!ReferenceEquals(instance, _candidate)) { _unknownManagerSeen = true; Fail(BoundaryFailure.UnknownManager); return false; }
                if (method.Name == nameof(SaveManager.UpdateProgressAfterCombatWon) && _updates == 0 && _saves == 0) { _updates++; return true; }
                if (method.Name == nameof(SaveManager.SaveProgressFile) && _updates == 1 && _saves == 0) { _saves++; return true; }
                Fail(BoundaryFailure.WrongCandidateOperation);
                return false;
            }
        }
        catch { FailOutsideLock(BoundaryFailure.WrongCandidateOperation); return false; }
    }

    internal static bool StoreEntry(MethodBase method, object instance, object[] args)
    {
        if (!IsObserverRunningFirst()) return true;
        try
        {
            lock (Gate)
            {
                if (!IsObserverRunningFirst()) return true;
                if (_originalStores.Contains(instance)) { _originalStoreSeen = true; Fail(BoundaryFailure.OriginalStore); return false; }
                if (!ReferenceEquals(instance, _memory)) { _unknownStoreSeen = true; Fail(BoundaryFailure.UnknownStore); return false; }

                // Only the exact candidate store is authorized to expose its arguments.
                if (method.Name != "WriteFile" || _writes != 0 || args.Length < 2) { Fail(BoundaryFailure.WrongCandidateOperation); return false; }
                _writes++;
                _syncWrite = true;
                var logical = args[0] as string;
                _exactName = string.Equals(Normalize(logical), ProbeConstants.ProgressLogicalName, StringComparison.Ordinal);
                if (!_exactName) { Fail(BoundaryFailure.WrongCandidatePath); return false; }
                if (args[1] is not string payload || _expectedPayloadCanonical is null) { Fail(BoundaryFailure.WrongCandidatePayload); return false; }
                var parsed = JsonSerializationUtility.FromJson<SerializableProgress>(payload);
                if (!parsed.Success || parsed.SaveData is null) { Fail(BoundaryFailure.WrongCandidatePayload); return false; }
                _payloadEqual = string.Equals(JsonSupport.Canonicalize(JsonSerializationUtility.ToJson(parsed.SaveData)), _expectedPayloadCanonical, StringComparison.Ordinal);
                if (!_payloadEqual) { Fail(BoundaryFailure.WrongCandidatePayload); return false; }
                return true;
            }
        }
        catch { FailOutsideLock(BoundaryFailure.WrongCandidatePayload); return false; }
    }

    internal static void ExternalEntry()
    {
        if (!IsObserverRunningFirst()) return;
        lock (Gate) { if (!IsObserverRunningFirst()) return; _externalSeen = true; Fail(BoundaryFailure.ExternalBoundary); }
    }

    internal static void UserActionEntry()
    {
        if (!IsObserverRunningFirst()) return;
        lock (Gate) { if (!IsObserverRunningFirst()) return; Fail(BoundaryFailure.UserAction); }
    }

    internal static AuditSnapshot SealAndSnapshot()
    {
        Interlocked.Exchange(ref _sealed, 1); Volatile.Write(ref _enabled, 0);
        lock (Gate) return SnapshotLocked();
    }

    internal static AuditSnapshot Snapshot()
    {
        lock (Gate) return SnapshotLocked();
    }

    private static AuditSnapshot SnapshotLocked() => new(_failure, _originalManagerSeen, _unknownManagerSeen, _originalStoreSeen, _unknownStoreSeen, _externalSeen, _updates, _saves, _writes, _syncWrite, _exactName, _payloadEqual);
    private static void Fail(BoundaryFailure failure)
    {
        if (_failure != BoundaryFailure.None) return;
        _failure = failure;
        DirectWriteTransaction.SignalBoundaryFailure(failure);
    }
    private static void FailOutsideLock(BoundaryFailure failure) { lock (Gate) Fail(failure); }
    private static string Normalize(string? value) => (value ?? "").Replace('\\', '/').TrimStart('/');

    private static HashSet<object> CollectStores(SaveManager manager)
    {
        var result = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var field in manager.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var value = field.GetValue(manager);
            if (value is ISaveStore store) AddStore(store, result);
            if (value is null || field.FieldType.Namespace?.Contains("Saves.Managers", StringComparison.Ordinal) != true) continue;
            foreach (var child in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (child.GetValue(value) is ISaveStore childStore) AddStore(childStore, result);
        }
        return result;
    }
    private static void AddStore(ISaveStore store, HashSet<object> result)
    {
        if (!result.Add(store)) return;
        foreach (var field in store.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.GetValue(store) is ISaveStore child) AddStore(child, result);
    }
}

[HarmonyPatch]
internal static class DirectManagerBoundaryPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        AccessTools.Method(typeof(SaveManager), nameof(SaveManager.UpdateProgressAfterCombatWon)),
        AccessTools.Method(typeof(SaveManager), nameof(SaveManager.SaveProgressFile))
    }.Where(x => x is not null)!;
    private static bool Prefix(MethodBase __originalMethod, SaveManager __instance)
    {
        if (!BoundaryAudit.IsObserverRunningFirst()) return true;
        return BoundaryAudit.ManagerEntry(__originalMethod, __instance);
    }
}

[HarmonyPatch]
internal static class StoreMutatorBoundaryPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new HashSet<string>(StringComparer.Ordinal) { "WriteFile", "WriteFileAsync", "DeleteFile", "DeleteDirectory", "RenameFile", "BeginSaveBatch", "EndSaveBatch", "OverwriteCloudWithLocal", "OverwriteCloudWithLocalDirectory", "ForgetFile", "SyncCloudToLocal", "SyncCloudToLocalDirectory", "SetLastModifiedTime" };
        return typeof(SaveManager).Assembly.GetTypes().Where(t => typeof(ISaveStore).IsAssignableFrom(t) && !t.IsInterface)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(m => names.Contains(m.Name));
    }
    private static bool Prefix(MethodBase __originalMethod, object __instance, object[] __args)
    {
        if (!BoundaryAudit.IsObserverRunningFirst()) return true;
        return BoundaryAudit.StoreEntry(__originalMethod, __instance, __args);
    }
}

[HarmonyPatch]
internal static class ExternalBoundaryPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(SaveManager).Assembly.GetTypes()
        .Where(t => t.Name.Contains("Metric", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Leaderboard", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Achievement", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Daily", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Unlock", StringComparison.OrdinalIgnoreCase))
        .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        .Where(m => (m.Name.StartsWith("Upload", StringComparison.OrdinalIgnoreCase) || m.Name.StartsWith("Submit", StringComparison.OrdinalIgnoreCase) || string.Equals(m.Name, "Unlock", StringComparison.OrdinalIgnoreCase)) && !m.ContainsGenericParameters && m.GetMethodBody() is not null);
    private static bool Prefix()
    {
        if (!BoundaryAudit.IsObserverRunningFirst()) return true;
        BoundaryAudit.ExternalEntry();
        return false;
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame.Quit))]
internal static class QuitGuardPatch
{
    private static bool Prefix() { if (!BoundaryAudit.IsObserverRunningFirst()) return true; BoundaryAudit.UserActionEntry(); return false; }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Input))]
internal static class InputGuardPatch
{
    private static bool Prefix() => !BoundaryAudit.IsObserverRunningFirst();
}

[HarmonyPatch]
internal static class MainMenuActionGuardPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(NMainMenu).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(m => m.Name is "OnContinueButtonPressed" or "SingleplayerButtonPressed" or "OpenMultiplayerSubmenu");
    private static bool Prefix() { if (!BoundaryAudit.IsObserverRunningFirst()) return true; BoundaryAudit.UserActionEntry(); return false; }
}
