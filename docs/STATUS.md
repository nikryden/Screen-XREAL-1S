# Project Status

> Read this first. Update it at the end of every task (see skill `docs-handoff`).

## Current
- **Milestone:** M0 Bootstrap — **done**. M1 Protocol spike — **tooling done, waiting for hardware test**.
- **Last updated:** 2026-09-26

## Done
- M0: research (`docs/findings/`), ADR-0001..0007, docs, role subagents, project skills, solution skeleton, CI workflow, WinUI shell.
- M0: `dotnet build` (Debug + Release, x64) and `dotnet test` green — 26 tests.
- M0: app launch verified [verified-local]: packaged `dotnet run`, window "XrealScreen", Mica + NavigationView, simulated head tracking at ~1000 samples/s moving the FOV box in the layout preview.
- M1 tooling: `xrs` CLI (`devices`, `probe`, `imu record/decode/live/replay/synth`), stream framer ported from one-xr (MIT), raw `.xrcap` capture, synthetic fixture `tests/fixtures/synthetic-yawsweep-2s.xrimu`.
- `xrs probe` without glasses reports "no adapter / ports unreachable" as expected.

## Next 3 steps
1. **User:** run `docs/testing/hardware-test-M1.md` with the XREAL 1S and report results.
2. From the recordings: confirm framing/units/axes, fix `XrealOneImuOptions` axis defaults, commit curated fixtures, tag facts `[verified-hw]` in `docs/findings/xreal-one-protocol.md`.
3. Start M2 (`src/XrealScreen.Display`): CCD enumeration + EDID glasses detection; install VirtualDrivers VDD and verify its pipe commands from source (ADR-0001).

## Blockers
- M1 exit needs the XREAL 1S plugged in (user).

## Open questions / known gaps
- Control port 52999 (recenter, brightness, display mode) not implemented; needs a captured request format first. Recenter is host-side today (`HeadTracker.RequestRecenter`).
- Workspace settings are not persisted yet (M5).
- App has no tray/startup yet (M6); `PublishTrimmed` removed from the template — decide trimming in M7.
- Sensor-to-body axis mapping is a hypothesis (`AxisMap` defaults).

## Last hardware verification
- None. (Record: date / device / firmware / what was verified.)

## Session log
| Date | Agent | Summary |
|------|-------|---------|
| 2026-09-26 | Claude | Research (SDK, protocol, VertoXR clean-room, VDD); plan approved; skills imported (winui-app, csharp-async, csharp-developer, csharp-xunit). |
| 2026-09-26 | Claude (docs subagent) | Docs, ADRs, findings, agents, project skills. |
| 2026-09-26 | Claude | Solution, Core tracking, One-series transport, xrs CLI, WinUI app, tests, CI; docs reconciled with code (Sniffer merged into `imu record`, control commands deferred). |
