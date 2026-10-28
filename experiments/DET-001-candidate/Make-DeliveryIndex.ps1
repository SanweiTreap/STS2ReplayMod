param([Parameter(Mandatory=$true)][string]$ValidationDirectory)
$ErrorActionPreference='Stop';$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$root=Join-Path $workspace 'evidence/det-001/candidate-r1';$validation=[IO.Path]::GetFullPath($ValidationDirectory)
if(-not $validation.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'VALIDATION_SCOPE'}
$receipt=Get-Content -LiteralPath (Join-Path $validation 'receipt.json') -Raw|ConvertFrom-Json
if($receipt.Status -cne 'READY_OFFLINE_CANDIDATE_FOR_REVIEW' -or $receipt.Runtime -cne 'HOLD' -or $receipt.Passed -ne 68){throw 'VALIDATION_GATE'}
function Entry([string]$path){$f=Get-Item -LiteralPath $path;[ordered]@{Path=[IO.Path]::GetRelativePath($workspace,$f.FullName).Replace('\','/');Bytes=$f.Length;SHA256=(Get-FileHash -LiteralPath $f.FullName).Hash}}
$files=@(Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -File|Where-Object {$_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in @('.cs','.csproj','.ps1','.txt','.json','.dll')}|ForEach-Object FullName)
$files+=@(Get-ChildItem -LiteralPath $validation -Recurse -File|ForEach-Object FullName)
$files+=@('README.md','neow-eligibility.md','first-reshuffle-contract.md','manual-flow-draft.md','source-origin.json')|ForEach-Object {Join-Path $root $_}
$files+=Join-Path $workspace 'project-management/investigation-reports/DET-001-candidate-report.md'
$entries=@($files|Sort-Object -Unique|ForEach-Object {Entry $_})
$index=[ordered]@{Schema=1;Date='2026-10-23';Status='READY_OFFLINE_CANDIDATE_FOR_PROJECT_LEAD';ModId='DET001';Version='0.1.0';Revision='candidate-r1';Owner='STS2ReplayMod.DET001.FirstReshuffle.candidate-r1';PrivateNamespace='user://DET001/candidate-r1/';PrivateSchema=1;Runtime='HOLD';FormalAttempts=0;RealReshuffleObserved=$false;Passed=68;Candidate=Entry (Join-Path $PSScriptRoot 'artifacts-candidate-r1/DET001.dll');Manifest=Entry (Join-Path $PSScriptRoot 'artifacts-candidate-r1/DET001.json');Validation=[IO.Path]::GetRelativePath($workspace,$validation).Replace('\','/');Report='project-management/investigation-reports/DET-001-candidate-report.md';LegacyPhase0IndexSHA256=$receipt.LegacyPhase0IndexSHA256;Legacy001BIndexSHA256=$receipt.Legacy001BIndexSHA256;Files=$entries}
$dest=Join-Path $root 'delivery-2026-10-23.json';$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($index|ConvertTo-Json -Depth 9)+[Environment]::NewLine);$s=[IO.File]::Open($dest,[IO.FileMode]::CreateNew);try{$s.Write($bytes)}finally{$s.Dispose()}
"PASS indexed $($entries.Count) candidate files; SHA256=$((Get-FileHash -LiteralPath $dest).Hash)"
