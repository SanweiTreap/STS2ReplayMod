$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDll=Join-Path $root 'artifacts-r2\SPIKEINV008A.dll';$sourceManifest=Join-Path $root 'artifacts-r2\SPIKEINV008A.json'
$v1Dll=Join-Path $root 'artifacts\SPIKEINV008A.dll';$v1Manifest=Join-Path $root 'artifacts\SPIKEINV008A.json'
$modsRoot='C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods';$destinationDll=Join-Path $modsRoot 'SPIKEINV008A.dll';$destinationManifest=Join-Path $modsRoot 'SPIKEINV008A.json'
function Assert-Exact([string]$Path,[long]$Length,[string]$Hash,[string]$Label){$item=Get-Item -LiteralPath $Path;$actual=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash;if($item.Length -ne $Length -or $actual -ne $Hash){throw "BLOCKED_INSTALL: $Label mismatch"};Write-Output "PASS $Label length=$($item.Length) sha256=$actual"}
if(Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue){throw 'BLOCKED_INSTALL: game process is running'}
Assert-Exact $destinationDll 215552 '4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717' 'installed v1 DLL'
Assert-Exact $destinationManifest 372 'FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4' 'installed v1 manifest'
Assert-Exact $v1Dll 215552 '4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717' 'recoverable immutable v1 workspace DLL'
Assert-Exact $v1Manifest 372 'FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4' 'recoverable immutable v1 workspace manifest'
foreach($path in @($sourceDll,$sourceManifest)){if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "BLOCKED_INSTALL: missing R2 artifact $path"}}
$manifest = Get-Content -Raw -LiteralPath $sourceManifest | ConvertFrom-Json
if ($manifest.id -ne 'SPIKEINV008A' -or $manifest.version -ne '0.2.0') { throw 'BLOCKED_INSTALL: R2 manifest mismatch' }
& (Join-Path $root 'Invoke-EnablementPrecheck-R2.ps1')
$old007=@{};foreach($name in @('SPIKEINV007A.dll','SPIKEINV007A.json')){$path=Join-Path $modsRoot $name;$old007[$name]="$(Get-Item -LiteralPath $path|Select-Object -ExpandProperty Length)|$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)"}
try{[IO.File]::Copy($sourceDll,$destinationDll,$true);[IO.File]::Copy($sourceManifest,$destinationManifest,$true)}catch{[IO.File]::Copy($v1Dll,$destinationDll,$true);[IO.File]::Copy($v1Manifest,$destinationManifest,$true);throw}
foreach($pair in @([pscustomobject]@{Source=$sourceDll;Destination=$destinationDll},[pscustomobject]@{Source=$sourceManifest;Destination=$destinationManifest})){$s=Get-Item -LiteralPath $pair.Source;$d=Get-Item -LiteralPath $pair.Destination;$sh=(Get-FileHash -LiteralPath $pair.Source -Algorithm SHA256).Hash;$dh=(Get-FileHash -LiteralPath $pair.Destination -Algorithm SHA256).Hash;if($s.Length -ne $d.Length -or $sh -ne $dh){throw "BLOCKED_INSTALL: installed R2 mismatch $($d.Name)"};Write-Output "PASS installed R2 $($d.Name) length=$($d.Length) sha256=$dh"}
foreach($name in @('SPIKEINV007A.dll','SPIKEINV007A.json')){$path=Join-Path $modsRoot $name;$now="$(Get-Item -LiteralPath $path|Select-Object -ExpandProperty Length)|$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)";if($now -ne $old007[$name]){throw "BLOCKED_INSTALL: 007A changed $name"};Write-Output "PASS untouched installed $name identity=$now"}
Write-Output 'PASS R2 replaced only the exact v1 SPIKEINV008A DLL/manifest; 007A files and enabled configuration were not modified'
