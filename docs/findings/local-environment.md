# Local development environments

All [verified-local]. Update when the toolchain changes; do not keep history.

## Hardware test PC "Garage_1" (glasses connected) — 2026-09-26
| Item | State |
|------|-------|
| OS | Windows 11 10.0.26200 |
| Repo path | `C:\GIT\Screen-XREAL-1S` |
| .NET SDK | 10.0.401 |
| GPU | AMD Radeon (integrated, `VEN_1002&DEV_1636`) — glasses and virtual monitors share this adapter |
| Developer Mode | On (required for `dotnet run` of the packaged app) |
| git identity | set repo-locally (`nikryden`) |
| Virtual display drivers | **Parsec Virtual Display Adapter** (`ROOT\DISPLAY\0000`, parsecvirtualds 0.3.10), **virtual-display-rs** "Virtual Display" (`ROOT\DISPLAY\0001`, provider "Cherry" 0.4.0.0, self-signed — installed by VertoXR). VirtualDrivers VDD (ADR-0001) **not installed** (`C:\VirtualDisplayDriver` absent). |
| Monitors | MSI MAG271C + XREAL 1S (`MRG4102`) |

## Primary dev PC (no glasses) — 2026-09-26
| Item | State |
|------|-------|
| OS | Windows 11 Pro 10.0.26200 |
| Repo path | `E:\GIT\Screen-XREAL-1S` |
| .NET SDK | 10.0.401 (also 9.x and a 10 preview) |
| Visual Studio | VS 2022 Community + VS 18 Insiders |
| Windows SDK | 10.0.26100, 10.0.22621 |
| WDK | **Not installed** (only needed for own IddCx driver, Later) |
| ViGEmBus | Installed (`oem42.inf`, from VertoXR) |
| Virtual display driver | None installed |
| Developer Mode | On |
