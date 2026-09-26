# Project Status

> Read this first. Update it at the end of every task (see skill `docs-handoff`).

## Current
- **Milestone:** M2 Display / virtual monitors — **done** (2026-09-26). Next: **M3 Render spike**.
- **Last updated:** 2026-09-26

## Done
- M0 bootstrap, M1 protocol spike (1S verified on hardware), M2 display/virtual monitors:
  - `XrealScreen.Display`: CCD read, glasses EDID detection (`MRG4102`), layout snapshot/restore, `VirtualDisplayRsProvider` (ADR-0008).
  - 1S display modes per OSD UltraWide setting recorded (120 Hz only with UltraWide Off).
  - `XrealScreen.Render`: `GraphicsDevice` (Vortice D3D11), `MonitorCapture` (borderless WGC) — captures virtual monitors on hardware.
  - CLI: `xrs display list|modes|save|restore`, `xrs vdd state|add|clear`, `xrs capture test`. 47/47 tests green.

## Next 3 steps
1. M3: pick the D3D11 adapter that owns the glasses output; set the glasses to 1920×1200 @ 120 Hz (UltraWide Off) via CCD.
2. M3: borderless fullscreen flip-model swapchain on the glasses monitor (dedicated render thread, waitable object, 120 Hz).
3. M3: draw one captured virtual monitor as a quad driven by `HeadTracker` (late-latched, predicted pose); frame/latency stats; drift check over 5 min.

## Blockers
- None. Glasses PC: `C:\GIT\Screen-XREAL-1S` on "Garage_1".

## Open questions / known gaps
- Virtual-display-rs install/signing for end users: decide in M7 (self-signed today; 0.4.0 not in a GitHub release).
- Control port 52999 not implemented (M4); yaw drift while worn needs better bias (M3/M5).
- Layout restore covers position/resolution only; refresh rate and primary selection come with native mode (M4).
- Workspace settings not persisted (M5); no tray/startup (M6); trimming decided in M7.

## Last hardware verification
- **2026-09-26 · XREAL 1S · PID 0x043E · fw 15.01.03.522** — NCM link, ports, IMU framing/units/axes, live tracking in app. Details: `docs/findings/xreal-one-protocol.md`, `docs/findings/xreal-1s-hardware.md`.

## Session log
| Date | Agent | Summary |
|------|-------|---------|
| 2026-09-26 | Claude | M0: research, plan, solution, app, docs, CI. |
| 2026-09-26 | Claude | M1 hardware test on 1S: protocol verified, axis mapping/pitch/bias fixed, fixtures + tests, docs updated. |
| 2026-09-26 | Claude | M2: Display project, glasses EDID detection, ADR-0008 virtual-display-rs provider verified on hardware. |
| 2026-09-26 | Claude | M2 done: layout snapshot/restore, 1S mode table, WGC capture of virtual monitors. |
