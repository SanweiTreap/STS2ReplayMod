$ErrorActionPreference = 'Stop'
$logs = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs'
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED: game is running' }
$latest = Get-ChildItem -LiteralPath $logs -File -Filter 'godot*.log' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'BLOCKED: no completed verification log' }
$text = Get-Content -Raw -LiteralPath $latest.FullName
if (-not $text.Contains('[Sentry.NET] Shutting down', [StringComparison]::Ordinal)) { throw 'BLOCKED: latest log is not a normally-ended process' }
if (-not $text.Contains('Skipping loading mod SPIKEINV008A, it is set to disabled in settings', [StringComparison]::Ordinal)) {
    throw 'BLOCKED: latest complete log does not prove SPIKEINV008A disabled'
}
$matches = [regex]::Matches($text, '^\[INFO\]\s+\d+:\s+.*\((?<id>[^()]+)\)\s*$', [Text.RegularExpressions.RegexOptions]::Multiline)
$ids = @($matches | ForEach-Object { $_.Groups['id'].Value })
if ($ids -contains 'SPIKEINV008A') { throw 'BLOCKED: retired SPIKEINV008A was loaded' }
"PASS latest normal exit proves SPIKEINV008A disabled; loaded IDs were [$([string]::Join(',', $ids))]"
"INFO log=$($latest.FullName) sha256=$((Get-FileHash -LiteralPath $latest.FullName -Algorithm SHA256).Hash)"
