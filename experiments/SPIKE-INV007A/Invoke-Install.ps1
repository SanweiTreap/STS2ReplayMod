$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDll = Join-Path $root 'artifacts\SPIKEINV007A.dll'
$sourceManifest = Join-Path $root 'artifacts\SPIKEINV007A.json'
$modsRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$destinationDll = Join-Path $modsRoot 'SPIKEINV007A.dll'
$destinationManifest = Join-Path $modsRoot 'SPIKEINV007A.json'

if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) {
    throw 'BLOCKED_INSTALL: game process is running'
}
foreach ($path in @($sourceDll, $sourceManifest)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "BLOCKED_INSTALL: missing artifact $path" }
}
$targets = @(Get-ChildItem -LiteralPath $modsRoot -File | Where-Object { $_.Name.StartsWith('SPIKEINV007A', [StringComparison]::Ordinal) })
$expectedNames = @('SPIKEINV007A.dll','SPIKEINV007A.json')
if ($targets.Count -ne 2 -or @($targets | Where-Object { $expectedNames -notcontains $_.Name }).Count -ne 0) {
    throw 'BLOCKED_INSTALL: target set is not the exact installed R2 pair'
}
$oldDll = Get-Item -LiteralPath $destinationDll
$oldManifest = Get-Item -LiteralPath $destinationManifest
$oldDllHash = (Get-FileHash -LiteralPath $destinationDll -Algorithm SHA256).Hash
$oldManifestHash = (Get-FileHash -LiteralPath $destinationManifest -Algorithm SHA256).Hash
if ($oldDll.Length -ne 174592 -or $oldDllHash -ne '5447E69F11041639ED9DD92E152D1B78B7A9A6A6025906C7BDD9900929021ACD' -or
    $oldManifest.Length -ne 370 -or $oldManifestHash -ne 'AA910BF2376FE5642372FF8C64E8F3138708F843BA2335112611F3E954E32869') {
    throw 'BLOCKED_INSTALL: installed pair is not the exact known R2 identity'
}

Copy-Item -LiteralPath $sourceDll -Destination $destinationDll -Force
Copy-Item -LiteralPath $sourceManifest -Destination $destinationManifest -Force

foreach ($pair in @(
    [pscustomobject]@{ Source = $sourceDll; Destination = $destinationDll },
    [pscustomobject]@{ Source = $sourceManifest; Destination = $destinationManifest }
)) {
    $source = Get-Item -LiteralPath $pair.Source
    $destination = Get-Item -LiteralPath $pair.Destination
    $sourceHash = (Get-FileHash -LiteralPath $pair.Source -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $pair.Destination -Algorithm SHA256).Hash
    if ($source.Length -ne $destination.Length -or $sourceHash -ne $destinationHash) {
        throw "BLOCKED_INSTALL: installed identity mismatch $($destination.Name)"
    }
    Write-Output "PASS installed $($destination.Name) length=$($destination.Length) sha256=$destinationHash"
}
Write-Output 'PASS install replaced only the exact known R2 SPIKEINV007A DLL and manifest'
