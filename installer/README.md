# XrealScreen installer (test build)

## Install
1. Copy this folder to the PC.
2. Open **PowerShell as administrator** in this folder and run:
   ```powershell
   Set-ExecutionPolicy -Scope Process Bypass
   .\Install-XrealScreen.ps1
   ```
3. Start **XrealScreen** from the Start menu.

The package is self-contained (.NET and Windows App SDK included). It is signed with a **test certificate**; the script trusts it on this PC (Local Machine → Trusted People).

## Requirements
- Windows 11, x64.
- XREAL One-series glasses (tested: XREAL 1S).
- **Virtual display driver** virtual-display-rs 0.4 — not bundled yet. The install script tells you whether it is present.

## Uninstall
```powershell
.\Uninstall-XrealScreen.ps1
```

## Build (developers)
```powershell
.\installer\Build-Package.ps1              # → artifacts\installer\
.\installer\Build-Package.ps1 -Version 0.2.0.0
```
