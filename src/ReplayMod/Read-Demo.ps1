param([string]$BackupId='DEMO001-20261026-180851-5ee9771d51')
$ErrorActionPreference='Stop'
trap{Write-Output ('DEMO001_READ_FAILED '+$_.Exception.GetType().Name+'; private paths withheld');exit 1}
if($BackupId -cnotmatch '^(DEMO00[12345]|STATE001|CONTENT00[1234])-\d{8}-\d{6}-[a-f0-9]{10}$'){throw 'invalid backup id'}
$private=Get-Content -LiteralPath (Join-Path ('C:/Projects/STS2ReplayMod-local/backups/'+$BackupId) 'backup.private.json') -Raw|ConvertFrom-Json
$root=Join-Path $private.UserRoot 'ReplayMod/demo-001'
$result=[ordered]@{GameProcesses=@(Get-Process -Name 'SlayTheSpire2','Slay the Spire 2','sts2' -ErrorAction SilentlyContinue|Select-Object ProcessName,Id);OwnDirectoryExists=(Test-Path -LiteralPath $root)}
$pointer=Join-Path $root 'clips/latest.txt'
if(Test-Path -LiteralPath $pointer){
    $id=[IO.File]::ReadAllText($pointer);if($id -cnotmatch '^[a-f0-9]{32}$'){throw 'invalid clip pointer'}
    $file=Join-Path $root ('clips/'+$id+'.json');if((Get-Item -LiteralPath $file).Length -gt 4194304){throw 'clip too large'}
    $envelope=Get-Content -LiteralPath $file -Raw|ConvertFrom-Json;$clip=$envelope.Data
    $result.LatestClip=[ordered]@{Id=$clip.Id;Format=$clip.Format;CreatedUtc=$clip.CreatedUtc;GameHash=$clip.GameHash;ModHash=$clip.ModHash;Character=$clip.Character;Seed=$clip.Seed;Row=$clip.Row;Col=$clip.Col;Encounter=$clip.Encounter;Preparation=$clip.Preparation;Authority=$clip.Authority;OperationCount=$clip.Operations.Count;Victory=($clip.Operations[-1].Victory -eq $true);Operations=@($clip.Operations|Select-Object Sequence,Kind,Turn,HandIndex,Card,CardState,Potion,Target,Victory);FileBytes=(Get-Item -LiteralPath $file).Length;NativeCheckpointPublished=$false}
    $result.LatestClip.RecordingTiming=$clip.Timing
    $result.LatestClip.RawOperationTimeKnown=($clip.Format -ge 5 -and $null -ne $clip.Timing)
    $result.LatestClip.OperationTimes=@($clip.Operations|Select-Object Sequence,Timing)
    if($clip.Choices){$result.Choices=@($clip.Choices)}
    $statePath=Join-Path $root ('states/'+$id+'.state.json')
    if(Test-Path -LiteralPath $statePath){$stateFile=Get-Item -LiteralPath $statePath;if($stateFile.Length -gt 8388608){throw 'state too large'};$s=(Get-Content -LiteralPath $statePath -Raw|ConvertFrom-Json).Data
        $result.AuthorityState=[ordered]@{Version=$s.Version;Clip=$s.Clip;Position=$s.Position;Game=$s.Game;Mod=$s.Mod;RecordingRuntime=$s.RecordingRuntime;ProcessId=$s.ProcessId;ProcessStartedUtc=$s.ProcessStartedUtc;FileBytes=$stateFile.Length;FileSHA256=(Get-FileHash -LiteralPath $statePath).Hash;Nodes=$s.Graph.Nodes.Count;Bindings=$s.Graph.Bindings;BindingShape=$s.Graph.BindingShape;PayloadPublished=$false}}
}
$sessions=Join-Path $root 'sessions'
if(Test-Path -LiteralPath $sessions){
    $recent=@(Get-ChildItem -LiteralPath $sessions -Directory|Sort-Object LastWriteTime -Descending|Select-Object -First 4)
    $result.Sessions=@(foreach($session in $recent){
        $events=@(Get-ChildItem -LiteralPath $session.FullName -File -Filter '*.json'|Sort-Object Name|ForEach-Object{
            $event=Get-Content -LiteralPath $_.FullName -Raw|ConvertFrom-Json
            if($event.Kind -ceq 'state-difference'){[pscustomobject]@{Kind=$event.Kind;At=$event.At;Point=$event.Data.Point;DetailedStates='retained privately'}}
            else{$event}
        });[pscustomobject]@{SessionId=$session.Name;Events=$events}
    })
}
$result|ConvertTo-Json -Depth 48
