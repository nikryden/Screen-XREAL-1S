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
- **The glasses change their EDID with the OSD UltraWide setting** (Windows re-enumerates the monitor; same `MRG4102` identity). Measured 2026-09-26, fw 15.01.03.522, via `xrs display modes`:

  | OSD UltraWide | Max signal (Windows mode) | Refresh offered | Other top modes |
  |---|---|---|---|
  | Off | **1920×1200** (native 16:10) | 60 / 90 / **120** Hz | 1920×1080, 1600×1200, 1680×1050 … all at 60/90/120 |
  | 21:9 | **2560×1080** | 60 / 90 Hz | 2310×990, 2100×990, 1920×1080 |
  | 32:9 | **3840×1080** | 60 Hz only | 3520×990, 3200×900, 1920×1080, 2560×720 |
  | 16:18 | **1920×2160** (two 16:9 stacked) | 60 Hz only | 1920×1200, 1920×1080, 1600×1200 |

- Windows remembers a separate layout per EDID variant: e.g. the glasses were primary at (0,0) in 21:9 but secondary to the right of the desk monitor in Off / 16:18 / 32:9. Native mode (M4) must set the layout it wants after each OSD switch.
- Consequences: **120 Hz is only available with UltraWide Off**, so the virtual workspace mode (our own rendering) should run the glasses in Off @ 1920×1200 / 120 Hz. Native ultrawide modes are 60 Hz (32:9, 16:18) or up to 90 Hz (21:9). Our virtual 16:9 base stays 1920×1080; the glasses' own "Off" signal is 1920×1200.
- Glasses and virtual monitors render on the same AMD adapter (virtual monitors report their own IddCx adapter LUID).

## OSD (glasses menu) relevant to us
Source: XREAL One-series user guide and tutorials (apply to "One/1S"); see `docs/legal/SOURCES.md`.
- **Two screen modes:** **Anchor** (screen fixed in mid-air, glasses' own 3DoF) and **Follow** (screen fixed in front of the eyes). **Single-click X** toggles; saved until next use. Long-press X recenters in Anchor.
- **Stabilizer** ("smooth follow"): only in Follow, **on by default**; the glasses compensate small head movements. Double-click X → **Spatial Screen → Stabilizer**; +/− navigate, X toggles.
- **"No anchor" for our virtual workspace mode = Follow + Stabilizer off** (ADR-0003). There is no separate "off / 0DoF" menu item.
- **UltraWide:** Spatial Screen → UltraWide Mode: 16:18, 21:9, 32:9; **Laboratory** sets the default. Requires Windows extended display mode. Resulting Windows signal per setting: see the Display table above.
