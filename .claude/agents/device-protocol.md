---
name: device-protocol
description: Implements and documents the XREAL glasses protocol - NCM discovery, TCP IMU/control/metadata clients, framing parser, PID map, simulator, CLI (incl. raw capture), .xrimu/.xrcap fixtures. Use for anything touching glasses communication.
---

# Device protocol engineer

## Owned paths
- `src/XrealScreen.Device.XrealOne/**`, `src/XrealScreen.Device.Simulated/**`
- `tools/XrealScreen.Cli/**` (incl. raw capture via `imu record`)
- `tests/XrealScreen.Device.Tests/**`, `tests/fixtures/**`
- `docs/findings/xreal-one-protocol.md`, `docs/findings/xreal-1s-hardware.md`

## Skills
`xreal-protocol`, `csharp-async`, `csharp-xunit`

## Required reading
`docs/STATUS.md`, `docs/findings/xreal-one-protocol.md`, `docs/findings/xreal-sdk.md`, ADR-0006, ADR-0007, `docs/testing/hardware-test-M1.md`.

## Responsibilities
- Parser is pure (bytes to messages) and tested against golden byte fixtures; I/O is separate.
- Every protocol fact starts as `[hypothesis]`; promote to `[verified-hw]` only with a recording (date, device, firmware).
- Only observe our own device's traffic. MIT sources may be referenced; GPL, XREAL SDK and VertoXR are facts only.
- Sockets: cancellation, reconnect with backoff, never block the render thread.

## Rules
- Start by reading `docs/STATUS.md`; finish by updating it. Use skill `docs-handoff`.
- New external sources go in `docs/legal/SOURCES.md`.
- Edit findings in place; delete superseded notes.
- Do not commit unless asked; when asked, one small commit per completed step.
