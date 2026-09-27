<#
.SYNOPSIS
  Installs or removes the bundled virtual display driver (virtual-display-rs 0.4, ADR-0008). Run as administrator.

.DESCRIPTION
  Install: if a virtual-display-rs device (Root\VirtualDisplayDriver) already exists it is left alone unless
  -Update is given. Otherwise the driver certificate is added to LocalMachine Root + TrustedPublisher (after asking;
  -Yes skips the question), a root-enumerated display device is created and the driver installed on it.
  What was installed is recorded in %ProgramData%\XrealScreen\driver-install.json so -Uninstall removes only that.
#>
param(
    [switch]$Uninstall,
    [switch]$Update,
    [switch]$Yes,
    [string]$DriverDir = (Join-Path $PSScriptRoot 'driver')
)
$ErrorActionPreference = 'Stop'
$hardwareId = 'Root\VirtualDisplayDriver'
$stateFile = Join-Path $env:ProgramData 'XrealScreen\driver-install.json'

Add-Type -Namespace XrealScreenSetup -Name Native -MemberDefinition @'
[StructLayout(LayoutKind.Sequential)]
public struct SP_DEVINFO_DATA { public int cbSize; public Guid ClassGuid; public int DevInst; public IntPtr Reserved; }

[DllImport("setupapi.dll", SetLastError = true)]
public static extern IntPtr SetupDiCreateDeviceInfoList(ref Guid classGuid, IntPtr hwndParent);
[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
public static extern bool SetupDiCreateDeviceInfoW(IntPtr set, string deviceName, ref Guid classGuid, string description, IntPtr hwndParent, int flags, ref SP_DEVINFO_DATA data);
[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
public static extern bool SetupDiSetDeviceRegistryPropertyW(IntPtr set, ref SP_DEVINFO_DATA data, int property, byte[] buffer, int size);
[DllImport("setupapi.dll", SetLastError = true)]
public static extern bool SetupDiCallClassInstaller(int installFunction, IntPtr set, ref SP_DEVINFO_DATA data);
[DllImport("setupapi.dll", SetLastError = true)]
public static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
[DllImport("newdev.dll", SetLastError = true, CharSet = CharSet.Unicode)]
public static extern bool UpdateDriverForPlugAndPlayDevicesW(IntPtr hwndParent, string hardwareId, string fullInfPath, int installFlags, out bool rebootRequired);
'@

function Get-VddDevice {
    Get-PnpDevice -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains $hardwareId }
}

# Creates a root-enumerated device node with our hardware ID (what "devcon install" does; SetupAPI docs).
function New-RootDevice {
    $displayClass = [Guid]'4d36e968-e325-11ce-bfc1-08002be10318'
    $set = [XrealScreenSetup.Native]::SetupDiCreateDeviceInfoList([ref]$displayClass, [IntPtr]::Zero)
    if ($set -eq [IntPtr](-1)) { throw "SetupDiCreateDeviceInfoList failed ($([Runtime.InteropServices.Marshal]::GetLastWin32Error()))" }
    try {
        $data = New-Object XrealScreenSetup.Native+SP_DEVINFO_DATA
        $data.cbSize = [Runtime.InteropServices.Marshal]::SizeOf($data)
        # DICD_GENERATE_ID = 1
        if (-not [XrealScreenSetup.Native]::SetupDiCreateDeviceInfoW($set, 'Display', [ref]$displayClass, 'Virtual Display', [IntPtr]::Zero, 1, [ref]$data)) {
            throw "SetupDiCreateDeviceInfo failed ($([Runtime.InteropServices.Marshal]::GetLastWin32Error()))"
        }
        $ids = [Text.Encoding]::Unicode.GetBytes("$hardwareId`0`0")
        # SPDRP_HARDWAREID = 1
        if (-not [XrealScreenSetup.Native]::SetupDiSetDeviceRegistryPropertyW($set, [ref]$data, 1, $ids, $ids.Length)) {
            throw "SetupDiSetDeviceRegistryProperty failed ($([Runtime.InteropServices.Marshal]::GetLastWin32Error()))"
        }
        # DIF_REGISTERDEVICE = 0x19
        if (-not [XrealScreenSetup.Native]::SetupDiCallClassInstaller(0x19, $set, [ref]$data)) {
            throw "SetupDiCallClassInstaller(DIF_REGISTERDEVICE) failed ($([Runtime.InteropServices.Marshal]::GetLastWin32Error()))"
        }
    }
    finally { [void][XrealScreenSetup.Native]::SetupDiDestroyDeviceInfoList($set) }
}

function Add-DriverCertificate([string]$cer) {
    foreach ($store in 'Root', 'TrustedPublisher') {
        Import-Certificate -FilePath $cer -CertStoreLocation "Cert:\LocalMachine\$store" | Out-Null
    }
}

if ($Uninstall) {
    if (-not (Test-Path $stateFile)) {
        Write-Host 'Driver: nothing installed by XrealScreen (left any existing virtual display driver alone).'
        return
    }
    $state = Get-Content $stateFile -Raw | ConvertFrom-Json
    if ($state.CreatedDevice) {
        Get-VddDevice | ForEach-Object { pnputil /remove-device $_.InstanceId | Out-Null }
    }
    if ($state.PublishedInf) { pnputil /delete-driver $state.PublishedInf /uninstall /force | Out-Null }
    foreach ($store in 'Root', 'TrustedPublisher') {
        Get-ChildItem "Cert:\LocalMachine\$store" | Where-Object Thumbprint -eq $state.CertThumbprint | Remove-Item
    }
    Remove-Item $stateFile
    Write-Host 'OK virtual display driver removed (device, driver package, driver certificate).'
    return
}

# Install
$inf = Join-Path $DriverDir 'VirtualDisplayDriver.inf'
$cer = Join-Path $DriverDir 'XrealScreenDriver.cer'
if (-not (Test-Path $inf)) { throw "driver package not found in $DriverDir" }

$existing = Get-VddDevice
if ($existing -and -not $Update) {
    $version = ($existing | Select-Object -First 1 | Get-PnpDeviceProperty -KeyName DEVPKEY_Device_DriverVersion -ErrorAction SilentlyContinue).Data
    Write-Host "OK virtual display driver already installed (version $version) - kept. Use -Update to replace it with the bundled one."
    return
}

$certObj = New-Object Security.Cryptography.X509Certificates.X509Certificate2($cer)
if (-not $Yes) {
    Write-Host ''
    Write-Host 'The virtual display driver is signed with a TEST certificate:' -ForegroundColor Yellow
    Write-Host "  $($certObj.Subject)  (thumbprint $($certObj.Thumbprint))"
    Write-Host '  Windows only installs it if this certificate is trusted as a root and publisher on this PC.'
    Write-Host '  Only continue if you trust the person who built this package.'
    $answer = Read-Host 'Trust it and install the driver? [y/N]'
    if ($answer -notmatch '^(y|yes|j|ja)$') {
        Write-Host '! driver not installed - XrealScreen cannot create virtual screens without it.' -ForegroundColor Yellow
        return
    }
}

Add-DriverCertificate $cer
$created = $false
if (-not $existing) {
    New-RootDevice
    $created = $true
}

$reboot = $false
# INSTALLFLAG_FORCE = 1 (also replaces a newer or differently signed driver when -Update is used)
if (-not [XrealScreenSetup.Native]::UpdateDriverForPlugAndPlayDevicesW([IntPtr]::Zero, $hardwareId, (Resolve-Path $inf).Path, 1, [ref]$reboot)) {
    throw "driver install failed ($([Runtime.InteropServices.Marshal]::GetLastWin32Error()))"
}

# The published oemNN.inf now bound to the device (to remove it on uninstall; pnputil text output is localized).
$published = (Get-CimInstance Win32_PnPSignedDriver | Where-Object HardWareID -eq $hardwareId | Select-Object -First 1).InfName

New-Item -ItemType Directory -Force (Split-Path $stateFile) | Out-Null
[pscustomobject]@{ CreatedDevice = $created; PublishedInf = $published; CertThumbprint = $certObj.Thumbprint; Date = (Get-Date).ToString('s') } |
    ConvertTo-Json | Set-Content $stateFile -Encoding utf8
Write-Host "OK virtual display driver installed ($published)$(if ($reboot) { ' - restart Windows to finish' })" -ForegroundColor Green
