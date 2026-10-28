$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$dotnet = 'C:\Users\sanwe\.dotnet\dotnet.exe'
$gameDll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'
$fixture = Join-Path $repo 'evidence\spike-inv003a\protected-backup\20260907T175924.5461327Z\profile1\replays\latest.mcr'
$game = Get-Item -LiteralPath $gameDll; $gameHash = (Get-FileHash -LiteralPath $gameDll -Algorithm SHA256).Hash
if ($game.Length -ne 9757184 -or $gameHash -ne '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9') { throw 'BLOCKED_IDENTITY: game DLL mismatch' }
$release = Get-Content -Raw -LiteralPath (Join-Path (Split-Path -Parent (Split-Path -Parent $gameDll)) 'release_info.json') | ConvertFrom-Json
if ($release.version -ne 'v0.111.0' -or $release.commit -ne '41cef1ea' -or $release.branch -ne 'v0.111.0') { throw 'BLOCKED_IDENTITY: release identity mismatch' }
$fix = Get-Item -LiteralPath $fixture; $fixHash = (Get-FileHash -LiteralPath $fixture -Algorithm SHA256).Hash
if ($fix.Length -ne 20441 -or $fixHash -ne '4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631') { throw 'BLOCKED_IDENTITY: fixture mismatch' }
Write-Output "PASS game identity length=$($game.Length) sha256=$gameHash"
Write-Output "PASS fixture identity length=$($fix.Length) sha256=$fixHash"
& $dotnet run --project (Join-Path $root 'tests\SPIKEINV006B.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: synthetic tests failed' }
& $dotnet build (Join-Path $root 'SPIKEINV006B.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: Release build failed' }
& (Join-Path $root 'Invoke-StaticAudit.ps1') -Configuration Release
$inspector = Join-Path $repo 'experiments\SPIKE-INV003A\FixtureInspector\FixtureInspector.csproj'
$artifactDll = Join-Path $root 'artifacts\SPIKEINV006B.dll'
& $dotnet run --project $inspector -c Release -- --patch-smoke $artifactDll
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: Harmony patch smoke failed' }
foreach ($path in @($artifactDll,(Join-Path $root 'artifacts\SPIKEINV006B.json'))) { $item=Get-Item -LiteralPath $path; $hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash; Write-Output "PASS artifact $($item.Name) length=$($item.Length) sha256=$hash" }
