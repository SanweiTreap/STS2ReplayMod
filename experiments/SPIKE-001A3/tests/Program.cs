using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Spike001A3;

var tests = new (string Name, Func<Task> Run)[]
{
    ("fixed INV-010 predecessor schema positive preflight", PredecessorSchema),
    ("schema and create-new artifact collision", SchemaAndCreateNew),
    ("create-new recovery collision", RecoveryCollision),
    ("exact mod set", () => Sync(ExactModSet)),
    ("profile1 only", () => Sync(ProfileOneOnly)),
    ("Cloud conjunction off", () => Sync(CloudOff)),
    ("checkpoint observer predicate", () => Sync(ObserverPredicate)),
    ("asymmetric map target keeps col/row order", () => Sync(MapTargetCoordinateOrder)),
    ("checkpoint target positive", () => Sync(CheckpointPositive)),
    ("completed task with missing checkpoint is rejected", () => Sync(CheckpointMissing)),
    ("completed task with unparseable checkpoint is rejected", () => Sync(CheckpointUnparseable)),
    ("production task fault propagates", ProductionFault),
    ("production task cancellation propagates", ProductionCancel),
    ("RoomEntered-before-baseline race", () => Sync(RoomEnteredRace)),
    ("first terminal wins under concurrency", () => Sync(FirstTerminalWins)),
    ("manual Ready over 120 seconds has no deadline", ManualReadyNoDeadline),
    ("independent deadline fires with zero ticker ticks", DeadlineZeroTicks),
    ("independent deadline fires after ticker stops", DeadlineTickerStops),
    ("completed capture cancels deadline", DeadlineCancelled),
    ("all required fields accept value true", () => Sync(AllReadinessTrue)),
    ("each required field null resets quiet", () => Sync(EveryRequiredNull)),
    ("each required read exception resets quiet", () => Sync(EveryRequiredException)),
    ("non-consecutive engine frame resets quiet", () => Sync(NonConsecutive)),
    ("target identity change resets quiet", () => Sync(IdentityChange)),
    ("bounded telemetry compacts events", () => Sync(BoundedTelemetry)),
    ("exception aggregation is bounded and counted", () => Sync(ExceptionAggregation)),
    ("B NumReloads exactly plus one", () => Sync(ReloadOnly)),
    ("B non-whitelist canonical diff rejected", () => Sync(NonWhitelistSaveDiff)),
    ("protection inventory change rejected", () => Sync(ProtectionInventory)),
    ("snapshot semantic diff detected", () => Sync(SnapshotDiff)),
    ("declared Harmony targets and persistent ticker contract", () => Sync(HarmonyTargets)),
};

var passed = 0;
foreach (var test in tests)
{
    try { await test.Run(); Console.WriteLine("PASS " + test.Name); passed++; }
    catch (Exception ex) { Console.Error.WriteLine("FAIL " + test.Name + ": " + ex); }
}
Console.WriteLine($"RESULT {passed}/{tests.Length}");
return passed == tests.Length ? 0 : 1;

static Task Sync(Action action) { action(); return Task.CompletedTask; }

static Task PredecessorSchema()
{
    const string path = @"C:\Projects\STS2ReplayMod\evidence\inv-010\reconstruction-contract.json";
    Eq(ProbeConstants.Inv010ContractSha256, Hash(path));
    using var doc = JsonDocument.Parse(File.ReadAllText(path)); var root = doc.RootElement;
    Eq(ProbeConstants.Id, root.GetProperty("runtimeId").GetString()!);
    Eq(ProbeConstants.PredecessorSchema, root.GetProperty("schemaId").GetString()!);
    if (ProbeConstants.Schema == ProbeConstants.PredecessorSchema) throw new InvalidOperationException("repair schema was not revised");
    Eq(ProbeConstants.QuietFrames, root.GetProperty("quietFramesRequired").GetInt32());
    Eq(ProbeConstants.CaptureDeadlineSeconds, root.GetProperty("captureDeadlineSeconds").GetInt32());
    if (root.GetProperty("armHasDeadline").GetBoolean()) throw new InvalidOperationException("arm must not time out");
    Eq(18, root.GetProperty("requiredReadinessFields").GetArrayLength());
    return Task.CompletedTask;
}

static Task SchemaAndCreateNew()
{
    var root = Temp(); try
    {
        var path = Path.Combine(root, "terminal.json"); var value = Terminal("X");
        JsonSupport.WriteCreateNew(path, value); Eq(ProbeConstants.Schema, JsonSupport.Read<TerminalArtifact>(path).SchemaId);
        Throws(() => JsonSupport.WriteCreateNew(path, value));
    } finally { Directory.Delete(root, true); }
    return Task.CompletedTask;
}

static Task RecoveryCollision()
{
    var root = Temp(); try
    {
        var path = Path.Combine(root, "recovery", "checkpoint.save");
        JsonSupport.WriteBytesCreateNew(path, [1,2,3]); Throws(() => JsonSupport.WriteBytesCreateNew(path, [4]));
    } finally { Directory.Delete(root, true); }
    return Task.CompletedTask;
}

static void ExactModSet()
{
    if (!ExperimentRules.ExactModSet([ProbeConstants.Id]) || ExperimentRules.ExactModSet([])
        || ExperimentRules.ExactModSet([ProbeConstants.Id, "X"])) throw new InvalidOperationException();
}
static void ProfileOneOnly()
{
    if (!ExperimentRules.IsProfileOne(1, "modded/profile1", true)
        || ExperimentRules.IsProfileOne(2, "modded/profile2", true)
        || ExperimentRules.IsProfileOne(1, "profile1", false)) throw new InvalidOperationException();
}
static void CloudOff()
{
    if (ExperimentRules.IsCloudOff(true, true) || !ExperimentRules.IsCloudOff(false, true)
        || !ExperimentRules.IsCloudOff(true, false)) throw new InvalidOperationException();
}
static void ObserverPredicate()
{
    if (!ExperimentRules.ShouldWrapCheckpoint(true, true, null, true)
        || ExperimentRules.ShouldWrapCheckpoint(false, true, null, true)
        || ExperimentRules.ShouldWrapCheckpoint(true, false, null, true)
        || ExperimentRules.ShouldWrapCheckpoint(true, true, new object(), true)
        || ExperimentRules.ShouldWrapCheckpoint(true, true, null, false)) throw new InvalidOperationException();
}
static void CheckpointPositive() => CheckpointReader.ValidateTarget(Checkpoint("ROOM", true), Target(), FileId("ARM", 8));
static void CheckpointMissing()
{
    var missing = new FileIdentity("P", false, null, null, null);
    var value = Checkpoint("ROOM", true) with { FirstRead = missing, SecondRead = missing };
    Throws(() => CheckpointReader.ValidateTarget(value, Target(), FileId("ARM", 8)));
}
static void CheckpointUnparseable()
{
    Throws(() => JsonSupport.Canonicalize("{not-json"));
}
static async Task ProductionFault()
{
    await ThrowsAsync(() => CheckpointObservation.AwaitProductionAsync(Task.FromException(new IOException("write failed"))));
}
static async Task ProductionCancel()
{
    var source = new CancellationTokenSource(); source.Cancel();
    await ThrowsAsync(() => CheckpointObservation.AwaitProductionAsync(Task.FromCanceled(source.Token)));
}
static void RoomEnteredRace()
{
    if (!CheckpointObservation.RoomEnteredBeforeBaseline(true, false)
        || CheckpointObservation.RoomEnteredBeforeBaseline(false, false)
        || CheckpointObservation.RoomEnteredBeforeBaseline(true, true)) throw new InvalidOperationException();
}
static void FirstTerminalWins()
{
    var latch = new TerminalLatch(); var wins = 0;
    Parallel.For(0, 100, _ => { if (latch.TrySeal()) Interlocked.Increment(ref wins); }); Eq(1, wins);
}
static async Task ManualReadyNoDeadline()
{
    var fired = 0; var deadline = new CaptureDeadline(TimeSpan.FromSeconds(30), _ => fired++, (_, _) => Task.CompletedTask);
    await Task.Yield(); if (deadline.IsStarted || fired != 0) throw new InvalidOperationException("unarmed Ready timed out");
}
static Task DeadlineZeroTicks()
{
    var fired = 0; var deadline = new CaptureDeadline(TimeSpan.FromSeconds(30), _ => fired++, (_, _) => Task.CompletedTask);
    deadline.Start("zero-tick"); Eq(1, fired); return Task.CompletedTask;
}
static Task DeadlineTickerStops()
{
    var fired = 0; var deadline = new CaptureDeadline(TimeSpan.FromSeconds(30), _ => fired++, (_, _) => Task.CompletedTask);
    deadline.Start("ticker-stopped-after-7"); Eq(1, fired); return Task.CompletedTask;
}
static async Task DeadlineCancelled()
{
    var fired = 0; var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var deadline = new CaptureDeadline(TimeSpan.FromSeconds(30), _ => fired++, (_, token) => gate.Task.WaitAsync(token));
    deadline.Start("capture"); deadline.Complete(); await Task.Yield(); await Task.Yield(); Eq(0, fired);
}
static void AllReadinessTrue()
{
    var result = ReadinessContract.Evaluate(ReadyFields(), true, true); if (!result.Ready || result.Breaker is not null) throw new InvalidOperationException();
}
static void EveryRequiredNull()
{
    foreach (var name in ReadinessContract.RequiredFields)
    {
        var fields = ReadyFields(); Replace(fields, name, new(name, "null", null, "site." + name, null, null));
        if (ReadinessContract.Evaluate(fields, true, true).Ready) throw new InvalidOperationException(name);
    }
}
static void EveryRequiredException()
{
    foreach (var name in ReadinessContract.RequiredFields)
    {
        var fields = ReadyFields(); Replace(fields, name, new(name, "exception", null, "site." + name, "System.InvalidOperationException", "boom"));
        if (ReadinessContract.Evaluate(fields, true, true).Ready) throw new InvalidOperationException(name);
    }
}
static void NonConsecutive()
{
    var result = ReadinessContract.Evaluate(ReadyFields(), false, true); Eq("non-consecutive-process-frame", result.Breaker!);
}
static void IdentityChange()
{
    var result = ReadinessContract.Evaluate(ReadyFields(), true, false); Eq("target-or-object-identity-change", result.Breaker!);
}
static void MapTargetCoordinateOrder()
{
    var coord = ProbeController.FindCoord(5, 8, (col, row) => col == 6 && row == 2);
    Eq(6, coord.col); Eq(2, coord.row);
}
static void BoundedTelemetry()
{
    var telemetry = new TelemetryCollector();
    for (var i=1;i<=300;i++) telemetry.RecordSample(Sample(i, ReadyFields().Append(new("changing", "value", i.ToString(), "changing", null, null)).ToList()), false);
    var snapshot = telemetry.Snapshot(); Eq(ProbeConstants.TelemetryEventLimit, snapshot.Events.Count);
    if (snapshot.DroppedEvents <= 0 || snapshot.LastRawSample?.Tick != 300) throw new InvalidOperationException();
}
static void ExceptionAggregation()
{
    var telemetry = new TelemetryCollector();
    for (var i=1;i<=400;i++)
    {
        var fields = ReadyFields(); Replace(fields, "statePresent", new("statePresent", "exception", null, "run.state", "E", "M"));
        telemetry.RecordSample(Sample(i, fields), true);
    }
    var snapshot = telemetry.Snapshot(); Eq(1, snapshot.Exceptions.Count); Eq(400, snapshot.Exceptions[0].Count);
    if (snapshot.Events.Count > ProbeConstants.TelemetryEventLimit || snapshot.DroppedEvents == 0) throw new InvalidOperationException();
}
static void ReloadOnly()
{
    var x = ExperimentRules.CompareContinueSave("{\"num_reloads\":2,\"seed\":\"S\"}", "{\"num_reloads\":3,\"seed\":\"S\"}", 2, 3);
    if (!x.Passed || x.AllDiffs.Count != 1) throw new InvalidOperationException(x.Failure);
}
static void NonWhitelistSaveDiff()
{
    var x = ExperimentRules.CompareContinueSave("{\"num_reloads\":2,\"seed\":\"S\"}", "{\"num_reloads\":3,\"seed\":\"X\"}", 2, 3);
    if (x.Passed || x.NonAllowlistedDiffs.Count != 1) throw new InvalidOperationException();
}
static void ProtectionInventory()
{
    var a = Protection("A", ["saves/current_run.save"]); var b = Protection("A", ["saves/current_run.save", "new.file"]);
    if (ExperimentRules.CompareProtection(a, b, false, false).Passed) throw new InvalidOperationException();
}
static void SnapshotDiff()
{
    var x = JsonSupport.Diff(new { hp=80, hand=new[]{"A"} }, new { hp=79, hand=new[]{"A"} });
    if (x.Count != 1 || x[0].Path != "$.hp") throw new InvalidOperationException();
}
static void HarmonyTargets()
{
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    if (typeof(NMainMenu).GetMethod(nameof(NMainMenu._Ready), flags) is null) throw new InvalidOperationException("main menu target missing");
    if (typeof(NGame).GetMethod(nameof(NGame._Input), flags, [typeof(Godot.InputEvent)]) is null) throw new InvalidOperationException("input target missing");
    if (typeof(SaveManager).GetMethod(nameof(SaveManager.SaveRun), flags, [typeof(AbstractRoom), typeof(bool)]) is null) throw new InvalidOperationException("save target missing");
    if (typeof(RunManager).GetMethod(nameof(RunManager.SetUpSavedSingleplayer), flags, [typeof(RunState), typeof(SerializableRun)]) is null) throw new InvalidOperationException("continue target missing");
    var ticker = typeof(ProbeTicker); if (ticker.GetMethod(nameof(ProbeTicker._Process), flags) is null
        || ticker.GetMethod(nameof(ProbeTicker._EnterTree), flags) is null || ticker.GetMethod(nameof(ProbeTicker._ExitTree), flags) is null)
        throw new InvalidOperationException("ticker lifecycle missing");
}

static List<FieldObservation> ReadyFields() => ReadinessContract.RequiredFields
    .Select(x => new FieldObservation(x, "value", "true", "site." + x, null, null)).ToList();
static void Replace(List<FieldObservation> fields, string name, FieldObservation replacement)
{
    var index = fields.FindIndex(x => x.Name == name); fields[index] = replacement;
}
static RawReadinessSample Sample(long tick, IReadOnlyList<FieldObservation> fields) => new(tick, (ulong)tick, tick, "T", fields,
    new("S","P","PCS","H","C","E","R"), "TARGET", false, 0, "breaker");
static TargetIdentity Target() => new("SEED", "CHAR", 0, 1, 1, 2, "Monster");
static CheckpointEvidence Checkpoint(string hash, bool nullRoom)
{
    var id = FileId(hash, 10); return new(id, id, hash, "CANON", 2, nullRoom, "SEED", "CHAR", 0, 1, 1, 2);
}
static FileIdentity FileId(string hash, long length) => new("P", true, length, hash, "T");
static ProtectedState Protection(string hash, IReadOnlyList<string> inventory)
{
    const string root = @"C:\P"; return new(root, [new(root+@"\saves\current_run.save", true, 1, hash, "T")], new([], inventory));
}
static TerminalArtifact Terminal(string classification) => new(ProbeConstants.Schema, ProbeConstants.Version,
    ProbeConstants.AttemptPrefix + "test", "T", 1, classification, "site", "reason", null,
    new([],0,[],null,[],0,0,0,null,null,null,0,0,0));
static string Hash(string path) { using var stream=File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
static string Temp() { var x=Path.Combine(Path.GetTempPath(),"spike001a3-tests-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(x); return x; }
static void Throws(Action action) { try { action(); } catch { return; } throw new InvalidOperationException("expected exception"); }
static async Task ThrowsAsync(Func<Task> action) { try { await action(); } catch { return; } throw new InvalidOperationException("expected exception"); }
static void Eq<T>(T expected, T actual) where T:notnull { if (!EqualityComparer<T>.Default.Equals(expected,actual)) throw new InvalidOperationException($"{expected} != {actual}"); }
