# ADR-0006: Clean-room and licensing policy

- **Status:** Accepted
- **Date:** 2026-09-26

## Context
Useful knowledge exists in sources with incompatible or unclear licenses: XREAL SDK (no license file, license-key-gated), GPL-3.0 drivers (XRLinuxDriver, AirAPI_Windows), a research-only project (Grayscale-Feed), and a closed-source competitor (VertoXR). Our repo is MIT.

## Decision
| Source class | Allowed use | Tag |
|--------------|-------------|-----|
| MIT / Apache / BSD | Read, reference, port code with attribution in THIRD-PARTY-NOTICES | `[from-source:MIT]` |
| GPL / LGPL | Facts only (IDs, ports, constants from docs); never copy or paraphrase code | `[from-GPL:facts-only]` |
| XREAL SDK | Facts only (enum values, IDs); never redistribute, link, or copy | `[from-sdk:facts-only]` |
| Research-only / unknown license | Facts only | — |
| VertoXR (proprietary) | Observation of installed file names/strings only; **no decompilation, no disassembly, no code/asset extraction** | — |
| Our own device | Black-box observation of our own traffic (`xrs imu record` raw capture) | `[verified-hw]` |

Every external source is listed in `docs/legal/SOURCES.md` with license and how used. Unverified claims are tagged `[hypothesis]`.

## Consequences
- + Clean MIT codebase; defensible provenance.
- − Protocol details must be re-derived from MIT sources and our own recordings.
- `legal-security-reviewer` subagent checks new sources and ported code.

## Alternatives
- Link XREAL SDK binaries — none exist for Windows and license forbids.
- Port GPL code — would force GPL on the project.
