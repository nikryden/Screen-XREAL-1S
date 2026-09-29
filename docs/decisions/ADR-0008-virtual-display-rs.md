# ADR-0008: Virtual display provider — virtual-display-rs behind `IVirtualDisplayProvider`

- **Status:** Accepted (supersedes ADR-0001)
- **Date:** 2026-09-26

## Context
ADR-0001 chose VirtualDrivers VDD for its signing. Researching its source in M2 (`findings/virtual-display-drivers.md`) showed it does not fit our model:
- one global monitor count and one shared mode list; no add/remove of a single monitor, no way to tell our monitors from other apps';
- every count/settings change reloads the driver, and the reload path currently crashes the driver host (upstream issue #351), so all VDD monitors drop and return.

virtual-display-rs (MIT) controls each monitor individually over `\\.\pipe\virtualdisplaydriver` (JSON: Notify/Remove/State), needs no admin and no restart, and its 0.4.0 driver is already installed on the glasses test PC.

## Decision
Use **virtual-display-rs 0.4.0** as the virtual display driver, accessed only through `IVirtualDisplayProvider` (`XrealScreen.Display.VirtualDisplayRs.VirtualDisplayRsProvider`). The user chose this on 2026-09-26. Release distribution and signing are decided in M7.

## Consequences
- + Per-monitor resolutions and refresh rates; add/remove one monitor without disturbing others. [verified-hw] 2026-09-26: 3840×1080, 1920×2160, 2560×1080 @ 60 Hz and 1920×1080 @ 120 Hz created together and removed cleanly.
- + Works without admin: pipe open to local users, no settings file.
- − **Max 16 monitors, IDs 0–15**: the driver uses the ID as the IddCx connector index (out-of-range IDs fail with STATUS_INVALID_PARAMETER, [verified-hw]). Ownership is marked by the monitor **name** prefix `XrealScreen`; we allocate IDs from 15 downward and always merge with the driver's current state.
- − Driver install uses a **self-signed** certificate; the last GitHub release (0.3.1) has an older protocol, so 0.4.0 must be built from source or the installed copy reused. M7 options: attestation-sign our own build (EV cert), or keep the interface and switch provider.
- − Upstream last pushed 2025-03-03; we may need to fork.
- Code: the wire format is re-implemented in C# from the MIT source (attribution in THIRD-PARTY-NOTICES.md).

## Alternatives
- VirtualDrivers VDD (ADR-0001): signed, but global count + crash-reload; kept as a possible fallback provider.
- Own IddCx driver: WDK + signing; still "Later".
- parsec-vdd, Amyuni: licensing (see findings).
