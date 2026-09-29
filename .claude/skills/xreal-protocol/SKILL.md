---
name: xreal-protocol
description: XREAL One/One Pro/1S glasses data protocol over the USB network adapter (IMU, control, metadata TCP ports), framing hypotheses, record/replay workflow with .xrimu fixtures, and verification tagging. Use when working on XrealScreen.Device.*, the xrs CLI (incl. raw .xrcap capture), IMU fixtures or protocol findings.
---

# XREAL protocol

Source of truth: `docs/findings/xreal-one-protocol.md`. Update it; do not duplicate facts here.

## Known transport (One series; 1S = [hypothesis] until M1)
| Item | Value |
|------|-------|
| Link | USB NCM network adapter |
| Glasses / PC IP | `169.254.2.1` / `169.254.2.10` |
| 52996 | metadata |
| 52997 | camera (Eye) |
| 52998 | IMU stream (~1 kHz gyro + accel) |
| 52999 | control (request/response) |
| USB VID | `0x3318`; 1S PIDs `0x043d`/`0x043e` [from-GPL:facts-only] |

## Framing hypotheses (from Skarian/one-xr, MIT)
- Magic value, then **big-endian** length, then header + payload.
- Header lengths 2836 / 2736 observed for stream types; meaning unknown.
- Treat every field layout as `[hypothesis]` until matched against our recordings.

## Parser design
- Pure function over `ReadOnlySpan<byte>` / `SequenceReader<byte>` from `System.IO.Pipelines`: returns messages + bytes consumed; handles partial frames and resync on bad magic.
- Explicit endianness via `BinaryPrimitives.ReadXxxBigEndian` / `LittleEndian`; never `BitConverter`.
- Unknown message types are counted and skipped, not thrown.
- Socket layer separate: `TcpClient` + `PipeReader`, `CancellationToken`, reconnect with backoff, `NoDelay = true`.
- IMU thread writes the latest sample/pose to a lock-free slot; never blocks the renderer.

## Record / replay workflow
```powershell
dotnet run --project tools/XrealScreen.Cli -- probe
dotnet run --project tools/XrealScreen.Cli -- imu record --seconds 30 --out tests/fixtures/1s-still.xrimu
dotnet run --project tools/XrealScreen.Cli -- imu replay tests/fixtures/1s-still.xrimu
dotnet run --project tools/XrealScreen.Cli -- imu live                      # fused pose; 'r' + Enter recenters
dotnet run --project tools/XrealScreen.Cli -- imu decode captures/x.xrcap   # re-decode raw bytes after parser changes
```
- `.xrimu` stores raw bytes with receive timestamps plus a header: device model, PID, firmware version, date, port. Replay feeds the same parser → deterministic tests.
- `xrs imu record` stores the raw 52998 bytes of **our own device** as `.xrcap` (the sniffer); `xrs imu decode` replays them through the current parser.
- Keep fixtures small (< 5 MB). Name: `<model>-<scenario>.xrimu`.

## Verifying units (M1)
- Still recording: gyro mean ≈ bias, |accel| ≈ 1 g → determines accel scale (g vs m/s²).
- Rotation recording: integrate gyro over a known 90° turn → rad/s vs deg/s.
- Record sample rate from timestamps (expect ~1000 Hz).

## Tagging
- Promote a fact to `[verified-hw]` only with a committed recording or pasted output, and note date, device, firmware.
- Firmware updates may change the protocol: always record firmware in fixtures and STATUS.md.

## Clean-room
- MIT sources (one-xr, SamiMitwalli demo, ar-drivers-rs) may be read and referenced; porting code requires attribution in THIRD-PARTY-NOTICES.md.
- XRLinuxDriver, AirAPI_Windows (GPL), XREAL SDK, VertoXR: facts only. Never open their code while writing ours.
- `XREALActionType` codes (e.g. `TRIGGER_RECENTER=34`) are SDK event codes, **not** wire command IDs.
