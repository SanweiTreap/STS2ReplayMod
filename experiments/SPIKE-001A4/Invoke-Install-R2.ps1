param(
    [Parameter(Mandatory=$true)][string]$ExpectedOldDllSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedOldManifestSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedNewDllSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedNewManifestSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedRepairLogSha256
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$logs = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs'
$evidence = 'C:\Projects\STS2ReplayMod\evidence\spike-001a4'
$srcDll = Join-Path $root 'artifacts-r2\SPIKE001A4.dll'
$srcManifest = Join-Path $root 'artifacts-r2\SPIKE001A4.json'
$rollbackDll = Join-Path $root 'artifacts-r1\SPIKE001A4.dll'
$rollbackManifest = Join-Path $root 'artifacts-r1\SPIKE001A4.json'
$dstDll = Join-Path $mods 'SPIKE001A4.dll'
$dstManifest = Join-Path $mods 'SPIKE001A4.json'

if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_A4_R2_INSTALL: game is running' }
if (@(Get-ChildItem -LiteralPath $evidence -Directory -Filter 'spike-001a4-r*-*').Count -ne 0) {
    throw 'BLOCKED_A4_R2_INSTALL: business attempt namespace exists'
}
$latest = Get-ChildItem -LiteralPath $logs -File -Filter 'godot*.log' |
    Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $latest -or (Get-FileHash -LiteralPath $latest.FullName -Algorithm SHA256).Hash -ne $ExpectedRepairLogSha256) {
    throw 'BLOCKED_A4_R2_INSTALL: r1 UI-error log identity mismatch'
}
if ((Get-FileHash -LiteralPath $dstDll -Algorithm SHA256).Hash -ne $ExpectedOldDllSha256 -or
    (Get-FileHash -LiteralPath $dstManifest -Algorithm SHA256).Hash -ne $ExpectedOldManifestSha256) {
    throw 'BLOCKED_A4_R2_INSTALL: installed r1 pair identity mismatch'
}
if ((Get-FileHash -LiteralPath $rollbackDll -Algorithm SHA256).Hash -ne $ExpectedOldDllSha256 -or
    (Get-FileHash -LiteralPath $rollbackManifest -Algorithm SHA256).Hash -ne $ExpectedOldManifestSha256) {
    throw 'BLOCKED_A4_R2_INSTALL: rollback r1 pair identity mismatch'
}
if ((Get-FileHash -LiteralPath $srcDll -Algorithm SHA256).Hash -ne $ExpectedNewDllSha256 -or
    (Get-FileHash -LiteralPath $srcManifest -Algorithm SHA256).Hash -ne $ExpectedNewManifestSha256) {
    throw 'BLOCKED_A4_R2_INSTALL: source r2 pair identity mismatch'
}

$other = @{}
Get-ChildItem -LiteralPath $mods -File | Where-Object { $_.Name -notin @('SPIKE001A4.dll','SPIKE001A4.json') } |
    ForEach-Object { $other[$_.Name] = "$($_.Length):$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)" }
try {
    Copy-Item -LiteralPath $srcDll -Destination $dstDll -Force
    Copy-Item -LiteralPath $srcManifest -Destination $dstManifest -Force
    if ((Get-FileHash -LiteralPath $dstDll -Algorithm SHA256).Hash -ne $ExpectedNewDllSha256 -or
        (Get-FileHash -LiteralPath $dstManifest -Algorithm SHA256).Hash -ne $ExpectedNewManifestSha256) {
        throw 'BLOCKED_A4_R2_INSTALL: installed r2 pair identity mismatch'
    }
    foreach ($pair in $other.GetEnumerator()) {
        $path = Join-Path $mods $pair.Key
        if (-not (Test-Path -LiteralPath $path)) { throw "BLOCKED_A4_R2_INSTALL: pre-existing mod disappeared: $($pair.Key)" }
        $identity = "$((Get-Item -LiteralPath $path).Length):$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)"
        if ($identity -ne $pair.Value) { throw "BLOCKED_A4_R2_INSTALL: pre-existing mod changed: $($pair.Key)" }
    }
} catch {
    Copy-Item -LiteralPath $rollbackDll -Destination $dstDll -Force
    Copy-Item -LiteralPath $rollbackManifest -Destination $dstManifest -Force
    if ((Get-FileHash -LiteralPath $dstDll -Algorithm SHA256).Hash -ne $ExpectedOldDllSha256 -or
        (Get-FileHash -LiteralPath $dstManifest -Algorithm SHA256).Hash -ne $ExpectedOldManifestSha256) {
        throw 'BLOCKED_A4_R2_INSTALL: install failed and r1 rollback identity failed'
    }
    throw
}
'PASS SPIKE001A4 r1 replaced by r2; rollback pair preserved; every non-A4 live-mod file unchanged'
