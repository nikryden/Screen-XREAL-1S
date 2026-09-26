# Roadmap

Risk-first. Each milestone ends with its exit criteria checked and `docs/STATUS.md` updated.

## M0 Bootstrap
Deliverables: solution skeleton, `global.json`, central package management, docs + ADRs + findings, subagents, skills, CLAUDE.md/AGENTS.md, GitHub Actions build+test.
- [x] `dotnet build` and `dotnet test` green locally (CI workflow added; first run on push)
- [x] WinUI 3 shell with Mica + NavigationView launches (2026-09-26)

## M1 Protocol spike (needs glasses)
Deliverables: `xrs` CLI (`probe`, `imu record/decode/live/replay`) - **done**; stream framer - **done**; raw capture `.xrcap` (replaces a separate Sniffer) - **done**; control-port client (recenter etc.) - after the stream is verified; real fixtures.
- [ ] 1S NCM adapter + TCP 52998 confirmed (or fallback documented)
- [ ] IMU rate and units verified
- [ ] Real fixtures committed (`tests/fixtures/*.xrimu`) with firmware version

## M2 Display / VDD spike
Deliverables: CCD enumeration, EDID glasses identification, VDD add/remove, topology snapshot/restore, borderless WGC capture.
- [ ] Virtual monitors at 1920×1080, 2560×1080, 3840×1080, 1920×2160
- [ ] 3 monitors created, captured and removed cleanly; topology restored
- [ ] Known whether the VDD pipe needs admin; pipe commands documented from source

## M3 Render spike
Deliverables: fullscreen swapchain on glasses output, captured monitor on pose-driven quad, latency stats.
- [ ] 120 fps sustained
- [ ] < 20 ms pose→present
- [ ] No visible drift over 5 min

## M4 Native mode MVP
Deliverables: Device page, recenter button + hotkey, correct resolution/refresh, ultrawide guidance/control.
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
