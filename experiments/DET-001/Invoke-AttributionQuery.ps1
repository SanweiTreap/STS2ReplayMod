param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$output=[IO.Path]::GetFullPath($OutputDirectory)
if(-not $output.StartsWith((Join-Path $workspace 'evidence/det-001')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $output)){throw 'NEW_OUTPUT_SCOPE'}
$query=Join-Path $workspace 'evidence/inv-002/Invoke-NarrowIlQuery.ps1';$cecil=Join-Path $env:USERPROFILE '.nuget/packages/ilspycmd/9.1.0.7988/tools/net8.0/any/Mono.Cecil.dll'
$pattern='(MegaRandom::(NextDouble\(\)|FillSerializableState\()|RunRngSet::(\.ctor\(System.String\)|CreateRng\()|Rng::\.ctor\(System.UInt64,System.String\))'
$lines=& $query -Mode Disassemble -Pattern $pattern -CecilPath $cecil;if(-not $lines){throw 'EMPTY_ATTRIBUTION_QUERY'}
[void][IO.Directory]::CreateDirectory($output);$path=Join-Path $output 'state-stream-bridge.il.txt';$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($lines -join [Environment]::NewLine)+[Environment]::NewLine);$s=[IO.File]::Open($path,[IO.FileMode]::CreateNew);try{$s.Write($bytes)}finally{$s.Dispose()}
$receipt=@{Schema=1;Date='2026-10-23';Mode='Disassemble';Pattern=$pattern;File='state-stream-bridge.il.txt';Bytes=$bytes.Length;SHA256=(Get-FileHash -LiteralPath $path).Hash;Scope='FIXED_IL_ONLY_NO_PRODUCTION_CALLS'};$path=Join-Path $output 'receipt.json';$bytes=[Text.Encoding]::UTF8.GetBytes(($receipt|ConvertTo-Json)+[Environment]::NewLine);$s=[IO.File]::Open($path,[IO.FileMode]::CreateNew);try{$s.Write($bytes)}finally{$s.Dispose()}
'PASS narrow RNG recurrence/serialization/stream-construction attribution bridge'
