$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDll=Join-Path $root 'artifacts-r3\SPIKEINV008A.dll';$sourceManifest=Join-Path $root 'artifacts-r3\SPIKEINV008A.json'
$mods='C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods';$destDll=Join-Path $mods 'SPIKEINV008A.dll';$destManifest=Join-Path $mods 'SPIKEINV008A.json'
$log='C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs\godot.log'
function Exact($p,$length,$hash,$label){$i=Get-Item -LiteralPath $p;$h=(Get-FileHash $p -Algorithm SHA256).Hash;if($i.Length-ne$length-or$h-ne$hash){throw "BLOCKED_INSTALL: $label mismatch"};"PASS $label length=$($i.Length) sha256=$h"}
if(Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue){throw 'BLOCKED_INSTALL: game running'}
Exact $destDll 229888 'D5746F9C560B6A0E09BE6235491554A47DAB3B1AEA84078EB25BF2E8077D2615' 'installed R2 DLL'
Exact $destManifest 353 '0FF3138632515C43F98E111752336C223C89B751AFFA8740491AFD11FF3118A1' 'installed R2 manifest'
Exact $log 252133 '7F01AE43BA2E43776D7A60413B3D6BC07875BFF38B92DE448420DDC573540171' 'complete R2 log'
$t=Get-Content -Raw $log;if(-not$t.Contains('Skipping loading mod SPIKEINV007A, it is set to disabled in settings')-or-not$t.Contains('Loaded 1 mods (16 total)')){throw 'BLOCKED_INSTALL: latest log does not prove 007A disabled / exactly 008A loaded'}
$m=Get-Content -Raw $sourceManifest|ConvertFrom-Json;if($m.id-ne'SPIKEINV008A'-or$m.version-ne'0.3.0'){throw 'BLOCKED_INSTALL: R3 manifest'}
$old007Dll=(Get-FileHash (Join-Path $mods 'SPIKEINV007A.dll') -Algorithm SHA256).Hash;$old007Manifest=(Get-FileHash (Join-Path $mods 'SPIKEINV007A.json') -Algorithm SHA256).Hash
try{Copy-Item -LiteralPath $sourceDll -Destination $destDll;Copy-Item -LiteralPath $sourceManifest -Destination $destManifest}catch{Copy-Item -LiteralPath (Join-Path $root 'artifacts-r2\SPIKEINV008A.dll') -Destination $destDll;Copy-Item -LiteralPath (Join-Path $root 'artifacts-r2\SPIKEINV008A.json') -Destination $destManifest;throw}
foreach($pair in @(@($sourceDll,$destDll),@($sourceManifest,$destManifest))){$s=Get-Item $pair[0];$d=Get-Item $pair[1];$sh=(Get-FileHash $pair[0] -Algorithm SHA256).Hash;$dh=(Get-FileHash $pair[1] -Algorithm SHA256).Hash;if($s.Length-ne$d.Length-or$sh-ne$dh){throw 'BLOCKED_INSTALL: installed R3 mismatch'};"PASS installed R3 $($d.Name) length=$($d.Length) sha256=$dh"}
if((Get-FileHash (Join-Path $mods 'SPIKEINV007A.dll') -Algorithm SHA256).Hash-ne$old007Dll-or(Get-FileHash (Join-Path $mods 'SPIKEINV007A.json') -Algorithm SHA256).Hash-ne$old007Manifest){throw 'BLOCKED_INSTALL: 007A changed'}
'PASS only the exact 008A pair was replaced; 007A files and enabled configuration were untouched'
