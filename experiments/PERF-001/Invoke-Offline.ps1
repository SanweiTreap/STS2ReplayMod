param(
    [Parameter(Mandatory)][ValidateSet('Build', 'Test', 'Bench', 'Verify')][string] $Action,
    [string] $Workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')),
    [string] $OutputName,
    [switch] $TimingWindowConfirmed,
    [string] $DotnetPath = 'C:/Users/sanwe/.dotnet/dotnet.exe'
)
$ErrorActionPreference = 'Stop'
$Workspace = [IO.Path]::GetFullPath($Workspace)
$project = Join-Path $Workspace 'experiments/PERF-001/Perf001.csproj'
if ($Action -eq 'Build') {
    & $DotnetPath build $project -c Release --configfile (Join-Path $PSScriptRoot 'NuGet.Config') --nologo
} else {
    if ($OutputName -notmatch '^[a-z0-9-]{1,64}$') { throw 'A short package-local OutputName is required' }
    if ($Action -eq 'Bench' -and !$TimingWindowConfirmed) {
        throw 'Bench requires human timing-window confirmation; do not infer this from game processes'
    }
    $dll = Join-Path $Workspace 'experiments/PERF-001/bin/Release/net9.0/Perf001.Offline.dll'
    if (!(Test-Path -LiteralPath $dll -PathType Leaf)) { throw 'Run Build first' }
    $kind = if ($Action -eq 'Test') { 'validation' } else { 'runs' }
    $output = Join-Path $Workspace "evidence/perf-001/$kind/$OutputName"
    $arguments = @($dll, $Action.ToLowerInvariant(), $Workspace, $output)
    if ($Action -eq 'Bench') { $arguments += '--timing-window-confirmed' }
    & $DotnetPath @arguments
}
if ($LASTEXITCODE -ne 0) { throw "Offline command failed with exit code $LASTEXITCODE; retain evidence" }
