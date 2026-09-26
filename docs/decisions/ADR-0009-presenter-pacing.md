# ADR-0009: Presenter pacing — composed window, paced by the glasses' vblank

- **Status:** Accepted
- **Date:** 2026-09-26

## Context
The workspace is drawn in a borderless popup window covering the glasses monitor with a flip-model swapchain (`GlassesPresenter`). Measured on the glasses PC (1S at 1920×1200 @ 120 Hz, MSI desk monitor @ 144 Hz primary) [verified-hw]:
- The window is **composed by DWM** (no independent flip; frame statistics unavailable) even with/without topmost/toolwindow styles and when foreground.
- Pacing with `Present(1)` or the frame-latency waitable object follows DWM's clock = the **primary monitor (144 fps)**, not the glasses (120 Hz) → judder.
- Exclusive fullscreen (`SetFullscreenState`) failed with `DXGI_ERROR_INVALID_CALL` and would drop out whenever focus moves to another monitor — unusable while the user works on the virtual monitors.
- `IDXGIOutput.WaitForVBlank()` on the glasses output + `Present(0)` → **119.7 fps, p99 frame 9 ms, 0 missed**, pose latch→present 0.26 ms.
- Also found: adding/removing a virtual monitor makes Windows re-apply the glasses' saved mode (120 Hz → 90 Hz); the glasses mode must be (re)set after topology changes and verified by vblank measurement.

## Decision
Keep a DWM-composed borderless window and pace the render thread on the **glasses output's vblank** (`WaitForVBlank`), late-latch the pose right after it, render, `Present(0)`; device max frame latency 1. Set the glasses refresh after all topology changes and verify with `OutputTiming.MeasureVblankHz`.

## Consequences
- + Exactly one frame per glasses refresh; independent of the primary monitor's rate; focus-independent.
- − DWM composition adds up to one compositor frame of latency on top of scan-out (not measured yet; M3 latency work). Prediction (`--predict-ms`, default 12 ms) compensates.
- − Future: try DirectComposition / composition swapchain or independent flip conditions to cut the DWM frame; re-check on other GPUs and when the glasses are the primary monitor.

## Alternatives
- Waitable object / `Present(1)`: paced at the primary monitor's rate (rejected).
- Exclusive fullscreen: focus-bound, failed (rejected).
