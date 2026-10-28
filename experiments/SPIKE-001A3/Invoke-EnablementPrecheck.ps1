$ErrorActionPreference = 'Stop'
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$logs = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs'
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) { throw 'BLOCKED_ENABLEMENT_PRECHECK: game is running' }
if (Test-Path -LiteralPath (Join-Path $mods 'SPIKE001A3.dll')) { throw 'BLOCKED_ENABLEMENT_PRECHECK: A3 is already installed' }
if (Test-Path -LiteralPath (Join-Path $mods 'SPIKE001A3.json')) { throw 'BLOCKED_ENABLEMENT_PRECHECK: A3 manifest is already installed' }
$projectIds = @(Get-ChildItem -LiteralPath $mods -File -Filter '*.json' | ForEach-Object {
    try { $manifest = Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json; if ($manifest.id -match '^SPIKE') { [string]$manifest.id } } catch { }
} | Sort-Object -Unique)
$latest = Get-ChildItem -LiteralPath $logs -File -Filter 'godot*.log' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'BLOCKED_ENABLEMENT_PRECHECK: no completed verification log' }
$text = Get-Content -Raw -LiteralPath $latest.FullName
if (-not $text.Contains('[Sentry.NET] Shutting down', [StringComparison]::Ordinal)) { throw 'BLOCKED_ENABLEMENT_PRECHECK: latest log is not a normal exit' }
$loaded = @([regex]::Matches($text, '^\[INFO\]\s+\d+:\s+.*\((?<id>[^()]+)\)\s*$', [Text.RegularExpressions.RegexOptions]::Multiline) |
    ForEach-Object { $_.Groups['id'].Value })
foreach ($id in $projectIds) {
    if ($loaded -contains $id) { throw "BLOCKED_ENABLEMENT_PRECHECK: retired project probe loaded: $id" }
    if (-not $text.Contains("Skipping loading mod $id, it is set to disabled in settings", [StringComparison]::Ordinal)) {
        throw "BLOCKED_ENABLEMENT_PRECHECK: no persisted-disabled evidence for $id"
    }
}
"PASS normal-exit log proves every installed project probe disabled; IDs=[$([string]::Join(',', $projectIds))]"
"INFO loaded IDs=[$([string]::Join(',', $loaded))]"
"INFO log=$($latest.FullName) length=$($latest.Length) sha256=$((Get-FileHash -Algorithm SHA256 -LiteralPath $latest.FullName).Hash)"
