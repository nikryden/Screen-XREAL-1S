<#
.SYNOPSIS
  Runs inside Windows Sandbox (see Start-SandboxTest.ps1): unattended install from C:\Package, then checks and
  writes C:\Test\results\report.txt. Checks: certificates, driver device + status, driver pipe, virtual monitor
  creation over the pipe, app package, uninstall.
#>
$ErrorActionPreference = 'Continue'
$report = 'C:\Test\results\report.txt'
function Log([string]$text) { $line = "$(Get-Date -Format HH:mm:ss) $text"; Write-Host $line; Add-Content $report $line }
function Check([string]$name, [bool]$ok, [string]$detail = '') { Log ("{0} {1} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail) }

Log "Windows $([Environment]::OSVersion.Version) - clean sandbox"
Check 'no driver before install' (-not (Get-PnpDevice -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains 'Root\VirtualDisplayDriver' }))

# Install (unattended)
Copy-Item C:\Package C:\Install -Recurse
$out = & C:\Install\Install-XrealScreen.ps1 -Yes *>&1 | Out-String
Log "install output:`n$out"

$driverCert = New-Object Security.Cryptography.X509Certificates.X509Certificate2('C:\Install\driver\XrealScreenDriver.cer')
Check 'driver cert in Root' ([bool](Get-ChildItem Cert:\LocalMachine\Root | Where-Object Thumbprint -eq $driverCert.Thumbprint))
Check 'driver cert in TrustedPublisher' ([bool](Get-ChildItem Cert:\LocalMachine\TrustedPublisher | Where-Object Thumbprint -eq $driverCert.Thumbprint))

$device = Get-PnpDevice -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains 'Root\VirtualDisplayDriver' } | Select-Object -First 1
Check 'driver device exists' ([bool]$device) "$($device.InstanceId)"
if ($device) {
    $problem = (Get-PnpDeviceProperty -InstanceId $device.InstanceId -KeyName DEVPKEY_Device_ProblemCode -ErrorAction SilentlyContinue).Data
    $version = (Get-PnpDeviceProperty -InstanceId $device.InstanceId -KeyName DEVPKEY_Device_DriverVersion -ErrorAction SilentlyContinue).Data
    Check 'driver started' ($device.Status -eq 'OK') "status=$($device.Status) problem=$problem version=$version"
}

Start-Sleep 3
$pipe = Test-Path '\\.\pipe\virtualdisplaydriver'
Check 'driver pipe present' $pipe

if ($pipe) {
    # Ask the driver for one 1920x1080@60 monitor (virtual-display-rs 0.4 IPC, JSON + 0x04 terminator; ADR-0008).
    $before = (Get-CimInstance -Namespace root\wmi -ClassName WmiMonitorBasicDisplayParams -ErrorAction SilentlyContinue | Measure-Object).Count
    try {
        $client = New-Object IO.Pipes.NamedPipeClientStream('.', 'virtualdisplaydriver', [IO.Pipes.PipeDirection]::InOut)
        $client.Connect(3000)
        $json = '{"Notify":[{"id":15,"name":"XrealScreen sandbox test","enabled":true,"modes":[{"width":1920,"height":1080,"refresh_rates":[60]}]}]}'
        $bytes = [Text.Encoding]::UTF8.GetBytes($json) + [byte]4
        $client.Write($bytes, 0, $bytes.Length); $client.Flush(); $client.Dispose()
        Start-Sleep 4
        $after = (Get-CimInstance -Namespace root\wmi -ClassName WmiMonitorBasicDisplayParams -ErrorAction SilentlyContinue | Measure-Object).Count
        Check 'virtual monitor created' ($after -gt $before) "monitors $before -> $after"
        $client = New-Object IO.Pipes.NamedPipeClientStream('.', 'virtualdisplaydriver', [IO.Pipes.PipeDirection]::InOut)
        $client.Connect(3000)
        $bytes = [Text.Encoding]::UTF8.GetBytes('"RemoveAll"') + [byte]4
        $client.Write($bytes, 0, $bytes.Length); $client.Flush(); $client.Dispose()
    }
    catch { Check 'virtual monitor created' $false $_.Exception.Message }
}

$app = Get-AppxPackage -Name '54F07E5F-F9F6-4716-B5CD-54271B62628E'
Check 'app installed' ([bool]$app) "$($app.PackageFullName)"

# Uninstall must remove what we installed
$out = & C:\Install\Uninstall-XrealScreen.ps1 *>&1 | Out-String
Log "uninstall output:`n$out"
Check 'app removed' (-not (Get-AppxPackage -Name '54F07E5F-F9F6-4716-B5CD-54271B62628E'))
Check 'driver device removed' (-not (Get-PnpDevice -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains 'Root\VirtualDisplayDriver' }))
Check 'driver cert removed' (-not (Get-ChildItem Cert:\LocalMachine\Root | Where-Object Thumbprint -eq $driverCert.Thumbprint))
Log 'DONE'
