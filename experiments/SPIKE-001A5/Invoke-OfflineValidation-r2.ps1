param([string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'))
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$evidence=Join-Path $workspace 'evidence/spike-001a5'
$validation=Join-Path $evidence ('validation-r2-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($validation)
function SaveLog([string]$name,$lines){
  $bytes=[Text.Encoding]::UTF8.GetBytes(($lines -join [Environment]::NewLine)+[Environment]::NewLine)
  $stream=[IO.File]::Open((Join-Path $validation $name),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
  try{$stream.Write($bytes)}finally{$stream.Dispose()}
}
function RunDotnet([string]$log,[string[]]$arguments){
  $result=& $Dotnet @arguments 2>&1;$code=$LASTEXITCODE;SaveLog $log $result
  if($code -ne 0){throw "VALIDATION_FAILED $log exit=$code"}
  $result | Select-Object -Last 2
}
$delivery=Get-Content -LiteralPath (Join-Path $evidence 'phase0-delivery-2026-10-09.json') -Raw | ConvertFrom-Json
foreach($f in $delivery.Files){
  $path=Join-Path $workspace $f.Path
  if((Get-Item -LiteralPath $path).Length -ne $f.Bytes -or (Get-FileHash -LiteralPath $path).Hash -ne $f.SHA256){throw 'R1_DELIVERY_CHANGED'}
}
$archive=Get-Content -LiteralPath (Join-Path $evidence 'r1-source-before-r2/archive-index.json') -Raw | ConvertFrom-Json
foreach($f in $archive.Files){
  $path=Join-Path $evidence ('r1-source-before-r2/'+$f.Path)
  if((Get-Item -LiteralPath $path).Length -ne $f.Bytes -or (Get-FileHash -LiteralPath $path).Hash -ne $f.SHA256){throw 'R1_ARCHIVE_CHANGED'}
}
SaveLog 'r1-preservation.json' @([pscustomobject]@{DeliveryFilesVerified=@($delivery.Files).Count;ArchiveFilesVerified=@($archive.Files).Count;R1DeliverySHA256=(Get-FileHash -LiteralPath (Join-Path $evidence 'phase0-delivery-2026-10-09.json')).Hash;Preserved=$true}|ConvertTo-Json)
RunDotnet 'toolchain.txt' @('--info')
RunDotnet 'release-build.txt' @('build',(Join-Path $PSScriptRoot 'tests/Offline.csproj'),'-c','Release','--nologo')
$testDll=Join-Path $PSScriptRoot 'tests/bin/Release/net9.0/Offline.dll'
RunDotnet 'contracts.txt' @($testDll,'contracts',(Join-Path $validation 'fixtures'))
RunDotnet 'patch-smoke.txt' @($testDll,'patch')
RunDotnet 'dummy-smoke.txt' @($testDll,'dummy')
$audit=& (Join-Path $evidence 'Invoke-Phase0Audit-r2.ps1') -Output (Join-Path $validation 'phase0-il-audit-r2.txt')
SaveLog 'audit-summary.txt' @($audit);$audit
$source=Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File -Recurse | Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'} | Sort-Object FullName
$identities=@($source | ForEach-Object {[pscustomobject]@{Path=[IO.Path]::GetRelativePath($PSScriptRoot,$_.FullName).Replace('\','/');Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$dll=Join-Path $PSScriptRoot 'bin/Release/net9.0/SPIKE001A5.dll';$manifest=Join-Path $PSScriptRoot 'SPIKE001A5.json'
$managed='C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods'
$installed=(Test-Path -LiteralPath (Join-Path $managed 'SPIKE001A5.dll')) -or (Test-Path -LiteralPath (Join-Path $managed 'SPIKE001A5.json'))
$receipt=[pscustomobject]@{Schema=2;Revision='phase0-r2';SDK='9.0.311';Runtime='9.0.13';GameProcessCount=@(Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue).Count;
  ProductionPayloadRead=$false;ConfigurationWritten=$false;GameLaunched=$false;A5Installed=$installed;Sources=$identities;
  Candidate=[pscustomobject]@{Bytes=(Get-Item -LiteralPath $dll).Length;SHA256=(Get-FileHash -LiteralPath $dll).Hash};
  Manifest=[pscustomobject]@{Bytes=(Get-Item -LiteralPath $manifest).Length;SHA256=(Get-FileHash -LiteralPath $manifest).Hash}}
SaveLog 'receipt.json' @($receipt|ConvertTo-Json -Depth 8)
Write-Output "PASS offline validation; immutable output=$validation"
