# ADR-0001: Virtual display provider — VirtualDrivers VDD behind `IVirtualDisplayProvider`

- **Status:** Superseded by ADR-0008 (2026-09-26: VDD has one global monitor count and a crashing reload path)
- **Date:** 2026-09-26

## Context
Virtual workspace mode needs N extra Windows monitors that can be captured. Windows only offers this via an Indirect Display Driver (IddCx, UMDF). Writing our own needs the WDK (not installed) and, for release, an EV certificate + Partner Center attestation signing. See `findings/virtual-display-drivers.md`.

## Decision
Use the already-signed MIT driver **VirtualDrivers/Virtual-Display-Driver** (SignPath-signed), accessed only through `IVirtualDisplayProvider` (implementation `VddPipeProvider` in `XrealScreen.Display`). The driver is installed by the bootstrapper, not bundled in the MSIX (ADR-0005).

## Consequences
- + No driver development or signing cost for v1.
- + Supports 640×480..8K, up to 500 Hz, covering 1920×1080, 2560×1080, 3840×1080, 1920×2160.
- − External dependency: pin version + hash; handle a driver shared with other apps (only manage monitors we created; version check).
- − Pipe command set (`MTTVirtualDisplayPipe`) is [hypothesis]; must be read from its source in M2. Settings file `C:\VirtualDisplayDriver\vdd_settings.xml` may need admin to write → helper service (ADR-0004).
- Follow-up: own IddCx driver remains a "Later" milestone behind the same interface.

## Alternatives
- Microsoft IddSampleDriver (MIT, IddCx 1.4) — unsigned sample; we would have to sign it.
- virtual-display-rs (MIT) — self-signed cert install; not suitable for end users.
- parsec-vdd — driver binary owned by Parsec, max 3 displays.
- Amyuni usbmmidd — commercial license.
- Own driver now — needs WDK, EV cert, attestation; too costly for v1.
