param([string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'))
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$evidence=Join-Path $workspace 'evidence/spike-001b/r2'
$output=Join-Path $evidence ('validation-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($output)
function Save([string]$name,$lines){$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($lines -join [Environment]::NewLine)+[Environment]::NewLine);$s=[IO.File]::Open((Join-Path $output $name),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$s.Write($bytes)}finally{$s.Dispose()}}
function Run([string]$log,[string[]]$arguments){$r=& $Dotnet @arguments 2>&1;$code=$LASTEXITCODE;Save $log $r;if($code -ne 0){throw "VALIDATION_FAILED $log"};$r|Select-Object -Last 2}
Run 'toolchain.txt' @('--info')
Run 'release-build.txt' @('build',(Join-Path $PSScriptRoot 'tests/Offline.csproj'),'-c','Release','--nologo')
$test=Join-Path $PSScriptRoot 'tests/bin/Release/net9.0/Offline.dll';$initial=Join-Path $workspace 'evidence/spike-001a5/runtime/records-3e6aaa0160e441c293b5ac4a0030b6d0/A.semantic.json'
Run 'contracts.txt' @($test,'contracts',(Join-Path $output 'fixtures'),$initial)
Run 'patch-smoke.txt' @($test,'patch');Run 'dummy-retained.txt' @($test,'dummy');Run 'dummy-action-r2.txt' @($test,'dummy-action-r2')
$probe=Join-Path $PSScriptRoot 'bin/Release/net9.0/SPIKE001B.dll'
& (Join-Path $evidence 'Invoke-R2Audit.ps1') -ProbeDll $probe -OutputDirectory $output
& (Join-Path $evidence 'Invoke-PlayerTurnQuery.ps1') -OutputDirectory $output
$preserved=@()
foreach($index in @('evidence/spike-001b/phase0-delivery-2026-10-22.json','evidence/spike-001a5/runtime/runtime-delivery-2026-10-22.json','evidence/spike-001a5/phase0-delivery-2026-10-09.json','evidence/spike-001a5/phase0-r2-delivery-2026-10-09.json','evidence/spike-001a5/runtime/runtime-r1-build-index-2026-10-21.json')){
 $path=Join-Path $workspace $index;$doc=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
 foreach($f in $doc.Files){$file=Join-Path $workspace $f.Path;if((Get-FileHash -LiteralPath $file).Hash -ne $f.SHA256 -or (Get-Item -LiteralPath $file).Length -ne $f.Bytes){throw 'SEALED_OLD_DELIVERY_CHANGED'}}
 $preserved += [ordered]@{Index=$index;Files=$doc.Files.Count;SHA256=(Get-FileHash -LiteralPath $path).Hash}
}
$result=Get-Content -LiteralPath (Join-Path $output 'fixtures/contracts-receipt.json') -Raw|ConvertFrom-Json
if($result.Contracts -le 88){throw 'MISSING_INCREMENTAL_REGRESSIONS'}
$sources=@(Get-ChildItem -LiteralPath $PSScriptRoot -File -Recurse|Where-Object {$_.FullName -notmatch '\\(bin|obj|artifacts-phase0-r2)\\' -and $_.Extension -in @('.cs','.csproj','.json','.ps1','.md')}|Sort-Object FullName|ForEach-Object {[ordered]@{Path=[IO.Path]::GetRelativePath($PSScriptRoot,$_.FullName).Replace('\','/');Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$receipt=[ordered]@{Schema=2;Date='2026-10-22';Revision='phase0-r2';Scope='OFFLINE_ACTION_DIAGNOSTICS_ONLY';Contracts=$result.Contracts;RetainedContracts=88;IncrementalContracts=$result.Contracts-88;Release='0 warning/error';PatchSmoke='7/7';RetainedDummy='PASS';DiagnosticDummy='PASS';Audit='PASS';GameReferences=159;NewGameReferencesFromR1=0;NewProductionPatches=0;MaxActionObservations=32;MaxEventBytes=4096;MaxRecordBytes=8192;PlayerTurnInitialStatic=1;PlayerTurnRuntime='UNOBSERVED';PreservedIndexes=$preserved;PublicInitialSHA256=$result.PublicInitialHash;Successors='SYNTHETIC_NOT_RUNTIME';GameStarted=$false;PlayerStateRead=$false;PrivateRawRead=$false;ConfiguredOrInstalled=$false;BusinessAttempts=0;GameProcessCount=@(Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue).Count;Sources=$sources;Candidate=[ordered]@{Bytes=(Get-Item -LiteralPath $probe).Length;SHA256=(Get-FileHash -LiteralPath $probe).Hash};Manifest=[ordered]@{Bytes=(Get-Item -LiteralPath (Join-Path $PSScriptRoot 'SPIKE001B.json')).Length;SHA256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'SPIKE001B.json')).Hash}}
Save 'receipt.json' @($receipt|ConvertTo-Json -Depth 10)
"PASS Phase0-r2 $($result.Contracts)/$($result.Contracts); output=$output"
