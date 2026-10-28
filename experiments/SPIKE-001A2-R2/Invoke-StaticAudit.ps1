$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $root 'artifacts-r3\SPIKE001A2R2.dll'
$sourceFiles = Get-ChildItem -LiteralPath $root -File -Filter '*.cs'
$sourceForbidden = @(
    'SetUpSavedSingleplayer\s*\(', 'SetUpReplay\s*\(', '\.LoadRun\s*\(', '\.ToSave\s*\(',
    '\.SaveRun\s*\(', '\.SaveProgressFile\s*\(', '\.SwitchProfileId\s*\(',
    'MockInstanceForTesting\s*=', '\.DeleteCurrentRun\s*\(', '\.WriteFile(?:Async)?\s*\('
)
foreach ($pattern in $sourceForbidden) {
    $hits = Select-String -LiteralPath $sourceFiles.FullName -Pattern $pattern
    if ($hits) { throw "FORBIDDEN_SOURCE_SURFACE pattern=$pattern hits=$($hits.Path -join ',')" }
}
$patchLines = @($sourceFiles | ForEach-Object { Select-String -LiteralPath $_.FullName -SimpleMatch '[HarmonyPatch(' })
if ($patchLines.Count -ne 2) { throw "FORBIDDEN_PATCH_SURFACE count=$($patchLines.Count)" }
$allowedPatchText = @('typeof(NMainMenu), nameof(NMainMenu._Ready)', 'typeof(NGame), nameof(NGame._Input)')
foreach ($line in $patchLines.Line) {
    if (-not ($allowedPatchText | Where-Object { $line.Contains($_, [StringComparison]::Ordinal) })) {
        throw "FORBIDDEN_PATCH_TARGET line=$line"
    }
}
if (-not (Test-Path -LiteralPath $dll)) { throw "missing release artifact: $dll" }
$cecil = Get-ChildItem 'C:\Program Files\JetBrains\Rider' -Filter Mono.Cecil.dll -Recurse -File |
    Sort-Object FullName | Select-Object -First 1
if ($null -eq $cecil) { throw 'Mono.Cecil not found' }
Add-Type -Path $cecil.FullName
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
try {
    $forbiddenNames = @('SetUpSavedSingleplayer','SetUpReplay','LoadRun','ToSave','SaveRun','SaveProgressFile',
        'SwitchProfileId','set_MockInstanceForTesting','DeleteCurrentRun','WriteFile','WriteFileAsync')
    $refs = @($assembly.MainModule.GetMemberReferences() | Where-Object {
        $_ -is [Mono.Cecil.MethodReference] -and $forbiddenNames -contains $_.Name
    } | ForEach-Object FullName)
    if ($refs.Count) { throw "FORBIDDEN_BINARY_SURFACE refs=$([string]::Join(';', $refs))" }
    "PASS source forbidden-surface audit ($($sourceFiles.Count) C# files)"
    'PASS Harmony patch allowlist (NMainMenu._Ready and NGame._Input only)'
    'PASS binary forbidden member-reference audit'
    "INFO assembly=$dll length=$((Get-Item -LiteralPath $dll).Length) sha256=$((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash)"
    "INFO Mono.Cecil=$($cecil.FullName)"
} finally { $assembly.Dispose() }
