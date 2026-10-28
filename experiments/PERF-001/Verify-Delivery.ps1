param(
    [string] $Workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')),
    [switch] $RecheckProxy
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Workspace = [IO.Path]::GetFullPath($Workspace)
$indexPath = Join-Path $Workspace 'evidence/perf-001/phase0-delivery-r1.json'
$index = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
if ($index.Schema -ne 1 -or $index.Conclusion -cne 'READY_PERF_MEASUREMENT_CONTRACT' -or $index.FileCount -ne @($index.Files).Count) { throw 'Index schema/count/conclusion mismatch' }
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$publicCount = 0
foreach ($entry in $index.Files) {
    $relative = [string]$entry.Path
    if ([IO.Path]::IsPathRooted($relative) -or $relative.Contains('\') -or $relative -match '(^|/)\.\.?(/|$)' -or !$seen.Add($relative)) { throw 'Noncanonical/duplicate path' }
    $ownedSource = $relative -cmatch '^experiments/PERF-001/[^/]+\.(cs|csproj|ps1|Config)$'
    $ownedEvidence = $relative.StartsWith('evidence/perf-001/', [StringComparison]::Ordinal) -and $relative -cne 'evidence/perf-001/phase0-delivery-r1.json'
    $report = $relative -ceq 'project-management/investigation-reports/PERF-001-Phase0-report.md'
    $public = $relative -cmatch '^evidence/det-001/runtime/records-af99c3b9c7bf411882d580c2f3565408/A\.point-[0-4]\.semantic\.json$'
    if (!$ownedSource -and !$ownedEvidence -and !$report -and !$public) { throw 'Index path outside declared delivery scope' }
    if ($public) { $publicCount++ }
    $full = [IO.Path]::GetFullPath((Join-Path $Workspace $relative))
    if (![IO.Path]::GetRelativePath($Workspace, $full).Replace('\', '/').Equals($relative, [StringComparison]::Ordinal)) { throw 'Path normalization changed entry' }
    for ($ancestor = $full; ![string]::IsNullOrEmpty($ancestor); $ancestor = [IO.Path]::GetDirectoryName($ancestor)) {
        if (Test-Path -LiteralPath $ancestor) {
            if (((Get-Item -LiteralPath $ancestor).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Reparse path excluded' }
        }
    }
    $file = Get-Item -LiteralPath $full
    if ($file.PSIsContainer -or $file.Length -ne $entry.Bytes -or (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash -cne $entry.SHA256) { throw "Delivery hash mismatch: $relative" }
}
if ($publicCount -ne 5) { throw 'Expected only the five direct public DTO inputs' }
if ($RecheckProxy) {
    $build = Get-Content -LiteralPath (Join-Path $Workspace 'evidence/perf-001/build-functional-receipt-r1.json') -Raw | ConvertFrom-Json
    $binaryRelative = 'experiments/PERF-001/bin/Release/net9.0/Perf001.Offline.dll'
    if ($build.ExecutedBinary.Path -cne $binaryRelative -or (Get-FileHash -LiteralPath (Join-Path $Workspace $binaryRelative) -Algorithm SHA256).Hash -cne $build.ExecutedBinary.SHA256) { throw 'Original verified binary absent/different; index-only verification remains available' }
    & (Join-Path $PSScriptRoot 'Invoke-Offline.ps1') -Action Verify -Workspace $Workspace -OutputName 'proxy-r1-20261026'
}
[ordered]@{Result='PASS_READ_ONLY_DELIVERY_INDEX';Files=$seen.Count;DirectPublicInputs=$publicCount;Writes=0;Builds=0;TimedBenchmarkRuns=0;GameRuntimeAttempts=0;ProxyRechecked=[bool]$RecheckProxy} | ConvertTo-Json -Compress
