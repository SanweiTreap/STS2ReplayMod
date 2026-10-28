param(
    [Parameter(Mandatory=$true)][string]$ExpectedDllSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedManifestSha256,
    [Parameter(Mandatory=$true)][string]$ExpectedEnablementLogSha256
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$logs = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs'
$srcDll = Join-Path $root 'artifacts-r1\SPIKE001A4.dll'
$srcManifest = Join-Path $root 'artifacts-r1\SPIKE001A4.json'
$dstDll = Join-Path $mods 'SPIKE001A4.dll'
$dstManifest = Join-Path $mods 'SPIKE001A4.json'
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_A4_INSTALL: game is running' }
if ((Test-Path -LiteralPath $dstDll) -or (Test-Path -LiteralPath $dstManifest)) {
    throw 'BLOCKED_A4_INSTALL: destination already exists'
}
if ((Get-FileHash -Algorithm SHA256 -LiteralPath $srcDll).Hash -ne $ExpectedDllSha256 -or
    (Get-FileHash -Algorithm SHA256 -LiteralPath $srcManifest).Hash -ne $ExpectedManifestSha256) {
    throw 'BLOCKED_A4_INSTALL: source pair identity mismatch'
}
$latest = Get-ChildItem -LiteralPath $logs -File -Filter 'godot*.log' |
    Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $latest -or (Get-FileHash -Algorithm SHA256 -LiteralPath $latest.FullName).Hash -ne $ExpectedEnablementLogSha256) {
    throw 'BLOCKED_A4_INSTALL: enablement verification log identity mismatch'
}
& (Join-Path $root 'Invoke-EnablementPrecheck.ps1') | Out-Host
$other = @{}
Get-ChildItem -LiteralPath $mods -File | ForEach-Object {
    $other[$_.Name] = "$($_.Length):$((Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash)"
}
try {
    Copy-Item -LiteralPath $srcDll -Destination $dstDll
    Copy-Item -LiteralPath $srcManifest -Destination $dstManifest
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $dstDll).Hash -ne $ExpectedDllSha256 -or
        (Get-FileHash -Algorithm SHA256 -LiteralPath $dstManifest).Hash -ne $ExpectedManifestSha256) {
        throw 'BLOCKED_A4_INSTALL: installed pair identity mismatch'
    }
    foreach ($pair in $other.GetEnumerator()) {
        $path = Join-Path $mods $pair.Key
        if (-not (Test-Path -LiteralPath $path)) { throw "BLOCKED_A4_INSTALL: pre-existing mod disappeared: $($pair.Key)" }
        $identity = "$((Get-Item -LiteralPath $path).Length):$((Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash)"
        if ($identity -ne $pair.Value) { throw "BLOCKED_A4_INSTALL: pre-existing mod changed: $($pair.Key)" }
    }
} catch {
    if (Test-Path -LiteralPath $dstDll) { Remove-Item -LiteralPath $dstDll }
    if (Test-Path -LiteralPath $dstManifest) { Remove-Item -LiteralPath $dstManifest }
    throw
}
'PASS new SPIKE001A4 pair installed create-new; every pre-existing mod file unchanged'
