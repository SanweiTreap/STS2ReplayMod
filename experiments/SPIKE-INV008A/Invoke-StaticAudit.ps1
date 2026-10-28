param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$assembly = Join-Path $root "bin\$Configuration\net9.0\SPIKEINV008A.dll"
$sources = @(Get-ChildItem -LiteralPath $root -Filter '*.cs' -File)
$joined = ($sources | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
$transaction = Get-Content -Raw -LiteralPath (Join-Path $root 'OwnerFaultTransaction.cs')
$controller = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeController.cs')
$models = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeModels.cs')
$phasePolicy = Get-Content -Raw -LiteralPath (Join-Path $root 'PhasePolicy.cs')
$audit = Get-Content -Raw -LiteralPath (Join-Path $root 'BoundaryAudit.cs')
$ui = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeUi.cs')
$precheck = Get-Content -Raw -LiteralPath (Join-Path $root 'ConfigurationPrecheck.cs')
$prior = Get-Content -Raw -LiteralPath (Join-Path $root 'PriorIntegrity.cs')
$oracle = Get-Content -Raw -LiteralPath (Join-Path $root 'PhaseAwareOracle.cs')
$observer = Get-Content -Raw -LiteralPath (Join-Path $root 'RestoredMenuObserver.cs')
$entry = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeEntryPoint.cs')
$manifest = Get-Content -Raw -LiteralPath (Join-Path $root 'SPIKEINV008A.json') | ConvertFrom-Json

function Assert-FrozenSource([string]$Name, [long]$Length, [string]$Sha256) {
    $path = Join-Path $root $Name; $item = Get-Item -LiteralPath $path
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($item.Length -ne $Length -or $hash -ne $Sha256) { throw "R6 changed frozen source: $Name" }
}
foreach ($frozen in @(
    @('JsonSupport.cs',1995,'EC64CF60A589CE54D97DA2CEC89B0F5AB4251CF6041C4935A0B5749481CAEFE6'),
    @('LiveSeedCandidate.cs',15613,'A26E4A7DCC0D986E987A5EC813C9EC75EB1938C63EF591EFEDFA2B535A17BCFF'),
    @('ProtectionScope.cs',14945,'133BBF94783EF2E057E3032D41EF5238B8FDDDFE7FC057B4C6F232491B2D851F'),
    @('PhaseAwareOracle.cs',4003,'9129CC9F252D661DE1F1692A89CDC2DDA75E4C4FFBC9789963481F48683386B6')
)) { Assert-FrozenSource $frozen[0] $frozen[1] $frozen[2] }

foreach ($term in @('SPIKEINV003A','SpikeInv003A','SPIKEINV007A','SpikeInv007A','PhaseSelector','phase-control.json')) {
    if ($joined.Contains($term, [StringComparison]::Ordinal)) { throw "forbidden old-probe/selector production surface: $term" }
}
foreach ($term in @('Environment.FailFast','Process.Kill','Kill(','NativeCrash','AccessViolationException','StackOverflowException')) {
    if ($joined.Contains($term, [StringComparison]::Ordinal)) { throw "forbidden crash/kill production surface: $term" }
}
if (-not $models.Contains('spike-inv008a.owner-fault.v6', [StringComparison]::Ordinal) -or
    -not $models.Contains('0.6.0-owner-fault-matrix-r6-b1', [StringComparison]::Ordinal) -or $manifest.version -ne '0.6.0') {
    throw 'schema/version identity surface missing'
}
if ($manifest.version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') { throw 'manifest version is not valid release SemVer' }
foreach ($name in @('setup-owner-fault-r6.json','restart-after-setup-r6.json','post-launch-owner-fault-r6.json','restart-after-post-launch-r6.json')) {
    if (-not $models.Contains($name, [StringComparison]::Ordinal)) { throw "fixed phase artifact missing: $name" }
}
foreach ($required in @('V1SetupArtifactSha256','V1InvalidArtifactSha256','R2PrecheckArtifactSha256','R3SetupArtifactSha256','R4SetupArtifactSha256','R4RecoveryAggregateSha256','Inv009IlOutputSha256','Inv009SummarySha256','R4Contract','R4LaterArtifactsAbsent','R4RecoveryIdentityValid','R5SetupArtifactSha256','R5RecoveryAggregateSha256','R5Contract','R5LaterArtifactsAbsent','R5RecoveryIdentityValid','BLOCKED_PRIOR_ATTEMPT_UNRESOLVED')) {
    if (-not $joined.Contains($required, [StringComparison]::Ordinal)) { throw "R6 prior-attempt surface missing: $required" }
}
foreach ($required in @('MaxRecordedIds = 32','MaxRecordedIdLength = 128','precheck-modset-r6-','FileMode.CreateNew','WrongLoadedModSet','TooManyLoadedMods','PostLoaderModSetGate')) {
    if (-not $precheck.Contains($required, [StringComparison]::Ordinal)) { throw "bounded precheck surface missing: $required" }
}
foreach ($required in @('R4SettingsLength','R4SettingsSha256','VerifyR6FixedInputs','ComparableHistoricalContentEqual','CurrentSettingsIdentityValid','ContinueFailure.MissingSchemaField','PriorGateReachability')) {
    if (-not $joined.Contains($required, [StringComparison]::Ordinal)) { throw "R6 baseline/Continue surface missing: $required" }
}
if ($prior -match 'RestartRules\.Compare\(recovery\.Snapshot' -or $prior -match 'RestartRules\.Compare\(recoveryBaseline\.Snapshot') { throw 'recovery-copy mtime is still used as source oracle' }
if ($transaction.IndexOf('ProtectionScope.CaptureAndBackup', [StringComparison]::Ordinal) -ge $transaction.IndexOf('LiveSeedCandidate.CaptureLive', [StringComparison]::Ordinal) -or
    $transaction.IndexOf('ProtectionScope.CaptureAndBackup', [StringComparison]::Ordinal) -ge $transaction.IndexOf('CaptureOriginalCanonical(original)', [StringComparison]::Ordinal)) { throw 'fresh phase baseline is not first before candidate/original serialization' }
foreach($required in @('ContinueEvidence? ContinueBefore','ContinueNotVisible','ContinueNotEnabled','ContinueMissingSaveData','ContinueNumReloadsMismatch','ContinueCanonicalMismatch')){if(-not$models.Contains($required,[StringComparison]::Ordinal)){throw "R4 Continue artifact/classification missing: $required"}}
foreach($required in @('CoreCleanupSteps','LobbyRemove','LobbyDispose','EvaluateCleanup','PreSetupAllowed','EvaluateLobby','ScenePassed')){if(-not$oracle.Contains($required,[StringComparison]::Ordinal)){throw "R5 phase-aware oracle surface missing: $required"}}
foreach($required in @('LobbyObservation','LobbyOracleEvidence','CleanupCallOracleEvidence','SceneEvidence','BlockedPreconditionStaleRunLobby','FailTeardownScene','RunLobbyBoundary','SceneReplacement')){if(-not$models.Contains($required,[StringComparison]::Ordinal)){throw "R5 artifact/classification surface missing: $required"}}
if([regex]::Matches($oracle,'"ActionQueueSet.Reset"|"CardSelectCmd.Reset"|"CombatManager.Reset"|"ActionExecutor.remove_JustBeforeActionFinishedExecuting"|"CombatReplayWriter.Dispose"|"ActionQueueSynchronizer.Dispose"|"PlayerChoiceSynchronizer.Dispose"|"RewardSynchronizer.Dispose"|"RewardsSetSynchronizer.Dispose"|"RestSiteSynchronizer.Dispose"|"FlavorSynchronizer.Dispose"|"ChecksumTracker.Dispose"|"INetGameService.Disconnect"').Count-ne13){throw 'R5 core cleanup cardinality mismatch'}
if(-not$oracle.Contains('lobbyApplicable ? remove == 1 && dispose == 1 : remove == 0 && dispose == 0',[StringComparison]::Ordinal)){throw 'RunLobby conditional pair oracle missing'}
if(-not$audit.Contains('Dictionary<string, int> CleanupCounts',[StringComparison]::Ordinal)-or$audit.Contains('HashSet<string> CleanupReached',[StringComparison]::Ordinal)){throw 'cleanup exact-call counter surface missing'}
if($transaction -notmatch '(?s)CaptureLobby\("PreSetup"\).*?PreSetupAllowed\(preSetup\).*?RunAsync\(phase, original, baseline, attempt, preSetup\)' -or
   $transaction -notmatch '(?s)CaptureAndBackup.*?BoundaryAudit\.Start\(original, candidate, memory, preSetup\).*?SetUpReplay.*?ObserveLobby\("PostSetup"\).*?Launch\(\).*?ObserveLobby\("PostLaunch"\).*?SetCurrentScene.*?ObserveLobby\("PostScene"\)') { throw 'R5 RunLobby observation/order surface missing' }
if($transaction.IndexOf('CaptureLobby("PreSetup")',[StringComparison]::Ordinal)-ge$transaction.IndexOf('CaptureAndBackup',[StringComparison]::Ordinal)){throw 'pre-setup stale-lobby gate is not before recovery'}
foreach($required in @('ownedRunScene','cleanupEntrySceneIdentity','RestoredSceneIsMainMenu','OwnedNRunNoLongerCurrent','SceneReplacementVerified','FailTeardownScene')){if(-not$joined.Contains($required,[StringComparison]::Ordinal)){throw "R5 scene ownership surface missing: $required"}}
foreach($required in @('RestoredMenuSignalEvidence','CandidateRuntimeIdentity','ReadyRuntimeIdentity','RefreshRuntimeIdentity','MenuReadyStatus','RefreshButtonsStatus','RestoredMenuIdentityOrder')){if(-not$models.Contains($required,[StringComparison]::Ordinal)){throw "R6 independent menu-signal evidence missing: $required"}}
foreach($required in @('RegisterCandidate','ObserveRefreshButtons','ObserveReady','SnapshotAfterWait','SnapshotLoadException','Task.WhenAny','WaitAsync(timeout)')){if(-not$observer.Contains($required,[StringComparison]::Ordinal)){throw "R6 restored-menu observer surface missing: $required"}}
if($observer.Contains('Task.Delay',[StringComparison]::Ordinal)-or$observer.Contains('while (',[StringComparison]::Ordinal)-or$observer.Contains('PeriodicTimer',[StringComparison]::Ordinal)){throw 'R6 observer uses forbidden delay/polling'}
if($models -notmatch 'TaskWaitSeconds = 10;' -or [regex]::Matches($models,'TaskWaitSeconds').Count-ne1){throw 'R6 changed fixed 10-second task wait'}
if($entry -notmatch '(?s)MainMenuReadyPatch.*?HarmonyPrefix.*?OnMainMenuReadyPrefix.*?HarmonyPostfix.*?OnMainMenuReady'){throw 'R6 _Ready prefix/postfix observer patches missing'}
$readyPrefix = [regex]::Match($controller, '(?s)internal static void OnMainMenuReadyPrefix\(NMainMenu menu\).*?\n    internal static void OnMainMenuReady').Value
if(-not [regex]::IsMatch($readyPrefix,'OwnerFaultTransaction\.Active.*?RestoredMenu\.RegisterCandidate\(menu\)',[Text.RegularExpressions.RegexOptions]::Singleline) -or $readyPrefix -match '_menu\s*='){throw 'R6 prefix does not only register the active candidate'}
if($controller -notmatch '(?s)OnRefreshButtons\(NMainMenu menu\).*?RestoredMenu\.ObserveRefreshButtons\(menu\).*?EventKind\.RefreshButtons'){throw 'R6 refresh postfix does not require candidate observer acceptance'}
if($controller -notmatch '(?s)OwnerFaultTransaction\.Active.*?RestoredMenu\.ObserveReady\(menu\).*?_menu = menu'){throw 'R6 _Ready postfix does not confirm candidate before ready/menu update'}
if($transaction -match '\(menuReady, refreshButtons\)\s*=\s*await' -or $transaction -notmatch '(?s)restoredMenuSignals = await ProbeController\.WaitForRestoredMenuAsync.*?MenuReadyStatus.*?RefreshButtonsStatus'){throw 'R6 does not independently capture menu signal states after bounded wait'}
if($transaction -notmatch '(?s)CaptureRestoredMenuLoadException\(exception\).*?FailureSite\.MainMenu' -and $observer -notmatch 'SnapshotLocked\(FailureSite\.MainMenu'){throw 'R6 real LoadMainMenu exception classification missing'}
if(-not$ui.Contains('TerminalTextForUi()', [StringComparison]::Ordinal)-or-not$ui.Contains('ComposeStatus', [StringComparison]::Ordinal)){throw 'terminal-before-page UI surface missing'}
$activate = [regex]::Match($controller, '(?s)internal static void Activate\(\).*?\n    internal static GraphicsCallToken').Value
if ($activate.Contains('GetLoadedModIds', [StringComparison]::Ordinal) -or $activate.Contains('ConfigurationPrecheck', [StringComparison]::Ordinal)) {
    throw 'initializer still performs final loaded-set evaluation'
}
$menuReady = [regex]::Match($controller, '(?s)internal static void OnMainMenuReady\(NMainMenu menu\).*?\n    private static bool EnsurePostLoaderConfiguration').Value
$postLoaderOffset = $menuReady.IndexOf('EnsurePostLoaderConfiguration()', [StringComparison]::Ordinal)
$activeOffset = $menuReady.IndexOf('OwnerFaultTransaction.Active', [StringComparison]::Ordinal)
$managerOffset = $menuReady.IndexOf('SaveManager.Instance', [StringComparison]::Ordinal)
if ($postLoaderOffset -lt 0 -or $activeOffset -lt 0 -or $managerOffset -lt 0 -or
    $postLoaderOffset -ge $activeOffset -or $postLoaderOffset -ge $managerOffset) {
    throw 'main-menu source does not place post-loader mod-set check first'
}
if ($precheck.Contains('Profile', [StringComparison]::OrdinalIgnoreCase) -or $precheck.Contains('SaveManager', [StringComparison]::Ordinal) -or
    $precheck.Contains('Protection', [StringComparison]::Ordinal) -or $precheck.Contains('RunManager', [StringComparison]::Ordinal) -or
    $precheck.Contains('Path.GetFullPath', [StringComparison]::Ordinal) -or $precheck.Contains('SHA256', [StringComparison]::Ordinal)) {
    throw 'precheck reads forbidden business/profile/path/payload surface'
}
if ([regex]::Matches($transaction, 'RunManager\.Instance\.SetUpReplay\(').Count -ne 1) { throw 'SetUpReplay source count is not exactly one' }
if ([regex]::Matches($transaction, 'RunManager\.Instance\.Launch\(').Count -ne 1) { throw 'Launch source count is not exactly one' }
if ([regex]::Matches($transaction, '\.SetCurrentScene\(').Count -ne 1) { throw 'SetCurrentScene source count is not exactly one' }
if ([regex]::Matches($transaction, 'RunManager\.Instance\.CleanUp\(true\)').Count -ne 1) { throw 'CleanUp(true) source count is not exactly one' }
foreach ($term in @('GenerateMap(','LoadIntoLatestMapCoord(','AwaitProcessFrame','UpdateProgressAfterCombatWon(','SaveProgressFile(','RecordInitialState(','WriteReplay(','EndCombat','Victory','ClaimReward','ReceiveReward','DispatchReplay','ReplayEvent')) {
    if ($transaction.Contains($term, [StringComparison]::Ordinal)) { throw "forbidden business/crash-path transaction surface: $term" }
}
if ($transaction -notmatch '(?s)throw new ControlledOwnerFaultException\(phase, expectedFault, attempt\);\s*}\s*catch \(ControlledOwnerFaultException controlled\) when \(controlled\.Phase == phase && controlled\.Site == expectedFault && controlled\.AttemptToken == attempt\).*?finally\s*\{\s*ownerFinally\+\+') {
    throw 'exact controlled fault catch/finally ownership surface missing'
}
if ($transaction -notmatch '(?s)finally\s*\{.*?plan\.Step\("restore-manager".*?SaveManager\.ClearInstanceForTesting\(\).*?SaveManager\.MockInstanceForTesting\(original\).*?await StepTask\(plan, "original-task-wait".*?LoadMainMenuMethod\.Invoke.*?WaitForRestoredMenuAsync') {
    throw 'ordered teardown/outer manager restore/menu gate surface missing'
}
foreach ($step in @('actions-disabled','isolated-task-wait-1','cleanup','isolated-task-wait-2','restore-manager','original-task-wait','main-menu-load','protection-after')) {
    if (-not $transaction.Contains('"' + $step + '"', [StringComparison]::Ordinal)) { throw "teardown step missing: $step" }
}
foreach ($step in @('ActionQueueSet.Reset','CardSelectCmd.Reset','CombatManager.Reset','ActionExecutor.remove_JustBeforeActionFinishedExecuting','CombatReplayWriter.Dispose','ActionQueueSynchronizer.Dispose','PlayerChoiceSynchronizer.Dispose','RewardSynchronizer.Dispose','RewardsSetSynchronizer.Dispose','RestSiteSynchronizer.Dispose','FlavorSynchronizer.Dispose','ChecksumTracker.Dispose','RunLobby.remove_RemotePlayerDisconnected','RunLobby.Dispose','INetGameService.Disconnect')) {
    if (-not $audit.Contains('"' + $step + '"', [StringComparison]::Ordinal)) { throw "cleanup disposer audit missing: $step" }
}
if ($audit -notmatch 'StoreEntry\(object instance\)\s*\{\s*if \(!IsObserverRunningFirst\(\)\) return true;') { throw 'store first-instruction sealed/enabled gate missing' }
if ($audit -notmatch '(?s)if \(_originalStores\.Contains\(instance\)\).*?return false;.*?if \(!ReferenceEquals\(instance, _memory\)\).*?return false;') { throw 'store identity-first order missing' }
if ($audit -notmatch 'ExternalEntry\(\)') { throw 'parameter-free external observer missing' }
if ($audit -notmatch '(?s)CleanupAuditPatch.*?Finalizer\(Exception\? __exception\).*?return __exception') { throw 'cleanup exception-preserving finalizer missing' }
if ($controller -notmatch '_startupTimeout\?\.Cancel\(\)' -or $controller -notmatch 'StartupGate\.TryTimeout\(\)' -or $controller -notmatch 'StartupGate\.TryCommitGateReady\(\)') { throw 'atomic startup timeout disarm surface missing' }
if ($controller -notmatch 'FileMode\.CreateNew' -or $controller -notmatch 'File\.Move\(_decision\.ArtifactPath \+ "\.tmp", _decision\.ArtifactPath\)') { throw 'create-new artifact writer surface missing' }
if ($phasePolicy.Contains('Environment.GetCommandLineArgs', [StringComparison]::Ordinal) -or $phasePolicy.Contains('GetEnvironmentVariable', [StringComparison]::Ordinal)) { throw 'external phase selector surface found' }
if ([regex]::Matches($models, '"MachineSettings:settings\.save"').Count -ne 1 -or
    [regex]::Matches($models, '"ActiveProfile:saves/prefs\.save\.backup"').Count -ne 1 -or
    [regex]::Matches($models, '"ActiveProfile:saves/progress\.save\.backup"').Count -ne 1 -or
    $models.Contains('ActiveProfile:saves/current_run.save"', [StringComparison]::Ordinal)) { throw 'restart mtime whitelist mismatch' }
foreach ($copy in @('SPIKE-INV008A','WAITING_EVENTS','Run Setup Fault','Run Post-launch Fault','no timeout while waiting')) {
    if (-not $ui.Contains($copy, [StringComparison]::Ordinal)) { throw "required UI copy missing: $copy" }
}

if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) { throw "missing Release assembly: $assembly" }
$bytes = [IO.File]::ReadAllBytes($assembly)
$ascii = [Text.Encoding]::ASCII.GetString($bytes)
$unicode = [Text.Encoding]::Unicode.GetString($bytes)
foreach ($term in @('SPIKEINV003A','SpikeInv003A','SPIKEINV007A','SpikeInv007A','Environment.FailFast','Process.Kill','NativeCrash')) {
    if ($ascii.Contains($term, [StringComparison]::Ordinal) -or $unicode.Contains($term, [StringComparison]::Ordinal)) { throw "forbidden production binary surface: $term" }
}

$cecil = 'C:\Program Files\JetBrains\Rider\r2r\2025.1.3R\CB7459855B94C993D768417A5CF3FF0\NetCore\Mono.Cecil.dll'
if (-not (Test-Path -LiteralPath $cecil -PathType Leaf)) { throw 'Mono.Cecil unavailable' }
Add-Type -LiteralPath $cecil
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($assembly)
try {
    $auditType = $module.Types | Where-Object FullName -eq 'SpikeInv008A.BoundaryAudit'
    $store = $auditType.Methods | Where-Object Name -eq 'StoreEntry'
    $instructions = @($store.Body.Instructions)
    if ($instructions[0].Operand.FullName -ne 'System.Boolean SpikeInv008A.BoundaryAudit::IsObserverRunningFirst()') { throw 'binary store first instruction is not observer gate' }
    $containsOffsets = @($instructions | Where-Object { $_.Operand -and $_.Operand.FullName -match 'HashSet`1<System.Object>::Contains' } | ForEach-Object Offset)
    $memoryOffset = @($instructions | Where-Object { $_.OpCode.Name -eq 'ldsfld' -and $_.Operand.Name -eq '_memory' } | ForEach-Object Offset | Select-Object -First 1)
    if ($containsOffsets.Count -lt 1 -or $memoryOffset.Count -ne 1 -or $containsOffsets[0] -ge $memoryOffset[0]) { throw 'binary original/memory identity order mismatch' }

    $owner = $module.Types | Where-Object FullName -eq 'SpikeInv008A.OwnerFaultTransaction'
    $stateMachine = @($owner.NestedTypes | Where-Object Name -like '*RunAsync*')
    if ($stateMachine.Count -ne 1) { throw 'RunAsync state machine identity mismatch' }
    function Get-TypeTree($type) { $type; foreach ($nested in $type.NestedTypes) { Get-TypeTree $nested } }
    $calls = @(Get-TypeTree $owner | ForEach-Object { $_.Methods } | Where-Object HasBody |
        ForEach-Object { $_.Body.Instructions } | Where-Object Operand | ForEach-Object { $_.Operand.FullName })
    function Count-Call([string]$Pattern) { @($calls | Where-Object { $_ -match $Pattern }).Count }
    if ((Count-Call 'RunManager::SetUpReplay') -ne 1 -or (Count-Call 'RunManager::Launch') -ne 1 -or
        (Count-Call 'NSceneContainer::SetCurrentScene') -ne 1 -or (Count-Call 'RunManager::CleanUp') -ne 1) { throw 'binary setup/launch/scene/cleanup call cardinality mismatch' }
    foreach ($pattern in @('RunManager::GenerateMap','RunManager::LoadIntoLatestMapCoord','SaveManager::UpdateProgressAfterCombatWon','SaveManager::SaveProgressFile','CombatReplayWriter::RecordInitialState','CombatReplayWriter::WriteReplay')) {
        if ((Count-Call $pattern) -ne 0) { throw "forbidden binary owner transaction call: $pattern" }
    }
    $moveNext = $stateMachine[0].Methods | Where-Object Name -eq 'MoveNext'
    $runIl = @($moveNext.Body.Instructions)
    $baselineOffset = @($runIl | Where-Object { $_.Operand -and $_.Operand.FullName -match 'ProtectionScope::CaptureAndBackup' } | ForEach-Object Offset)
    $candidateOffset = @($runIl | Where-Object { $_.Operand -and $_.Operand.FullName -match 'LiveSeedCandidate::CaptureLive' } | ForEach-Object Offset)
    $canonicalOffset = @($runIl | Where-Object { $_.Operand -and $_.Operand.FullName -match 'OwnerFaultTransaction::CaptureOriginalCanonical' } | ForEach-Object Offset)
    if($baselineOffset.Count-ne1-or$candidateOffset.Count-ne1-or$canonicalOffset.Count-lt1-or$baselineOffset[0]-ge$candidateOffset[0]-or$baselineOffset[0]-ge$canonicalOffset[0]){throw 'binary fresh baseline order mismatch'}
    $controllerType = $module.Types | Where-Object FullName -eq 'SpikeInv008A.ProbeController'
    $activateIl = @($controllerType.Methods | Where-Object Name -eq 'Activate' | ForEach-Object Body | ForEach-Object Instructions)
    if (@($activateIl | Where-Object { $_.Operand -and ($_.Operand.FullName -match 'ConfigurationPrecheck::Evaluate|ProbeController::GetLoadedModIds|ModManager::GetLoadedMods') }).Count -ne 0) {
        throw 'binary initializer still reads/evaluates loaded mods'
    }
    $menuIl = @($controllerType.Methods | Where-Object Name -eq 'OnMainMenuReady' | ForEach-Object Body | ForEach-Object Instructions)
    $postLoaderIl = @($menuIl | Where-Object { $_.Operand -and $_.Operand.FullName -match 'ProbeController::EnsurePostLoaderConfiguration' } | ForEach-Object Offset)
    $businessIl = @($menuIl | Where-Object { $_.Operand -and $_.Operand.FullName -match 'OwnerFaultTransaction::get_Active|SaveManager::get_Instance|ProbeController::ReadContinueEvidence|ProbeController::RunPriorGate' } | ForEach-Object Offset)
    if ($postLoaderIl.Count -ne 1 -or $businessIl.Count -lt 2 -or @($businessIl | Where-Object { $_ -le $postLoaderIl[0] }).Count -ne 0) {
        throw 'binary main-menu precheck is not before business/profile surfaces'
    }
    $menuPatch = $module.Types | Where-Object FullName -eq 'SpikeInv008A.MainMenuReadyPatch'
    $prefixCalls = @($menuPatch.Methods | Where-Object Name -eq 'Prefix' | ForEach-Object Body | ForEach-Object Instructions |
        Where-Object { $_.Operand -and $_.Operand.FullName -match 'ProbeController::OnMainMenuReadyPrefix' })
    $postfixCalls = @($menuPatch.Methods | Where-Object Name -eq 'Postfix' | ForEach-Object Body | ForEach-Object Instructions |
        Where-Object { $_.Operand -and $_.Operand.FullName -match 'ProbeController::OnMainMenuReady' })
    if($prefixCalls.Count-ne1-or$postfixCalls.Count-ne1){throw 'binary _Ready candidate-prefix/ready-postfix patch mismatch'}
    $observerType = $module.Types | Where-Object FullName -eq 'SpikeInv008A.RestoredMenuObserver'
    foreach($method in @('RegisterCandidate','ObserveRefreshButtons','ObserveReady','WaitAsync','SnapshotLoadException')){
        if(@($observerType.Methods|Where-Object Name -eq $method).Count-ne1){throw "binary restored-menu observer method mismatch: $method"}
    }
} finally { $module.Dispose() }

$gameAssembly = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'
$gameInfo = Get-Item -LiteralPath $gameAssembly
if($gameInfo.Length-ne9757184-or(Get-FileHash -Algorithm SHA256 -LiteralPath $gameAssembly).Hash-ne'0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9'){throw 'fixed game identity mismatch'}
$gameModule = [Mono.Cecil.ModuleDefinition]::ReadModule($gameAssembly)
try {
    $mainMenuType = $gameModule.GetType('MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu')
    $readyMethod = @($mainMenuType.Methods | Where-Object Name -eq '_Ready')
    if($readyMethod.Count-ne1){throw 'production NMainMenu._Ready identity mismatch'}
    $readyIl = @($readyMethod[0].Body.Instructions)
    $refreshCalls = @($readyIl | Where-Object { $_.Operand -and $_.Operand.FullName -match 'NMainMenu::RefreshButtons\(' })
    $returns = @($readyIl | Where-Object { $_.OpCode.Name -eq 'ret' })
    if($refreshCalls.Count-ne1-or$returns.Count-lt1-or$refreshCalls[0].Offset-ge$returns[-1].Offset){throw 'production RefreshButtons is not inside _Ready body before return'}
} finally { $gameModule.Dispose() }

Write-Output "PASS production source separation and fixed identity ($($sources.Count) source files)"
Write-Output 'PASS frozen candidate protection JSON and phase-aware oracle sources unchanged'
Write-Output 'PASS v1/R2/R3/R4/R5 and INV-009 exact prior identity chain'
Write-Output 'PASS initializer has no final loaded-set read and NMainMenu._Ready checks first in source/IL'
Write-Output 'PASS bounded R6 attempt-specific wrong-mod-set precheck isolation'
Write-Output 'PASS real-schema historical content/settings/fresh-Continue prior gate surface'
Write-Output 'PASS recovery-copy mtime excluded and fresh baseline precedes candidate in source/IL'
Write-Output 'PASS terminal-before-page UI and distinct Continue failure evidence'
Write-Output 'PASS valid SemVer manifest and R6 schema/revision identity'
Write-Output 'PASS four-artifact phase chain with no selector and create-new writer'
Write-Output 'PASS exact controlled fault/catch/finally ownership surface'
Write-Output 'PASS ordered teardown, cleanup disposer coverage, manager restore, and explicit menu gates'
Write-Output 'PASS production _Ready-body RefreshButtons order and R6 candidate-prefix/ready-postfix binary patches'
Write-Output 'PASS independent menu-ready/refresh task status and precise identity/order/load failure evidence'
Write-Output 'PASS phase-aware exact-count core/conditional-lobby oracle and boundary observations'
Write-Output 'PASS setup no-owned-NRun and post-launch scene replacement evidence'
Write-Output 'PASS identity-first sealed observer source and binary audit'
Write-Output 'PASS forbidden kill/crash/business/replay/progress source and binary audit'
Write-Output 'PASS exact setup/launch/scene/cleanup binary call cardinality'
Write-Output 'PASS atomic startup timeout disarm and fixed Ready UI copy'
Write-Output 'PASS exact restart mtime whitelist'
