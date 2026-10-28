# Read-only index/source/public legacy/pair verification. No build, account, settings or game launch.
$ErrorActionPreference='Stop';$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$indexPath=Join-Path $workspace 'evidence/det-001/candidate-r1/delivery-2026-10-23.json';$index=Get-Content -LiteralPath $indexPath -Raw|ConvertFrom-Json
if($index.Runtime -cne 'HOLD' -or $index.FormalAttempts -ne 0 -or $index.Passed -ne 68 -or $index.ModId -cne 'DET001'){throw 'STATUS_GATE'}
foreach($e in $index.Files){$p=[IO.Path]::GetFullPath((Join-Path $workspace $e.Path));if(-not $p.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'PATH_SCOPE'};if((Get-Item -LiteralPath $p).Length -ne $e.Bytes -or (Get-FileHash -LiteralPath $p).Hash -cne $e.SHA256){throw "FILE_IDENTITY $($e.Path)"}}
$receipt=Get-Content -LiteralPath (Join-Path $workspace ($index.Validation+'/receipt.json')) -Raw|ConvertFrom-Json
foreach($e in $receipt.Sources){$p=Join-Path $workspace $e.Path;if((Get-Item -LiteralPath $p).Length -ne $e.Bytes -or (Get-FileHash -LiteralPath $p).Hash -cne $e.SHA256){throw 'VALIDATED_SOURCE_CHANGED'}}
if($receipt.Candidate.SHA256 -cne $index.Candidate.SHA256 -or $receipt.ActualPatchTargetsInstalledAndRemoved -ne 11 -or $receipt.ProductionCallbacksExecuted){throw 'CANDIDATE_RECEIPT'}
foreach($e in @($index.Candidate,$index.Manifest)){$bin=Join-Path $PSScriptRoot ('bin/Release/net9.0/'+[IO.Path]::GetFileName($e.Path));if((Get-FileHash -LiteralPath $bin).Hash -cne $e.SHA256){throw 'BIN_PAIR_CHANGED'}}
$old=Join-Path $workspace 'evidence/spike-001b/runtime-delivery-2026-10-23.json';$phase0=Join-Path $workspace 'evidence/det-001/phase0-delivery-2026-10-23.json'
if((Get-FileHash -LiteralPath $old).Hash -cne $index.Legacy001BIndexSHA256 -or (Get-FileHash -LiteralPath $phase0).Hash -cne $index.LegacyPhase0IndexSHA256){throw 'LEGACY_INDEX_CHANGED'}
$legacy=Get-Content -LiteralPath $old -Raw|ConvertFrom-Json;foreach($e in $legacy.Files){$p=Join-Path $workspace $e.Path;if((Get-Item -LiteralPath $p).Length -ne $e.Bytes -or (Get-FileHash -LiteralPath $p).Hash -cne $e.SHA256){throw 'LEGACY_TARGET_CHANGED'}}
& (Join-Path $workspace 'experiments/DET-001/Verify-Phase0Delivery.ps1')
[pscustomobject]@{Result='PASS_DET001_CANDIDATE_DELIVERY';Files=$index.Files.Count;Passed=$index.Passed;PatchTargets=11;Legacy001B=$legacy.Files.Count;Runtime='HOLD';Attempts=0;CurrentAccountRead=$false;Writes=$false}|ConvertTo-Json -Compress
