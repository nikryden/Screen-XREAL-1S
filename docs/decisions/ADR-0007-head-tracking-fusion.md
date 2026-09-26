# ADR-0007: Head-tracking fusion — own Madgwick implementation + gyro bias + prediction

- **Status:** Accepted
- **Date:** 2026-09-26

## Context
Virtual workspace mode needs a stable, low-latency 3DoF orientation from raw gyro + accel (~1 kHz, TCP 52998, [hypothesis] for 1S). Magnetometer is unreliable near a laptop, so yaw is gyro-integrated and drifts; drift must be controlled by gyro bias estimation and recenter.

## Decision
- Implement **Madgwick AHRS (IMU variant, gyro+accel)** ourselves from the published paper: S. O. H. Madgwick, "An efficient orientation filter for inertial and inertial/magnetic sensor arrays", 2010.
- **Gyro bias estimation:** at-rest detection (low gyro variance + |accel| ≈ 1 g) updates a slow bias estimate; initial calibration on connect.
- **Pose prediction:** extrapolate orientation by angular velocity to predicted photon time (render + scanout latency, measured in M3).
- **Recenter:** yaw-offset reset (manual, hotkey, glasses key, or follow mode per `RecenterPolicy`).
- Filter gain β and thresholds are configurable and tuned with recorded fixtures.

## Consequences
- + No license entanglement; deterministic, unit-testable with `.xrimu` replay.
- − Yaw drift remains; mitigated by bias estimation + Auto center.
- If the glasses deliver a fused pose (Skarian/one-xr reports a pose message), evaluate using it in M1 and supersede if better.

## Alternatives
- Mahony filter — similar; Madgwick chosen for familiarity and published reference.
- Fusion (xioTechnologies, MIT) library port — viable fallback; would add attribution.
- EKF — more complex, unnecessary for 3DoF.
- Glasses onboard 3DoF (native mode) — used in native mode, not controllable enough for the workspace.

## Update 2026-09-27 — rotation gate for the tilt correction
- [verified-hw] 2026-09-27, 1S fw 15.01.03.522: in App head tracking the screens were "shaky when tilting up and down".
  The nod recording (`tests/fixtures/1s-nod-fw15.01.03.522.xrimu` + cues) shows why: nodding swings the direction of the
  measured acceleration but barely its size, so the `| |a| − g |` gate lets it through and Madgwick's fixed-rate
  (β) correction pushes pitch up and down with every nod (≈35 px bob).
- Decision addition: **`MadgwickFilter.RateGate`** (default 0.09 rad/s ≈ 5°/s) — the accelerometer correction only runs
  while the head is nearly still. Nod fixture (NodShakeTests): bob 35 → 14 px (slow nods), 34 → 22 px (fast nods); still
  shake unchanged. Glasses A/B with the user: gated "by far the best, very little shaking".
- Remaining bob is mostly the stabilizer's smoothing lag (Balanced 10–17 px, Off 6–8 px offline). A speed-hold in the
  stabilizer (keep smoothing released through nod reversals) was tried and did not help.
