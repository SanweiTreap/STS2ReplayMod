param([string] $Workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))
$ErrorActionPreference='Stop'
$stage='resolve-scope';$backup=$null
trap { Write-Output "PERF001_BACKUP_BLOCKED stage=$stage type=$($_.Exception.GetType().Name); private details withheld"; exit 1 }
function Need($ok,$why){if(!$ok){throw $why}}
function NoGame {Need (@(Get-Process -Name 'SlayTheSpire2','Slay the Spire 2','sts2' -ErrorAction SilentlyContinue).Count -eq 0) 'game-still-running'}
function NoLinks([string]$path){for($p=[IO.Path]::GetFullPath($path);$p;$p=[IO.Path]::GetDirectoryName($p)){if(Test-Path -LiteralPath $p){Need (((Get-Item -LiteralPath $p).Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0) 'reparse-path'}}}
NoGame
$userRoot=Join-Path $env:APPDATA 'SlayTheSpire2'
$active=(Get-ItemProperty -LiteralPath 'HKCU:/Software/Valve/Steam/ActiveProcess' -Name ActiveUser).ActiveUser
Need ($active -gt 0) 'no-active-steam-account'
$account=[IO.Path]::GetFullPath((Join-Path $userRoot ('steam/'+([UInt64]76561197960265728+[UInt64]$active).ToString())))
$known=Join-Path $userRoot 'SPIKE001A5/runtime-r1/3e6aaa0160e441c293b5ac4a0030b6d0/native-path.private.json'
NoLinks $known
$prior=Get-Content -LiteralPath $known -Raw | ConvertFrom-Json
Need ([IO.Path]::GetFullPath($prior.NativePath) -ieq [IO.Path]::GetFullPath((Join-Path $account 'modded/profile1/saves/current_run.save'))) 'active-account-not-original-test-account'
NoLinks $account;Need (Test-Path -LiteralPath (Join-Path $account 'settings.save') -PathType Leaf) 'original-account-settings-absent'
$items=@(Get-ChildItem -LiteralPath $account -Recurse -Force)
foreach($item in $items){Need (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0) 'account-reparse-item'}
$files=@($items | Where-Object { !$_.PSIsContainer });Need ($files.Count -gt 0) 'empty-account'
$stage='new-private-backup'
$id='PERF001-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,10)
$backupRoot='C:/Projects/STS2ReplayMod-local/backups'
$backup=[IO.Path]::GetFullPath((Join-Path $backupRoot $id));NoLinks $backup
Need ($backup.StartsWith([IO.Path]::GetFullPath($backupRoot)+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -and !(Test-Path -LiteralPath $backup)) 'new-backup-target'
[void](New-Item -ItemType Directory -Path $backup)
$manifest=@($files | ForEach-Object {[ordered]@{Relative=[IO.Path]::GetRelativePath($account,$_.FullName);Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
$stage='copy-original-account'
NoGame
Copy-Item -LiteralPath $account -Destination (Join-Path $backup 'account') -Recurse -Force
$stage='readback-all-account-files'
foreach($file in $manifest){
    $source=Join-Path $account $file.Relative;$copy=Join-Path $backup ('account/'+$file.Relative)
    Need ((Get-Item -LiteralPath $copy).Length -eq $file.Bytes -and (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash -ceq $file.SHA256) 'backup-readback-mismatch'
    Need ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ceq $file.SHA256) 'source-changed-during-backup'
}
Need ((Get-ItemProperty -LiteralPath 'HKCU:/Software/Valve/Steam/ActiveProcess' -Name ActiveUser).ActiveUser -eq $active) 'steam-account-changed'
NoGame
$stage='private-restore-journal'
$logical='user://steam/'+([UInt64]76561197960265728+[UInt64]$active).ToString()+'/modded/profile1'
$scopeHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes('PERF001-scope:'+$logical)))
$private=[ordered]@{Schema=1;BackupId=$id;SourceAccount=$account;UserRoot=$userRoot;ActiveUser=$active;ProfileScopeBinding=$scopeHash;Files=$manifest;CreatedAt=(Get-Date -Format 'o');AllReadBack=$true}
$private | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backup 'backup.private.json') -Encoding utf8
@"
# Restore instructions (private)
Game must be normally exited and Steam Cloud off. This backup belongs only to the original test account.
Source account directory: $account
Backup account directory: $(Join-Path $backup 'account')
Before restoring, retain the failed/current account directory as a separate backup. Copy this backup's account contents back to the exact source above, then verify against backup.private.json. Do not copy into another account. Restore is a new round; retained runtime results are not erased. No automatic restore has been executed.
This backup contains local files only; Steam Cloud state is not backed up or guaranteed.
"@ | Set-Content -LiteralPath (Join-Path $backup 'RESTORE.private.md') -Encoding utf8
$receipt=[ordered]@{Result='ORIGINAL_TEST_ACCOUNT_LOCAL_BACKUP_READBACK_VERIFIED';BackupId=$id;BackupDirectory=$backup;Files=$manifest.Count;Bytes=[long](($manifest | ForEach-Object {$_.Bytes} | Measure-Object -Sum).Sum);AllFileHashesReadBack=$true;SourceUnchangedDuringCopy=$true;OriginalActiveAccountMatchedKnownMetadata=$true;OtherAccountsRead=$false;GameProcesses=0;LocalOnly=$true;Cloud='User confirmed off before this preparation; not programmatically modified';PrivateManifestSHA256=(Get-FileHash -LiteralPath (Join-Path $backup 'backup.private.json') -Algorithm SHA256).Hash}
$publicRoot=Join-Path $Workspace 'evidence/perf-001/candidate-r1/preparation';[void](New-Item -ItemType Directory -Path $publicRoot -Force)
$receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $publicRoot ($id+'-backup.json')) -Encoding utf8
$receipt | ConvertTo-Json -Depth 5
