$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDll = Join-Path $root 'artifacts\SPIKEINV008A.dll'
$sourceManifest = Join-Path $root 'artifacts\SPIKEINV008A.json'
$modsRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$destinationDll = Join-Path $modsRoot 'SPIKEINV008A.dll'
$destinationManifest = Join-Path $modsRoot 'SPIKEINV008A.json'

if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) { throw 'BLOCKED_INSTALL: game process is running' }
foreach ($path in @($sourceDll, $sourceManifest)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "BLOCKED_INSTALL: missing Release artifact $path" }
}
$manifest = Get-Content -Raw -LiteralPath $sourceManifest | ConvertFrom-Json
if ($manifest.id -ne 'SPIKEINV008A' -or $manifest.version -ne '0.1.0-owner-fault-matrix') { throw 'BLOCKED_INSTALL: artifact manifest identity mismatch' }
$existing = @(Get-ChildItem -LiteralPath $modsRoot -File | Where-Object { $_.Name.StartsWith('SPIKEINV008A', [StringComparison]::Ordinal) })
if ($existing.Count -ne 0 -or (Test-Path -LiteralPath $destinationDll) -or (Test-Path -LiteralPath $destinationManifest)) { throw 'BLOCKED_INSTALL: SPIKEINV008A target exists; overwrite is forbidden' }

$createdDll = $false
$createdManifest = $false
try {
    [IO.File]::Copy($sourceDll, $destinationDll, $false); $createdDll = $true
    [IO.File]::Copy($sourceManifest, $destinationManifest, $false); $createdManifest = $true
    foreach ($pair in @(
        [pscustomobject]@{ Source = $sourceDll; Destination = $destinationDll },
        [pscustomobject]@{ Source = $sourceManifest; Destination = $destinationManifest }
    )) {
        $source = Get-Item -LiteralPath $pair.Source
        $destination = Get-Item -LiteralPath $pair.Destination
        $sourceHash = (Get-FileHash -LiteralPath $pair.Source -Algorithm SHA256).Hash
        $destinationHash = (Get-FileHash -LiteralPath $pair.Destination -Algorithm SHA256).Hash
        if ($source.Length -ne $destination.Length -or $sourceHash -ne $destinationHash) { throw "BLOCKED_INSTALL: installed identity mismatch $($destination.Name)" }
        Write-Output "PASS installed new $($destination.Name) length=$($destination.Length) sha256=$destinationHash"
    }
} catch {
    if ($createdManifest -and (Test-Path -LiteralPath $destinationManifest)) { Remove-Item -LiteralPath $destinationManifest -Force }
    if ($createdDll -and (Test-Path -LiteralPath $destinationDll)) { Remove-Item -LiteralPath $destinationDll -Force }
    throw
}
Write-Output 'PASS safe install added only the new SPIKEINV008A DLL and manifest; no existing probe was modified or overwritten'
