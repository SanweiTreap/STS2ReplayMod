$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$dotnet = 'C:\Users\sanwe\.dotnet\dotnet.exe'
$evidence = Join-Path $repo 'evidence\spike-inv008a'
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$game = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
$userRoot = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122'
$log = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs\godot.log'

function Exact($path, $length, $hash, $label) {
    $item = Get-Item -LiteralPath $path
    $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($item.Length -ne $length -or $actualHash -ne $hash) { throw "BLOCKED_IDENTITY: $label" }
    "PASS $label length=$($item.Length) sha256=$actualHash"
}
function Absent($path, $label) {
    if (Test-Path -LiteralPath $path) { throw "BLOCKED_PRIOR_ATTEMPT_UNRESOLVED: $label" }
    "PASS absent $label"
}
function Tree($path) {
    $rows = [Collections.Generic.List[string]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $path -Recurse -File | Sort-Object FullName) {
        $relative = [IO.Path]::GetRelativePath($path, $file.FullName).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        $rows.Add("$relative`t$($file.Length)`t$hash`n")
    }
    [pscustomobject]@{ Count = $rows.Count; Hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([string]::Concat($rows)))) }
}
function ExactTree($path, $count, $hash, $label) {
    $actual = Tree $path
    if ($actual.Count -ne $count -or $actual.Hash -ne $hash) { throw "BLOCKED_IDENTITY: $label tree" }
    "PASS $label untouched files=$($actual.Count) aggregateSha256=$($actual.Hash)"
}
function OldEvidenceSnapshot {
    $result = @{}
    $r6Recovery = Join-Path $evidence 'restricted-recovery\r6-'
    foreach ($file in Get-ChildItem -LiteralPath $evidence -Recurse -File | Where-Object {
        $_.Name -ne 'README.md' -and $_.Name -notlike 'r6-*' -and
        -not $_.FullName.StartsWith($r6Recovery, [StringComparison]::OrdinalIgnoreCase)
    }) { $result[[IO.Path]::GetRelativePath($evidence, $file.FullName)] = "$($file.Length):$((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash)" }
    $result
}
function SameMap($left, $right, $label) {
    if ($left.Count -ne $right.Count -or @($left.Keys | Where-Object { -not $right.ContainsKey($_) -or $right[$_] -ne $left[$_] }).Count) { throw "BLOCKED_IDENTITY: $label" }
    "PASS $label files=$($left.Count)"
}

if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_PREFLIGHT: game running' }
$oldEvidence = OldEvidenceSnapshot
Exact (Join-Path $game 'data_sts2_windows_x86_64\sts2.dll') 9757184 '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9' 'game DLL'
$release = Get-Content -Raw -LiteralPath (Join-Path $game 'release_info.json') | ConvertFrom-Json
if ($release.version -ne 'v0.111.0' -or $release.commit -ne '41cef1ea' -or $release.branch -ne 'v0.111.0') { throw 'BLOCKED_IDENTITY: release' }
'PASS release v0.111.0 / 41cef1ea'
Exact (Join-Path $evidence 'setup-owner-fault-r5.json') 25790 '9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9' 'fixed R5 artifact'
ExactTree (Join-Path $evidence 'restricted-recovery\r5-setup-e41635f71d044ca29e8105ae8a007522') 12 'DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C' 'fixed R5 recovery'
Exact (Join-Path $evidence 'project-lead-r5-menu-observer-review.md') 2510 '8AD3CD6B66DF1A1DD1A0D91B1002E383520A4D326CDCC13B86F81824201FE415' 'Project Lead R5 review'
Exact (Join-Path $root 'artifacts-r5\SPIKEINV008A.dll') 275456 '20C5BC84E378E507013A51887629779BC31966A23CEE637C2E0DD2D0DF1E7D3B' 'immutable R5 workspace DLL'
Exact (Join-Path $root 'artifacts-r5\SPIKEINV008A.json') 353 '385E1D6D633757D93A326B2846950EA06D1F505AC1D0A520069B6D58D0E796FD' 'immutable R5 workspace manifest'
Exact (Join-Path $mods 'SPIKEINV008A.dll') 275456 '20C5BC84E378E507013A51887629779BC31966A23CEE637C2E0DD2D0DF1E7D3B' 'installed R5 DLL'
Exact (Join-Path $mods 'SPIKEINV008A.json') 353 '385E1D6D633757D93A326B2846950EA06D1F505AC1D0A520069B6D58D0E796FD' 'installed R5 manifest'
Exact (Join-Path $userRoot 'settings.save') 5238 '2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F' 'fixed settings'
Exact $log 183524 '6C2794022719702B3CCF76DDFCBDFE8B2F2942D225AE0306AEE28DA9875BC848' 'complete R5 process log'
$logText = Get-Content -Raw -LiteralPath $log
foreach ($disabled in 'SPIKEINV004B','SPIKEINV005A','SPIKEINV006A','SPIKEINV006B','SPIKEINV007A') {
    if (-not $logText.Contains("Skipping loading mod $disabled, it is set to disabled in settings", [StringComparison]::Ordinal)) { throw "BLOCKED_PREFLIGHT: $disabled disabled state" }
}
if (-not $logText.Contains('Loaded 1 mods (16 total)', [StringComparison]::Ordinal) -or -not $logText.Contains('(SPIKEINV008A)', [StringComparison]::Ordinal) -or -not $logText.Contains('NGame.Quit called', [StringComparison]::Ordinal)) { throw 'BLOCKED_PREFLIGHT: complete exactly-008A loaded log' }
'PASS latest complete process loaded exactly 008A; all installed retired probes disabled'
'LIVE PROJECT PROBE INVENTORY:'
foreach ($file in Get-ChildItem -LiteralPath $mods -File | Where-Object Name -Match '^SPIKEINV\d+[A-Z]?\.(dll|json)$' | Sort-Object Name) {
    "$($file.Name) length=$($file.Length) sha256=$((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash)"
}
foreach ($name in 'setup-owner-fault-r6.json','restart-after-setup-r6.json','post-launch-owner-fault-r6.json','restart-after-post-launch-r6.json','invalid-phase-r6.json') {
    Absent (Join-Path $evidence $name) $name
    Absent (Join-Path $evidence ($name + '.tmp')) ($name + '.tmp')
}
if (@(Get-ChildItem -LiteralPath $evidence -File -Filter 'precheck-modset-r6-*.json').Count) { throw 'BLOCKED_PREFLIGHT: R6 precheck exists' }
if (@(Get-ChildItem -LiteralPath (Join-Path $evidence 'restricted-recovery') -Directory | Where-Object Name -Match '^r6-').Count) { throw 'BLOCKED_PREFLIGHT: R6 recovery exists' }
ExactTree (Join-Path $repo 'experiments\SPIKE-INV003A') 78 '31402E2455182279F56E544B78FA8E58BCEE39B661FEB4F8FE55FD498BBEA619' 'SPIKE-INV003A tree'
ExactTree (Join-Path $repo 'experiments\SPIKE-INV007A') 137 'AB18646445A2B17B15520553279328F6F071F0FB34B359C252DF6FC121C515A6' 'SPIKE-INV007A tree'
& $dotnet run --project (Join-Path $root 'tests\SPIKEINV008A.Tests.csproj') -c Release
if ($LASTEXITCODE) { throw 'BLOCKED_INSTRUMENTATION: tests' }
& $dotnet build (Join-Path $root 'SPIKEINV008A.csproj') -c Release
if ($LASTEXITCODE) { throw 'BLOCKED_INSTRUMENTATION: build' }
& (Join-Path $root 'Invoke-StaticAudit.ps1') -Configuration Release
& $dotnet run --project (Join-Path $root 'tests\PatchSmoke\PatchSmoke.csproj') -c Release -- (Join-Path $root 'artifacts-r6\SPIKEINV008A.dll')
if ($LASTEXITCODE) { throw 'BLOCKED_INSTRUMENTATION: smoke' }
foreach ($path in (Join-Path $root 'artifacts-r6\SPIKEINV008A.dll'),(Join-Path $root 'artifacts-r6\SPIKEINV008A.json')) {
    $item = Get-Item -LiteralPath $path
    "PASS R6 artifact $($item.Name) length=$($item.Length) sha256=$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)"
}
SameMap $oldEvidence (OldEvidenceSnapshot) 'all pre-R6 evidence files unchanged'
ExactTree (Join-Path $evidence 'restricted-recovery\r5-setup-e41635f71d044ca29e8105ae8a007522') 12 'DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C' 'post-build immutable R5 recovery'
Exact (Join-Path $evidence 'setup-owner-fault-r5.json') 25790 '9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9' 'post-build immutable R5 artifact'
'PASS SPIKE_INV008A_R6_PHASE0_OFFLINE_PREFLIGHT'
