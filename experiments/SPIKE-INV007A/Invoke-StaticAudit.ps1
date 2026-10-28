param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$assembly = Join-Path $root "bin\$Configuration\net9.0\SPIKEINV007A.dll"
$sources = Get-ChildItem -LiteralPath $root -Filter '*.cs' -File
$joined = ($sources | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
$transaction = Get-Content -Raw -LiteralPath (Join-Path $root 'DirectWriteTransaction.cs')
$models = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeModels.cs')
$auditSource = Get-Content -Raw -LiteralPath (Join-Path $root 'BoundaryAudit.cs')
$controller = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeController.cs')
$ui = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeUi.cs')
$protection = Get-Content -Raw -LiteralPath (Join-Path $root 'ProtectionScope.cs')
$oracle = Get-Content -Raw -LiteralPath (Join-Path $root 'DetachedMutationOracle.cs')
$priorIntegrity = Get-Content -Raw -LiteralPath (Join-Path $root 'PriorAttemptIntegrity.cs')

foreach ($term in @(
    'SPIKEINV006B','SpikeInv006B','spike-inv006b','spike-inv004b',
    'TOADPOLE','IRONCLAD','ActionExecutor.IsPaused',
    'EndCombatInternal','CheckForDefeatedAllEnemiesAchievement','replay.events',
    'SearchOption.AllDirectories','accountTopLevelInventory','CaptureAccountTopLevelInventory'
)) {
    if ($joined.Contains($term, [StringComparison]::OrdinalIgnoreCase)) {
        throw "forbidden production source surface: $term"
    }
}
if ($joined -match 'FailureSite\.RunState(?![A-Za-z])') {
    throw 'forbidden aggregate FailureSite.RunState surface'
}

$businessEnd = $transaction.IndexOf('finally', [StringComparison]::Ordinal)
if ($businessEnd -lt 0) { throw 'transaction finally block not found' }
$business = $transaction.Substring(0, $businessEnd)
if ($business.Contains('AwaitProcessFrame', [StringComparison]::Ordinal)) {
    throw 'executor/frame wait exists before direct calls'
}
if ($business -match '(?i)(enemies|enemyIds)\.Count\s*==\s*2') {
    throw 'fixture enemy-count gate exists'
}
if ($business -match '(?i)(character|encounter|enemy).*Id.*(Equals|==).*["'']') {
    throw 'fixed model-id comparison exists'
}
if ([regex]::Matches($transaction, 'SaveManager\.Instance\.UpdateProgressAfterCombatWon\(').Count -ne 1) {
    throw 'production update call count is not exactly one'
}
if ([regex]::Matches($transaction, 'SaveManager\.Instance\.SaveProgressFile\(').Count -ne 1) {
    throw 'production save call count is not exactly one'
}
if ($transaction.IndexOf('UpdateProgressAfterCombatWon', [StringComparison]::Ordinal) -ge
    $transaction.IndexOf('SaveProgressFile', [StringComparison]::Ordinal)) {
    throw 'direct method order is invalid'
}

foreach ($required in @(
    'RunStateParsed','ReplaySetupComplete','SceneLoadComplete','DirectArgumentsReady','ProgressUpdated','ProgressSaved',
    'FixturePlayerCardinality','ReplaySetupFailed','ShouldSaveDisableFailed','NGameUnavailable','SceneLoadFailed',
    'ManagerOverrideLost','RunStateBindingLost','CurrentRoomNotCombat','UnsupportedRoomType','CombatStateUnavailable',
    'EncounterUnavailable','CharacterUnavailable','DiscoveredEnemiesUnavailable','EnemyListUnreadable',
    'EnemyEntryInvalid','ProgressSubsystemUnavailable',
    'DoCloudSync','InitializeGraphicsPreferences','MockInstanceForTesting','ClearInstanceForTesting',
    'ProtectionScope.CaptureAndBackup','BoundaryAudit.Start','CleanUp(true)'
)) {
    if (-not $joined.Contains($required, [StringComparison]::Ordinal)) {
        throw "missing required production source surface: $required"
    }
}

foreach ($required in @('EnumerateFileSystemEntries','FileAttributes.ReparsePoint','GetFinalPathNameByHandle')) {
    if (-not $joined.Contains($required, [StringComparison]::Ordinal)) {
        throw "missing protected-scope containment surface: $required"
    }
}

if ($transaction -notmatch '(?s)catch \(StopRunException stop\).*?classification = stop\.Classification;.*?site = stop\.Site;.*?catch\s*\{\s*classification = TerminalClassification\.BlockedInstrumentation;\s*site = FailureSite\.RunCallback;') {
    throw 'generic exception taxonomy does not fail as instrumentation'
}
if ($models -notmatch 'FailMutation, FailWrite' -or $models -match 'FailMutationOrWrite') {
    throw 'mutation and write classifications are not separate'
}
if (-not $models.Contains('spike-inv007a.direct-write.v3', [StringComparison]::Ordinal) -or
    -not $models.Contains('runtime-artifact-r3.json', [StringComparison]::Ordinal) -or
    -not $models.Contains('BlockedPriorAttemptUnresolved', [StringComparison]::Ordinal) -or
    -not $models.Contains('BlockedOracleParse', [StringComparison]::Ordinal) -or
    -not $models.Contains('BlockedOracleAliasing', [StringComparison]::Ordinal) -or
    -not $controller.Contains('BLOCKED_PRIOR_ATTEMPT_UNRESOLVED', [StringComparison]::Ordinal)) {
    throw 'R3 schema/artifact/oracle taxonomy surface missing'
}
if ($controller -notmatch 'if \(_gateReady \|\| Volatile\.Read\(ref _transactionOwner\) != 0 \|\| _verifyUsed\) return;' -or
    -not $controller.Contains('_startupTimeout?.Cancel();', [StringComparison]::Ordinal) -or
    -not $controller.Contains('EventKind.PriorAttemptVerified', [StringComparison]::Ordinal)) {
    throw 'R2 timeout disarm or prior-attempt gate surface missing'
}
if (-not $ui.Contains('Direct Write Ready — R3', [StringComparison]::Ordinal) -or
    -not $ui.Contains('R2 artifact, recovery, and active baseline verified. Click once; no timeout while waiting.', [StringComparison]::Ordinal)) {
    throw 'R3 Ready copy mismatch'
}
if (-not $protection.Contains('"r3-" + DateTimeOffset.UtcNow', [StringComparison]::Ordinal) -or
    -not $protection.Contains('ComparePrior', [StringComparison]::Ordinal)) {
    throw 'R3 unique recovery or prior comparison surface missing'
}
if ($controller -notmatch 'File\.Exists\(destination\).*?return false' -or
    $controller -notmatch 'FileMode\.CreateNew' -or
    $controller -notmatch 'File\.Move\(temporary, destination\)') {
    throw 'R3 artifact create-new/no-overwrite surface missing'
}
if ($transaction -match 'ApplyExpected\(LiveSeedCandidate\.CaptureProgress' -or
    $transaction -match 'var\s+expected\s*=\s*LiveSeedCandidate\.CaptureProgress') {
    throw 'direct ApplyExpected to mutable CaptureProgress is forbidden'
}
foreach ($required in @('JsonSerializationUtility.FromJson<SerializableProgress>',
    'CountSharedMutableReferences', 'CandidateCanonicalUnchanged', 'DiscoveredEnemiesUnchanged',
    'oracle.BaselineCanonical', 'DetachedMutationOracle.AdmissionPassed')) {
    if (-not $joined.Contains($required, [StringComparison]::Ordinal)) {
        throw "missing detached oracle gate surface: $required"
    }
}
$oracleCapture = $transaction.IndexOf('DetachedMutationOracle.Capture(candidate)', [StringComparison]::Ordinal)
$oracleApply = $transaction.IndexOf('DetachedMutationOracle.ApplyExpected', [StringComparison]::Ordinal)
$oracleRecheck = $transaction.IndexOf('candidateAfterOracle = LiveSeedCandidate.CanonicalProgress(candidate)', [StringComparison]::Ordinal)
$oracleAdmission = $transaction.IndexOf('DetachedMutationOracle.AdmissionPassed', [StringComparison]::Ordinal)
$productionUpdate = $transaction.IndexOf('SaveManager.Instance.UpdateProgressAfterCombatWon', [StringComparison]::Ordinal)
if ($oracleCapture -lt 0 -or $oracleApply -le $oracleCapture -or $oracleRecheck -le $oracleApply -or
    $oracleAdmission -le $oracleRecheck -or $productionUpdate -le $oracleAdmission) {
    throw 'detached oracle source ordering is invalid'
}
foreach ($required in @('ReadPriorProtectionAfter', 'artifactRecovery', 'artifactCurrent')) {
    if (-not $priorIntegrity.Contains($required, [StringComparison]::Ordinal)) {
        throw "missing R2 artifact/recovery/current integrity surface: $required"
    }
}
if ($auditSource -notmatch 'StoreEntry\(MethodBase method, object instance, object\[\] args\)\s*\{\s*if \(!IsObserverRunningFirst\(\)\) return true;') {
    throw 'store first-instruction gate missing'
}
if ($auditSource -notmatch '(?s)if \(_originalStores\.Contains\(instance\)\).*?return false;.*?if \(!ReferenceEquals\(instance, _memory\)\).*?return false;.*?args\[0\]') {
    throw 'store identity-before-argument order audit failed'
}
if ($auditSource -match 'ExternalEntry\([^)]' -or $auditSource -match 'ExternalEntry\(\).*?args') {
    throw 'external boundary must not consume parameters'
}

if (-not (Test-Path -LiteralPath $assembly)) { throw "missing assembly: $assembly" }
$bytes = [IO.File]::ReadAllBytes($assembly)
$ascii = [Text.Encoding]::ASCII.GetString($bytes)
$unicode = [Text.Encoding]::Unicode.GetString($bytes)
foreach ($term in @(
    'SPIKEINV006B','SpikeInv006B','spike-inv006b','spike-inv004b',
    'TOADPOLE','IRONCLAD','get_IsPaused','EndCombatInternal',
    'CheckForDefeatedAllEnemiesAchievement','FailMutationOrWrite'
)) {
    if ($ascii.Contains($term, [StringComparison]::OrdinalIgnoreCase) -or
        $unicode.Contains($term, [StringComparison]::OrdinalIgnoreCase)) {
        throw "forbidden production binary surface: $term"
    }
}

$cecil = 'C:\Program Files\JetBrains\Rider\r2r\2025.1.3R\CB7459855B94C993D768417A5CF3FF0\NetCore\Mono.Cecil.dll'
if (-not (Test-Path -LiteralPath $cecil)) { throw 'Mono.Cecil identity unavailable' }
Add-Type -LiteralPath $cecil
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($assembly)
try {
    $type = $module.Types | Where-Object FullName -eq 'SpikeInv007A.BoundaryAudit'
    $store = $type.Methods | Where-Object Name -eq 'StoreEntry'
    $instructions = @($store.Body.Instructions)
    if ($instructions[0].Operand.FullName -ne 'System.Boolean SpikeInv007A.BoundaryAudit::IsObserverRunningFirst()') {
        throw 'binary store first instruction is not sealed/enabled gate'
    }
    $firstArgumentRead = ($instructions | Where-Object OpCode -Match '^ldelem' | Select-Object -First 1).Offset
    $originalIdentity = ($instructions | Where-Object { $_.Operand -and $_.Operand.FullName -match 'HashSet`1<System.Object>::Contains' } | Select-Object -First 1).Offset
    $memoryIdentity = ($instructions | Where-Object { $_.OpCode.Name -eq 'ldsfld' -and $_.Operand.Name -eq '_memory' } | Select-Object -First 1).Offset
    if ($null -eq $firstArgumentRead -or $originalIdentity -ge $firstArgumentRead -or $memoryIdentity -ge $firstArgumentRead) {
        throw 'binary identity-before-argument order failed'
    }

    $oracleType = $module.Types | Where-Object FullName -eq 'SpikeInv007A.DetachedMutationOracle'
    $capture = $oracleType.Methods | Where-Object Name -eq 'Capture'
    $captureCalls = @($capture.Body.Instructions | Where-Object Operand | ForEach-Object { $_.Operand.FullName })
    if (-not ($captureCalls | Where-Object { $_ -match 'JsonSerializationUtility::FromJson' }) -or
        -not ($captureCalls | Where-Object { $_ -match 'DetachedMutationOracle::CountSharedMutableReferences' })) {
        throw 'binary detached parse/reference gate missing'
    }
    $stateMachine = $module.Types | Where-Object FullName -eq 'SpikeInv007A.DirectWriteTransaction' |
        ForEach-Object NestedTypes | Where-Object Name -like '*RunAsync*'
    $moveNext = $stateMachine.Methods | Where-Object Name -eq 'MoveNext'
    $calls = @($moveNext.Body.Instructions | Where-Object Operand | ForEach-Object {
        [pscustomobject]@{ Offset = $_.Offset; Name = $_.Operand.FullName }
    })
    function First-CallOffset([string]$Pattern) {
        return ($calls | Where-Object Name -Match $Pattern | Select-Object -First 1).Offset
    }
    $binaryCapture = First-CallOffset 'DetachedMutationOracle::Capture'
    $binaryApply = First-CallOffset 'DetachedMutationOracle::ApplyExpected'
    $binaryRecheck = First-CallOffset 'LiveSeedCandidate::CanonicalProgress'
    $binaryAdmission = First-CallOffset 'DetachedMutationOracle::AdmissionPassed'
    $binaryUpdate = First-CallOffset 'SaveManager::UpdateProgressAfterCombatWon'
    if ($null -in @($binaryCapture,$binaryApply,$binaryRecheck,$binaryAdmission,$binaryUpdate) -or
        $binaryApply -le $binaryCapture -or $binaryRecheck -le $binaryApply -or
        $binaryAdmission -le $binaryRecheck -or $binaryUpdate -le $binaryAdmission) {
        throw 'binary detached oracle/pre-call baseline ordering failed'
    }
} finally {
    $module.Dispose()
}

Write-Output "PASS production source separation/forbidden-surface audit ($($sources.Count) files)"
Write-Output 'PASS independent preconditions and generic-exception taxonomy audit'
Write-Output 'PASS no fixed character/encounter/enemy/count gate'
Write-Output 'PASS no executor/frame wait before direct calls'
Write-Output 'PASS exact one update then one save source audit'
Write-Output 'PASS identity-before-path/payload source and binary audit'
Write-Output 'PASS terminal/schema/binary forbidden-surface audit'
Write-Output 'PASS R3 timeout disarm/locked guard and Ready copy audit'
Write-Output 'PASS detached JSON oracle, recursive alias check, and pre-call baseline audit'
Write-Output 'PASS R3 prior-attempt comparison and unique recovery surface audit'
