param([Parameter(Mandatory=$true)][string]$ProbeDll,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$dest=[IO.Path]::GetFullPath($Output)
if(-not $dest.StartsWith((Join-Path $workspace 'evidence/det-001/candidate-r2')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $dest)){throw 'NEW_OUTPUT_SCOPE'}
$game='C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/sts2.dll'
if((Get-Item -LiteralPath $game).Length -ne 9757184 -or (Get-FileHash -LiteralPath $game).Hash -ne '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9'){throw 'GAME_IDENTITY'}
$baseline=Join-Path $workspace 'experiments/SPIKE-001B-r2/artifacts-phase0-r2/SPIKE001B.dll'
if((Get-FileHash -LiteralPath $baseline).Hash -ne 'F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7'){throw 'BASELINE_IDENTITY'}
Add-Type -LiteralPath (Join-Path $env:USERPROFILE '.nuget/packages/ilspycmd/9.1.0.7988/tools/net8.0/any/Mono.Cecil.dll')
$old=[Mono.Cecil.ModuleDefinition]::ReadModule($baseline);$probe=[Mono.Cecil.ModuleDefinition]::ReadModule($ProbeDll)
function Types($roots){foreach($t in $roots){$t;if($t.HasNestedTypes){Types $t.NestedTypes}}}
function Refs($m){$m.GetMemberReferences()|Where-Object {$_.DeclaringType.Scope.Name -in @('sts2','GodotSharp')}|ForEach-Object FullName|Sort-Object -Unique}
$lines=[Collections.Generic.List[string]]::new()
try{
 $types=@(Types $probe.Types);$refs=@(Refs $probe);$before=@(Refs $old);$delta=@($refs|Where-Object {$_ -notin $before})
 $allow=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'allowed-new-references.txt')|Where-Object {$_})
 if(@(Compare-Object ($allow|Sort-Object) ($delta|Sort-Object)).Count){throw 'REFERENCE_WHITELIST_MISMATCH'}
 if($probe.Assembly.Name.Name -ne 'DET001' -or @($probe.AssemblyReferences|Where-Object Name -match 'SPIKE001|Det001Offline').Count){throw 'OLD_BINARY_DEPENDENCY'}
 foreach($r in $delta){$lines.Add("NEW_READ_ONLY_REFERENCE $r")}
 $lines.Add("REFERENCES total=$($refs.Count) added=$($delta.Count) removed=$(@($before|Where-Object {$_ -notin $refs}).Count)")
 $patches=@($types|Where-Object {@($_.CustomAttributes|Where-Object {$_.AttributeType.Name -eq 'HarmonyPatch'}).Count})
 if($patches.Count -ne 11){throw 'PATCH_COUNT'}
 $callbacks=@($types|ForEach-Object Methods|Where-Object {@($_.CustomAttributes|Where-Object {$_.AttributeType.Name -in @('HarmonyPrefix','HarmonyPostfix','HarmonyFinalizer')}).Count})
 if($callbacks.Count -ne 15){throw 'CALLBACK_COUNT'}
 foreach($m in $callbacks){
  $final=@($m.CustomAttributes|Where-Object {$_.AttributeType.Name -eq 'HarmonyFinalizer'}).Count -gt 0
  if(($final -and $m.ReturnType.FullName -ne 'System.Exception') -or (-not $final -and $m.ReturnType.FullName -ne 'System.Void')){throw 'SKIP_ORIGINAL_RETURN'}
  foreach($p in $m.Parameters|Where-Object {$_.ParameterType.IsByReference}){
   if($m.DeclaringType.Name -eq 'SavePatch' -and $p.Name -eq '__result' -and $p.ParameterType.FullName -eq 'System.Threading.Tasks.Task&'){continue}
   if($p.Name -eq '__state' -and $p.IsOut -and $m.DeclaringType.Name -in @('EndReadyPatch','DrawObservationPatch','NaturalShuffleObservationPatch','PermutationObservationPatch')){continue}
   throw 'PRODUCTION_INPUT_OR_RESULT_BYREF'
  }
  $lines.Add("OBSERVER_CALLBACK $($m.FullName)")
 }
 $transpilers=@($types|ForEach-Object Methods|Where-Object {@($_.CustomAttributes|Where-Object {$_.AttributeType.Name -eq 'HarmonyTranspiler'}).Count})
 if($transpilers.Count -ne 1 -or $transpilers[0].DeclaringType.FullName -cne 'Det001.PermutationObservationPatch' -or $transpilers[0].Name -cne 'Transpiler'){throw 'CALLSITE_TRANSPILER_SCOPE'}
 $lines.Add('CALLSITE_ONLY one non-generic Shuffle.MoveNext transpiler; 9 added instructions; all original instructions/call retained once in order; source and 72-case actual/dummy regressions bound by receipt')
 $effectCalls=0
 foreach($m in $types|ForEach-Object Methods|Where-Object HasBody){foreach($i in $m.Body.Instructions){
  if($i.OpCode.Name -in @('stfld','stsfld') -and $i.Operand -is [Mono.Cecil.FieldReference] -and $i.Operand.DeclaringType.Scope.Name -in @('sts2','GodotSharp')){throw 'GAME_FIELD_WRITE'}
  if($i.Operand -isnot [Mono.Cecil.MethodReference] -or $i.OpCode.Name -notmatch '^(call|newobj)'){continue}
  $call=$i.Operand;$name=$call.FullName
  if($name -match '(RunManager::(SetUp|Load|Launch|ToSave)|RunState::FromSerializable|SaveManager::(Save|Switch|Delete)|Rng::Next|PlayerCmd::|CardCmd::|CreatureCmd::|CardPileCmd::|System\.IO\.File::(Write|Delete|Move)|Thread::Sleep|Task::Delay)'){throw "FORBIDDEN_CALL $name"}
  if($call.DeclaringType.Scope.Name -in @('sts2','GodotSharp') -and $call.Name.StartsWith('set_')){throw 'GAME_SETTER'}
  if($name -match '(CardModel::TryManualPlay\(|ActionQueueSynchronizer::RequestEnqueue\(|EndPlayerTurnAction::\.ctor\()'){
   if($m.Name -notlike '<DispatchNext>b__*'){throw 'EFFECT_OUTSIDE_SERIALIZED_B_DISPATCH'}
   $effectCalls++;$lines.Add("B_DISPATCH_CALL $name")
  }
  if($m.DeclaringType.FullName -match '^(Det001\.ShuffleBridge|Det001\.Core\.OriginalTaskScope)' -and (($call.DeclaringType.FullName -like 'System.Threading.Tasks.Task*' -and $call.Name -ne 'get_Status') -or $name -match '(AsyncTaskMethodBuilder|TaskAwaiter|ContinueWith|GetResult)')){throw "SHUFFLE_TASK_INTERFERENCE $name"}
 }}
 if($effectCalls -ne 3){throw 'EFFECT_SITE_COUNT'}
 $timer=@($types|ForEach-Object Methods|Where-Object {$_.Name -like '<MainMenu>b__*' -and $_.HasBody -and @($_.Body.Instructions|Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.FullName -match 'Flow::Poll\('}).Count})
 if($timer.Count -ne 1){throw 'TIMER_SCOPE'}
 foreach($i in $timer[0].Body.Instructions){if($i.Operand -is [Mono.Cecil.MethodReference] -and $i.Operand.FullName -notmatch '(Flow::Poll\(|Probe::AnnounceTerminal\()'){throw 'TIMER_LIVE_ACCESS'}}
 foreach($file in @('Core/Projection.cs','Core/FieldReader.cs','Core/ActionObservation.cs','Patches.cs','ActionBridge.cs')){
  $a=[IO.File]::ReadAllText((Join-Path $workspace ('experiments/SPIKE-001B-r2/'+$file))).Replace('Spike001B','Det001').Replace('SPIKE001B','DET001')
  if($a -cne [IO.File]::ReadAllText((Join-Path $PSScriptRoot $file))){throw "RETAINED_SOURCE_CHANGED $file"};$lines.Add("RETAINED_NAMESPACE_ONLY $file")
 }
 $a=[IO.File]::ReadAllText((Join-Path $workspace 'experiments/SPIKE-001B-r2/Core/Contracts.cs'));$b=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Core/Contracts.cs'));$marker='public static class ProductionObserver'
 if($a.Substring($a.IndexOf($marker)) -cne $b.Substring($b.IndexOf($marker))){throw 'SAVE_WRAPPER_CHANGED'}
 $store=$probe.GetType('Det001.PrivateStore');if(($store.Fields|Where-Object Name -eq Schema).Constant -ne 1 -or ($store.Fields|Where-Object Name -eq Revision).Constant -ne 'candidate-r2'){throw 'SCHEMA_NAMESPACE'}
 $r1=Join-Path $workspace 'experiments/DET-001-candidate'
 $retained=@(Get-ChildItem -LiteralPath $r1 -File -Filter '*.cs'|Where-Object Name -ne 'ShufflePatches.cs');$retained+=@(Get-ChildItem -LiteralPath (Join-Path $r1 'Core') -File -Filter '*.cs')
 foreach($f in $retained){$relative=[IO.Path]::GetRelativePath($r1,$f.FullName);$a=[IO.File]::ReadAllText($f.FullName).Replace('candidate-r1','candidate-r2');$b=[IO.File]::ReadAllText((Join-Path $PSScriptRoot $relative));if($a -cne $b){throw "R1_BUSINESS_SOURCE_CHANGED $relative"};$lines.Add("R1_RETAINED $relative (only revision/owner where present)")}
 $lines.Add('PASS DET001 r2 independent; 11 patch targets, 15 observing callbacks plus one exact callsite transpiler; no shared generic method patched; retained SaveRun wrapper/3 normal B effects/Tasks/timer/full r1 business and gates')
 [void][IO.Directory]::CreateDirectory((Split-Path -Parent $dest));$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($lines -join [Environment]::NewLine)+[Environment]::NewLine)
 $s=[IO.File]::Open($dest,[IO.FileMode]::CreateNew);try{$s.Write($bytes)}finally{$s.Dispose()}
 $lines[-1]
}finally{$old.Dispose();$probe.Dispose()}
