using System.Text.Json;
using SpikeInv012A.Core;

namespace SpikeInv012A;

// Only writes inside this probe's fresh attempt directory. Paths are never serialized.
internal sealed class EvidenceSink(string root, string attempt) : IEvidence
{
    private readonly object _gate = new();
    private readonly string _directory = Path.Combine(root, "SPIKEINV012A", attempt);
    private int _next, _diagnostic, _summaries;
    private bool _final;
    private static int _fallback;
    public bool TryWrite(Record record, bool final = false)
    {
        lock (_gate)
        {
            if (final) { if (_final || _next >= 16) return false; _final = true; }
            else if (record.Event == "writer-summary") { if (_summaries >= 2 || _next >= 16) return false; _summaries++; }
            else { if (_diagnostic >= 12) return true; if (_next >= 16) return false; _diagnostic++; }
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(record);
                if (bytes.Length > 4096) return Fail();
                Directory.CreateDirectory(_directory);
                var path = Path.Combine(_directory, $"{_next++:D2}.json");
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                stream.Write(bytes);
                stream.Flush(true);
                return true;
            }
            catch { return Fail(); }
        }
    }
    private static bool Fail()
    {
        if (Interlocked.Exchange(ref _fallback, 1) == 0)
            try { Console.Error.WriteLine("SPIKEINV012A evidence_failure"); } catch { }
        return false;
    }
}
