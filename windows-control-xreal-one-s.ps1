param(
    [ValidateSet("extend", "duplicate", "second-screen-only")]
    [string]$Mode = "second-screen-only",
    [switch]$RevertToInternal
)

if ($RevertToInternal) {
    Start-Process -FilePath "DisplaySwitch.exe" -ArgumentList "/internal" -NoNewWindow -Wait
    Write-Host "Switched display to internal screen."
    exit 0
}

$displaySwitchArg = switch ($Mode) {
    "extend" { "/extend" }
    "duplicate" { "/clone" }
    default { "/external" }
}

Start-Process -FilePath "DisplaySwitch.exe" -ArgumentList $displaySwitchArg -NoNewWindow -Wait
Write-Host "Applied display mode: $Mode"
