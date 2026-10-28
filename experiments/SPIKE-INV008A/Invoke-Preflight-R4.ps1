$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path;$repo=Split-Path -Parent (Split-Path -Parent $root)
$dotnet='C:\Users\sanwe\.dotnet\dotnet.exe';$evidence=Join-Path $repo 'evidence\spike-inv008a'
$mods='C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods';$game='C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
$userRoot='C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122';$log='C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs\godot.log'
function Exact($p,$length,$hash,$label){$i=Get-Item -LiteralPath $p;$h=(Get-FileHash $p -Algorithm SHA256).Hash;if($i.Length-ne$length-or$h-ne$hash){throw "BLOCKED_IDENTITY: $label"};"PASS $label length=$($i.Length) sha256=$h"}
function Absent($p,$label){if(Test-Path -LiteralPath $p){throw "BLOCKED_PRIOR_ATTEMPT_UNRESOLVED: $label"};"PASS absent $label"}
function Tree($p){$rows=[Collections.Generic.List[string]]::new();foreach($f in Get-ChildItem -LiteralPath $p -Recurse -File|Sort-Object FullName){$r=[IO.Path]::GetRelativePath($p,$f.FullName).Replace('\','/');$h=(Get-FileHash $f.FullName -Algorithm SHA256).Hash;$rows.Add("$r`t$($f.Length)`t$h`n")};[pscustomobject]@{Count=$rows.Count;Hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([string]::Concat($rows))))}}
function ExactTree($p,$count,$hash,$label){$x=Tree $p;if($x.Count-ne$count-or$x.Hash-ne$hash){throw "BLOCKED_IDENTITY: $label tree"};"PASS $label untouched files=$($x.Count) aggregateSha256=$($x.Hash)"}
if(Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue){throw 'BLOCKED_PREFLIGHT: game running'}
Exact (Join-Path $game 'data_sts2_windows_x86_64\sts2.dll') 9757184 '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9' 'game DLL'
$release=Get-Content -Raw (Join-Path $game 'release_info.json')|ConvertFrom-Json;if($release.version-ne'v0.111.0'-or$release.commit-ne'41cef1ea'-or$release.branch-ne'v0.111.0'){throw 'BLOCKED_IDENTITY: release'};'PASS release v0.111.0 / 41cef1ea'
foreach($x in @(@('setup-owner-fault.json',2359,'56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48'),@('invalid-phase.json',1479,'A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F'),@('precheck-modset-r2-998e07af004a4268b1c33dd2e7644bc5.json',242,'2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916'),@('setup-owner-fault-r3.json',3740,'5C8E49B98F7BC9A647033F779DC3CACCEBE3D840214E4D95995EBADB7C0B13F0'),@('r2-precheck-attempt1-validation.txt',993,'F6572728A4599FFFB8A984C6A68E133954D8F7BCD13A36055B07F209F5DC6831'),@('r3-setup-attempt1-validation.txt',2156,'CEC46FF2A52F138F00041B948A96D876D039F4528F1BB0174AA6E567970B060E'),@('project-lead-r3-prior-integrity-review.md',3539,'19278145B12A7CDF50894832824BDEE378944893E087C5D56BED650CC251F9B8'))){Exact (Join-Path $evidence $x[0]) $x[1] $x[2] "fixed evidence $($x[0])"}
$r3=Get-Content -Raw (Join-Path $evidence 'setup-owner-fault-r3.json')|ConvertFrom-Json;if($r3.terminalClassification-ne'BLOCKED_PRIOR_INTEGRITY'-or$r3.firstFailureSite-ne'PriorIntegrity'){throw 'BLOCKED_IDENTITY: R3 terminal'};foreach($n in 'protectionBefore','protectionAfter','recoveryRoot','candidate','fault','audit','teardown','restart'){if($null-ne$r3.$n){throw "BLOCKED_IDENTITY: R3 $n not null"}};'PASS R3 fixed pre-business-null contract'
foreach($n in 'restart-after-setup-r3.json','postlaunch-owner-fault-r3.json','restart-after-postlaunch-r3.json','invalid-phase-r3.json'){Absent (Join-Path $evidence $n) $n}
if(Test-Path (Join-Path $evidence 'restricted-recovery')){if(@(Get-ChildItem (Join-Path $evidence 'restricted-recovery') -Directory|Where-Object Name -Match '^r3-').Count){throw 'BLOCKED_PRIOR_ATTEMPT_UNRESOLVED: R3 recovery'}};'PASS R3 recovery absent'
Exact (Join-Path $root 'artifacts\SPIKEINV008A.dll') 215552 '4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717' 'v1 workspace DLL'
Exact (Join-Path $root 'artifacts-r2\SPIKEINV008A.dll') 229888 'D5746F9C560B6A0E09BE6235491554A47DAB3B1AEA84078EB25BF2E8077D2615' 'R2 workspace DLL'
Exact (Join-Path $root 'artifacts-r3\SPIKEINV008A.dll') 235008 '2FE763CC52805D1C9ED513D97B0AF8370EB4BF659E6CB8BF376F99747D047F01' 'R3 workspace DLL'
Exact (Join-Path $root 'artifacts-r3\SPIKEINV008A.json') 353 '1B0ECD1E95639DA0B95EB84576C17D4D0CAB67D70D2E15496702533A028D22A3' 'R3 workspace manifest'
Exact (Join-Path $mods 'SPIKEINV008A.dll') 235008 '2FE763CC52805D1C9ED513D97B0AF8370EB4BF659E6CB8BF376F99747D047F01' 'installed R3 DLL'
Exact (Join-Path $mods 'SPIKEINV008A.json') 353 '1B0ECD1E95639DA0B95EB84576C17D4D0CAB67D70D2E15496702533A028D22A3' 'installed R3 manifest'
Exact (Join-Path $userRoot 'settings.save') 5238 '2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F' 'Project Lead post-R3 settings'
Exact $log 184193 'D6AABEF1BDB7C2102CFA338CFC16EC10DF157806CBEF15C6001C9385727BA31F' 'complete R3 log'
$t=Get-Content -Raw $log;if(-not$t.Contains('Skipping loading mod SPIKEINV007A, it is set to disabled in settings')-or-not$t.Contains('Loaded 1 mods (16 total)')){throw 'BLOCKED_PREFLIGHT: loaded set log'};'PASS latest complete log proves 007A disabled / exactly 008A loaded'
foreach($n in 'setup-owner-fault-r4.json','restart-after-setup-r4.json','postlaunch-owner-fault-r4.json','restart-after-postlaunch-r4.json','invalid-phase-r4.json'){Absent (Join-Path $evidence $n) $n;Absent (Join-Path $evidence ($n+'.tmp')) ($n+'.tmp')};if(@(Get-ChildItem $evidence -File -Filter 'precheck-modset-r4-*.json').Count){throw 'BLOCKED_PREFLIGHT: R4 precheck exists'}
ExactTree (Join-Path $repo 'experiments\SPIKE-INV003A') 78 '31402E2455182279F56E544B78FA8E58BCEE39B661FEB4F8FE55FD498BBEA619' 'SPIKE-INV003A tree'
ExactTree (Join-Path $repo 'experiments\SPIKE-INV007A') 137 'AB18646445A2B17B15520553279328F6F071F0FB34B359C252DF6FC121C515A6' 'SPIKE-INV007A tree'
& $dotnet run --project (Join-Path $root 'tests\SPIKEINV008A.Tests.csproj') -c Release;if($LASTEXITCODE){throw 'BLOCKED_INSTRUMENTATION: tests'}
& $dotnet build (Join-Path $root 'SPIKEINV008A.csproj') -c Release;if($LASTEXITCODE){throw 'BLOCKED_INSTRUMENTATION: build'}
& (Join-Path $root 'Invoke-StaticAudit.ps1') -Configuration Release
& $dotnet run --project (Join-Path $root 'tests\PatchSmoke\PatchSmoke.csproj') -c Release -- (Join-Path $root 'artifacts-r4\SPIKEINV008A.dll');if($LASTEXITCODE){throw 'BLOCKED_INSTRUMENTATION: smoke'}
foreach($p in @((Join-Path $root 'artifacts-r4\SPIKEINV008A.dll'),(Join-Path $root 'artifacts-r4\SPIKEINV008A.json'))){$i=Get-Item $p;"PASS R4 artifact $($i.Name) length=$($i.Length) sha256=$((Get-FileHash $p -Algorithm SHA256).Hash)"}
Exact (Join-Path $root 'artifacts-r3\SPIKEINV008A.dll') 235008 '2FE763CC52805D1C9ED513D97B0AF8370EB4BF659E6CB8BF376F99747D047F01' 'post-build immutable R3 DLL'
'PASS SPIKE_INV008A_R4_OFFLINE_PREFLIGHT_AND_REAL_PRIOR_GATE_REACHABILITY'
