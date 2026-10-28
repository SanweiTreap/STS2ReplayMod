param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$output=[IO.Path]::GetFullPath($OutputDirectory)
if(-not $output.StartsWith((Join-Path $workspace 'evidence/det-001')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'OUTPUT_SCOPE'}
if(Test-Path -LiteralPath $output){throw 'CREATE_NEW_OUTPUT_REQUIRED'}
$dll='C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/sts2.dll'
if((Get-Item -LiteralPath $dll).Length -ne 9757184 -or (Get-FileHash -LiteralPath $dll).Hash -cne '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9'){throw 'FIXED_ASSEMBLY_IDENTITY'}
[void][IO.Directory]::CreateDirectory($output)
$query=Join-Path $workspace 'evidence/inv-002/Invoke-NarrowIlQuery.ps1';$cecil=Join-Path $env:USERPROFILE '.nuget/packages/ilspycmd/9.1.0.7988/tools/net8.0/any/Mono.Cecil.dll'
$sets=[ordered]@{
 'draw-reshuffle.il.txt'=@('Disassemble','(CardPileCmd/<(DrawInternal|ShuffleIfNecessary|Shuffle|ShuffleFtueCheck)>d__.*::MoveNext\(|CardPileCmd::(CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot|Shuffle|ShuffleIfNecessary)\()')
 'permutation-rng.il.txt'=@('Disassemble','(ListExtensions::(StableShuffle|UnstableShuffle)\(|CardModel::CompareTo\(|AbstractModel::CompareTo\(|ModelId::CompareTo\(|Rng::(NextInt\(System.Int32\)|ToSerializable\()|MegaRandom::(Next\(System.Int32\)|NextInner\(System.Int32\)|NextULongInner\(|GetState\(|ToSerializable\()|RunRngSet::(get_Shuffle|get_CombatCardGeneration|GetRng|ToSerializable)\()')
 'initial-turn-input.il.txt'=@('Disassemble','(Player::PopulateCombatState\(|CardPile::(RandomizeOrderInternal|get_MaxCardsInHand|AddInternal)\(|CombatManager/<SetupPlayerTurn>d__.*::MoveNext\(|Ironclad::get_StartingDeck\(|Hook::ModifyShuffleOrder\()')
 'draw-shuffle-callers.txt'=@('FindCalls','(CardPileCmd::(DrawInternal|ShuffleIfNecessary|Shuffle)\(|CardPile::RandomizeOrderInternal\(|Player::PopulateCombatState\(|ListExtensions::StableShuffle)')
}
$files=@();foreach($entry in $sets.GetEnumerator()){$lines=& $query -Mode $entry.Value[0] -Pattern $entry.Value[1] -AssemblyPath $dll -CecilPath $cecil;if(-not $lines){throw 'EMPTY_QUERY'};$path=Join-Path $output $entry.Key;$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($lines -join [Environment]::NewLine)+[Environment]::NewLine);$stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$stream.Write($bytes)}finally{$stream.Dispose()};$files+=@{File=$entry.Key;Mode=$entry.Value[0];Pattern=$entry.Value[1];Bytes=$bytes.Length;SHA256=(Get-FileHash -LiteralPath $path).Hash}}
$receipt=[ordered]@{Schema=1;Date='2026-10-23';Scope='DET001_OFFLINE_FIXED_IL_ONLY';AssemblyBytes=9757184;AssemblySHA256=(Get-FileHash -LiteralPath $dll).Hash;CecilSHA256=(Get-FileHash -LiteralPath $cecil).Hash;ReaderSHA256=(Get-FileHash -LiteralPath $query).Hash;Files=$files;PlayerOrPrivateOrLogRead=$false;GameStarted=$false;ProductionEffectsCalled=$false}
$path=Join-Path $output 'receipt.json';$bytes=[Text.Encoding]::UTF8.GetBytes(($receipt|ConvertTo-Json -Depth 7)+[Environment]::NewLine);$stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$stream.Write($bytes)}finally{$stream.Dispose()}
'PASS fixed hash-gated narrow draw/reshuffle/permutation/owner/turn IL queries'
