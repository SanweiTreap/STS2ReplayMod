param([Parameter(Mandatory=$true)][string]$ExpectedR3DllSha256,
      [Parameter(Mandatory=$true)][string]$ExpectedR3ManifestSha256)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$srcDll = Join-Path $root 'artifacts-r3\SPIKE001A2R2.dll'
$srcManifest = Join-Path $root 'artifacts-r3\SPIKE001A2R2.json'
$r2Dll = Join-Path $root 'artifacts-r2\SPIKE001A2R2.dll'
$r2Manifest = Join-Path $root 'artifacts-r2\SPIKE001A2R2.json'
$dstDll = Join-Path $mods 'SPIKE001A2R2.dll'
$dstManifest = Join-Path $mods 'SPIKE001A2R2.json'
$r2DllHash = 'CAB456916A3C03BA8AD5EFA77085029AAF6B53061DF8417D33CE68C7599C734F'
$r2ManifestHash = '58E4A6CF0832BE7F75982A5F96CDEE650B9CB8BC1B68A91EF1E27277394A9875'
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_R3_INSTALL: game is running' }
if ((Get-FileHash $dstDll -Algorithm SHA256).Hash -ne $r2DllHash -or (Get-FileHash $dstManifest -Algorithm SHA256).Hash -ne $r2ManifestHash) { throw 'BLOCKED_R3_INSTALL: installed R2 identity mismatch' }
if ((Get-FileHash $r2Dll -Algorithm SHA256).Hash -ne $r2DllHash -or (Get-FileHash $r2Manifest -Algorithm SHA256).Hash -ne $r2ManifestHash) { throw 'BLOCKED_R3_INSTALL: rollback R2 identity mismatch' }
if ((Get-FileHash $srcDll -Algorithm SHA256).Hash -ne $ExpectedR3DllSha256 -or (Get-FileHash $srcManifest -Algorithm SHA256).Hash -ne $ExpectedR3ManifestSha256) { throw 'BLOCKED_R3_INSTALL: R3 source identity mismatch' }
$other = @{}
Get-ChildItem $mods -File | Where-Object { $_.Name -match '^SPIKE.*\.(dll|json)$' -and $_.Name -notlike 'SPIKE001A2R2.*' } | ForEach-Object { $other[$_.Name]="$($_.Length):$((Get-FileHash $_.FullName -Algorithm SHA256).Hash)" }
try { Copy-Item $srcDll $dstDll; Copy-Item $srcManifest $dstManifest }
catch { Copy-Item $r2Dll $dstDll; Copy-Item $r2Manifest $dstManifest; throw }
if ((Get-FileHash $dstDll -Algorithm SHA256).Hash -ne $ExpectedR3DllSha256 -or (Get-FileHash $dstManifest -Algorithm SHA256).Hash -ne $ExpectedR3ManifestSha256) { throw 'BLOCKED_R3_INSTALL: installed R3 identity mismatch' }
foreach($pair in $other.GetEnumerator()){ $p=Join-Path $mods $pair.Key; if(-not(Test-Path $p)-or "$((Get-Item $p).Length):$((Get-FileHash $p -Algorithm SHA256).Hash)" -ne $pair.Value){throw "BLOCKED_R3_INSTALL: other probe changed $($pair.Key)"} }
'PASS exact SPIKE001A2R2 R2 pair replaced with R3; every other probe file unchanged'
