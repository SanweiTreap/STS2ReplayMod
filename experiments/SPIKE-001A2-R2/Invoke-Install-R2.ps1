param([Parameter(Mandatory=$true)][string]$ExpectedR2DllSha256,
      [Parameter(Mandatory=$true)][string]$ExpectedR2ManifestSha256)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$srcDll = Join-Path $root 'artifacts-r2\SPIKE001A2R2.dll'
$srcManifest = Join-Path $root 'artifacts-r2\SPIKE001A2R2.json'
$r1Dll = Join-Path $root 'artifacts\SPIKE001A2R2.dll'
$r1Manifest = Join-Path $root 'artifacts\SPIKE001A2R2.json'
$dstDll = Join-Path $mods 'SPIKE001A2R2.dll'
$dstManifest = Join-Path $mods 'SPIKE001A2R2.json'
$r1DllHash = '9581CABC7103A6E13693DDC35F65ED788226EBA3DC084441A6D1B7C0A0F084A4'
$r1ManifestHash = 'B7E5F3C6FA53ECB26DFCAFA984728BEA9EA220D96890B07B3D27EF62A0C89B49'
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_R2_INSTALL: game is running' }
if ((Get-FileHash -LiteralPath $dstDll -Algorithm SHA256).Hash -ne $r1DllHash -or
    (Get-FileHash -LiteralPath $dstManifest -Algorithm SHA256).Hash -ne $r1ManifestHash) { throw 'BLOCKED_R2_INSTALL: installed R1 identity mismatch' }
if ((Get-FileHash -LiteralPath $r1Dll -Algorithm SHA256).Hash -ne $r1DllHash -or
    (Get-FileHash -LiteralPath $r1Manifest -Algorithm SHA256).Hash -ne $r1ManifestHash) { throw 'BLOCKED_R2_INSTALL: rollback pair identity mismatch' }
if ((Get-FileHash -LiteralPath $srcDll -Algorithm SHA256).Hash -ne $ExpectedR2DllSha256 -or
    (Get-FileHash -LiteralPath $srcManifest -Algorithm SHA256).Hash -ne $ExpectedR2ManifestSha256) { throw 'BLOCKED_R2_INSTALL: R2 source identity mismatch' }
$otherBefore = @{}
Get-ChildItem -LiteralPath $mods -File | Where-Object { $_.Name -match '^SPIKE.*\.(dll|json)$' -and $_.Name -notlike 'SPIKE001A2R2.*' } | ForEach-Object {
    $otherBefore[$_.Name] = "$( $_.Length ):$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
}
try {
    Copy-Item -LiteralPath $srcDll -Destination $dstDll
    Copy-Item -LiteralPath $srcManifest -Destination $dstManifest
} catch {
    Copy-Item -LiteralPath $r1Dll -Destination $dstDll
    Copy-Item -LiteralPath $r1Manifest -Destination $dstManifest
    throw
}
if ((Get-FileHash -LiteralPath $dstDll -Algorithm SHA256).Hash -ne $ExpectedR2DllSha256 -or
    (Get-FileHash -LiteralPath $dstManifest -Algorithm SHA256).Hash -ne $ExpectedR2ManifestSha256) { throw 'BLOCKED_R2_INSTALL: installed R2 identity mismatch' }
foreach ($pair in $otherBefore.GetEnumerator()) {
    $path = Join-Path $mods $pair.Key
    if (-not (Test-Path -LiteralPath $path) -or "$( (Get-Item -LiteralPath $path).Length ):$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)" -ne $pair.Value) {
        throw "BLOCKED_R2_INSTALL: other probe changed: $($pair.Key)"
    }
}
'PASS exact SPIKE001A2R2 R1 pair replaced with R2; every other probe file unchanged'
