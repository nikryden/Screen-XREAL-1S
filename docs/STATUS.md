# Project Status

> Read this first. Update it at the end of every task (see skill `docs-handoff`).

## Current
- **Milestone:** M4/M5 — workspace in the WinUI app — **in progress** (2026-09-26).
- **Last updated:** 2026-09-26

## Done
- M0–M3 (see ROADMAP): 1S protocol, virtual monitors, capture, head-tracked rendering verified in the glasses.
- `XrealScreen.Host.WorkspaceEngine`: the whole session in one class; `xrs render` and the app both use it.
- App: Home → **Start workspace / Stop / Recenter**, status + log; Tracking → stabilizer choice (default Strong); settings persisted to `%LOCALAPPDATA%\XrealScreen\settings.json`; closing the app stops the session and restores the desktop.
- Manifest: `graphicsCaptureProgrammatic`, `graphicsCaptureWithoutBorder`.
- Verified via UI automation: start → 3 virtual monitors + desk 120 Hz + glasses 120 Hz; stop → desktop restored.
- 57/57 tests.

## Next 3 steps
1. User test: start/stop the workspace from the app in the glasses.
2. M6: tray icon + start with Windows; app-level hotkeys; crash-safe restore on next start (snapshot file present → offer restore).
3. ADR-0009 follow-up: independent flip / DirectComposition (desk keeps 144 Hz); pose trace logging.

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
| 2026-09-26 | Claude | M4/M5 start: WorkspaceEngine (Host), app Start/Stop workspace, stabilizer UI, settings persistence. |
