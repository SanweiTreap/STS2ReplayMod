using System.Diagnostics;
using System.Reflection;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace Spike001A;

internal static class ProbeController
{
    private const string FixtureRunFileName = "fixture.run.json";
    private const string FixtureExpectedFileName = "fixture.expected.json";
    private const string FixtureMetadataFileName = "fixture.metadata.json";
    private static readonly string SessionStartedUtc = DateTimeOffset.UtcNow.ToString("O");
    private static readonly SemaphoreSlim OperationLock = new(1, 1);
    private static readonly PropertyInfo ShouldSaveProperty = typeof(RunManager).GetProperty(
        "ShouldSave",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new MissingMemberException(typeof(RunManager).FullName, "ShouldSave");
    private static string OutputDirectory { get; set; } = "";
    private static EnvironmentIdentity? Environment { get; set; }

    internal static void Initialize()
    {
        OutputDirectory = ProjectSettings.GlobalizePath("user://SPIKE-001A");
        Directory.CreateDirectory(OutputDirectory);
        Environment = ReadEnvironment();
        JsonSupport.WriteAtomic(Path.Combine(OutputDirectory, "environment.json"), Environment);

        if (!Environment.MatchesFixedEnvironment)
        {
            Error("Environment drift detected. Probe disabled. See environment.json.");
            return;
        }

        Log($"Ready. F8=capture fixture, F9=restore fixture, F10=status. Output: {OutputDirectory}");
    }

    internal static void PrintStatus()
    {
        var fixtureExists = File.Exists(Path.Combine(OutputDirectory, FixtureRunFileName));
        Log($"status environmentOk={Environment?.MatchesFixedEnvironment} fixtureExists={fixtureExists} busy={OperationLock.CurrentCount == 0} output={OutputDirectory}");
    }

    internal static async Task CaptureFixtureAsync()
    {
        if (!CanRun() || !await OperationLock.WaitAsync(0))
        {
            Error("Capture ignored: environment invalid or another probe operation is running.");
            return;
        }

        try
        {
            Log("Capture started.");
            var scenarioErrors = ValidateScenario();
            if (scenarioErrors.Count > 0)
            {
                JsonSupport.WriteAtomic(Path.Combine(OutputDirectory, "capture-rejected.json"), new
                {
                    timestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                    errors = scenarioErrors,
                    readiness = TryCaptureReadiness(),
                });
                Error("Capture rejected: " + string.Join("; ", scenarioErrors));
                return;
            }

            var liveSaveBefore = ReadLiveSaveIdentity();
            var before = CaptureSnapshot();
            var runManager = RunManager.Instance;
            var currentState = CurrentState();
            var save = runManager.ToSave(currentState.CurrentRoom);
            var after = CaptureSnapshot();
            var prePostDiff = JsonSupport.Diff(before, after);
            var liveSaveAfter = ReadLiveSaveIdentity();

            if (prePostDiff.Count > 0)
            {
                JsonSupport.WriteAtomic(Path.Combine(OutputDirectory, "capture-tosave-state-change.json"), new
                {
                    timestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                    before,
                    after,
                    diff = prePostDiff,
                    liveSaveBefore,
                    liveSaveAfter,
                });
                Error($"ToSave changed {prePostDiff.Count} observed field(s); fixture not created.");
                return;
            }

            if (!FileIdentityEquals(liveSaveBefore, liveSaveAfter))
            {
                JsonSupport.WriteAtomic(Path.Combine(OutputDirectory, "capture-live-save-change.json"), new
                {
                    timestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                    liveSaveBefore,
                    liveSaveAfter,
                });
                Error("Ordinary live save changed during capture; fixture not created.");
                return;
            }

            var nativeJson = JsonSerializationUtility.ToJson(save);
            var fixturePath = Path.Combine(OutputDirectory, FixtureRunFileName);
            File.WriteAllText(fixturePath + ".tmp", nativeJson, new UTF8Encoding(false));
            File.Move(fixturePath + ".tmp", fixturePath, true);
            var fixtureHash = JsonSupport.Sha256(nativeJson);

            var player = currentState.Players.Single();
            var metadata = new FixtureMetadata(
                ProbeConstants.SchemaVersion,
                ProbeConstants.ProbeVersion,
                DateTimeOffset.UtcNow.ToString("O"),
                System.Environment.ProcessId,
                SessionStartedUtc,
                Environment!.GameVersion,
                Environment.Commit,
                Environment.Sts2DllSha256,
                Environment.ProbeArtifactSha256,
                fixturePath,
                fixtureHash,
                player.Character.Id.ToString(),
                save.SerializableRng.Seed ?? "",
                currentState.GameMode.ToString(),
                currentState.AscensionLevel,
                Environment.LoadedMods,
                liveSaveBefore,
                liveSaveAfter,
                prePostDiff);

            JsonSupport.WriteAtomic(Path.Combine(OutputDirectory, FixtureExpectedFileName), before);
            JsonSupport.WriteAtomic(Path.Combine(OutputDirectory, FixtureMetadataFileName), metadata);
            Log($"Capture succeeded. fixtureSha256={fixtureHash}");
        }
        catch (Exception ex)
        {
            JsonSupport.WriteAtomic(Path.Combine(OutputDirectory, "capture-exception.json"), ExceptionPayload(ex));
            Error($"Capture failed: {ex}");
        }
        finally
        {
            OperationLock.Release();
        }
    }

    internal static async Task RestoreFixtureAsync()
    {
        if (!CanRun() || !await OperationLock.WaitAsync(0))
        {
            Error("Restore ignored: environment invalid or another probe operation is running.");
            return;
        }

        var attempt = NextAttemptNumber();
        var startedUtc = DateTimeOffset.UtcNow.ToString("O");
        var stages = new List<StageObservation>();
        var mismatches = new List<string>();
        WhitelistSnapshot? expected = null;
        WhitelistSnapshot? actual = null;
        IReadOnlyList<JsonDiffEntry> diff = Array.Empty<JsonDiffEntry>();
        string fixtureHash = "unavailable";
        string? firstFailure = null;
        var liveSaveBefore = ReadLiveSaveIdentity();
        var liveSaveAfter = liveSaveBefore;
        FixtureMetadata? metadata = null;
        var environmentValidated = false;

        try
        {
            Log($"Restore attempt {attempt} started.");
            var fixturePath = Path.Combine(OutputDirectory, FixtureRunFileName);
            var metadataPath = Path.Combine(OutputDirectory, FixtureMetadataFileName);
            var expectedPath = Path.Combine(OutputDirectory, FixtureExpectedFileName);

            await ObserveStage("validate_fixture_and_environment", stages, () =>
            {
                if (!File.Exists(fixturePath) || !File.Exists(metadataPath) || !File.Exists(expectedPath))
                    throw new FileNotFoundException("Fixture set is incomplete. Press F8 at the required boundary first.");

                metadata = JsonSupport.Read<FixtureMetadata>(metadataPath);
                expected = JsonSupport.Read<WhitelistSnapshot>(expectedPath);
                fixtureHash = JsonSupport.Sha256File(fixturePath);
                mismatches.AddRange(ValidateFixture(metadata, fixtureHash));
                if (mismatches.Count > 0)
                    throw new InvalidOperationException("Fixture/environment mismatch: " + string.Join("; ", mismatches));
                environmentValidated = true;
                return Task.CompletedTask;
            });

            SerializableRun? save = null;
            await ObserveStage("parse_fixture", stages, () =>
            {
                var result = JsonSerializationUtility.FromJson<SerializableRun>(File.ReadAllText(fixturePath));
                if (!result.Success || result.SaveData is null)
                    throw new InvalidDataException($"Native fixture parse failed: status={result.Status}, error={result.ErrorMessage}");
                save = result.SaveData;
                return Task.CompletedTask;
            });

            RunState? restoredState = null;
            await ObserveStage("RunState.FromSerializable", stages, () =>
            {
                restoredState = RunState.FromSerializable(save!);
                return Task.CompletedTask;
            });

            await ObserveStage("disable_ordinary_save_before_restore", stages, () =>
            {
                DisableOrdinarySaving();
                return Task.CompletedTask;
            });
            await ObserveStage("RunManager.SetUpSavedSingleplayer", stages, async () =>
            {
                await RunManager.Instance.SetUpSavedSingleplayer(restoredState!, save!);
                DisableOrdinarySaving();
            });

            await ObserveStage("NGame.LoadRun", stages, async () =>
            {
                var game = NGame.Instance ?? throw new InvalidOperationException("NGame.Instance is null.");
                await game.LoadRun(restoredState!, save!.PreFinishedRoom);
                DisableOrdinarySaving();
            });

            await ObserveStage("wait_for_input_readiness", stages, async () =>
            {
                var deadline = Stopwatch.StartNew();
                while (deadline.Elapsed < TimeSpan.FromSeconds(15))
                {
                    if (TryCaptureReadiness()?.CandidateReady == true)
                        return;
                    await Task.Delay(50);
                }

                throw new TimeoutException("Input readiness candidate was not reached within 15 seconds: " +
                                           System.Text.Json.JsonSerializer.Serialize(TryCaptureReadiness(), JsonSupport.Options));
            });

            await ObserveStage("capture_actual_and_diff", stages, () =>
            {
                actual = CaptureSnapshot();
                diff = JsonSupport.Diff(expected!, actual);
                if (diff.Count > 0)
                    firstFailure = "whitelist_or_rng_diff";
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            firstFailure = stages.LastOrDefault(x => x.Outcome == "exception")?.Name ?? "probe_internal";
            Error($"Restore attempt {attempt} stopped at {firstFailure}: {ex.Message}");
        }
        finally
        {
            try
            {
                DisableOrdinarySaving();
                liveSaveAfter = ReadLiveSaveIdentity();
            }
            catch (Exception ex)
            {
                firstFailure ??= "live_save_verification";
                Error($"Live save verification failed: {ex.Message}");
            }

            var liveSaveUnchanged = FileIdentityEquals(liveSaveBefore, liveSaveAfter);
            if (!liveSaveUnchanged)
                firstFailure ??= "ordinary_live_save_changed";

            var passed = firstFailure is null && diff.Count == 0 && liveSaveUnchanged;
            var result = new RestoreAttemptResult(
                attempt,
                startedUtc,
                DateTimeOffset.UtcNow.ToString("O"),
                System.Environment.ProcessId,
                SessionStartedUtc,
                metadata is not null && metadata.CreatedSessionStartedUtc != SessionStartedUtc,
                fixtureHash,
                environmentValidated,
                mismatches,
                stages,
                firstFailure,
                expected,
                actual,
                diff,
                liveSaveBefore,
                liveSaveAfter,
                liveSaveUnchanged,
                passed);

            var resultPath = Path.Combine(OutputDirectory, $"restore-attempt-{attempt:D2}.json");
            JsonSupport.WriteAtomic(resultPath, result);
            UpdateSummary();
            Log($"Restore attempt {attempt} finished. passed={passed} diffCount={diff.Count} liveSaveUnchanged={liveSaveUnchanged}");
            OperationLock.Release();
        }
    }

    private static async Task ObserveStage(string name, ICollection<StageObservation> stages, Func<Task> action)
    {
        var started = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        Log($"stage={name} outcome=start");
        try
        {
            await action();
            stopwatch.Stop();
            stages.Add(new StageObservation(name, started.ToString("O"), DateTimeOffset.UtcNow.ToString("O"), stopwatch.ElapsedMilliseconds, "success", null, null, null));
            Log($"stage={name} outcome=success elapsedMs={stopwatch.ElapsedMilliseconds}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            stages.Add(new StageObservation(name, started.ToString("O"), DateTimeOffset.UtcNow.ToString("O"), stopwatch.ElapsedMilliseconds, "exception", ex.GetType().FullName, ex.Message, ex.StackTrace));
            Log($"stage={name} outcome=exception elapsedMs={stopwatch.ElapsedMilliseconds} error={ex.Message}");
            throw;
        }
    }

    private static WhitelistSnapshot CaptureSnapshot()
    {
        var runManager = RunManager.Instance ?? throw new InvalidOperationException("RunManager.Instance is null.");
        var state = CurrentState();
        var player = state.Players.Single();
        var playerCombat = player.PlayerCombatState ?? throw new InvalidOperationException("PlayerCombatState is null.");
        var combat = CombatManager.Instance ?? throw new InvalidOperationException("CombatManager.Instance is null.");
        var combatState = combat.DebugOnlyGetState() ?? throw new InvalidOperationException("CombatState is null.");
        var coord = state.CurrentMapCoord;

        static IReadOnlyList<string> OrderedCards(CardPile pile) => pile.Cards.Select(c => c.Id.ToString()).ToList();
        static IReadOnlyList<string> CardMultiset(CardPile pile) => pile.Cards.Select(c => c.Id.ToString()).OrderBy(x => x, StringComparer.Ordinal).ToList();

        var serializableRng = state.Rng.ToSerializable();
        var rngEntries = serializableRng.Rngs
            .OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)
            .Select(x => new RngEntrySnapshot(x.Key.ToString(), x.Value.counter, x.Value.state0, x.Value.state1, x.Value.state2, x.Value.state3))
            .ToList();
        var rngCanonical = string.Join("\n", rngEntries.Select(x => $"{x.Key}|{x.Counter}|{x.State0:X16}|{x.State1:X16}|{x.State2:X16}|{x.State3:X16}"));

        return new WhitelistSnapshot(
            new PositionSnapshot(
                state.CurrentActIndex,
                coord?.row,
                coord?.col,
                state.CurrentRoom?.Id,
                state.CurrentRoom?.ModelId?.ToString() ?? "",
                state.CurrentRoom?.RoomType.ToString() ?? "",
                state.TotalFloor,
                combatState.RoundNumber),
            new PlayerSnapshot(
                player.Character.Id.ToString(),
                player.Creature.CurrentHp,
                playerCombat.Energy,
                OrderedCards(playerCombat.Hand),
                CardMultiset(playerCombat.DrawPile),
                CardMultiset(playerCombat.DiscardPile),
                CardMultiset(playerCombat.ExhaustPile)),
            combatState.Enemies
                .OrderBy(x => x.CombatId)
                .Select(x => new EnemySnapshot(x.ModelId.ToString(), x.CombatId, x.CurrentHp, x.IsAlive))
                .ToList(),
            new RngSnapshot(serializableRng.Seed ?? "", JsonSupport.Sha256(rngCanonical), rngEntries),
            CaptureReadiness(state, playerCombat));
    }

    private static ReadinessSnapshot CaptureReadiness(RunState state, MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState playerCombat)
    {
        var combat = CombatManager.Instance;
        var executor = RunManager.Instance.ActionExecutor;
        var player = state.Players.Single();
        var ready = combat.IsInProgress &&
                    !combat.IsOverOrEnding &&
                    !combat.IsPaused &&
                    !RunManager.Instance.IsPaused &&
                    !combat.PlayerActionsDisabled &&
                    combat.IsPartOfPlayerTurn(player) &&
                    playerCombat.Phase == PlayerTurnPhase.Play &&
                    !executor.IsRunning;

        return new ReadinessSnapshot(
            playerCombat.Phase.ToString(),
            combat.IsPartOfPlayerTurn(player),
            combat.PlayerActionsDisabled,
            combat.IsPaused,
            RunManager.Instance.IsPaused,
            combat.IsInProgress,
            combat.IsOverOrEnding,
            executor.IsRunning,
            executor.IsPaused,
            executor.CurrentlyRunningAction?.GetType().FullName,
            NGame.Instance?.CurrentRunNode is not null,
            ready);
    }

    private static ReadinessSnapshot? TryCaptureReadiness()
    {
        try
        {
            var state = RunManager.Instance is null ? null : RunManager.Instance.DebugOnlyGetState();
            var playerCombat = state?.Players.SingleOrDefault()?.PlayerCombatState;
            return state is null || playerCombat is null ? null : CaptureReadiness(state, playerCombat);
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ValidateScenario()
    {
        var errors = new List<string>();
        var state = RunManager.Instance is null ? null : RunManager.Instance.DebugOnlyGetState();
        var combat = CombatManager.Instance;
        if (state is null)
            return ["RunManager.State is null"];
        if (state.Players.Count != 1)
            errors.Add($"expected one player, actual {state.Players.Count}");
        if (!string.Equals(state.GameMode.ToString(), "Standard", StringComparison.OrdinalIgnoreCase))
            errors.Add($"expected Standard, actual {state.GameMode}");
        if (state.AscensionLevel != 0)
            errors.Add($"expected Ascension 0, actual {state.AscensionLevel}");
        if (state.CurrentActIndex != 0)
            errors.Add($"expected act index 0, actual {state.CurrentActIndex}");
        // In v0.111.0 row 0 is the opening ancient/Neow point; the first weak-monster
        // combat is therefore row 1 even when the new-profile flow does not show a
        // separate Neow interaction to the player.
        if (state.CurrentMapCoord?.row != 1)
            errors.Add($"expected first weak-monster map row 1, actual {state.CurrentMapCoord?.row.ToString() ?? "null"}");
        if (combat?.DebugOnlyGetState()?.RoundNumber != 1)
            errors.Add($"expected combat round 1, actual {combat?.DebugOnlyGetState()?.RoundNumber.ToString() ?? "null"}");
        if (state.CurrentRoom is null || !state.CurrentRoom.RoomType.ToString().Contains("Monster", StringComparison.OrdinalIgnoreCase))
            errors.Add($"expected a weak-monster room, actual {state.CurrentRoom?.RoomType.ToString() ?? "null"}");
        if (TryCaptureReadiness()?.CandidateReady != true)
            errors.Add("input readiness candidate is false");
        return errors;
    }

    private static IReadOnlyList<string> ValidateFixture(FixtureMetadata metadata, string fixtureHash)
    {
        var mismatches = new List<string>();
        if (metadata.SchemaVersion != ProbeConstants.SchemaVersion) mismatches.Add($"schema expected {ProbeConstants.SchemaVersion}, fixture {metadata.SchemaVersion}");
        if (metadata.ProbeVersion != ProbeConstants.ProbeVersion) mismatches.Add($"probe version expected {ProbeConstants.ProbeVersion}, fixture {metadata.ProbeVersion}");
        if (metadata.GameVersion != Environment!.GameVersion) mismatches.Add($"game version fixture {metadata.GameVersion}, current {Environment.GameVersion}");
        if (metadata.Commit != Environment.Commit) mismatches.Add($"commit fixture {metadata.Commit}, current {Environment.Commit}");
        if (metadata.Sts2DllSha256 != Environment.Sts2DllSha256) mismatches.Add("sts2.dll hash mismatch");
        if (metadata.ProbeArtifactSha256 != Environment.ProbeArtifactSha256) mismatches.Add("probe artifact hash mismatch");
        if (metadata.FixtureSha256 != fixtureHash) mismatches.Add($"fixture hash expected {metadata.FixtureSha256}, actual {fixtureHash}");
        if (!JsonSupport.Diff(metadata.LoadedMods, Environment.LoadedMods).Any()) return mismatches;
        mismatches.Add("loaded mod set/version/artifact differs from fixture");
        return mismatches;
    }

    private static EnvironmentIdentity ReadEnvironment()
    {
        var mismatches = new List<string>();
        var sts2Path = typeof(NGame).Assembly.Location;
        var sts2Hash = JsonSupport.Sha256File(sts2Path);
        var gameDir = Directory.GetParent(Path.GetDirectoryName(sts2Path)!)!.FullName;
        var releasePath = Path.Combine(gameDir, "release_info.json");
        using var release = System.Text.Json.JsonDocument.Parse(File.ReadAllText(releasePath));
        var root = release.RootElement;
        var version = root.GetProperty("version").GetString() ?? "unknown";
        var commit = root.GetProperty("commit").GetString() ?? "unknown";
        var branch = root.GetProperty("branch").GetString() ?? "unknown";
        if (version != ProbeConstants.ExpectedGameVersion) mismatches.Add($"version expected {ProbeConstants.ExpectedGameVersion}, actual {version}");
        if (commit != ProbeConstants.ExpectedCommit) mismatches.Add($"commit expected {ProbeConstants.ExpectedCommit}, actual {commit}");
        if (sts2Hash != ProbeConstants.ExpectedDllSha256) mismatches.Add($"sts2.dll hash expected {ProbeConstants.ExpectedDllSha256}, actual {sts2Hash}");

        var probePath = typeof(ProbeController).Assembly.Location;
        var mods = ModManager.GetLoadedMods()
            .Where(x => x.manifest is not null)
            .Select(x =>
            {
                var manifest = x.manifest!;
                return new LoadedModInfo(
                    manifest.id ?? "",
                    manifest.name ?? "",
                    manifest.version ?? "",
                    manifest.affectsGameplay,
                    x.state.ToString(),
                    x.assemblies.Select(a => a.Location)
                        .Where(File.Exists)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .Select(path => new ModAssemblyInfo(path, JsonSupport.Sha256File(path)))
                        .ToList());
            })
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
        var otherLoadedMods = mods.Where(x => !string.Equals(x.Id, "SPIKE001A", StringComparison.Ordinal)).Select(x => x.Id).ToList();
        if (otherLoadedMods.Count > 0)
            mismatches.Add("other loaded mods are forbidden for this experiment: " + string.Join(", ", otherLoadedMods));

        return new EnvironmentIdentity(
            version,
            commit,
            branch,
            sts2Path,
            sts2Hash,
            ProbeConstants.ProbeVersion,
            ProbeConstants.SchemaVersion,
            probePath,
            JsonSupport.Sha256File(probePath),
            OutputDirectory,
            System.Environment.ProcessId,
            SessionStartedUtc,
            mods,
            mismatches.Count == 0,
            mismatches);
    }

    private static FileIdentity ReadLiveSaveIdentity()
    {
        try
        {
            var profileId = SaveManager.Instance.CurrentProfileId;
            var godotPath = UserDataPathProvider.GetProfileScopedPath(profileId, "saves/" + RunSaveManager.runSaveFileName);
            var path = ProjectSettings.GlobalizePath(godotPath);
            if (!File.Exists(path))
                return new FileIdentity(path, false, null, null, null);
            var info = new FileInfo(path);
            return new FileIdentity(path, true, info.Length, JsonSupport.Sha256File(path), info.LastWriteTimeUtc.ToString("O"));
        }
        catch (Exception ex)
        {
            return new FileIdentity("unresolved: " + ex.Message, false, null, null, null);
        }
    }

    private static bool FileIdentityEquals(FileIdentity left, FileIdentity right)
        => left.Path == right.Path && left.Exists == right.Exists && left.Length == right.Length && left.Sha256 == right.Sha256 && left.LastWriteUtc == right.LastWriteUtc;

    private static RunState CurrentState()
        => RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("RunManager state is unavailable.");

    private static void DisableOrdinarySaving()
    {
        ShouldSaveProperty.SetValue(RunManager.Instance, false);
        if (RunManager.Instance.ShouldSave)
            throw new InvalidOperationException("Could not disable ordinary run saving before restore.");
    }

    private static int NextAttemptNumber()
    {
        var existing = Directory.GetFiles(OutputDirectory, "restore-attempt-*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(x => int.TryParse(x?["restore-attempt-".Length..], out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();
        return existing + 1;
    }

    private static void UpdateSummary()
    {
        var files = Directory.GetFiles(OutputDirectory, "restore-attempt-*.json").OrderBy(x => x, StringComparer.Ordinal).ToList();
        var results = files.Select(JsonSupport.Read<RestoreAttemptResult>).ToList();
        var passed = results.Count(x => x.Passed);
        var restarted = results.Any(x => x.Passed && x.AfterFullRestart);
        var baselinePassed = passed >= 2 && restarted;
        JsonSupport.WriteAtomic(Path.Combine(OutputDirectory, "summary.json"), new ProbeSummary(
            DateTimeOffset.UtcNow.ToString("O"),
            results.Count,
            passed,
            restarted,
            baselinePassed,
            baselinePassed
                ? "Restore baseline passed; recommend unblocking SPIKE-001B"
                : "Restore baseline not established; keep SPIKE-001B blocked",
            files));
    }

    private static object ExceptionPayload(Exception ex) => new
    {
        timestampUtc = DateTimeOffset.UtcNow.ToString("O"),
        exceptionType = ex.GetType().FullName,
        ex.Message,
        ex.StackTrace,
    };

    private static bool CanRun() => Environment?.MatchesFixedEnvironment == true;

    private static void Log(string message) => GD.Print($"[SPIKE-001A] {message}");
    private static void Error(string message) => GD.PrintErr($"[SPIKE-001A] {message}");
}
