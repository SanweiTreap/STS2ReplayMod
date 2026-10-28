using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace SpikeInv004A;

internal static class ProbeController
{
    private static readonly SemaphoreSlim OperationLock = new(1, 1);
    private static WeakReference<NMainMenu>? _mainMenu;
    internal static void Initialize()
    {
        Directory.CreateDirectory(Path.Combine(ProbeConstants.EvidenceRoot, "runtime"));
        Log("Direct-write sentinel ready. F8=preflight, F9=native fixture validation, F10=status. Runtime stays locked until phase-control.json says DIRECT_WRITE.");
    }

    internal static void ObserveMainMenu(NMainMenu menu)
    {
        _mainMenu = new WeakReference<NMainMenu>(menu);
        ProbeUi.Attach(menu);
        Log("Main menu observed. Confirm Steam Cloud is OFF and visible profile is modded slot 1 before pressing F8.");
    }

    internal static void PrintStatus()
    {
        var menuPresent = TryGetMainMenu(out _);
        var stateNull = RunManager.Instance?.DebugOnlyGetState() is null;
        Log($"status phase={ReplayTransaction.ReadControlPhase()} mainMenu={menuPresent} stateNull={stateNull} transaction={ReplayTransaction.Active} busy={OperationLock.CurrentCount == 0} evidence={ProbeConstants.EvidenceRoot}");
    }

    internal static void RequestFailClosed(string reason) => ReplayTransaction.RequestFailClosed(reason);
    internal static void ProbeLog(string message) => Log(message);
    internal static void ProbeError(string message) => Error(message);

    internal static async Task RunPreflightAsync()
    {
        if (!await OperationLock.WaitAsync(0))
        {
            Error("Preflight ignored because another probe operation is running.");
            return;
        }

        var started = DateTimeOffset.UtcNow;
        try
        {
            Log("Phase 0 preflight started. It will not install a SaveManager override or start a run.");
            var blockers = new List<string>();
            var environment = ReadEnvironment();
            blockers.AddRange(environment.Mismatches);

            var manager = SaveManager.Instance;
            var managerIdentity = DescribeManager(manager);
            var activeProfileId = manager.CurrentProfileId;
            var profileToken = UserDataPathProvider.GetProfileDir(activeProfileId);
            var profileRoot = Normalize(ProjectSettings.GlobalizePath(
                UserDataPathProvider.GetProfileScopedBasePath(activeProfileId, null, null)));
            var accountRoot = Normalize(Directory.GetParent(profileRoot)?.FullName
                ?? throw new InvalidOperationException("Could not resolve account root from active profile root."));
            var settingsPath = Normalize(ProjectSettings.GlobalizePath(
                UserDataPathProvider.GetAccountScopedBasePath(null).PathJoin(SettingsSaveManager.settingsSaveFileName)));
            var currentRunPath = Normalize(Path.Combine(profileRoot, "saves", RunSaveManager.runSaveFileName));

            if (activeProfileId != 1)
                blockers.Add($"active profile id must be 1; actual {activeProfileId}");
            if (!UserDataPathProvider.IsRunningModded)
                blockers.Add("game is not using the modded save tree");
            if (!string.Equals(profileToken.Replace('\\', '/'), "modded/profile1", StringComparison.OrdinalIgnoreCase))
                blockers.Add($"active profile token must be modded/profile1; actual {profileToken}");

            if (!IsStrictChild(profileRoot, accountRoot))
                blockers.Add("active profile root is not a unique child of the resolved account root");
            if (!Directory.Exists(profileRoot))
                blockers.Add("active profile directory does not exist");

            var stateIsNull = RunManager.Instance?.DebugOnlyGetState() is null;
            if (!stateIsNull)
                blockers.Add("RunManager.State is not null");

            var mainMenuObserved = TryGetMainMenu(out var mainMenu);
            if (!mainMenuObserved)
                blockers.Add("NMainMenu is not active");

            var continueButton = mainMenuObserved
                ? mainMenu!.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/ContinueButton")
                : null;
            var continueVisible = continueButton?.Visible == true;
            var continueEnabled = continueButton is not null && ReadEnabled(continueButton);
            if (!continueVisible || !continueEnabled)
                blockers.Add("Continue is not visibly enabled on the active main menu");

            var taskObservation = await ObserveCurrentSaveTask(manager);
            if (!taskObservation.IsCompleted || taskObservation.IsFaulted || taskObservation.IsCanceled)
                blockers.Add("current save task did not complete successfully");

            ContinueSummary? continueSummary = null;
            if (manager.HasRunSave && mainMenuObserved)
            {
                var read = ReadCachedContinue(mainMenu!);
                if (!read.Success || read.SaveData is null)
                    blockers.Add($"cached Continue could not be parsed: status={read.Status}, error={read.ErrorMessage}");
                else
                    continueSummary = SummarizeContinue(read.SaveData);
            }
            else
            {
                blockers.Add("active profile has no Continue isolation sentinel");
            }

            var profileInventory = ReadInventory(profileRoot, recursive: true);
            var accountInventory = ReadInventory(accountRoot, recursive: false);
            if (profileInventory.Files.Count == 0)
                blockers.Add("active profile inventory is empty");

            var protectedFiles = new List<FileIdentity>();
            protectedFiles.AddRange(profileInventory.Files.Select(relative =>
                ReadFileIdentity("active-profile", Path.Combine(profileRoot, relative), relative)));
            protectedFiles.Add(ReadFileIdentity("account-exact", Path.Combine(accountRoot, "profile.save"), "profile.save"));
            protectedFiles.Add(ReadFileIdentity("machine-settings-exact", settingsPath, null));

            string? backupDirectory = null;
            IReadOnlyList<BackupEntry> backupEntries = Array.Empty<BackupEntry>();
            if (blockers.Count == 0)
            {
                backupDirectory = Path.Combine(
                    ProbeConstants.EvidenceRoot,
                    "protected-backup",
                    started.ToString("yyyyMMddTHHmmss.fffffffZ"),
                    Path.GetFileName(profileRoot));
                backupEntries = CopyProfileBackup(profileRoot, profileInventory.Files, backupDirectory);
                if (backupEntries.Any(x => !string.Equals(x.SourceSha256, x.DestinationSha256, StringComparison.Ordinal)))
                    blockers.Add("protected backup verification failed");
            }

            var evidence = new PreflightEvidence(
                ProbeConstants.SchemaVersion,
                ProbeConstants.ProbeVersion,
                started.ToString("O"),
                DateTimeOffset.UtcNow.ToString("O"),
                System.Environment.ProcessId,
                blockers.Count == 0 ? "AWAITING_USER_CONFIRMATION" : "BLOCKED",
                blockers,
                new[]
                {
                    "Confirm the visible active profile was modded profile slot 1 when F8 was pressed.",
                    "Confirm Steam Cloud was disabled before this game session.",
                    "Confirm Continue was visibly present and matched the intended active-profile isolation sentinel.",
                    "Confirm profile1 remained the dedicated fully-unlocked test profile."
                },
                environment,
                activeProfileId,
                profileToken,
                profileRoot,
                accountRoot,
                settingsPath,
                currentRunPath,
                UserDataPathProvider.IsRunningModded,
                stateIsNull,
                mainMenuObserved,
                continueVisible,
                continueEnabled,
                continueSummary,
                managerIdentity,
                taskObservation,
                protectedFiles.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList(),
                profileInventory,
                accountInventory,
                backupDirectory,
                backupEntries,
                "Backup contains only files beneath the resolved active profile root and may contain protected test-run data. Exact account-level profile.save and machine-level settings.save identities are recorded but not copied because they are not exclusive to one profile. Playback remains locked until this preflight and native fixture validation pass for the installed artifact." );

            var outputPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "preflight.json");
            JsonSupport.WriteAtomic(outputPath, evidence);
            if (blockers.Count == 0)
                Log($"Preflight evidence captured; runtime remains locked pending user confirmations. Evidence: {outputPath}");
            else
                Error($"Preflight BLOCKED: {string.Join("; ", blockers)}. Evidence: {outputPath}");
        }
        catch (Exception ex)
        {
            var outputPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "preflight-exception.json");
            JsonSupport.WriteAtomic(outputPath, new
            {
                schemaVersion = ProbeConstants.SchemaVersion,
                probeVersion = ProbeConstants.ProbeVersion,
                startedUtc = started.ToString("O"),
                finishedUtc = DateTimeOffset.UtcNow.ToString("O"),
                outcome = "BLOCKED",
                exceptionType = ex.GetType().FullName,
                ex.Message,
                ex.StackTrace,
            });
            Error($"Preflight exception; runtime remains locked: {ex}");
        }
        finally
        {
            OperationLock.Release();
        }
    }

    internal static async Task ValidateNativeFixtureAsync()
    {
        if (!await OperationLock.WaitAsync(0))
        {
            Error("Fixture validation ignored because another probe operation is running.");
            return;
        }

        try
        {
            var preflightPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "preflight.json");
            using var preflight = JsonDocument.Parse(File.ReadAllText(preflightPath));
            var root = preflight.RootElement;
            if (root.GetProperty("blockingReasons").GetArrayLength() != 0)
                throw new InvalidOperationException("Phase 0 contains blocking reasons.");
            if (RunManager.Instance?.DebugOnlyGetState() is not null)
                throw new InvalidOperationException("RunManager.State must remain null during fixture validation.");
            if (!TryGetMainMenu(out _))
                throw new InvalidOperationException("Native fixture validation is allowed only on the active main menu.");

            var fixturePath = Normalize(ProbeConstants.ExpectedFixturePath);
            var bytes = File.ReadAllBytes(fixturePath);
            var actualHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            if (!string.Equals(actualHash, ProbeConstants.ExpectedFixtureSha256, StringComparison.Ordinal)
                || bytes.Length != ProbeConstants.ExpectedFixtureLength)
                throw new InvalidDataException($"Fixed fixture identity mismatch: length={bytes.Length}, sha256={actualHash}");

            var reader = new PacketReader();
            reader.Reset(bytes);
            var replay = reader.Read<CombatReplay>();
            var run = replay.serializableRun ?? throw new InvalidDataException("CombatReplay.serializableRun is null.");
            if (!string.Equals(replay.version, ProbeConstants.ExpectedGameVersion, StringComparison.Ordinal))
                throw new InvalidDataException($"Replay version mismatch: {replay.version}");
            if (!string.Equals(replay.gitCommit, ProbeConstants.ExpectedCommit, StringComparison.Ordinal))
                throw new InvalidDataException($"Replay commit mismatch: {replay.gitCommit}");

            var latest = run.VisitedMapCoords.Count > 0 ? run.VisitedMapCoords[^1] : (MegaCrit.Sts2.Core.Map.MapCoord?)null;
            var output = new
            {
                schemaVersion = ProbeConstants.SchemaVersion,
                probeVersion = ProbeConstants.ProbeVersion,
                validatedUtc = DateTimeOffset.UtcNow.ToString("O"),
                outcome = "VALID_NATIVE_FIXTURE",
                fixture = new
                {
                    path = fixturePath,
                    length = bytes.Length,
                    sha256 = actualHash,
                    replay.version,
                    replay.gitCommit,
                    replay.modelIdHash,
                    bytesConsumed = (reader.BitPosition + 7) / 8,
                    trailingBytes = bytes.Length - ((reader.BitPosition + 7) / 8),
                },
                run = new
                {
                    run.SchemaVersion,
                    run.CurrentActIndex,
                    run.FloorReached,
                    latestMapRow = latest?.row,
                    latestMapCol = latest?.col,
                    seed = run.SerializableRng.Seed,
                    run.NumReloads,
                    gameMode = run.GameMode.ToString(),
                    run.Ascension,
                    playerCount = run.Players.Count,
                },
                runManagerStateIsNull = RunManager.Instance?.DebugOnlyGetState() is null,
            };
            var outputPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "fixture-validation.json");
            JsonSupport.WriteAtomic(outputPath, output);
            Log($"Native fixture validation succeeded. Evidence: {outputPath}");
        }
        catch (Exception ex)
        {
            var outputPath = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "fixture-validation-exception.json");
            JsonSupport.WriteAtomic(outputPath, new
            {
                schemaVersion = ProbeConstants.SchemaVersion,
                probeVersion = ProbeConstants.ProbeVersion,
                validatedUtc = DateTimeOffset.UtcNow.ToString("O"),
                outcome = "BLOCKED_FIXTURE",
                exceptionType = ex.GetType().FullName,
                ex.Message,
                ex.StackTrace,
            });
            Error($"Native fixture validation failed; playback remains locked: {ex}");
        }
        finally
        {
            OperationLock.Release();
        }
    }

    internal static JsonElement ReadPreflightRoot()
    {
        var path = Path.Combine(ProbeConstants.EvidenceRoot, "runtime", "preflight.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    internal static bool IsContinueVisibleAndEnabled()
    {
        if (!TryGetMainMenu(out var menu)) return false;
        var button = menu!.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/ContinueButton");
        return button?.Visible == true && ReadEnabled(button);
    }

    internal static IReadOnlyList<FileIdentity> CaptureProtectedFiles(JsonElement preflight)
    {
        return preflight.GetProperty("protectedFiles").EnumerateArray()
            .Select(entry => ReadFileIdentity(
                entry.GetProperty("scope").GetString() ?? "unknown",
                entry.GetProperty("path").GetString() ?? throw new InvalidDataException("protected file path missing"),
                entry.TryGetProperty("relativePath", out var relative) && relative.ValueKind != JsonValueKind.Null ? relative.GetString() : null))
            .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static DirectoryInventory CaptureProfileInventory(JsonElement preflight)
        => ReadInventory(preflight.GetProperty("profileRoot").GetString()
                         ?? throw new InvalidDataException("preflight profileRoot missing"), recursive: true);

    internal static DirectoryInventory CaptureAccountTopLevelInventory(JsonElement preflight)
        => ReadInventory(preflight.GetProperty("accountRoot").GetString()
                         ?? throw new InvalidDataException("preflight accountRoot missing"), recursive: false);

    internal static List<string> CompareInventories(DirectoryInventory before, DirectoryInventory after, string label)
    {
        var differences = new List<string>();
        if (!before.Directories.SequenceEqual(after.Directories, StringComparer.OrdinalIgnoreCase))
            differences.Add($"{label} directory inventory changed");
        if (!before.Files.SequenceEqual(after.Files, StringComparer.OrdinalIgnoreCase))
            differences.Add($"{label} file inventory changed");
        return differences;
    }

    internal static List<string> CompareFileIdentities(IReadOnlyList<FileIdentity> before, IReadOnlyList<FileIdentity> after)
    {
        var differences = new List<string>();
        var beforeByPath = before.ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
        var afterByPath = after.ToDictionary(x => x.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var path in beforeByPath.Keys.Union(afterByPath.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (!beforeByPath.TryGetValue(path, out var left) || !afterByPath.TryGetValue(path, out var right))
            {
                differences.Add($"inventory changed: {path}");
                continue;
            }
            if (left.Exists != right.Exists || left.Length != right.Length || left.Sha256 != right.Sha256 || left.LastWriteUtc != right.LastWriteUtc)
                differences.Add($"identity changed: {path}");
        }
        return differences;
    }

    private static EnvironmentIdentity ReadEnvironment()
    {
        var mismatches = new List<string>();
        var sts2Path = Normalize(typeof(NGame).Assembly.Location);
        var sts2Hash = JsonSupport.Sha256File(sts2Path);
        var gameDir = Directory.GetParent(Path.GetDirectoryName(sts2Path)!)!.FullName;
        var releasePath = Path.Combine(gameDir, "release_info.json");
        using var release = JsonDocument.Parse(File.ReadAllText(releasePath));
        var root = release.RootElement;
        var version = root.GetProperty("version").GetString() ?? "unknown";
        var commit = root.GetProperty("commit").GetString() ?? "unknown";
        var branch = root.GetProperty("branch").GetString() ?? "unknown";
        if (version != ProbeConstants.ExpectedGameVersion) mismatches.Add($"version expected {ProbeConstants.ExpectedGameVersion}, actual {version}");
        if (commit != ProbeConstants.ExpectedCommit) mismatches.Add($"commit expected {ProbeConstants.ExpectedCommit}, actual {commit}");
        if (sts2Hash != ProbeConstants.ExpectedDllSha256) mismatches.Add($"sts2.dll hash expected {ProbeConstants.ExpectedDllSha256}, actual {sts2Hash}");

        var mods = ModManager.GetLoadedMods()
            .Where(x => x.manifest is not null)
            .Select(x =>
            {
                var manifest = x.manifest!;
                return new LoadedModIdentity(
                    manifest.id ?? "",
                    manifest.name ?? "",
                    manifest.version ?? "",
                    manifest.affectsGameplay,
                    x.state.ToString(),
                    x.assemblies.Select(a => Normalize(a.Location))
                        .Where(File.Exists)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .Select(path => new ModAssemblyIdentity(path, JsonSupport.Sha256File(path)))
                        .ToList());
            })
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var ids = mods.Select(x => x.Id).ToList();
        if (ids.Count != 1 || !ids.Contains("SPIKEINV004A", StringComparer.Ordinal))
            mismatches.Add("enabled mod set must contain exactly SPIKEINV004A; actual: " + string.Join(", ", ids));

        var probePath = Normalize(typeof(ProbeController).Assembly.Location);
        return new EnvironmentIdentity(
            version,
            commit,
            branch,
            sts2Path,
            new FileInfo(sts2Path).Length,
            sts2Hash,
            ProbeConstants.ProbeVersion,
            probePath,
            JsonSupport.Sha256File(probePath),
            mods,
            mismatches);
    }

    private static async Task<SaveTaskObservation> ObserveCurrentSaveTask(SaveManager manager)
    {
        var task = manager.CurrentRunSaveTask;
        if (task is null)
            return new SaveTaskObservation("NoTask", "NoTask", true, false, false, null);
        var before = task.Status.ToString();
        Exception? observed = null;
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            observed = ex;
        }

        return new SaveTaskObservation(
            before,
            task.Status.ToString(),
            task.IsCompleted,
            task.IsFaulted,
            task.IsCanceled,
            observed?.ToString() ?? task.Exception?.ToString());
    }

    private static ContinueSummary SummarizeContinue(SerializableRun save)
    {
        MegaCrit.Sts2.Core.Map.MapCoord? latest = save.VisitedMapCoords.Count > 0
            ? save.VisitedMapCoords[^1]
            : null;
        var json = JsonSerializationUtility.ToJson(save);
        return new ContinueSummary(
            save.CurrentActIndex,
            save.FloorReached,
            latest?.row,
            latest?.col,
            save.SerializableRng.Seed ?? "",
            save.NumReloads,
            save.GameMode.ToString(),
            save.Ascension,
            save.Players.Count,
            JsonSupport.Sha256Text(json));
    }

    private static ObjectIdentity DescribeManager(SaveManager manager)
    {
        var fields = manager.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var stores = fields
            .Where(f => typeof(ISaveStore).IsAssignableFrom(f.FieldType))
            .Select(f => $"{f.Name}:{f.GetValue(manager)?.GetType().FullName ?? "null"}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        var providers = fields
            .Where(f => typeof(IProfileIdProvider).IsAssignableFrom(f.FieldType))
            .Select(f => $"{f.Name}:{f.GetValue(manager)?.GetType().FullName ?? "null"}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        foreach (var field in fields.Where(f => f.FieldType.Namespace?.Contains("Saves.Managers", StringComparison.Ordinal) == true))
        {
            var child = field.GetValue(manager);
            if (child is null) continue;
            foreach (var childField in child.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (typeof(ISaveStore).IsAssignableFrom(childField.FieldType))
                    stores.Add($"{field.Name}.{childField.Name}:{childField.GetValue(child)?.GetType().FullName ?? "null"}");
                if (typeof(IProfileIdProvider).IsAssignableFrom(childField.FieldType))
                    providers.Add($"{field.Name}.{childField.Name}:{childField.GetValue(child)?.GetType().FullName ?? "null"}");
            }
        }

        return new ObjectIdentity(
            "original",
            manager.GetType().FullName ?? manager.GetType().Name,
            RuntimeHelpers.GetHashCode(manager),
            stores.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList(),
            providers.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList(),
            stores.Any(x => x.Contains("CloudSaveStore", StringComparison.Ordinal) || x.Contains("RemoteSaveStore", StringComparison.Ordinal)),
            "Runtime wrapper shape only; Steam Cloud enabled/disabled state requires user confirmation for this session.");
    }

    private static ReadSaveResult<SerializableRun> ReadCachedContinue(NMainMenu menu)
    {
        var field = typeof(NMainMenu).GetField("_readRunSaveResult", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(NMainMenu).FullName, "_readRunSaveResult");
        return field.GetValue(menu) as ReadSaveResult<SerializableRun>
            ?? throw new InvalidOperationException("Main-menu cached Continue result is null or has the wrong type.");
    }

    private static DirectoryInventory ReadInventory(string root, bool recursive)
    {
        if (!Directory.Exists(root))
            return new DirectoryInventory(root, Array.Empty<string>(), Array.Empty<string>());

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var directories = Directory.GetDirectories(root, "*", option)
            .Select(path => NormalizeRelative(Path.GetRelativePath(root, path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var files = Directory.GetFiles(root, "*", option)
            .Select(path => NormalizeRelative(Path.GetRelativePath(root, path)))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new DirectoryInventory(root, directories, files);
    }

    private static FileIdentity ReadFileIdentity(string scope, string path, string? relativePath)
    {
        path = Normalize(path);
        if (!File.Exists(path))
            return new FileIdentity(scope, path, relativePath, false, null, null, null);
        var info = new FileInfo(path);
        return new FileIdentity(scope, path, relativePath, true, info.Length, JsonSupport.Sha256File(path), info.LastWriteTimeUtc.ToString("O"));
    }

    private static IReadOnlyList<BackupEntry> CopyProfileBackup(string profileRoot, IReadOnlyList<string> files, string backupRoot)
    {
        Directory.CreateDirectory(backupRoot);
        var entries = new List<BackupEntry>();
        foreach (var relative in files)
        {
            if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("..", StringComparer.Ordinal))
                throw new InvalidOperationException($"Backup inventory contains an unsafe relative path: {relative}");
            var source = Normalize(Path.Combine(profileRoot, relative));
            if (!IsStrictChild(source, profileRoot))
                throw new InvalidOperationException($"Backup source escaped active profile root: {source}");
            var destination = Normalize(Path.Combine(backupRoot, relative));
            if (!IsStrictChild(destination, backupRoot))
                throw new InvalidOperationException($"Backup destination escaped evidence backup root: {destination}");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: false);
            var sourceHash = JsonSupport.Sha256File(source);
            var destinationHash = JsonSupport.Sha256File(destination);
            entries.Add(new BackupEntry(source, relative, destination, new FileInfo(source).Length, sourceHash, destinationHash));
        }
        return entries;
    }

    private static string NormalizeRelative(string path)
    {
        if (Path.IsPathRooted(path))
            throw new InvalidOperationException($"Inventory path unexpectedly became rooted: {path}");
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        if (normalized.Split(Path.DirectorySeparatorChar).Contains("..", StringComparer.Ordinal))
            throw new InvalidOperationException($"Inventory path escaped its root: {path}");
        return normalized;
    }

    private static bool TryGetMainMenu(out NMainMenu? menu)
    {
        menu = null;
        if (_mainMenu is null || !_mainMenu.TryGetTarget(out var candidate))
            return false;
        if (!GodotObject.IsInstanceValid(candidate) || !candidate.IsInsideTree())
            return false;
        menu = candidate;
        return true;
    }

    private static bool ReadEnabled(NMainMenuTextButton button)
    {
        var property = typeof(NClickableControl).GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? typeof(NClickableControl).GetProperty("Enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return property?.GetValue(button) as bool? ?? true;
    }

    private static bool IsStrictChild(string candidate, string root)
    {
        var normalizedCandidate = Normalize(candidate);
        var normalizedRoot = Normalize(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
               && !string.Equals(normalizedCandidate.TrimEnd(Path.DirectorySeparatorChar), normalizedRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path) => Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    private static void Log(string message) => GD.Print($"[SPIKE-INV004A] {message}");
    private static void Error(string message) => GD.PrintErr($"[SPIKE-INV004A] {message}");
}
