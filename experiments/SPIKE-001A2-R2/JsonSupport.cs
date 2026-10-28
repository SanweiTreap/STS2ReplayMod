using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Spike001A2R2;

internal static class JsonSupport
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    internal static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options)
        ?? throw new InvalidDataException("JSON deserialized to null: " + path);
    internal static string Sha256File(string path) { using var s = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(s)); }
    internal static string Sha256Text(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    internal static string Canonicalize(string json)
    {
        using var document = JsonDocument.Parse(json); using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteCanonical(writer, document.RootElement);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var p in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                { writer.WritePropertyName(p.Name); WriteCanonical(writer, p.Value); }
                writer.WriteEndObject(); break;
            case JsonValueKind.Array:
                writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) WriteCanonical(writer, item); writer.WriteEndArray(); break;
            default: value.WriteTo(writer); break;
        }
    }
    internal static void WriteCreateNew<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); var temporary = path + ".tmp";
        if (File.Exists(path) || File.Exists(temporary)) throw new IOException("artifact already exists: " + path);
        var bytes = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(value, Options));
        using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { output.Write(bytes); output.Flush(true); }
        File.Move(temporary, path);
    }
    internal static IReadOnlyList<JsonDiff> Diff<T>(T before, T after)
        => DiffNodes(JsonSerializer.SerializeToNode(before, Options), JsonSerializer.SerializeToNode(after, Options), "$", []);
    internal static IReadOnlyList<JsonDiff> DiffJson(string before, string after)
        => DiffNodes(JsonNode.Parse(before), JsonNode.Parse(after), "$", []);
    private static List<JsonDiff> DiffNodes(JsonNode? left, JsonNode? right, string path, List<JsonDiff> output)
    {
        if (JsonNode.DeepEquals(left, right)) return output;
        if (left is JsonObject lo && right is JsonObject ro)
        {
            foreach (var key in lo.Select(x => x.Key).Concat(ro.Select(x => x.Key)).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
                DiffNodes(lo[key], ro[key], path + "." + key, output);
        }
        else if (left is JsonArray la && right is JsonArray ra)
        {
            for (var i = 0; i < Math.Max(la.Count, ra.Count); i++)
                DiffNodes(i < la.Count ? la[i] : null, i < ra.Count ? ra[i] : null, path + "[" + i + "]", output);
        }
        else output.Add(new(path, left?.DeepClone(), right?.DeepClone()));
        return output;
    }
}
