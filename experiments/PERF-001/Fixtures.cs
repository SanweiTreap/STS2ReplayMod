using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Perf001;

public static class DataJson
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = false, MaxDepth = 64 };
    public static string Hash(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    public static void CreateJson(string path, object value)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, value, new JsonSerializerOptions { WriteIndented = true, MaxDepth = 64 });
        stream.Flush();
    }
}

public sealed record InputIdentity(string Path, int Bytes, string SHA256);
public sealed class Fixtures : IDisposable
{
    public const string Prefix = "evidence/det-001/runtime/records-af99c3b9c7bf411882d580c2f3565408/";
    public static readonly InputIdentity[] Approved =
    [
        new(Prefix+"A.point-0.semantic.json",7545,"78C840628A1AC54F7256B1EF303895658F0B938B17F330AF8AA666CAFE9BDAB1"),
        new(Prefix+"A.point-1.semantic.json",7544,"9BEF32909EB1C7154B183003BD49FAE191902A6C924703070A3A1CE51BA6032E"),
        new(Prefix+"A.point-2.semantic.json",7584,"36217D89C02E60726CC7864F11DE8247DE31DDD2409494E9C062384ADC3CBDA6"),
        new(Prefix+"A.point-3.semantic.json",7584,"4B203C5229C395214E228931416686E7A749E51C0F14536C875BB10A5A3E8668"),
        new(Prefix+"A.point-4.semantic.json",7583,"6EF4ECD4217E6C5CE72E4F5482495BFD369979F63E7E434DC8F169804BCE1276")
    ];
    private readonly string _workspace;
    private readonly List<JsonDocument> _documents = [];
    private readonly byte[][] _oracleBytes;
    public Fixtures(string workspace)
    {
        _workspace = workspace;
        _oracleBytes = Approved.Select(e =>
        {
            var path = Path.Combine(workspace, e.Path); PackagePaths.NoLinks(path);
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length != e.Bytes || DataJson.Hash(bytes) != e.SHA256) throw new InvalidDataException("input-identity");
            _documents.Add(JsonDocument.Parse(bytes));
            return bytes.ToArray(); // expected raw input never aliases mutable candidate buffers
        }).ToArray();
    }
    public JsonElement Input(int index) => _documents[index].RootElement;
    public JsonNode Oracle(int index) => JsonNode.Parse(_oracleBytes[index])!; // detached, fresh expected graph
    public void VerifyUnchanged()
    {
        foreach (var e in Approved)
        {
            var b = File.ReadAllBytes(Path.Combine(_workspace, e.Path));
            if (b.Length != e.Bytes || DataJson.Hash(b) != e.SHA256) throw new InvalidDataException("input-changed");
        }
    }
    public void Dispose() { foreach (var doc in _documents) doc.Dispose(); }
}

public sealed record Envelope(int Sequence, int InputIndex, JsonElement Payload);
public sealed class FrozenRecord
{
    private readonly byte[] _bytes;
    public int Sequence { get; }
    public int InputIndex { get; }
    public ReadOnlyMemory<byte> Bytes => _bytes;
    public int Length => _bytes.Length;
    public FrozenRecord(int sequence, int inputIndex, ReadOnlySpan<byte> serialized)
    {
        Sequence = sequence; InputIndex = inputIndex;
        _bytes = new byte[serialized.Length + 1];
        serialized.CopyTo(_bytes); _bytes[^1] = (byte)'\n';
    }
}

public sealed class References
{
    public FrozenRecord[] Records { get; }
    public string SHA256 { get; }
    public long Bytes { get; }
    public References(Fixtures fixtures, int count)
    {
        Records = new FrozenRecord[count];
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        for (var i = 0; i < count; i++)
        {
            // Independent envelope construction, outside all timed windows.
            using var memory = new MemoryStream();
            using (var w = new Utf8JsonWriter(memory))
            {
                w.WriteStartObject(); w.WriteNumber("Sequence", i + 1); w.WriteNumber("InputIndex", i % 5);
                w.WritePropertyName("Payload"); fixtures.Oracle(i % 5).WriteTo(w); w.WriteEndObject(); w.Flush();
            }
            var r = new FrozenRecord(i + 1, i % 5, memory.ToArray()); Records[i] = r;
            digest.AppendData(r.Bytes.Span); Bytes += r.Length;
        }
        SHA256 = Convert.ToHexString(digest.GetHashAndReset());
    }
}

public static class PackagePaths
{
    public static void NoLinks(string path)
    {
        for (var p = Path.GetFullPath(path); !string.IsNullOrEmpty(p); p = Path.GetDirectoryName(p))
            if (File.Exists(p) || Directory.Exists(p))
                if ((File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("reparse-path");
    }
    public static string NewOutput(string workspace, string output, string kind)
    {
        var root = Path.GetFullPath(Path.Combine(workspace, "evidence/perf-001", kind));
        var full = Path.GetFullPath(output); NoLinks(full);
        if (!string.Equals(Path.GetDirectoryName(full), root, StringComparison.OrdinalIgnoreCase) ||
            !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(full), "^[a-z0-9-]{1,64}$") ||
            Directory.Exists(full) || File.Exists(full)) throw new InvalidDataException("new-package-output-only");
        Directory.CreateDirectory(full); return full;
    }
}
