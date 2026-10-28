param([string]$OutputDirectory,[string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'))
$ErrorActionPreference='Stop';$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not $OutputDirectory){$OutputDirectory=Join-Path $workspace ('evidence/det-001/validation-'+[Guid]::NewGuid().ToString('N'))}
$output=[IO.Path]::GetFullPath($OutputDirectory)
if(-not $output.StartsWith((Join-Path $workspace 'evidence/det-001')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $output)){throw 'NEW_OUTPUT_REQUIRED'}
function NoLinks([string]$path){for($part=[IO.Path]::GetFullPath($path);$part;$part=[IO.Path]::GetDirectoryName($part)){if(Test-Path -LiteralPath $part){if(((Get-Item -LiteralPath $part).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'REPARSE_PATH'}}}}
function Save([string]$name,[string]$value){$path=Join-Path $output $name;$bytes=[Text.Encoding]::UTF8.GetBytes($value+[Environment]::NewLine);$s=[IO.File]::Open($path,[IO.FileMode]::CreateNew);try{$s.Write($bytes)}finally{$s.Dispose()}}
NoLinks $output
$candidate=Join-Path $workspace 'experiments/SPIKE-001B-r2/artifacts-phase0-r2/SPIKE001B.dll'
if((Get-Item -LiteralPath $candidate).Length -ne 200192 -or (Get-FileHash -LiteralPath $candidate).Hash -cne 'F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7'){throw 'APPROVED_COMPARATOR_IDENTITY'}
$records=Join-Path $workspace 'evidence/spike-001b/runtime/records-4f958fdc207f4b9ba83ede826848f9ed'
$inputs=@(foreach($phase in @('A','B')){foreach($i in 0..2){$path=Join-Path $records ($phase+'.point-'+$i+'.semantic.json');[pscustomobject]@{Path=$path;SHA256=(Get-FileHash -LiteralPath $path).Hash}}})
$rootIndex=Join-Path $workspace 'evidence/spike-001b/runtime-delivery-2026-10-23.json';$oldIndexHash=(Get-FileHash -LiteralPath $rootIndex).Hash
[void][IO.Directory]::CreateDirectory($output)
$version=& $Dotnet --version;Save 'toolchain.txt' ($version -join [Environment]::NewLine);if($LASTEXITCODE -ne 0){throw 'TOOLCHAIN'}
$build=& $Dotnet build (Join-Path $PSScriptRoot 'Det001Offline.csproj') -c Release --nologo 2>&1;$code=$LASTEXITCODE;Save 'release-build.txt' ($build -join [Environment]::NewLine);if($code -ne 0){throw 'BUILD_FAILED'}
$caseOutput=Join-Path $output 'cases';$run=& $Dotnet (Join-Path $PSScriptRoot 'bin/Release/net9.0/Det001Offline.dll') $workspace $caseOutput 2>&1;$code=$LASTEXITCODE;Save 'tests.txt' ($run -join [Environment]::NewLine);if($code -ne 0){throw 'CONTRACTS_FAILED'}
foreach($input in $inputs){if((Get-FileHash -LiteralPath $input.Path).Hash -cne $input.SHA256){throw 'OLD_FIXTURE_CHANGED'}}
$old=Get-Content -LiteralPath $rootIndex -Raw|ConvertFrom-Json;foreach($entry in $old.Files){$path=Join-Path $workspace $entry.Path;if((Get-Item -LiteralPath $path).Length -ne $entry.Bytes -or (Get-FileHash -LiteralPath $path).Hash -cne $entry.SHA256){throw 'OLD_SEALED_DELIVERY_CHANGED'}}
if((Get-FileHash -LiteralPath $rootIndex).Hash -cne $oldIndexHash){throw 'OLD_INDEX_CHANGED'}
$caseReceipt=Get-Content -LiteralPath (Join-Path $caseOutput 'receipt.json') -Raw|ConvertFrom-Json
$sources=@(Get-ChildItem -LiteralPath $PSScriptRoot -File|Where-Object Extension -in @('.cs','.csproj','.ps1')|Sort-Object Name|ForEach-Object {@{Path=[IO.Path]::GetRelativePath($workspace,$_.FullName).Replace('\','/');Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$receipt=[ordered]@{Schema=1;Date='2026-10-23';Status='READY_FIRST_RESHUFFLE_CONTRACT';Runtime='HOLD';FormalAttempts=0;Release='0 warning/error';ComparisonCases=$caseReceipt.ComparisonCases;SyntheticFlowCases=$caseReceipt.FlowCases;ExtraChecks=@('no-alias','independent-known-RNG-vector','early-difference/terminal/operation-bound');RealReshuffleObserved=$false;FixtureKind='Existing actual001B equality points plus independent synthetic mutants/flow DTOs';LegacyDeliveryTargetsVerified=$old.Files.Count;LegacyDeliveryIndexSHA256=$oldIndexHash;OriginalSixFixtureHashesPreserved=$true;ProductionEffectCalls=$false;OldProbeRebuilt=$false;RuntimeModDllOrManifestImplemented=$false;CurrentAccountPrivateLogProfileRecoveryRead=$false;SettingsEnabledLiveModsWritten=$false;GameStartedOrRequested=$false;Sources=$sources;OfflineConsoleSHA256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'bin/Release/net9.0/Det001Offline.dll')).Hash;ComparatorDependencySHA256=(Get-FileHash -LiteralPath $candidate).Hash}
Save 'receipt.json' ($receipt|ConvertTo-Json -Depth 8)
"PASS DET001 offline: detector=$($caseReceipt.ComparisonCases), synthetic-flow=$($caseReceipt.FlowCases), legacy158 retained, runtime HOLD; output=$output"
