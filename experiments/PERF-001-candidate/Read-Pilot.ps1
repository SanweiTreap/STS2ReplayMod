param([Parameter(Mandatory)][string]$BackupId,[Parameter(Mandatory)][string]$RunId)
$ErrorActionPreference='Stop'
trap{Write-Output ('PERF001_READ_FAILED '+$_.Exception.GetType().Name+'; private details withheld');exit 1}
if($BackupId -notmatch '^PERF001-\d{8}-\d{6}-[a-f0-9]{10}$' -or $RunId -notmatch '^[a-z0-9-]{1,48}$'){throw 'invalid scoped names'}
$private=Get-Content -LiteralPath (Join-Path ('C:/Projects/STS2ReplayMod-local/backups/'+$BackupId) 'backup.private.json') -Raw|ConvertFrom-Json
$root=Join-Path $private.UserRoot ('PERF001/candidate-r1/'+$RunId)
$summary=[ordered]@{RunId=$RunId;GameProcesses=@(Get-Process -Name 'SlayTheSpire2','Slay the Spire 2','sts2' -ErrorAction SilentlyContinue|Select-Object ProcessName,Id);RunDirectoryCreated=(Test-Path -LiteralPath $root)}
if(Test-Path -LiteralPath $root){
    $latest=Get-ChildItem -LiteralPath $root -File -Filter 'status-*.json'|Sort-Object Name|Select-Object -Last 1
    if($latest){$summary.LatestStatus=Get-Content -LiteralPath $latest.FullName -Raw|ConvertFrom-Json}
    foreach($name in @('complete','failure','published-identity')){if(Test-Path -LiteralPath (Join-Path $root ($name+'.json'))){$summary[$name]=Get-Content -LiteralPath (Join-Path $root ($name+'.json')) -Raw|ConvertFrom-Json}}
    $summary.AuthoritativeStage=if($summary.Contains('failure')){'Failed'}elseif($summary.Contains('complete')){'Complete'}else{$summary.LatestStatus.Stage}
    $summary.Files=@(Get-ChildItem -LiteralPath $root -File|Select-Object Name,Length)
}
$summary|ConvertTo-Json -Depth 7
