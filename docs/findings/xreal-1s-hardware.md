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
- Windows monitor EDID: manufacturer **`MRG`**, product **`4102`**, name **"XREAL 1S"** (`DISPLAY\MRG4102\...`). Use this to find the glasses output (M2, `windows-display-ccd`).

## OSD features relevant to us
- Screen modes: **anchor**, **follow**, **smooth follow** (onboard 3DoF).
- **Ultrawide** in OSD "Laboratory": 21:9, 32:9, 16:18. Requires Windows extended display mode.
- Exact ultrawide signal resolution on 1S: unknown. One Pro reportedly 3840×1080 for 32:9. [hypothesis]
- Community advice: disable glasses anchor/stabilizer when the host does head tracking (avoids double correction). → ADR-0003.

## Open questions (M2)
- [ ] Which display modes does the 1S EDID expose (1920×1080@120? 1920×1200? ultrawide signals)?
- [ ] What each OSD ultrawide mode changes on the Windows side (resolution reported to Windows).
- [ ] Which OSD screen mode name corresponds to "no anchor" on fw 15.01.03.522.
