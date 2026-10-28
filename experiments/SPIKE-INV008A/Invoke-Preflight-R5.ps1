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
    foreach ($file in Get-ChildItem -LiteralPath $evidence -Recurse -File | Where-Object {
        $_.Name -ne 'README.md' -and $_.Name -notlike 'r5-*' -and
        -not $_.FullName.StartsWith((Join-Path $evidence 'restricted-recovery'), [StringComparison]::OrdinalIgnoreCase)
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
Exact (Join-Path $evidence 'setup-owner-fault-r4.json') 23241 '40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040' 'fixed R4 artifact'
ExactTree (Join-Path $evidence 'restricted-recovery\r4-setup-df7ceb3e2c724e73a6bc0c368d69aec5') 12 'DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C' 'fixed R4 recovery'
Exact (Join-Path $repo 'evidence\inv-009\static-lobby-query-output.txt') 253870 '488814C65B9CD8E56F888CFCFE50AD7BEAA890E0396F084BED3916CFBDF772D7' 'INV-009 IL output'
Exact (Join-Path $repo 'evidence\inv-009\r4-bounded-summary.txt') 2063 '2B50415302BBE5D38874C6AA5CA29EAF2012FA7D4DE923AA842A8AD26762D9D0' 'INV-009 bounded summary'
Exact (Join-Path $root 'artifacts-r4\SPIKEINV008A.dll') 245248 '422FF9C84722394E3E99A708DA3EE31140540AE067DCD22318BB7330188BFB16' 'immutable R4 workspace DLL'
Exact (Join-Path $root 'artifacts-r4\SPIKEINV008A.json') 353 'C5E73FEAAC16C49BFF2C14625D8D7340A4D17D683F4D2B4A5F21D31DDE259D26' 'immutable R4 workspace manifest'
Exact (Join-Path $mods 'SPIKEINV008A.dll') 245248 '422FF9C84722394E3E99A708DA3EE31140540AE067DCD22318BB7330188BFB16' 'installed R4 DLL'
Exact (Join-Path $mods 'SPIKEINV008A.json') 353 'C5E73FEAAC16C49BFF2C14625D8D7340A4D17D683F4D2B4A5F21D31DDE259D26' 'installed R4 manifest'
Exact (Join-Path $userRoot 'settings.save') 5238 '2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F' 'fixed settings'
Exact $log 183370 'F5D4A8C19AEEDC0410B1CC5E4800836FB3A043D090CA9801D837EABA61A4425C' 'complete R4 process log'
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
foreach ($name in 'setup-owner-fault-r5.json','restart-after-setup-r5.json','post-launch-owner-fault-r5.json','restart-after-post-launch-r5.json','invalid-phase-r5.json') {
    Absent (Join-Path $evidence $name) $name
    Absent (Join-Path $evidence ($name + '.tmp')) ($name + '.tmp')
}
if (@(Get-ChildItem -LiteralPath $evidence -File -Filter 'precheck-modset-r5-*.json').Count) { throw 'BLOCKED_PREFLIGHT: R5 precheck exists' }
if (@(Get-ChildItem -LiteralPath (Join-Path $evidence 'restricted-recovery') -Directory | Where-Object Name -Match '^r5-').Count) { throw 'BLOCKED_PREFLIGHT: R5 recovery exists' }
ExactTree (Join-Path $repo 'experiments\SPIKE-INV003A') 78 '31402E2455182279F56E544B78FA8E58BCEE39B661FEB4F8FE55FD498BBEA619' 'SPIKE-INV003A tree'
ExactTree (Join-Path $repo 'experiments\SPIKE-INV007A') 137 'AB18646445A2B17B15520553279328F6F071F0FB34B359C252DF6FC121C515A6' 'SPIKE-INV007A tree'
& $dotnet run --project (Join-Path $root 'tests\SPIKEINV008A.Tests.csproj') -c Release
if ($LASTEXITCODE) { throw 'BLOCKED_INSTRUMENTATION: tests' }
& $dotnet build (Join-Path $root 'SPIKEINV008A.csproj') -c Release
if ($LASTEXITCODE) { throw 'BLOCKED_INSTRUMENTATION: build' }
& (Join-Path $root 'Invoke-StaticAudit.ps1') -Configuration Release
& $dotnet run --project (Join-Path $root 'tests\PatchSmoke\PatchSmoke.csproj') -c Release -- (Join-Path $root 'artifacts-r5\SPIKEINV008A.dll')
if ($LASTEXITCODE) { throw 'BLOCKED_INSTRUMENTATION: smoke' }
foreach ($path in (Join-Path $root 'artifacts-r5\SPIKEINV008A.dll'),(Join-Path $root 'artifacts-r5\SPIKEINV008A.json')) {
    $item = Get-Item -LiteralPath $path
    "PASS R5 artifact $($item.Name) length=$($item.Length) sha256=$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)"
}
SameMap $oldEvidence (OldEvidenceSnapshot) 'all pre-R5 evidence files unchanged'
ExactTree (Join-Path $evidence 'restricted-recovery\r4-setup-df7ceb3e2c724e73a6bc0c368d69aec5') 12 'DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C' 'post-build immutable R4 recovery'
Exact (Join-Path $evidence 'setup-owner-fault-r4.json') 23241 '40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040' 'post-build immutable R4 artifact'
'PASS SPIKE_INV008A_R5_PHASE0_OFFLINE_PREFLIGHT'
