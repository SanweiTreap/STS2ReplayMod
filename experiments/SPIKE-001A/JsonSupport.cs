using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spike001A;

internal static class JsonSupport
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    internal static string Sha256(string value) => Sha256(Encoding.UTF8.GetBytes(value));

    internal static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    internal static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    internal static void WriteAtomic<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, Options), new UTF8Encoding(false));
        File.Move(temporaryPath, path, true);
    }

    internal static T Read<T>(string path) where T : class
        => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
           ?? throw new InvalidDataException($"JSON deserialized to null: {path}");

    internal static IReadOnlyList<JsonDiffEntry> Diff<T>(T expected, T actual)
    {
        var left = JsonSerializer.SerializeToNode(expected, Options);
        var right = JsonSerializer.SerializeToNode(actual, Options);
        var output = new List<JsonDiffEntry>();
        DiffNode("$", left, right, output);
        return output;
    }

    private static void DiffNode(string path, JsonNode? expected, JsonNode? actual, List<JsonDiffEntry> output)
    {
        if (expected is JsonObject expectedObject && actual is JsonObject actualObject)
        {
            foreach (var key in expectedObject.Select(x => x.Key).Union(actualObject.Select(x => x.Key)).OrderBy(x => x, StringComparer.Ordinal))
                DiffNode($"{path}.{key}", expectedObject[key], actualObject[key], output);
            return;
        }

        if (expected is JsonArray expectedArray && actual is JsonArray actualArray)
        {
            var count = Math.Max(expectedArray.Count, actualArray.Count);
            for (var i = 0; i < count; i++)
                DiffNode($"{path}[{i}]", i < expectedArray.Count ? expectedArray[i] : null, i < actualArray.Count ? actualArray[i] : null, output);
            return;
        }

        if (!JsonNode.DeepEquals(expected, actual))
            output.Add(new JsonDiffEntry(path, expected?.DeepClone(), actual?.DeepClone()));
    }
}
