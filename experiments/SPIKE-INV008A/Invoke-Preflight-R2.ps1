$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$dotnet = 'C:\Users\sanwe\.dotnet\dotnet.exe'
$gameDll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'
$releaseInfo = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\release_info.json'
$fixture = Join-Path $repo 'evidence\spike-inv003a\protected-backup\20260907T175924.5461327Z\profile1\replays\latest.mcr'
$priorArtifact = Join-Path $repo 'evidence\spike-inv007a\runtime-artifact-r3.json'
$priorRecovery = Join-Path $repo 'evidence\spike-inv007a\restricted-recovery\r3-20260914T152630.2908445Z'
$evidenceRoot = Join-Path $repo 'evidence\spike-inv008a'
$modsRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'

function Assert-ExactFile([string]$Path, [long]$Length, [string]$Sha256, [string]$Label) {
    $item = Get-Item -LiteralPath $Path; $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if ($item.Length -ne $Length -or $hash -ne $Sha256) { throw "BLOCKED_IDENTITY: $Label mismatch" }
    Write-Output "PASS $Label length=$($item.Length) sha256=$hash"
}
function Get-TreeAggregate([string]$Path) {
    $lines=[Collections.Generic.List[string]]::new()
    foreach($file in Get-ChildItem -LiteralPath $Path -File -Recurse | Sort-Object FullName){$relative=[IO.Path]::GetRelativePath($Path,$file.FullName).Replace('\','/');$hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;$lines.Add("$relative`t$($file.Length)`t$hash`n")}
    $bytes=[Text.Encoding]::UTF8.GetBytes([string]::Concat($lines)); [pscustomobject]@{Count=$lines.Count;Hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))}
}
function Assert-Tree([string]$Path,[int]$Count,[string]$Sha256,[string]$Label){$actual=Get-TreeAggregate $Path;if($actual.Count -ne $Count -or $actual.Hash -ne $Sha256){throw "BLOCKED_IDENTITY: $Label tree mismatch"};Write-Output "PASS $Label untouched files=$($actual.Count) aggregateSha256=$($actual.Hash)"}

if ((Split-Path -Leaf $root) -ne 'SPIKE-INV008A') { throw 'BLOCKED_IDENTITY: source directory mismatch' }
$manifestPath=Join-Path $root 'SPIKEINV008A.json'; $manifest=Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
if ($manifest.id -ne 'SPIKEINV008A' -or $manifest.version -ne '0.2.0' -or $manifest.version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') { throw 'BLOCKED_IDENTITY: R2 manifest/SemVer mismatch' }
$models=Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeModels.cs')
if (-not $models.Contains('spike-inv008a.owner-fault.v2',[StringComparison]::Ordinal) -or -not $models.Contains('0.2.0-owner-fault-matrix-r2',[StringComparison]::Ordinal)) { throw 'BLOCKED_IDENTITY: R2 schema/revision mismatch' }
if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) { throw 'BLOCKED_PREFLIGHT: game process is running' }

Assert-ExactFile (Join-Path $evidenceRoot 'setup-owner-fault.json') 2359 '56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48' 'immutable v1 setup artifact'
Assert-ExactFile (Join-Path $evidenceRoot 'invalid-phase.json') 1479 'A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F' 'immutable v1 invalid artifact'
foreach($old in @(
    @('phase0-install-results.txt',359,'53490C4C4EF59177B154ACA6BA591422FE45AF14325C686A20AC55367EE2A3B4'),
    @('phase0-post-install-integrity.txt',1190,'26F1D0A19F93EB429A8BF7D8642B43C8E32FF0429BDD24813E998E8A647500D6'),
    @('phase0-preflight-attempt1-static-audit-script-defect.txt',2757,'C728D49DFE6E1FB21D509F9C3F743852E8BD707135EE1A71EBA8190FA9B2D685'),
    @('phase0-preflight-results.txt',4423,'19668D971CA219CCD8EA09436C9AFA1AC82CF1B19E9E0605300CFC4A889D0D75'),
    @('phase0-static-audit-results.txt',592,'30B90CBC9D763EC67793FD1DD867CEEE2CE2E8089D1EA4A45ADDC8A9BF0BA12B')
)){Assert-ExactFile (Join-Path $evidenceRoot $old[0]) $old[1] $old[2] "immutable v1 evidence $($old[0])"}
Assert-ExactFile (Join-Path $root 'artifacts\SPIKEINV008A.dll') 215552 '4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717' 'immutable v1 workspace DLL'
Assert-ExactFile (Join-Path $root 'artifacts\SPIKEINV008A.json') 372 'FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4' 'immutable v1 workspace manifest'
$installedDll=Join-Path $modsRoot 'SPIKEINV008A.dll';$installedManifest=Join-Path $modsRoot 'SPIKEINV008A.json'
Assert-ExactFile $installedDll 215552 '4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717' 'installed v1 DLL'
Assert-ExactFile $installedManifest 372 'FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4' 'installed v1 manifest'
foreach($name in @('setup-owner-fault-r2.json','restart-after-setup-r2.json','postlaunch-owner-fault-r2.json','restart-after-postlaunch-r2.json','invalid-phase-r2.json')){foreach($suffix in @('','.tmp')){if(Test-Path -LiteralPath (Join-Path $evidenceRoot ($name+$suffix))){throw "BLOCKED_ARTIFACT_WRITE: R2 artifact exists $name$suffix"}}}
if(@(Get-ChildItem -LiteralPath $evidenceRoot -File -Filter 'precheck-modset-r2-*.json').Count -ne 0){throw 'BLOCKED_PREFLIGHT: unexpected existing R2 precheck evidence'}
$recoveryRoot=Join-Path $evidenceRoot 'restricted-recovery';if(Test-Path -LiteralPath $recoveryRoot){if(@(Get-ChildItem -LiteralPath $recoveryRoot -Directory).Count -ne 0){throw 'BLOCKED_PRIOR_ATTEMPT_UNRESOLVED: recovery directory unexpectedly exists'}}

Assert-ExactFile $gameDll 9757184 '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9' 'game DLL identity'
$release = Get-Content -Raw -LiteralPath $releaseInfo | ConvertFrom-Json
if ($release.version -ne 'v0.111.0' -or $release.commit -ne '41cef1ea' -or $release.branch -ne 'v0.111.0') { throw 'BLOCKED_IDENTITY: release identity mismatch' }
Write-Output "PASS release identity version=$($release.version) commit=$($release.commit) branch=$($release.branch)"
Assert-ExactFile $fixture 20441 '4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631' 'fixed replay fixture identity'
Assert-ExactFile $priorArtifact 19953 'AE4D9998CAF03DA3FB2F17656E12A0216B7160FD69017154B128C3F6365C9553' 'fixed R3 artifact identity'
Assert-Tree $priorRecovery 12 '9719C685A08E86786D96DE55309F19C6242A71D8001B1E841BF88B89598AB9E2' 'fixed R3 recovery identity'
Assert-Tree (Join-Path $repo 'experiments\SPIKE-INV003A') 78 '31402E2455182279F56E544B78FA8E58BCEE39B661FEB4F8FE55FD498BBEA619' 'SPIKE-INV003A tree'
Assert-Tree (Join-Path $repo 'experiments\SPIKE-INV007A') 137 'AB18646445A2B17B15520553279328F6F071F0FB34B359C252DF6FC121C515A6' 'SPIKE-INV007A tree'
Assert-Tree (Join-Path $repo 'evidence\spike-inv003a') 68 '12E9C6F93CF503B875953662CE6EC5CB204C558C211C915F4F5BEE5A5B437D0A' 'SPIKE-INV003A evidence'
Assert-Tree (Join-Path $repo 'evidence\spike-inv007a') 49 '308B818EFB888555161727051B36EE7A13C28A9541EA8D64AD75795C78F0DBCD' 'SPIKE-INV007A evidence'

& $dotnet run --project (Join-Path $root 'tests\SPIKEINV008A.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: R2 tests failed' }
& $dotnet build (Join-Path $root 'SPIKEINV008A.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: R2 Release build failed' }
& (Join-Path $root 'Invoke-StaticAudit.ps1') -Configuration Release
$artifactDll = Join-Path $root 'artifacts-r2\SPIKEINV008A.dll'
& $dotnet run --project (Join-Path $root 'tests\PatchSmoke\PatchSmoke.csproj') -c Release -- $artifactDll
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: Harmony smoke failed' }
foreach($path in @($artifactDll,(Join-Path $root 'artifacts-r2\SPIKEINV008A.json'))){$item=Get-Item -LiteralPath $path;$hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash;Write-Output "PASS R2 release artifact $($item.Name) length=$($item.Length) sha256=$hash"}
Assert-ExactFile (Join-Path $root 'artifacts\SPIKEINV008A.dll') 215552 '4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717' 'post-build immutable v1 workspace DLL'
Assert-ExactFile (Join-Path $root 'artifacts\SPIKEINV008A.json') 372 'FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4' 'post-build immutable v1 workspace manifest'
Write-Output 'PASS SPIKE_INV008A_R2_OFFLINE_PREFLIGHT'
