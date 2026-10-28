[CmdletBinding()]
param(
    [Parameter()]
    [string] $AssemblyPath = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll',

    [Parameter()]
    [string] $OutputRoot = 'C:\Projects\STS2ReplayMod-local\decompiled'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ExpectedGameVersion = 'v0.111.0'
$ExpectedCommit = '41cef1ea'
$ExpectedLength = [long]9757184
$ExpectedSha256 = '0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9'
$CacheKey = 'v0.111.0-41cef1ea-0861BFA1'
$DecompilerPackage = 'ilspycmd'
$DecompilerVersion = '9.1.0.7988'
$GameInstallRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
$ToolManifestPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\.config\dotnet-tools.json'))
$WorkspaceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))

function Get-NormalizedPath {
    param([Parameter(Mandatory)][string] $Path)

    return [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Test-PathWithin {
    param(
        [Parameter(Mandatory)][string] $Candidate,
        [Parameter(Mandatory)][string] $Root,
        [switch] $AllowEqual
    )

    $candidatePath = Get-NormalizedPath $Candidate
    $rootPath = Get-NormalizedPath $Root
    if ($AllowEqual -and $candidatePath.Equals($rootPath, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $prefix = $rootPath + [IO.Path]::DirectorySeparatorChar
    return $candidatePath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-CachePathSafety {
    param(
        [Parameter(Mandatory)][string] $EffectiveOutputRoot,
        [Parameter(Mandatory)][string] $FinalDirectory,
        [string] $TemporaryDirectory
    )

    if (-not (Test-PathWithin -Candidate $FinalDirectory -Root $EffectiveOutputRoot)) {
        throw "Refusing unsafe cache target outside output root: $FinalDirectory"
    }

    $finalParent = Get-NormalizedPath ([IO.Path]::GetDirectoryName($FinalDirectory))
    if (-not $finalParent.Equals((Get-NormalizedPath $EffectiveOutputRoot), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Final cache directory must be a direct child of the output root: $FinalDirectory"
    }

    if (Test-PathWithin -Candidate $FinalDirectory -Root $WorkspaceRoot -AllowEqual) {
        throw "Refusing to place decompiled output inside the workspace: $FinalDirectory"
    }

    if (Test-PathWithin -Candidate $FinalDirectory -Root $GameInstallRoot -AllowEqual) {
        throw "Refusing to modify the game installation directory: $FinalDirectory"
    }

    if ($TemporaryDirectory) {
        if (-not (Test-PathWithin -Candidate $TemporaryDirectory -Root $EffectiveOutputRoot)) {
            throw "Refusing unsafe temporary target outside output root: $TemporaryDirectory"
        }

        $temporaryParent = Get-NormalizedPath ([IO.Path]::GetDirectoryName($TemporaryDirectory))
        if (-not $temporaryParent.Equals((Get-NormalizedPath $EffectiveOutputRoot), [StringComparison]::OrdinalIgnoreCase)) {
            throw "Temporary cache directory must be a direct child of the output root: $TemporaryDirectory"
        }
    }
}

function Get-OutputInventory {
    param([Parameter(Mandatory)][string] $SourceDirectory)

    $files = @(Get-ChildItem -LiteralPath $SourceDirectory -File -Recurse)
    $directories = @(Get-ChildItem -LiteralPath $SourceDirectory -Directory -Recurse)
    $totalBytes = if ($files.Count -eq 0) { 0L } else { [long](($files | Measure-Object -Property Length -Sum).Sum) }

    return [ordered]@{
        fileCount = $files.Count
        csharpFileCount = @($files | Where-Object Extension -EQ '.cs').Count
        directoryCount = $directories.Count
        totalBytes = $totalBytes
    }
}

function Assert-ManifestAndCache {
    param(
        [Parameter(Mandatory)][string] $CacheDirectory,
        [Parameter(Mandatory)][string] $ExpectedAssemblyPath
    )

    $manifestPath = Join-Path $CacheDirectory 'manifest.json'
    $sourceDirectory = Join-Path $CacheDirectory 'src'
    $readmePath = Join-Path $CacheDirectory 'README.txt'

    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Existing cache is incomplete: manifest not found at $manifestPath"
    }
    if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
        throw "Existing cache is incomplete: src directory not found at $sourceDirectory"
    }
    if (-not (Test-Path -LiteralPath $readmePath -PathType Leaf)) {
        throw "Existing cache is incomplete: README not found at $readmePath"
    }

    try {
        $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Existing cache manifest is not valid JSON: $($_.Exception.Message)"
    }

    $checks = [ordered]@{
        schemaVersion = [int]$manifest.schemaVersion -eq 1
        cacheKey = [string]$manifest.cacheKey -ceq $CacheKey
        nonAuthoritative = ($manifest.nonAuthoritative -is [bool]) -and ($manifest.nonAuthoritative -eq $true)
        gameVersion = [string]$manifest.game.version -ceq $ExpectedGameVersion
        gameCommit = [string]$manifest.game.commit -ceq $ExpectedCommit
        sourcePath = (Get-NormalizedPath ([string]$manifest.sourceAssembly.absolutePath)).Equals((Get-NormalizedPath $ExpectedAssemblyPath), [StringComparison]::OrdinalIgnoreCase)
        sourceLength = [long]$manifest.sourceAssembly.length -eq $ExpectedLength
        sourceSha256 = [string]$manifest.sourceAssembly.sha256 -ceq $ExpectedSha256
        decompilerPackage = [string]$manifest.decompiler.package -ceq $DecompilerPackage
        decompilerVersion = [string]$manifest.decompiler.version -ceq $DecompilerVersion
    }

    foreach ($check in $checks.GetEnumerator()) {
        if (-not $check.Value) {
            throw "Existing cache manifest failed critical identity check: $($check.Key)"
        }
    }

    $generatedUtc = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse([string]$manifest.generatedUtc, [ref]$generatedUtc)) {
        throw 'Existing cache manifest has an invalid generatedUtc value.'
    }

    if (@($manifest.decompiler.arguments).Count -lt 4 -or @($manifest.decompiler.generationArguments).Count -lt 4) {
        throw 'Existing cache manifest does not contain the complete decompiler arguments.'
    }

    $inventory = Get-OutputInventory -SourceDirectory $sourceDirectory
    if ($inventory.fileCount -le 0 -or $inventory.csharpFileCount -le 0 -or $inventory.totalBytes -le 0) {
        throw 'Existing cache src inventory is empty or unreadable.'
    }
    if ([int]$manifest.outputInventory.fileCount -ne $inventory.fileCount -or
        [int]$manifest.outputInventory.csharpFileCount -ne $inventory.csharpFileCount -or
        [int]$manifest.outputInventory.directoryCount -ne $inventory.directoryCount -or
        [long]$manifest.outputInventory.totalBytes -ne $inventory.totalBytes) {
        throw 'Existing cache inventory no longer matches manifest.json; refusing to treat it as idempotently valid.'
    }

    $readme = Get-Content -Raw -LiteralPath $readmePath
    if ($readme -notmatch '仅导航缓存' -or $readme -notmatch '不作为方法体级验收证据') {
        throw 'Existing cache README is missing the required non-authoritative warning.'
    }

    return $manifest
}

# Identity checks intentionally happen before any cache directory creation, tool restore,
# or existing-cache handling. A wrong assembly can never reuse or generate this cache key.
$assemblyFullPath = Get-NormalizedPath $AssemblyPath
if (-not (Test-Path -LiteralPath $assemblyFullPath -PathType Leaf)) {
    throw "Assembly does not exist: $assemblyFullPath"
}

$assemblyItem = Get-Item -LiteralPath $assemblyFullPath
if ([long]$assemblyItem.Length -ne $ExpectedLength) {
    throw "Assembly identity mismatch: expected length $ExpectedLength bytes, found $($assemblyItem.Length). No cache was generated."
}

$assemblySha256 = (Get-FileHash -LiteralPath $assemblyFullPath -Algorithm SHA256).Hash.ToUpperInvariant()
if ($assemblySha256 -cne $ExpectedSha256) {
    throw "Assembly identity mismatch: expected SHA-256 $ExpectedSha256, found $assemblySha256. No cache was generated."
}

$effectiveOutputRoot = Get-NormalizedPath $OutputRoot
$finalDirectory = Get-NormalizedPath (Join-Path $effectiveOutputRoot $CacheKey)
Assert-CachePathSafety -EffectiveOutputRoot $effectiveOutputRoot -FinalDirectory $finalDirectory

if (Test-Path -LiteralPath $finalDirectory) {
    if (-not (Test-Path -LiteralPath $finalDirectory -PathType Container)) {
        throw "Cache target exists but is not a directory: $finalDirectory"
    }

    $null = Assert-ManifestAndCache -CacheDirectory $finalDirectory -ExpectedAssemblyPath $assemblyFullPath
    Write-Output "CACHE_VALID $finalDirectory"
    return
}

if (-not (Test-Path -LiteralPath $ToolManifestPath -PathType Leaf)) {
    throw "Pinned .NET tool manifest not found: $ToolManifestPath"
}

$toolManifest = Get-Content -Raw -LiteralPath $ToolManifestPath | ConvertFrom-Json -ErrorAction Stop
if ([string]$toolManifest.tools.ilspycmd.version -cne $DecompilerVersion -or
    $toolManifest.tools.ilspycmd.rollForward -isnot [bool] -or
    $toolManifest.tools.ilspycmd.rollForward -ne $false) {
    throw "Tool manifest must pin ilspycmd exactly to $DecompilerVersion with rollForward=false."
}

if (-not (Test-Path -LiteralPath $effectiveOutputRoot)) {
    New-Item -ItemType Directory -Path $effectiveOutputRoot -ErrorAction Stop | Out-Null
}
elseif (-not (Test-Path -LiteralPath $effectiveOutputRoot -PathType Container)) {
    throw "Output root exists but is not a directory: $effectiveOutputRoot"
}

$temporaryDirectory = Get-NormalizedPath (Join-Path $effectiveOutputRoot ('.' + $CacheKey + '.tmp-' + [Guid]::NewGuid().ToString('N')))
Assert-CachePathSafety -EffectiveOutputRoot $effectiveOutputRoot -FinalDirectory $finalDirectory -TemporaryDirectory $temporaryDirectory

$temporarySourceDirectory = Join-Path $temporaryDirectory 'src'
$temporaryManifestPath = Join-Path $temporaryDirectory 'manifest.json'
$temporaryReadmePath = Join-Path $temporaryDirectory 'README.txt'
$manifestWasWritten = $false

try {
    New-Item -ItemType Directory -Path $temporarySourceDirectory -ErrorAction Stop | Out-Null

    & dotnet tool restore --tool-manifest $ToolManifestPath --disable-parallel
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }

    Push-Location -LiteralPath $WorkspaceRoot
    try {
        $versionOutput = (& dotnet tool run ilspycmd -- --version 2>&1 | Out-String).Trim()
        $versionExitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    if ($versionExitCode -ne 0) {
        throw "Pinned ilspycmd version check failed with exit code $versionExitCode."
    }
    if ($versionOutput -notmatch [regex]::Escape($DecompilerVersion)) {
        throw "Pinned ilspycmd reported an unexpected version: $versionOutput"
    }

    $generationArguments = @('--project', '--outputdir', $temporarySourceDirectory, $assemblyFullPath)
    $dotnetArguments = @('tool', 'run', 'ilspycmd', '--') + $generationArguments
    Push-Location -LiteralPath $WorkspaceRoot
    try {
        & dotnet @dotnetArguments
        $decompilerExitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    if ($decompilerExitCode -ne 0) {
        throw "ilspycmd failed with exit code $decompilerExitCode."
    }

    $inventory = Get-OutputInventory -SourceDirectory $temporarySourceDirectory
    if ($inventory.fileCount -le 0 -or $inventory.csharpFileCount -le 0 -or $inventory.totalBytes -le 0) {
        throw 'ilspycmd produced an empty or unreadable source tree.'
    }

    @'
STS2 LOCAL DECOMPILATION NAVIGATION CACHE — NON-AUTHORITATIVE

本目录仅导航缓存（navigation cache / non-authoritative）。所有技术结论，尤其方法体级结论，
必须回到 manifest.json 固定的原始 DLL 与可复现 IL 复核；本缓存不作为方法体级验收证据。

仅限本地、非分发、最小范围使用。不得公开分发本目录内容，不得把大段反编译源码复制进
模组源码、项目 evidence 或 references。不要修改、编译或执行本目录下的反编译源码。
'@ | Set-Content -LiteralPath $temporaryReadmePath -Encoding utf8NoBOM

    $logicalArguments = @('--project', '--outputdir', (Join-Path $finalDirectory 'src'), $assemblyFullPath)
    $manifest = [ordered]@{
        schemaVersion = 1
        cacheKey = $CacheKey
        nonAuthoritative = $true
        game = [ordered]@{
            version = $ExpectedGameVersion
            commit = $ExpectedCommit
        }
        sourceAssembly = [ordered]@{
            absolutePath = $assemblyFullPath
            length = $ExpectedLength
            sha256 = $ExpectedSha256
        }
        decompiler = [ordered]@{
            package = $DecompilerPackage
            version = $DecompilerVersion
            command = 'dotnet tool run ilspycmd'
            arguments = $logicalArguments
            generationArguments = $generationArguments
            reportedVersion = $versionOutput
        }
        generatedUtc = [DateTimeOffset]::UtcNow.ToString('o')
        outputInventory = $inventory
    }
    $manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $temporaryManifestPath -Encoding utf8NoBOM
    $manifestWasWritten = $true

    $null = Assert-ManifestAndCache -CacheDirectory $temporaryDirectory -ExpectedAssemblyPath $assemblyFullPath
    if (Test-Path -LiteralPath $finalDirectory) {
        throw "Final cache target appeared during generation; refusing to overwrite it: $finalDirectory"
    }

    Move-Item -LiteralPath $temporaryDirectory -Destination $finalDirectory -ErrorAction Stop
    $manifestWasWritten = $false
    Write-Output "CACHE_CREATED $finalDirectory"
}
catch {
    if ($manifestWasWritten -and (Test-Path -LiteralPath $temporaryManifestPath -PathType Leaf)) {
        $failedManifestPath = Join-Path $temporaryDirectory 'manifest.failed.json'
        if (-not (Test-Path -LiteralPath $failedManifestPath)) {
            Move-Item -LiteralPath $temporaryManifestPath -Destination $failedManifestPath -ErrorAction SilentlyContinue
        }
    }
    throw "Decompilation cache generation failed. Incomplete artifacts, if any, remain only under $temporaryDirectory and do not contain manifest.json. $($_.Exception.Message)"
}
