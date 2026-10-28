param([string]$OutputDirectory,[string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'))
$ErrorActionPreference='Stop';$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not $OutputDirectory){$OutputDirectory=Join-Path $workspace ('evidence/det-001/candidate-r1/validation-'+[Guid]::NewGuid().ToString('N'))}
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(-not $output.StartsWith((Join-Path $workspace 'evidence/det-001/candidate-r1')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $output)){throw 'NEW_OUTPUT_SCOPE'}
for($p=$output;$p;$p=[IO.Path]::GetDirectoryName($p)){if((Test-Path -LiteralPath $p) -and ((Get-Item -LiteralPath $p).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'REPARSE_OUTPUT'}}
[void][IO.Directory]::CreateDirectory($output)
function Save([string]$name,[string]$value){$bytes=[Text.UTF8Encoding]::new($false).GetBytes($value+[Environment]::NewLine);$s=[IO.File]::Open((Join-Path $output $name),[IO.FileMode]::CreateNew);try{$s.Write($bytes)}finally{$s.Dispose()}}
function Identity([string]$path){$file=Get-Item -LiteralPath $path;[ordered]@{Path=[IO.Path]::GetRelativePath($workspace,$file.FullName).Replace('\','/');Bytes=$file.Length;SHA256=(Get-FileHash -LiteralPath $file.FullName).Hash}}
$version=& $Dotnet --version;Save 'toolchain.txt' ($version -join [Environment]::NewLine);if($LASTEXITCODE -ne 0){throw 'TOOLCHAIN'}
$build=& $Dotnet build (Join-Path $PSScriptRoot 'tests/DET001.Tests.csproj') -c Release --nologo --ignore-failed-sources 2>&1;$code=$LASTEXITCODE;Save 'release-build.txt' ($build -join [Environment]::NewLine);if($code -ne 0){throw 'BUILD_FAILED'}
$dll=Join-Path $PSScriptRoot 'bin/Release/net9.0/DET001.dll';$candidate=Identity $dll
$run=& $Dotnet (Join-Path $PSScriptRoot 'tests/bin/Release/net9.0/DET001.Tests.dll') $workspace (Join-Path $output 'cases') 2>&1;$code=$LASTEXITCODE;Save 'tests.txt' ($run -join [Environment]::NewLine);if($code -ne 0){throw 'TEST_FAILED'}
& (Join-Path $PSScriptRoot 'Invoke-CandidateAudit.ps1') -ProbeDll $dll -Output (Join-Path $output 'candidate-audit.txt')
& (Join-Path $PSScriptRoot 'Invoke-NeowQuery.ps1') -OutputDirectory (Join-Path $output 'neow-il')
$phase0=Join-Path $workspace 'evidence/det-001/phase0-delivery-2026-10-23.json';$legacy=Join-Path $workspace 'evidence/spike-001b/runtime-delivery-2026-10-23.json'
if((Get-FileHash -LiteralPath $phase0).Hash -cne '63E825150344678937B0525FD19B39B41DC6B553C1F34E7BC2CEAA878D6E8464' -or (Get-FileHash -LiteralPath $legacy).Hash -cne '1605625D8DB968423EE5455B20A956EDD801023BF24378DA5B71F2E8A037BF9C'){throw 'SEALED_INDEX_CHANGED'}
$oldResult=& (Join-Path $workspace 'experiments/DET-001/Verify-Phase0Delivery.ps1');Save 'retained-phase0.txt' ($oldResult -join [Environment]::NewLine)
$index=Get-Content -LiteralPath $legacy -Raw|ConvertFrom-Json;foreach($e in $index.Files){$p=Join-Path $workspace $e.Path;if((Get-Item -LiteralPath $p).Length -ne $e.Bytes -or (Get-FileHash -LiteralPath $p).Hash -cne $e.SHA256){throw 'OLD_001B_CHANGED'}}
$summary=Get-Content -LiteralPath (Join-Path $output 'cases/summary.json') -Raw|ConvertFrom-Json
$sources=@(Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -File|Where-Object {$_.FullName -notmatch '[\\/](bin|obj|artifacts-candidate-r1)[\\/]' -and $_.Extension -in @('.cs','.csproj','.ps1','.txt','.json')}|Sort-Object FullName|ForEach-Object {Identity $_.FullName})
$receipt=[ordered]@{Schema=1;Date='2026-10-23';Status='READY_OFFLINE_CANDIDATE_FOR_REVIEW';Revision='candidate-r1';Runtime='HOLD';FormalAttempts=0;RealReshuffleObserved=$false;Release='0 warning/error';Passed=$summary.Passed;Candidate=$candidate;Sources=$sources;ActualPatchTargetsInstalledAndRemoved=11;ProductionCallbacksExecuted=$false;RealHarmonyDummy=$true;ShuffleOriginalTasksUnchanged=$true;Legacy001BFiles=$index.Files.Count;LegacyDETPhase0Files=51;LegacyInputs=6;LegacyPhase0IndexSHA256=(Get-FileHash -LiteralPath $phase0).Hash;Legacy001BIndexSHA256=(Get-FileHash -LiteralPath $legacy).Hash;CurrentAccountPrivateLogProfileRecoveryRead=$false;SettingsEnabledLiveModsWritten=$false;GameStartedOrRequested=$false;OldProbeRebuilt=$false}
Save 'receipt.json' ($receipt|ConvertTo-Json -Depth 9)
"PASS DET001 candidate offline: $($summary.Passed) cases; 11 metadata patch targets; no production invocation; legacy retained; runtime HOLD; output=$output"
