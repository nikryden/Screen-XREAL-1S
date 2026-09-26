<#
.SYNOPSIS
  Installs XrealScreen from this folder. Run in PowerShell "as administrator"
  (needed once to trust the test certificate).
#>
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal $id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Host 'Please run this script in PowerShell started with "Run as administrator".' -ForegroundColor Yellow
    exit 1
}

# 1. Trust the signing certificate (test certificate; a public release uses a trusted one)
$cer = Join-Path $here 'XrealScreen.cer'
Import-Certificate -FilePath $cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
Write-Host 'OK certificate trusted'

# 2. Virtual display driver (virtual-display-rs, ADR-0008) - not bundled yet
$pipe = Test-Path '\\.\pipe\virtualdisplaydriver'
$device = Get-PnpDevice -PresentOnly -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains 'Root\VirtualDisplayDriver' }
if ($pipe -or $device) {
    Write-Host 'OK virtual display driver found'
} else {
    Write-Host '! virtual display driver (virtual-display-rs 0.4) NOT found.' -ForegroundColor Yellow
    Write-Host '  XrealScreen installs, but the workspace cannot create screens until the driver is installed.' -ForegroundColor Yellow
    Write-Host '  A bundled driver is planned (see docs/ROADMAP.md, M7).' -ForegroundColor Yellow
}

# 3. Install (or update) the app
$existing = Get-AppxPackage -Name '54F07E5F-F9F6-4716-B5CD-54271B62628E' -ErrorAction SilentlyContinue
if ($existing -and $existing.IsDevelopmentMode) {
    Write-Host "Removing developer registration ($($existing.PackageFullName))"
    Remove-AppxPackage -Package $existing.PackageFullName
}
Add-AppxPackage -Path (Join-Path $here 'XrealScreen.msix') -ForceUpdateFromAnyVersion
Write-Host 'OK XrealScreen installed - find it in the Start menu.' -ForegroundColor Green
