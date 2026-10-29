# Roadmap

Risk-first. Each milestone ends with its exit criteria checked and `docs/STATUS.md` updated.

## M0 Bootstrap
Deliverables: solution skeleton, `global.json`, central package management, docs + ADRs + findings, subagents, skills, CLAUDE.md/AGENTS.md, GitHub Actions build+test.
- [x] `dotnet build` and `dotnet test` green locally (CI workflow added; first run on push)
- [x] WinUI 3 shell with Mica + NavigationView launches (2026-09-26)

## M1 Protocol spike (needs glasses)
Deliverables: `xrs` CLI (`probe`, `imu record/decode/live/replay`) - **done**; stream framer - **done**; raw capture `.xrcap` (replaces a separate Sniffer) - **done**; control-port client (recenter etc.) - moved to M4 (native mode needs it); real fixtures - **done**.
- [x] 1S NCM adapter + TCP 52998 confirmed (2026-09-26, fw 15.01.03.522)
- [x] IMU rate and units verified; sensor axes determined
- [x] Real fixtures committed (`tests/fixtures/*fw15.01.03.522*`) with regression tests

## M2 Display / VDD spike
Deliverables: CCD enumeration - **done**; EDID glasses identification - **done**; virtual monitor add/remove via virtual-display-rs (ADR-0008) - **done**; layout snapshot/restore (positions + resolutions, `%LOCALAPPDATA%\XrealScreen\topology-snapshot.json`) - **done**; borderless WGC capture - **done** (`XrealScreen.Render`, `xrs capture test`).
- [x] Virtual monitors at 1920×1080 (60/120 Hz), 2560×1080, 3840×1080, 1920×2160 (2026-09-26)
- [x] Monitors created, captured and removed cleanly; topology restored (2026-09-26)
- [x] Driver needs no admin at runtime; pipe protocol documented from source and verified

## M3 Render spike — done 2026-09-26
Deliverables: fullscreen swapchain on glasses output, captured monitor on pose-driven quad, latency stats.
- [x] 120 fps sustained (119.9 fps over 5 min, vblank-paced, ADR-0009)
- [x] < 20 ms pose→present (latch→present ≈ 0.2 ms; true motion-to-photon not measured yet)
- [x] No visible drift over 5 min (user, glasses test 2026-09-26)
- [x] Mouse usable on the virtual monitors (desktop order desk → virtual → glasses)

## M4 Native mode MVP — superseded by the glasses-anchor workspace (ADR-0010), done 2026-09-27
- [x] Glasses keep the image still (OSD Anchor); app splits the UltraWide image into 1–3 pixel-exact screens
- [x] Gap + aspect-preserving fill; preview; Apply button; auto-restart on UltraWide change
- [ ] Control port 52999 (glasses-side recenter / UltraWide switching from the app) — optional

## M5 Virtual workspace MVP — mostly done 2026-09-27
- [x] App: Start/Stop/Recenter, screen count, ultrawide, layout, distance (live), stabilizer presets, tracking axes, neck model
- [x] Settings persisted (`%LOCALAPPDATA%\XrealScreen\settings.json`)
- [x] Mouse usable (desktop order desk → virtual → glasses)
- [x] Comfort: Full mode with less lag — late latch, 20 ms prediction, gyro-speed stabilizer, nod rate gate (ADR-0009/0007 updates)
- [ ] Remove the DWM frame from the glasses path (ADR-0009 follow-up)

## M6 Background & service (in progress: crash-safe restore done)
Deliverables: tray, StartupTask, helper service + ACL'd pipe, crash-safe topology restore, sleep/resume + hot-plug state machines.
- [ ] Kill app mid-session → topology restored
- [ ] Sleep/resume and unplug/replug recover without restart

## M7 Packaging (phases 1 + 2 done 2026-09-27)
- [x] App icon + MSIX logos (`tools/branding/Make-Icons.ps1`)
- [x] Signed self-contained MSIX + install/uninstall scripts (`installer/`, ADR-0005 update)
- [x] Phase 2: build + sign virtual-display-rs 0.4 ourselves; installer installs it (`installer/driver/Build-Driver.ps1`, `Install-Driver.ps1`, ADR-0005)
- [x] MSI with FalkForge: unpackaged self-contained app, `xrs driver` installs the driver (`installer\Build-Msi.ps1`, ADR-0013) — install test on a PC pending
- [ ] Trusted signing certificate (or Store) for a public release; driver via EV + attestation signing; license list of the driver's Rust crates
- [ ] Clean-VM install test

## M8 Hardening
Deliverables: HDR tone-map, DPI, hybrid GPU, One/One Pro regression, accessibility, v1.0.
- [ ] Hybrid-GPU laptop works (VDD + glasses on same adapter)
- [ ] Accessibility pass
- [ ] v1.0 tagged

## M9 XREAL Eye camera & hand control (planned 2026-10-29, ADR-0016)
Facts: `docs/findings/xreal-eye-camera.md`. Order: E0 → E1 → E2 → E3 → E4 (user priority: keyboard), then E5–E8. Every camera feature is off by default.

**E0 Camera feed foundation**
Deliverables: `EyeFrameFramer` + `EyeCameraClient` (reconnect like the IMU client) in `Device.XrealOne`; `.xrcam` format (header + 8-bit frames, device timestamps); `xrs camera snapshot | record | live`; tests on synthetic frames (no real room images in the repo); firmware read from the OSD and recorded.
- [ ] `xrs camera record` keeps 60 fps for 5 min with 0 dropped frames
- [ ] Framer tests: resync after garbage, split TCP reads, wrong length

**E1 Metadata port 52996 (glasses' own 6DoF?)**
Deliverables: `xrs probe metadata --record`; recordings with Eye on/off, OSD Spatial Anchor on/off, head moved forward/back 20 cm; finding updated.
- [ ] Content of 52996 described, position present or ruled out

**E2 Hand tracking engine**
Deliverables: `tools/Convert-HandModels.ps1` (MediaPipe hand landmarker → ONNX); `HandTracker` (blur → palm detection → landmarks → One Euro filter) on ONNX Runtime; `PinchDetector` (hysteresis 0.35/0.6 + minimum hold); landmark fixtures; `xrs hands live` (preview window with skeleton).
- [ ] Same hand-found rates as the Python test (≥ 95 % raised/pinch) on a recorded `.xrcam`
- [ ] ≤ 15 ms per frame at 30 fps on the dev PC; latency camera → landmarks measured
- [ ] Pinch detector: ≥ 95 % of pinches, ≤ 1 false click per minute with the hand idle in view

**E3 Hand control (pointer + click)**
Deliverables: `HandControlEngine` (SendInput like head look); 5-point calibration; pinch = click, pinch-hold = drag, pinch-drag vertical = scroll; pauses on real mouse/keyboard input and after 3 s without a hand; Ctrl+Alt+J; app **Hands** page (on/off, calibrate, sensitivity, left/right hand, preview) + tray item; camera-in-use indicator; warning to keep the glasses' Gesture Control off.
- [ ] User can click 10 targets of 40 px on the glasses display, ≤ 1 miss
- [ ] Off = camera socket closed, 0 % CPU

**E4 Virtual keyboard**
Deliverables: phase a — open `osk.exe` on the glasses display from the Hands page; phase b — own no-activate overlay keyboard (large keys, hover highlight, pinch press, key repeat, Shift/symbols, Swedish and English layouts, Unicode input for å ä ö, click sound), Ctrl+Alt+K to show/hide.
- [ ] User types a 20-character sentence with ≤ 2 errors (target ≥ 5 words/min)
- [ ] Focus stays in the target app while typing

**E5 Passthrough peek**
Deliverables: hotkey/tray shows the camera feed as a panel (top-most window on the glasses in Glasses anchor; texture in the scene in App head tracking), optional hand skeleton.
- [ ] Peek opens in < 200 ms and shows the keyboard/desk when looking down

**E6 Camera drift correction (App head tracking)**
Deliverables: visual yaw estimate from feature tracking fused with the gyro (ADR-0007 update); A/B in the glasses.
- [ ] Yaw drift over 10 min reduced vs gyro-only (measured)

**E7 Lean tracking / 6DoF for workspace and games**
Deliverables: position from 52996 if E1 found it, otherwise visual-inertial odometry research; lean-in parallax in App head tracking; opentrack UDP output on the Games page (ADR-0011 update).
- [ ] Lean 10 cm toward a screen → visible parallax; works in one sim via opentrack

**E8 SDK, local API, docs, privacy**
Deliverables: camera frames + hand events in `XrealScreen.Sdk` and the local API (opt-in, ADR-0012 update); docs/sdk pages + sample; website privacy policy and FAQ (EN + SV); Store notes; legal-security-reviewer pass before merge.
- [ ] Privacy policy states what the camera is used for and that nothing leaves the PC

## Later
- Own IddCx driver (WDK + EV cert + attestation signing)
- Air-series HID support
- SBS stereo
- Pin virtual screens next to the real monitor (detect it in the Eye feed)
- Hand gestures beyond pinch (palm menu, swipe between screens)
