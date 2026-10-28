param([string]$BackupId,[string]$BuildName='build-001',[ValidateSet('demo-001','demo-002','demo-003','demo-004','demo-005','state-001','content-001','content-002','content-003','content-004','playback-001','seek-001')][string]$Demo='demo-002',[ValidatePattern('^direct-r\d+$')][string]$Iteration,[switch]$CloudConfirmedOff)
$ErrorActionPreference='Stop';$stage='preflight'
trap{Write-Output "DEMO001_PREPARATION_FAILED stage=$stage type=$($_.Exception.GetType().Name); private paths withheld; inspect before retry";exit 1}
function Need($ok,$why){if(!$ok){throw $why}}
function NoGame{Need (@(Get-Process -Name 'SlayTheSpire2','Slay the Spire 2','sts2' -ErrorAction SilentlyContinue).Count -eq 0) 'game-running'}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
function JsonFile([string]$path,$value){[IO.File]::WriteAllText($path,($value|ConvertTo-Json -Depth 70),[Text.UTF8Encoding]::new($false))}
Need ($BuildName -cmatch '^build-\d{3}$') 'arguments';NoGame
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$evidence=Join-Path $workspace ('evidence/'+$Demo)
if($Iteration){$evidence=Join-Path $evidence $Iteration}
[void](New-Item -ItemType Directory -Path (Join-Path $evidence 'preparation') -Force)
if(!$BackupId){
    $prior=Get-Content -LiteralPath 'C:/Projects/STS2ReplayMod-local/backups/PERF001-20261026-154425-9c3b1c897d/backup.private.json' -Raw|ConvertFrom-Json
    Need ($prior.AllReadBack -and (Get-ItemProperty -LiteralPath 'HKCU:/Software/Valve/Steam/ActiveProcess' -Name ActiveUser).ActiveUser -eq $prior.ActiveUser) 'original-account-not-active'
    $stage='fresh-account-backup';$BackupId=$Demo.Replace('-','').ToUpperInvariant()+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,10)
    $backup=Join-Path 'C:/Projects/STS2ReplayMod-local/backups' $BackupId;Need (!(Test-Path -LiteralPath $backup)) 'backup-exists'
    $items=@(Get-ChildItem -LiteralPath $prior.SourceAccount -Recurse -Force)
    Need (@($items|Where-Object{($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0}).Count -eq 0) 'linked-account-file'
    $files=@($items|Where-Object{!$_.PSIsContainer}|ForEach-Object{[pscustomobject]@{Relative=[IO.Path]::GetRelativePath($prior.SourceAccount,$_.FullName);Bytes=$_.Length;SHA256=(Hash $_.FullName)}})
    [void](New-Item -ItemType Directory -Path $backup);NoGame
    Copy-Item -LiteralPath $prior.SourceAccount -Destination (Join-Path $backup 'account') -Recurse
    foreach($file in $files){Need ((Hash (Join-Path $backup ('account/'+$file.Relative))) -ceq $file.SHA256 -and (Hash (Join-Path $prior.SourceAccount $file.Relative)) -ceq $file.SHA256) 'backup-readback-or-source-changed'}
    $private=[ordered]@{BackupId=$BackupId;SourceAccount=$prior.SourceAccount;UserRoot=$prior.UserRoot;ActiveUser=$prior.ActiveUser;CreatedAt=(Get-Date -Format o);AllReadBack=$true;Files=$files}
    JsonFile (Join-Path $backup 'backup.private.json') $private
    $restore="Game must be normally exited, original test account active and Cloud off. Preserve current files separately before any restore.`nCopy contents of: $(Join-Path $backup 'account')`nBack to exact original account: $($prior.SourceAccount)`nVerify against backup.private.json. Do not copy to another account. This is a local backup only. No restore performed. Old failures/recordings remain evidence. This restores the exact local state immediately before this candidate installation."
    [IO.File]::WriteAllText((Join-Path $backup 'RESTORE.private.md'),$restore,[Text.UTF8Encoding]::new($false))
    JsonFile (Join-Path $evidence ('preparation/'+$BackupId+'-backup.json')) ([ordered]@{Result='ORIGINAL_ACCOUNT_LOCAL_BACKUP_VERIFIED';BackupId=$BackupId;Files=$files.Count;Bytes=[long](($files|Measure-Object Bytes -Sum).Sum);AllHashesReadBack=$true;SourceStable=$true;OriginalActiveAccountMatched=$true;OtherAccountsRead=$false;LocalOnly=$true;RestorePerformed=$false})
}
Need ($BackupId -cmatch '^(DEMO00[12345]|STATE001|CONTENT00[1234])-\d{8}-\d{6}-[a-f0-9]{10}$') 'backup-id'
$backup=Join-Path 'C:/Projects/STS2ReplayMod-local/backups' $BackupId
$private=Get-Content -LiteralPath (Join-Path $backup 'backup.private.json') -Raw|ConvertFrom-Json
Need ($private.AllReadBack -and $private.BackupId -ceq $BackupId -and (Get-ItemProperty -LiteralPath 'HKCU:/Software/Valve/Steam/ActiveProcess' -Name ActiveUser).ActiveUser -eq $private.ActiveUser) 'backup-scope'
$game='C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2';$mods=Join-Path $game 'mods'
Need ((Hash (Join-Path $game 'data_sts2_windows_x86_64/sts2.dll')) -ceq '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9') 'different-game'
$stage='seal-build';$sealed=Join-Path $evidence ('builds/'+$BuildName)
if(!(Test-Path -LiteralPath $sealed)){[void](New-Item -ItemType Directory -Path $sealed);foreach($name in @('ReplayMod.dll','ReplayMod.json')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('bin/Release/net9.0/'+$name)) -Destination (Join-Path $sealed $name)}}
foreach($name in @('ReplayMod.dll','ReplayMod.json')){Need ((Hash (Join-Path $sealed $name)) -ceq (Hash (Join-Path $PSScriptRoot ('bin/Release/net9.0/'+$name)))) 'build-name-reused-with-different-pair'}
$preparation=$BuildName+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8);$before=Join-Path $backup ('preparations/'+$preparation);[void](New-Item -ItemType Directory -Path $before)
$settingsPath=Join-Path $private.SourceAccount 'settings.save';$selectorPath=Join-Path $private.SourceAccount 'modded/profile.save'
Copy-Item -LiteralPath $settingsPath -Destination (Join-Path $before 'settings.before.save');Copy-Item -LiteralPath $selectorPath -Destination (Join-Path $before 'selector.before.save')
foreach($name in @('ReplayMod.dll','ReplayMod.json')){if(Test-Path -LiteralPath (Join-Path $mods $name)){Copy-Item -LiteralPath (Join-Path $mods $name) -Destination (Join-Path $before $name)}}
$settings=Get-Content -LiteralPath $settingsPath -Raw|ConvertFrom-Json -AsHashtable;$selector=Get-Content -LiteralPath $selectorPath -Raw|ConvertFrom-Json -AsHashtable
foreach($entry in $settings.mod_settings.mod_list){$entry.is_enabled=$false}
$settings.mod_settings.mod_list=@($settings.mod_settings.mod_list|Where-Object{!($_.id -ceq 'ReplayMod' -and $_.source -ceq 'mods_directory')})+@(@{id='ReplayMod';source='mods_directory';is_enabled=$true})
$settings.mod_settings.mods_enabled=$true;$selector.last_profile_id=1
$stage='install';NoGame
foreach($name in @('ReplayMod.dll','ReplayMod.json')){Copy-Item -LiteralPath (Join-Path $sealed $name) -Destination (Join-Path $mods $name) -Force}
JsonFile $settingsPath $settings;JsonFile $selectorPath $selector
foreach($name in @('ReplayMod.dll','ReplayMod.json')){Need ((Hash (Join-Path $mods $name)) -ceq (Hash (Join-Path $sealed $name))) 'installed-pair-mismatch'}
$readback=Get-Content -LiteralPath $settingsPath -Raw|ConvertFrom-Json;Need (@($readback.mod_settings.mod_list|Where-Object is_enabled).Count -eq 1 -and @($readback.mod_settings.mod_list|Where-Object is_enabled)[0].id -ceq 'ReplayMod') 'enabled-set'
NoGame
$cloud=if($CloudConfirmedOff){'User confirmed off for current test batch; no automatic Cloud modification'}else{'Unconfirmed; user must confirm off before manually starting the test; no automatic Cloud modification'}
$receipt=[ordered]@{Result=($Demo.Replace('-','').ToUpperInvariant()+'_INSTALLED_NOT_RUNTIME_VERIFIED');BackupId=$BackupId;Preparation=$preparation;Build=$BuildName;ModSHA256=(Hash (Join-Path $sealed 'ReplayMod.dll'));ManifestSHA256=(Hash (Join-Path $sealed 'ReplayMod.json'));PairMatches=$true;ConfiguredEnabledMods=@('ReplayMod');ConfiguredProfile='modded1';GameProcesses=0;Cloud=$cloud;WholeSaveRestorePerformed=$false;OtherAccountsRead=$false}
JsonFile (Join-Path $evidence ('preparation/'+$preparation+'.json')) $receipt
$receipt|ConvertTo-Json -Depth 5
