# Workspace pair only. Does not configure or install a mod.
$ErrorActionPreference='Stop'
$dest=Join-Path $PSScriptRoot 'artifacts-candidate-r1'
if(Test-Path -LiteralPath $dest){throw 'SEALED_PAIR_EXISTS'}
[void][IO.Directory]::CreateDirectory($dest)
foreach($name in @('DET001.dll','DET001.json')){
 $source=Join-Path $PSScriptRoot ('bin/Release/net9.0/'+$name);$target=Join-Path $dest $name
 [IO.File]::Copy($source,$target,$false)
 if((Get-FileHash -LiteralPath $source).Hash -cne (Get-FileHash -LiteralPath $target).Hash){throw 'PAIR_COPY_MISMATCH'}
 (Get-Item -LiteralPath $target).IsReadOnly=$true
 [pscustomobject]@{Path=$target;Bytes=(Get-Item -LiteralPath $target).Length;SHA256=(Get-FileHash -LiteralPath $target).Hash;ReadOnly=$true}
}
