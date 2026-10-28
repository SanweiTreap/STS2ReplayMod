using System.Diagnostics;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using Steamworks;

namespace Spike001A3;

internal static class ProbeController
{
    private static readonly object Gate = new();
    private static readonly TerminalLatch Terminal = new();
    private static readonly TelemetryCollector Telemetry = new();
    private static EnvironmentEvidence? _environment; private static ProbePhase _phase;
    private static string? _attemptId; private static string? _attemptRoot; private static TargetIdentity? _target;
    private static ProbeTicker? _ticker; private static bool _initialized; private static bool _roomEventAttached;
    private static CaptureDeadline? _deadline; private static int _quiet; private static long _tick;
    private static long _quietStartTick; private static ulong _quietStartFrame;
    private static ulong? _lastProcessFrame; private static string? _quietIdentityToken;

    internal static void Initialize()
    {
        Directory.CreateDirectory(ProbeConstants.EvidenceRoot); _initialized = true;
        GD.Print("[SPIKE-001A3] initialized; waiting for final post-loader main-menu gate.");
    }

    internal static void OnMainMenuReady()
    {
        if (!_initialized || Terminal.IsSealed) return;
        try
        {
            EnsureTicker(); AttachRoomEnteredGuard(); _environment = CaptureEnvironment();
            if (_environment.Failures.Count != 0)
            {
                var classification = _environment.Failures.Any(x => x.StartsWith("BLOCKED_IDENTITY", StringComparison.Ordinal))
                    ? "BLOCKED_IDENTITY" : "BLOCKED_ENVIRONMENT";
                var precheck = Path.Combine(ProbeConstants.EvidenceRoot,
                    "precheck-r2-" + Guid.NewGuid().ToString("N") + ".json");
                JsonSupport.WriteCreateNew(precheck, new PrecheckArtifact(ProbeConstants.Schema, ProbeConstants.Version,
                    DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId, _environment, classification));
                ProbeUi.Show(classification + "\n" + string.Join("\n", _environment.Failures)
                    + "\nStop and exit normally. No business attempt was consumed.");
                return;
            }
            ResolveAttemptAndPhase();
        }
        catch (Exception ex) { PreBusinessStop("environment.main-menu", ex); }
    }

    private static void EnsureTicker()
    {
        if (_ticker is not null && GodotObject.IsInstanceValid(_ticker) && _ticker.IsInsideTree()) return;
        var game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance unavailable at main-menu ready");
        _ticker = new ProbeTicker { Name = "Spike001A3PersistentTicker", ProcessMode = Node.ProcessModeEnum.Always };
        game.AddChild(_ticker); _ticker.SetProcess(true);
        OnTickerLifecycle(_ticker, "attach");
    }

    private static void AttachRoomEnteredGuard()
    {
        if (_roomEventAttached) return;
        var manager = RunManager.Instance ?? throw new InvalidOperationException("RunManager.Instance unavailable");
        manager.RoomEntered += OnRoomEntered; _roomEventAttached = true;
    }

    private static void ResolveAttemptAndPhase()
    {
        var attempts = Directory.EnumerateDirectories(ProbeConstants.EvidenceRoot, ProbeConstants.AttemptPrefix + "*",
            SearchOption.TopDirectoryOnly).OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (attempts.Count > 1) throw new InvalidDataException("multiple A3 attempt namespaces exist");
        if (attempts.Count == 0)
        {
            _phase = ProbePhase.FixturePreparation;
            ProbeUi.Show("READY_FIXTURE_PREP\nUse modded profile 1. Prepare an Act 1 map position with a reachable normal weak-monster node. Stop before clicking it, then press F8 once.");
            return;
        }
        _attemptRoot = attempts[0]; _attemptId = Path.GetFileName(_attemptRoot);
        var terminal = Exists(ProbeConstants.TerminalFile); var final = Exists(ProbeConstants.FinalFile);
        var arm = Exists(ProbeConstants.ArmFile); var baseline = Exists(ProbeConstants.BaselineFile);
        var a = Exists(ProbeConstants.SnapshotAFile); var postA = Exists(ProbeConstants.PostAFile);
        var bPre = Exists(ProbeConstants.BPreFile); var b = Exists(ProbeConstants.SnapshotBFile);
        if (terminal || final || b) { _phase = ProbePhase.Terminal; ProbeUi.Show("TERMINAL_ALREADY_RECORDED\nDo not rerun this probe."); return; }
        if (!arm) { Seal("FAIL_ISOLATION_OR_PROTOCOL", "attempt.resolve", "attempt directory exists without arm artifact"); return; }
        var armData = JsonSupport.Read<ArmArtifact>(PathOf(ProbeConstants.ArmFile)); _target = armData.Target;
        if (!baseline)
        {
            if (armData.ProcessId != System.Environment.ProcessId) { Seal("FAIL_HARNESS_AFTER_BUSINESS_BOUNDARY", "attempt.resume", "armed A process ended before baseline/A completion"); return; }
            _phase = ProbePhase.AwaitA;
            ProbeUi.Show("READY_A_NODE\nClick only the recorded target " + TargetText(_target) + ", then stop all input. Do not play, End Turn, open a choice, or claim rewards.");
            return;
        }
        var baselineData = JsonSupport.Read<BaselineArtifact>(PathOf(ProbeConstants.BaselineFile)); _target = baselineData.Target;
        if (!a)
        {
            if (baselineData.ProcessId != System.Environment.ProcessId) { Seal("FAIL_HARNESS_AFTER_BUSINESS_BOUNDARY", "attempt.resume", "formal baseline process ended without A"); return; }
            _phase = ProbePhase.AwaitA; StartDeadline("resume-a-after-baseline"); return;
        }
        var aData = JsonSupport.Read<SnapshotArtifact>(PathOf(ProbeConstants.SnapshotAFile));
        if (aData.ProcessId == System.Environment.ProcessId)
        {
            _phase = ProbePhase.WaitForAExit;
            ProbeUi.Show("READY_EXIT_AFTER_A\nA snapshot is sealed. Stop all input and exit normally now. Do not play, End Turn, open choices, or claim rewards.");
            return;
        }
        if (postA || bPre) { Seal("FAIL_ISOLATION_OR_PROTOCOL", "attempt.resume", "incomplete or repeated B process"); return; }
        VerifyAfterAAndArmB(baselineData);
    }

    internal static void ArmA()
    {
        lock (Gate)
        {
            if (Terminal.IsSealed || _phase != ProbePhase.FixturePreparation || _environment?.Failures.Count != 0) return;
            try
            {
                var state = RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("no active run");
                ValidateArmScene(state); var target = SelectTarget(state);
                var checkpoint = CheckpointReader.Capture();
                if (checkpoint.Evidence.PreFinishedRoomNull)
                    throw new InvalidOperationException("arm-time map checkpoint must be the existing non-null save");
                var protection = ProtectionScope.Capture(SaveManager.Instance);
                _attemptId = ProbeConstants.AttemptPrefix + Guid.NewGuid().ToString("N");
                _attemptRoot = Path.Combine(ProbeConstants.EvidenceRoot, _attemptId);
                Directory.CreateDirectory(_attemptRoot); _target = target;
                JsonSupport.WriteCreateNew(PathOf(ProbeConstants.ArmFile), new ArmArtifact(ProbeConstants.Schema,
                    ProbeConstants.Version, _attemptId, DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId,
                    _environment!, target, checkpoint.Evidence.SecondRead, checkpoint.Evidence, protection));
                _phase = ProbePhase.AwaitA; ResetQuiet();
                ProbeUi.Show("READY_A_NODE\nA is armed for " + TargetText(target)
                    + ". Click only that normal weak-monster node, then stop all input. No cards, End Turn, choices, pause, or rewards.");
            }
            catch (Exception ex) { PreBusinessStop("arm", ex); }
        }
    }

    internal static bool ShouldWrapProductionSave(AbstractRoom? preFinishedRoom, bool saveProgress)
    {
        lock (Gate)
        {
            if (_phase != ProbePhase.AwaitA || _target is null || _attemptRoot is null || Exists(ProbeConstants.BaselineFile)) return false;
            var state = RunManager.Instance?.DebugOnlyGetState(); var coord = state?.CurrentMapCoord;
            var targetMatches = coord?.row == _target.MapRow && coord?.col == _target.MapCol;
            return ExperimentRules.ShouldWrapCheckpoint(true, targetMatches, preFinishedRoom, saveProgress);
        }
    }

    internal static async Task WrapProductionSaveAsync(Task original)
    {
        try { await CheckpointObservation.AwaitProductionAsync(original); }
        catch (Exception ex) { Seal("BLOCKED_CHECKPOINT_WRITE_FAILED", "checkpoint.production-task", ex.GetType().FullName + ": " + ex.Message); throw; }
        StartDeadline("production-save-task-completed");
        lock (Gate)
        {
            if (Terminal.IsSealed) return;
            try
            {
                var arm = JsonSupport.Read<ArmArtifact>(PathOf(ProbeConstants.ArmFile));
                var captured = CheckpointReader.Capture(); CheckpointReader.ValidateTarget(captured.Evidence, arm.Target, arm.NonNullSaveAtArm);
                var target = arm.Target with { TotalFloor = captured.Evidence.FloorReached }; _target = target;
                var protection = ProtectionScope.Capture(SaveManager.Instance);
                if (_deadline?.IsExpired != false) throw new TimeoutException("checkpoint validation exceeded the monotonic capture deadline");
                JsonSupport.WriteBytesCreateNew(PathOf(ProbeConstants.BaselineRecoveryFile), captured.RawBytes);
                if (_deadline.IsExpired) throw new TimeoutException("checkpoint recovery seal exceeded the monotonic capture deadline");
                var recovery = new FileIdentity(PathOf(ProbeConstants.BaselineRecoveryFile), true, captured.RawBytes.LongLength,
                    JsonSupport.Sha256Bytes(captured.RawBytes), File.GetLastWriteTimeUtc(PathOf(ProbeConstants.BaselineRecoveryFile)).ToString("O"));
                JsonSupport.WriteCreateNew(PathOf(ProbeConstants.BaselineFile), new BaselineArtifact(ProbeConstants.Schema,
                    ProbeConstants.Version, _attemptId!, DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId,
                    _environment!, target, captured.Evidence, JsonSupport.Canonicalize(captured.RawJson), recovery,
                    protection, _deadline!.StartedUtc, _deadline.StartedMonotonicTicks));
                ResetQuiet();
            }
            catch (Exception ex)
            {
                Seal(ex is TimeoutException ? "BLOCKED_CAPTURE_DEADLINE" : "BLOCKED_CHECKPOINT_VALIDATION",
                    "checkpoint.baseline", ex.GetType().FullName + ": " + ex.Message);
                throw;
            }
        }
    }

    internal static void OnNormalContinueBegin(RunState state, SerializableRun save)
    {
        lock (Gate)
        {
            if (Terminal.IsSealed || _phase != ProbePhase.AwaitB || _target is null) return;
            try
            {
                var target = _target ?? throw new InvalidOperationException("target unavailable");
                var player = save.Players.Single(); var coord = save.VisitedMapCoords.Last();
                if (save.SerializableRng?.Seed != target.Seed || player.CharacterId?.ToString() != target.Character
                    || save.CurrentActIndex != target.ActIndex || coord.row != target.MapRow || coord.col != target.MapCol
                    || state.CurrentActIndex != target.ActIndex)
                    throw new InvalidDataException("normal Continue input does not match sealed target");
                StartDeadline("normal-continue-setup-begin"); ResetQuiet();
            }
            catch (Exception ex) { Seal("BLOCKED_ENVIRONMENT", "continue.begin", ex.GetType().FullName + ": " + ex.Message); }
        }
    }

    private static void OnRoomEntered()
    {
        lock (Gate)
        {
            if (_phase != ProbePhase.AwaitA || _target is null || Exists(ProbeConstants.BaselineFile)) return;
            var coord = RunManager.Instance?.DebugOnlyGetState()?.CurrentMapCoord;
            if (CheckpointObservation.RoomEnteredBeforeBaseline(
                    coord?.row == _target.MapRow && coord?.col == _target.MapCol, Exists(ProbeConstants.BaselineFile)))
                Seal("BLOCKED_CHECKPOINT_OBSERVATION_MISSED", "room-entered.guard", "target RoomEntered occurred before verified baseline");
        }
    }

    internal static void Tick(ProbeTicker ticker)
    {
        var tick = Interlocked.Increment(ref _tick); var frame = Engine.GetProcessFrames(); var monotonic = Stopwatch.GetTimestamp();
        var combat = CombatManager.Instance?.IsInProgress == true; var baseline = _attemptRoot is not null && Exists(ProbeConstants.BaselineFile);
        Telemetry.TickSeen(tick, frame, monotonic, baseline, combat);
        if (tick == 1 || tick % ProbeConstants.PeriodicSampleTicks == 0) OnTickerLifecycle(ticker, tick == 1 ? "first-global-tick" : "periodic");
        if (Terminal.IsSealed || _phase is not (ProbePhase.AwaitA or ProbePhase.AwaitB) || _target is null || !baseline) { _lastProcessFrame = frame; return; }
        if (_phase == ProbePhase.AwaitB && _deadline?.IsStarted != true) { _lastProcessFrame = frame; return; }
        var sample = ReadinessReader.Read(_target, tick, frame, monotonic, _quiet, _quietIdentityToken, _lastProcessFrame, false);
        var breaker = !sample.RequiredReady; Telemetry.RecordSample(sample, breaker);
        _lastProcessFrame = frame;
        if (!sample.RequiredReady) { ResetQuiet(false); return; }
        if (_quiet == 0) { _quietStartTick = tick; _quietStartFrame = frame; _quietIdentityToken = ContinuityIdentity(sample); }
        _quiet++;
        if (_quiet < ProbeConstants.QuietFrames) return;
        var recheck = ReadinessReader.Read(_target, tick, frame, Stopwatch.GetTimestamp(), _quiet,
            _quietIdentityToken, frame, true);
        Telemetry.RecordSample(recheck, !recheck.RequiredReady);
        if (!recheck.RequiredReady) { ResetQuiet(false); return; }
        lock (Gate)
        {
            if (Terminal.IsSealed) return;
            try { if (_phase == ProbePhase.AwaitA) CaptureA(recheck); else CaptureB(recheck); }
            catch (Exception ex) { Seal("FAIL_HARNESS_AFTER_BUSINESS_BOUNDARY", "snapshot.capture", ex.GetType().FullName + ": " + ex.Message); }
        }
    }

    internal static void OnTickerLifecycle(ProbeTicker ticker, string eventName)
    {
        try
        {
            var parent = ticker.GetParent(); var tree = ticker.GetTree();
            Telemetry.RecordHeartbeat(new(eventName, DateTimeOffset.UtcNow.ToString("O"), Volatile.Read(ref _tick),
                Engine.GetProcessFrames(), Stopwatch.GetTimestamp(), parent?.Name, ticker.IsInsideTree() ? ticker.GetPath().ToString() : null,
                ticker.GetInstanceId(), GodotObject.IsInstanceValid(ticker), ticker.IsInsideTree(), ticker.IsProcessing(),
                ticker.ProcessMode.ToString(), tree?.Paused ?? false));
        }
        catch (Exception ex) { GD.PrintErr("[SPIKE-001A3] heartbeat failed: " + ex); }
    }

    private static void CaptureA(RawReadinessSample readiness)
    {
        var baseline = JsonSupport.Read<BaselineArtifact>(PathOf(ProbeConstants.BaselineFile)); ValidateCombat(baseline.Target);
        var current = ProtectionScope.CurrentRunIdentity();
        if (!ExperimentRules.SameCheckpointContent(baseline.Checkpoint.SecondRead, current))
        { Seal("BLOCKED_SOURCE_CHANGED_AFTER_A", "snapshot-a.checkpoint", "checkpoint content changed before A snapshot"); return; }
        var protection = ProtectionScope.Capture(SaveManager.Instance); var snapshot = SnapshotCapture.Capture(readiness, _quietStartTick, _quietStartFrame);
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.SnapshotAFile), new SnapshotArtifact(ProbeConstants.Schema,
            ProbeConstants.Version, _attemptId!, "A", DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId,
            _environment!, baseline.Checkpoint, current, protection, snapshot, Telemetry.Snapshot()));
        _deadline?.Complete(); _phase = ProbePhase.WaitForAExit;
        ProbeUi.Show("READY_EXIT_AFTER_A\nA snapshot is sealed. Immediately stop all input and exit normally. Do not play, End Turn, pause, open choices, or claim any reward.");
    }

    private static void VerifyAfterAAndArmB(BaselineArtifact baseline)
    {
        var current = ProtectionScope.CurrentRunIdentity(); var protection = ProtectionScope.Capture(SaveManager.Instance);
        var checkpointEqual = ExperimentRules.SameCheckpointContent(baseline.Checkpoint.SecondRead, current);
        var pDiff = ExperimentRules.CompareProtection(baseline.Protection, protection, true, false);
        var noRun = RunManager.Instance?.DebugOnlyGetState() is null && CombatManager.Instance?.IsInProgress != true;
        var passed = checkpointEqual && pDiff.Passed && noRun;
        var verification = new VerificationArtifact(ProbeConstants.Schema, ProbeConstants.Version, _attemptId!,
            DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId, "A_POST_AND_B_PRECONTINUE",
            baseline.Checkpoint.SecondRead, current, checkpointEqual, pDiff, noRun, passed,
            passed ? "READY_B_CONTINUE" : "BLOCKED_SOURCE_CHANGED_AFTER_A");
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.PostAFile), verification);
        if (!passed) { Seal("BLOCKED_SOURCE_CHANGED_AFTER_A", "b.preflight", "checkpoint/profile protection or no-run gate failed after A"); return; }
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.BPreFile), verification with { Stage = "B_PRECONTINUE" });
        _phase = ProbePhase.AwaitB; _deadline = null; ResetQuiet();
        ProbeUi.Show("READY_B_CONTINUE\nA-post and B-pre gates passed. Click the normal main-menu Continue exactly once, then stop all input. Do not play, End Turn, pause, open choices, or claim rewards.");
    }

    private static void CaptureB(RawReadinessSample readiness)
    {
        var baseline = JsonSupport.Read<BaselineArtifact>(PathOf(ProbeConstants.BaselineFile)); ValidateCombat(baseline.Target);
        var a = JsonSupport.Read<SnapshotArtifact>(PathOf(ProbeConstants.SnapshotAFile));
        var after = CheckpointReader.Capture(); var protection = ProtectionScope.Capture(SaveManager.Instance);
        var snapshot = SnapshotCapture.Capture(readiness, _quietStartTick, _quietStartFrame);
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.SnapshotBFile), new SnapshotArtifact(ProbeConstants.Schema,
            ProbeConstants.Version, _attemptId!, "B", DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId,
            _environment!, baseline.Checkpoint, after.Evidence.SecondRead, protection, snapshot, Telemetry.Snapshot()));
        var snapshotDiff = JsonSupport.Diff(Normalize(a.Snapshot), Normalize(snapshot));
        var saveDiff = ExperimentRules.CompareContinueSave(baseline.CheckpointCanonicalJson,
            JsonSupport.Canonicalize(after.RawJson), baseline.Checkpoint.NumReloads, after.Evidence.NumReloads);
        var protectionDiff = ExperimentRules.CompareProtection(baseline.Protection, protection, true, true);
        var classification = !saveDiff.Passed || !protectionDiff.Passed ? "FAIL_ISOLATION_OR_PROTOCOL"
            : snapshotDiff.Count != 0 ? "FAIL_RECONSTRUCTION_DIFF" : "PASS_RECONSTRUCTION_EQUIVALENT";
        var first = !saveDiff.Passed ? saveDiff.Failure : !protectionDiff.Passed ? "profile1 protection diff"
            : snapshotDiff.FirstOrDefault()?.Path ?? "None";
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.FinalFile), new FinalArtifact(ProbeConstants.Schema,
            ProbeConstants.Version, _attemptId!, DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId,
            classification, snapshotDiff, saveDiff, protectionDiff, first,
            "Only NumReloads +1, wall-clock, probe sequence, and observation tick/frame are excluded."));
        Seal(classification, "compare.final", first);
    }

    private static CombatSnapshot Normalize(CombatSnapshot value) => value with
    {
        CapturedUtc = "", ProcessId = 0, QuietStartTick = 0, QuietStartFrame = 0, SnapshotTick = 0, SnapshotFrame = 0,
        Readiness = value.Readiness with
        {
            Tick = 0, EngineFrame = 0, MonotonicTicks = 0, CreatedUtc = "", QuietFramesObserved = 0,
            Identities = new(null, null, null, null, null, null, null)
        }
    };

    private static void ValidateArmScene(RunState state)
    {
        if (state.Players.Count != 1 || !state.GameMode.ToString().Equals("Standard", StringComparison.OrdinalIgnoreCase)
            || state.CurrentActIndex != 0 || CombatManager.Instance?.IsInProgress == true || state.CurrentMapPoint is null)
            throw new InvalidOperationException("expected Act 1 Standard map with no combat in progress");
    }

    private static TargetIdentity SelectTarget(RunState state)
    {
        var current = state.CurrentMapPoint ?? throw new InvalidOperationException("current map point unavailable");
        var candidates = current.Children.Where(x => x.PointType == MapPointType.Monster)
            .Select(x => (Point: x, Coord: FindCoord(state.Map, x))).OrderBy(x => x.Coord.row).ThenBy(x => x.Coord.col).ToList();
        if (candidates.Count == 0) throw new InvalidOperationException("no reachable normal weak-monster target");
        var chosen = candidates[0]; var seed = state.Rng.ToSerializable().Seed ?? "";
        return new(seed, state.Players.Single().Character.Id.ToString(), state.CurrentActIndex, -1,
            chosen.Coord.row, chosen.Coord.col, chosen.Point.PointType.ToString());
    }

    private static MapCoord FindCoord(ActMap map, MapPoint point)
        => FindCoord(map.GetRowCount(), map.GetColumnCount(),
            (col, row) => ReferenceEquals(map.GetPoint(col, row), point));

    internal static MapCoord FindCoord(int rowCount, int columnCount, Func<int, int, bool> matches)
    {
        for (var row = 0; row < rowCount; row++)
        for (var col = 0; col < columnCount; col++)
            if (matches(col, row)) return new MapCoord(col, row);
        throw new InvalidOperationException("target point not found in map grid");
    }

    private static void ValidateCombat(TargetIdentity target)
    {
        var state = RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("run state unavailable");
        var combat = CombatManager.Instance?.DebugOnlyGetState(); var coord = state.CurrentMapCoord;
        if (state.Players.Count != 1 || state.CurrentActIndex != target.ActIndex || coord?.row != target.MapRow
            || coord?.col != target.MapCol || combat?.RoundNumber != 1 || state.CurrentRoom is null
            || !state.CurrentRoom.RoomType.ToString().Contains("Monster", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("expected armed Act 1 weak-monster target at round 1");
    }

    private static EnvironmentEvidence CaptureEnvironment()
    {
        var failures = new List<string>(); var sts2 = typeof(NGame).Assembly.Location; var info = new FileInfo(sts2);
        var gameRoot = Directory.GetParent(Path.GetDirectoryName(sts2)!)!.FullName;
        using var release = JsonDocument.Parse(File.ReadAllText(Path.Combine(gameRoot, "release_info.json")));
        var version = release.RootElement.GetProperty("version").GetString() ?? "";
        var commit = release.RootElement.GetProperty("commit").GetString() ?? ""; var hash = JsonSupport.Sha256File(sts2);
        var mods = ModManager.GetLoadedMods().Where(x => x.manifest is not null).Select(x => x.manifest!.id ?? "")
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        var manager = SaveManager.Instance; var token = UserDataPathProvider.GetProfileDir(manager.CurrentProfileId).Replace('\\', '/');
        bool accountCloud; bool appCloud;
        try { accountCloud = SteamRemoteStorage.IsCloudEnabledForAccount(); appCloud = SteamRemoteStorage.IsCloudEnabledForApp(); }
        catch (Exception ex) { accountCloud = appCloud = true; failures.Add("Steam Cloud unreadable: " + ex.GetType().FullName); }
        if (version != ProbeConstants.GameVersion || commit != ProbeConstants.GameCommit
            || info.Length != ProbeConstants.GameDllLength || hash != ProbeConstants.GameDllSha256)
            failures.Add("BLOCKED_IDENTITY game/DLL mismatch");
        if (!ExperimentRules.ExactModSet(mods)) failures.Add("loaded mod set must be exactly [SPIKE001A3]; actual=[" + string.Join(',', mods) + "]");
        if (!ExperimentRules.IsProfileOne(manager.CurrentProfileId, token, UserDataPathProvider.IsRunningModded)) failures.Add("active profile is not modded/profile1");
        if (!ExperimentRules.IsCloudOff(accountCloud, appCloud)) failures.Add("Steam Cloud production conjunction is enabled");
        var probe = typeof(ProbeController).Assembly.Location; var pi = new FileInfo(probe);
        return new(version, commit, info.Length, hash, ProbeConstants.Version, pi.Length, JsonSupport.Sha256File(probe), mods,
            manager.CurrentProfileId, token, UserDataPathProvider.IsRunningModded, accountCloud, appCloud,
            ExperimentRules.IsCloudOff(accountCloud, appCloud), failures);
    }

    private static void StartDeadline(string site)
    {
        if (_deadline?.IsStarted == true) return;
        _deadline = new CaptureDeadline(TimeSpan.FromSeconds(ProbeConstants.CaptureDeadlineSeconds),
            owner => Seal("BLOCKED_CAPTURE_DEADLINE", owner, "30-second monotonic capture deadline expired"));
        _deadline.Start(site);
    }

    private static void ResetQuiet(bool resetFrame = true)
    {
        _quiet = 0; _quietStartTick = 0; _quietStartFrame = 0; _quietIdentityToken = null;
        if (resetFrame) _lastProcessFrame = null;
    }

    private static string ContinuityIdentity(RawReadinessSample sample) => string.Join("|", new[]
    {
        sample.Identities.State, sample.Identities.Player, sample.Identities.PlayerCombatState, sample.Identities.Hand,
        sample.Identities.Combat, sample.Identities.Executor, sample.Identities.Room, sample.TargetIdentity
    });

    private static void PreBusinessStop(string site, Exception ex)
    {
        var reason = ex.GetType().FullName + ": " + ex.Message;
        if (_attemptRoot is null) ProbeUi.Show("BLOCKED_ENVIRONMENT\n" + site + "\n" + reason + "\nStop and exit normally; no business attempt was consumed.");
        else Seal("BLOCKED_ENVIRONMENT", site, reason);
    }

    private static void Seal(string classification, string site, string reason)
    {
        if (!Terminal.TrySeal()) return;
        try
        {
            _deadline?.Complete(); CheckpointEvidence? baseline = null;
            if (_attemptRoot is not null && Exists(ProbeConstants.BaselineFile)) baseline = JsonSupport.Read<BaselineArtifact>(PathOf(ProbeConstants.BaselineFile)).Checkpoint;
            if (_attemptRoot is not null && !Exists(ProbeConstants.TerminalFile))
                JsonSupport.WriteCreateNew(PathOf(ProbeConstants.TerminalFile), new TerminalArtifact(ProbeConstants.Schema,
                    ProbeConstants.Version, _attemptId!, DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId,
                    classification, site, reason, baseline, Telemetry.Snapshot()));
        }
        catch (Exception ex) { GD.PrintErr("[SPIKE-001A3] terminal artifact write failed: " + ex); }
        _phase = ProbePhase.Terminal;
        ProbeUi.Show(classification + "\n" + site + "\n" + reason + "\nImmediately stop all input and exit normally. Do not retry this probe.");
    }

    private static string TargetText(TargetIdentity target) => $"Act {target.ActIndex + 1} row {target.MapRow} col {target.MapCol} {target.PointType}";
    private static bool Exists(string name) => _attemptRoot is not null && File.Exists(PathOf(name));
    private static string PathOf(string name) => Path.Combine(_attemptRoot ?? throw new InvalidOperationException("attempt root unavailable"), name);
}
