# ADR-0003: Two operating modes — native vs virtual workspace

- **Status:** Accepted
- **Date:** 2026-09-26

## Context
XREAL 1S has onboard 3DoF on the X1 chip (3 ms M2P), OSD anchor/follow/smooth-follow modes and native ultrawide (21:9, 32:9, 16:18) in OSD "Laboratory". Multi-screen workspaces with arbitrary layouts need host-side rendering and host-side head tracking. Running both trackers causes double correction.

## Decision
Support two mutually exclusive modes:
- **Native mode:** glasses handle anchoring and ultrawide. App sets the Windows display mode for the glasses (CCD), exposes recenter/control, and guides the user through OSD settings.
- **Virtual workspace mode:** app creates virtual monitors (ADR-0001), captures them, renders a head-tracked scene fullscreen on the glasses using its own IMU fusion (ADR-0007). Glasses must be in **Follow** mode with **Stabilizer off** (1S/One OSD: single-click X toggles Anchor/Follow; Spatial Screen → Stabilizer); app warns about the conflict.

Ultrawide options in virtual mode: Off, 21:9 (2560×1080), 32:9 (3840×1080), 16:18 = two 16:9 stacked (1920×2160).

## Consequences
- + Native mode ships early (M4) with minimal risk and best latency.
- + Virtual mode enables 1–6 screens, presets and Auto center.
- − Two code paths to test; mode switch must restore topology.
- − Cannot programmatically toggle the OSD anchor (no known command) → user guidance until M1 proves otherwise.

## Alternatives
- Virtual only — ignores the best-latency hardware path.
- Native only — no multi-monitor workspace, which is the product goal.
