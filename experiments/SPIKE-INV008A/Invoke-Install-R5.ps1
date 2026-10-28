$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$evidence = Join-Path $repo 'evidence\spike-inv008a'
$settings = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122\settings.save'
$log = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs\godot.log'
$srcDll = Join-Path $root 'artifacts-r5\SPIKEINV008A.dll'
$srcManifest = Join-Path $root 'artifacts-r5\SPIKEINV008A.json'
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
Exact $dstDll 245248 '422FF9C84722394E3E99A708DA3EE31140540AE067DCD22318BB7330188BFB16' 'installed R4 DLL'
Exact $dstManifest 353 'C5E73FEAAC16C49BFF2C14625D8D7340A4D17D683F4D2B4A5F21D31DDE259D26' 'installed R4 manifest'
Exact $srcDll 275456 '20C5BC84E378E507013A51887629779BC31966A23CEE637C2E0DD2D0DF1E7D3B' 'R5 source DLL'
Exact $srcManifest 353 '385E1D6D633757D93A326B2846950EA06D1F505AC1D0A520069B6D58D0E796FD' 'R5 source manifest'
Exact $settings 5238 '2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F' 'fixed settings'
Exact $log 183370 'F5D4A8C19AEEDC0410B1CC5E4800836FB3A043D090CA9801D837EABA61A4425C' 'complete exactly-008A R4 log'
Exact (Join-Path $evidence 'setup-owner-fault-r4.json') 23241 '40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040' 'immutable R4 artifact'
$manifest = Get-Content -Raw -LiteralPath $srcManifest | ConvertFrom-Json
if ($manifest.id -ne 'SPIKEINV008A' -or $manifest.version -ne '0.5.0') { throw 'BLOCKED_INSTALL: R5 manifest contract' }
$other = OtherProbeSnapshot
try {
    Copy-Item -LiteralPath $srcDll -Destination $dstDll
    Copy-Item -LiteralPath $srcManifest -Destination $dstManifest
} catch {
    Copy-Item -LiteralPath (Join-Path $root 'artifacts-r4\SPIKEINV008A.dll') -Destination $dstDll
    Copy-Item -LiteralPath (Join-Path $root 'artifacts-r4\SPIKEINV008A.json') -Destination $dstManifest
    throw
}
Exact $dstDll 275456 '20C5BC84E378E507013A51887629779BC31966A23CEE637C2E0DD2D0DF1E7D3B' 'installed R5 DLL'
Exact $dstManifest 353 '385E1D6D633757D93A326B2846950EA06D1F505AC1D0A520069B6D58D0E796FD' 'installed R5 manifest'
SameMap $other (OtherProbeSnapshot)
'PASS only the exact 008A pair was replaced; enabled configuration and every other probe were untouched'
