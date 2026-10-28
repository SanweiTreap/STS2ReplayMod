param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$assembly = Join-Path $root "bin\$Configuration\net9.0\SPIKEINV006A.dll"
$productionSources = Get-ChildItem -LiteralPath $root -Filter '*.cs' -File
$forbidden = @(
    'ISaveStore',
    'CloudSaveStore',
    'SteamRemoteSaveStore',
    '__args',
    'SummarizeArguments',
    'modded/profile',
    'settings.save',
    'payloadHash',
    'payloadLength',
    'HashFixedAssembly',
    'Canonicalize',
    'Sha256Text'
)

foreach ($term in $forbidden) {
    foreach ($source in $productionSources) {
        if (Select-String -LiteralPath $source.FullName -SimpleMatch $term -Quiet) {
            throw "Forbidden production source term '$term' in $($source.Name)"
        }
    }
}

if (-not (Test-Path -LiteralPath $assembly)) { throw "Missing production assembly: $assembly" }
$bytes = [System.IO.File]::ReadAllBytes($assembly)
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)
$unicode = [System.Text.Encoding]::Unicode.GetString($bytes)
foreach ($term in $forbidden) {
    if ($ascii.Contains($term, [System.StringComparison]::Ordinal) -or $unicode.Contains($term, [System.StringComparison]::Ordinal)) {
        throw "Forbidden production binary term '$term'"
    }
}

$patchText = ($productionSources | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
$required = @('InitializeGraphicsPreferences', 'DoCloudSync', 'NMainMenu')
foreach ($term in $required) {
    if (-not $patchText.Contains($term, [System.StringComparison]::Ordinal)) { throw "Missing required observation surface '$term'" }
}

Write-Output "PASS source forbidden-surface audit ($($productionSources.Count) production source files)"
Write-Output "PASS binary forbidden-surface audit ($([System.IO.Path]::GetFileName($assembly)))"
Write-Output "PASS required observation surface audit"
