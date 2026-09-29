# ADR-0010: Glasses-anchor workspace (native mode with split screens)

- **Status:** Accepted
- **Date:** 2026-09-26

## Context
In the app-tracking workspace (ADR-0003 virtual mode, ADR-0009) the user found the image good but not "perfectly still": some lag and residual tilt remain, and partial locking (yaw + roll without pitch) caused motion sickness. Our motion-to-photon path is IMU → TCP → host fusion → D3D → DWM → DisplayPort → glasses; the 1S X1 chip world-locks its own image in ~3 ms (vendor), which a host pipeline cannot match. The user asked for Anchor-mode stability with several screens, possibly via the glasses' UltraWide mode.

## Decision
Add a second workspace kind, **GlassesAnchor** (`WorkspaceKind`), alongside **AppTracking**:
- The user sets the glasses OSD to **Anchor** + **UltraWide 32:9 / 16:18 / 21:9**. The glasses hold that ultrawide image still.
- The app creates 1–3 virtual monitors sized to split the glasses signal exactly (`AnchorSplit`: 32:9 → 2 × 1920×1080 or 3 × 1280×1080; 16:18 → 2 × 1920×1080 stacked; 21:9 → 2 × 1280×1080), captures them and draws them **flat and pixel-exact** into the glasses output (`WorkspaceScene.AddFlatScreen`, presenter `Flat` = identity view-projection). No head tracking, no refresh-rate changes.
- Desktop order and cleanup are the same as for app tracking (`WorkspaceArrangement`, layout snapshot).
- Recenter is the glasses' own (long-press X).

## Consequences
- + Stability of the glasses' own anchor (hardware 3DoF) with several separate Windows monitors; sharp (1:1 pixels).
- − Limited to the glasses' ultrawide canvas: at most 2 full-HD screens (or 3 narrower); flat, not curved; 60 Hz in 32:9 and 16:18 (90 Hz in 21:9) — glasses limits ([verified-hw] mode table).
- − Distance/size are the glasses' (OSD), not ours; neck model / stabilizer / axes do not apply.
- AppTracking remains for more/curved screens.

## Alternatives
- Only app tracking with more latency work (independent flip, better prediction): still cannot reach on-glasses latency.
- Use the glasses ultrawide monitor directly without virtual monitors: works too (one big monitor), but no separate monitors for maximize/snap per screen.
