param(
    [string] $AssemblyPath = 'C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/sts2.dll',
    [string] $CecilPath = 'C:/Users/sanwe/.nuget/packages/ilspycmd/9.1.0.7988/tools/net8.0/any/Mono.Cecil.dll'
)
$ErrorActionPreference = 'Stop'
# Metadata/IL only. Never load sts2.dll for execution or resolve its dependencies.
$expected = '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9'
$identity = Get-Item -LiteralPath $AssemblyPath
$hash = (Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash
if ($identity.Length -ne 9757184 -or $hash -ne $expected) { throw 'Fixed investigation DLL identity mismatch' }
Add-Type -LiteralPath $CecilPath
$module = [Mono.Cecil.ModuleDefinition]::ReadModule($AssemblyPath)
try {
    "AssemblyBytes=$($identity.Length) SHA256=$hash MVID=$($module.Mvid)"
    "CecilSHA256=$((Get-FileHash -LiteralPath $CecilPath -Algorithm SHA256).Hash)"
    $gameMode = $module.Types | Where-Object FullName -eq 'MegaCrit.Sts2.Core.Runs.GameMode'
    foreach ($field in $gameMode.Fields | Where-Object HasConstant) { "GameMode.$($field.Name)=$($field.Constant)" }
    $character = $module.Types | Where-Object FullName -eq 'MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen'
    $custom = $module.Types | Where-Object FullName -eq 'MegaCrit.Sts2.Core.Nodes.Screens.CustomRun.NCustomRunScreen'
    if ($null -eq $character -or $null -eq $custom) { throw 'Expected types absent' }
    $methods = @($character.Methods | Where-Object Name -in @('InitializeSingleplayer', 'SeedChanged'))
    $methods += @($custom.Methods | Where-Object Name -in @('InitializeSingleplayer', 'OnSeedInputSubmitted'))
    $state = @($custom.NestedTypes | Where-Object Name -like '<StartNewSingleplayerRun>d__*')
    if ($state.Count -ne 1) { throw 'Custom start async-state identity ambiguous' }
    $methods += @($state[0].Methods | Where-Object Name -eq 'MoveNext')
    if ($methods.Count -ne 5) { throw 'Expected exactly five narrow methods' }
    foreach ($method in $methods) {
        "METHOD $($method.FullName) TOKEN=0x$('{0:x8}' -f $method.MetadataToken.ToInt32())"
        foreach ($instruction in $method.Body.Instructions) {
            $operand = $instruction.Operand
            if ($null -eq $operand) { $formatted = '' }
            elseif ($operand -is [Mono.Cecil.Cil.Instruction]) { $formatted = 'IL_{0:x4}' -f $operand.Offset }
            elseif ($operand -is [Mono.Cecil.MethodReference] -or $operand -is [Mono.Cecil.FieldReference] -or $operand -is [Mono.Cecil.TypeReference]) { $formatted = $operand.FullName }
            else { $formatted = $operand.ToString() }
            '  IL_{0:x4}: {1,-12} {2}' -f $instruction.Offset, $instruction.OpCode.Name, $formatted
        }
    }
} finally { $module.Dispose() }
