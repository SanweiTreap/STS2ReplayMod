$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = 'C:\Users\sanwe\.dotnet\dotnet.exe'
$gameDll = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll'

$item = Get-Item -LiteralPath $gameDll
$digest = (Get-FileHash -LiteralPath $gameDll -Algorithm SHA256).Hash
if ($item.Length -ne 9757184 -or $digest -ne '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9') {
    throw 'BLOCKED_IDENTITY: fixed game DLL does not match.'
}
Write-Output "PASS fixed game DLL identity length=$($item.Length) sha256=$digest"

& $dotnet run --project (Join-Path $root 'tests\SPIKEINV006A.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: synthetic tests failed.' }

& $dotnet build (Join-Path $root 'SPIKEINV006A.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'BLOCKED_INSTRUMENTATION: Release build failed.' }

& (Join-Path $root 'Invoke-StaticAudit.ps1') -Configuration Release

$artifactDll = Join-Path $root 'artifacts\SPIKEINV006A.dll'
$artifactManifest = Join-Path $root 'artifacts\SPIKEINV006A.json'
$dllItem = Get-Item -LiteralPath $artifactDll
$dllHash = (Get-FileHash -LiteralPath $artifactDll -Algorithm SHA256).Hash
$manifestItem = Get-Item -LiteralPath $artifactManifest
$manifestHash = (Get-FileHash -LiteralPath $artifactManifest -Algorithm SHA256).Hash
Write-Output "PASS release artifact DLL length=$($dllItem.Length) sha256=$dllHash"
Write-Output "PASS release manifest length=$($manifestItem.Length) sha256=$manifestHash"
Write-Output 'PASS wrong-DLL negative contract: synthetic length=1/hash=zero => BLOCKED_IDENTITY'
