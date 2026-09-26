<#
.SYNOPSIS
Switches Windows display modes for XREAL One S glasses.
.PARAMETER Mode
Display mode to apply: extend, duplicate, or second-screen-only.
.PARAMETER RevertToInternal
Switch back to the internal display.
#>
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
    "second-screen-only" { "/external" }
    default { throw "Unsupported mode: $Mode" }
}

Start-Process -FilePath "DisplaySwitch.exe" -ArgumentList $displaySwitchArg -NoNewWindow -Wait
Write-Host "Applied display mode: $Mode"
