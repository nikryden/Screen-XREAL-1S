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
