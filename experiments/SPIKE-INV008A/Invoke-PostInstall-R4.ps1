$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent (Split-Path -Parent $root)
$evidence = Join-Path $repo 'evidence\spike-inv008a'
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$userRoot = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\steam\76561199141262122'

function Exact($path, $length, $hash, $label) {
    $item = Get-Item -LiteralPath $path
    $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($item.Length -ne $length -or $actualHash -ne $hash) { throw "BLOCKED_POST_INSTALL: $label" }
    "PASS $label length=$($item.Length) sha256=$actualHash"
}

function Absent($path, $label) {
    if (Test-Path -LiteralPath $path) { throw "BLOCKED_POST_INSTALL: unexpected $label" }
    "PASS absent $label"
}

function Tree($path) {
    $rows = [Collections.Generic.List[string]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $path -Recurse -File | Sort-Object FullName) {
        $relative = [IO.Path]::GetRelativePath($path, $file.FullName).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        $rows.Add("$relative`t$($file.Length)`t$hash`n")
    }
    [pscustomobject]@{
        Count = $rows.Count
        Hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([string]::Concat($rows))))
    }
}

function ExactTree($path, $count, $hash, $label) {
    $actual = Tree $path
    if ($actual.Count -ne $count -or $actual.Hash -ne $hash) { throw "BLOCKED_POST_INSTALL: $label tree" }
    "PASS $label untouched files=$($actual.Count) aggregateSha256=$($actual.Hash)"
}

if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_POST_INSTALL: game running' }
Exact (Join-Path $mods 'SPIKEINV008A.dll') 245248 '422FF9C84722394E3E99A708DA3EE31140540AE067DCD22318BB7330188BFB16' 'installed R4 DLL'
Exact (Join-Path $mods 'SPIKEINV008A.json') 353 'C5E73FEAAC16C49BFF2C14625D8D7340A4D17D683F4D2B4A5F21D31DDE259D26' 'installed R4 manifest'
Exact (Join-Path $root 'artifacts-r4\SPIKEINV008A.dll') 245248 '422FF9C84722394E3E99A708DA3EE31140540AE067DCD22318BB7330188BFB16' 'workspace R4 DLL'
Exact (Join-Path $root 'artifacts-r4\SPIKEINV008A.json') 353 'C5E73FEAAC16C49BFF2C14625D8D7340A4D17D683F4D2B4A5F21D31DDE259D26' 'workspace R4 manifest'
Exact (Join-Path $root 'artifacts-r3\SPIKEINV008A.dll') 235008 '2FE763CC52805D1C9ED513D97B0AF8370EB4BF659E6CB8BF376F99747D047F01' 'immutable R3 workspace DLL'
Exact (Join-Path $root 'artifacts-r3\SPIKEINV008A.json') 353 '1B0ECD1E95639DA0B95EB84576C17D4D0CAB67D70D2E15496702533A028D22A3' 'immutable R3 workspace manifest'
Exact (Join-Path $evidence 'setup-owner-fault-r3.json') 3740 '5C8E49B98F7BC9A647033F779DC3CACCEBE3D840214E4D95995EBADB7C0B13F0' 'immutable R3 artifact'
Exact (Join-Path $userRoot 'settings.save') 5238 '2680514D9857AB079A72F550A7C50C4A9FF5F872B5E87E9262899EDE674EB11F' 'fixed settings'
ExactTree (Join-Path $repo 'experiments\SPIKE-INV003A') 78 '31402E2455182279F56E544B78FA8E58BCEE39B661FEB4F8FE55FD498BBEA619' 'SPIKE-INV003A tree'
ExactTree (Join-Path $repo 'experiments\SPIKE-INV007A') 137 'AB18646445A2B17B15520553279328F6F071F0FB34B359C252DF6FC121C515A6' 'SPIKE-INV007A tree'
foreach ($name in 'setup-owner-fault-r4.json','restart-after-setup-r4.json','postlaunch-owner-fault-r4.json','restart-after-postlaunch-r4.json','invalid-phase-r4.json') {
    Absent (Join-Path $evidence $name) $name
    Absent (Join-Path $evidence ($name + '.tmp')) ($name + '.tmp')
}
if (@(Get-ChildItem -LiteralPath $evidence -File -Filter 'precheck-modset-r4-*.json').Count) { throw 'BLOCKED_POST_INSTALL: R4 precheck exists' }
if (Test-Path -LiteralPath (Join-Path $evidence 'restricted-recovery')) {
    if (@(Get-ChildItem -LiteralPath (Join-Path $evidence 'restricted-recovery') -Directory | Where-Object Name -Match '^r4-').Count) { throw 'BLOCKED_POST_INSTALL: R4 recovery exists' }
}
'PASS stopped-game R4 post-install integrity; SETUP_OWNER_FAULT_R4 is the sole unlocked phase'
