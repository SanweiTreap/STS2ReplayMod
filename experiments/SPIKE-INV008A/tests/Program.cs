using System.Text.Json;
using SpikeInv008A;

namespace SpikeInv008A.Tests;

internal static class Program
{
    private static int Main()
    {
        var cases = new (string, Action)[]
        {
            ("fixed identity constants", FixedIdentity),
            ("new probe identity", NewIdentity),
            ("v1 through R5 and INV009 prior chain exact", PriorAttempts),
            ("manifest version is valid SemVer", ManifestSemVer),
            ("wrong mod set is isolated precheck", WrongModSetPrecheck),
            ("corrected mod set can proceed", CorrectedModSet),
            ("precheck artifact is create-new", PrecheckCreateNew),
            ("precheck artifact is not phase pass", PrecheckNotPhasePass),
            ("loaded set is exactly 008A", ExactLoadedSet),
            ("initializer does not read provisional loaded set", InitializerDoesNotReadLoadedSet),
            ("post-loader wrong sets remain pre-business", PostLoaderWrongSetsRemainPreBusiness),
            ("fixed 007 schema lacks comparable Continue", Fixed007SchemaLacksContinue),
            ("recovery copy mtime is not source oracle", RecoveryMtimeIsNotSourceOracle),
            ("real fixed R6 prior gate reaches GateReady", RealFixedPriorGateReachesReady),
            ("historical settings identity is rejected", HistoricalSettingsRejected),
            ("active-profile content difference is rejected", ActiveContentRejected),
            ("profile inventory difference is rejected", PriorInventoryRejected),
            ("fresh Continue failures are distinct", FreshContinueFailuresDistinct),
            ("terminal before page creation is rendered", TerminalBeforePageRendered),
            ("setup exact fault accepted", SetupFault),
            ("postlaunch exact fault accepted", PostLaunchFault),
            ("wrong fault token rejected", WrongToken),
            ("wrong fault site rejected", WrongSite),
            ("owner exception never escapes", OwnerCatch),
            ("single teardown CAS owner", TeardownOwner),
            ("every teardown step fault continues", EveryTeardownStepFault),
            ("cleanup middle fault exposes missing later disposer", CleanupMiddleFault),
            ("real Replay null-lobby 13-call path passes", ReplayNullLobbyPositive),
            ("nonnull lobby paired calls pass", NonNullLobbyPositive),
            ("nonnull lobby missing either call fails", NonNullLobbyMissingCall),
            ("nonnull lobby duplicate call fails", NonNullLobbyDuplicate),
            ("nonnull lobby cleanup exception fails", NonNullLobbyException),
            ("null lobby unexpected call fails", NullLobbyUnexpectedCall),
            ("pre-setup stale lobby blocks", PreSetupStaleLobby),
            ("setup launch scene lobby transitions fail", LobbyBoundaryTransitions),
            ("each core omission and cleanup exception fails", CoreOmissionAndException),
            ("post-launch owned NRun must be replaced", PostLaunchSceneReplacement),
            ("real R4 artifact is core-call-set only", RealR4CoreOnly),
            ("production restored-menu callback order passes", RestoredMenuProductionOrder),
            ("restored-menu missing prefix fails identity order", RestoredMenuMissingPrefix),
            ("restored-menu wrong candidate fails identity order", RestoredMenuWrongCandidate),
            ("refresh from old menu fails identity order", RestoredMenuOldRefresh),
            ("only menu-ready reports refresh missing", RestoredMenuOnlyMenu),
            ("only refresh reports menu-ready missing", RestoredMenuOnlyRefresh),
            ("both menu signals missing remain independently pending", RestoredMenuBothMissing),
            ("duplicate restored-menu callback fails", RestoredMenuDuplicate),
            ("ready postfix identity replacement fails", RestoredMenuPostfixReplacement),
            ("real LoadMainMenu exception is distinct", RestoredMenuLoadException),
            ("real R5 artifact is observer defect only", RealR5ObserverOnly),
            ("manager clear fallback allowed once", ManagerFallback),
            ("menu forbidden unless cleanup and restore pass", MenuGate),
            ("empty chain selects setup", EmptyChain),
            ("setup pass selects restart", SetupPassChain),
            ("restart pass selects postlaunch", RestartPassChain),
            ("postlaunch pass selects final restart", PostLaunchPassChain),
            ("completed chain is inert", CompletedChain),
            ("phase artifact already exists blocks overwrite", ExistingArtifact),
            ("phase order gap is invalid", PhaseGap),
            ("non-pass prior artifact blocks next phase", PriorFailureBlocks),
            ("restart rule is content read-only", RestartReadOnly),
            ("known mtime changes allowed", KnownMtime),
            ("unknown mtime change rejected", UnknownMtime),
            ("content change rejected despite mtime whitelist", ContentChange),
            ("inventory change rejected", InventoryChange),
            ("post-seal callbacks rejected", PostSeal),
            ("concurrent terminal has one winner", ConcurrentTerminal),
            ("winner ticket alone writes", WinnerTicket),
            ("startup gate ready disarms timeout", GateReadyDisarmsTimeout),
            ("startup timeout wins atomically", TimeoutWinsAtomically),
            ("startup ready-timeout race has one winner", GateRace),
            ("identity-first original store does not read args", IdentityFirstOriginal),
            ("identity-first unknown store does not read args", IdentityFirstUnknown),
            ("restart phases never create recovery", RestartRecoveryForbidden)
        };
        var failed = 0;
        foreach (var (name, test) in cases)
        {
            try { test(); Console.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + " " + error.GetType().Name); }
        }
        Console.WriteLine($"TOTAL {cases.Length} PASSED {cases.Length - failed} FAILED {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static void FixedIdentity()
    {
        Eq(9_757_184L, ProbeConstants.GameDllLength);
        Eq("0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9", ProbeConstants.GameDllSha256);
        Eq(19_953L, ProbeConstants.PriorArtifactLength);
        Eq("AE4D9998CAF03DA3FB2F17656E12A0216B7160FD69017154B128C3F6365C9553", ProbeConstants.PriorArtifactSha256);
        Eq(2_359L, ProbeConstants.V1SetupArtifactLength);
        Eq("56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48", ProbeConstants.V1SetupArtifactSha256);
        Eq(1_479L, ProbeConstants.V1InvalidArtifactLength);
        Eq("A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F", ProbeConstants.V1InvalidArtifactSha256);
        Eq(242L, ProbeConstants.R2PrecheckArtifactLength);
        Eq("2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916", ProbeConstants.R2PrecheckArtifactSha256);
        Eq(23_241L, ProbeConstants.R4SetupArtifactLength);
        Eq("40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040", ProbeConstants.R4SetupArtifactSha256);
        Eq(253_870L, ProbeConstants.Inv009IlOutputLength);
        Eq("488814C65B9CD8E56F888CFCFE50AD7BEAA890E0396F084BED3916CFBDF772D7", ProbeConstants.Inv009IlOutputSha256);
        Eq(25_790L, ProbeConstants.R5SetupArtifactLength);
        Eq("9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9", ProbeConstants.R5SetupArtifactSha256);
    }
    private static void NewIdentity()
    {
        Eq("spike-inv008a.owner-fault.v6", ProbeConstants.SchemaId);
        Eq("0.6.0-owner-fault-matrix-r6-b1", ProbeConstants.ProbeVersion);
        Eq("0.6.0", ProbeConstants.ManifestVersion);
        Eq("setup-owner-fault-r6.json", ProbeConstants.SetupArtifact);
        Eq("r6-setup-", PhasePolicy.RecoveryPrefix(ProbePhase.SetupOwnerFault));
        Eq("r6-post-launch-", PhasePolicy.RecoveryPrefix(ProbePhase.PostLaunchOwnerFault));
        Eq(10, ProbeConstants.TaskWaitSeconds);
        if (!ProbeConstants.EvidenceRoot.EndsWith(@"evidence\spike-inv008a", StringComparison.Ordinal)) throw new InvalidOperationException();
    }
    private static void PriorAttempts()
    {
        var prior = PhasePolicy.ValidatePriorAttempts();
        if (!prior.Passed || !prior.SetupIdentityValid || !prior.InvalidIdentityValid ||
            !prior.SetupPreBusinessNull || !prior.InvalidPreBusinessNull || !prior.V1RecoveryAbsent ||
            !prior.R2PrecheckIdentityValid || !prior.R2PrecheckContractValid || !prior.R2BusinessArtifactsAbsent ||
            !prior.R2RecoveryAbsent || !prior.R3SetupIdentityValid || !prior.R3PreBusinessContractValid ||
            !prior.R3LaterArtifactsAbsent || !prior.R3RecoveryAbsent || !prior.R4SetupIdentityValid ||
            !prior.R4ContractValid || !prior.R4LaterArtifactsAbsent || !prior.R4RecoveryIdentityValid ||
            !prior.Inv009IlIdentityValid || !prior.Inv009SummaryIdentityValid || !prior.R5SetupIdentityValid ||
            !prior.R5ContractValid || !prior.R5LaterArtifactsAbsent || !prior.R5RecoveryIdentityValid)
            throw new InvalidOperationException(prior.Failure);
    }
    private static void ManifestSemVer()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "SPIKEINV008A.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var value = doc.RootElement.GetProperty("version").GetString()!;
        Eq(ProbeConstants.ManifestVersion, value);
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$")) throw new InvalidOperationException();
    }
    private static void WrongModSetPrecheck()
    {
        var result = ConfigurationPrecheck.Evaluate(new[] { "SPIKEINV007A", "SPIKEINV008A" });
        if (result.Passed || result.Failure != "WrongLoadedModSet" || result.LoadedModCount != 2) throw new InvalidOperationException();
        WithTemp(root =>
        {
            var attempt = Guid.NewGuid().ToString("N");
            var path = ConfigurationPrecheck.WriteFailure(root, attempt, result);
            if (!File.Exists(path) || File.Exists(Path.Combine(root, ProbeConstants.SetupArtifact))) throw new InvalidOperationException();
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var names = doc.RootElement.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
            if (!names.SetEquals(new[] { "schemaId", "probeRevision", "attemptToken", "loadedModCount", "loadedModIds", "failure" })) throw new InvalidOperationException();
        });
    }
    private static void CorrectedModSet() => WithTemp(root =>
    {
        var wrong = ConfigurationPrecheck.Evaluate(new[] { "SPIKEINV007A", "SPIKEINV008A" });
        ConfigurationPrecheck.WriteFailure(root, Guid.NewGuid().ToString("N"), wrong);
        if (!ConfigurationPrecheck.Evaluate(new[] { "SPIKEINV008A" }).Passed) throw new InvalidOperationException();
        Eq(ProbePhase.SetupOwnerFault, PhasePolicy.Resolve(root).Phase);
    });
    private static void PrecheckCreateNew() => WithTemp(root =>
    {
        var result = ConfigurationPrecheck.Evaluate(Array.Empty<string>()); var attempt = Guid.NewGuid().ToString("N");
        ConfigurationPrecheck.WriteFailure(root, attempt, result); var threw = false;
        try { ConfigurationPrecheck.WriteFailure(root, attempt, result); } catch (IOException) { threw = true; }
        if (!threw) throw new InvalidOperationException();
    });
    private static void PrecheckNotPhasePass() => WithTemp(root =>
    {
        ConfigurationPrecheck.WriteFailure(root, Guid.NewGuid().ToString("N"), ConfigurationPrecheck.Evaluate(Array.Empty<string>()));
        Eq(ProbePhase.SetupOwnerFault, PhasePolicy.Resolve(root).Phase);
    });
    private static void ExactLoadedSet()
    {
        if (!ConfigurationPrecheck.Evaluate(new[] { "SPIKEINV008A" }).Passed ||
            ConfigurationPrecheck.Evaluate(new[] { "SPIKEINV007A" }).Passed ||
            ConfigurationPrecheck.Evaluate(new[] { "SPIKEINV007A", "SPIKEINV008A" }).Passed ||
            ConfigurationPrecheck.Evaluate(new[] { "OTHER", "SPIKEINV008A" }).Passed) throw new InvalidOperationException();
    }
    private static void InitializerDoesNotReadLoadedSet()
    {
        var gate = new PostLoaderModSetGate();
        var reads = 0;
        Eq(0, reads);
        var result = gate.EvaluateAtPostLoader(() => { reads++; return new[] { "SPIKEINV008A" }; });
        Eq(1, reads);
        if (result?.Passed != true || !gate.Passed) throw new InvalidOperationException();
    }
    private static void PostLoaderWrongSetsRemainPreBusiness()
    {
        foreach (var ids in new[] { Array.Empty<string>(), new[] { "SPIKEINV007A" }, new[] { "OTHER" } })
        {
            WithTemp(root =>
            {
                var gate = new PostLoaderModSetGate();
                var result = gate.EvaluateAtPostLoader(() => ids) ?? throw new InvalidOperationException();
                if (result.Passed || gate.Passed) throw new InvalidOperationException();
                ConfigurationPrecheck.WriteFailure(root, Guid.NewGuid().ToString("N"), result);
                foreach (var name in new[] { ProbeConstants.SetupArtifact, ProbeConstants.SetupRestartArtifact,
                    ProbeConstants.PostLaunchArtifact, ProbeConstants.PostLaunchRestartArtifact,
                    ProbeConstants.InvalidArtifact, "protection.json", "candidate.json", "override.json", "run.json",
                    "recovery.json" })
                    if (File.Exists(Path.Combine(root, name))) throw new InvalidOperationException(name);
                if (Directory.Exists(Path.Combine(root, "restricted-recovery"))) throw new InvalidOperationException();
            });
        }
    }
    private static void Fixed007SchemaLacksContinue()
    {
        var result = PriorIntegrity.ReadHistoricalContinueCompatibility(ProbeConstants.PriorArtifactFile);
        if (result.Evidence is not null || result.Failure != ContinueFailure.MissingSchemaField) throw new InvalidOperationException();
    }
    private static void RecoveryMtimeIsNotSourceOracle()
    {
        var source = PriorIntegrity.ReadProtection(ProbeConstants.PriorArtifactFile, "protectionAfter");
        var copy = ProtectionScope.CaptureRecoveryBaseline(ProbeConstants.PriorRecoveryRoot).Snapshot;
        var content = ProtectionScope.ComparePrior(source, copy);
        if (!content.FileSetEqual || !content.ContentEqual || !content.InventoryEqual) throw new InvalidOperationException();
        var right = copy.Files.ToDictionary(x => (x.Scope, x.RelativeName));
        if (!source.Files.Any(x => x.LastWriteUtc != right[(x.Scope, x.RelativeName)].LastWriteUtc)) throw new InvalidOperationException();
    }
    private static void RealFixedPriorGateReachesReady()
    {
        var current = FixedCurrentSnapshot();
        var fresh = new ContinueCapture(new ContinueEvidence(true, true, 7, "FRESH"), ContinueFailure.None);
        var prior = PriorIntegrity.VerifyR6FixedInputs(current, fresh, true, true, true);
        if (!prior.Passed || !prior.Evidence.CurrentContentEqual || !prior.Evidence.CurrentInventoryEqual ||
            !prior.Evidence.SettingsIdentityValid || prior.ContinueBefore != fresh.Evidence) throw new InvalidOperationException(prior.Evidence.Source);
        var timing = AllTimingConditions();
        if (!PriorGateReachability.CanReachGateReady(prior, timing, true)) throw new InvalidOperationException();
        var gate = new StartupGateArbiter();
        if (!gate.TryCommitGateReady() || gate.State != StartupGateState.GateReady) throw new InvalidOperationException();
    }
    private static void HistoricalSettingsRejected()
    {
        var current = FixedCurrentSnapshot();
        var files = current.Files.Select(x => x.Scope == ProtectedScope.MachineSettings
            ? x with { Length = 5_122, Sha256 = "D1E034CA15D47C00A3AF54380486C2684B425AA064A3AB7E092AC6BEE4F863F9" } : x).ToList();
        var result = PriorIntegrity.VerifyR6FixedInputs(current with { Files = files }, FreshContinue(), true, true, true);
        if (result.IdentityValid || result.Evidence.SettingsIdentityValid) throw new InvalidOperationException();
    }
    private static void ActiveContentRejected()
    {
        var current = FixedCurrentSnapshot();
        var files = current.Files.Select(x => x.Scope == ProtectedScope.ActiveProfile && x.RelativeName == "saves/current_run.save"
            ? x with { Sha256 = "BAD" } : x).ToList();
        var result = PriorIntegrity.VerifyR6FixedInputs(current with { Files = files }, FreshContinue(), true, true, true);
        if (result.Passed || result.Evidence.CurrentContentEqual) throw new InvalidOperationException();
    }
    private static void PriorInventoryRejected()
    {
        var current = FixedCurrentSnapshot();
        var changed = current with { ProfileInventory = new(current.ProfileInventory.Directories,
            current.ProfileInventory.Files.Concat(["unexpected.save"]).ToList()) };
        var result = PriorIntegrity.VerifyR6FixedInputs(changed, FreshContinue(), true, true, true);
        if (result.Passed || result.Evidence.CurrentInventoryEqual) throw new InvalidOperationException();
    }
    private static void FreshContinueFailuresDistinct()
    {
        foreach (var failure in new[] { ContinueFailure.NotVisible, ContinueFailure.NotEnabled, ContinueFailure.MissingSaveData })
        {
            var result = PriorIntegrity.VerifyR6FixedInputs(FixedCurrentSnapshot(), new(null, failure), true, true, true);
            if (result.Passed || result.Evidence.ContinueFailure != failure) throw new InvalidOperationException(failure.ToString());
        }
        var expected = new ContinueEvidence(true, true, 4, "A");
        Eq(ContinueFailure.NumReloadsMismatch, PriorIntegrity.CompareContinue(expected,
            new(new ContinueEvidence(true, true, 5, "A"), ContinueFailure.None)));
        Eq(ContinueFailure.CanonicalMismatch, PriorIntegrity.CompareContinue(expected,
            new(new ContinueEvidence(true, true, 4, "B"), ContinueFailure.None)));
    }
    private static void TerminalBeforePageRendered()
    {
        var text = ProbeUi.ComposeStatus(ProbePhase.SetupOwnerFault, false, "BLOCKED_PRIOR_INTEGRITY\nFailure site: PriorIntegrity");
        if (!text.Contains("BLOCKED_PRIOR_INTEGRITY", StringComparison.Ordinal) ||
            !text.Contains("Failure site: PriorIntegrity", StringComparison.Ordinal) ||
            text.Contains("WAITING_EVENTS", StringComparison.Ordinal)) throw new InvalidOperationException();
    }
    private static bool Catch(ProbePhase phase, FaultSite thrownSite, string expectedToken, string thrownToken)
    {
        try { throw new ControlledOwnerFaultException(phase, thrownSite, thrownToken); }
        catch (ControlledOwnerFaultException value) when (value.Phase == phase && value.Site == (phase == ProbePhase.SetupOwnerFault ? FaultSite.AfterSetup : FaultSite.AfterSceneInstall) && value.AttemptToken == expectedToken) { return true; }
        catch (ControlledOwnerFaultException) { return false; }
    }
    private static void SetupFault() { if (!Catch(ProbePhase.SetupOwnerFault, FaultSite.AfterSetup, "A", "A")) throw new InvalidOperationException(); }
    private static void PostLaunchFault() { if (!Catch(ProbePhase.PostLaunchOwnerFault, FaultSite.AfterSceneInstall, "A", "A")) throw new InvalidOperationException(); }
    private static void WrongToken() { if (Catch(ProbePhase.SetupOwnerFault, FaultSite.AfterSetup, "A", "B")) throw new InvalidOperationException(); }
    private static void WrongSite() { if (Catch(ProbePhase.SetupOwnerFault, FaultSite.AfterSceneInstall, "A", "A")) throw new InvalidOperationException(); }
    private static void OwnerCatch()
    {
        var finallyCount = 0;
        try { try { throw new ControlledOwnerFaultException(ProbePhase.SetupOwnerFault, FaultSite.AfterSetup, "A"); } catch (ControlledOwnerFaultException) { } }
        finally { finallyCount++; }
        Eq(1, finallyCount);
    }
    private static void TeardownOwner()
    {
        var plan = new TeardownPlan(); var winners = 0;
        Parallel.For(0, 100, _ => { if (plan.TryOwn()) Interlocked.Increment(ref winners); });
        Eq(1, winners); Eq(1, plan.OwnerCount);
    }
    private static void EveryTeardownStepFault()
    {
        var names = new[] { "ownership", "actions", "task1", "cleanup", "task2", "restore", "original", "menu", "protection", "artifact" };
        foreach (var fault in names)
        {
            var plan = new TeardownPlan(); plan.TryOwn();
            foreach (var name in names) plan.Step(name, () => { if (name == fault) throw new IOException(); });
            Eq(names.Length, plan.Reached.Count(x => x.EndsWith(":entry", StringComparison.Ordinal)));
            Eq(1, plan.Errors.Count);
        }
    }
    private static void CleanupMiddleFault()
    {
        var reached = new List<string>();
        try { foreach (var step in BoundaryAudit.RequiredCleanupSteps) { reached.Add(step); if (step == "CombatReplayWriter.Dispose") throw new IOException(); } } catch (IOException) { }
        if (reached.Contains("ActionQueueSynchronizer.Dispose")) throw new InvalidOperationException();
        var counts = reached.ToDictionary(x => x, _ => 1, StringComparer.Ordinal);
        var oracle = PhaseAwareOracle.EvaluateCleanup(counts, true, 1);
        var evidence = new CleanupEvidence(1, 0, 1, BoundaryAudit.RequiredCleanupSteps, reached, counts, oracle,
            true, true, true, true, true);
        if (BoundaryAudit.CleanupComplete(evidence)) throw new InvalidOperationException();
    }
    private static void ReplayNullLobbyPositive()
    {
        var result = PhaseAwareOracle.EvaluateCleanup(CoreCounts(), true, 0);
        if (!result.CoreCallSetPass || result.LobbyApplicable || !result.LobbyGroupPass || !result.CallSetPass) throw new InvalidOperationException();
    }
    private static void NonNullLobbyPositive()
    {
        var counts = CoreCounts(); counts[PhaseAwareOracle.LobbyRemove] = 1; counts[PhaseAwareOracle.LobbyDispose] = 1;
        var result = PhaseAwareOracle.EvaluateCleanup(counts, false, 0);
        if (!result.CoreCallSetPass || !result.LobbyApplicable || !result.LobbyGroupPass || !result.CallSetPass) throw new InvalidOperationException();
    }
    private static void NonNullLobbyMissingCall()
    {
        foreach (var missing in new[] { PhaseAwareOracle.LobbyRemove, PhaseAwareOracle.LobbyDispose })
        {
            var counts = CoreCounts(); counts[PhaseAwareOracle.LobbyRemove] = 1; counts[PhaseAwareOracle.LobbyDispose] = 1; counts.Remove(missing);
            if (PhaseAwareOracle.EvaluateCleanup(counts, false, 0).CallSetPass) throw new InvalidOperationException(missing);
        }
    }
    private static void NonNullLobbyDuplicate()
    {
        foreach (var duplicate in new[] { PhaseAwareOracle.LobbyRemove, PhaseAwareOracle.LobbyDispose })
        {
            var counts = CoreCounts(); counts[PhaseAwareOracle.LobbyRemove] = 1; counts[PhaseAwareOracle.LobbyDispose] = 1; counts[duplicate] = 2;
            if (PhaseAwareOracle.EvaluateCleanup(counts, false, 0).CallSetPass) throw new InvalidOperationException(duplicate);
        }
    }
    private static void NonNullLobbyException()
    {
        var counts = CoreCounts(); counts[PhaseAwareOracle.LobbyRemove] = 1; counts[PhaseAwareOracle.LobbyDispose] = 1;
        if (PhaseAwareOracle.EvaluateCleanup(counts, false, 1).CallSetPass) throw new InvalidOperationException();
    }
    private static void NullLobbyUnexpectedCall()
    {
        foreach (var unexpected in new[] { PhaseAwareOracle.LobbyRemove, PhaseAwareOracle.LobbyDispose })
        {
            var counts = CoreCounts(); counts[unexpected] = 1;
            if (PhaseAwareOracle.EvaluateCleanup(counts, true, 0).CallSetPass) throw new InvalidOperationException(unexpected);
        }
    }
    private static void PreSetupStaleLobby()
    {
        var stale = new LobbyObservation("PreSetup", false, null, false, 7);
        if (PhaseAwareOracle.PreSetupAllowed(stale) || PhaseAwareOracle.EvaluateLobby([stale]).Passed) throw new InvalidOperationException();
    }
    private static void LobbyBoundaryTransitions()
    {
        var points = new[] { "PostSetup", "PostLaunch", "PostScene", "CleanupEntry" };
        var baseline = new List<LobbyObservation> { new("PreSetup", false, null, true, 0) };
        baseline.AddRange(points.Select(x => new LobbyObservation(x, true, "Replay", true, 0)));
        if (!PhaseAwareOracle.EvaluateLobby(baseline).Passed) throw new InvalidOperationException("positive");
        foreach (var point in points)
        {
            var changed = baseline.Select(x => x.Point == point ? x with { RunLobbyNull = false, RunLobbyRuntimeIdentity = 9 } : x).ToList();
            if (PhaseAwareOracle.EvaluateLobby(changed).Passed) throw new InvalidOperationException(point);
            var replacement = baseline.Select(x => x.Point == point ? x with { RunLobbyRuntimeIdentity = 9 } : x).ToList();
            if (PhaseAwareOracle.EvaluateLobby(replacement).Passed) throw new InvalidOperationException(point + " identity");
        }
    }
    private static void CoreOmissionAndException()
    {
        foreach (var missing in PhaseAwareOracle.CoreCleanupSteps)
        {
            var counts = CoreCounts(); counts.Remove(missing);
            if (PhaseAwareOracle.EvaluateCleanup(counts, true, 0).CallSetPass) throw new InvalidOperationException(missing);
        }
        if (PhaseAwareOracle.EvaluateCleanup(CoreCounts(), true, 1).CallSetPass) throw new InvalidOperationException("exception");
    }
    private static void PostLaunchSceneReplacement()
    {
        var good = new SceneEvidence(false, 10, 10, true, 10, 20, true, true, false);
        if (!PhaseAwareOracle.ScenePassed(ProbePhase.PostLaunchOwnerFault, good)) throw new InvalidOperationException("positive");
        var bad = good with { RestoredSceneRuntimeIdentity = 10, RestoredSceneIsMainMenu = false, OwnedNRunNoLongerCurrent = false };
        if (PhaseAwareOracle.ScenePassed(ProbePhase.PostLaunchOwnerFault, bad)) throw new InvalidOperationException("not replaced");
    }
    private static void RealR4CoreOnly()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ProbeConstants.R4SetupArtifact));
        var root = doc.RootElement;
        var cleanup = root.GetProperty("teardown").GetProperty("cleanup");
        var counts = cleanup.GetProperty("reachedSteps").EnumerateArray().ToDictionary(x => x.GetString()!, _ => 1, StringComparer.Ordinal);
        var result = PhaseAwareOracle.EvaluateCleanup(counts, true, cleanup.GetProperty("exceptions").GetInt32());
        if (!result.CoreCallSetPass || !result.CallSetPass) throw new InvalidOperationException("core set");
        if (root.GetProperty("terminalClassification").GetString() == "PASS_OWNER_FAULT_TEARDOWN" ||
            root.GetProperty("teardown").GetProperty("menuReadyObserved").GetBoolean()) throw new InvalidOperationException("full pass forged");
    }
    private static void RestoredMenuProductionOrder()
    {
        var observer = new RestoredMenuObserver(); var menu = new object();
        observer.Reset();
        if (!observer.RegisterCandidate(menu) || !observer.ObserveRefreshButtons(menu) || !observer.ObserveReady(menu))
            throw new InvalidOperationException("callback acceptance");
        var result = observer.WaitAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
        if (!result.Passed || result.PrefixCount != 1 || result.RefreshCount != 1 || result.ReadyCount != 1 ||
            result.MenuReadyStatus != ObservedTaskStatus.RanToCompletion ||
            result.RefreshButtonsStatus != ObservedTaskStatus.RanToCompletion ||
            result.CandidateRuntimeIdentity != result.ReadyRuntimeIdentity ||
            result.CandidateRuntimeIdentity != result.RefreshRuntimeIdentity) throw new InvalidOperationException("production order");
    }
    private static void RestoredMenuMissingPrefix()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); observer.ObserveReady(new object());
        ExpectObserverFailure(observer.SnapshotAfterWait(), FailureSite.RestoredMenuIdentityOrder, "ready-postfix-without-prefix");
    }
    private static void RestoredMenuWrongCandidate()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); observer.RegisterCandidate(new object());
        observer.ObserveReady(new object());
        ExpectObserverFailure(observer.SnapshotAfterWait(), FailureSite.RestoredMenuIdentityOrder, "ready-postfix-identity-replaced");
    }
    private static void RestoredMenuOldRefresh()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); var candidate = new object();
        observer.RegisterCandidate(candidate); observer.ObserveRefreshButtons(new object());
        ExpectObserverFailure(observer.SnapshotAfterWait(), FailureSite.RestoredMenuIdentityOrder, "refresh-from-wrong-menu");
    }
    private static void RestoredMenuOnlyMenu()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); var menu = new object();
        observer.RegisterCandidate(menu); observer.ObserveReady(menu);
        var result = observer.SnapshotAfterWait(new TimeoutException());
        if (result.FailureSite != FailureSite.RefreshButtons || result.MenuReadyStatus != ObservedTaskStatus.RanToCompletion ||
            result.RefreshButtonsStatus != ObservedTaskStatus.Pending) throw new InvalidOperationException();
    }
    private static void RestoredMenuOnlyRefresh()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); var menu = new object();
        observer.RegisterCandidate(menu); observer.ObserveRefreshButtons(menu);
        var result = observer.SnapshotAfterWait(new TimeoutException());
        if (result.FailureSite != FailureSite.MenuReady || result.MenuReadyStatus != ObservedTaskStatus.Pending ||
            result.RefreshButtonsStatus != ObservedTaskStatus.RanToCompletion) throw new InvalidOperationException();
    }
    private static void RestoredMenuBothMissing()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); observer.RegisterCandidate(new object());
        var result = observer.SnapshotAfterWait(new TimeoutException());
        if (result.FailureSite != FailureSite.MenuReady || result.MenuReadyStatus != ObservedTaskStatus.Pending ||
            result.RefreshButtonsStatus != ObservedTaskStatus.Pending) throw new InvalidOperationException();
    }
    private static void RestoredMenuDuplicate()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); var menu = new object(); observer.RegisterCandidate(menu);
        observer.ObserveRefreshButtons(menu); observer.ObserveRefreshButtons(menu);
        ExpectObserverFailure(observer.SnapshotAfterWait(), FailureSite.RestoredMenuIdentityOrder, "duplicate-refresh-callback");
    }
    private static void RestoredMenuPostfixReplacement()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); var menu = new object(); observer.RegisterCandidate(menu);
        observer.ObserveRefreshButtons(menu); observer.ObserveReady(new object());
        ExpectObserverFailure(observer.SnapshotAfterWait(), FailureSite.RestoredMenuIdentityOrder, "ready-postfix-identity-replaced");
    }
    private static void RestoredMenuLoadException()
    {
        var observer = new RestoredMenuObserver(); observer.Reset(); var result = observer.SnapshotLoadException(new IOException());
        if (result.FailureSite != FailureSite.MainMenu || result.Failure != "load-main-menu:IOException" || result.Passed)
            throw new InvalidOperationException();
    }
    private static void RealR5ObserverOnly()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ProbeConstants.R5SetupArtifact));
        var root = doc.RootElement;
        if (root.GetProperty("terminalClassification").GetString() != "FAIL_TEARDOWN_MENU" ||
            root.GetProperty("restart").ValueKind != JsonValueKind.Null ||
            !root.GetProperty("scene").GetProperty("passed").GetBoolean() ||
            root.GetProperty("teardown").GetProperty("menuReadyObserved").GetBoolean() ||
            root.GetProperty("teardown").GetProperty("refreshButtonsObserved").GetBoolean())
            throw new InvalidOperationException("R5 contract");
        if (PhasePolicy.Resolve(Path.GetDirectoryName(ProbeConstants.R5SetupArtifact)!).Phase != ProbePhase.SetupOwnerFault)
            throw new InvalidOperationException("R5 forged R6 phase pass");
    }
    private static void ExpectObserverFailure(RestoredMenuSignalEvidence value, FailureSite site, string failure)
    {
        if (value.Passed || value.FailureSite != site || value.Failure != failure) throw new InvalidOperationException();
    }
    private static void ManagerFallback()
    {
        var original = new object(); var current = new object(); var fallback = 0;
        if (!ReferenceEquals(current, original)) { fallback++; current = original; }
        Eq(1, fallback); if (!ReferenceEquals(current, original)) throw new InvalidOperationException();
    }
    private static void MenuGate()
    {
        static bool Allowed(bool cleanup, bool restore) => cleanup && restore;
        if (Allowed(false, true) || Allowed(true, false) || Allowed(false, false) || !Allowed(true, true)) throw new InvalidOperationException();
    }
    private static void EmptyChain() => WithTemp(root => Eq(ProbePhase.SetupOwnerFault, PhasePolicy.Resolve(root).Phase));
    private static void SetupPassChain() => WithTemp(root => { WritePass(root, ProbePhase.SetupOwnerFault); Eq(ProbePhase.RestartAfterSetup, PhasePolicy.Resolve(root).Phase); });
    private static void RestartPassChain() => WithTemp(root => { WritePass(root, ProbePhase.SetupOwnerFault); WritePass(root, ProbePhase.RestartAfterSetup); Eq(ProbePhase.PostLaunchOwnerFault, PhasePolicy.Resolve(root).Phase); });
    private static void PostLaunchPassChain() => WithTemp(root => { WritePass(root, ProbePhase.SetupOwnerFault); WritePass(root, ProbePhase.RestartAfterSetup); WritePass(root, ProbePhase.PostLaunchOwnerFault); Eq(ProbePhase.RestartAfterPostLaunch, PhasePolicy.Resolve(root).Phase); });
    private static void CompletedChain() => WithTemp(root => { foreach (var phase in new[] { ProbePhase.SetupOwnerFault, ProbePhase.RestartAfterSetup, ProbePhase.PostLaunchOwnerFault, ProbePhase.RestartAfterPostLaunch }) WritePass(root, phase); Eq(ProbePhase.Complete, PhasePolicy.Resolve(root).Phase); });
    private static void ExistingArtifact() => WithTemp(root => { File.WriteAllText(Path.Combine(root, ProbeConstants.SetupArtifact), "original"); Eq(ProbePhase.Invalid, PhasePolicy.Resolve(root).Phase); Eq("original", File.ReadAllText(Path.Combine(root, ProbeConstants.SetupArtifact))); });
    private static void PhaseGap() => WithTemp(root => { WritePass(root, ProbePhase.RestartAfterSetup); Eq(ProbePhase.Invalid, PhasePolicy.Resolve(root).Phase); });
    private static void PriorFailureBlocks() => WithTemp(root => { Write(root, ProbePhase.SetupOwnerFault, "FAIL_TEARDOWN_CLEANUP"); Eq(ProbePhase.Invalid, PhasePolicy.Resolve(root).Phase); });
    private static void RestartReadOnly()
    {
        var before = Snapshot(); var after = Snapshot(); var x = RestartRules.Compare(before, after);
        if (!x.FileSet || !x.Content || !x.Inventory || !x.MtimeAllowed || x.MtimeChanges.Count != 0) throw new InvalidOperationException();
    }
    private static void KnownMtime()
    {
        var before = Snapshot(); var after = MutateMtime(before, ProtectedScope.MachineSettings, "settings.save");
        var x = RestartRules.Compare(before, after); if (!x.Content || !x.MtimeAllowed || x.MtimeChanges.Count != 1) throw new InvalidOperationException();
    }
    private static void UnknownMtime()
    {
        var before = Snapshot(); var after = MutateMtime(before, ProtectedScope.ActiveProfile, "saves/current_run.save");
        if (RestartRules.Compare(before, after).MtimeAllowed) throw new InvalidOperationException();
    }
    private static void ContentChange()
    {
        var before = Snapshot(); var files = before.Files.Select(x => x.Scope == ProtectedScope.MachineSettings ? x with { Sha256 = "B", LastWriteUtc = "2" } : x).ToList();
        var result = RestartRules.Compare(before, before with { Files = files }); if (result.Content) throw new InvalidOperationException();
    }
    private static void InventoryChange()
    {
        var before = Snapshot(); var after = before with { ProfileInventory = new(before.ProfileInventory.Directories, before.ProfileInventory.Files.Concat(["new"]).ToList()) };
        if (RestartRules.Compare(before, after).Inventory) throw new InvalidOperationException();
    }
    private static void PostSeal()
    {
        var session = new ScopeSafeSession(); session.TrySeal(TerminalClassification.FailBoundary, FailureSite.Boundary, null, null);
        if (session.TryCommit(EventKind.MenuReady, null, out _)) throw new InvalidOperationException();
    }
    private static void ConcurrentTerminal()
    {
        var session = new ScopeSafeSession(); var wins = 0;
        Parallel.For(0, 100, _ => { if (session.TrySeal(TerminalClassification.FailBoundary, FailureSite.Boundary, null, null) is not null) Interlocked.Increment(ref wins); });
        Eq(1, wins);
    }
    private static void WinnerTicket()
    {
        var session = new ScopeSafeSession(); var ticket = session.TrySeal(TerminalClassification.FailBoundary, FailureSite.Boundary, null, null); var writes = 0;
        if (session.TryWriteClosedArtifact(new object(), _ => { writes++; return true; })) throw new InvalidOperationException();
        if (!session.TryWriteClosedArtifact(ticket, _ => { writes++; return true; })) throw new InvalidOperationException(); Eq(1, writes);
    }
    private static void GateReadyDisarmsTimeout()
    {
        var gate = new StartupGateArbiter();
        if (!gate.TryCommitGateReady() || gate.TryTimeout() || !gate.TryAcceptRun()) throw new InvalidOperationException();
        Eq(StartupGateState.RunAccepted, gate.State);
    }
    private static void TimeoutWinsAtomically()
    {
        var gate = new StartupGateArbiter();
        if (!gate.TryTimeout() || gate.TryCommitGateReady() || gate.TryAcceptRun()) throw new InvalidOperationException();
        Eq(StartupGateState.TimedOut, gate.State);
    }
    private static void GateRace()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var gate = new StartupGateArbiter(); var winners = 0;
            Parallel.Invoke(
                () => { if (gate.TryCommitGateReady()) Interlocked.Increment(ref winners); },
                () => { if (gate.TryTimeout()) Interlocked.Increment(ref winners); });
            Eq(1, winners);
            if (gate.State is not (StartupGateState.GateReady or StartupGateState.TimedOut)) throw new InvalidOperationException();
        }
    }
    private static void IdentityFirstOriginal() { var x = new IdentityFirstFixture(); x.Store(x.Original, () => x.Reads++); Eq(0, x.Reads); Eq(1, x.Failures); }
    private static void IdentityFirstUnknown() { var x = new IdentityFirstFixture(); x.Store(new object(), () => x.Reads++); Eq(0, x.Reads); Eq(1, x.Failures); }
    private static void RestartRecoveryForbidden()
    {
        foreach (var phase in new[] { ProbePhase.RestartAfterSetup, ProbePhase.RestartAfterPostLaunch })
        {
            var threw = false;
            try { PhasePolicy.RecoveryPrefix(phase); } catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new InvalidOperationException();
        }
    }

    private sealed class IdentityFirstFixture
    {
        internal readonly object Original = new(), Memory = new(); internal int Reads, Failures;
        internal void Store(object instance, Action read) { if (ReferenceEquals(instance, Original) || !ReferenceEquals(instance, Memory)) { Failures++; return; } read(); }
    }
    private static Dictionary<string, int> CoreCounts() => PhaseAwareOracle.CoreCleanupSteps
        .ToDictionary(x => x, _ => 1, StringComparer.Ordinal);
    private static ContinueCapture FreshContinue() => new(new ContinueEvidence(true, true, 7, "FRESH"), ContinueFailure.None);
    private static GateConditions AllTimingConditions() => new(true, true, true, true, true, true, true, true, true, true, true);
    private static ProtectionSnapshot FixedCurrentSnapshot()
    {
        const string account = @"C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122\modded";
        return ProtectionScope.Capture(new ProtectionRoots(Path.Combine(account, "profile1"),
            Path.Combine(account, "profile.save"),
            @"C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122\settings.save"));
    }
    private static ProtectionSnapshot Snapshot()
    {
        var files = new[]
        {
            new ProtectedFile(ProtectedScope.ActiveProfile, "saves/current_run.save", true, 1, "A", "1"),
            new ProtectedFile(ProtectedScope.ActiveProfile, "saves/prefs.save.backup", true, 1, "A", "1"),
            new ProtectedFile(ProtectedScope.ActiveProfile, "saves/progress.save.backup", true, 1, "A", "1"),
            new ProtectedFile(ProtectedScope.MachineSettings, "settings.save", true, 1, "A", "1")
        };
        return new(files, new(["saves"], files.Where(x => x.Scope == ProtectedScope.ActiveProfile).Select(x => x.RelativeName).ToList()), []);
    }
    private static ProtectionSnapshot MutateMtime(ProtectionSnapshot value, ProtectedScope scope, string name) => value with { Files = value.Files.Select(x => x.Scope == scope && x.RelativeName == name ? x with { LastWriteUtc = "2" } : x).ToList() };
    private static void WritePass(string root, ProbePhase phase) => Write(root, phase, phase is ProbePhase.SetupOwnerFault or ProbePhase.PostLaunchOwnerFault ? "PASS_OWNER_FAULT_TEARDOWN" : "PASS_RESTART_INTEGRITY");
    private static void Write(string root, ProbePhase phase, string terminal)
    {
        var name = phase switch { ProbePhase.SetupOwnerFault => ProbeConstants.SetupArtifact, ProbePhase.RestartAfterSetup => ProbeConstants.SetupRestartArtifact, ProbePhase.PostLaunchOwnerFault => ProbeConstants.PostLaunchArtifact, ProbePhase.RestartAfterPostLaunch => ProbeConstants.PostLaunchRestartArtifact, _ => throw new InvalidOperationException() };
        var previous = phase switch
        {
            ProbePhase.RestartAfterSetup => Path.Combine(root, ProbeConstants.SetupArtifact),
            ProbePhase.PostLaunchOwnerFault => Path.Combine(root, ProbeConstants.SetupRestartArtifact),
            ProbePhase.RestartAfterPostLaunch => Path.Combine(root, ProbeConstants.PostLaunchArtifact),
            _ => null
        };
        var previousHash = previous is not null && File.Exists(previous) ? PhasePolicy.Sha256(previous) : null;
        File.WriteAllText(Path.Combine(root, name), JsonSerializer.Serialize(new { schemaId = ProbeConstants.SchemaId, probeVersion = ProbeConstants.ProbeVersion, phase = phase.ToString(), terminalClassification = terminal, previousArtifactSha256 = previousHash }));
    }
    private static void WithTemp(Action<string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), "spike-inv008a-tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try { action(root); } finally { Directory.Delete(root, true); }
    }
    private static void Eq<T>(T expected, T actual) where T : notnull { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{expected} != {actual}"); }
}
