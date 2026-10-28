param(
    [Parameter(Mandatory)][string]$BackupId,
    [ValidateSet('C1','C0','C2')][string]$Mode='C1',
    [string]$BuildName='build-001',
    [string]$RunId='c1-20261026-r1-01',
    [string]$Seed='PERF001R1A',
    [string]$BaselineRun,
    [switch]$CloudConfirmedOff,
    [string]$Workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
)
$ErrorActionPreference='Stop';$stage='preflight'
trap{Write-Output "PERF001_PREPARATION_BLOCKED stage=$stage type=$($_.Exception.GetType().Name); inspect current receipt, do not blindly replay; private details withheld";exit 1}
function Need($ok,$why){if(!$ok){throw $why}}
function NoGame {Need (@(Get-Process -Name 'SlayTheSpire2','Slay the Spire 2','sts2' -ErrorAction SilentlyContinue).Count -eq 0) 'game-running'}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
function NoLinks([string]$path){for($p=[IO.Path]::GetFullPath($path);$p;$p=[IO.Path]::GetDirectoryName($p)){if(Test-Path -LiteralPath $p){Need (((Get-Item -LiteralPath $p).Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0) 'reparse-path'}}}
function WriteJson([string]$path,$value){NoLinks $path;[IO.File]::WriteAllText($path,($value|ConvertTo-Json -Depth 70),[Text.UTF8Encoding]::new($false))}
Need ($CloudConfirmedOff -and $BackupId -cmatch '^PERF001-\d{8}-\d{6}-[a-f0-9]{10}$' -and $BuildName -cmatch '^build-\d{3}$' -and $RunId -cmatch '^[a-z0-9-]{1,48}$' -and $Seed -cmatch '^[A-Za-z0-9]{1,64}$') 'preparation-arguments'
NoGame
$backup=Join-Path 'C:/Projects/STS2ReplayMod-local/backups' $BackupId;NoLinks $backup
$private=Get-Content -LiteralPath (Join-Path $backup 'backup.private.json') -Raw|ConvertFrom-Json
Need ($private.AllReadBack -and $private.BackupId -ceq $BackupId) 'backup-not-verified'
Need ((Get-ItemProperty -LiteralPath 'HKCU:/Software/Valve/Steam/ActiveProcess' -Name ActiveUser).ActiveUser -eq $private.ActiveUser) 'original-account-changed'
$game='C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2';$mods=Join-Path $game 'mods'
Need ((Hash (Join-Path $game 'data_sts2_windows_x86_64/sts2.dll')) -ceq '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9') 'game-build-changed'
$namespace=Join-Path $private.UserRoot 'PERF001/candidate-r1';NoLinks $namespace
Need (!(Test-Path -LiteralPath (Join-Path $namespace $RunId))) 'run-already-exists'
$expected=@()
if($Mode -ne 'C1'){
    Need ($BaselineRun -cmatch '^[a-z0-9-]{1,48}$') 'baseline-required'
    $baseline=Join-Path $namespace $BaselineRun
    $complete=Get-Content -LiteralPath (Join-Path $baseline 'complete.json') -Raw|ConvertFrom-Json
    Need ($complete.Mode -ceq 'C1' -and $complete.Terminal) 'baseline-not-complete-C1'
    $expected=@('map','combat','card','turn' | ForEach-Object {Get-Content -LiteralPath (Join-Path $baseline ('scene-'+$_+'.json')) -Raw|ConvertFrom-Json})
}
$stage='seal-pair-and-preserve-prior-targets'
$sealed=Join-Path $Workspace ('evidence/perf-001/candidate-r1/builds/'+$BuildName)
if(!(Test-Path -LiteralPath $sealed)){
    [void](New-Item -ItemType Directory -Path $sealed)
    foreach($name in @('PERF001.dll','PERF001.json')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('bin/Release/net9.0/'+$name)) -Destination (Join-Path $sealed $name)}
}
foreach($name in @('PERF001.dll','PERF001.json')){Need ((Hash (Join-Path $sealed $name)) -ceq (Hash (Join-Path $PSScriptRoot ('bin/Release/net9.0/'+$name)))) 'sealed-pair-differs-from-build'}
$prepId=$RunId+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$prior=Join-Path $backup ('preparations/'+$prepId);[void](New-Item -ItemType Directory -Path $prior)
foreach($name in @('PERF001.dll','PERF001.json')){if(Test-Path -LiteralPath (Join-Path $mods $name)){Copy-Item -LiteralPath (Join-Path $mods $name) -Destination (Join-Path $prior $name)}}
if(Test-Path -LiteralPath (Join-Path $namespace 'configuration.json')){Copy-Item -LiteralPath (Join-Path $namespace 'configuration.json') -Destination (Join-Path $prior 'configuration.before.json')}
$settingsPath=Join-Path $private.SourceAccount 'settings.save';$selectorPath=Join-Path $private.SourceAccount 'modded/profile.save'
Copy-Item -LiteralPath $settingsPath -Destination (Join-Path $prior 'settings.before.save')
Copy-Item -LiteralPath $selectorPath -Destination (Join-Path $prior 'selector.before.save')
$settings=Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json -AsHashtable
$selector=Get-Content -LiteralPath $selectorPath -Raw | ConvertFrom-Json -AsHashtable
Need ($settings.ContainsKey('mod_settings') -and $selector.ContainsKey('last_profile_id')) 'settings-shape'
$list=[Collections.Generic.List[object]]::new();$keys=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach($entry in $settings.mod_settings.mod_list){$entry.is_enabled=($entry.id -ceq 'PERF001' -and $entry.source -ceq 'mods_directory');[void]$keys.Add($entry.id+'|'+$entry.source);$list.Add($entry)}
function Inventory([string]$folder,[string]$source){
    if(!(Test-Path -LiteralPath $folder)){return}
    foreach($file in Get-ChildItem -LiteralPath $folder -Recurse -File -Filter '*.json'){
        $manifest=Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json -AsHashtable
        if($manifest -is [Collections.IDictionary] -and $manifest.ContainsKey('id') -and $manifest.ContainsKey('version') -and ($manifest.ContainsKey('has_dll') -or $manifest.ContainsKey('has_pck'))){[pscustomobject]@{Id=[string]$manifest.id;Source=$source}}
    }
}
$inventory=@(Inventory $mods 'mods_directory')+@(Inventory (Join-Path $game 'mods_STEAMTEST') 'steam_workshop')
$vdf=[IO.File]::ReadAllText('C:/Program Files (x86)/Steam/steamapps/libraryfolders.vdf')
foreach($match in [regex]::Matches($vdf,'"path"\s+"([^"]+)"')){$lib=$match.Groups[1].Value.Replace('\\','\');$inventory+=@(Inventory (Join-Path $lib 'steamapps/workshop/content/2868840') 'steam_workshop')}
$inventory+=,[pscustomobject]@{Id='PERF001';Source='mods_directory'}
foreach($item in $inventory){if($keys.Add($item.Id+'|'+$item.Source)){$list.Add(@{id=$item.Id;source=$item.Source;is_enabled=($item.Id -ceq 'PERF001' -and $item.Source -ceq 'mods_directory')})}}
$settings.mod_settings.mod_list=$list.ToArray();$settings.mod_settings.mods_enabled=$true;$selector.last_profile_id=1
$stage='install-and-configure'
NoGame
foreach($name in @('PERF001.dll','PERF001.json')){NoLinks (Join-Path $mods $name);Copy-Item -LiteralPath (Join-Path $sealed $name) -Destination (Join-Path $mods $name) -Force}
[void](New-Item -ItemType Directory -Path $namespace -Force)
WriteJson (Join-Path $namespace 'configuration.json') ([ordered]@{Schema=1;Revision='candidate-r1';Mode=$Mode;RunId=$RunId;Seed=$Seed;RuntimeAuthorized=$true;UserConfirmedCloudOff=$true;EstablishBaseline=($Mode -eq 'C1');CandidateSHA=(Hash (Join-Path $sealed 'PERF001.dll'));ManifestSHA=(Hash (Join-Path $sealed 'PERF001.json'));ExpectedProfileScopeBinding=$private.ProfileScopeBinding;ExpectedScenes=$expected})
WriteJson $settingsPath $settings;WriteJson $selectorPath $selector
$stage='readback'
foreach($name in @('PERF001.dll','PERF001.json')){Need ((Hash (Join-Path $mods $name)) -ceq (Hash (Join-Path $sealed $name))) 'installed-pair-mismatch'}
$reread=Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
Need (@($reread.mod_settings.mod_list | Where-Object is_enabled).Count -eq 1 -and @($reread.mod_settings.mod_list | Where-Object is_enabled)[0].id -ceq 'PERF001') 'enabled-set-mismatch'
NoGame
$receipt=[ordered]@{Result='PERF001_PILOT_PREPARED_NOT_RUNTIME_VERIFIED';Preparation=$prepId;BackupId=$BackupId;Mode=$Mode;RunId=$RunId;Seed=$Seed;Build=$BuildName;CandidateSHA256=(Hash (Join-Path $sealed 'PERF001.dll'));ManifestSHA256=(Hash (Join-Path $sealed 'PERF001.json'));InstalledPairMatches=$true;ConfiguredEnabledIds=@('PERF001');ConfiguredProfile=1;ConfiguredNamespace='modded';ExpectedScenes=$expected.Count;GameProcesses=0;Cloud='user confirmed off';AccountPathsPublished=$false;OtherAccountsRead=$false;ActualLoadedSet='not yet observed';GameRuntimeAttempts=0}
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $Workspace ('evidence/perf-001/candidate-r1/preparation/'+$prepId+'.json')) -Encoding utf8
$receipt | ConvertTo-Json -Depth 6
