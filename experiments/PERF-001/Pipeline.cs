using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Perf001;

public static class Codec
{
    public static FrozenRecord Encode(Fixtures fixtures, int sequence)
    {
        var index = (sequence - 1) % 5;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new Envelope(sequence, index, fixtures.Input(index)), DataJson.Options);
        return new(sequence, index, bytes); // UTF8 serialization + owned frozen copy + newline
    }
}

public sealed record Integrity(bool Complete, int ParsedRecords, long Bytes, string SHA256,
    string ExpectedSHA256, bool EveryPayloadAndSequenceVerified);
public static class IntegrityCheck
{
    public static Integrity FileRecords(string path, Fixtures fixtures, References expected)
    {
        int count = 0;
        using (var reader = new StreamReader(path))
        {
            while (reader.ReadLine() is { } line)
            {
                if (line.Length > 16384 || ++count > expected.Records.Length) throw new InvalidDataException("output-line-bound");
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement; var index = (count - 1) % 5;
                if (root.GetProperty("Sequence").GetInt32() != count || root.GetProperty("InputIndex").GetInt32() != index ||
                    !JsonNode.DeepEquals(fixtures.Oracle(index), JsonNode.Parse(root.GetProperty("Payload").GetRawText())))
                    throw new InvalidDataException("output-order-or-payload");
            }
        }
        using var file = System.IO.File.OpenRead(path); var hash = Convert.ToHexString(SHA256.HashData(file));
        var length = file.Length;
        if (count != expected.Records.Length || length != expected.Bytes || hash != expected.SHA256)
            throw new InvalidDataException("output-count-bytes-or-checksum");
        return new(true, count, length, hash, expected.SHA256, true);
    }
}

public enum ProxyMode { Noop, SerializeMemory, SerializeQueuedFile }
public readonly record struct OperationSample(int Sequence, int InputIndex, long OperationTicks,
    long? EncodingFreezeTicks, long? EnqueueTicks, int Bytes);
public sealed record BlockResult(int Repetition, ProxyMode Mode, bool Warmup, int Operations,
    long BytesObserved, string MainChecksum, string ExpectedChecksum, long ProducerLoopTicks,
    long TotalCompletionTicks, long MainLoopThreadAllocatedBytes, long ProcessAllocatedBytes,
    double ProcessCpuMilliseconds, int[] GcCollections, Summary ProducerOperationWall,
    Summary EncodingFreezeWall, Summary EnqueueWall, WriterOutcome? Writer, Integrity? OutputIntegrity,
    string? OutputFile, string? RawSamplesFile);

public static class Pipeline
{
    public const int Count = 1000, WarmupCount = 100, Repetitions = 3;
    public const long OutputCap = 64L * 1024 * 1024;
    private static long _blackhole;
    public static bool ReleaseBuild
    {
        get
        {
#if DEBUG
            return false;
#else
            return true;
#endif
        }
    }

    public static BlockResult RunBlock(Fixtures fixtures, References references, string output, int repetition,
        ProxyMode mode, bool warmup, OutputBudget budget, Stopwatch commandClock)
    {
        var count = references.Records.Length;
        var samples = new OperationSample[count]; // same preallocated metrics storage in every mode
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var process = Process.GetCurrentProcess();
        var collectionBefore = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
        var cpuBefore = process.TotalProcessorTime;
        var allocatedBefore = GC.GetTotalAllocatedBytes(true);
        var totalStart = Stopwatch.GetTimestamp();
        BoundedWriter? writer = null;
        var fileName = mode == ProxyMode.SerializeQueuedFile
            ? $"{(warmup ? "warmup" : "measured")}-queued-r{repetition}.ndjson" : null;
        if (fileName is not null) writer = new(Path.Combine(output, fileName), count, budget);
        var producerAllocation = GC.GetAllocatedBytesForCurrentThread();
        var producerStart = Stopwatch.GetTimestamp();
        long bytes = 0;
        for (int i = 0; i < count; i++)
        {
            if (commandClock.Elapsed > TimeSpan.FromSeconds(45)) throw new TimeoutException("command-45s-bound");
            var start = Stopwatch.GetTimestamp();
            long? encodingTicks = null, enqueueTicks = null;
            FrozenRecord record;
            if (mode == ProxyMode.Noop) record = references.Records[i];
            else
            {
                var before = Stopwatch.GetTimestamp(); record = Codec.Encode(fixtures, i + 1);
                encodingTicks = Stopwatch.GetTimestamp() - before;
            }
            // Common measurement-shell work in ALL modes, NOT recorder work:
            // strong incremental checksum over identical expected-length bytes.
            digest.AppendData(record.Bytes.Span); bytes += record.Length;
            Volatile.Write(ref _blackhole, unchecked(_blackhole + record.Length + record.Sequence));
            if (writer is not null)
            {
                var before = Stopwatch.GetTimestamp(); var accepted = writer.TryEnqueue(record);
                enqueueTicks = Stopwatch.GetTimestamp() - before;
                if (!accepted)
                {
                    writer.CompleteAdding();
                    var failed = writer.Finish(TimeSpan.FromSeconds(3));
                    DataJson.CreateJson(Path.Combine(output, $"invalid-{repetition}-{mode}-{warmup}.json"), failed);
                    throw new InvalidDataException("queue-refused-record: no performance-complete result");
                }
            }
            samples[i] = new(i + 1, i % 5, Stopwatch.GetTimestamp() - start, encodingTicks, enqueueTicks, record.Length);
        }
        var producerTicks = Stopwatch.GetTimestamp() - producerStart;
        var producerAllocated = GC.GetAllocatedBytesForCurrentThread() - producerAllocation;
        var checksum = Convert.ToHexString(digest.GetHashAndReset());
        WriterOutcome? outcome = null;
        if (writer is not null) { writer.CompleteAdding(); outcome = writer.Finish(TimeSpan.FromSeconds(3)); }
        var totalTicks = Stopwatch.GetTimestamp() - totalStart;
        var processAllocation = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        var processCpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
        var collections = Enumerable.Range(0, 3).Select(i => GC.CollectionCount(i) - collectionBefore[i]).ToArray();
        // All structural parsing, file hashing and statistics are AFTER total completion.
        if (checksum != references.SHA256 || bytes != references.Bytes || outcome is { Complete: false })
            throw new InvalidDataException("incomplete-or-checksum-mismatch");
        var integrity = fileName is null ? null : IntegrityCheck.FileRecords(Path.Combine(output, fileName), fixtures, references);
        string? csv = null;
        if (!warmup)
        {
            csv = $"samples-r{repetition}-{mode}.csv";
            using var stream = new FileStream(Path.Combine(output, csv), FileMode.CreateNew);
            using var text = new StreamWriter(stream);
            text.WriteLine("sequence,input_index,operation_stopwatch_ticks,encoding_freeze_ticks,enqueue_ticks,bytes");
            foreach (var row in samples)
                text.WriteLine(FormattableString.Invariant($"{row.Sequence},{row.InputIndex},{row.OperationTicks},{row.EncodingFreezeTicks},{row.EnqueueTicks},{row.Bytes}"));
        }
        var frequency = Stopwatch.Frequency;
        return new(repetition, mode, warmup, count, bytes, checksum, references.SHA256, producerTicks, totalTicks,
            producerAllocated, processAllocation, processCpu, collections,
            Statistics.Describe(samples.Select(x => Statistics.TicksToMilliseconds(x.OperationTicks, frequency))),
            Statistics.Describe(samples.Where(x => x.EncodingFreezeTicks.HasValue).Select(x => Statistics.TicksToMilliseconds(x.EncodingFreezeTicks!.Value, frequency))),
            Statistics.Describe(samples.Where(x => x.EnqueueTicks.HasValue).Select(x => Statistics.TicksToMilliseconds(x.EnqueueTicks!.Value, frequency))),
            outcome, integrity, fileName, csv);
    }

    public static object Run(string workspace, string output)
    {
        if (!ReleaseBuild || Debugger.IsAttached) throw new InvalidOperationException("Release/no-debugger-required");
        var existing = Directory.Exists(Path.Combine(workspace, "evidence/perf-001"))
            ? Directory.EnumerateFiles(Path.Combine(workspace, "evidence/perf-001"), "*", SearchOption.AllDirectories).Sum(p => new FileInfo(p).Length) : 0;
        using var fixtures = new Fixtures(workspace);
        var expected = new References(fixtures, Count); var warmup = new References(fixtures, WarmupCount);
        var plannedBytes = Repetitions * (expected.Bytes + warmup.Bytes);
        if (existing + plannedBytes + 4 * 1024 * 1024 > OutputCap) throw new InvalidOperationException("retained-output-64MiB-bound");
        var budget = new OutputBudget(OutputCap - existing - 4 * 1024 * 1024);
        var commandClock = Stopwatch.StartNew(); var blocks = new List<BlockResult>();
        var orders = new[]
        {
            new[] { ProxyMode.Noop, ProxyMode.SerializeMemory, ProxyMode.SerializeQueuedFile },
            new[] { ProxyMode.SerializeMemory, ProxyMode.SerializeQueuedFile, ProxyMode.Noop },
            new[] { ProxyMode.SerializeQueuedFile, ProxyMode.Noop, ProxyMode.SerializeMemory }
        };
        for (var repetition = 0; repetition < Repetitions; repetition++)
            foreach (var mode in orders[repetition])
            {
                blocks.Add(RunBlock(fixtures, warmup, output, repetition, mode, true, budget, commandClock));
                blocks.Add(RunBlock(fixtures, expected, output, repetition, mode, false, budget, commandClock));
            }
        fixtures.VerifyUnchanged();
        return new
        {
            Schema = 1, Revision = "offline-r1", Result = "COMPLETE_OFFLINE_PROXY_NOT_GAME_PERFORMANCE",
            ReleaseBuild, DebuggerAttached = Debugger.IsAttached, Framework = RuntimeInformation.FrameworkDescription,
            RuntimeVersion = Environment.Version.ToString(), Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            LogicalProcessorCount = Environment.ProcessorCount, Stopwatch.Frequency, Stopwatch.IsHighResolution,
            ServerGC = GCSettings.IsServerGC, GCLatencyMode = GCSettings.LatencyMode.ToString(),
            GcPauseTime = "Unavailable: no GC pause-event collector", PerThreadCpu = "Unavailable: not sampled",
            GPUAndPresentationTime = "Unavailable: no game/GPU/presentation observer",
            Inputs = Fixtures.Approved, InputsUnchangedAfter = true, MeasuredOperationsPerModePerBlock = Count,
            WarmupOperationsPerModePerBlock = WarmupCount, Repetitions, Orders = orders.Select(x => x.Select(m => m.ToString()).ToArray()),
            TimingWindowUserConfirmed = true, Isolation = "User finished gaming; OS/background contention not programmatically excluded",
            CommonShell = "per-operation timestamps, preallocated sample rows, incremental SHA256, volatile consumption; Noop consumes independently prebuilt reference envelopes",
            OutsideTimedWindow = "source read/hash/parse; independent reference encoding; result aggregation/statistics/CSV; post-drain file hash+all-line structural verification",
            EncodingStage = "JsonSerializer SerializeToUtf8Bytes of Envelope<JsonElement> + owned frozen copy + newline; live capture absent",
            NativeGameAPIsOrProbeDLLUsed = false, ThreadSemantics = "Producer synchronous same thread; dedicated synchronous writer; process allocation covers both plus runtime; wall times overlap, do not sum",
            FlushSemantics = "one explicit FileStream.Flush per queued block; buffered API Write calls counted, OS syscall count/durable persistence unavailable; Dispose implicit work not separately counted",
            QueueBound = "128 queued records AND 2MiB queued bytes, record<=16KiB; plus <=1 writer in-flight and <=1 producer record; overflow invalidates attempt, no silent drop or successful partial drain",
            OutputBudgetBytes = OutputCap, PlannedNdjsonBytes = plannedBytes, ReservedWrittenBytes = budget.Used,
            CommandMeasuredWorkMilliseconds = commandClock.Elapsed.TotalMilliseconds, Blackhole = Volatile.Read(ref _blackhole), Blocks = blocks
        };
    }
}
