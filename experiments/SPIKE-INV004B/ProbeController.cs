using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpikeInv004B;

internal static class ProbeController
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static WeakReference<NMainMenu>? _mainMenu;
    private static bool _completed;

    internal static void Initialize()
    {
        Directory.CreateDirectory(Path.Combine(ProbeConstants.EvidenceRoot, "runtime"));
        Log("Deep-copy diagnostic ready. It is main-menu-only and has one run entry.");
    }

    internal static void ObserveMainMenu(NMainMenu menu)
    {
        _mainMenu = new WeakReference<NMainMenu>(menu);
        ProbeUi.Attach(menu);
        Log("Main menu observed. Do not enter Continue or any game mode.");
    }

    internal static void StartFromUi()
    {
        if (_completed) { ProbeUi.ShowStatus("Diagnostic already completed in this process. Exit normally and inspect the evidence."); return; }
        MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely(RunAsync());
    }

    private static async Task RunAsync()
    {
        if (!await Gate.WaitAsync(0)) return;
        var started = DateTimeOffset.UtcNow;
        _completed = true;
        var blockers = new List<string>();
        var safetyFailures = new List<string>();
        var checkpoints = new List<string>();
        Exception? exception = null;
        string? firstFailurePoint = null;
        DeepCopyDiagnostic.CandidateResult? candidateA = null;
        DeepCopyDiagnostic.CandidateResult? candidateB = null;
        IReadOnlyList<FileIdentity> beforeFiles = Array.Empty<FileIdentity>();
        IReadOnlyList<FileIdentity> afterFiles = Array.Empty<FileIdentity>();
        DirectoryInventory? beforeInventory = null;
        DirectoryInventory? afterInventory = null;
        object? fixedIdentity = null;
        int? originalManagerIdentity = null;
        int? finalManagerIdentity = null;
        Dictionary<string, JsonSnapshot>? liveBefore = null;
        Dictionary<string, JsonSnapshot>? liveAfter = null;
        string? backupDirectory = null;
        IReadOnlyList<BackupEntry> backupEntries = Array.Empty<BackupEntry>();
        SaveManager? originalManager = null;
        string? profileRoot = null;
        string? accountModdedRoot = null;
        string? settingsPath = null;

        try
        {
            ProbeUi.ShowStatus("Diagnostic running. Do not click anything until completion is shown.");
            if (!TryGetMainMenu(out var menu)) blockers.Add("NMainMenu is not active");
            if (RunManager.Instance?.DebugOnlyGetState() is not null) blockers.Add("RunManager.State is not null");
            if (!IsContinueVisibleAndEnabled(menu)) blockers.Add("Continue is not visibly enabled");

            var environment = ReadEnvironment();
            blockers.AddRange(environment.Mismatches);
            fixedIdentity = environment;
            var original = SaveManager.Instance;
            originalManager = original;
            originalManagerIdentity = RuntimeHelpers.GetHashCode(original);
            if (original.CurrentProfileId != 1) blockers.Add($"active profile id expected 1, actual {original.CurrentProfileId}");
            var profileToken = UserDataPathProvider.GetProfileDir(original.CurrentProfileId).Replace('\\', '/');
            if (!UserDataPathProvider.IsRunningModded || !string.Equals(profileToken, "modded/profile1", StringComparison.OrdinalIgnoreCase))
                blockers.Add($"active profile token expected modded/profile1, actual {profileToken}");
            var saveTask = original.CurrentRunSaveTask;
            if (saveTask is not null) await saveTask.WaitAsync(TimeSpan.FromSeconds(10));
            if (saveTask?.IsFaulted == true || saveTask?.IsCanceled == true) blockers.Add("current save task did not complete successfully");

            profileRoot = Normalize(ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedBasePath(original.CurrentProfileId, null, null)));
            var accountStoreRoot = Normalize(ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath(null)));
            accountModdedRoot = Normalize(Directory.GetParent(profileRoot)?.FullName ?? throw new InvalidOperationException("Could not resolve modded account root."));
            settingsPath = Normalize(Path.Combine(accountStoreRoot, SettingsSaveManager.settingsSaveFileName));
            beforeInventory = ReadInventory(profileRoot);
            beforeFiles = CaptureProtected(profileRoot, accountModdedRoot, settingsPath, beforeInventory);
            if (beforeFiles.Count != 12 || beforeFiles.Any(x => !x.Exists)) blockers.Add($"expected 12 existing protected files; actual {beforeFiles.Count(x => x.Exists)}/{beforeFiles.Count}");
            if (blockers.Count != 0) { firstFailurePoint = "identity/precondition gate"; throw new DiagnosticBlockedException(string.Join("; ", blockers)); }

            backupDirectory = Path.Combine(ProbeConstants.EvidenceRoot, "protected-backup", started.ToString("yyyyMMddTHHmmss.fffffffZ"), "profile1");
            backupEntries = CopyProfileBackup(profileRoot, beforeInventory.Files, backupDirectory);
            if (backupEntries.Any(x => x.SourceSha256 != x.DestinationSha256)) throw new InvalidOperationException("Protected backup verification failed.");
            checkpoints.Add("identity-profile-main-menu-protection-gates-passed");

            var live = DeepCopyDiagnostic.CaptureLive(original);
            liveBefore = LiveSnapshots(live);
            candidateA = DeepCopyDiagnostic.CreateDiskCandidate(live, beforeFiles, accountStoreRoot);
            checkpoints.Add("candidate-a-disk-rehydrated-compared");
            candidateB = DeepCopyDiagnostic.CreateLiveCandidate(live);
            checkpoints.Add("candidate-b-live-seeded-compared-and-references-checked");
        }
        catch (Exception ex)
        {
            exception = ex;
            firstFailurePoint ??= ex is DiagnosticBlockedException ? "identity/precondition gate" : "candidate construction or comparison";
        }

        try
        {
            if (originalManager is not null)
            {
                if (!ReferenceEquals(SaveManager.Instance, originalManager)) safetyFailures.Add("global SaveManager identity changed");
                finalManagerIdentity = RuntimeHelpers.GetHashCode(SaveManager.Instance);
                liveAfter = LiveSnapshots(DeepCopyDiagnostic.CaptureLive(originalManager));
                if (liveBefore is not null)
                    foreach (var component in liveBefore.Keys)
                        if (liveBefore[component].CanonicalSha256 != liveAfter[component].CanonicalSha256)
                            safetyFailures.Add($"live {component} canonical hash changed");
            }
            if (RunManager.Instance?.DebugOnlyGetState() is not null) safetyFailures.Add("RunManager.State became non-null");
            if (!TryGetMainMenu(out var finalMenu)) safetyFailures.Add("main menu ceased to be active");
            if (!IsContinueVisibleAndEnabled(finalMenu)) safetyFailures.Add("Continue ceased to be visibly enabled");
            if (profileRoot is not null && accountModdedRoot is not null && settingsPath is not null && beforeInventory is not null)
            {
                afterInventory = ReadInventory(profileRoot);
                afterFiles = CaptureProtected(profileRoot, accountModdedRoot, settingsPath, afterInventory);
                safetyFailures.AddRange(CompareFiles(beforeFiles, afterFiles));
                safetyFailures.AddRange(CompareInventory(beforeInventory, afterInventory));
            }
            if (candidateA is not null && !candidateA.StoreContainmentPassed) safetyFailures.Add("candidate A store containment runtime assertion failed");
            if (candidateB is not null && !candidateB.StoreContainmentPassed) safetyFailures.Add("candidate B store containment runtime assertion failed");
            checkpoints.Add("live-and-protected-state-rechecked");
        }
        catch (Exception safetyException)
        {
            safetyFailures.Add("post-diagnostic safety capture failed: " + safetyException.Message);
        }

        var classification = Classify(blockers, safetyFailures, candidateA, candidateB, exception);
        var outputPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "deep-copy-diagnostic.json");
        JsonSupport.WriteAtomic(outputPath, new
        {
            schemaVersion = ProbeConstants.SchemaVersion,
            probeVersion = ProbeConstants.ProbeVersion,
            startedUtc = started.ToString("O"),
            finishedUtc = DateTimeOffset.UtcNow.ToString("O"),
            resultClassification = classification,
            firstFailurePoint,
            limitation = "Candidate construction/equivalence only; no SaveManager override, replay setup/events, run/scene transition, or progress mutation/save sentinel.",
            fixedIdentity,
            userConfirmationRequired = new[] { "Steam Cloud was disabled before launch", "visible active profile was modded slot 1", "only SPIKEINV004B was enabled" },
            blockers,
            safetyFailures,
            checkpoints,
            exception = exception?.ToString(),
            originalManagerIdentity,
            finalManagerIdentity,
            originalManagerUnchanged = originalManagerIdentity == finalManagerIdentity,
            liveBefore,
            liveAfter,
            candidateA = Project(candidateA),
            candidateB = Project(candidateB),
            beforeFiles,
            afterFiles,
            beforeInventory,
            afterInventory,
            backupDirectory,
            backupEntries,
        });
        ProbeUi.ShowStatus($"Diagnostic stopped: {classification}. Exit normally now. Evidence: {outputPath}");
        if (classification == "PASS_DIAGNOSED") Log($"Diagnostic PASS_DIAGNOSED. Evidence: {outputPath}");
        else Error($"Diagnostic {classification}. Evidence: {outputPath}");
        Gate.Release();
    }

    private static object? Project(DeepCopyDiagnostic.CandidateResult? candidate) => candidate is null ? null : new
    {
        candidate.Name,
        managerIdentity = RuntimeHelpers.GetHashCode(candidate.Manager),
        candidate.CurrentProfileId,
        candidate.ProfileLastProfileId,
        candidate.Components,
        candidate.StoreAudit,
        candidate.StoreContainmentPassed,
        candidate.StoreContainmentFailures,
        candidate.References,
    };

    private static string Classify(IReadOnlyList<string> blockers, IReadOnlyList<string> safety, DeepCopyDiagnostic.CandidateResult? a, DeepCopyDiagnostic.CandidateResult? b, Exception? ex)
    {
        if (safety.Count != 0) return "FAIL_SAFETY";
        if (blockers.Count != 0) return "BLOCKED_IDENTITY";
        if (ex is not null || a is null || b is null) return "PARTIAL_DIAGNOSED";
        var aHasLocatedDifference = a.Components.Any(x => !x.RawEqual || !x.CanonicalEqual);
        var bEquivalent = b.Components.All(x => x.CanonicalEqual);
        var refs = b.References;
        var bIndependent = refs.ManagerIndependent && refs.SettingsIndependent && refs.PrefsIndependent && refs.ProgressIndependent && refs.SharedMutableReferences.Count == 0;
        return aHasLocatedDifference && bEquivalent && bIndependent ? "PASS_DIAGNOSED" : "PARTIAL_DIAGNOSED";
    }

    private static Dictionary<string, JsonSnapshot> LiveSnapshots(DeepCopyDiagnostic.LiveState live)
        => new(StringComparer.Ordinal)
        {
            ["Settings"] = Snapshot(live.SettingsJson),
            ["Prefs"] = Snapshot(live.PrefsJson),
            ["Progress"] = Snapshot(live.ProgressJson),
        };

    private static JsonSnapshot Snapshot(string raw) { var canonical = JsonSupport.Canonicalize(raw); return new(JsonSupport.Sha256Text(raw), JsonSupport.Sha256Text(canonical), raw.Length, canonical.Length); }

    private static IReadOnlyList<FileIdentity> CaptureProtected(string profileRoot, string moddedRoot, string settingsPath, DirectoryInventory inventory)
    {
        var files = inventory.Files.Select(relative => ReadIdentity("active-profile", Path.Combine(profileRoot, relative), relative)).ToList();
        files.Add(ReadIdentity("account-exact", Path.Combine(moddedRoot, "profile.save"), "modded/profile.save"));
        files.Add(ReadIdentity("machine-settings-exact", settingsPath, "settings.save"));
        return files.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IReadOnlyList<BackupEntry> CopyProfileBackup(string root, IReadOnlyList<string> files, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);
        var entries = new List<BackupEntry>();
        foreach (var relative in files)
        {
            if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("..", StringComparer.Ordinal)) throw new InvalidOperationException("Unsafe backup path.");
            var source = Normalize(Path.Combine(root, relative));
            var destination = Normalize(Path.Combine(destinationRoot, relative));
            if (!IsStrictChild(source, root) || !IsStrictChild(destination, destinationRoot)) throw new InvalidOperationException("Backup path escaped its root.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, false);
            entries.Add(new BackupEntry(source, relative, destination, new FileInfo(source).Length, JsonSupport.Sha256File(source), JsonSupport.Sha256File(destination)));
        }
        return entries;
    }

    private static DirectoryInventory ReadInventory(string root)
    {
        var dirs = Directory.GetDirectories(root, "*", SearchOption.AllDirectories).Select(x => Path.GetRelativePath(root, x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(x => Path.GetRelativePath(root, x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        return new DirectoryInventory(root, dirs, files);
    }

    private static FileIdentity ReadIdentity(string scope, string path, string? relative)
    {
        path = Normalize(path);
        if (!File.Exists(path)) return new(scope, path, relative, false, null, null, null);
        var info = new FileInfo(path);
        return new(scope, path, relative, true, info.Length, JsonSupport.Sha256File(path), info.LastWriteTimeUtc.ToString("O"));
    }

    private static IEnumerable<string> CompareFiles(IReadOnlyList<FileIdentity> before, IReadOnlyList<FileIdentity> after)
    {
        var right = after.ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var left in before)
            if (!right.TryGetValue(left.Path, out var item) || left.Exists != item.Exists || left.Length != item.Length || left.Sha256 != item.Sha256 || left.LastWriteUtc != item.LastWriteUtc)
                yield return "protected file identity changed: " + left.Path;
    }

    private static IEnumerable<string> CompareInventory(DirectoryInventory before, DirectoryInventory after)
    {
        if (!before.Directories.SequenceEqual(after.Directories, StringComparer.OrdinalIgnoreCase)) yield return "protected directory inventory changed";
        if (!before.Files.SequenceEqual(after.Files, StringComparer.OrdinalIgnoreCase)) yield return "protected file inventory changed";
    }

    private static bool TryGetMainMenu(out NMainMenu? menu)
    {
        menu = null;
        if (_mainMenu is null || !_mainMenu.TryGetTarget(out var value) || !GodotObject.IsInstanceValid(value) || !value.IsInsideTree()) return false;
        menu = value; return true;
    }

    private static bool IsContinueVisibleAndEnabled(NMainMenu? menu)
    {
        var button = menu?.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/ContinueButton");
        if (button?.Visible != true) return false;
        var property = typeof(NClickableControl).GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? typeof(NClickableControl).GetProperty("Enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(button) as bool? ?? true;
    }

    private static EnvironmentEvidence ReadEnvironment()
    {
        var mismatches = new List<string>();
        var dll = Normalize(typeof(NGame).Assembly.Location);
        var dllHash = JsonSupport.Sha256File(dll);
        var gameDir = Directory.GetParent(Path.GetDirectoryName(dll)!)!.FullName;
        using var release = JsonDocument.Parse(File.ReadAllText(Path.Combine(gameDir, "release_info.json")));
        var version = release.RootElement.GetProperty("version").GetString() ?? "unknown";
        var commit = release.RootElement.GetProperty("commit").GetString() ?? "unknown";
        var branch = release.RootElement.GetProperty("branch").GetString() ?? "unknown";
        if (version != ProbeConstants.ExpectedGameVersion) mismatches.Add("game version mismatch");
        if (commit != ProbeConstants.ExpectedCommit) mismatches.Add("game commit mismatch");
        if (dllHash != ProbeConstants.ExpectedDllSha256) mismatches.Add("sts2.dll hash mismatch");
        var ids = ModManager.GetLoadedMods().Where(x => x.manifest is not null).Select(x => x.manifest!.id ?? "").OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (ids.Count != 1 || ids[0] != "SPIKEINV004B") mismatches.Add("enabled mod set must contain exactly SPIKEINV004B; actual: " + string.Join(", ", ids));
        var probe = Normalize(typeof(ProbeController).Assembly.Location);
        return new(version, commit, branch, dll, new FileInfo(dll).Length, dllHash, probe, JsonSupport.Sha256File(probe), ids, mismatches);
    }

    private sealed record EnvironmentEvidence(string GameVersion, string Commit, string Branch, string Sts2DllPath, long Sts2DllLength, string Sts2DllSha256, string ProbeAssemblyPath, string ProbeAssemblySha256, IReadOnlyList<string> LoadedModIds, IReadOnlyList<string> Mismatches);
    private sealed class DiagnosticBlockedException(string message) : Exception(message);
    private static bool IsStrictChild(string candidate, string root) => Normalize(candidate).StartsWith(Normalize(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static string Normalize(string path) => Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    private static void Log(string message) => GD.Print("[SPIKE-INV004B] " + message);
    private static void Error(string message) => GD.PrintErr("[SPIKE-INV004B] " + message);
}
