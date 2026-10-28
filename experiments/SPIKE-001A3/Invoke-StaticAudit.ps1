$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $root 'artifacts-r2\SPIKE001A3.dll'
$gameDll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'
$invReport = 'C:\Projects\STS2ReplayMod\project-management\investigation-reports\INV-010-report.md'
$invContract = 'C:\Projects\STS2ReplayMod\evidence\inv-010\reconstruction-contract.json'

$fixed = @(
    @($gameDll, 9757184, '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9'),
    @($invReport, 19818, '59D52227F5146253CEF41C5E93EDBC5AC4195EEE754CD4DCE6F39000F5FC61E0'),
    @($invContract, 1268, '54D99A392E5B113E78F770984100549E486A19D06145469E1D22BC54F1B02CF1')
)
foreach ($item in $fixed) {
    $info = Get-Item -LiteralPath $item[0]
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $item[0]).Hash
    if ($info.Length -ne $item[1] -or $hash -ne $item[2]) { throw "BLOCKED_IDENTITY $($item[0])" }
}

$sourceFiles = Get-ChildItem -LiteralPath $root -File -Filter '*.cs'
$allSource = ($sourceFiles | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
foreach ($forbidden in @('SPIKE001A2R2','spike-001a2-r2','SetUpReplay(','LoadRun(','ToSave(','SaveProgressFile(',
    'SwitchProfileId(','MockInstanceForTesting','DeleteCurrentRun(','WriteFile(','WriteFileAsync(')) {
    if ($allSource.Contains($forbidden, [StringComparison]::Ordinal)) { throw "FORBIDDEN_SOURCE_SURFACE $forbidden" }
}
$patches = @($sourceFiles | ForEach-Object { Select-String -LiteralPath $_.FullName -SimpleMatch '[HarmonyPatch(' })
if ($patches.Count -ne 4) { throw "FORBIDDEN_PATCH_COUNT $($patches.Count)" }
foreach ($required in @('NMainMenu', 'NGame', 'SaveManager', 'RunManager')) {
    if (-not ($patches.Line | Where-Object { $_.Contains("typeof($required)", [StringComparison]::Ordinal) })) {
        throw "MISSING_PATCH_TARGET $required"
    }
}
$hasAlwaysMode = $allSource.Contains('ProcessMode = ProcessModeEnum.Always', [StringComparison]::Ordinal)
$hasSetProcess = $allSource.Contains('SetProcess(true)', [StringComparison]::Ordinal)
if ((-not $hasAlwaysMode) -or (-not $hasSetProcess)) {
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
    $entry = $assembly.MainModule.GetType('Spike001A3.ProductionSaveObserverPatch')
    $postfix = $entry.Methods | Where-Object Name -eq 'Postfix'
    if ($null -eq $postfix -or -not ($postfix.Parameters | Where-Object { $_.Name -eq '__result' -and $_.ParameterType.IsByReference })) {
        throw 'SAVE_TASK_RESULT_WRAPPER_IL_MISSING'
    }
    $ticker = $assembly.MainModule.GetType('Spike001A3.ProbeTicker')
    foreach ($method in @('_Process','_EnterTree','_ExitTree')) {
        if (-not ($ticker.Methods | Where-Object Name -eq $method)) { throw "TICKER_LIFECYCLE_IL_MISSING $method" }
    }
    $stateMachine = $assembly.MainModule.GetType('Spike001A3.ProbeController').NestedTypes |
        Where-Object { $_.Name -like '*WrapProductionSaveAsync*' } | Select-Object -First 1
    $moveNext = $stateMachine.Methods | Where-Object Name -eq 'MoveNext' | Select-Object -First 1
    $orderedNames = @($moveNext.Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] } |
        ForEach-Object { $_.Operand.FullName } | Where-Object {
            $_ -match 'AwaitProductionAsync|ProbeController::StartDeadline|CheckpointReader::Capture|WriteBytesCreateNew|WriteCreateNew<Spike001A3.BaselineArtifact>'
        })
    $orderOk = $orderedNames.Count -eq 5
    if ($orderOk) {
        $orderOk = ($orderedNames[0] -match 'AwaitProductionAsync') -and ($orderedNames[1] -match 'StartDeadline')
        $orderOk = $orderOk -and ($orderedNames[2] -match 'CheckpointReader::Capture')
        $orderOk = $orderOk -and ($orderedNames[3] -match 'WriteBytesCreateNew')
        $orderOk = $orderOk -and ($orderedNames[4] -match 'BaselineArtifact')
    }
    if (-not $orderOk) {
        throw "CHECKPOINT_OBSERVER_IL_ORDER_MISMATCH $([string]::Join(' -> ', $orderedNames))"
    }
    'PASS fixed identity gate'
    "PASS source forbidden-surface and four-patch allowlist ($($sourceFiles.Count) source files)"
    'PASS binary mutation-call audit'
    'PASS transparent SaveRun Task-result wrapper IL contract'
    'PASS await -> deadline -> stable capture -> recovery -> baseline IL order'
    'PASS persistent ticker lifecycle IL contract'
    "INFO assembly=$dll length=$((Get-Item -LiteralPath $dll).Length) sha256=$((Get-FileHash -Algorithm SHA256 -LiteralPath $dll).Hash)"
    "INFO manifest=$(Join-Path $root 'artifacts-r2\SPIKE001A3.json') length=$((Get-Item -LiteralPath (Join-Path $root 'artifacts-r2\SPIKE001A3.json')).Length) sha256=$((Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $root 'artifacts-r2\SPIKE001A3.json')).Hash)"
} finally { $assembly.Dispose() }
