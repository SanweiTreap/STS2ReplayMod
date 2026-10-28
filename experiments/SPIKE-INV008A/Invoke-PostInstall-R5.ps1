$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$evidence = Join-Path $repo 'evidence\spike-inv008a'
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$settings = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122\settings.save'
function Exact($path, $length, $hash, $label) {
    $item = Get-Item -LiteralPath $path; $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($item.Length -ne $length -or $actual -ne $hash) { throw "BLOCKED_POST_INSTALL: $label" }
    "PASS $label length=$($item.Length) sha256=$actual"
}
function Absent($path, $label) { if (Test-Path -LiteralPath $path) { throw "BLOCKED_POST_INSTALL: $label" }; "PASS absent $label" }
function Tree($path) {
    $rows = [Collections.Generic.List[string]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $path -Recurse -File | Sort-Object FullName) {
        $relative = [IO.Path]::GetRelativePath($path, $file.FullName).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        $rows.Add("$relative`t$($file.Length)`t$hash`n")
    }
    [pscustomobject]@{ Count=$rows.Count; Hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([string]::Concat($rows)))) }
}
function ExactTree($path, $count, $hash, $label) { $x=Tree $path; if($x.Count-ne$count-or$x.Hash-ne$hash){throw "BLOCKED_POST_INSTALL: $label"}; "PASS $label files=$($x.Count) aggregateSha256=$($x.Hash)" }
if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_POST_INSTALL: game running' }
Exact (Join-Path $mods 'SPIKEINV008A.dll') 275456 '20C5BC84E378E507013A51887629779BC31966A23CEE637C2E0DD2D0DF1E7D3B' 'installed R5 DLL'
Exact (Join-Path $mods 'SPIKEINV008A.json') 353 '385E1D6D633757D93A326B2846950EA06D1F505AC1D0A520069B6D58D0E796FD' 'installed R5 manifest'
Exact (Join-Path $root 'artifacts-r5\SPIKEINV008A.dll') 275456 '20C5BC84E378E507013A51887629779BC31966A23CEE637C2E0DD2D0DF1E7D3B' 'workspace R5 DLL'
Exact (Join-Path $root 'artifacts-r5\SPIKEINV008A.json') 353 '385E1D6D633757D93A326B2846950EA06D1F505AC1D0A520069B6D58D0E796FD' 'workspace R5 manifest'
Exact (Join-Path $root 'artifacts-r4\SPIKEINV008A.dll') 245248 '422FF9C84722394E3E99A708DA3EE31140540AE067DCD22318BB7330188BFB16' 'immutable R4 workspace DLL'
Exact (Join-Path $evidence 'setup-owner-fault-r4.json') 23241 '40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040' 'immutable R4 artifact'
ExactTree (Join-Path $evidence 'restricted-recovery\r4-setup-df7ceb3e2c724e73a6bc0c368d69aec5') 12 'DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C' 'immutable R4 recovery'
Exact (Join-Path $repo 'evidence\inv-009\static-lobby-query-output.txt') 253870 '488814C65B9CD8E56F888CFCFE50AD7BEAA890E0396F084BED3916CFBDF772D7' 'immutable INV-009 IL output'
Exact $settings 5238 '2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F' 'fixed settings'
ExactTree (Join-Path $repo 'experiments\SPIKE-INV003A') 78 '31402E2455182279F56E544B78FA8E58BCEE39B661FEB4F8FE55FD498BBEA619' 'SPIKE-INV003A tree'
ExactTree (Join-Path $repo 'experiments\SPIKE-INV007A') 137 'AB18646445A2B17B15520553279328F6F071F0FB34B359C252DF6FC121C515A6' 'SPIKE-INV007A tree'
foreach ($name in 'setup-owner-fault-r5.json','restart-after-setup-r5.json','post-launch-owner-fault-r5.json','restart-after-post-launch-r5.json','invalid-phase-r5.json') { Absent (Join-Path $evidence $name) $name; Absent (Join-Path $evidence ($name+'.tmp')) ($name+'.tmp') }
if (@(Get-ChildItem -LiteralPath $evidence -File -Filter 'precheck-modset-r5-*.json').Count) { throw 'BLOCKED_POST_INSTALL: R5 precheck exists' }
if (@(Get-ChildItem -LiteralPath (Join-Path $evidence 'restricted-recovery') -Directory | Where-Object Name -Match '^r5-').Count) { throw 'BLOCKED_POST_INSTALL: R5 recovery exists' }
'PASS stopped-game R5 post-install integrity; SETUP_OWNER_FAULT_R5 is the sole unlocked phase'
