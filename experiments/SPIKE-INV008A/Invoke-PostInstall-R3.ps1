$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$repo=Split-Path -Parent (Split-Path -Parent $root)
$mods='C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
function Show-Exact($path,$length,$hash,$label){$i=Get-Item -LiteralPath $path;$h=(Get-FileHash $path -Algorithm SHA256).Hash;if($i.Length-ne$length-or$h-ne$hash){throw "BLOCKED_POST_INSTALL: $label"};"PASS $label length=$($i.Length) sha256=$h"}
if(Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue){throw 'BLOCKED_POST_INSTALL: game running'}
Show-Exact (Join-Path $mods 'SPIKEINV008A.dll') 235008 '2FE763CC52805D1C9ED513D97B0AF8370EB4BF659E6CB8BF376F99747D047F01' 'installed R3 DLL'
Show-Exact (Join-Path $mods 'SPIKEINV008A.json') 353 '1B0ECD1E95639DA0B95EB84576C17D4D0CAB67D70D2E15496702533A028D22A3' 'installed R3 manifest'
Show-Exact (Join-Path $mods 'SPIKEINV007A.dll') 187904 '9AEC3090F5BC0BC301437D789AAE84748B7A3BCE6AAC9D3111994374C7B1C46E' 'untouched installed 007A DLL'
Show-Exact (Join-Path $mods 'SPIKEINV007A.json') 370 '0E3FA78CE308FD14997A6C2BB02AF12335A629354A7837523EC489E11D005289' 'untouched installed 007A manifest'
Show-Exact (Join-Path $root 'artifacts\SPIKEINV008A.dll') 215552 '4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717' 'immutable v1 workspace DLL'
Show-Exact (Join-Path $root 'artifacts-r2\SPIKEINV008A.dll') 229888 'D5746F9C560B6A0E09BE6235491554A47DAB3B1AEA84078EB25BF2E8077D2615' 'immutable R2 workspace DLL'
$evidence=Join-Path $repo 'evidence\spike-inv008a'
foreach($name in @('setup-owner-fault-r3.json','restart-after-setup-r3.json','postlaunch-owner-fault-r3.json','restart-after-postlaunch-r3.json','invalid-phase-r3.json')){if(Test-Path (Join-Path $evidence $name)){throw "BLOCKED_POST_INSTALL: unexpected $name"}}
if(@(Get-ChildItem $evidence -File -Filter 'precheck-modset-r3-*.json').Count){throw 'BLOCKED_POST_INSTALL: unexpected R3 precheck'}
'PASS no R3 runtime artifact/precheck/recovery exists'
'PASS game stopped; R3 installed without launching or changing enabled configuration'
