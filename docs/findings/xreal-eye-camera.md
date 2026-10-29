# XREAL Eye camera

The Eye is XREAL's clip-on camera for the One series. We read its tracking-camera feed over the glasses' USB network and ran hand tracking on it. Plan: ROADMAP M9, ADR-0016.

Hardware runs on 2026-10-29 used our XREAL 1S (PID 0x043E) with the Eye attached; the firmware was not re-read that day (last recorded 15.01.03.522, possibly updated since — confirm in the OSD before relying on the version).

## Hardware (vendor docs)
| Fact | Value | Tag |
|------|-------|-----|
| Photo camera | 12 MP, f/2.25; photos up to 2016×1512 JPG, video 1600×1200 MP4 30/60 fps, recorded by the glasses (buttons) | vendor spec, [hypothesis] for PC access |
| Supported glasses | "One and One Pro series"; works on our 1S | [verified-hw] 2026-10-29 |
| 6DoF | "Native 6DoF" computed in the glasses' X1 chip | vendor spec, [hypothesis] whether the pose reaches the PC |
| XREAL SDK camera API | Unity/Android only (YUV_420_888 frames); not usable on Windows (ADR-0006) | [from-sdk:facts-only] |

## Feed on TCP 52997
| Fact | Value | Tag |
|------|-------|-----|
| Availability | Streams as soon as a client connects; no Nebula beta, no extra OSD setting needed on the 1S | [verified-hw] 2026-10-29 |
| Rate | ~11.6 MB/s, 60 fps (device timestamps 16.68 ms apart), 0 gaps over 127 s | [verified-hw] 2026-10-29 |
| Framing | magic `27 48`, then **u32 big-endian** body length (193,856) — same shape as the IMU port | [verified-hw] 2026-10-29 |
| Body | 320-byte header + one **8-bit grayscale 512×378** image (row stride 512) | [verified-hw] 2026-10-29 |
| Header u64 LE @17 | device timestamp, ns (no separate frame counter found) | [verified-hw] 2026-10-29 |
| Header @5 / @9 / @14 | 504, 378, 2 (usable width? height, camera count?) | [hypothesis] |
| Cameras | one view per frame; no left/right alternation seen | [verified-hw] 2026-10-29 |
| Image noise | pixel-level checkerboard noise; must be smoothed (3×3 Gaussian) before ML models | [verified-hw] 2026-10-29 |
| Other glasses | One Pro + Nebula beta reportedly send 4-bit stereo 512×378 and need OSD Spatial Anchor (Aloim, research-only) | [hypothesis] for us |

## Metadata on TCP 52996
| Fact | Value | Tag |
|------|-------|-----|
| Rate with Eye attached | ~3.4 KB/s, magic `27 31` | [verified-hw] 2026-10-29 |
| Content (6DoF pose?) | not decoded | [hypothesis] |

## Hand tracking test (2026-10-29)
MediaPipe Hand Landmarker (float16 model, Apache-2.0) in Python on the recorded feed (30 fps kept), 3×3 blur first. User wore the glasses and followed spoken steps. [verified-hw] 2026-10-29

| Pose | Hand found | Note |
|------|-----------|------|
| Raised, still (~40 cm) | 100 % | fingertip jitter 1.2 px/frame (512 px wide image) |
| Pointing, drawing a square | 43 % | side-on hand is the weak case |
| Pinch once per second | 96 % | pinch ratio \|thumb tip − index tip\| / \|wrist − middle MCP\|: 0.1–0.2 closed, ~1.0 open |
| Closer / further | 100 % | hand size 57–89 px |
| Typing height, looking ahead | 100 % | hands at desk height are in view |
| Looking down at the hand | 100 % | |

- Without the blur: 0 % found (score 0.97 with it, same frame).
- Inference 10–19 ms per frame on the dev PC CPU [verified-local].

## Glasses' own Gesture Control
| Fact | Value | Tag |
|------|-------|-----|
| Feature | firmware gestures with the Eye: hand moves a cursor, pinch (≥ ~300 ms, < ~2.2 cm) clicks, wrist flip turns pages, palm-back opens a menu | vendor docs, [hypothesis] on Windows |
| Switch | OSD: double-tap X → Additional (Other) → Lab → Gesture Controls → Gesture Control; **off by default**; can be bound to a shortcut key | vendor docs |
| Conflicts | mutually exclusive with 3D mode; would double-click together with our hand control → keep it off | vendor docs |
| Feed with it off | the 52997 feed works with default settings (gesture control presumably off) | [verified-hw] 2026-10-29, OSD state to confirm |
