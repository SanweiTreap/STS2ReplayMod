param(
    [Parameter(Mandatory=$true)][string]$ExpectedDllSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedManifestSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedCompletedLogSha256
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$log = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs\godot.log'
$evidence = 'C:\Projects\STS2ReplayMod\evidence\spike-001a3'
$srcDll = Join-Path $root 'artifacts-r2\SPIKE001A3.dll'
$srcManifest = Join-Path $root 'artifacts-r2\SPIKE001A3.json'
$dstDll = Join-Path $mods 'SPIKE001A3.dll'
$dstManifest = Join-Path $mods 'SPIKE001A3.json'
$oldDllHash = '565981122880F11913976380738DAEB1A5E81D95B94E364BA90ECFE7670E504D'
$oldManifestHash = 'ADCE78A98561C06CD40C8DB86870644118C4A3484D6165CFD161CB9C0260C6A9'

if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_A3_R2_INSTALL: game is running' }
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $dstDll).Hash -ne $oldDllHash -or
    (Get-FileHash -Algorithm SHA256 -LiteralPath $dstManifest).Hash -ne $oldManifestHash) {
    throw 'BLOCKED_A3_R2_INSTALL: installed R1 pair identity mismatch'
}
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $srcDll).Hash -ne $ExpectedDllSha256 -or
    (Get-FileHash -Algorithm SHA256 -LiteralPath $srcManifest).Hash -ne $ExpectedManifestSha256) {
    throw 'BLOCKED_A3_R2_INSTALL: source R2 pair identity mismatch'
}
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $log).Hash -ne $ExpectedCompletedLogSha256) {
    throw 'BLOCKED_A3_R2_INSTALL: completed R1 log identity mismatch'
}
$logText = Get-Content -Raw -LiteralPath $log
if (-not $logText.Contains('[Sentry.NET] Shutting down', [StringComparison]::Ordinal) -or
    -not $logText.Contains('[SPIKE-001A3] READY_A_NODE', [StringComparison]::Ordinal)) {
    throw 'BLOCKED_A3_R2_INSTALL: R1 log does not prove armed process and normal exit'
}
$r1Attempts = @(Get-ChildItem -LiteralPath $evidence -Directory -Filter 'spike-001a3-r1-*')
if ($r1Attempts.Count -ne 1 -or -not (Test-Path -LiteralPath (Join-Path $r1Attempts[0].FullName 'arm.json'))) {
    throw 'BLOCKED_A3_R2_INSTALL: expected exactly one preserved armed R1 attempt'
}
$forbiddenR1 = @('checkpoint-baseline.json','snapshot-a.json','post-a-checkpoint-verification.json',
    'b-precontinue.json','snapshot-b.json','final.json','terminal.json','recovery\checkpoint-baseline.save')
foreach ($name in $forbiddenR1) {
    if (Test-Path -LiteralPath (Join-Path $r1Attempts[0].FullName $name)) {
        throw "BLOCKED_A3_R2_INSTALL: R1 crossed pre-business boundary: $name"
    }
}
if (@(Get-ChildItem -LiteralPath $evidence -Directory -Filter 'spike-001a3-r2-*').Count -ne 0) {
    throw 'BLOCKED_A3_R2_INSTALL: R2 attempt namespace already exists'
}

$other = @{}
Get-ChildItem -LiteralPath $mods -File | Where-Object { $_.Name -notin @('SPIKE001A3.dll','SPIKE001A3.json') } |
    ForEach-Object { $other[$_.Name] = "$($_.Length):$((Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash)" }
$token = [Guid]::NewGuid().ToString('N')
$newDll = Join-Path $mods ("SPIKE001A3.dll.r2-new-" + $token)
$newManifest = Join-Path $mods ("SPIKE001A3.json.r2-new-" + $token)
$oldDll = Join-Path $mods ("SPIKE001A3.dll.r1-old-" + $token)
$oldManifest = Join-Path $mods ("SPIKE001A3.json.r1-old-" + $token)
foreach ($path in @($newDll,$newManifest,$oldDll,$oldManifest)) {
    if ([IO.Path]::GetFullPath([IO.Path]::GetDirectoryName($path)) -ne [IO.Path]::GetFullPath($mods)) {
        throw 'BLOCKED_A3_R2_INSTALL: staging path escaped mods directory'
    }
    if (Test-Path -LiteralPath $path) { throw "BLOCKED_A3_R2_INSTALL: staging collision $path" }
}

try {
    Copy-Item -LiteralPath $srcDll -Destination $newDll
    Copy-Item -LiteralPath $srcManifest -Destination $newManifest
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $newDll).Hash -ne $ExpectedDllSha256 -or
        (Get-FileHash -Algorithm SHA256 -LiteralPath $newManifest).Hash -ne $ExpectedManifestSha256) {
        throw 'BLOCKED_A3_R2_INSTALL: staged pair identity mismatch'
    }
    Move-Item -LiteralPath $dstDll -Destination $oldDll
    Move-Item -LiteralPath $dstManifest -Destination $oldManifest
    Move-Item -LiteralPath $newDll -Destination $dstDll
    Move-Item -LiteralPath $newManifest -Destination $dstManifest
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $dstDll).Hash -ne $ExpectedDllSha256 -or
        (Get-FileHash -Algorithm SHA256 -LiteralPath $dstManifest).Hash -ne $ExpectedManifestSha256) {
        throw 'BLOCKED_A3_R2_INSTALL: installed pair identity mismatch'
    }
    foreach ($pair in $other.GetEnumerator()) {
        $path = Join-Path $mods $pair.Key
        if (-not (Test-Path -LiteralPath $path)) { throw "BLOCKED_A3_R2_INSTALL: pre-existing mod disappeared: $($pair.Key)" }
        $identity = "$((Get-Item -LiteralPath $path).Length):$((Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash)"
        if ($identity -ne $pair.Value) { throw "BLOCKED_A3_R2_INSTALL: pre-existing mod changed: $($pair.Key)" }
    }
    Remove-Item -LiteralPath $oldDll
    Remove-Item -LiteralPath $oldManifest
} catch {
    if (Test-Path -LiteralPath $dstDll) { Remove-Item -LiteralPath $dstDll }
    if (Test-Path -LiteralPath $dstManifest) { Remove-Item -LiteralPath $dstManifest }
    if (Test-Path -LiteralPath $oldDll) { Move-Item -LiteralPath $oldDll -Destination $dstDll }
    if (Test-Path -LiteralPath $oldManifest) { Move-Item -LiteralPath $oldManifest -Destination $dstManifest }
    if (Test-Path -LiteralPath $newDll) { Remove-Item -LiteralPath $newDll }
    if (Test-Path -LiteralPath $newManifest) { Remove-Item -LiteralPath $newManifest }
    throw
}
'PASS SPIKE001A3 R1 pair replaced by verified R2 pair; old R1 evidence preserved; every other mod file unchanged'
