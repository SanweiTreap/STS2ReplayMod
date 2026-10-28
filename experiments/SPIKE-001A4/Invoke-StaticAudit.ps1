$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $root 'artifacts-r2\SPIKE001A4.dll'
$manifest = Join-Path $root 'artifacts-r2\SPIKE001A4.json'
$gameDll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'
$invReport = 'C:\Projects\STS2ReplayMod\project-management\investigation-reports\INV-011-report.md'
$invContract = 'C:\Projects\STS2ReplayMod\evidence\inv-011\successor-contract.json'
$invAudit = 'C:\Projects\STS2ReplayMod\evidence\inv-011\placeholder-validation-audit.md'

$fixed = @(
    @($gameDll, 9757184, '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9'),
    @($invReport, 19465, '7BE386861AC14433F4C61FB42A93C3C9879AF6B2B7B0E42D6463C2F50CAF2DBB'),
    @($invContract, 7770, '1527728A8481D98D357D4484D7A3269153F2F9A918B2657A02A3C9096511CFB7'),
    @($invAudit, 10445, 'C5CF56B9C11CF4D43893ADE5E4C5E73AD9F0DDBEDAD262787AE5F6C609258843')
)
foreach ($item in $fixed) {
    $info = Get-Item -LiteralPath $item[0]
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $item[0]).Hash
    if ($info.Length -ne $item[1] -or $hash -ne $item[2]) { throw "BLOCKED_IDENTITY $($item[0])" }
}

$sourceFiles = Get-ChildItem -LiteralPath $root -File -Filter '*.cs'
$allSource = ($sourceFiles | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
foreach ($forbidden in @('Spike001A3','SPIKE001A3','spike-001a3','SetUpReplay(','LoadRun(','ToSave(',
    'SaveProgressFile(','SwitchProfileId(','MockInstanceForTesting','DeleteCurrentRun(','WriteFile(','WriteFileAsync(')) {
    if ($allSource.Contains($forbidden, [StringComparison]::Ordinal)) { throw "FORBIDDEN_SOURCE_SURFACE $forbidden" }
}
foreach ($required in @('ArmPredecessorFloor','CheckpointFloorBinding','ExpectedPlayableFloor',
    'SemanticProjection','room.CombatState.Encounter','ObservationStatus.ReadException',
    'save.Players.Count != 1','save.VisitedMapCoords.Count == 0')) {
    if (-not $allSource.Contains($required, [StringComparison]::Ordinal)) { throw "MISSING_REQUIRED_SOURCE_CONTRACT $required" }
}
$observerSource = Get-Content -Raw -LiteralPath (Join-Path $root 'ProductionTaskObserver.cs')
if (($observerSource.Split('throw;', [StringSplitOptions]::None).Count - 1) -ne 2) {
    throw 'OBSERVER_SOURCE_PRODUCTION_RETHROW_COUNT_MISMATCH'
}
$observerStart = $observerSource.IndexOf('await observer()', [StringComparison]::Ordinal)
$observerEnd = $observerSource.IndexOf('private static async Task BestEffortTerminal', [StringComparison]::Ordinal)
$observerCatchRethrows = $observerStart -ge 0 -and $observerEnd -gt $observerStart -and
    $observerSource.Substring($observerStart, $observerEnd - $observerStart).Contains('throw;', [StringComparison]::Ordinal)
if ($observerStart -lt 0 -or $observerEnd -le $observerStart -or $observerCatchRethrows) {
    throw 'OBSERVER_ONLY_CATCH_RETHROWS_IN_SOURCE'
}
$snapshotSource = Get-Content -Raw -LiteralPath (Join-Path $root 'SnapshotCapture.cs')
foreach ($forbidden in @('BindingFlags','GetProperty(','GetField(','<exception:','?? ""')) {
    if ($snapshotSource.Contains($forbidden, [StringComparison]::Ordinal)) { throw "SNAPSHOT_FALLBACK_FORBIDDEN $forbidden" }
}
$uiSource = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeUi.cs')
if (-not $uiSource.Contains('internal const int OverlayZIndex = 4096;', [StringComparison]::Ordinal) -or
    -not $uiSource.Contains('ZIndex = OverlayZIndex', [StringComparison]::Ordinal) -or
    $uiSource.Contains('ZIndex = 10000', [StringComparison]::Ordinal)) {
    throw 'PROBE_UI_Z_INDEX_CONTRACT_MISSING'
}

$patches = @($sourceFiles | ForEach-Object { Select-String -LiteralPath $_.FullName -SimpleMatch '[HarmonyPatch(' })
if ($patches.Count -ne 4) { throw "FORBIDDEN_PATCH_COUNT $($patches.Count)" }
foreach ($required in @('NMainMenu', 'NGame', 'SaveManager', 'RunManager')) {
    if (-not ($patches.Line | Where-Object { $_.Contains("typeof($required)", [StringComparison]::Ordinal) })) {
        throw "MISSING_PATCH_TARGET $required"
    }
}
if (-not $allSource.Contains('ProcessMode = ProcessModeEnum.Always', [StringComparison]::Ordinal) -or
    -not $allSource.Contains('SetProcess(true)', [StringComparison]::Ordinal)) {
    throw 'TICKER_ALWAYS_CONTRACT_MISSING'
}
if (-not $allSource.Contains('FileMode.CreateNew', [StringComparison]::Ordinal)) { throw 'CREATE_NEW_WRITER_MISSING' }

if (-not (Test-Path -LiteralPath $dll)) { throw "missing release artifact $dll" }
$cecil = Get-ChildItem 'C:\Program Files\JetBrains\Rider' -Filter Mono.Cecil.dll -Recurse -File |
    Sort-Object FullName | Select-Object -First 1
if ($null -eq $cecil) { throw 'Mono.Cecil not found' }
Add-Type -Path $cecil.FullName
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
try {
    $forbiddenNames = @('SetUpReplay','LoadRun','ToSave','SaveProgressFile','SwitchProfileId',
        'set_MockInstanceForTesting','DeleteCurrentRun','WriteFile','WriteFileAsync')
    $refs = @($assembly.MainModule.GetMemberReferences() | Where-Object {
        $_ -is [Mono.Cecil.MethodReference] -and $forbiddenNames -contains $_.Name
    } | ForEach-Object FullName)
    if ($refs.Count) { throw "FORBIDDEN_BINARY_SURFACE $([string]::Join(';', $refs))" }
    $saveCalls = @($assembly.MainModule.GetMemberReferences() | Where-Object {
        $_ -is [Mono.Cecil.MethodReference] -and $_.Name -eq 'SaveRun'
    })
    if ($saveCalls.Count) { throw "PRODUCTION_SAVE_CALL_PRESENT $([string]::Join(';', ($saveCalls | ForEach-Object FullName)))" }

    $entry = $assembly.MainModule.GetType('Spike001A4.ProductionSaveObserverPatch')
    $postfix = $entry.Methods | Where-Object Name -eq 'Postfix'
    if ($null -eq $postfix -or -not ($postfix.Parameters | Where-Object {
        $_.Name -eq '__result' -and $_.ParameterType.IsByReference })) {
        throw 'SAVE_TASK_RESULT_WRAPPER_IL_MISSING'
    }

    $controller = $assembly.MainModule.GetType('Spike001A4.ProbeController')
    $wrapper = $controller.Methods | Where-Object Name -eq 'WrapProductionSaveAsync' | Select-Object -First 1
    $wrapperCalls = @($wrapper.Body.Instructions | Where-Object Operand -is [Mono.Cecil.MethodReference] |
        ForEach-Object { $_.Operand.FullName })
    if (-not ($wrapperCalls | Where-Object { $_ -match 'ProductionTaskObserver::WrapAsync' })) {
        throw 'CONCRETE_WRAPPER_DOES_NOT_ROUTE_THROUGH_NONINTERFERENCE_CORE'
    }

    $observer = $assembly.MainModule.GetType('Spike001A4.ProductionTaskObserver')
    $stateMachine = $observer.NestedTypes | Where-Object { $_.Name -like '*WrapAsync*' } | Select-Object -First 1
    $moveNext = $stateMachine.Methods | Where-Object Name -eq 'MoveNext' | Select-Object -First 1
    $typedHandlers = @($moveNext.Body.ExceptionHandlers | Where-Object {
        $_.CatchType -and $_.TryStart.Offset -gt 20 -and $_.HandlerStart.Offset -lt 800
    } | Sort-Object { $_.TryStart.Offset })
    if ($typedHandlers.Count -ne 3) { throw "OBSERVER_IL_TYPED_HANDLER_COUNT $($typedHandlers.Count)" }
    $observerHandler = $typedHandlers[-1]
    $lateThrows = @($moveNext.Body.Instructions | Where-Object {
        ($_.OpCode.Code.ToString() -in @('Throw','Rethrow')) -and $_.Offset -ge $observerHandler.TryStart.Offset
    })
    if ($lateThrows.Count) { throw "OBSERVER_ONLY_IL_RETHROW $([string]::Join(',', ($lateThrows | ForEach-Object Offset)))" }
    $bestEffortRefs = @($moveNext.Body.Instructions | Where-Object {
        $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'BestEffortTerminal'
    })
    if ($bestEffortRefs.Count -ne 3) { throw "OBSERVER_IL_BEST_EFFORT_BRANCH_COUNT $($bestEffortRefs.Count)" }

    $success = $controller.Methods | Where-Object Name -eq 'ObserveCheckpointSuccessAsync' | Select-Object -First 1
    $ordered = @($success.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] } |
        ForEach-Object { $_.Operand.FullName } | Where-Object {
            $_ -match 'CheckpointReader::Capture|CheckpointReader::ValidateTarget|WriteBytesCreateNew|WriteCreateNew<Spike001A4.BaselineArtifact>'
        })
    if ($ordered.Count -ne 4 -or $ordered[0] -notmatch 'Capture' -or $ordered[1] -notmatch 'ValidateTarget' -or
        $ordered[2] -notmatch 'WriteBytesCreateNew' -or $ordered[3] -notmatch 'BaselineArtifact') {
        throw "CHECKPOINT_OBSERVER_IL_ORDER_MISMATCH $([string]::Join(' -> ', $ordered))"
    }

    $semantic = $assembly.MainModule.GetType('Spike001A4.SemanticProjection')
    $semanticTypes = @($semantic.Properties | ForEach-Object { $_.PropertyType.FullName })
    if ($semanticTypes -match 'RawReadinessSample|ObjectIdentities') { throw 'PHASE_LOCAL_IDENTITY_IN_SEMANTIC_PROJECTION' }

    $ticker = $assembly.MainModule.GetType('Spike001A4.ProbeTicker')
    foreach ($method in @('_Process','_EnterTree','_ExitTree')) {
        if (-not ($ticker.Methods | Where-Object Name -eq $method)) { throw "TICKER_LIFECYCLE_IL_MISSING $method" }
    }
    'PASS fixed identity gate'
    "PASS source forbidden-surface and four-patch allowlist ($($sourceFiles.Count) source files)"
    'PASS typed floor, required-field, and dedicated semantic-projection source contract'
    'PASS source observer-only catch has no rethrow'
    'PASS binary mutation-call audit'
    'PASS transparent SaveRun Task-result wrapper IL contract'
    'PASS compiled IL observer-only branch has no throw/rethrow and routes terminal best-effort'
    'PASS capture -> validate/bind -> recovery -> baseline IL order'
    'PASS phase-local identity excluded from semantic projection IL shape'
    'PASS persistent ticker lifecycle IL contract'
    'PASS probe UI ZIndex is within Godot CanvasItem bounds'
    "INFO assembly=$dll length=$((Get-Item -LiteralPath $dll).Length) sha256=$((Get-FileHash -Algorithm SHA256 -LiteralPath $dll).Hash)"
    "INFO manifest=$manifest length=$((Get-Item -LiteralPath $manifest).Length) sha256=$((Get-FileHash -Algorithm SHA256 -LiteralPath $manifest).Hash)"
} finally { $assembly.Dispose() }
