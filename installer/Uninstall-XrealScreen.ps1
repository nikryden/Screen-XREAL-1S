<#
.SYNOPSIS
  Removes XrealScreen. Your monitors are restored by the app itself when it exits; run
  "xrs recover" (or start and close XrealScreen once) if a session crashed before uninstalling.
#>
$ErrorActionPreference = 'Stop'
$pkg = Get-AppxPackage -Name '54F07E5F-F9F6-4716-B5CD-54271B62628E' -ErrorAction SilentlyContinue
if ($pkg) {
    Get-Process XrealScreen.App -ErrorAction SilentlyContinue | Stop-Process -Force
    Remove-AppxPackage -Package $pkg.PackageFullName
    Write-Host 'OK XrealScreen removed.'
} else {
    Write-Host 'XrealScreen is not installed.'
}
Write-Host 'Settings remain in %LOCALAPPDATA%\XrealScreen (delete the folder to remove them).'
