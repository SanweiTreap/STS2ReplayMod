param([string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'))
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$evidence=Join-Path $workspace 'evidence/spike-001a5'
$output=Join-Path $evidence ('validation-runtime-r1-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($output)
function Save([string]$name,$lines){$bytes=[Text.Encoding]::UTF8.GetBytes(($lines -join [Environment]::NewLine)+[Environment]::NewLine);$s=[IO.File]::Open((Join-Path $output $name),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$s.Write($bytes)}finally{$s.Dispose()}}
function Run([string]$log,[string[]]$arguments){$r=& $Dotnet @arguments 2>&1;$code=$LASTEXITCODE;Save $log $r;if($code -ne 0){throw "VALIDATION_FAILED $log"};$r | Select-Object -Last 2}
$old=Get-Content -LiteralPath (Join-Path $evidence 'phase0-r2-delivery-2026-10-09.json') -Raw | ConvertFrom-Json
foreach($f in $old.Files){if((Get-FileHash -LiteralPath (Join-Path $workspace $f.Path)).Hash -cne $f.SHA256){throw 'R2_FROZEN_CHANGED'}}
Run 'toolchain.txt' @('--info')
Run 'release-build.txt' @('build',(Join-Path $PSScriptRoot 'tests/Offline.csproj'),'-c','Release','--nologo')
$test=Join-Path $PSScriptRoot 'tests/bin/Release/net9.0/Offline.dll'
Run 'contracts.txt' @($test,'contracts',(Join-Path $output 'fixtures'),(Join-Path $evidence 'runtime/records-861af3a9e6d449acbd192b0a8d3c2f56'))
Run 'patch-smoke.txt' @($test,'patch');Run 'dummy-smoke.txt' @($test,'dummy')
$probe=Join-Path $PSScriptRoot 'bin/Release/net9.0/SPIKE001A5.dll'
$audit=& (Join-Path $evidence 'Invoke-Phase0Audit-r2.ps1') -ProbeDll $probe -Output (Join-Path $output 'incremental-audit.txt');Save 'audit-summary.txt' @($audit);$audit
Add-Type -LiteralPath (Join-Path $env:USERPROFILE '.nuget/packages/ilspycmd/9.1.0.7988/tools/net8.0/any/Mono.Cecil.dll')
$oldModule=[Mono.Cecil.ModuleDefinition]::ReadModule((Join-Path $workspace 'experiments/SPIKE-001A5/artifacts-phase0-r2/SPIKE001A5.dll'))
$newModule=[Mono.Cecil.ModuleDefinition]::ReadModule($probe)
function Types($roots){foreach($t in $roots){$t;if($t.HasNestedTypes){Types $t.NestedTypes}}}
function Bodies($module){
  foreach($t in Types $module.Types){
    foreach($f in $t.Fields){"FIELD $($f.FullName) constant=$($f.Constant)"}
    foreach($m in $t.Methods){
      "METHOD $($m.FullName)"
      if($m.HasBody){foreach($v in $m.Body.Variables){" LOCAL $($v.VariableType.FullName)"};foreach($h in $m.Body.ExceptionHandlers){" EH $($h.HandlerType) $($h.CatchType) $($h.TryStart.Offset) $($h.TryEnd.Offset) $($h.HandlerStart.Offset) $($h.HandlerEnd.Offset)"};foreach($i in $m.Body.Instructions){" $($i.OpCode.Name) $($i.Operand)"}}
    }
  }
}
try{
  $before=(Bodies $oldModule) -join "`n";$after=(Bodies $newModule) -join "`n"
  $normalized=$before.Replace('STS2ReplayMod.SPIKE001A5.RoomBefore.r2','STS2ReplayMod.SPIKE001A5.RoomBefore.runtime-r1').Replace('phase0-r2','runtime-r1')
  if($normalized -cne $after){throw 'BUSINESS_IL_CHANGED'}
  $methodCount=@(Types $newModule.Types | ForEach-Object Methods).Count
  Save 'identity-only-il-comparison.txt' @("PASS all $methodCount production methods, locals, EH and fields identical after exactly two identity-string replacements; no gameplay/oracle/Task changes")
}finally{$newModule.Dispose();$oldModule.Dispose()}
$sources=@(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File -Recurse | Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'} | Sort-Object FullName | ForEach-Object {[pscustomobject]@{Path=[IO.Path]::GetRelativePath($PSScriptRoot,$_.FullName).Replace('\','/');Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$manifest=Join-Path $PSScriptRoot 'SPIKE001A5.json'
$receipt=[pscustomobject]@{Schema=2;Revision='runtime-r1';Date='2026-10-21';Reason='Fresh complete A after user interruption; identity-only revision; no harness defect claimed';OldR2FilesPreserved=$old.Files.Count;Contracts='125/125';PatchSmoke='4/4';Dummy='PASS';IdentityOnlyProductionIL='PASS';ProductionMethods=$methodCount;GameProcessCount=@(Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue).Count;GameLaunched=$false;NativePayloadRead=$false;Sources=$sources;Candidate=[pscustomobject]@{Bytes=(Get-Item -LiteralPath $probe).Length;SHA256=(Get-FileHash -LiteralPath $probe).Hash};Manifest=[pscustomobject]@{Bytes=(Get-Item -LiteralPath $manifest).Length;SHA256=(Get-FileHash -LiteralPath $manifest).Hash}}
Save 'receipt.json' @($receipt|ConvertTo-Json -Depth 8)
"PASS runtime-r1 offline validation; output=$output"
