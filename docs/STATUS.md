# Project Status

> Read this first. Update it at the end of every task (see skill `docs-handoff`).

## Current
- **Milestone:** M1 Protocol spike — **done** (2026-09-26). Next: **M2 Display / VDD spike**.
- **Last updated:** 2026-09-26

## Done
- M0: solution, docs, ADRs, agents, skills, CI, WinUI shell (see ROADMAP).
- M1 on real XREAL 1S (fw 15.01.03.522):
  - USB network link, all ports 52996–52999 open, IMU stream decoded at 1000 Hz with 0 errors.
  - Units (gyro rad/s, accel m/s², timestamps ns) and **sensor frame X right / Y down / Z forward** verified; mapping `+z,-x,-y` now the default.
  - Pitch sign fixed (look up = +); gyro-bias stillness now on a 100 ms low-pass (bias was never learned before).
  - Real fixtures + regression tests (`tests/fixtures/*fw15.01.03.522*`); 29/29 tests green.
  - App tracks the glasses live (Tracking page → source "XREAL One-series glasses", 1000 samples/s).

## Next 3 steps
1. M2: create `src/XrealScreen.Display` — CCD enumeration, find the glasses output by EDID `MRG4102`, read its modes (does the 1S offer 1920×1080@120 / 1920×1200 / ultrawide signals?).
2. M2: install VirtualDrivers VDD on the glasses PC (not installed; Parsec VDA and virtual-display-rs already are — see `docs/findings/local-environment.md`), read its pipe commands from source, implement `VddPipeProvider`.
3. M2: borderless WGC capture of one virtual monitor; topology snapshot/restore.

## Blockers
- None. Glasses PC is `C:\GIT\Screen-XREAL-1S` on "Garage_1" (Developer Mode on).

## Open questions / known gaps
- Control port 52999 not implemented (moved to M4); glasses-side recenter/display mode unknown.
- Yaw drift while worn: bias estimator needs still periods; consider reading the factory gyro bias from the device config (control port) or a start-up "hold still" calibration (M3/M5).
- Workspace settings not persisted (M5); no tray/startup (M6); trimming decided in M7.

## Last hardware verification
- **2026-09-26 · XREAL 1S · PID 0x043E · fw 15.01.03.522** — NCM link, ports, IMU framing/units/axes, live tracking in app. Details: `docs/findings/xreal-one-protocol.md`, `docs/findings/xreal-1s-hardware.md`.

## Session log
| Date | Agent | Summary |
|------|-------|---------|
| 2026-09-26 | Claude | M0: research, plan, solution, app, docs, CI. |
| 2026-09-26 | Claude | M1 hardware test on 1S: protocol verified, axis mapping/pitch/bias fixed, fixtures + tests, docs updated. |
