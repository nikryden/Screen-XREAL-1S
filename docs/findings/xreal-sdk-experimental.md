# XREAL SDK experimental package (Windows) — facts

Source: `com.xreal.xr.experimental` 3.1.0 (Unity package, local `E:\XREAL SDK\com.xreal.xr.experimental.tar`), read 2026-10-29. **No license** → facts only (ADR-0006): function names, enum values, strings. No code copied, no disassembly, and its DLLs are never shipped or loaded by XrealScreen.

## What is in the package
| Fact | Tag |
|------|-----|
| Unity C# wrappers (`Runtime/*.cs`) over native libraries; Windows x64: `libnr_api.dll` (72 MB), `libnr_glasses_api.dll`, `libnr_loader.dll`, `XREALXRPlugin.dll`, `ov_utils.dll`, plus `.pdb` symbol files; also macOS/iOS/Android builds | [from-sdk:facts-only] |
| Samples: glasses control (display mode, brightness, electrochromic dimming, IPD, temperature/sensors), HelloMR, **RGB camera on Windows standalone** (`XREALRGBCameraTexture`, YUV textures) | [from-sdk:facts-only] |
| Windows-only helpers: finds the glasses monitor by name containing "Air" or "One", or by a 3840×1080 mode; sets it as primary; full-screens a window on it | [from-sdk:facts-only] |

## Glasses functions (C# wrapper, ~150 `Control*`/`Get*`/`Set*` calls)
Function names only. The wire command behind each one is not in the C# and is not known.

| Area | Functions (examples) | Possible use for us |
|------|----------------------|---------------------|
| Versions | `GetGlassesSystemVersion`, `GetGlassesSWVersion`, `GetGlassesDspVersion`, `GetGlassesHWVersion`, `GetGlassesProductName`, `ControlGetDPFwVersion` | `GetGlassesSystemVersion` may be the `15.1.03.603` firmware part we cannot read yet (E0 open item) [hypothesis] |
| Display | `SetDpCurrentEdid` / `GetDpCurrentEdid` (1920×1080 or 3840×1080 at 60/72/90/120 Hz), `SetGlassesUltraWideEnable`, `SetDpInputMode` (mono/stereo), brightness, colour temperature | set UltraWide from the app per workspace type (ADR-0010) |
| Space | `SetGlassesSpaceMode` (Hover = 0, Follow = 1), `SetGlassesSceneMode`, `RecenterGlasses` | set Follow/Anchor and recenter from the app instead of the OSD |
| Sensors | `GetProximityWearingState` (worn or not), `GetTemperatureValue` (SoC + NTC), `ControlSetIMUFrequencyDivider` | pause hand control/workspace when the glasses are taken off |
| Other | electrochromic dimming level, power save/sleep time, key events (`StartGlassesEventsReport`), `RebootGlasses`, storage mode, audio pickup mode | dimming button in the app; glasses buttons as hotkeys |
| Firmware update | `NativeOTA`: firmware parts MCU, DP, audio, **camera**, bin, app; updates over USB | not planned (no update check, user 2026-10-29) |

## Native library (strings in `libnr_api.dll`)
| Fact | Tag |
|------|-----|
| **6DoF on the PC:** the Windows build contains XREAL's visual-inertial SLAM (VIO, bundle adjustment, KLT tracker, loop closure, relocalization) fed by an "sdk-camera-driver" and "sdk-imu-driver" → the Eye grayscale camera + IMU are meant for PC-side 6DoF, not done in the glasses | [from-sdk:facts-only] |
| **Grayscale camera API:** `NRGrayscaleCamera` Create, Start, Stop, Pause, Resume, InitSetAutoExposureType, InitSetExposureTime, InitSetGain, InitSetImageResolution, InitSetPixelFormat; per image: exposure start time (device), exposure time, gain, HMD time, camera id mask. Each Init/Start/Stop is an RPC (`…Req`/`…Rsp`, `HandleRpcRequest/Timeout`). **No frame-rate setter** | [from-sdk:facts-only] |
| → The SDK starts the grayscale camera by RPC; the 60 fps we see with Gesture Control on may be the same "camera started" state, so an RPC start could give 60 fps without Gesture Control (E10) | [hypothesis] |
| **RGB camera API:** `NRRgbCamera` Create, Start, Stop, Get/SetConfig, InitSetCompression, resolution, exposure, gain, pixel format, plug-in state, SN, host time offset — also RPCs | [from-sdk:facts-only] |
| **Hand tracking on the PC:** `NRHandTracking*`, `NRHandState*` (tracked, confidence, gesture type, pinch strength, hand type, joints with poses), `NRConfigSetHandTrackingEnabled`, `PerceptionHandTrackingProvider` | [from-sdk:facts-only] |
| Sensor property messages `MSG_SENSOR_SET_PROPERTY`, `REQ/RESP_SENSOR_GET_PROPERTY` exist | [from-sdk:facts-only] |
| gRPC is linked; the only XREAL gRPC method name found is `/frames.service.Frames/StartStreaming` (same as on port 50051); address strings `169.254.1.10`, `127.0.0.1` | [from-sdk:facts-only] |
| Wire format of the camera RPCs (port, message bytes): **not in the strings**; would need disassembly or watching the traffic of an XREAL program | — |
