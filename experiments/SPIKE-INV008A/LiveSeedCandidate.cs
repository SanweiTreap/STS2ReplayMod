using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Test;

namespace SpikeInv008A;

internal static class LiveSeedCandidate
{
    private const int MaxDiffs = 256;

    internal sealed record LiveState(SaveManager Manager, ProfileSave Profile, string ProfileJson, string SettingsJson, string PrefsJson, string ProgressJson);
    internal sealed record CandidateResult(
        string Name,
        SaveManager Manager,
        MockGodotFileIo Store,
        IReadOnlyList<ComponentComparison> Components,
        IReadOnlyList<StoreCallEvidence> StoreAudit,
        int CurrentProfileId,
        int ProfileLastProfileId,
        bool StoreContainmentPassed,
        IReadOnlyList<string> StoreContainmentFailures,
        ReferenceCheck References);

    internal static LiveState CaptureLive(SaveManager manager)
    {
        var progress = CaptureProgress(manager);
        return new LiveState(
            manager,
            ReadProfile(manager),
            JsonSerializationUtility.ToJson(ReadProfile(manager)),
            JsonSerializationUtility.ToJson(manager.SettingsSave),
            JsonSerializationUtility.ToJson(manager.PrefsSave),
            JsonSerializationUtility.ToJson(progress));
    }

    internal static SerializableProgress CaptureProgress(SaveManager manager)
    {
        var progress = manager.Progress.ToSerializable();
        progress.SchemaVersion = GetLatestProgressSchema(manager);
        return progress;
    }

    internal static string CanonicalProgress(SaveManager manager)
        => JsonSupport.Canonicalize(JsonSerializationUtility.ToJson(CaptureProgress(manager)));

    internal static CandidateResult CreateLiveCandidate(LiveState live)
    {
        var store = new MockGodotFileIo("user://spike-inv008a-live-candidate") { getCurrentTime = () => DateTimeOffset.UtcNow };
        store.WriteFile(ProfileSaveManager.GetProfileSavePath(null), live.ProfileJson);
        store.WriteFile(SettingsSaveManager.settingsSaveFileName, live.SettingsJson);
        store.WriteFile(PrefsSaveManager.GetPrefsPath(live.Manager.CurrentProfileId, null), live.PrefsJson);
        store.WriteFile(ProgressSaveManager.GetProgressPathForProfile(live.Manager.CurrentProfileId, null), live.ProgressJson);
        var audit = new List<StoreCallEvidence>();
        CaptureCalls(store, audit, "seed-live-serialized", 0);
        return Initialize("live-seeded", live, store, audit);
    }

    private static CandidateResult Initialize(string name, LiveState live, MockGodotFileIo store, List<StoreCallEvidence> audit)
    {
        var cursor = store.Calls.Count;
        var manager = new SaveManager(store, store, true);
        CaptureCalls(store, audit, "construct", cursor); cursor = store.Calls.Count;
        manager.InitProfileId(null);
        CaptureCalls(store, audit, "init-profile", cursor); cursor = store.Calls.Count;
        var settings = manager.InitSettingsData();
        CaptureCalls(store, audit, "init-settings", cursor); cursor = store.Calls.Count;
        var prefs = manager.InitPrefsData();
        CaptureCalls(store, audit, "init-prefs", cursor); cursor = store.Calls.Count;
        var progress = manager.InitProgressData();
        CaptureCalls(store, audit, "init-progress", cursor);

        if (!settings.Success || !prefs.Success || !progress.Success)
            throw new InvalidDataException($"{name} initialization failed: settings={settings.Status}, prefs={prefs.Status}, progress={progress.Status}");

        var candidateLive = CaptureLive(manager);
        var components = new[]
        {
            Compare("Profile", live.ProfileJson, candidateLive.ProfileJson),
            Compare("Settings", live.SettingsJson, candidateLive.SettingsJson),
            Compare("Prefs", live.PrefsJson, candidateLive.PrefsJson),
            Compare("Progress", live.ProgressJson, candidateLive.ProgressJson),
        };
        var containmentFailures = VerifyStoreContainment(manager, store);
        return new CandidateResult(
            name,
            manager,
            store,
            components,
            audit,
            manager.CurrentProfileId,
            ReadProfile(manager).LastProfileId,
            containmentFailures.Count == 0,
            containmentFailures,
            CheckReferences(live.Manager, manager));
    }

    private static ComponentComparison Compare(string component, string liveJson, string candidateJson)
    {
        var liveCanonical = JsonSupport.Canonicalize(liveJson);
        var candidateCanonical = JsonSupport.Canonicalize(candidateJson);
        using var left = JsonDocument.Parse(liveJson);
        using var right = JsonDocument.Parse(candidateJson);
        var allDiffs = new List<JsonDiff>();
        Diff(left.RootElement, right.RootElement, "$", allDiffs);
        return new ComponentComparison(
            component,
            Snapshot(liveJson, liveCanonical),
            Snapshot(candidateJson, candidateCanonical),
            string.Equals(liveJson, candidateJson, StringComparison.Ordinal),
            string.Equals(liveCanonical, candidateCanonical, StringComparison.Ordinal),
            allDiffs.Count,
            allDiffs.Count > MaxDiffs,
            allDiffs.Take(MaxDiffs).ToList());
    }

    private static JsonSnapshot Snapshot(string raw, string canonical)
        => new(JsonSupport.Sha256Text(raw), JsonSupport.Sha256Text(canonical), raw.Length, canonical.Length);

    private static void Diff(JsonElement live, JsonElement candidate, string path, List<JsonDiff> output)
    {
        if (live.ValueKind != candidate.ValueKind)
        {
            output.Add(new JsonDiff(path, "type-mismatch", live.ValueKind.ToString(), candidate.ValueKind.ToString(), Summarize(live), Summarize(candidate)));
            return;
        }
        switch (live.ValueKind)
        {
            case JsonValueKind.Object:
                var left = live.EnumerateObject().ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
                var right = candidate.EnumerateObject().ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
                foreach (var name in left.Keys.Union(right.Keys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
                {
                    var child = path + "." + EscapePath(name);
                    if (!left.TryGetValue(name, out var lv)) output.Add(new JsonDiff(child, "missing-live", null, right[name].ValueKind.ToString(), null, Summarize(right[name])));
                    else if (!right.TryGetValue(name, out var rv)) output.Add(new JsonDiff(child, "missing-candidate", lv.ValueKind.ToString(), null, Summarize(lv), null));
                    else Diff(lv, rv, child, output);
                }
                break;
            case JsonValueKind.Array:
                var la = live.EnumerateArray().ToArray();
                var ra = candidate.EnumerateArray().ToArray();
                if (la.Length != ra.Length)
                    output.Add(new JsonDiff(path, "array-length", "Array", "Array", new { length = la.Length }, new { length = ra.Length }));
                for (var i = 0; i < Math.Min(la.Length, ra.Length); i++) Diff(la[i], ra[i], $"{path}[{i}]", output);
                break;
            default:
                if (live.GetRawText() != candidate.GetRawText())
                    output.Add(new JsonDiff(path, "value-mismatch", live.ValueKind.ToString(), candidate.ValueKind.ToString(), Summarize(live), Summarize(candidate)));
                break;
        }
    }

    private static object? Summarize(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => new { type = "string", length = value.GetString()?.Length ?? 0, sha256 = JsonSupport.Sha256Text(value.GetString() ?? "") },
        JsonValueKind.Number => new { type = "number", value = value.GetRawText() },
        JsonValueKind.True => new { type = "boolean", value = true },
        JsonValueKind.False => new { type = "boolean", value = false },
        JsonValueKind.Null => new { type = "null" },
        JsonValueKind.Array => new { type = "array", length = value.GetArrayLength() },
        JsonValueKind.Object => new { type = "object", propertyCount = value.EnumerateObject().Count() },
        _ => new { type = value.ValueKind.ToString() },
    };

    private static string EscapePath(string name) => name.All(ch => char.IsLetterOrDigit(ch) || ch == '_') ? name : "['" + name.Replace("'", "\\'") + "']";

    private static void CaptureCalls(MockGodotFileIo store, List<StoreCallEvidence> output, string phase, int start)
    {
        for (var i = start; i < store.Calls.Count; i++)
        {
            var call = store.Calls[i];
            var args = call.Item2 ?? Array.Empty<object>();
            output.Add(new StoreCallEvidence(phase, i, call.Item1, args.FirstOrDefault() as string, args.Select(SummarizeArgument).ToList()));
        }
    }

    private static object SummarizeArgument(object? value) => value switch
    {
        null => new { type = "null" },
        string text when text.TrimStart().StartsWith('{') || text.TrimStart().StartsWith('[')
            => new { type = "json-payload", length = text.Length, sha256 = JsonSupport.Sha256Text(text) },
        string text => new { type = "string", value = text },
        byte[] bytes => new { type = "bytes", length = bytes.Length, sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)) },
        _ => new { type = value.GetType().FullName, value = value.ToString() },
    };

    private static List<string> VerifyStoreContainment(SaveManager manager, MockGodotFileIo expected)
    {
        var failures = new List<string>();
        foreach (var field in manager.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var value = field.GetValue(manager);
            if (value is ISaveStore store && !ReferenceEquals(store, expected)) failures.Add(field.Name);
            if (value is null || field.FieldType.Namespace?.Contains("Saves.Managers", StringComparison.Ordinal) != true) continue;
            foreach (var childField in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (childField.GetValue(value) is ISaveStore childStore && !ReferenceEquals(childStore, expected)) failures.Add(field.Name + "." + childField.Name);
        }
        return failures;
    }

    private static ReferenceCheck CheckReferences(SaveManager live, SaveManager candidate)
    {
        var liveNodes = CollectMutableNodes(new object[] { ReadProfile(live), live.SettingsSave, live.PrefsSave, live.Progress });
        var candidateNodes = CollectMutableNodes(new object[] { ReadProfile(candidate), candidate.SettingsSave, candidate.PrefsSave, candidate.Progress });
        var liveByReference = liveNodes.ToDictionary(x => x.Value, x => x.Path, ReferenceEqualityComparer.Instance);
        var shared = candidateNodes.Where(x => liveByReference.ContainsKey(x.Value))
            .Select(x => $"{x.Path} shares {liveByReference[x.Value]}").OrderBy(x => x, StringComparer.Ordinal).ToList();
        return new ReferenceCheck(
            !ReferenceEquals(live, candidate),
            !ReferenceEquals(live.SettingsSave, candidate.SettingsSave),
            !ReferenceEquals(live.PrefsSave, candidate.PrefsSave),
            !ReferenceEquals(live.Progress, candidate.Progress),
            liveNodes.Count,
            candidateNodes.Count,
            shared,
            candidateNodes.Where(x => x.Kind == "collection").Select(x => new ReferenceNode(x.Path, x.Value.GetType().FullName ?? x.Value.GetType().Name, RuntimeHelpers.GetHashCode(x.Value), x.Kind, CollectionCount(x.Value))).ToList());
    }

    private sealed record MutableNode(string Path, object Value, string Kind);

    private static List<MutableNode> CollectMutableNodes(IEnumerable<object> roots)
    {
        var result = new List<MutableNode>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var index = 0;
        foreach (var root in roots) Visit(root, "$root[" + index++ + "]", result, seen);
        return result;
    }

    private static void Visit(object? value, string path, List<MutableNode> result, HashSet<object> seen)
    {
        if (value is null || value is string || value.GetType().IsValueType || !seen.Add(value)) return;
        var type = value.GetType();
        var collection = value is IEnumerable;
        var allowed = type.Namespace?.StartsWith("MegaCrit.Sts2.Core.Saves", StringComparison.Ordinal) == true
                      || type.Namespace?.StartsWith("MegaCrit.Sts2.Core.Modding", StringComparison.Ordinal) == true;
        if (!collection && !allowed) return;
        result.Add(new MutableNode(path, value, collection ? "collection" : "save-object"));
        if (value is IDictionary dictionary)
        {
            var i = 0;
            foreach (DictionaryEntry entry in dictionary) Visit(entry.Value, $"{path}.values[{i++}]", result, seen);
            return;
        }
        if (collection)
        {
            var i = 0;
            foreach (var item in (IEnumerable)value) Visit(item, $"{path}[{i++}]", result, seen);
            return;
        }
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            Visit(field.GetValue(value), path + "." + field.Name, result, seen);
    }

    private static int? CollectionCount(object value)
    {
        if (value is ICollection collection) return collection.Count;
        var property = value.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(value) as int?;
    }

    private static ProfileSave ReadProfile(SaveManager manager)
    {
        var field = typeof(SaveManager).GetField("_profileSaveManager", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(typeof(SaveManager).FullName, "_profileSaveManager");
        var child = field.GetValue(manager) ?? throw new InvalidOperationException("ProfileSaveManager is null.");
        var property = child.GetType().GetProperty("Profile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? throw new MissingMemberException(child.GetType().FullName, "Profile");
        return (ProfileSave)(property.GetValue(child) ?? throw new InvalidOperationException("ProfileSave state is null."));
    }

    private static int GetLatestProgressSchema(SaveManager manager)
    {
        var field = typeof(SaveManager).GetField("_migrationManager", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(typeof(SaveManager).FullName, "_migrationManager");
        var migration = field.GetValue(manager) ?? throw new InvalidOperationException("MigrationManager is null.");
        var method = migration.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(x => x.Name == "GetLatestVersion" && x.IsGenericMethodDefinition && x.GetParameters().Length == 0);
        return (int)(method.MakeGenericMethod(typeof(SerializableProgress)).Invoke(migration, null)
                     ?? throw new InvalidOperationException("Latest progress schema was null."));
    }
}
