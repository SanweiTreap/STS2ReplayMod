using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpikeInv005A;

internal static class ProbeController
{
    private static readonly object Gate = new();
    private static readonly List<Milestone> Milestones = new();
    private static readonly List<string> SafetyFailures = new();
    private static readonly List<string> GateFailures = new();
    private static readonly List<string> InstrumentationFailures = new();
    private static readonly List<FileSystemWatcher> ProtectionWatchers = new();
    private static readonly DateTimeOffset StartedUtc = DateTimeOffset.UtcNow;
    private static long _sequence;
    private static SaveManager? _originalManager;
    private static int? _originalManagerIdentity;
    private static WeakReference<NMainMenu>? _mainMenu;
    private static EnvironmentIdentity? _environment;
    private static bool _menuObserved;
    private static bool _startupTerminalRecorded;
    private static bool _startupAwaitStarted;
    private static int _graphicsActive;
    private static int _graphicsEntries;
    private static int _graphicsNormalExits;
    private static bool _graphicsFaulted;
    private static long _lastGraphicsEntrySequence;
    private static GateSnapshot? _gateSnapshot;
    private static bool _verifyRequested;
    private static string? _classification;
    private static string? _firstFailurePoint;
    private static string? _terminalUtc;
    private static bool _protectionEventObserved;
    private static string? _protectionEvent;
    private static string? _profileRoot;
    private static string? _moddedRoot;
    private static string? _settingsPath;
    private static ComponentSet? _verifyComponents;
    private static ProtectionSnapshot? _verifyProtection;
    private static int? _verifyManagerIdentity;
    private static string? _verifyRunSaveTaskStatus;

    internal static long NextSequence() => Interlocked.Increment(ref _sequence);
    internal static ulong ProcessFrame() { try { return Engine.GetProcessFrames(); } catch { return 0; } }

    internal static void Initialize()
    {
        Directory.CreateDirectory(ProbeConstants.EvidenceRoot);
        _originalManager = SaveManager.Instance;
        _originalManagerIdentity = RuntimeHelpers.GetHashCode(_originalManager);
        WriterAudit.Start(_originalManager);
        AddMilestone("patch-active", new { harmonyOwner = "STS2ReplayMod.SPIKEINV005A.StartupBarrier", installedBeforeGraphicsEntry = Volatile.Read(ref _graphicsEntries) == 0 });
        TaskHelper.RunSafely(MonitorAsync());
        GD.Print("[SPIKE-INV005A] Read-only startup barrier probe active.");
    }

    internal static void ObserveMainMenu(NMainMenu menu)
    {
        _mainMenu = new WeakReference<NMainMenu>(menu);
        _menuObserved = true;
        AddMilestone("menu-ready", null);
        ProbeUi.Attach(menu);
        if (!_startupAwaitStarted)
        {
            _startupAwaitStarted = true;
            TaskHelper.RunSafely(AwaitStartupCompleteAsync());
        }
    }

    internal sealed record GraphicsCallState(ComponentFingerprint? SettingsBefore, long EntrySequence, int ManagerIdentity, string? InstrumentationError);

    internal static GraphicsCallState GraphicsEntry()
    {
        var manager = SaveManager.Instance;
        Interlocked.Increment(ref _graphicsActive);
        Interlocked.Increment(ref _graphicsEntries);
        ComponentFingerprint? settings = null;
        string? instrumentationError = null;
        try { settings = CaptureSettings(manager); }
        catch (Exception ex) { instrumentationError = ex.ToString(); }
        var state = new GraphicsCallState(settings, NextSequence(), RuntimeHelpers.GetHashCode(manager), instrumentationError);
        Interlocked.Exchange(ref _lastGraphicsEntrySequence, state.EntrySequence);
        AddMilestoneWithSequence(state.EntrySequence, "graphics-entry", new { settingsBefore = state.SettingsBefore, instrumentationError });
        if (instrumentationError is not null) InstrumentationFailuresAdd("could not fingerprint Settings at graphics entry", "graphics-entry-instrumentation");
        if (_gateSnapshot is not null) GateFailuresAdd("graphics callback entered after gate-open", "graphics-entry-after-gate");
        return state;
    }

    internal static void GraphicsNormalExit(GraphicsCallState state)
    {
        Interlocked.Increment(ref _graphicsNormalExits);
        ComponentFingerprint? after = null;
        string? instrumentationError = null;
        try { after = CaptureSettings(SaveManager.Instance); }
        catch (Exception ex) { instrumentationError = ex.ToString(); InstrumentationFailuresAdd("could not fingerprint Settings at graphics exit", "graphics-exit-instrumentation"); }
        AddMilestone("graphics-normal-exit", new
        {
            entrySequence = state.EntrySequence,
            managerIdentityAtEntry = state.ManagerIdentity,
            managerIdentityAtExit = RuntimeHelpers.GetHashCode(SaveManager.Instance),
            settingsBefore = state.SettingsBefore,
            settingsAfter = after,
            instrumentationError
        });
    }

    internal static void GraphicsFinalizer(GraphicsCallState? state, Exception? exception)
    {
        if (exception is not null)
        {
            _graphicsFaulted = true;
            AddMilestone("graphics-exception", new { entrySequence = state?.EntrySequence, exception = exception.ToString() });
        }
        Interlocked.Decrement(ref _graphicsActive);
    }

    internal static void WriterEntryObserved()
    {
        if (_gateSnapshot is not null) GateFailuresAdd("component/store writer operation began after gate-open", "writer-after-gate");
    }

    internal static void AsyncTaskTerminalObserved(string status)
    {
        if (status is "Faulted" or "Canceled") GateFailuresAdd("observed async store task ended " + status, "async-store-task-terminal");
    }

    internal static void ForbiddenOperationObserved(string operation) => SafetyFailuresAdd("forbidden override operation observed: " + operation, "forbidden-override");
    internal static void InstrumentationFailureObserved(string operation, Exception exception) => InstrumentationFailuresAdd(operation + ": " + exception, "instrumentation");

    internal static void VerifyFromUi()
    {
        if (_classification is not null || _gateSnapshot is null || _verifyRequested) return;
        _verifyRequested = true;
        VerifyNow();
    }

    private static async Task MonitorAsync()
    {
        while (_classification is null)
        {
            await NGame.Instance!.GetTree().Root.AwaitProcessFrame();
            CheckContinuousSafety();
            if (_classification is not null) break;
            if (_gateSnapshot is null) TryOpenGate();
            if (_classification is not null) break;
            var elapsed = DateTimeOffset.UtcNow - StartedUtc;
            if (elapsed.TotalSeconds >= ProbeConstants.MissingEventTimeoutSeconds)
            {
                var startup = NGame.Instance?.GameStartupComplete;
                if (Volatile.Read(ref _graphicsNormalExits) == 0 || startup?.IsCompleted != true)
                    Finish("STOP_MISSING_EVENT", "timeout waiting for production graphics normal exit and/or GameStartupComplete", "missing-event-timeout");
                else
                    Finish("FAIL_GATE", "explicit startup events completed but gate conditions did not become simultaneously true", "gate-timeout");
            }
        }
    }

    private static async Task AwaitStartupCompleteAsync()
    {
        var task = NGame.Instance?.GameStartupComplete;
        if (task is null)
        {
            InstrumentationFailuresAdd("NGame.GameStartupComplete was unavailable at menu-ready", "startup-task-unavailable");
            return;
        }
        try { await task; }
        catch { /* The original task state and exception are recorded below; probe code does not alter it. */ }
        var status = TaskStatusText(task);
        _startupTerminalRecorded = true;
        AddMilestone("game-startup-complete-task-terminal", new { status, exception = task.Exception?.ToString() });
        if (task.IsFaulted || task.IsCanceled) GateFailuresAdd("GameStartupComplete did not complete successfully: " + status, "startup-task-terminal");
    }

    private static void CheckContinuousSafety()
    {
        if (_originalManager is null) return;
        if (!ReferenceEquals(SaveManager.Instance, _originalManager)) SafetyFailuresAdd("original SaveManager identity changed", "manager-identity");
        if (RunManager.Instance?.DebugOnlyGetState() is not null) SafetyFailuresAdd("RunManager.State became non-null", "run-state");
        if (_protectionEventObserved) SafetyFailuresAdd("protected filesystem changed: " + _protectionEvent, "protected-filesystem-event");
        if (InstrumentationFailures.Count != 0) Finish("BLOCKED_INSTRUMENTATION", InstrumentationFailures[0], _firstFailurePoint ?? "instrumentation");
        if (_gateSnapshot is null && GateFailures.Count != 0 && _startupTerminalRecorded && Volatile.Read(ref _graphicsNormalExits) > 0)
            Finish("FAIL_GATE", GateFailures[0], _firstFailurePoint ?? "pre-gate-failure");
        if (_gateSnapshot is not null && GateFailures.Count != 0) Finish("FAIL_GATE", GateFailures[0], _firstFailurePoint ?? "gate-stability");
        if (SafetyFailures.Count != 0) Finish("FAIL_SAFETY", SafetyFailures[0], _firstFailurePoint ?? "continuous-safety");
    }

    private static void TryOpenGate()
    {
        if (!_menuObserved || _originalManager is null || NGame.Instance is null) return;
        if (_environment is null)
        {
            try
            {
                _environment = ReadEnvironment();
                AddMilestone("fixed-identity-checked", _environment);
                if (_environment.Mismatches.Count != 0) { Finish("BLOCKED_IDENTITY", string.Join("; ", _environment.Mismatches), "fixed-identity"); return; }
                ResolveProtectedPaths(_originalManager);
            }
            catch (Exception ex) { Finish("BLOCKED_IDENTITY", ex.Message, "fixed-identity-capture"); return; }
        }

        if (_graphicsFaulted) { Finish("FAIL_GATE", "a graphics callback threw", "graphics-exception"); return; }
        if (WriterAudit.HasAsyncFaultOrCancellation) { Finish("FAIL_GATE", "an observed async store task faulted or was canceled", "async-store-task"); return; }
        var currentRunTask = _originalManager.CurrentRunSaveTask;
        if (currentRunTask is { IsCompleted: true, IsCompletedSuccessfully: false }) { Finish("FAIL_GATE", "original CurrentRunSaveTask faulted or was canceled", "current-run-save-task"); return; }
        if (!BaseGateConditions(out _)) return;

        try
        {
            StartProtectionWatchers();
            var components = CaptureComponents(_originalManager);
            var protection = CaptureProtection();
            if (_protectionEventObserved) { Finish("FAIL_SAFETY", "protected filesystem changed while gate baseline was captured", "gate-baseline-protection"); return; }
            if (!BaseGateConditions(out _)) { DisposeProtectionWatchers(); return; }
            _gateSnapshot = new GateSnapshot(DateTimeOffset.UtcNow.ToString("O"), _originalManagerIdentity!.Value, CurrentRunSaveTaskStatus(_originalManager), components,
                protection, WriterAudit.LastWriterEntrySequence, Volatile.Read(ref _lastGraphicsEntrySequence));
            AddMilestone("gate-open", _gateSnapshot);
            ProbeUi.SetGateReady();
        }
        catch (Exception ex)
        {
            DisposeProtectionWatchers();
            Finish("BLOCKED_INSTRUMENTATION", ex.ToString(), "gate-baseline-capture");
        }
    }

    private static bool BaseGateConditions(out string reason)
    {
        reason = "";
        if (_originalManager is null || !ReferenceEquals(SaveManager.Instance, _originalManager)) { reason = "manager identity"; return false; }
        if (Volatile.Read(ref _graphicsEntries) < 1 || Volatile.Read(ref _graphicsNormalExits) < 1 || Volatile.Read(ref _graphicsActive) != 0) { reason = "graphics callback"; return false; }
        var startup = NGame.Instance?.GameStartupComplete;
        if (startup?.IsCompletedSuccessfully != true) { reason = "startup task"; return false; }
        if (RunManager.Instance?.DebugOnlyGetState() is not null) { reason = "run state"; return false; }
        var saveTask = _originalManager.CurrentRunSaveTask;
        if (saveTask is not null && !saveTask.IsCompletedSuccessfully) { reason = "current run save task"; return false; }
        if (WriterAudit.ActiveComponentWriters != 0 || WriterAudit.ActiveSyncStoreOperations != 0 || WriterAudit.HasPendingAsync || !WriterAudit.AllAsyncTerminalSuccessfully) { reason = "writer/task state"; return false; }
        try { _ = CaptureComponents(_originalManager); } catch { reason = "components not loaded"; return false; }
        return true;
    }

    private static void VerifyNow()
    {
        ProbeUi.ShowStatus("VERIFY running — do not operate the game.");
        AddMilestone("verify-clicked", null);
        CheckContinuousSafety();
        if (_classification is not null || _gateSnapshot is null || _originalManager is null) return;
        try
        {
            if (!BaseGateConditions(out var reason)) { Finish("FAIL_GATE", "gate condition no longer holds at Verify: " + reason, "verify-gate-condition"); return; }
            _verifyManagerIdentity = RuntimeHelpers.GetHashCode(SaveManager.Instance);
            _verifyRunSaveTaskStatus = CurrentRunSaveTaskStatus(_originalManager);
            _verifyComponents = CaptureComponents(_originalManager);
            _verifyProtection = CaptureProtection();
            var differences = CompareGateSnapshot(_gateSnapshot, _verifyManagerIdentity.Value, _verifyComponents, _verifyProtection);
            if (differences.Count != 0) { Finish(differences.Any(x => x.StartsWith("protected", StringComparison.Ordinal)) ? "FAIL_SAFETY" : "FAIL_GATE", differences[0], "verify-comparison"); return; }
            if (WriterAudit.LastWriterEntrySequence > _gateSnapshot.LastWriterEntrySequence || Volatile.Read(ref _lastGraphicsEntrySequence) > _gateSnapshot.LastGraphicsEntrySequence)
            { Finish("FAIL_GATE", "new writer or graphics entry was observed after gate-open", "verify-event-sequence"); return; }
            Finish("PASS_GATE", "all explicit gate conditions and stable Verify comparisons passed", null);
        }
        catch (Exception ex) { Finish("BLOCKED_INSTRUMENTATION", ex.ToString(), "verify-capture"); }
    }

    private static List<string> CompareGateSnapshot(GateSnapshot baseline, int managerIdentity, ComponentSet components, ProtectionSnapshot protection)
    {
        var differences = new List<string>();
        if (managerIdentity != baseline.ManagerIdentity) differences.Add("manager identity changed");
        foreach (var component in baseline.Components.Components.Keys)
            if (baseline.Components.Components[component] != components.Components[component]) differences.Add(component + " raw/canonical fingerprint changed");
        var beforeFiles = baseline.Protection.Files.ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
        var afterFiles = protection.Files.ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
        if (beforeFiles.Count != afterFiles.Count || beforeFiles.Any(x => !afterFiles.TryGetValue(x.Key, out var value) || value != x.Value)) differences.Add("protected file identity changed");
        if (!baseline.Protection.Profile1Inventory.Directories.SequenceEqual(protection.Profile1Inventory.Directories, StringComparer.OrdinalIgnoreCase)
            || !baseline.Protection.Profile1Inventory.Files.SequenceEqual(protection.Profile1Inventory.Files, StringComparer.OrdinalIgnoreCase)) differences.Add("protected profile1 inventory changed");
        return differences;
    }

    private static ComponentSet CaptureComponents(SaveManager manager)
    {
        var progress = manager.Progress.ToSerializable();
        progress.SchemaVersion = GetLatestProgressSchema(manager);
        var raw = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Settings"] = JsonSerializationUtility.ToJson(manager.SettingsSave),
            ["Prefs"] = JsonSerializationUtility.ToJson(manager.PrefsSave),
            ["Progress"] = JsonSerializationUtility.ToJson(progress)
        };
        return new ComponentSet(raw.ToDictionary(x => x.Key, x => Fingerprint(x.Value), StringComparer.Ordinal));
    }

    private static ComponentFingerprint Fingerprint(string raw)
    {
        var canonical = JsonSupport.Canonicalize(raw);
        return new ComponentFingerprint(JsonSupport.Sha256Text(raw), JsonSupport.Sha256Text(canonical), raw.Length, canonical.Length);
    }

    private static ComponentFingerprint CaptureSettings(SaveManager manager) => Fingerprint(JsonSerializationUtility.ToJson(manager.SettingsSave));

    private static int GetLatestProgressSchema(SaveManager manager)
    {
        var field = typeof(SaveManager).GetField("_migrationManager", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException("_migrationManager");
        var migration = field.GetValue(manager) ?? throw new InvalidOperationException("MigrationManager is null");
        var method = migration.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(x => x.Name == "GetLatestVersion" && x.IsGenericMethodDefinition && x.GetParameters().Length == 0);
        return (int)method.MakeGenericMethod(typeof(SerializableProgress)).Invoke(migration, null)!;
    }

    private static void ResolveProtectedPaths(SaveManager manager)
    {
        if (manager.CurrentProfileId != 1) throw new InvalidOperationException($"active profile expected 1, actual {manager.CurrentProfileId}");
        var profileToken = UserDataPathProvider.GetProfileDir(1).Replace('\\', '/');
        if (!UserDataPathProvider.IsRunningModded || !string.Equals(profileToken, "modded/profile1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("active profile token expected modded/profile1, actual " + profileToken);
        _profileRoot = Normalize(ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedBasePath(1, null, null)));
        _moddedRoot = Normalize(Directory.GetParent(_profileRoot)?.FullName ?? throw new InvalidOperationException("Could not resolve modded root"));
        var accountRoot = Normalize(ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath(null)));
        _settingsPath = Normalize(Path.Combine(accountRoot, SettingsSaveManager.settingsSaveFileName));
    }

    private static ProtectionSnapshot CaptureProtection()
    {
        if (_profileRoot is null || _moddedRoot is null || _settingsPath is null) throw new InvalidOperationException("Protected paths are unresolved");
        var directories = Directory.GetDirectories(_profileRoot, "*", SearchOption.AllDirectories).Select(x => Path.GetRelativePath(_profileRoot, x).Replace('\\', '/')).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        var files = Directory.GetFiles(_profileRoot, "*", SearchOption.AllDirectories).Select(x => Path.GetRelativePath(_profileRoot, x).Replace('\\', '/')).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        var identities = files.Select(relative => ReadIdentity("active-profile", Path.Combine(_profileRoot, relative), "profile1/" + relative)).ToList();
        identities.Add(ReadIdentity("account-exact", Path.Combine(_moddedRoot, "profile.save"), "modded/profile.save"));
        identities.Add(ReadIdentity("machine-settings-exact", _settingsPath, "settings.save"));
        return new ProtectionSnapshot(identities.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList(), new DirectoryInventory("modded/profile1", directories, files));
    }

    private static FileIdentity ReadIdentity(string scope, string physicalPath, string logicalPath)
    {
        if (!File.Exists(physicalPath)) return new FileIdentity(scope, logicalPath, logicalPath, false, null, null, null);
        var info = new FileInfo(physicalPath);
        return new FileIdentity(scope, logicalPath, logicalPath, true, info.Length, JsonSupport.Sha256File(physicalPath), info.LastWriteTimeUtc.ToString("O"));
    }

    private static void StartProtectionWatchers()
    {
        if (ProtectionWatchers.Count != 0 || _profileRoot is null || _moddedRoot is null || _settingsPath is null) return;
        AddWatcher(_profileRoot, "*", true, "profile1");
        AddWatcher(_moddedRoot, "profile.save*", false, "modded/profile.save");
        AddWatcher(Path.GetDirectoryName(_settingsPath)!, "settings.save*", false, "settings.save");
    }

    private static void AddWatcher(string path, string filter, bool recursive, string scope)
    {
        var watcher = new FileSystemWatcher(path, filter) { IncludeSubdirectories = recursive, NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime };
        FileSystemEventHandler changed = (_, e) => ProtectionChanged(scope, e.ChangeType, e.Name);
        RenamedEventHandler renamed = (_, e) => ProtectionChanged(scope, WatcherChangeTypes.Renamed, e.OldName + " -> " + e.Name);
        watcher.Changed += changed; watcher.Created += changed; watcher.Deleted += changed; watcher.Renamed += renamed;
        watcher.EnableRaisingEvents = true;
        ProtectionWatchers.Add(watcher);
    }

    private static void ProtectionChanged(string scope, WatcherChangeTypes kind, string? name)
    {
        _protectionEvent = scope + " " + kind + " " + (name ?? "<unknown>");
        _protectionEventObserved = true;
        _firstFailurePoint ??= "protected-filesystem-event";
    }

    private static void DisposeProtectionWatchers()
    {
        foreach (var watcher in ProtectionWatchers) watcher.Dispose();
        ProtectionWatchers.Clear();
    }

    private static EnvironmentIdentity ReadEnvironment()
    {
        var mismatches = new List<string>();
        var dll = Normalize(typeof(NGame).Assembly.Location);
        var info = new FileInfo(dll);
        var dllHash = JsonSupport.Sha256File(dll);
        var gameDir = Directory.GetParent(Path.GetDirectoryName(dll)!)!.FullName;
        using var release = JsonDocument.Parse(File.ReadAllText(Path.Combine(gameDir, "release_info.json")));
        var version = release.RootElement.GetProperty("version").GetString() ?? "unknown";
        var commit = release.RootElement.GetProperty("commit").GetString() ?? "unknown";
        var branch = release.RootElement.GetProperty("branch").GetString() ?? "unknown";
        if (version != ProbeConstants.ExpectedGameVersion) mismatches.Add("game version mismatch");
        if (commit != ProbeConstants.ExpectedCommit) mismatches.Add("game commit mismatch");
        if (info.Length != ProbeConstants.ExpectedDllLength) mismatches.Add("sts2.dll length mismatch");
        if (dllHash != ProbeConstants.ExpectedDllSha256) mismatches.Add("sts2.dll hash mismatch");
        var ids = ModManager.GetLoadedMods().Where(x => x.manifest is not null).Select(x => x.manifest!.id ?? "").OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (ids.Count != 1 || ids[0] != "SPIKEINV005A") mismatches.Add("enabled mod set must contain exactly SPIKEINV005A; actual: " + string.Join(", ", ids));
        var probe = Normalize(typeof(ProbeController).Assembly.Location);
        return new EnvironmentIdentity(version, commit, branch, dll, info.Length, dllHash, probe, JsonSupport.Sha256File(probe), ids, mismatches);
    }

    private static void Finish(string classification, string detail, string? firstFailurePoint)
    {
        lock (Gate)
        {
            if (_classification is not null) return;
            _classification = classification;
            _firstFailurePoint ??= firstFailurePoint;
            _terminalUtc = DateTimeOffset.UtcNow.ToString("O");
        }
        AddMilestone("terminal", new { classification, detail, firstFailurePoint = _firstFailurePoint });
        DisposeProtectionWatchers();
        WriterAudit.Stop();
        WriteArtifact(detail);
        ProbeUi.SetTerminal(classification, ProbeConstants.EvidenceRoot + @"\runtime-artifact.json");
    }

    private static void WriteArtifact(string detail)
    {
        JsonSupport.WriteAtomic(Path.Combine(ProbeConstants.EvidenceRoot, "runtime-artifact.json"), new
        {
            schemaVersion = ProbeConstants.SchemaVersion,
            probeVersion = ProbeConstants.ProbeVersion,
            startedUtc = StartedUtc.ToString("O"),
            terminalUtc = _terminalUtc,
            resultClassification = _classification,
            firstFailurePoint = _firstFailurePoint,
            terminalDetail = detail,
            scopeLimitation = "Startup timing/barrier only. No SaveManager override, candidate manager, run setup/load/launch, replay access, progress mutation, or active save call.",
            externalConfirmationsRequired = new[] { "public-beta v0.111.0", "Steam Cloud disabled before launch", "modded profile 1", "only SPIKEINV005A enabled" },
            fixedIdentity = _environment,
            patchActiveBeforeFirstGraphicsEntry = Milestones.FirstOrDefault(x => x.Name == "patch-active")?.Sequence < Milestones.FirstOrDefault(x => x.Name == "graphics-entry")?.Sequence,
            originalManagerIdentity = _originalManagerIdentity,
            finalManagerIdentity = _originalManager is null ? (int?)null : RuntimeHelpers.GetHashCode(SaveManager.Instance),
            graphics = new { entries = _graphicsEntries, normalExits = _graphicsNormalExits, active = _graphicsActive, faulted = _graphicsFaulted },
            startupTaskStatus = NGame.Instance is null ? "NGame-null" : TaskStatusText(NGame.Instance.GameStartupComplete),
            currentRunSaveTaskStatus = _originalManager is null ? "manager-null" : CurrentRunSaveTaskStatus(_originalManager),
            writerState = new { activeComponentWriters = WriterAudit.ActiveComponentWriters, activeSyncStoreOperations = WriterAudit.ActiveSyncStoreOperations, asyncTasks = WriterAudit.AsyncTaskSnapshot() },
            milestones = Milestones.OrderBy(x => x.Sequence).ToList(),
            writerAudit = WriterAudit.Snapshot().OrderBy(x => x.Sequence).ToList(),
            gateSnapshot = _gateSnapshot,
            verify = new { requested = _verifyRequested, managerIdentity = _verifyManagerIdentity, runSaveTaskStatus = _verifyRunSaveTaskStatus, components = _verifyComponents, protection = _verifyProtection },
            protectionWatcherFirstEvent = _protectionEvent,
            safetyFailures = SafetyFailures.ToList(),
            gateFailures = GateFailures.ToList(),
            instrumentationFailures = InstrumentationFailures.ToList()
        });
    }

    private static void SafetyFailuresAdd(string value, string point)
    {
        lock (Gate) if (!SafetyFailures.Contains(value, StringComparer.Ordinal)) SafetyFailures.Add(value);
        _firstFailurePoint ??= point;
    }

    private static void GateFailuresAdd(string value, string point)
    {
        lock (Gate) if (!GateFailures.Contains(value, StringComparer.Ordinal)) GateFailures.Add(value);
        _firstFailurePoint ??= point;
    }

    private static void InstrumentationFailuresAdd(string value, string point)
    {
        lock (Gate) if (!InstrumentationFailures.Contains(value, StringComparer.Ordinal)) InstrumentationFailures.Add(value);
        _firstFailurePoint ??= point;
    }

    private static void AddMilestone(string name, object? detail) => AddMilestoneWithSequence(NextSequence(), name, detail);
    private static void AddMilestoneWithSequence(long sequence, string name, object? detail)
    {
        var manager = TryManager();
        lock (Gate) Milestones.Add(new Milestone(sequence, name, DateTimeOffset.UtcNow.ToString("O"), ProcessFrame(), System.Environment.CurrentManagedThreadId,
            manager is null ? null : RuntimeHelpers.GetHashCode(manager), manager is null ? null : CurrentRunSaveTaskStatus(manager), detail));
    }

    private static SaveManager? TryManager() { try { return SaveManager.Instance; } catch { return null; } }
    private static string CurrentRunSaveTaskStatus(SaveManager manager) => manager.CurrentRunSaveTask is null ? "null" : TaskStatusText(manager.CurrentRunSaveTask);
    private static string TaskStatusText(Task task) => task.IsCanceled ? "Canceled" : task.IsFaulted ? "Faulted" : task.IsCompletedSuccessfully ? "RanToCompletion" : task.Status.ToString();
    private static string Normalize(string path) => Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
}
