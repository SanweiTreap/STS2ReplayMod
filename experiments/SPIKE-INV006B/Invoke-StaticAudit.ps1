param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$assembly = Join-Path $root "bin\$Configuration\net9.0\SPIKEINV006B.dll"
$sources = Get-ChildItem -LiteralPath $root -Filter '*.cs' -File
$joined = ($sources | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"

foreach ($term in @('SPIKEINV005A','spike-inv005a','accountTopLevelInventory','CaptureAccountTopLevelInventory','SearchOption.AllDirectories','EndCombatInternal','CheckForDefeatedAllEnemiesAchievement','replay.events')) {
    if ($joined.Contains($term, [StringComparison]::OrdinalIgnoreCase)) { throw "forbidden production source surface: $term" }
}
foreach ($required in @('DoCloudSync','InitializeGraphicsPreferences','UpdateProgressAfterCombatWon','SaveProgressFile','MockInstanceForTesting','ClearInstanceForTesting','SetUpReplay','CleanUp(true)','ProtectionScope.CaptureAndBackup','BoundaryAudit.Start')) {
    if (-not $joined.Contains($required, [StringComparison]::Ordinal)) { throw "missing required source surface: $required" }
}
foreach ($required in @('EnumerateFileSystemEntries','FileAttributes.ReparsePoint','GetFinalPathNameByHandle')) {
    if (-not $joined.Contains($required, [StringComparison]::Ordinal)) { throw "missing protected-scope containment surface: $required" }
}

$audit = Get-Content -Raw -LiteralPath (Join-Path $root 'BoundaryAudit.cs')
if ($audit -notmatch 'StoreEntry\(MethodBase method, object instance, object\[\] args\)\s*\{\s*if \(!IsObserverRunningFirst\(\)\) return true;') { throw 'store first-instruction gate missing' }
if ($audit -notmatch '(?s)if \(_originalStores.Contains\(instance\)\).*?return false;.*?if \(!ReferenceEquals\(instance, _memory\)\).*?return false;.*?args\[0\]' ) { throw 'store identity-before-argument order audit failed' }
if ($audit -match 'ExternalEntry\([^)]' -or $audit -match 'ExternalEntry\(\).*?args') { throw 'external boundary must not consume parameters' }

if (-not (Test-Path -LiteralPath $assembly)) { throw "missing assembly: $assembly" }
$bytes = [IO.File]::ReadAllBytes($assembly)
$ascii = [Text.Encoding]::ASCII.GetString($bytes); $unicode = [Text.Encoding]::Unicode.GetString($bytes)
foreach ($term in @('SPIKEINV005A','spike-inv005a','accountTopLevelInventory','EndCombatInternal','CheckForDefeatedAllEnemiesAchievement')) {
    if ($ascii.Contains($term,[StringComparison]::OrdinalIgnoreCase) -or $unicode.Contains($term,[StringComparison]::OrdinalIgnoreCase)) { throw "forbidden production binary surface: $term" }
}
$cecil = 'C:\Program Files\JetBrains\Rider\r2r\2025.1.3R\CB7459855B94C993D768417A5CF3FF0\NetCore\Mono.Cecil.dll'
Add-Type -LiteralPath $cecil
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($assembly)
try {
    $type = $module.Types | Where-Object FullName -eq 'SpikeInv006B.BoundaryAudit'
    $store = $type.Methods | Where-Object Name -eq 'StoreEntry'
    $instructions = @($store.Body.Instructions)
    if ($instructions[0].Operand.FullName -ne 'System.Boolean SpikeInv006B.BoundaryAudit::IsObserverRunningFirst()') { throw 'binary store first instruction is not the sealed/enabled gate' }
    $firstArgumentRead = ($instructions | Where-Object OpCode -Match '^ldelem' | Select-Object -First 1).Offset
    $originalIdentity = ($instructions | Where-Object { $_.Operand -and $_.Operand.FullName -match 'HashSet`1<System.Object>::Contains' } | Select-Object -First 1).Offset
    $memoryIdentity = ($instructions | Where-Object { $_.OpCode.Name -eq 'ldsfld' -and $_.Operand.Name -eq '_memory' } | Select-Object -First 1).Offset
    if ($null -eq $firstArgumentRead -or $originalIdentity -ge $firstArgumentRead -or $memoryIdentity -ge $firstArgumentRead) { throw 'binary identity-before-path/payload order failed' }
} finally { $module.Dispose() }
Write-Output "PASS production source forbidden-surface audit ($($sources.Count) files)"
Write-Output 'PASS store identity-before-path/payload order audit'
Write-Output 'PASS binary first-instruction and identity-before-argument order audit'
Write-Output 'PASS external fixed-boolean/no-parameter audit'
Write-Output 'PASS production binary forbidden-surface audit'
