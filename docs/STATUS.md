# Project Status

> Read this first. Update it at the end of every task (see skill `docs-handoff`).

## Current
- **Milestone:** M3 Render spike — **in progress** (2026-09-26). Working in the glasses; tuning done with the user.
- **Last updated:** 2026-09-26

## Done
- M0, M1 (1S protocol verified), M2 (display, virtual monitors, capture) — see ROADMAP.
- M3 so far (all verified in the glasses with the user):
  - `xrs render`: N virtual monitors → WGC capture → curved panels fixed in space on the glasses, 120 fps (vblank-paced, ADR-0009).
  - Tracking tuned from user feedback: Madgwick beta 0.02 + accel gate, One Euro stabilizer (`--stabilize off|balanced|strong|ultra`, default strong).
  - Shake root cause found by A/B: 144 Hz desk vs 120 Hz glasses (DWM clock) → `--sync-desktop` (default on) sets the desk to 120 Hz for the session and restores it.
  - Global hotkeys Ctrl+Alt+R (recenter) / Ctrl+Alt+Q (stop); cleanup always runs (try/finally).
  - 55/55 tests; real-data tuning table via `StabilizerTuningTests` → `stabilizer-tuning.txt`.

## Next 3 steps
1. M3 exit: 5-minute drift test in the glasses (`xrs render --seconds 300`), then mark M3 done.
2. M3/M8: remove DWM from the glasses path (independent flip / DirectComposition) so the desk monitor can keep 144 Hz; measure motion-to-photon.
3. M4/M5: bring the renderer into the WinUI app (Host project, Screens/Tracking pages drive it); persist settings.

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
