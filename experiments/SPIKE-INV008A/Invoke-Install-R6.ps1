$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$evidence = Join-Path $repo 'evidence\spike-inv008a'
$settings = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122\settings.save'
$log = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs\godot.log'
$srcDll = Join-Path $root 'artifacts-r6\SPIKEINV008A.dll'
$srcManifest = Join-Path $root 'artifacts-r6\SPIKEINV008A.json'
$dstDll = Join-Path $mods 'SPIKEINV008A.dll'
$dstManifest = Join-Path $mods 'SPIKEINV008A.json'

function Exact($path, $length, $hash, $label) {
    $item = Get-Item -LiteralPath $path; $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($item.Length -ne $length -or $actual -ne $hash) { throw "BLOCKED_INSTALL: $label" }
    "PASS $label length=$($item.Length) sha256=$actual"
}
function OtherProbeSnapshot {
    $result = @{}
    foreach ($file in Get-ChildItem -LiteralPath $mods -File | Where-Object { $_.Name -match '^SPIKEINV\d+[A-Z]?\.(dll|json)$' -and $_.Name -notlike 'SPIKEINV008A.*' }) {
        $result[$file.Name] = "$($file.Length):$((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash)"
    }
    $result
}
function SameMap($left, $right) {
    if ($left.Count -ne $right.Count -or @($left.Keys | Where-Object { -not $right.ContainsKey($_) -or $right[$_] -ne $left[$_] }).Count) { throw 'BLOCKED_INSTALL: another project probe changed' }
}

if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_INSTALL: game running' }
Exact $dstDll 275456 '20C5BC84E378E507013A51887629779BC31966A23CEE637C2E0DD2D0DF1E7D3B' 'installed R5 DLL'
Exact $dstManifest 353 '385E1D6D633757D93A326B2846950EA06D1F505AC1D0A520069B6D58D0E796FD' 'installed R5 manifest'
Exact $srcDll 288768 '9058DD9B3CE2503F06A550642C961B936E5138976240AC820034E0C7E525C83D' 'R6 source DLL'
Exact $srcManifest 353 '5442294037F63781E57BC1803CFE4258BD51AE06138B097FDCB224B40DD8A17D' 'R6 source manifest'
Exact $settings 5238 '2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F' 'fixed settings'
Exact $log 183524 '6C2794022719702B3CCF76DDFCBDFE8B2F2942D225AE0306AEE28DA9875BC848' 'complete exactly-008A R5 log'
Exact (Join-Path $evidence 'setup-owner-fault-r5.json') 25790 '9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9' 'immutable R5 artifact'
$logText = Get-Content -Raw -LiteralPath $log
if (-not $logText.Contains('Skipping loading mod SPIKEINV007A, it is set to disabled in settings', [StringComparison]::Ordinal) -or
    -not $logText.Contains('Loaded 1 mods (16 total)', [StringComparison]::Ordinal) -or
    -not $logText.Contains('(SPIKEINV008A)', [StringComparison]::Ordinal) -or
    -not $logText.Contains('NGame.Quit called', [StringComparison]::Ordinal)) { throw 'BLOCKED_INSTALL: enabled/load-state log contract' }
$manifest = Get-Content -Raw -LiteralPath $srcManifest | ConvertFrom-Json
if ($manifest.id -ne 'SPIKEINV008A' -or $manifest.version -ne '0.6.0') { throw 'BLOCKED_INSTALL: R6 manifest contract' }
$other = OtherProbeSnapshot
try {
    Copy-Item -LiteralPath $srcDll -Destination $dstDll
    Copy-Item -LiteralPath $srcManifest -Destination $dstManifest
} catch {
    Copy-Item -LiteralPath (Join-Path $root 'artifacts-r5\SPIKEINV008A.dll') -Destination $dstDll
    Copy-Item -LiteralPath (Join-Path $root 'artifacts-r5\SPIKEINV008A.json') -Destination $dstManifest
    throw
}
Exact $dstDll 288768 '9058DD9B3CE2503F06A550642C961B936E5138976240AC820034E0C7E525C83D' 'installed R6 DLL'
Exact $dstManifest 353 '5442294037F63781E57BC1803CFE4258BD51AE06138B097FDCB224B40DD8A17D' 'installed R6 manifest'
SameMap $other (OtherProbeSnapshot)
'PASS only the exact 008A pair was replaced; enabled configuration and every other probe were untouched'
