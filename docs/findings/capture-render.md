# Capture and rendering

## Capture
| Fact | Tag |
|------|-----|
| WGC works on virtual-display-rs monitors: 3840×1080 frames with real desktop content via `CreateForMonitor` interop, free-threaded pool, D3D11 device (Vortice). Implemented in `src/XrealScreen.Render/Capture/`, test `xrs capture test` | [verified-hw] 2026-09-26 |
| Borderless capture: `GraphicsCaptureAccess.RequestAccessAsync(Borderless)` returned Allowed for the unpackaged CLI; `IsBorderRequired = false` accepted | [verified-local] |
| WGC delivers frames **only when content changes** (static virtual desktop: ~8 fps, busy monitor ~21 fps in a 3 s test). The renderer must keep and reuse the last texture per monitor. | [verified-hw] |
| `Direct3D11CaptureFrame.SystemRelativeTime` is 6–9 ms **ahead** of QPC-now in the callback, so it is probably the DWM target present time, not a capture timestamp. Measure capture latency another way in M3 (e.g. PresentMon, or a timestamp drawn into the captured content). | [hypothesis] |
| WGC adds ~1 frame latency | [hypothesis] — measure in M3 (see SystemRelativeTime note above) |
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
