param(
    [string]$SourceRoot = 'C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src',
    [string]$OutputDirectory = 'C:/Projects/STS2ReplayMod/evidence/content-coverage'
)

$ErrorActionPreference = 'Stop'
$sourceBase = [IO.Path]::GetFullPath($SourceRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath (Join-Path $sourceBase 'MegaCrit.Sts2.Core.Models/ModelDb.cs'))) {
    throw 'Expected decompiled source layout is missing.'
}

# Lexical research index only: do not load game assemblies or execute models.
$families = @('Cards','Relics','Potions','Powers','Monsters','Encounters','Characters','Orbs','Afflictions','Enchantments','CardPools','RelicPools','PotionPools','Acts','Modifiers')
$poolRefs = [Collections.Generic.List[object]]::new()
$actEncounters = [Collections.Generic.List[object]]::new()
$encounterMonsters = [Collections.Generic.List[object]]::new()
$models = [Collections.Generic.List[object]]::new()
$calls = [Collections.Generic.List[object]]::new()
$familyCounts = [Collections.Generic.List[object]]::new()
$commandPattern = [regex]'\b(CardSelectCmd|CardCmd|CardPileCmd|CreatureCmd|OrbCmd|OstyCmd|PlayerCmd|PowerCmd|RelicCmd|PotionCmd)\.([A-Za-z_]\w*)'
$poolPattern = [regex]'\bModelDb\.(Card|Relic|Potion)<([A-Za-z_]\w*)>'
$targetPattern = [regex]'\bTargetType\.([A-Za-z_]\w*)'
$signals = [ordered]@{
    Choice = '\bCardSelectCmd\.'
    Orb = '\bOrbCmd\.|\bOrbQueue\b'
    Pet = '\bOstyCmd\.|\bAddPet(?:Internal)?\b|\bPetOwner\b|TargetType\.Osty'
    CreatureAddition = '\bCreatureCmd\.Add\s*<|\bCreateCreature\s*\('
    Transformation = '\bCardCmd\.Transform|\bIsHatched\b|\bRespawns\b'
    DeathOrCombatEndOverride = 'ShouldCreatureBeRemovedFromCombatAfterDeath|ShouldOwnerDeathTriggerFatal|AfterDeath\s*\(|ShouldStopCombatFromEnding|ShouldDie(?:Late)?\s*\(|AfterPreventingDeath\s*\('
    RelicMembership = '\bRelicCmd\.(Obtain|Remove)|AddRelicInternal|RemoveRelicInternal'
    RandomReference = '\b(?:RunRng|PlayerRng|Rng)\.'
    MultiplayerFilter = '\bMultiplayerConstraint\b'
    DelayedOrAsync = '\bTaskHelper\.RunSafely|\bCallDeferred\b|\bContinueWith\b'
}

foreach ($family in $families) {
    $directory = Join-Path $sourceBase ('MegaCrit.Sts2.Core.Models.' + $family)
    if (-not (Test-Path -LiteralPath $directory)) { continue }
    $files = @(Get-ChildItem -LiteralPath $directory -Filter '*.cs' -File | Sort-Object Name)
    $familyCounts.Add([ordered]@{ Family=$family; SourceFiles=$files.Count })
    foreach ($file in $files) {
        $contents = [IO.File]::ReadAllText($file.FullName)
        $relative = $file.FullName.Substring($sourceBase.Length + 1).Replace('\','/')
        $tags = @($signals.Keys | Where-Object { $contents -match $signals[$_] })
        $targets = @($targetPattern.Matches($contents) | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        $models.Add([ordered]@{ Family=$family; Model=$file.BaseName; Source=$relative; CandidateMechanisms=$tags; TargetReferences=$targets })
        if ($family -in @('CardPools','RelicPools','PotionPools')) {
            foreach ($match in $poolPattern.Matches($contents)) {
                $poolRefs.Add([ordered]@{ Pool=$file.BaseName; PoolFamily=$family; ModelFamily=($match.Groups[1].Value + 's'); Model=$match.Groups[2].Value })
            }
        }
        if ($family -eq 'Acts') {
            $encounters = @([regex]::Matches($contents, '\bModelDb\.Encounter<([A-Za-z_]\w*)>') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
            foreach ($encounter in $encounters) { $actEncounters.Add([ordered]@{ Act=$file.BaseName; Encounter=$encounter }) }
        }
        if ($family -eq 'Encounters') {
            $monsters = @([regex]::Matches($contents, '\bModelDb\.Monster<([A-Za-z_]\w*)>') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
            foreach ($monster in $monsters) { $encounterMonsters.Add([ordered]@{ Encounter=$file.BaseName; Monster=$monster }) }
        }
        $lines = [IO.File]::ReadAllLines($file.FullName)
        for ($lineNumber=0; $lineNumber -lt $lines.Length; $lineNumber++) {
            foreach ($match in $commandPattern.Matches($lines[$lineNumber])) {
                $calls.Add([ordered]@{ Family=$family; Model=$file.BaseName; Source=$relative; Line=$lineNumber+1; Command=$match.Groups[1].Value; Method=$match.Groups[2].Value })
            }
        }
    }
}

$modelDb = [IO.File]::ReadAllText((Join-Path $sourceBase 'MegaCrit.Sts2.Core.Models/ModelDb.cs'))
$characterBlock = [regex]::Match($modelDb, 'AllCharacters\s*=>[\s\S]*?\}\);').Value
$characters = @([regex]::Matches($characterBlock, '\bCharacter<([A-Za-z_]\w*)>') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
$actReferences = @([regex]::Matches($modelDb, '\bAct<([A-Za-z_]\w*)>') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -ne 'T' } | Sort-Object -Unique)
$eventEncounterBlock = [regex]::Match($modelDb, 'EventEncounters\s*=>[\s\S]*?\}\)\);').Value
$eventEncounters = @([regex]::Matches($eventEncounterBlock, '\bEncounter<([A-Za-z_]\w*)>') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
$knownModels = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($model in $models) { [void]$knownModels.Add($model.Family + '/' + $model.Model) }
$unresolvedPools = @($poolRefs | Where-Object { -not $knownModels.Contains($_.ModelFamily + '/' + $_.Model) } | ForEach-Object { [pscustomobject]@{ModelFamily=$_.ModelFamily;Model=$_.Model} } | Sort-Object ModelFamily,Model -Unique)
$summary = [ordered]@{
    BaselineGame='v0.111.0-41cef1ea'
    BaselineModCommit='891e5ff'
    GameAssemblySHA256='0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9'
    RegistryCharacters=$characters
    RegistryActSourceReferences=$actReferences
    RegistryEventEncounters=$eventEncounters
    Families=$familyCounts.ToArray()
    ModelSourceFiles=$models.Count
    DirectPoolReferences=$poolRefs.Count
    LexicalCommandReferences=$calls.Count
    ActEncounterReferences=$actEncounters.Count
    EncounterMonsterReferences=$encounterMonsters.Count
    SymbolicOrOutsidePrimaryDirectoryPoolReferences=$unresolvedPools
    RuntimeExecuted=$false
    Notes=@('File counts are not collectible or supported counts.','Pool references and mechanism tags are lexical leads, not reachability or coverage proof.','Source changes require regeneration; concurrent implementation is not read by this index.')
}

[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$output = [ordered]@{ Summary=$summary; Models=$models.ToArray(); PoolReferences=$poolRefs.ToArray(); CommandReferences=$calls.ToArray(); ActEncounterReferences=$actEncounters.ToArray(); EncounterMonsterReferences=$encounterMonsters.ToArray() }
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'native-content-index.json'), ($output | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
$summary | ConvertTo-Json -Depth 5
