# Read-only public identity/receipt verification. No build or game/player access.
$ErrorActionPreference='Stop';$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$indexPath=Join-Path $workspace 'evidence/det-001/phase0-delivery-2026-10-23.json'
$index=Get-Content -LiteralPath $indexPath -Raw|ConvertFrom-Json
if($index.Result -cne 'READY_FIRST_RESHUFFLE_CONTRACT' -or $index.Runtime -cne 'HOLD' -or $index.FormalAttempts -ne 0){throw 'STATUS_GATE'}
foreach($entry in $index.Files+$index.Inputs){$path=[IO.Path]::GetFullPath((Join-Path $workspace $entry.Path));if(-not $path.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'PATH_SCOPE'};if((Get-Item -LiteralPath $path).Length -ne $entry.Bytes -or (Get-FileHash -LiteralPath $path).Hash -cne $entry.SHA256){throw 'FILE_IDENTITY'}}
$receipt=Get-Content -LiteralPath (Join-Path $workspace $index.Validation) -Raw|ConvertFrom-Json
if($receipt.ComparisonCases -ne 11 -or $receipt.SyntheticFlowCases -ne 15 -or -not $receipt.OriginalSixFixtureHashesPreserved -or $receipt.LegacyDeliveryTargetsVerified -ne 158){throw 'FINAL_VALIDATION_GATE'}
foreach($entry in $receipt.Sources){$path=Join-Path $workspace $entry.Path;if((Get-Item -LiteralPath $path).Length -ne $entry.Bytes -or (Get-FileHash -LiteralPath $path).Hash -cne $entry.SHA256){throw 'VALIDATED_SOURCE_CHANGED'}}
$old=Join-Path $workspace 'evidence/spike-001b/runtime-delivery-2026-10-23.json';if((Get-FileHash -LiteralPath $old).Hash -cne $index.LegacyIndexSHA256){throw 'OLD_INDEX_CHANGED'}
[pscustomobject]@{Result='PASS_DET001_PHASE0_PUBLIC_DELIVERY';Files=$index.Files.Count;Inputs=$index.Inputs.Count;ComparisonCases=11;SyntheticFlowCases=15;Runtime='HOLD';FormalAttempts=0;PrivateOrPlayerDataRead=$false;Writes=$false}|ConvertTo-Json -Compress
