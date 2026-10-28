param(
    [string]$ProbeDataDirectory = ""
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$destination = Join-Path $repositoryRoot 'evidence\spike-001a\runtime'

if ([string]::IsNullOrWhiteSpace($ProbeDataDirectory)) {
    $stsDataRoot = Join-Path $env:APPDATA 'SlayTheSpire2'
    if (Test-Path -LiteralPath $stsDataRoot) {
        $ProbeDataDirectory = Get-ChildItem -LiteralPath $stsDataRoot -Directory -Recurse -Filter 'SPIKE-001A' |
            Select-Object -First 1 -ExpandProperty FullName
    }
}

if ([string]::IsNullOrWhiteSpace($ProbeDataDirectory) -or -not (Test-Path -LiteralPath $ProbeDataDirectory)) {
    throw 'Probe data directory not found. Pass -ProbeDataDirectory using environment.json.outputDirectory.'
}

$resolvedSource = (Resolve-Path -LiteralPath $ProbeDataDirectory).Path
if (-not $resolvedSource.EndsWith([IO.Path]::DirectorySeparatorChar + 'SPIKE-001A', [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing unexpected source directory: $resolvedSource"
}

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Get-ChildItem -LiteralPath $resolvedSource -Force | Copy-Item -Destination $destination -Recurse -Force
Write-Output "Collected SPIKE-001A evidence from '$resolvedSource' to '$destination'."
