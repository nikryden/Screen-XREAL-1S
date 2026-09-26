# VertoXR — clean-room observation

**Method:** listing of installed files and their embedded names/strings only, at `C:\Program Files\verto_xr` (v0.2.10+18, vendor vertoxr.com). **No decompilation, disassembly, or code/asset extraction.** (ADR-0006)

## Observed architecture
| Area | Observation |
|------|-------------|
| UI | Flutter Windows runner + `rust_lib_vertoxr.dll` (flutter_rust_bridge 2.12) |
| Renderer | Separate process `uni_ar_space.exe` (Rust, Bevy/wgpu; D3D11/12; OpenXR optional) |
| Virtual monitors | virtual-display-rs (MolotovCherry, MIT) IddCx 1.2 UMDF driver, controlled via named pipe `\\.\pipe\virtualdisplaydriver`; installed with self-signed cert + nefconc. Not installed on this PC. |
| Capture | `windows-capture` crate (Windows.Graphics.Capture). No DXGI Desktop Duplication strings. |
| Tracking | Fork of ar_drivers; One series via TCP `169.254.2.1:52998/52999`; Air via HID (hidapi / nusb) |
| Fusion | Fusion AHRS / Madgwick; recenter overlay |
| Devices | Air family, One, One Pro, OneS / "XREAL 1S", R1, VITURE (proprietary VITURE SDK license), Rokid, RayNeo |
| Extras | ViGEmBus bundled (installed on this PC as `oem42.inf`) [verified-local] |

## Conclusion
Confirms our architecture (IDD + WGC + TCP IMU + AHRS + fullscreen renderer on glasses) and that 1S is supported through the same network path. It is evidence of feasibility, not a source of code.

## Policy for further analysis
Only black-box observation of **our own device's** traffic (`xrs imu record` raw `.xrcap` capture) plus public OSS documentation.
