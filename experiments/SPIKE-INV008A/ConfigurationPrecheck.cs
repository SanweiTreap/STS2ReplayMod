using System.Text;
using System.Text.Json;

namespace SpikeInv008A;

internal sealed record ModSetPrecheckResult(bool Passed, int LoadedModCount, IReadOnlyList<string> BoundedIds, string Failure);

internal static class ConfigurationPrecheck
{
    internal const int MaxRecordedIds = 32;
    internal const int MaxRecordedIdLength = 128;

    internal static ModSetPrecheckResult Evaluate(IEnumerable<string> loadedIds)
    {
        var ids = loadedIds.Select(value => value ?? string.Empty).ToList();
        var bounded = ids.Select(value => value.Length <= MaxRecordedIdLength ? value : value[..MaxRecordedIdLength])
            .OrderBy(value => value, StringComparer.Ordinal).Take(MaxRecordedIds).ToList();
        var passed = ids.Count == 1 && ids[0] == "SPIKEINV008A";
        var failure = passed ? "None" : ids.Count > MaxRecordedIds ? "TooManyLoadedMods" : "WrongLoadedModSet";
        return new(passed, ids.Count, bounded, failure);
    }

    internal static string WriteFailure(string evidenceRoot, string attemptToken, ModSetPrecheckResult result)
    {
        if (result.Passed || !Guid.TryParseExact(attemptToken, "N", out _)) throw new InvalidOperationException();
        Directory.CreateDirectory(evidenceRoot);
        var destination = Path.Combine(evidenceRoot, "precheck-modset-r6-" + attemptToken + ".json");
        var temporary = destination + ".tmp";
        if (File.Exists(destination) || File.Exists(temporary)) throw new IOException("precheck artifact already exists");
        var artifact = new ModSetPrecheckArtifact(ProbeConstants.PrecheckSchemaId, ProbeConstants.ProbeVersion,
            attemptToken, result.LoadedModCount, result.BoundedIds, result.Failure);
        var bytes = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(artifact,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
        using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            output.Write(bytes);
            output.Flush(true);
        }
        File.Move(temporary, destination);
        return destination;
    }
}

internal sealed class PostLoaderModSetGate
{
    private int _state;
    internal bool Passed => Volatile.Read(ref _state) == 1;

    internal ModSetPrecheckResult? EvaluateAtPostLoader(Func<IEnumerable<string>> readLoadedIds)
    {
        if (Passed) return null;
        if (Interlocked.CompareExchange(ref _state, -1, 0) != 0)
            throw new InvalidOperationException("post-loader mod-set precheck already attempted");
        var result = ConfigurationPrecheck.Evaluate(readLoadedIds());
        Volatile.Write(ref _state, result.Passed ? 1 : 2);
        return result;
    }
}
