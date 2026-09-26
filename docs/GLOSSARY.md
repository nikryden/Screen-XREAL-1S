# Glossary

| Term | Meaning |
|------|---------|
| **3DoF** | Three degrees of freedom: head rotation only (yaw, pitch, roll). 6DoF adds position. |
| **Anchor** | Glasses OSD mode that fixes the screen in space using onboard 3DoF. Must be off in virtual workspace mode. |
| **CCD** | Connecting and Configuring Displays API (`QueryDisplayConfig` / `SetDisplayConfig`): Windows display topology and modes. |
| **DP alt mode** | DisplayPort over USB-C; how the glasses receive video. |
| **IDD / IddCx** | Indirect Display Driver / its UMDF class extension: user-mode driver that creates monitors without physical hardware. |
| **NCM** | USB Network Control Model: USB Ethernet adapter the glasses expose for IMU/control TCP traffic. |
| **OSD** | On-screen display menu of the glasses. |
| **Recenter** | Reset yaw (and optionally pitch) so the workspace is in front of the current gaze. |
| **SBS** | Side-by-side stereo: left/right eye images in one frame. |
| **VDD** | Virtual Display Driver (VirtualDrivers/Virtual-Display-Driver), our IDD provider. |
| **WGC** | Windows.Graphics.Capture: API to capture monitors/windows into D3D11 textures. |
| **xrimu / xrcap** | Our recording formats: IMU stream fixture / raw sniffed traffic. |
