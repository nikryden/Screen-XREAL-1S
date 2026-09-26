# Virtual display drivers (IddCx)

Virtual monitors on Windows require an Indirect Display Driver (UMDF, IddCx). Decision: ADR-0001.

## Chosen: VirtualDrivers/Virtual-Display-Driver
| Fact | Value | Tag |
|------|-------|-----|
| Repo | https://github.com/VirtualDrivers/Virtual-Display-Driver | |
| License | MIT | [from-source:MIT] |
| Signing | SignPath.io (SignPath Foundation certificate) | [from-source:MIT] |
| Modes | 640×480 .. 8K, up to 500 Hz, float refresh rates | [from-source:MIT] |
| Settings | `C:\VirtualDisplayDriver\vdd_settings.xml` | [from-source:MIT] |
| Install | `winget install --id=VirtualDrivers.Virtual-Display-Driver -e` | [from-source:MIT] |
| Control pipe | `MTTVirtualDisplayPipe` | [hypothesis] — not in README; confirm in source (M2) |
| Pipe commands, admin requirement | unknown | [hypothesis] — M2 |

## Alternatives
| Driver | License | Notes |
|--------|---------|-------|
| Microsoft IddSampleDriver | MIT | IddCx 1.4 sample, `SwDeviceCreate`; unsigned — we'd sign it |
| virtual-display-rs (MolotovCherry) | MIT | Rust; pipe `\\.\pipe\virtualdisplaydriver`; self-signed install (used by VertoXR) |
| parsec-vdd | Driver binary Parsec-owned | Max 3 displays |
| Amyuni usbmmidd | Commercial | Licensing cost |

## Own driver (Later)
- Needs WDK (not installed, [verified-local]).
- Dev: `bcdedit /set testsigning on` + test cert.
- Release: EV code-signing certificate + Microsoft Partner Center attestation signing.

## Pitfalls
- VDD may be shared with other apps: only add/remove monitors we created; check driver version.
- Virtual monitors must be on the same GPU adapter as the glasses output (hybrid laptops).
- Set virtual monitors to 100% scaling to avoid blurry capture.
