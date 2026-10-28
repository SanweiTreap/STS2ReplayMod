$ErrorActionPreference='Stop';$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$mods='C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods';$userRoot='C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122'
$srcDll=Join-Path $root 'artifacts-r4\SPIKEINV008A.dll';$srcManifest=Join-Path $root 'artifacts-r4\SPIKEINV008A.json';$dstDll=Join-Path $mods 'SPIKEINV008A.dll';$dstManifest=Join-Path $mods 'SPIKEINV008A.json'
function Exact($p,$length,$hash,$label){$i=Get-Item -LiteralPath $p;$h=(Get-FileHash $p -Algorithm SHA256).Hash;if($i.Length-ne$length-or$h-ne$hash){throw "BLOCKED_INSTALL: $label"};"PASS $label length=$($i.Length) sha256=$h"}
if(Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue){throw 'BLOCKED_INSTALL: game running'}
Exact $dstDll 235008 '2FE763CC52805D1C9ED513D97B0AF8370EB4BF659E6CB8BF376F99747D047F01' 'installed R3 DLL';Exact $dstManifest 353 '1B0ECD1E95639DA0B95EB84576C17D4D0CAB67D70D2E15496702533A028D22A3' 'installed R3 manifest';Exact (Join-Path $userRoot 'settings.save') 5238 '2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F' 'fixed settings'
$m=Get-Content -Raw $srcManifest|ConvertFrom-Json;if($m.id-ne'SPIKEINV008A'-or$m.version-ne'0.4.0'){throw 'BLOCKED_INSTALL: R4 manifest'}
$h7d=(Get-FileHash (Join-Path $mods 'SPIKEINV007A.dll') -Algorithm SHA256).Hash;$h7m=(Get-FileHash (Join-Path $mods 'SPIKEINV007A.json') -Algorithm SHA256).Hash
try{Copy-Item -LiteralPath $srcDll -Destination $dstDll;Copy-Item -LiteralPath $srcManifest -Destination $dstManifest}catch{Copy-Item -LiteralPath (Join-Path $root 'artifacts-r3\SPIKEINV008A.dll') -Destination $dstDll;Copy-Item -LiteralPath (Join-Path $root 'artifacts-r3\SPIKEINV008A.json') -Destination $dstManifest;throw}
foreach($x in @(@($srcDll,$dstDll),@($srcManifest,$dstManifest))){$s=Get-Item $x[0];$d=Get-Item $x[1];$sh=(Get-FileHash $x[0] -Algorithm SHA256).Hash;$dh=(Get-FileHash $x[1] -Algorithm SHA256).Hash;if($s.Length-ne$d.Length-or$sh-ne$dh){throw 'BLOCKED_INSTALL: pair mismatch'};"PASS installed R4 $($d.Name) length=$($d.Length) sha256=$dh"}
if((Get-FileHash (Join-Path $mods 'SPIKEINV007A.dll') -Algorithm SHA256).Hash-ne$h7d-or(Get-FileHash (Join-Path $mods 'SPIKEINV007A.json') -Algorithm SHA256).Hash-ne$h7m){throw 'BLOCKED_INSTALL: 007A changed'}
'PASS only 008A pair replaced; R3 evidence, 007A files and enabled configuration untouched'
