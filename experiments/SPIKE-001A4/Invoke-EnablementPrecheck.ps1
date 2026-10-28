$ErrorActionPreference = 'Stop'
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods'
$logs = 'C:\Users\sanwe\AppData\Roaming\SlayTheSpire2\logs'
if (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue) {
    throw 'BLOCKED_ENABLEMENT_PRECHECK: game is running'
}
foreach ($name in @('SPIKE001A4.dll','SPIKE001A4.json')) {
    if (Test-Path -LiteralPath (Join-Path $mods $name)) {
        throw "BLOCKED_ENABLEMENT_PRECHECK: A4 must still be absent: $name"
    }
}
$projectIds = @(Get-ChildItem -LiteralPath $mods -File -Filter '*.json' | ForEach-Object {
    try {
        $manifest = Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json
        if ($manifest.id -match '^SPIKE') { [string]$manifest.id }
    } catch { }
} | Sort-Object -Unique)
$latest = Get-ChildItem -LiteralPath $logs -File -Filter 'godot*.log' |
    Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'BLOCKED_ENABLEMENT_PRECHECK: no completed verification log' }
$text = Get-Content -Raw -LiteralPath $latest.FullName
if (-not $text.Contains('[Sentry.NET] Shutting down', [StringComparison]::Ordinal)) {
    throw 'BLOCKED_ENABLEMENT_PRECHECK: latest log is not a normal exit'
}
$loaded = @([regex]::Matches($text, '^\[INFO\]\s+\d+:\s+.*\((?<id>[^()]+)\)\s*$',
    [Text.RegularExpressions.RegexOptions]::Multiline) | ForEach-Object { $_.Groups['id'].Value })
if ($loaded.Count -ne 0) {
    throw "BLOCKED_ENABLEMENT_PRECHECK: verification launch loaded mods: [$([string]::Join(',', $loaded))]"
}
foreach ($id in $projectIds) {
    if (-not $text.Contains("Skipping loading mod $id, it is set to disabled in settings", [StringComparison]::Ordinal)) {
        throw "BLOCKED_ENABLEMENT_PRECHECK: no persisted-disabled evidence for retired project probe $id"
    }
}
"PASS A4-absent normal-exit verification proves empty loaded set and every installed project probe persisted disabled; IDs=[$([string]::Join(',', $projectIds))]"
"INFO log=$($latest.FullName) length=$($latest.Length) sha256=$((Get-FileHash -Algorithm SHA256 -LiteralPath $latest.FullName).Hash)"
