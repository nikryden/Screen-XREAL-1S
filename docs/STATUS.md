# Project Status

> Read this first. Update it at the end of every task (see skill `docs-handoff`).

## Current
- **Milestone:** M2 Display / virtual monitors — **in progress** (2026-09-26).
- **Last updated:** 2026-09-26

## Done
- M0 bootstrap, M1 protocol spike (see ROADMAP; 1S verified on hardware).
- M2 so far:
  - `src/XrealScreen.Display`: CCD topology read, EDID glasses detection (1S = `MRG4102`), `xrs display list|modes`.
  - **ADR-0008: virtual-display-rs replaces VirtualDrivers VDD** (user decision; VDD has a global monitor count and a crashing reload).
  - `VirtualDisplayRsProvider` + `xrs vdd state|add|clear`: verified on hardware with 3840×1080, 1920×2160, 2560×1080 @ 60 Hz and 1920×1080 @ 120 Hz at once, clean removal.
  - 44/44 tests green.

## Next 3 steps
1. M2: topology snapshot/restore (`IDisplayTopology` write side, `%LOCALAPPDATA%\XrealScreen	opology-snapshot.json`), restore after `vdd clear` and on exit.
2. M2: borderless Windows.Graphics.Capture of one virtual monitor (`XrealScreen.Render` project start; skill `d3d11-vortice`).
3. M3 prep: virtual workspace mode should drive the glasses at UltraWide Off, 1920×1200 @ 120 Hz (set via CCD) — see mode table in `xreal-1s-hardware.md`.

## Blockers
- None. Glasses PC: `C:\GIT\Screen-XREAL-1S` on "Garage_1".

## Open questions / known gaps
- Virtual-display-rs install/signing for end users: decide in M7 (self-signed today; 0.4.0 not in a GitHub release).
- Control port 52999 not implemented (M4); yaw drift while worn needs better bias (M3/M5).
- Workspace settings not persisted (M5); no tray/startup (M6); trimming decided in M7.

## Last hardware verification
- **2026-09-26 · XREAL 1S · PID 0x043E · fw 15.01.03.522** — NCM link, ports, IMU framing/units/axes, live tracking in app. Details: `docs/findings/xreal-one-protocol.md`, `docs/findings/xreal-1s-hardware.md`.

## Session log
| Date | Agent | Summary |
|------|-------|---------|
| 2026-09-26 | Claude | M0: research, plan, solution, app, docs, CI. |
| 2026-09-26 | Claude | M1 hardware test on 1S: protocol verified, axis mapping/pitch/bias fixed, fixtures + tests, docs updated. |
| 2026-09-26 | Claude | M2: Display project, glasses EDID detection, ADR-0008 virtual-display-rs provider verified on hardware. |
