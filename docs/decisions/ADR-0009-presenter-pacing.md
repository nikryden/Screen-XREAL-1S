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

## Update 2026-09-26 (glasses A/B tests, user feedback)
- Frozen pose (`--source fixed`, no tracking): **no shake** → glasses, link and our rendering are clean.
- Stabilizer strong vs ultra with the desk at 144 Hz: **no noticeable difference** → remaining shake was not tracking noise.
- Desk monitor at **120 Hz** (same as the glasses): **calmer**. The DWM clock mismatch (144 vs 120) varies the
  frame-to-photon delay frame by frame, which reads as shake during head motion.
- Decision addition: `xrs render` sets the other physical monitors to the glasses refresh for the session
  (`--sync-desktop`, default on) and restores them on exit. Follow-up: remove DWM from the glasses path
  (independent flip / DirectComposition) so the desk can keep its own rate.

## Consequences
- + Exactly one frame per glasses refresh; independent of the primary monitor's rate; focus-independent.
- − DWM composition adds up to one compositor frame of latency on top of scan-out (not measured yet; M3 latency work). Prediction (`--predict-ms`, default 20 ms) compensates.
- − Future: try DirectComposition / composition swapchain or independent flip conditions to cut the DWM frame; re-check on other GPUs and when the glasses are the primary monitor.

## Alternatives
- Waitable object / `Present(1)`: paced at the primary monitor's rate (rejected).
- Exclusive fullscreen: focus-bound, failed (rejected).

## Update 2026-09-27 — late latch and prediction (lag in App head tracking)
- **Late latch** (`GlassesPresenter.LatchLeadMs`, `--latch-lead-ms`): after `WaitForVBlank` sleep on a high-resolution
  waitable timer (`CREATE_WAITABLE_TIMER_HIGH_RESOLUTION`; `Thread.Sleep` was 15.6 ms coarse → 35 fps), spin to
  `LatchLeadMs` before the next vblank, then read the pose and render (~0.2 ms). 1.5 ms keeps 120 fps (p99 8.6 ms; a few
  missed frames per ~5,400); 2.5 ms missed more at 90 Hz.
- **Stabilizer** reads the gyro's measured speed instead of estimating it from its own output (StabilizerOrderTests):
  Strong 0.50 px still shake / 0.34° turn error (was 0.75 px / 0.77°).
- Glasses A/B ([verified-hw] 2026-09-27, 1S fw 15.01.03.522, 1920×1200 @ 120 Hz, axes Full): latch 0 / predict 12 vs
  latch 1.5 / predict 12 vs latch 1.5 / predict 20 → the last lagged least ("a little" lag left).
- **Defaults now: `LatchLeadMs` 1.5, `PredictMs` 20.** Remaining lag: the DWM frame (follow-up above).
