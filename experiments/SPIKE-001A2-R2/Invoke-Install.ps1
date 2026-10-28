param([Parameter(Mandatory=$true)][string]$ExpectedDllSha256,
      [Parameter(Mandatory=$true)][string]$ExpectedManifestSha256)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$srcDll = Join-Path $root 'artifacts\SPIKE001A2R2.dll'
$srcManifest = Join-Path $root 'artifacts\SPIKE001A2R2.json'
$dstDll = Join-Path $mods 'SPIKE001A2R2.dll'
$dstManifest = Join-Path $mods 'SPIKE001A2R2.json'
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_INSTALL: game is running' }
if ((Get-FileHash -LiteralPath $srcDll -Algorithm SHA256).Hash -ne $ExpectedDllSha256) { throw 'BLOCKED_INSTALL: source DLL identity' }
if ((Get-FileHash -LiteralPath $srcManifest -Algorithm SHA256).Hash -ne $ExpectedManifestSha256) { throw 'BLOCKED_INSTALL: source manifest identity' }
if ((Test-Path -LiteralPath $dstDll) -or (Test-Path -LiteralPath $dstManifest)) { throw 'BLOCKED_INSTALL: destination pair already exists' }
$otherBefore = @{}
Get-ChildItem -LiteralPath $mods -File | Where-Object Name -Match '^SPIKE.*\.(dll|json)$' | ForEach-Object {
    $otherBefore[$_.Name] = "$( $_.Length ):$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
}
Copy-Item -LiteralPath $srcDll -Destination $dstDll
Copy-Item -LiteralPath $srcManifest -Destination $dstManifest
if ((Get-FileHash -LiteralPath $dstDll -Algorithm SHA256).Hash -ne $ExpectedDllSha256 -or
    (Get-FileHash -LiteralPath $dstManifest -Algorithm SHA256).Hash -ne $ExpectedManifestSha256) { throw 'BLOCKED_INSTALL: installed identity' }
foreach ($pair in $otherBefore.GetEnumerator()) {
    $path = Join-Path $mods $pair.Key
    if (-not (Test-Path -LiteralPath $path) -or "$( (Get-Item -LiteralPath $path).Length ):$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)" -ne $pair.Value) {
        throw "BLOCKED_INSTALL: other probe changed: $($pair.Key)"
    }
}
"PASS installed exact SPIKE001A2R2 DLL/manifest pair; all pre-existing probe files unchanged"
