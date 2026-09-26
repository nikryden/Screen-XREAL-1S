# ADR-0005: MSIX + bootstrapper

- **Status:** Accepted
- **Date:** 2026-09-26

## Context
We want clean install/update/uninstall (MSIX), but MSIX cannot install kernel or UMDF drivers. The VDD (ADR-0001) must be installed separately. The helper service (ADR-0004) can be packaged via `desktop6:Service`, which needs the restricted `packagedServices` capability.

## Decision
- App + service ship as a **signed MSIX** (test certificate during development).
- A **bootstrapper** (`installer/`) installs a **pinned, hash-verified** VirtualDrivers VDD release, then installs the MSIX.
- Capabilities: `runFullTrust`, `graphicsCaptureWithoutBorder`, `graphicsCaptureProgrammatic`, `packagedServices`; `StartupTask` for autostart.

## Consequences
- + Clean app lifecycle; OS handles updates and uninstall of app/service.
- − Uninstalling the MSIX leaves the VDD; bootstrapper/uninstaller must offer removal (only if we installed it and no other app uses it).
- − Code signing cost: publisher certificate needed for release.
- VDD is listed in THIRD-PARTY-NOTICES but not bundled in the MSIX.

## Alternatives
- MSI/WiX only — can install drivers but loses MSIX lifecycle and packaged identity (needed for some capture capabilities).
- Unpackaged + winget script — fragile, no identity.

## Update 2026-09-27 — phase 1 implemented (user decision: "step 1 now, driver later")
- `installer/Build-Package.ps1` builds a **self-contained** MSIX (x64; .NET + Windows App SDK inside, no framework package dependency) signed with a self-signed test certificate `CN=Niklas Ryden` (private key in git-ignored `installer/.signing/`).
- `installer/Install-XrealScreen.ps1` (admin): trusts the certificate (LocalMachine\TrustedPeople), **checks** for the virtual-display-rs driver and explains if missing, removes a developer registration, installs the MSIX. `Uninstall-XrealScreen.ps1` removes it.
- The driver is **not bundled yet** (ADR-0008: 0.4.0 has no official release; building it needs Rust + WDK). Phase 2: build and sign virtual-display-rs ourselves and let the bootstrapper install it.
- Trimming stays off (WinUI + reflection risk); ReadyToRun on in Release.

