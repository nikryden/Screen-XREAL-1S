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

## M4 Native mode MVP
Deliverables: Device page, control-port 52999 client (glasses-side recenter, display mode, brightness - capture the request format first), recenter button + hotkey, correct resolution/refresh, ultrawide guidance/control.
- [ ] Recenter works from app and hotkey
- [ ] Glasses output set to correct mode via CCD

## M5 Virtual workspace MVP
Deliverables: 1–6 screens, presets (arc/grid/stacked), ultrawide Off/21:9/32:9/16:18, curvature/distance, Auto center (manual / follow with dead-zone+smoothing / glasses key), stabilizer-conflict warning, settings persistence.
- [ ] All presets and ultrawide modes render correctly
- [ ] Auto center modes behave as specified
- [ ] Settings survive restart

## M6 Background & service
Deliverables: tray, StartupTask, helper service + ACL'd pipe, crash-safe topology restore, sleep/resume + hot-plug state machines.
- [ ] Kill app mid-session → topology restored
- [ ] Sleep/resume and unplug/replug recover without restart

## M7 Packaging
Deliverables: signed MSIX (test cert first), bootstrapper installing pinned, hash-verified VDD, clean uninstall, THIRD-PARTY-NOTICES.
- [ ] Clean install/uninstall on a fresh VM
- [ ] Notices complete

## M8 Hardening
Deliverables: HDR tone-map, DPI, hybrid GPU, One/One Pro regression, accessibility, v1.0.
- [ ] Hybrid-GPU laptop works (VDD + glasses on same adapter)
- [ ] Accessibility pass
- [ ] v1.0 tagged

## Later
- Own IddCx driver (WDK + EV cert + attestation signing)
- Air-series HID support
- SBS stereo
- 6DoF (Eye camera)
- **Lean tracking (6DoF-lite)**: the 1S measures rotation only; real lean-in parallax needs head position, e.g. from a webcam (face tracking) or the XREAL Eye camera. Today: neck model + Ctrl+Alt+Up/Down distance (user request 2026-09-26).
