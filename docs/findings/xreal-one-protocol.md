# XREAL One-series protocol (USB network)

Applies to XREAL One / One Pro per community sources; **1S assumed identical until M1 hardware test** (`docs/testing/hardware-test-M1.md`).

## Transport
| Fact | Value | Source | Verified? |
|------|-------|--------|-----------|
| Data link | USB NCM network adapter (appears in `Get-NetAdapter`) | Skarian/one-xr, SamiMitwalli | [from-source:MIT] — 1S: [hypothesis] |
| Glasses IP | `169.254.2.1` | one-xr, SamiMitwalli | [from-source:MIT] |
| PC IP | `169.254.2.10` | one-xr | [from-source:MIT] |
| TCP 52996 | metadata | one-xr, Grayscale-Feed port list | [from-source:MIT] |
| TCP 52997 | camera (Eye accessory) | Grayscale-Feed port list | facts-only (research license) |
| TCP 52998 | IMU stream, ~1000 Hz gyro + accel | one-xr, SamiMitwalli | [from-source:MIT] — rate: [hypothesis] |
| TCP 52999 | control (request/response) | one-xr | [from-source:MIT] |
| Video | USB-C DP alt mode (normal monitor) | vendor | [hypothesis] until seen |

## Stream framing (port 52998)
Implemented in `src/XrealScreen.Device.XrealOne/OneReportFramer.cs`; ported from one-xr `OneXrReportMessageParser.kt` [from-source:MIT]. All [hypothesis] for 1S until M1.

Header (6 bytes): `magic0` = `0x28` (or `0x27`), `magic1` = `0x36`, then **u32 big-endian** body length = 128.
Body (128 bytes, little-endian):

| Offset | Type | Field |
|--------|------|-------|
| 0x00 | u64 | device id |
| 0x08 | u64 | device timestamp, ns |
| 0x18 | u32 | report type: `0x0B` IMU, `0x04` magnetometer |
| 0x1C | 3x f32 | gyro x,y,z - rad/s (one-xr multiplies by rad->deg) |
| 0x28 | 3x f32 | accel x,y,z - unit [hypothesis: m/s2] |
| 0x34 | 3x f32 | magnetometer x,y,z |
| 0x40 | f32 | temperature C |
| 0x44 | u8 | IMU id |
| 0x45 | 3x u8 | frame id |

Axes: one-xr treats gyro Y as yaw and remaps accel into the gyro frame as (z, y, x). Our defaults: gyro `+x,+y,+z`, accel `+z,+y,+x` (`XrealOneImuOptions`), overridable with `xrs imu decode --gyro-axes/--accel-axes`. [hypothesis] - confirm with the rotation recording.
One-xr also subtracts a factory gyro bias (temperature-interpolated, read from the device config on the control port); we currently rely on our own still-detection bias estimator.

## Control features (one-xr reports) [from-source:MIT], all [hypothesis] for 1S
- `zeroView()` / recalibrate (recenter)
- brightness, dimmer (electrochromic)
- scene / input mode
- key events (see `XREALKeyType` in `xreal-sdk.md`)
- device id / firmware version

## USB IDs
| Model | PIDs (VID `0x3318`) | Source |
|-------|------|--------|
| One Pro | `0x0435`, `0x0436` | XRLinuxDriver [from-GPL:facts-only] |
| One | `0x0437`, `0x0438` | XRLinuxDriver [from-GPL:facts-only] |
| **1S** | `0x043e`, `0x043d` | XRLinuxDriver [from-GPL:facts-only] |
| Air series | HID only (no network) | ar-drivers-rs [from-source:MIT] |

## Related work (not used for code)
- wheaney/XRLinuxDriver — GPL-3.0, facts only.
- MSmithDev/AirAPI_Windows — GPL-3.0, Air HID; facts only.
- ar-drivers-rs — MIT, Air HID; reference for later Air support.

## M1 verification checklist
- [ ] NCM adapter present with 1S; PC gets `169.254.2.10`
- [ ] TCP 52998 connectable; stream rate measured
- [ ] Magic, length endianness, header sizes confirmed on bytes
- [ ] Gyro units (rad/s vs deg/s) and accel units (g vs m/s²) confirmed via still + rotation recordings
- [ ] Control port 52999 request format captured (client not written yet)
- [ ] Firmware version recorded
