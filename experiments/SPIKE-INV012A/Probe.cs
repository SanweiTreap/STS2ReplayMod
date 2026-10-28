using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using SpikeInv012A.Core;

namespace SpikeInv012A;

[ModInitializer(nameof(Initialize))]
public static class Probe
{
    private const string Owner = "STS2ReplayMod.SPIKEINV012A.Phase0";
    private const string ExpectedHash = "0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9";
    private static DiagnosticCore? _core;
    private static System.Threading.Timer? _timer;
    private static bool _gatePassed, _armed, _precheckBlocked;
    private static long _frame;
    private static int _precheckPassLogged, _captureLogged, _terminalLogged;
    public static void Initialize()
    {
        // Fail closed before installing any patch, without opening player data.
        Harmony? owner = null;
        try
        {
            var dll = typeof(NGame).Assembly.Location;
            var info = new FileInfo(dll);
            if (info.Length != 9757184 || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll))) != ExpectedHash) return;
            var releasePath = Path.Combine(Directory.GetParent(Path.GetDirectoryName(dll)!)!.FullName, "release_info.json");
            using var release = JsonDocument.Parse(File.ReadAllText(releasePath));
            if (release.RootElement.GetProperty("version").GetString() != "v0.111.0" ||
                release.RootElement.GetProperty("commit").GetString() != "41cef1ea" ||
                typeof(Harmony).Assembly.GetName().Version?.ToString() != "2.4.2.0") return;
            owner = new Harmony(Owner);
            owner.PatchAll(typeof(Probe).Assembly);
        }
        catch
        {
            // PatchAll can fail after installing earlier classes. Remove only this owner.
            try { owner?.UnpatchAll(Owner); } catch { }
        }
    }
    internal static void Safe(string site, Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            try { _core?.ObserverFailed(ex.GetType().Name, site); } catch { }
        }
    }
    internal static void MainMenuReady()
    {
        if (_core is not null || _precheckBlocked) return;
        _core = PrebusinessGate.Open(() =>
        {
            var ids = ModManager.GetLoadedMods().Select(x => x.manifest?.id).ToArray();
            var manager = SaveManager.Instance;
            return PrebusinessGate.ExactIdentity(ids, manager is not null,
                manager?.CurrentProfileId, UserDataPathProvider.IsRunningModded);
        }, () =>
        {
            var attempt = Guid.NewGuid().ToString("N");
            var process = Guid.NewGuid().ToString("N");
            var boot = Guid.NewGuid().ToString("N");
            var root = ProjectSettings.GlobalizePath("user://");
            return new DiagnosticCore(new StopwatchClock(), new EvidenceSink(root, attempt), attempt, process, boot);
        }, marker => GD.Print(marker));
        if (_core is null) { _precheckBlocked = true; return; }
        _core.Precheck(true);
        if (_core.Stage == Stage.Terminal) { AnnounceTerminal(); return; }
        if (!PrebusinessGate.RequireTickerOwner(_core, () =>
        {
            var game = NGame.Instance;
            if (game is null || !GodotObject.IsInstanceValid(game) || !game.IsInsideTree()) return false;
            var tree = game.GetTree();
            if (tree is null || !GodotObject.IsInstanceValid(tree)) return false;
            // This plain .NET mod assembly has no Godot script source generator. A custom
            // Node subclass can be added, but its _Ready/_Process overrides never run.
            tree.ProcessFrame += OnProcessFrame;
            _core.TickerAttached(true, true);
            return true;
        }, AnnounceTerminal)) return;
        _gatePassed = true;
        _timer = new System.Threading.Timer(_ => Safe("deadline.timer", () => { _core?.Poll(); AnnounceTerminal(); }), null, 100, 100);
    }
    internal static void Arm()
    {
        if (!_gatePassed || _armed || _core?.Stage != Stage.ManualReady) return;
        Readiness.CaptureTarget(); // live in-memory comparison only; no target values enter evidence
        _core.Arm();
        if (_core.Stage != Stage.ManualReady) { AnnounceTerminal(); return; }
        _armed = true; GD.Print("SPIKEINV012A READY_A_NODE");
    }
    internal static void SaveEntry(AbstractRoom? room, bool saveProgress)
    {
        if (!_armed) return;
        _core?.SaveEntered(room is null && Readiness.TargetMatchesCurrentMap(), saveProgress);
    }
    internal static void SaveReturn(Task task)
    {
        if (!_armed || _core?.Stage != Stage.ProductionSave) return;
        // The production Task reference is neither replaced nor consumed. No exception payload is read.
        _ = task.ContinueWith(t => Safe("save.task", () =>
        {
            _core?.SaveCompleted(t.IsCompletedSuccessfully, t.IsCanceled);
            if (t.IsCompletedSuccessfully) _core?.SealCheckpointMetadata();
        }), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }
    private static void OnProcessFrame() => Safe("ticker.tick", Tick);
    internal static void Tick()
    {
        if (!_gatePassed || _core is null) return;
        var fields = _core.Stage == Stage.Capture ? Readiness.Read() : Array.Empty<FieldSample>();
        bool recording = false;
        try { recording = RunManager.Instance?.CombatReplayWriter?.IsRecordingReplay == true; }
        catch (Exception ex) { _core.ObserverFailed(ex.GetType().Name, "recording-flag"); return; }
        _core.Tick(Interlocked.Increment(ref _frame), fields, recording);
        if (PrebusinessGate.ReadyToAnnounce(_core) && Interlocked.Exchange(ref _precheckPassLogged, 1) == 0)
            GD.Print("SPIKEINV012A PRECHECK_PASS");
        if (_core.Stage == Stage.AwaitExit && Interlocked.Exchange(ref _captureLogged, 1) == 0)
            GD.Print("SPIKEINV012A CAPTURE_READY_STOP_INPUT");
        AnnounceTerminal();
    }
    internal static void Writer(string name, bool entry, bool? flag = null) => _core?.Writer(name, entry, flag);
    private static void AnnounceTerminal()
    {
        if (_core?.Stage == Stage.Terminal && Interlocked.Exchange(ref _terminalLogged, 1) == 0)
            GD.Print("SPIKEINV012A PROBE_TERMINAL_STOP_INPUT");
    }
}
