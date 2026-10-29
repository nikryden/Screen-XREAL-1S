# ADR-0016: XREAL Eye camera feed and hand control

- **Status:** Proposed
- **Date:** 2026-10-29

## Context
The user has the XREAL Eye on the 1S and wants a virtual keyboard clicked by hand tracking, plus the other camera uses suggested on 2026-10-29 (passthrough, lean tracking, drift correction). The glasses stream an 8-bit grayscale 512×378 60 fps feed on TCP 52997 without any vendor software, and MediaPipe's hand model finds the hand in 96–100 % of frames once the image is blurred (`docs/findings/xreal-eye-camera.md`). One camera gives no reliable depth. The glasses have their own Gesture Control (off by default), which would compete with ours. The XREAL SDK camera API is Unity/Android only and has no license (ADR-0006).

## Decision
We read the Eye feed ourselves on port 52997 and run hand tracking in-process with MediaPipe's hand models (Apache-2.0) converted to ONNX and executed by ONNX Runtime (MIT; DirectML when available, CPU otherwise). Hands drive Windows through `SendInput` like head look (ADR-0011): the pinch point moves the pointer and a pinch clicks; a virtual keyboard is clicked the same way.

- **Feed:** `EyeCameraClient` in `Device.XrealOne` (framer shared with the IMU port's shape); `.xrcam` recordings for tests; `xrs camera …`.
- **Pipeline:** 3×3 blur → palm detection (only when no hand is tracked) → 21 landmarks → One Euro filter → gesture state machine (pinch with hysteresis 0.35 / 0.6 and a minimum hold) → `HandControlEngine`. 30 fps processing is enough; every second frame is used.
- **Hand tracking uses the grayscale feed only**, never the colour camera (the colour camera is researched separately for other uses, ROADMAP M9 E9).
- **Click = pinch, not air poke** (no depth with one camera).
- **Mapping camera → screen:** a 5-point calibration per user (point at targets in the glasses), stored in settings; the camera is rigid with the displays.
- **Keyboard:** first the Windows on-screen keyboard (`osk.exe`) clicked by hand control; then our own no-activate overlay keyboard (large keys, hover highlight, Unicode input for å ä ö) on the glasses display.
- **Safety and privacy:** hand control and every camera feature are **off by default**; Ctrl+Alt+J toggles hand control, a visible indicator shows while the camera is read; no frames leave the PC or are stored unless the user records; real mouse/keyboard input pauses hand control; the app tells the user to keep the glasses' own Gesture Control off.
- The camera feed and hand events are exposed in the SDK and local API later, behind the same opt-in.

## Consequences
- + Works on Windows with no XREAL software; one model stack for keyboard, pointer and later gestures.
- + Reuses the head-look input path, settings and hotkey patterns.
- − ONNX conversion and the MediaPipe pre/post-processing (anchors, ROI from landmarks) must be ported to C# with attribution (THIRD-PARTY-NOTICES, SOURCES).
- − Arm fatigue and ~5–10 words per minute: the keyboard is for short input, not a replacement.
- − `SendInput` cannot reach elevated windows (same as head look).
- − The camera is not a Windows camera device: no OS privacy switch or indicator, so ours is mandatory; website privacy policy and Store listing need updating before release.
- Follow-up: metadata port 52996 may carry the glasses' own 6DoF pose (M9 E1), which would decide how lean tracking is built.

## Alternatives
- Colour (RGB) camera for hand tracking — not exposed without unknown RPCs, 30 fps and compressed (more latency) vs 60 fps raw grayscale, model input is 224 px anyway, extra power/heat; grayscale already finds the hand in 96–100 % of frames.
- Glasses' built-in Gesture Control — no keyboard, no API, made for phones, cannot be tuned; it would also double-click with ours.
- XREAL SDK camera API — Unity/Android only, no license.
- Python + MediaPipe sidecar process — easiest prototype (used for the 2026-10-29 test) but ships a Python runtime and is hard to package.
- Air poke / tap on the desk — needs depth; unreliable with one camera.
- OpenCV via Emgu CV — GPL/commercial; OpenCvSharp (Apache-2.0) only if we need more than a blur.
