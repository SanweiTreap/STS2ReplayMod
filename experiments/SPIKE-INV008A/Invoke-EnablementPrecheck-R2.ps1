$ErrorActionPreference = 'Stop'
$logsRoot = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs'
if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) { throw 'BLOCKED_INSTALL_PRECHECK: game process is running' }
$latest = Get-ChildItem -LiteralPath $logsRoot -File -Filter 'godot*.log' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'BLOCKED_INSTALL_PRECHECK: no startup log found' }
$text = Get-Content -Raw -LiteralPath $latest.FullName
if (-not $text.Contains('--- RUNNING MODDED! ---', [StringComparison]::Ordinal) -or
    -not $text.Contains('[Sentry.NET] Shutting down', [StringComparison]::Ordinal)) {
    throw 'BLOCKED_INSTALL_PRECHECK: latest log is not a complete normally-ended startup log'
}
if (-not $text.Contains('Skipping loading mod SPIKEINV007A, it is set to disabled in settings', [StringComparison]::Ordinal)) {
    throw 'BLOCKED_INSTALL_PRECHECK: latest complete log does not prove SPIKEINV007A disabled'
}
$matches = [regex]::Matches($text, '^\[INFO\]\s+\d+:\s+.*\((?<id>[^()]+)\)\s*$', [Text.RegularExpressions.RegexOptions]::Multiline)
$ids = @($matches | ForEach-Object { $_.Groups['id'].Value })
if ($ids.Count -ne 1 -or $ids[0] -ne 'SPIKEINV008A' -or
    -not $text.Contains('Loaded 1 mods', [StringComparison]::Ordinal)) {
    throw "BLOCKED_INSTALL_PRECHECK: latest complete loaded set is [$([string]::Join(',', $ids))]"
}
$hash = (Get-FileHash -LiteralPath $latest.FullName -Algorithm SHA256).Hash
Write-Output "PASS game process stopped"
Write-Output "PASS latest complete startup log path=$($latest.FullName) length=$($latest.Length) sha256=$hash"
Write-Output 'PASS SPIKEINV007A persisted disabled'
Write-Output 'PASS actual loaded mod set exactly [SPIKEINV008A]'
