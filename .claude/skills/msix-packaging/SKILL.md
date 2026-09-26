---
name: msix-packaging
description: MSIX packaging for XrealScreen - Package.appxmanifest capabilities (runFullTrust, graphicsCaptureWithoutBorder, graphicsCaptureProgrammatic, packagedServices), packaged Windows service (desktop6:Service), StartupTask, test-certificate signing, and the bootstrapper needed because MSIX cannot install drivers. Use when editing XrealScreen.Package, the manifest, signing, or the installer.
---

# MSIX packaging

Decision: ADR-0005 (MSIX + bootstrapper). Project: `src/XrealScreen.Package` (or single-project MSIX in `XrealScreen.App`).

## Manifest namespaces
```xml
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:uap5="http://schemas.microsoft.com/appx/manifest/uap/windows10/5"
  xmlns:uap11="http://schemas.microsoft.com/appx/manifest/uap/windows10/11"
  xmlns:desktop="http://schemas.microsoft.com/appx/manifest/desktop/windows10"
  xmlns:desktop6="http://schemas.microsoft.com/appx/manifest/desktop/windows10/6"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap uap5 uap11 desktop desktop6 rescap">
```

## Capabilities
```xml
<Capabilities>
  <rescap:Capability Name="runFullTrust" />
  <uap11:Capability Name="graphicsCaptureWithoutBorder" />
  <uap11:Capability Name="graphicsCaptureProgrammatic" />
  <rescap:Capability Name="packagedServices" />
  <rescap:Capability Name="localSystemServices" /> <!-- only if the service runs as LocalSystem -->
</Capabilities>
```
- `graphicsCaptureWithoutBorder` → `session.IsBorderRequired = false`.
- `graphicsCaptureProgrammatic` → capture without the picker.
- Verify exact namespace prefixes against current Microsoft docs when adding (they are version-gated); `MakeAppx` errors name the wrong prefix.
- Restricted capabilities need justification for Store submission; fine for sideloaded MSIX.

## Packaged service (helper, ADR-0004)
```xml
<Extensions>
  <desktop6:Extension Category="windows.service"
      Executable="XrealScreen.Service\XrealScreen.Service.exe" EntryPoint="Windows.FullTrustApplication">
    <desktop6:Service Name="XrealScreenHelper" StartupType="auto" StartAccount="localSystem" />
  </desktop6:Extension>
</Extensions>
```
- Goes under `<Application><Extensions>` (or package-level, per schema); requires `packagedServices` (+ `localSystemServices` for LocalSystem).
- Installing a package with a service requires admin (the bootstrapper runs elevated anyway).
- Prefer `localService` if LocalSystem is not needed.

## Startup task
```xml
<uap5:Extension Category="windows.startupTask">
  <uap5:StartupTask TaskId="XrealScreenStartup" Enabled="false" DisplayName="XrealScreen" />
</uap5:Extension>
```
- Toggle from Settings via `StartupTask.GetAsync("XrealScreenStartup")` → `RequestEnableAsync()`; respect `DisabledByUser`.
- Launch with an argument/flag to start minimized to tray.

## Signing (development)
```powershell
$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=XrealScreen Dev" `
  -KeyUsage DigitalSignature -FriendlyName "XrealScreen Dev" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
Export-PfxCertificate -Cert $cert -FilePath dev.pfx -Password (Read-Host -AsSecureString)
# Trust on test machine (admin): import dev.cer into LocalMachine\TrustedPeople
signtool sign /fd SHA256 /f dev.pfx /p <pwd> XrealScreen.msix
```
- Manifest `Identity Publisher` must equal the certificate Subject exactly.
- Never commit `.pfx` files or passwords; CI uses a secret.
- Release: real code-signing certificate (OV/EV) or Azure Trusted Signing.

## Drivers: why a bootstrapper
- **MSIX cannot install drivers** (kernel or UMDF IddCx).
- Bootstrapper (`installer/`), elevated:
  1. Check OS ≥ 10.0.22621, architecture.
  2. Detect VDD; if missing or older than pinned version, download the **pinned release**, verify **SHA-256**, install it (signed by SignPath).
  3. Install the MSIX (`Add-AppxPackage` / `PackageManager.AddPackageAsync`) and its dependencies (Windows App SDK runtime if framework-dependent).
  4. Record that we installed the VDD (for uninstall).
- Uninstall: remove MSIX; offer VDD removal only if we installed it and no other app depends on it.

## Build
```powershell
dotnet publish src/XrealScreen.App -c Release -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true
```
- Windows App SDK: `WindowsPackageType=MSIX` (default for packaged); self-contained option `WindowsAppSDKSelfContained=true` avoids runtime install.
- Validate: install on a clean VM, launch, uninstall; check no leftover service or files.
