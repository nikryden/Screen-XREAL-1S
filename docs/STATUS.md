# Project Status

> Read this first. Update it at the end of every task (see skill `docs-handoff`).

## Current
- **Milestone:** M3 Render spike — **done** (2026-09-26). Next: bring the workspace into the WinUI app (M4/M5).
- **Last updated:** 2026-09-26

## Done
- M0, M1 (1S protocol verified), M2 (display, virtual monitors, capture), **M3 (render in the glasses)** — see ROADMAP.
- Usable today from the CLI: `xrs render --screens 3` (defaults: stabilizer strong, desktop refresh sync on, Manual recenter via Ctrl+Alt+R, stop Ctrl+Alt+Q). Glasses OSD: UltraWide Off, Follow, Stabilizer off.
- Verified with the user in the glasses: 120 fps, calm image (after tracker tuning, One Euro stabilizer, desk 120 Hz sync), no drift over 5 min, mouse and window dragging work on the virtual monitors.
- 57/57 tests.

## Next 3 steps
1. M4/M5: `XrealScreen.Host` engine (wraps what `RenderCommands` does) and drive it from the WinUI app: Screens page (count, ultrawide mode, layout), Tracking page (stabilizer preset, recenter, follow), Start/Stop workspace.
2. Persist settings; tray + global hotkeys owned by the app (M6).
3. Latency/DWM follow-up (ADR-0009): independent flip or DirectComposition so the desk can keep 144 Hz; measure motion-to-photon; pose trace logging for drift analysis.

## Blockers
- None. Glasses PC: `C:\GIT\Screen-XREAL-1S` on "Garage_1".

## Open questions / known gaps
- Virtual-display-rs install/signing for end users: decide in M7 (self-signed today; 0.4.0 not in a GitHub release).
- Control port 52999 not implemented (M4).
- `missed` frame counter in presenter stats is noisy (heuristic vs moving average); p99 frame time is the reliable number.
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
| 2026-09-26 | Claude | M3: renderer in the glasses; user A/B tests → tracker tuning, stabilizer presets, desktop refresh sync (ADR-0009 update). |
| 2026-09-26 | Claude | M3 done: drift test 5 min OK, mouse fixed (desktop order), all verified with the user in the glasses. |
