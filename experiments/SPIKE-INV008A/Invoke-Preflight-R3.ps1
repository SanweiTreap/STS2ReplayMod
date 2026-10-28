$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$dotnet = 'C:\Users\sanwe\.dotnet\dotnet.exe'
$evidence = Join-Path $repo 'evidence\spike-inv008a'
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$game = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
$log = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs\godot.log'

function Assert-Exact([string]$Path,[long]$Length,[string]$Hash,[string]$Label) {
    $item=Get-Item -LiteralPath $Path; $actual=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if($item.Length -ne $Length -or $actual -ne $Hash){throw "BLOCKED_IDENTITY: $Label"}
    "PASS $Label length=$($item.Length) sha256=$actual"
}
function Assert-Absent([string]$Path,[string]$Label){if(Test-Path -LiteralPath $Path){throw "BLOCKED_PRIOR_ATTEMPT_UNRESOLVED: $Label"};"PASS absent $Label"}
function Tree([string]$Path){$rows=[Collections.Generic.List[string]]::new();foreach($f in Get-ChildItem -LiteralPath $Path -Recurse -File|Sort-Object FullName){$r=[IO.Path]::GetRelativePath($Path,$f.FullName).Replace('\','/');$h=(Get-FileHash $f.FullName -Algorithm SHA256).Hash;$rows.Add("$r`t$($f.Length)`t$h`n")};[pscustomobject]@{Count=$rows.Count;Hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([string]::Concat($rows))))}}
function Assert-Tree([string]$Path,[int]$Count,[string]$Hash,[string]$Label){$x=Tree $Path;if($x.Count-ne$Count-or$x.Hash-ne$Hash){throw "BLOCKED_IDENTITY: $Label tree"};"PASS $Label untouched files=$($x.Count) aggregateSha256=$($x.Hash)"}

if(Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue){throw 'BLOCKED_PREFLIGHT: game is running'}
Assert-Exact (Join-Path $game 'data_sts2_windows_x86_64\sts2.dll') 9757184 '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9' 'game DLL'
$release=Get-Content -Raw (Join-Path $game 'release_info.json')|ConvertFrom-Json;if($release.version-ne'v0.111.0'-or$release.commit-ne'41cef1ea'-or$release.branch-ne'v0.111.0'){throw 'BLOCKED_IDENTITY: release info'}
'PASS release identity v0.111.0 / 41cef1ea'
Assert-Exact (Join-Path $evidence 'setup-owner-fault.json') 2359 '56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48' 'v1 setup artifact'
Assert-Exact (Join-Path $evidence 'invalid-phase.json') 1479 'A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F' 'v1 invalid artifact'
Assert-Exact (Join-Path $evidence 'precheck-modset-r2-998e07af004a4268b1c33dd2e7644bc5.json') 242 '2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916' 'R2 precheck artifact'
Assert-Exact (Join-Path $evidence 'r2-precheck-attempt1-validation.txt') 993 'F6572728A4599FFFB8A984C6A68E133954D8F7BCD13A36055B07F209F5DC6831' 'R2 runtime validation'
foreach($old in @(@('phase0-install-results.txt',359,'53490C4C4EF59177B154ACA6BA591422FE45AF14325C686A20AC55367EE2A3B4'),@('phase0-post-install-integrity.txt',1190,'26F1D0A19F93EB429A8BF7D8642B43C8E32FF0429BDD24813E998E8A647500D6'),@('phase0-preflight-attempt1-static-audit-script-defect.txt',2757,'C728D49DFE6E1FB21D509F9C3F743852E8BD707135EE1A71EBA8190FA9B2D685'),@('phase0-preflight-results.txt',4423,'19668D971CA219CCD8EA09436C9AFA1AC82CF1B19E9E0605300CFC4A889D0D75'),@('phase0-static-audit-results.txt',592,'30B90CBC9D763EC67793FD1DD867CEEE2CE2E8089D1EA4A45ADDC8A9BF0BA12B'),@('r2-phase0-preflight-results.txt',6105,'F3160F08E11C366A488D2E2E51A5F4F0713CD6686B2A646A4936716117DD9577'),@('r2-static-audit-results.txt',867,'25CC6E3FDCC06FB5ABB879F438E1CEB2716719B0DD2C7F6A3741007F6E8E67F7'),@('r2-install-enablement-precheck.txt',301,'C4E4468EE4A699A53703F6D75F908EBB52FF5A7DE95B6EB47F2318681C0B35A0'),@('r2-install-results.txt',1391,'E7A5BB7909B7A2D46E9FA6E8BD9203FB16FF97A5AE6F06FBD9625DF9EFC00362'),@('r2-post-install-integrity.txt',1002,'D10848FAE1AE5D29A4CF655B348943519D4706B226BD02FE6DD15286953D3BDA'))){Assert-Exact (Join-Path $evidence $old[0]) $old[1] $old[2] "immutable evidence $($old[0])"}
Assert-Exact (Join-Path $root 'artifacts\SPIKEINV008A.dll') 215552 '4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717' 'v1 workspace DLL'
Assert-Exact (Join-Path $root 'artifacts\SPIKEINV008A.json') 372 'FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4' 'v1 workspace manifest'
Assert-Exact (Join-Path $root 'artifacts-r2\SPIKEINV008A.dll') 229888 'D5746F9C560B6A0E09BE6235491554A47DAB3B1AEA84078EB25BF2E8077D2615' 'R2 workspace DLL'
Assert-Exact (Join-Path $root 'artifacts-r2\SPIKEINV008A.json') 353 '0FF3138632515C43F98E111752336C223C89B751AFFA8740491AFD11FF3118A1' 'R2 workspace manifest'
Assert-Exact (Join-Path $mods 'SPIKEINV008A.dll') 229888 'D5746F9C560B6A0E09BE6235491554A47DAB3B1AEA84078EB25BF2E8077D2615' 'installed R2 DLL'
Assert-Exact (Join-Path $mods 'SPIKEINV008A.json') 353 '0FF3138632515C43F98E111752336C223C89B751AFFA8740491AFD11FF3118A1' 'installed R2 manifest'
Assert-Exact $log 252133 '7F01AE43BA2E43776D7A60413B3D6BC07875BFF38B92DE448420DDC573540171' 'R2 complete startup log'
$logText=Get-Content -Raw -LiteralPath $log;if(-not$logText.Contains('Skipping loading mod SPIKEINV007A, it is set to disabled in settings')-or-not$logText.Contains('0: SPIKE-INV008A Controlled Owner Fault Matrix (SPIKEINV008A)')-or-not$logText.Contains('Loaded 1 mods (16 total)')){throw 'BLOCKED_PRECHECK: log mod set'}
'PASS latest full log: 007A disabled and loaded set exactly 008A'
foreach($name in @('setup-owner-fault-r2.json','restart-after-setup-r2.json','postlaunch-owner-fault-r2.json','restart-after-postlaunch-r2.json','invalid-phase-r2.json','setup-owner-fault-r3.json','restart-after-setup-r3.json','postlaunch-owner-fault-r3.json','restart-after-postlaunch-r3.json','invalid-phase-r3.json')){Assert-Absent (Join-Path $evidence $name) $name;Assert-Absent (Join-Path $evidence ($name+'.tmp')) ($name+'.tmp')}
if(@(Get-ChildItem $evidence -File -Filter 'precheck-modset-r3-*.json').Count){throw 'BLOCKED_PREFLIGHT: R3 precheck already exists'}
if(Test-Path (Join-Path $evidence 'restricted-recovery')){if(@(Get-ChildItem (Join-Path $evidence 'restricted-recovery') -Directory|Where-Object Name -Match '^(setup-|postlaunch-|r2-|r3-)').Count){throw 'BLOCKED_PRIOR_ATTEMPT_UNRESOLVED: recovery exists'}}
Assert-Tree (Join-Path $repo 'experiments\SPIKE-INV003A') 78 '31402E2455182279F56E544B78FA8E58BCEE39B661FEB4F8FE55FD498BBEA619' 'SPIKE-INV003A tree'
Assert-Tree (Join-Path $repo 'experiments\SPIKE-INV007A') 137 'AB18646445A2B17B15520553279328F6F071F0FB34B359C252DF6FC121C515A6' 'SPIKE-INV007A tree'
Assert-Tree (Join-Path $repo 'evidence\spike-inv003a') 68 '12E9C6F93CF503B875953662CE6EC5CB204C558C211C915F4F5BEE5A5B437D0A' 'SPIKE-INV003A evidence'
Assert-Tree (Join-Path $repo 'evidence\spike-inv007a') 49 '308B818EFB888555161727051B36EE7A13C28A9541EA8D64AD75795C78F0DBCD' 'SPIKE-INV007A evidence'
& $dotnet run --project (Join-Path $root 'tests\SPIKEINV008A.Tests.csproj') -c Release;if($LASTEXITCODE){throw 'BLOCKED_INSTRUMENTATION: tests'}
& $dotnet build (Join-Path $root 'SPIKEINV008A.csproj') -c Release;if($LASTEXITCODE){throw 'BLOCKED_INSTRUMENTATION: build'}
& (Join-Path $root 'Invoke-StaticAudit.ps1') -Configuration Release
& $dotnet run --project (Join-Path $root 'tests\PatchSmoke\PatchSmoke.csproj') -c Release -- (Join-Path $root 'artifacts-r3\SPIKEINV008A.dll');if($LASTEXITCODE){throw 'BLOCKED_INSTRUMENTATION: Harmony smoke'}
foreach($p in @((Join-Path $root 'artifacts-r3\SPIKEINV008A.dll'),(Join-Path $root 'artifacts-r3\SPIKEINV008A.json'))){$i=Get-Item $p;"PASS R3 artifact $($i.Name) length=$($i.Length) sha256=$((Get-FileHash $p -Algorithm SHA256).Hash)"}
Assert-Exact (Join-Path $root 'artifacts-r2\SPIKEINV008A.dll') 229888 'D5746F9C560B6A0E09BE6235491554A47DAB3B1AEA84078EB25BF2E8077D2615' 'post-build R2 workspace DLL'
'PASS SPIKE_INV008A_R3_OFFLINE_PREFLIGHT'
