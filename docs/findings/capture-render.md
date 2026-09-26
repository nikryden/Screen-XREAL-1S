# Capture and rendering

## Capture
| Fact | Tag |
|------|-----|
| Windows.Graphics.Capture (WGC) works on IDD monitors (VertoXR does this) | [hypothesis] until M2 |
| WGC adds ~1 frame latency | [hypothesis] — measure in M3 |
| Yellow capture border removable: `GraphicsCaptureSession.IsBorderRequired = false` + `graphicsCaptureWithoutBorder` capability | documented by Microsoft |
| Programmatic capture without picker: `GraphicsCaptureItem.TryCreateFromDisplayId` / `CreateForMonitor` interop + `graphicsCaptureProgrammatic` capability | documented by Microsoft |
| Alternative: DXGI Desktop Duplication (`IDXGIOutput1.DuplicateOutput`) — lower-level, per output, no border | documented by Microsoft |
| HDR monitors: capture as `R16G16B16A16Float` and tone-map (M8) | documented by Microsoft |

Frames are never written to disk (privacy).

## Render
- Fullscreen D3D11 **flip-model** swapchain (`FLIP_DISCARD`, waitable object) on the glasses output, in a borderless window positioned on that monitor.
- Device created on the **same adapter** as the glasses output and the VDD monitors (no cross-adapter copies).
- Each virtual monitor = textured quad (or curved mesh) rotated by the inverse head pose.
- **Late latch:** read the newest predicted pose immediately before issuing draw calls; predict to photon time.
- Stats: pose→present latency, present-to-present jitter, dropped frames (DXGI frame statistics).

## Libraries
| Library | Version | License |
|---------|---------|---------|
| Vortice.Windows (Direct3D11, DXGI) | 3.8.x (net10) | MIT |
| CommunityToolkit.Mvvm | 8.x | MIT |
| Windows App SDK | latest stable — verify on NuGet | MIT |
| HidSharp / HidApi.Net | optional, Air HID (Later) | Apache-2.0 / MIT (verify) |

Skill: `.claude/skills/d3d11-vortice`.
