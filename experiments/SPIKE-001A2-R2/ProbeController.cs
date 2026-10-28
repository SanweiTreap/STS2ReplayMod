using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using Steamworks;
using System.Text.Json;

namespace Spike001A2R2;

internal static class ProbeController
{
    private static readonly object Gate = new();
    private static readonly TerminalLatch Terminal = new();
    private static EnvironmentEvidence? _environment; private static ProbePhase _phase;
    private static int _quiet; private static long _frame; private static long _firstReadyFrame = -1;
    private static bool _initialized; private static bool _bWasArmedAtMenu;
    private static ProbeTicker? _ticker;

    internal static void Initialize()
    {
        Directory.CreateDirectory(ProbeConstants.EvidenceRoot); _initialized = true;
        GD.Print("[SPIKE-001A2-R2] initialized; waiting for post-loader main menu gate.");
    }

    internal static void OnMainMenuReady()
    {
        if (!_initialized || Terminal.IsSealed) return;
        try
        {
            EnsureTicker();
            _environment = CaptureEnvironment();
            if (_environment.Failures.Count != 0)
            {
                var precheck = Path.Combine(ProbeConstants.EvidenceRoot, "precheck-r1-" + Guid.NewGuid().ToString("N") + ".json");
                JsonSupport.WriteCreateNew(precheck, _environment);
                ProbeUi.Show("BLOCKED_ENVIRONMENT\n" + string.Join("\n", _environment.Failures) + "\nStop and exit normally; do not begin fixture preparation.");
                return;
            }
            ResolveAndEnterPhase();
        }
        catch (Exception ex) { Seal("BLOCKED_ENVIRONMENT", ex.GetType().Name + ": " + ex.Message); }
    }

    private static void EnsureTicker()
    {
        if (_ticker is not null && GodotObject.IsInstanceValid(_ticker) && _ticker.IsInsideTree()) return;
        var game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance unavailable at main-menu ready");
        _ticker = new ProbeTicker { Name = "Spike001A2R2Ticker" };
        game.AddChild(_ticker);
        _ticker.SetProcess(true);
    }

    private static void ResolveAndEnterPhase()
    {
        var arm = Exists(ProbeConstants.ArmFile); var baseline = Exists(ProbeConstants.BaselineFile); var a = Exists(ProbeConstants.SnapshotAFile);
        var postA = Exists(ProbeConstants.PostAFile); var bPre = Exists(ProbeConstants.BPreFile);
        var b = Exists(ProbeConstants.SnapshotBFile); var terminal = Exists(ProbeConstants.TerminalFile);
        var armData = arm ? JsonSupport.Read<ArmArtifact>(PathOf(ProbeConstants.ArmFile)) : null;
        var baselineData = baseline ? JsonSupport.Read<BaselineArtifact>(PathOf(ProbeConstants.BaselineFile)) : null;
        var aData = a ? JsonSupport.Read<SnapshotArtifact>(PathOf(ProbeConstants.SnapshotAFile)) : null;
        var bPreData = bPre ? JsonSupport.Read<VerificationArtifact>(PathOf(ProbeConstants.BPreFile)) : null;
        _phase = ExperimentRules.ResolvePhase(arm, baseline, a, postA, bPre, b, terminal,
            armData?.ProcessId ?? -1, baselineData?.ProcessId ?? -1, aData?.ProcessId ?? -1,
            bPreData?.ProcessId ?? -1, System.Environment.ProcessId);
        switch (_phase)
        {
            case ProbePhase.FixturePreparation:
                ProbeUi.Show("READY_FIXTURE_PREP\nUse modded profile 1. Create the fixed-seed run and stop on the Act 1 map before clicking the first normal weak-monster node. Press F8 once to arm A; the native null-checkpoint will be sealed before the A snapshot.");
                break;
            case ProbePhase.WaitForAExit:
                ProbeUi.Show("READY_EXIT_AFTER_A\nA snapshot is sealed. Stop all input: do not play a card, End Turn, open a choice, or claim rewards. Exit the game normally now.");
                break;
            case ProbePhase.ReadyBContinue:
                VerifyAfterAAndArmB(baselineData!, aData!); break;
            case ProbePhase.AwaitB:
                _bWasArmedAtMenu = true;
                ProbeUi.Show("READY_B_CONTINUE\nCheckpoint and profile1 protection passed. Click the normal main-menu Continue exactly once, then provide no further input.");
                break;
            case ProbePhase.Terminal:
                if (!terminal) Seal("FAIL_ISOLATION_OR_PROTOCOL", "phase/process ordering mismatch or an incomplete prior process");
                else ProbeUi.Show("TERMINAL_ALREADY_RECORDED\nDo not rerun this probe.");
                break;
            case ProbePhase.AwaitA:
                ProbeUi.Show("READY_A_NODE\nA is armed. Click only the recorded first normal weak-monster node, then stop all input. The probe will seal the native null-checkpoint and capture A after 30 quiet frames.");
                break;
        }
    }

    internal static void EstablishBaseline()
    {
        lock (Gate)
        {
            if (Terminal.IsSealed || _environment?.Failures.Count != 0 || Exists(ProbeConstants.ArmFile) || Exists(ProbeConstants.BaselineFile)) return;
            try
            {
                var state = RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("no active run");
                ValidateBaselineScene(state);
                var (checkpoint, _) = CheckpointReader.Capture(state);
                var protection = ProtectionScope.Capture(SaveManager.Instance);
                var arm = new ArmArtifact(ProbeConstants.Schema, ProbeConstants.Version, DateTimeOffset.UtcNow.ToString("O"),
                    System.Environment.ProcessId, _environment!, checkpoint, protection);
                JsonSupport.WriteCreateNew(PathOf(ProbeConstants.ArmFile), arm);
                _phase = ProbePhase.AwaitA; _quiet = 0; _firstReadyFrame = -1;
                ProbeUi.Show("READY_A_NODE\nA armed; formal baseline is not yet established. Click only the recorded first normal weak-monster node, then stop. The native null-checkpoint will be sealed before A snapshot. No cards, End Turn, choices, or rewards.");
            }
            catch (Exception ex) { Seal("BLOCKED_ENVIRONMENT", "baseline: " + ex.Message); }
        }
    }

    internal static void Tick()
    {
        _frame++;
        if (Terminal.IsSealed || _phase is not (ProbePhase.AwaitA or ProbePhase.AwaitB)) return;
        if (_phase == ProbePhase.AwaitB && !_bWasArmedAtMenu) { Seal("FAIL_ISOLATION_OR_PROTOCOL", "B was not armed at the main-menu pre-Continue gate"); return; }
        var readiness = SnapshotCapture.TryReadiness(_quiet, _firstReadyFrame, _frame);
        if (readiness?.CandidateReady != true) { _quiet = 0; _firstReadyFrame = -1; return; }
        if (_quiet == 0) _firstReadyFrame = _frame;
        _quiet++;
        if (_quiet < ProbeConstants.QuietFrames) return;
        readiness = SnapshotCapture.TryReadiness(_quiet, _firstReadyFrame, _frame);
        if (readiness?.CandidateReady != true) { _quiet = 0; _firstReadyFrame = -1; return; }
        lock (Gate)
        {
            if (Terminal.IsSealed) return;
            try { if (_phase == ProbePhase.AwaitA) CaptureA(readiness); else CaptureB(readiness); }
            catch (Exception ex) { Seal("FAIL_ISOLATION_OR_PROTOCOL", "snapshot: " + ex.Message); }
        }
    }

    private static void CaptureA(ReadinessSnapshot readiness)
    {
        ValidateCombat();
        if (!Exists(ProbeConstants.BaselineFile))
        {
            var state = RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("run state unavailable while sealing baseline");
            var (checkpoint, raw) = CheckpointReader.Capture(state);
            if (!ExperimentRules.IsFormalRoomBeforeCheckpoint(checkpoint.PreFinishedRoomNull))
                throw new InvalidOperationException("native room-before checkpoint did not become PreFinishedRoom=null before A readiness");
            var baselineProtection = ProtectionScope.Capture(SaveManager.Instance);
            JsonSupport.WriteCreateNew(PathOf(ProbeConstants.BaselineFile), new BaselineArtifact(ProbeConstants.Schema,
                ProbeConstants.Version, DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId,
                _environment!, checkpoint, JsonSupport.Canonicalize(raw), baselineProtection));
        }
        var baseline = JsonSupport.Read<BaselineArtifact>(PathOf(ProbeConstants.BaselineFile));
        var current = ProtectionScope.CurrentRunIdentity();
        if (!ExperimentRules.SameCheckpointContent(baseline.Checkpoint.File, current))
        { Seal("BLOCKED_SOURCE_CHANGED_AFTER_A", "checkpoint content changed before A snapshot"); return; }
        var snapshot = SnapshotCapture.Capture(readiness); var protection = ProtectionScope.Capture(SaveManager.Instance);
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.SnapshotAFile), new SnapshotArtifact(ProbeConstants.Schema,
            ProbeConstants.Version, "A", DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId, _environment!,
            baseline.Checkpoint, current, protection, snapshot));
        _phase = ProbePhase.WaitForAExit;
        ProbeUi.Show("READY_EXIT_AFTER_A\nA snapshot sealed. Immediately stop: do not play a card, End Turn, open a choice, or claim rewards. Exit the game normally.");
    }

    private static void VerifyAfterAAndArmB(BaselineArtifact baseline, SnapshotArtifact a)
    {
        var current = ProtectionScope.CurrentRunIdentity(); var protection = ProtectionScope.Capture(SaveManager.Instance);
        var checkpointEqual = ExperimentRules.SameCheckpointContent(baseline.Checkpoint.File, current);
        var pDiff = ExperimentRules.CompareProtection(baseline.Protection, protection, true, false);
        var passed = checkpointEqual && pDiff.Passed;
        var verification = new VerificationArtifact(ProbeConstants.Schema, ProbeConstants.Version, DateTimeOffset.UtcNow.ToString("O"),
            System.Environment.ProcessId, "A_POST_EXIT", baseline.Checkpoint.File, current, checkpointEqual, pDiff, passed,
            passed ? "READY_B_CONTINUE" : "BLOCKED_SOURCE_CHANGED_AFTER_A");
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.PostAFile), verification);
        if (!passed) { Seal("BLOCKED_SOURCE_CHANGED_AFTER_A", "checkpoint/profile content or inventory changed after A"); return; }
        var bPre = verification with { Stage = "B_PRECONTINUE", Classification = "READY_B_CONTINUE" };
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.BPreFile), bPre);
        _phase = ProbePhase.AwaitB; _bWasArmedAtMenu = true;
        ProbeUi.Show("READY_B_CONTINUE\nA source verification passed. Click normal Continue exactly once, then provide no further input. The probe captures after 30 quiet frames.");
    }

    private static void CaptureB(ReadinessSnapshot readiness)
    {
        ValidateCombat(); var baseline = JsonSupport.Read<BaselineArtifact>(PathOf(ProbeConstants.BaselineFile));
        var a = JsonSupport.Read<SnapshotArtifact>(PathOf(ProbeConstants.SnapshotAFile));
        var (afterCheckpoint, afterRaw) = CheckpointReader.Capture(RunManager.Instance.DebugOnlyGetState());
        var protection = ProtectionScope.Capture(SaveManager.Instance); var snapshot = SnapshotCapture.Capture(readiness);
        var b = new SnapshotArtifact(ProbeConstants.Schema, ProbeConstants.Version, "B", DateTimeOffset.UtcNow.ToString("O"),
            System.Environment.ProcessId, _environment!, baseline.Checkpoint, afterCheckpoint.File, protection, snapshot);
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.SnapshotBFile), b);
        var semanticA = Normalize(a.Snapshot); var semanticB = Normalize(snapshot);
        var snapshotDiff = JsonSupport.Diff(semanticA, semanticB);
        var saveDiff = ExperimentRules.CompareContinueSave(baseline.CheckpointCanonicalJson, JsonSupport.Canonicalize(afterRaw),
            baseline.Checkpoint.NumReloads, afterCheckpoint.NumReloads);
        // The same content was re-captured at B pre-Continue; reconstruct its protection baseline from the A-post verification inputs.
        // Baseline protection remains canonical because post-A verification required content equality and only allowed fixed exit mtimes.
        var protectionDiff = ExperimentRules.CompareProtection(baseline.Protection, protection, true, true);
        var classification = !saveDiff.Passed || !protectionDiff.Passed ? "FAIL_ISOLATION_OR_PROTOCOL"
            : snapshotDiff.Count != 0 ? "FAIL_RECONSTRUCTION_DIFF" : "PASS_RECONSTRUCTION_EQUIVALENT";
        var first = !saveDiff.Passed ? saveDiff.Failure : !protectionDiff.Passed ? "profile1 protection diff" :
            snapshotDiff.FirstOrDefault()?.Path ?? "None";
        JsonSupport.WriteCreateNew(PathOf(ProbeConstants.FinalFile), new FinalArtifact(ProbeConstants.Schema,
            ProbeConstants.Version, DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId, classification,
            snapshotDiff, saveDiff, protectionDiff, first,
            "Normal Continue production NumReloads +1 is allowed; wall-clock/artifact sequence and snapshot frame ticks are excluded. This is reconstruction control, not Replay session isolation."));
        Seal(classification, first);
    }

    private static CombatSnapshot Normalize(CombatSnapshot value) => value with
    {
        CapturedUtc = "", ProcessId = 0,
        Readiness = value.Readiness with { FirstReadyFrame = 0, SnapshotFrame = 0 }
    };

    private static void ValidateBaselineScene(RunState state)
    {
        if (state.Players.Count != 1 || !state.GameMode.ToString().Equals("Standard", StringComparison.OrdinalIgnoreCase)
            || state.CurrentActIndex != 0 || CombatManager.Instance?.IsInProgress == true)
            throw new InvalidOperationException("expected Act 1 Standard map with no combat in progress");
    }
    private static void ValidateCombat()
    {
        var state = RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("run state unavailable");
        var combat = CombatManager.Instance?.DebugOnlyGetState();
        if (state.Players.Count != 1 || state.CurrentActIndex != 0 || state.CurrentMapCoord?.row != 1
            || combat?.RoundNumber != 1 || state.CurrentRoom is null
            || !state.CurrentRoom.RoomType.ToString().Contains("Monster", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("expected Act 1 first weak-monster, round 1");
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
        catch (Exception ex) { accountCloud = appCloud = true; failures.Add("Steam Cloud status unreadable: " + ex.GetType().Name); }
        if (version != ProbeConstants.GameVersion) failures.Add("game version mismatch: " + version);
        if (commit != ProbeConstants.GameCommit) failures.Add("game commit mismatch: " + commit);
        if (info.Length != ProbeConstants.GameDllLength || hash != ProbeConstants.GameDllSha256) failures.Add("sts2.dll identity mismatch");
        if (!ExperimentRules.ExactModSet(mods)) failures.Add("loaded mod set must be exactly SPIKE001A2R2; actual=" + string.Join(",", mods));
        if (!ExperimentRules.IsProfileOne(manager.CurrentProfileId, token, UserDataPathProvider.IsRunningModded)) failures.Add("active profile is not modded profile1");
        if (!ExperimentRules.IsCloudOff(accountCloud, appCloud)) failures.Add("Steam Cloud is enabled");
        var probe = typeof(ProbeController).Assembly.Location; var pi = new FileInfo(probe);
        return new(version, commit, info.Length, hash, ProbeConstants.Version, pi.Length, JsonSupport.Sha256File(probe), mods,
            manager.CurrentProfileId, token, UserDataPathProvider.IsRunningModded, accountCloud, appCloud,
            ExperimentRules.IsCloudOff(accountCloud, appCloud), failures);
    }

    private static void Seal(string classification, string reason)
    {
        if (!Terminal.TrySeal()) return;
        try { if (!Exists(ProbeConstants.TerminalFile)) JsonSupport.WriteCreateNew(PathOf(ProbeConstants.TerminalFile),
            new TerminalArtifact(ProbeConstants.Schema, ProbeConstants.Version, DateTimeOffset.UtcNow.ToString("O"), System.Environment.ProcessId, classification, reason)); }
        catch (Exception ex) { GD.PrintErr("[SPIKE-001A2-R2] terminal artifact write failed: " + ex); }
        _phase = ProbePhase.Terminal;
        ProbeUi.Show(classification + "\n" + reason + "\nImmediately stop all input and exit normally. Do not retry this probe.");
    }
    private static bool Exists(string name) => File.Exists(PathOf(name));
    private static string PathOf(string name) => Path.Combine(ProbeConstants.EvidenceRoot, name);
}
