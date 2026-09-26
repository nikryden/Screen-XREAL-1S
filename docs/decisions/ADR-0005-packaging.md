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
