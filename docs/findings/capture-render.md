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

## Render — verified on the 1S (2026-09-26)
| Fact | Tag |
|------|-----|
| Borderless popup + flip-model swapchain on the glasses is **composed by DWM** (no independent flip), pacing follows the primary monitor clock | [verified-hw] |
| Pacing on the glasses output vblank (`WaitForVBlank`) gives 119.9 fps at 120 Hz, p99 8.6 ms, pose latch→present ~0.2 ms | [verified-hw] |
| Adding/removing virtual monitors or changing another monitor's mode makes Windows re-apply the glasses saved mode (120 → 90 Hz); set the glasses mode last and verify by vblank | [verified-hw] |
| Visible shake while holding still came mainly from the 144 Hz (desk) vs 120 Hz (glasses) DWM mismatch; fixed pose showed no shake; desk at 120 Hz was calmer | [verified-hw] user A/B |
| Head micro-motion while "still" ≈ 3–4 °/s → ~1.3 px/frame; One Euro stabilizer presets reduce it (strong 0.75 px, ultra 0.43 px); user preferred strong | [verified-hw] fixture + user A/B |
| XREAL OSD must be **Follow + Stabilizer off**; Anchor during a test made the image wobble (double correction) | [verified-hw] |
| Mouse: with the glasses area between desk and virtual monitors the pointer was head-locked and clicks were swallowed by the 3D window. Desktop order **[desk][virtual 1..n][glasses]** + hidden pointer over the glasses window fixed it; the captured pointer is world-locked and clicks/drag work | [verified-hw] user test |
| 5-minute test: no visible drift (user). Learned gyro bias varies between runs (up to 0.02 rad/s on one axis in the mouse test) — watch; consider factory bias from the control port | [verified-hw] / [hypothesis] |

## Render (design)
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
