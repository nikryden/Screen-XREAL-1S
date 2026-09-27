<#
.SYNOPSIS
  Clean-PC install test (M7): starts Windows Sandbox with artifacts\installer mapped read-only, installs XrealScreen
  including the bundled driver unattended, and writes a report to artifacts\sandbox\results\report.txt.

.DESCRIPTION
  Build first: installer\driver\Build-Driver.ps1, then installer\Build-Package.ps1.
  Requires the Windows Sandbox feature (Containers-DisposableClientVM). Close the Sandbox window when done;
  everything inside it is discarded.
#>
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$package = Join-Path $repo 'artifacts\installer'
$work = Join-Path $repo 'artifacts\sandbox'
if (-not (Test-Path (Join-Path $package 'XrealScreen.msix'))) { throw 'build the package first (installer\Build-Package.ps1)' }

New-Item -ItemType Directory -Force (Join-Path $work 'results') | Out-Null
Remove-Item (Join-Path $work 'results\*') -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $PSScriptRoot 'Run-InSandbox.ps1') $work -Force

$wsb = Join-Path $work 'XrealScreen-test.wsb'
@"
<Configuration>
  <vGPU>Enable</vGPU>
  <Networking>Disable</Networking>
  <MappedFolders>
    <MappedFolder><HostFolder>$package</HostFolder><SandboxFolder>C:\Package</SandboxFolder><ReadOnly>true</ReadOnly></MappedFolder>
    <MappedFolder><HostFolder>$work</HostFolder><SandboxFolder>C:\Test</SandboxFolder><ReadOnly>false</ReadOnly></MappedFolder>
  </MappedFolders>
  <LogonCommand>
    <Command>powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\Test\Run-InSandbox.ps1</Command>
  </LogonCommand>
</Configuration>
"@ | Set-Content $wsb -Encoding utf8

Start-Process $wsb
Write-Host "Sandbox starting. Report: $work\results\report.txt (written when the in-sandbox test finishes)."
