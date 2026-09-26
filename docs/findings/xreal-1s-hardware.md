# XREAL 1S hardware

Hardware facts below were measured on our unit on 2026-09-26 (firmware **15.01.03.522**) and are tagged [verified-hw]. Everything else comes from public reviews and specs (see `docs/legal/SOURCES.md`).

## Specs (vendor / reviews)
| Item | Value |
|------|-------|
| Announced / released | 2025-12-01 / 2026-01-04, US$449 |
| Display | Sony micro-OLED, 1920×1200 per eye |
| Refresh | 120 Hz |
| Brightness | 700 nits (perceived) |
| FOV | 52° |
| Weight | 82 g |
| Chip | XREAL X1: onboard 3DoF, 3 ms motion-to-photon, "Real 3D" |
| Video input | USB-C DisplayPort alt mode |

## USB device [verified-hw]
One USB composite device `VID_3318&PID_043E` (friendly name "XREAL 1S"):

| Interface | Windows class / name | Status | Use |
|-----------|---------------------|--------|-----|
| MI_00 | HID "HID-compliant device" | OK | vendor HID, unexplored |
| MI_01 | Net "UsbNcm Host Device" | OK | IMU/control network link (see `xreal-one-protocol.md`) |
| MI_03 | "CDC ECM" | **Error** (no driver) | alternative network function; not needed, NCM works |
| MI_05 | HID "consumer control" | OK | glasses buttons (volume/brightness?) [hypothesis] |
| MI_06 | MEDIA "XREAL 1S" | OK | audio |

Only PID `0x043E` was seen; `0x043D` (from XRLinuxDriver) remains [from-GPL:facts-only].

## Display [verified-hw]
- Windows monitor EDID: manufacturer **`MRG`**, product **`4102`**, name **"XREAL 1S"** (`DISPLAY\MRG4102\...`), output technology DisplayPort. Detected by `GlassesDisplayLocator` / `xrs display list`.
- 2026-09-26, OSD UltraWide presumably 21:9: current mode **2560×1080 @ 60 Hz**; offered modes up to 2560×1080, refresh **60 and 90 Hz only** (no 120 Hz in this mode). Full list: 2560×1080, 2310×990, 2100×990, 1920×1080, 1680×1050, 1920×820, 1280×1024, 1680×720, 1280×720, 1024×768, 800×600, 640×480.
- Glasses and virtual monitors render on the same AMD adapter (virtual monitors report their own IddCx adapter LUID).

## OSD (glasses menu) relevant to us
Source: XREAL One-series user guide and tutorials (apply to "One/1S"); see `docs/legal/SOURCES.md`.
- **Two screen modes:** **Anchor** (screen fixed in mid-air, glasses' own 3DoF) and **Follow** (screen fixed in front of the eyes). **Single-click X** toggles; saved until next use. Long-press X recenters in Anchor.
- **Stabilizer** ("smooth follow"): only in Follow, **on by default**; the glasses compensate small head movements. Double-click X → **Spatial Screen → Stabilizer**; +/− navigate, X toggles.
- **"No anchor" for our virtual workspace mode = Follow + Stabilizer off** (ADR-0003). There is no separate "off / 0DoF" menu item.
- **UltraWide:** Spatial Screen → UltraWide Mode: 16:18, 21:9, 32:9; **Laboratory** sets the default. Requires Windows extended display mode.
- Exact ultrawide signal resolution on 1S: unknown. One Pro reportedly 3840×1080 for 32:9. [hypothesis]

## Open questions (M2)
- [ ] Which display modes does the 1S EDID expose (1920×1080@120? 1920×1200? ultrawide signals)?
- [ ] What each OSD ultrawide mode changes on the Windows side (resolution reported to Windows).
