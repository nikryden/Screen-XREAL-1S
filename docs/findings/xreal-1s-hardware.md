# XREAL 1S hardware

Source: public reviews/specs (see `docs/legal/SOURCES.md`). None of this is `[verified-hw]` yet.

## Specs
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

## OSD features relevant to us
- Screen modes: **anchor**, **follow**, **smooth follow** (onboard 3DoF).
- **Ultrawide** in OSD "Laboratory": 21:9, 32:9, 16:18. Requires Windows extended display mode.
- Exact ultrawide signal resolution on 1S: unknown. One Pro reportedly 3840×1080 for 32:9. [hypothesis]
- Community advice: disable glasses anchor/stabilizer when the host does head tracking (avoids double correction). → ADR-0003.

## Open questions (M1/M2)
- [ ] Which EDID modes does 1S expose (1920×1080@120? 1920×1200? ultrawide)? → `xrs display list`
- [ ] USB PID observed on this unit (expected `0x043d` or `0x043e`) [from-GPL:facts-only]
- [ ] Firmware version (OSD) — record in STATUS.md and fixtures
- [ ] Does 1S expose the NCM network adapter like One/One Pro? [hypothesis]
