using System.Globalization;
using System.Text.Json;

namespace Perf001;

public static class Verification
{
    private static void Need(bool condition, string reason) { if (!condition) throw new InvalidDataException(reason); }
    public static object Run(string workspace, string output)
    {
        var full = Path.GetFullPath(output);
        var root = Path.GetFullPath(Path.Combine(workspace, "evidence/perf-001/runs"));
        PackagePaths.NoLinks(full);
        Need(Path.GetDirectoryName(full)!.Equals(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(full), "owned-run-directory-only");
        var resultPath = Path.Combine(full, "result.json"); PackagePaths.NoLinks(resultPath);
        using var result = JsonDocument.Parse(File.ReadAllBytes(resultPath));
        var r = result.RootElement;
        Need(r.GetProperty("Schema").GetInt32() == 1 && r.GetProperty("Revision").GetString() == "offline-r1" &&
            r.GetProperty("Result").GetString() == "COMPLETE_OFFLINE_PROXY_NOT_GAME_PERFORMANCE" &&
            r.GetProperty("ReleaseBuild").GetBoolean() && !r.GetProperty("DebuggerAttached").GetBoolean() &&
            r.GetProperty("InputsUnchangedAfter").GetBoolean() && r.GetProperty("TimingWindowUserConfirmed").GetBoolean(), "run-identity-or-completion");
        var frequency = r.GetProperty("Frequency").GetInt64(); Need(frequency > 0, "frequency");
        using var fixtures = new Fixtures(workspace);
        var measured = new References(fixtures, Pipeline.Count); var warmup = new References(fixtures, Pipeline.WarmupCount);
        Need(r.GetProperty("Inputs").GetRawText() == JsonSerializer.SerializeToElement(Fixtures.Approved).GetRawText() ||
            System.Text.Json.Nodes.JsonNode.DeepEquals(System.Text.Json.Nodes.JsonNode.Parse(r.GetProperty("Inputs").GetRawText()),
                System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(Fixtures.Approved))), "input-declarations");
        var blocks = r.GetProperty("Blocks").Deserialize<BlockResult[]>(DataJson.Options)!;
        Need(blocks.Length == 18, "expected-18-warmup-and-measured-blocks");
        var seen = new HashSet<(int, ProxyMode, bool)>(); int files = 0, rows = 0;
        int[][] orders = [[0, 1, 2], [1, 2, 0], [2, 0, 1]];
        for (var position = 0; position < blocks.Length; position++)
        {
            var b = blocks[position]; var expected = b.Warmup ? warmup : measured;
            Need(b.Repetition == position / 6 && (int)b.Mode == orders[position / 6][position % 6 / 2] &&
                b.Warmup == (position % 2 == 0) && seen.Add((b.Repetition, b.Mode, b.Warmup)), "block-order-or-duplicate");
            Need(b.Operations == expected.Records.Length && b.BytesObserved == expected.Bytes &&
                b.MainChecksum == expected.SHA256 && b.ExpectedChecksum == expected.SHA256, "producer-count-bytes-checksum");
            Need(b.TotalCompletionTicks >= b.ProducerLoopTicks && b.ProducerLoopTicks >= 0 &&
                b.MainLoopThreadAllocatedBytes >= 0 && b.ProcessAllocatedBytes >= 0 &&
                double.IsFinite(b.ProcessCpuMilliseconds) && b.ProcessCpuMilliseconds >= 0 &&
                b.GcCollections.Length == 3 && b.GcCollections.All(x => x >= 0), "invalid-timing-or-allocation-counters");
            if (b.Mode == ProxyMode.SerializeQueuedFile)
            {
                var name = $"{(b.Warmup ? "warmup" : "measured")}-queued-r{b.Repetition}.ndjson";
                Need(b.OutputFile == name && b.Writer is not null, "owned-queued-output");
                var w = b.Writer!;
                Need(w.Complete && w.Failure is null && w.Accepted == b.Operations && w.Written == b.Operations &&
                    w.EnqueuedBytes == expected.Bytes && w.WrittenBytes == expected.Bytes &&
                    w.WriteCalls == b.Operations && w.FlushCalls == 1 && w.Overflows == 0 && w.Rejections == 0 &&
                    w.CapacityRecords == 128 && w.CapacityBytes == 2 * 1024 * 1024 && w.MaximumRecordBytes == 16384 &&
                    w.PeakQueuedRecords <= w.CapacityRecords && w.PeakQueuedBytes <= w.CapacityBytes &&
                    w.ThreadStopped && w.CompletionRequested && w.WriterThreadAllocatedBytes >= 0 &&
                    w.WriteTicks >= 0 && w.FlushTicks >= 0 && w.WriterWallTicks >= 0, "writer-incomplete-or-counters");
                var path = Path.Combine(full, name); PackagePaths.NoLinks(path);
                var actual = IntegrityCheck.FileRecords(path, fixtures, expected);
                Need(actual == b.OutputIntegrity, "independent-output-integrity-differs"); files++;
            }
            else Need(b.Writer is null && b.OutputFile is null && b.OutputIntegrity is null, "non-file-mode-has-writer");
            if (!b.Warmup)
            {
                var name = $"samples-r{b.Repetition}-{b.Mode}.csv";
                Need(b.RawSamplesFile == name, "owned-raw-file");
                var path = Path.Combine(full, name); PackagePaths.NoLinks(path);
                var lines = File.ReadAllLines(path);
                Need(lines.Length == Pipeline.Count + 1 && lines[0] == "sequence,input_index,operation_stopwatch_ticks,encoding_freeze_ticks,enqueue_ticks,bytes", "raw-header-or-count");
                var operations = new List<double>(); var encoding = new List<double>(); var enqueue = new List<double>();
                long operationTicks = 0;
                for (var i = 1; i < lines.Length; i++)
                {
                    var cells = lines[i].Split(','); Need(cells.Length == 6, "raw-columns");
                    long Number(int c) => long.Parse(cells[c], CultureInfo.InvariantCulture);
                    Need(Number(0) == i && Number(1) == (i - 1) % 5 && Number(5) == expected.Records[i - 1].Length, "raw-identity");
                    var ticks = Number(2); operationTicks = checked(operationTicks + ticks);
                    operations.Add(Statistics.TicksToMilliseconds(ticks, frequency));
                    Need((cells[3].Length > 0) == (b.Mode != ProxyMode.Noop) &&
                        (cells[4].Length > 0) == (b.Mode == ProxyMode.SerializeQueuedFile), "raw-stage-presence");
                    if (cells[3].Length > 0) encoding.Add(Statistics.TicksToMilliseconds(Number(3), frequency));
                    if (cells[4].Length > 0) enqueue.Add(Statistics.TicksToMilliseconds(Number(4), frequency));
                    rows++;
                }
                Need(operationTicks <= b.ProducerLoopTicks, "raw-operation-sum-exceeds-loop");
                Need(JsonSerializer.Serialize(Statistics.Describe(operations)) == JsonSerializer.Serialize(b.ProducerOperationWall) &&
                    JsonSerializer.Serialize(Statistics.Describe(encoding)) == JsonSerializer.Serialize(b.EncodingFreezeWall) &&
                    JsonSerializer.Serialize(Statistics.Describe(enqueue)) == JsonSerializer.Serialize(b.EnqueueWall), "raw-statistics-differ");
            }
            else Need(b.RawSamplesFile is null, "warmup-raw-schema");
        }
        fixtures.VerifyUnchanged();
        return new {Result="PASS_READ_ONLY_PROXY_RECHECK", Blocks=blocks.Length, ParsedQueuedFiles=files,
            RecomputedMeasuredRows=rows, InputsUnchanged=true, Writes=0, TimedBenchmarkRuns=0, GameRuntimeAttempts=0};
    }
}
