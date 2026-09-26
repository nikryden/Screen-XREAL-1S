# ADR-0004: Service scope — user-session engine, elevated helper service only

- **Status:** Accepted
- **Date:** 2026-09-26

## Context
Windows services run in Session 0, which has no access to the interactive desktop: no Windows.Graphics.Capture of user monitors, no swapchain on the user's display, and CCD `SetDisplayConfig` applies to the calling session. Some operations need admin: VDD install/check, writing `C:\VirtualDisplayDriver\vdd_settings.xml` [hypothesis], possibly the VDD pipe.

## Decision
- Capture, render, IMU and CCD run **in the user session** (`XrealScreen.Host`, hosted by the App).
- `XrealScreen.Service` is a **small elevated helper**: VDD presence/version check and install trigger, settings XML writes, pipe calls that need admin, topology-restore watchdog (asks a user-session agent to restore if the App dies). _(Note 2026-09-26: with ADR-0008 there are no settings XML or admin pipe calls; the service is left with driver install/version checks and the watchdog.)_
- IPC: named pipe, ACL = interactive user + SYSTEM, versioned DTOs in `XrealScreen.Contracts`.

## Consequences
- + Minimal privileged surface; the app works without the service for native mode.
- − Two processes to version and update together.
- − MSIX packaged services need the restricted `packagedServices` capability (ADR-0005).
- Security review required for pipe ACLs and message validation.

## Alternatives
- Everything in the service — impossible for capture/render (Session 0).
- No service, run app elevated — UAC every launch; poor UX, bigger attack surface.
