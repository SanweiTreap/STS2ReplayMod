$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$dotnet = 'C:\Users\sanwe\.dotnet\dotnet.exe'
$gameDll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'
$fixture = Join-Path $repo 'evidence\spike-inv003a\protected-backup\20260907T175924.5461327Z\profile1\replays\latest.mcr'
$r1Artifact = Join-Path $repo 'evidence\spike-inv007a\runtime-artifact.json'
$r1Recovery = Join-Path $repo 'evidence\spike-inv007a\restricted-recovery\20260914T081712.1432035Z'
$priorArtifact = Join-Path $repo 'evidence\spike-inv007a\runtime-artifact-r2.json'
$priorRecovery = Join-Path $repo 'evidence\spike-inv007a\restricted-recovery\r2-20260914T091555.1356848Z'
$installedDll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\SPIKEINV007A.dll'
$installedManifest = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\SPIKEINV007A.json'

if ((Split-Path -Leaf $root) -ne 'SPIKE-INV007A') { throw 'BLOCKED_IDENTITY: source directory mismatch' }
$manifestPath = Join-Path $root 'SPIKEINV007A.json'
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
if ($manifest.id -ne 'SPIKEINV007A') { throw 'BLOCKED_IDENTITY: probe id mismatch' }
if ($manifest.version -ne '0.3.0-direct-write-r3') { throw 'BLOCKED_IDENTITY: probe version mismatch' }
$models = Get-Content -Raw -LiteralPath (Join-Path $root 'ProbeModels.cs')
if (-not $models.Contains('spike-inv007a.direct-write.v3', [StringComparison]::Ordinal)) {
    throw 'BLOCKED_IDENTITY: schema mismatch'
}
if (Test-Path -LiteralPath (Join-Path $repo 'evidence\spike-inv007a\runtime-artifact-r3.json')) {
    throw 'BLOCKED_ARTIFACT_WRITE: R3 artifact already exists'
}
if (Test-Path -LiteralPath (Join-Path $repo 'evidence\spike-inv007a\runtime-artifact-r3.json.tmp')) {
    throw 'BLOCKED_ARTIFACT_WRITE: R3 artifact temporary already exists'
}

function Assert-ExactFile([string]$Path, [long]$Length, [string]$Sha256, [string]$Label) {
    $item = Get-Item -LiteralPath $Path
    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if ($item.Length -ne $Length -or $hash -ne $Sha256) { throw "BLOCKED_IDENTITY: $Label mismatch" }
    Write-Output "PASS $Label length=$($item.Length) sha256=$hash"
}

function Assert-Recovery([string]$Path, [string]$Label) {
    $recoveryFiles = @(Get-ChildItem -LiteralPath $Path -File -Recurse)
    if ($recoveryFiles.Count -ne 12) { throw "BLOCKED_IDENTITY: $Label file count mismatch" }
    $recoveryLines = [Collections.Generic.List[string]]::new()
    foreach ($file in $recoveryFiles) {
        $relative = [IO.Path]::GetRelativePath($Path, $file.FullName).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        $recoveryLines.Add("$relative`t$($file.Length)`t$hash`n")
    }
    $recoveryArray = $recoveryLines.ToArray()
    [Array]::Sort($recoveryArray, [StringComparer]::Ordinal)
    $bytes = [Text.Encoding]::UTF8.GetBytes([string]::Concat($recoveryArray))
    $aggregate = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
    if ($aggregate -ne '9719C685A08E86786D96DE55309F19C6242A71D8001B1E841BF88B89598AB9E2') {
        throw "BLOCKED_IDENTITY: $Label aggregate mismatch"
    }
    Write-Output "PASS $Label files=$($recoveryFiles.Count) aggregateSha256=$aggregate"
}

Assert-ExactFile $r1Artifact 3108 'D38EC65F8380DDC2AFF1C8F6EB9E7FDEBEAB1D706424C4471C36DAAD0488ACA2' 'R1 artifact identity'
Assert-Recovery $r1Recovery 'R1 recovery identity'
Assert-ExactFile $priorArtifact 19259 '4B48A75F650A687F206255F2425140B3F640277E3D62F21495F99CC6086B691F' 'R2 artifact identity'
Assert-Recovery $priorRecovery 'R2 recovery identity'
Assert-ExactFile $installedDll 174592 '5447E69F11041639ED9DD92E152D1B78B7A9A6A6025906C7BDD9900929021ACD' 'installed R2 DLL identity'
Assert-ExactFile $installedManifest 370 'AA910BF2376FE5642372FF8C64E8F3138708F843BA2335112611F3E954E32869' 'installed R2 manifest identity'
if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) {
    throw 'BLOCKED_IDENTITY: game process is running'
}

$lockedSources = @(
    @('LiveSeedCandidate.cs',15613,'84788D8138F4814135120FE3C57F4B7910F4B632674E0ECD63EAB0081D202C1B'),
    @('BoundaryAudit.cs',11179,'BFA476A70E78A57C14B3BDA8B1AAD35978A00A8353ACE7338C57ADC7190F47EB'),
    @('ContractRules.cs',1683,'3E594440E2E0422AD0A7F605C3C9AECF0C0C32A50D631DDC818545817985BA0F'),
    @('ScopeSafeSession.cs',2990,'C72AD59A4BA903C0788DA71AD4820F366BC04DDCCEC418000A041F724A3B6F31')
)
foreach ($source in $lockedSources) { Assert-ExactFile (Join-Path $root $source[0]) $source[1] $source[2] "locked source $($source[0])" }

$game = Get-Item -LiteralPath $gameDll
$gameHash = (Get-FileHash -LiteralPath $gameDll -Algorithm SHA256).Hash
if ($game.Length -ne 9757184 -or $gameHash -ne '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9') {
    throw 'BLOCKED_IDENTITY: game DLL mismatch'
}
$release = Get-Content -Raw -LiteralPath (Join-Path (Split-Path -Parent (Split-Path -Parent $gameDll)) 'release_info.json') | ConvertFrom-Json
if ($release.version -ne 'v0.111.0' -or $release.commit -ne '41cef1ea' -or $release.branch -ne 'v0.111.0') {
    throw 'BLOCKED_IDENTITY: release identity mismatch'
}
$fix = Get-Item -LiteralPath $fixture
$fixHash = (Get-FileHash -LiteralPath $fixture -Algorithm SHA256).Hash
if ($fix.Length -ne 20441 -or $fixHash -ne '4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631') {
    throw 'BLOCKED_IDENTITY: fixture mismatch'
}

Write-Output 'PASS controlled R3 identity id=SPIKEINV007A directory=SPIKE-INV007A schema=spike-inv007a.direct-write.v3 version=0.3.0-direct-write-r3'
Write-Output "PASS game identity version=$($release.version) commit=$($release.commit) branch=$($release.branch) length=$($game.Length) sha256=$gameHash"
Write-Output "PASS fixture identity length=$($fix.Length) sha256=$fixHash"

& $dotnet run --project (Join-Path $root 'tests\SPIKEINV007A.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: synthetic tests failed' }

& $dotnet build (Join-Path $root 'SPIKEINV007A.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: Release build failed' }

& (Join-Path $root 'Invoke-StaticAudit.ps1') -Configuration Release

$artifactDll = Join-Path $root 'artifacts\SPIKEINV007A.dll'
& $dotnet run --project (Join-Path $root 'tests\PatchSmoke\PatchSmoke.csproj') -c Release -- $artifactDll
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: Harmony patch smoke failed' }

foreach ($path in @($artifactDll, (Join-Path $root 'artifacts\SPIKEINV007A.json'))) {
    $item = Get-Item -LiteralPath $path
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    Write-Output "PASS artifact $($item.Name) length=$($item.Length) sha256=$hash"
}
