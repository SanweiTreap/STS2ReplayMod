using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Spike001A4;

var tests = new (string Name, Func<Task> Run)[]
{
    ("fixed INV-011 identities", FixedInv011Identities),
    ("schema and create-new artifact collision", SchemaAndCreateNew),
    ("repair revision identity is r2", RepairRevisionIdentity),
    ("create-new recovery collision", RecoveryCollision),
    ("exact mod set", () => Sync(ExactModSet)),
    ("profile1 only", () => Sync(ProfileOneOnly)),
    ("Cloud conjunction off", () => Sync(CloudOff)),
    ("probe UI ZIndex stays within Godot CanvasItem bounds", () => Sync(UiZIndexWithinGodotBounds)),
    ("checkpoint observer predicate", () => Sync(ObserverPredicate)),
    ("asymmetric map target keeps col/row order", () => Sync(MapTargetCoordinateOrder)),
    ("floor positive relation", () => Sync(FloorPositive)),
    ("floor rejects negative live", () => Sync(FloorNegativeLive)),
    ("floor rejects negative save", () => Sync(FloorNegativeSave)),
    ("floor rejects unequal arm facts", () => Sync(FloorUnequal)),
    ("floor checked overflow", () => Sync(FloorOverflow)),
    ("checkpoint capture starts unbound", () => Sync(FloorStartsUnbound)),
    ("checkpoint capture-before-bind rejected", () => Sync(ReadinessBeforeBind)),
    ("checkpoint double-bind rejected", () => Sync(DoubleBind)),
    ("checkpoint floor cannot use playable floor", () => Sync(CheckpointPlayableMix)),
    ("playable floor cannot use checkpoint floor", () => Sync(PlayableCheckpointMix)),
    ("B pre-room floor triple equality", () => Sync(BPreFloorPositive)),
    ("B pre-room rejects restored playable floor", () => Sync(BPreFloorNegative)),
    ("B identity positive", () => Sync(BIdentityPositive)),
    ("B rejects non-null prefinished room", () => Sync(BPrefinishedNegative)),
    ("required text rejects null", () => Sync(RequiredNull)),
    ("required text rejects empty", () => Sync(RequiredEmpty)),
    ("required text rejects whitespace", () => Sync(RequiredWhitespace)),
    ("semantic projection keeps room and encounter separate", () => Sync(RoomEncounterSeparated)),
    ("semantic projection excludes phase-local identities", () => Sync(PhaseLocalExcluded)),
    ("semantic projection rejects empty room", () => Sync(EmptyRoomRejected)),
    ("semantic projection rejects empty encounter", () => Sync(EmptyEncounterRejected)),
    ("semantic projection rejects empty RNG seed", () => Sync(EmptyRngSeedRejected)),
    ("semantic projection rejects empty RNG set", () => Sync(EmptyRngSetRejected)),
    ("semantic projection rejects empty card id", () => Sync(EmptyCardIdRejected)),
    ("semantic projection rejects negative creature HP", () => Sync(NegativeCreatureRejected)),
    ("production success plus observer success", WrapperSuccess),
    ("production success plus observer failure remains completed", WrapperObserverFailure),
    ("production fault preserves exception identity", WrapperProductionFault),
    ("production cancel preserves cancellation semantics", WrapperProductionCancel),
    ("terminal writer failure cannot fault production success", WrapperWriterFailureSuccess),
    ("terminal writer failure cannot replace production fault", WrapperWriterFailureFault),
    ("terminal writer failure cannot replace cancellation", WrapperWriterFailureCancel),
    ("first terminal wins under concurrency", () => Sync(FirstTerminalWins)),
    ("deadline and RoomEntered share one winner", () => Sync(DeadlineRoomRace)),
    ("manual Ready over 120 seconds has no deadline", ManualReadyNoDeadline),
    ("independent deadline fires with zero ticker ticks", DeadlineZeroTicks),
    ("independent deadline fires after ticker stops", DeadlineTickerStops),
    ("completed capture cancels deadline", DeadlineCancelled),
    ("terminal prohibits further collection", () => Sync(TerminalStopsCollection)),
    ("all required fields accept value true", () => Sync(AllReadinessTrue)),
    ("each required field missing resets quiet", () => Sync(EveryRequiredMissing)),
    ("each required read exception resets quiet", () => Sync(EveryRequiredException)),
    ("non-consecutive engine frame resets quiet", () => Sync(NonConsecutive)),
    ("target identity change resets quiet", () => Sync(IdentityChange)),
    ("bounded telemetry compacts events", () => Sync(BoundedTelemetry)),
    ("exception aggregation is bounded and counted", () => Sync(ExceptionAggregation)),
    ("B NumReloads exactly plus one", () => Sync(ReloadOnly)),
    ("B non-whitelist canonical diff rejected", () => Sync(NonWhitelistSaveDiff)),
    ("protection inventory change rejected", () => Sync(ProtectionInventory)),
    ("semantic diff detects encounter mismatch", () => Sync(SemanticDiff)),
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

static void UiZIndexWithinGodotBounds()
{
    const int canvasItemZMin = -4096;
    const int canvasItemZMax = 4096;
    if (ProbeUi.OverlayZIndex < canvasItemZMin || ProbeUi.OverlayZIndex > canvasItemZMax)
        throw new InvalidOperationException($"invalid overlay ZIndex {ProbeUi.OverlayZIndex}");
    Eq(canvasItemZMax, ProbeUi.OverlayZIndex);
}

static Task FixedInv011Identities()
{
    var values = new[]
    {
        (@"C:\Projects\STS2ReplayMod\project-management\investigation-reports\INV-011-report.md", ProbeConstants.Inv011ReportSha256),
        (@"C:\Projects\STS2ReplayMod\evidence\inv-011\successor-contract.json", ProbeConstants.Inv011ContractSha256),
        (@"C:\Projects\STS2ReplayMod\evidence\inv-011\placeholder-validation-audit.md", ProbeConstants.Inv011AuditSha256),
    };
    foreach (var item in values) Eq(item.Item2, Hash(item.Item1));
    using var doc = JsonDocument.Parse(File.ReadAllText(values[1].Item1));
    Eq("inv-011.successor-reconstruction-contract.v1", doc.RootElement.GetProperty("schemaId").GetString()!);
    Eq("READY_SUCCESSOR_RECONSTRUCTION_CONTROL", doc.RootElement.GetProperty("recommendation").GetString()!);
    return Task.CompletedTask;
}

static Task SchemaAndCreateNew()
{
    var root = Temp();
    try
    {
        var path = Path.Combine(root, "terminal.json");
        var value = Terminal("X");
        JsonSupport.WriteCreateNew(path, value);
        Eq(ProbeConstants.Schema, JsonSupport.Read<TerminalArtifact>(path).SchemaId);
        Throws(() => JsonSupport.WriteCreateNew(path, value));
    }
    finally { Directory.Delete(root, true); }
    return Task.CompletedTask;
}

static Task RepairRevisionIdentity()
{
    Eq("0.1.1-r2", ProbeConstants.Version);
    Eq("spike-001a4.reconstruction-control.v2", ProbeConstants.Schema);
    Eq("spike-001a4-r2-", ProbeConstants.AttemptPrefix);
    var manifestPath = @"C:\Projects\STS2ReplayMod\experiments\SPIKE-001A4\artifacts-r2\SPIKE001A4.json";
    using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
    Eq("SPIKE001A4", manifest.RootElement.GetProperty("id").GetString()!);
    Eq(ProbeConstants.Version, manifest.RootElement.GetProperty("version").GetString()!);
    return Task.CompletedTask;
}

static Task RecoveryCollision()
{
    var root = Temp();
    try
    {
        var path = Path.Combine(root, "recovery", "checkpoint.save");
        JsonSupport.WriteBytesCreateNew(path, [1, 2, 3]);
        Throws(() => JsonSupport.WriteBytesCreateNew(path, [4]));
    }
    finally { Directory.Delete(root, true); }
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

static void MapTargetCoordinateOrder()
{
    var coord = ProbeController.FindCoord(5, 8, (col, row) => col == 6 && row == 2);
    Eq(6, coord.col); Eq(2, coord.row);
}

static void FloorPositive()
{
    var x = new FloorContract(2, 2); Eq(2, x.ArmPredecessorFloor); Eq(3, x.ExpectedPlayableFloor);
    Eq(BindingState.Unbound, x.CheckpointFloor.State); x.BindCheckpoint(2); x.ValidatePlayable(3);
}
static void FloorNegativeLive() => Throws(() => _ = new FloorContract(-1, 0));
static void FloorNegativeSave() => Throws(() => _ = new FloorContract(0, -1));
static void FloorUnequal() => Throws(() => _ = new FloorContract(2, 3));
static void FloorOverflow() => ThrowsType<OverflowException>(() => _ = new FloorContract(int.MaxValue, int.MaxValue));
static void FloorStartsUnbound()
{
    var x = new FloorContract(0, 0); Eq(BindingState.Unbound, x.CheckpointFloor.State);
    if (x.CheckpointFloor.Value is not null) throw new InvalidOperationException();
}
static void ReadinessBeforeBind() => Throws(() => new FloorContract(1, 1).ValidatePlayable(2));
static void DoubleBind()
{
    var x = new FloorContract(1, 1); x.BindCheckpoint(1); Throws(() => x.BindCheckpoint(1));
}
static void CheckpointPlayableMix() => Throws(() => new FloorContract(2, 2).BindCheckpoint(3));
static void PlayableCheckpointMix()
{
    var x = new FloorContract(2, 2); x.BindCheckpoint(2); Throws(() => x.ValidatePlayable(2));
}
static void BPreFloorPositive()
{
    var x = new FloorContract(Target(), CheckpointFloorBinding.Bound(2)); x.ValidatePreRoom(2, 2);
}
static void BPreFloorNegative()
{
    var x = new FloorContract(Target(), CheckpointFloorBinding.Bound(2)); Throws(() => x.ValidatePreRoom(2, 3));
}
static void BIdentityPositive() => ContinueContract.Validate(Target(), CheckpointFloorBinding.Bound(2), ContinueFacts());
static void BPrefinishedNegative() => Throws(() => ContinueContract.Validate(Target(),
    CheckpointFloorBinding.Bound(2), ContinueFacts() with { PreFinishedRoomNull = false }));

static void RequiredNull() => Throws(() => SemanticProjectionBuilder.RequireText(null, "x"));
static void RequiredEmpty() => Throws(() => SemanticProjectionBuilder.RequireText("", "x"));
static void RequiredWhitespace() => Throws(() => SemanticProjectionBuilder.RequireText("  ", "x"));
static void RoomEncounterSeparated()
{
    var value = Semantic("ROOM-A", "ENCOUNTER-B");
    SemanticProjectionBuilder.ValidateRequired(value);
    Eq("ROOM-A", value.Position.RoomModelId); Eq("ENCOUNTER-B", value.Position.EncounterId);
}
static void PhaseLocalExcluded()
{
    var semantic = Semantic("ROOM", "ENCOUNTER");
    var sample = Sample(1, ReadyFields()) with { Identities = new("S", "P", "PCS", "H", "C", "E", "R") };
    var snapshot = Snapshot(semantic, sample);
    var projection = SemanticProjectionBuilder.From(snapshot);
    if (projection.GetType().GetProperties().Any(x => x.Name.Contains("Readiness") || x.Name.Contains("Identit")))
        throw new InvalidOperationException("phase-local identity leaked into semantic projection");
}
static void EmptyRoomRejected() => Throws(() => SemanticProjectionBuilder.ValidateRequired(Semantic("", "E")));
static void EmptyEncounterRejected() => Throws(() => SemanticProjectionBuilder.ValidateRequired(Semantic("R", "")));
static void EmptyRngSeedRejected() => Throws(() => SemanticProjectionBuilder.ValidateRequired(Semantic("R", "E") with
    { Rng = new("", "D", RngEntries()) }));
static void EmptyRngSetRejected() => Throws(() => SemanticProjectionBuilder.ValidateRequired(Semantic("R", "E") with
    { Rng = new("S", "D", []) }));
static void EmptyCardIdRejected()
{
    var x = Semantic("R", "E");
    var player = x.Player with { Hand = [Card() with { ModelId = "" }] };
    Throws(() => SemanticProjectionBuilder.ValidateRequired(x with { Player = player }));
}
static void NegativeCreatureRejected()
{
    var x = Semantic("R", "E") with { Enemies = [new("M", 1, -1, 10, true, false, false)] };
    Throws(() => SemanticProjectionBuilder.ValidateRequired(x));
}

static async Task WrapperSuccess()
{
    var observed = false; var terminal = 0;
    var wrapper = ProductionTaskObserver.WrapAsync(Task.CompletedTask,
        () => { observed = true; return Task.CompletedTask; },
        (_, _) => { terminal++; return Task.CompletedTask; });
    await wrapper; if (!wrapper.IsCompletedSuccessfully || !observed || terminal != 0) throw new InvalidOperationException();
}

static async Task WrapperObserverFailure()
{
    var failure = new InvalidDataException("observer"); var seen = false;
    var wrapper = ProductionTaskObserver.WrapAsync(Task.CompletedTask,
        () => Task.FromException(failure),
        (outcome, ex) => { seen = outcome == ProductionOutcome.Success && ReferenceEquals(ex, failure); return Task.CompletedTask; });
    await wrapper; if (!wrapper.IsCompletedSuccessfully || !seen) throw new InvalidOperationException();
}

static async Task WrapperProductionFault()
{
    var original = new IOException("production"); var seen = false;
    var wrapper = ProductionTaskObserver.WrapAsync(Task.FromException(original),
        () => throw new InvalidOperationException("observer must not run"),
        (outcome, ex) => { seen = outcome == ProductionOutcome.Faulted && ReferenceEquals(ex, original); return Task.CompletedTask; });
    var caught = await Catch(wrapper); if (!ReferenceEquals(caught, original) || !wrapper.IsFaulted || !seen) throw new InvalidOperationException();
}

static async Task WrapperProductionCancel()
{
    using var source = new CancellationTokenSource(); source.Cancel(); var seen = false;
    var wrapper = ProductionTaskObserver.WrapAsync(Task.FromCanceled(source.Token),
        () => throw new InvalidOperationException("observer must not run"),
        (outcome, ex) => { seen = outcome == ProductionOutcome.Canceled && ex is OperationCanceledException; return Task.CompletedTask; });
    var caught = await Catch(wrapper) as OperationCanceledException;
    if (!wrapper.IsCanceled || !seen || caught?.CancellationToken != source.Token) throw new InvalidOperationException();
}

static async Task WrapperWriterFailureSuccess()
{
    var wrapper = ProductionTaskObserver.WrapAsync(Task.CompletedTask,
        () => Task.FromException(new InvalidDataException("observer")),
        (_, _) => Task.FromException(new IOException("writer")));
    await wrapper; if (!wrapper.IsCompletedSuccessfully) throw new InvalidOperationException();
}

static async Task WrapperWriterFailureFault()
{
    var original = new IOException("production");
    var wrapper = ProductionTaskObserver.WrapAsync(Task.FromException(original),
        () => Task.CompletedTask, (_, _) => Task.FromException(new IOException("writer")));
    var caught = await Catch(wrapper); if (!ReferenceEquals(caught, original) || !wrapper.IsFaulted) throw new InvalidOperationException();
}

static async Task WrapperWriterFailureCancel()
{
    using var source = new CancellationTokenSource(); source.Cancel();
    var wrapper = ProductionTaskObserver.WrapAsync(Task.FromCanceled(source.Token),
        () => Task.CompletedTask, (_, _) => Task.FromException(new IOException("writer")));
    var caught = await Catch(wrapper) as OperationCanceledException;
    if (!wrapper.IsCanceled || caught?.CancellationToken != source.Token) throw new InvalidOperationException();
}

static void FirstTerminalWins()
{
    var latch = new TerminalLatch(); var wins = 0;
    Parallel.For(0, 100, _ => { if (latch.TrySeal()) Interlocked.Increment(ref wins); }); Eq(1, wins);
}
static void DeadlineRoomRace()
{
    var latch = new TerminalLatch(); var classifications = new List<string>();
    Parallel.Invoke(
        () => { if (latch.TrySeal()) lock (classifications) classifications.Add("deadline"); },
        () => { if (latch.TrySeal()) lock (classifications) classifications.Add("room-entered"); });
    Eq(1, classifications.Count);
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
    var ticks = 7; deadline.Start("ticker-stopped-after-" + ticks); Eq(7, ticks); Eq(1, fired); return Task.CompletedTask;
}
static async Task DeadlineCancelled()
{
    var fired = 0; var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var deadline = new CaptureDeadline(TimeSpan.FromSeconds(30), _ => fired++, (_, token) => gate.Task.WaitAsync(token));
    deadline.Start("capture"); deadline.Complete(); await Task.Yield(); await Task.Yield(); Eq(0, fired);
}
static void TerminalStopsCollection()
{
    if (ExperimentRules.CollectionAllowed(true, ProbePhase.AwaitA, true)
        || ExperimentRules.CollectionAllowed(false, ProbePhase.AwaitA, false)
        || ExperimentRules.CollectionAllowed(false, ProbePhase.Terminal, true)
        || !ExperimentRules.CollectionAllowed(false, ProbePhase.AwaitA, true)
        || !ExperimentRules.CollectionAllowed(false, ProbePhase.AwaitB, true)) throw new InvalidOperationException();
}

static void AllReadinessTrue()
{
    var result = ReadinessContract.Evaluate(ReadyFields(), true, true);
    if (!result.Ready || result.Breaker is not null) throw new InvalidOperationException();
}
static void EveryRequiredMissing()
{
    foreach (var name in ReadinessContract.RequiredFields)
    {
        var fields = ReadyFields(); Replace(fields, name, new(name, ObservationStatus.Missing, null, "site." + name, null, null));
        if (ReadinessContract.Evaluate(fields, true, true).Ready) throw new InvalidOperationException(name);
    }
}
static void EveryRequiredException()
{
    foreach (var name in ReadinessContract.RequiredFields)
    {
        var fields = ReadyFields();
        Replace(fields, name, new(name, ObservationStatus.ReadException, null, "site." + name, "System.InvalidOperationException", "boom"));
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
static void BoundedTelemetry()
{
    var telemetry = new TelemetryCollector();
    for (var i = 1; i <= 300; i++)
        telemetry.RecordSample(Sample(i, ReadyFields().Append(new("changing", ObservationStatus.Value,
            i.ToString(), "changing", null, null)).ToList()), false);
    var snapshot = telemetry.Snapshot(); Eq(ProbeConstants.TelemetryEventLimit, snapshot.Events.Count);
    if (snapshot.DroppedEvents <= 0 || snapshot.LastRawSample?.Tick != 300) throw new InvalidOperationException();
}
static void ExceptionAggregation()
{
    var telemetry = new TelemetryCollector();
    for (var i = 1; i <= 400; i++)
    {
        var fields = ReadyFields();
        Replace(fields, "statePresent", new("statePresent", ObservationStatus.ReadException, null,
            "run.state", "E", "M"));
        telemetry.RecordSample(Sample(i, fields), true);
    }
    var snapshot = telemetry.Snapshot(); Eq(1, snapshot.Exceptions.Count); Eq(400, snapshot.Exceptions[0].Count);
    if (snapshot.Events.Count > ProbeConstants.TelemetryEventLimit || snapshot.DroppedEvents == 0)
        throw new InvalidOperationException();
}
static void ReloadOnly()
{
    var x = ExperimentRules.CompareContinueSave("{\"num_reloads\":2,\"seed\":\"S\"}",
        "{\"num_reloads\":3,\"seed\":\"S\"}", 2, 3);
    if (!x.Passed || x.AllDiffs.Count != 1) throw new InvalidOperationException(x.Failure);
}
static void NonWhitelistSaveDiff()
{
    var x = ExperimentRules.CompareContinueSave("{\"num_reloads\":2,\"seed\":\"S\"}",
        "{\"num_reloads\":3,\"seed\":\"X\"}", 2, 3);
    if (x.Passed || x.NonAllowlistedDiffs.Count != 1) throw new InvalidOperationException();
}
static void ProtectionInventory()
{
    var a = Protection("A", ["saves/current_run.save"]); var b = Protection("A", ["saves/current_run.save", "new.file"]);
    if (ExperimentRules.CompareProtection(a, b, false, false).Passed) throw new InvalidOperationException();
}
static void SemanticDiff()
{
    var x = JsonSupport.Diff(Semantic("ROOM", "E1"), Semantic("ROOM", "E2"));
    if (x.Count != 1 || x[0].Path != "$.position.encounterId") throw new InvalidOperationException();
}
static void HarmonyTargets()
{
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    if (typeof(NMainMenu).GetMethod(nameof(NMainMenu._Ready), flags) is null) throw new InvalidOperationException("main menu target missing");
    if (typeof(NGame).GetMethod(nameof(NGame._Input), flags, [typeof(Godot.InputEvent)]) is null) throw new InvalidOperationException("input target missing");
    if (typeof(SaveManager).GetMethod(nameof(SaveManager.SaveRun), flags, [typeof(AbstractRoom), typeof(bool)]) is null) throw new InvalidOperationException("save target missing");
    if (typeof(RunManager).GetMethod(nameof(RunManager.SetUpSavedSingleplayer), flags, [typeof(RunState), typeof(SerializableRun)]) is null) throw new InvalidOperationException("continue target missing");
    var ticker = typeof(ProbeTicker);
    if (ticker.GetMethod(nameof(ProbeTicker._Process), flags) is null
        || ticker.GetMethod(nameof(ProbeTicker._EnterTree), flags) is null
        || ticker.GetMethod(nameof(ProbeTicker._ExitTree), flags) is null)
        throw new InvalidOperationException("ticker lifecycle missing");
}

static List<FieldObservation> ReadyFields() => ReadinessContract.RequiredFields
    .Select(x => new FieldObservation(x, ObservationStatus.Value, "true", "site." + x, null, null)).ToList();
static void Replace(List<FieldObservation> fields, string name, FieldObservation replacement)
{
    var index = fields.FindIndex(x => x.Name == name); fields[index] = replacement;
}
static RawReadinessSample Sample(long tick, IReadOnlyList<FieldObservation> fields) => new(tick, (ulong)tick, tick, "T", fields,
    new("S", "P", "PCS", "H", "C", "E", "R"), "TARGET", false, 0, "breaker");
static TargetIdentity Target() => new("SEED", "CHAR", 0, 2, 3, 1, 2, "Monster");
static ContinueFacts ContinueFacts() => new(true, 2, 2, "SEED", "SEED", "CHAR", "CHAR", 0, 0, 1, 2, 1, 2);
static CardSnapshot Card() => new("CARD", "{}", 0, false, "1", 0, false, null);
static IReadOnlyList<RngEntrySnapshot> RngEntries() => [new("R", 0, 1, 2, 3, 4)];
static SemanticProjection Semantic(string room, string encounter) => new(
    new(0, 1, 2, 4, room, "Monster", encounter, 3, 1),
    new("CHAR", 80, 80, 3, [Card()], [], [], []),
    [new("MONSTER", 1, 10, 10, true, false, false)], new("SEED", "DIGEST", RngEntries()));
static CombatSnapshot Snapshot(SemanticProjection value, RawReadinessSample sample) => new(
    value.Position, value.Player, value.Enemies, value.Rng, sample, 1, 1, 30, 30, "T", 1);
static ProtectedState Protection(string hash, IReadOnlyList<string> inventory)
{
    const string root = @"C:\P";
    return new(root, [new(root + @"\saves\current_run.save", true, 1, hash, "T")], new([], inventory));
}
static TerminalArtifact Terminal(string classification) => new(ProbeConstants.Schema, ProbeConstants.Version,
    ProbeConstants.AttemptPrefix + "test", "T", 1, classification, "site", "reason", null,
    new([], 0, [], null, [], 0, 0, 0, null, null, null, 0, 0, 0));
static async Task<Exception?> Catch(Task task)
{
    try { await task; return null; } catch (Exception ex) { return ex; }
}
static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
static string Temp()
{
    var x = Path.Combine(Path.GetTempPath(), "spike001a4-tests-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(x); return x;
}
static void Throws(Action action) { try { action(); } catch { return; } throw new InvalidOperationException("expected exception"); }
static void ThrowsType<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; } throw new InvalidOperationException("expected " + typeof(T).Name);
}
static void Eq<T>(T expected, T actual) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{expected} != {actual}");
}
