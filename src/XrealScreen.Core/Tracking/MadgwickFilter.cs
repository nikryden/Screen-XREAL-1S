using System.Numerics;

namespace XrealScreen.Core.Tracking;

/// <summary>
/// Gyro + accelerometer orientation filter (gradient descent), implemented from
/// S. Madgwick, "An efficient orientation filter for inertial and inertial/magnetic
/// sensor arrays", 2010. See docs/decisions/ADR-0007.
/// World frame: Z up (gravity reads as +Z on the accelerometer when level).
/// </summary>
public sealed class MadgwickFilter
{
    /// <summary>Accelerometer correction gain. Higher = faster tilt correction, more noise.</summary>
    public float Beta { get; set; }

    /// <summary>
    /// Accelerometer correction is skipped while |accel| differs from <see cref="GravityMagnitude"/>
    /// by more than this (head is accelerating, so the reading is not "down"). [verified-hw] without
    /// the gate the 1S image wobbled ("corrects itself") during head motion. 0 disables the gate.
    /// </summary>
    public float AccelGate { get; set; } = 0.6f;

    /// <summary>
    /// Accelerometer correction is also skipped while the head rotates faster than this (rad/s): nodding
    /// swings the acceleration's direction but barely its size, so <see cref="AccelGate"/> lets it through and
    /// the fixed-rate correction bobs the screens up and down ([verified-hw] 2026-09-27: "shaky when tilting up and
    /// down"). On the nod fixture 0.09 rad/s (5°/s) cuts the bob from 35 to 14 px (slow nods) and 34 to 22 px (fast
    /// nods) (NodShakeTests). 0 disables.
    /// </summary>
    public float RateGate { get; set; } = 0.09f;

    /// <summary>Expected |accel| at rest (m/s² for XREAL One-series glasses).</summary>
    public float GravityMagnitude { get; set; } = 9.81f;

    public Quaternion Orientation { get; private set; } = Quaternion.Identity;

    /// <param name="beta">Tilt correction gain; 0.02 ≈ 1°/s max correction rate (was 0.05 before hardware tuning).</param>
    public MadgwickFilter(float beta = 0.02f) => Beta = beta;

    public void Reset(Quaternion? orientation = null) => Orientation = orientation ?? Quaternion.Identity;

    /// <summary>Advances the filter by <paramref name="dt"/> seconds.</summary>
    /// <param name="gyro">Bias-corrected angular velocity, rad/s, sensor frame.</param>
    /// <param name="accel">Accelerometer reading, sensor frame (unit irrelevant).</param>
    public void Update(Vector3 gyro, Vector3 accel, float dt)
    {
        if (dt <= 0f || !float.IsFinite(dt))
        {
            return;
        }

        float q0 = Orientation.W, q1 = Orientation.X, q2 = Orientation.Y, q3 = Orientation.Z;
        float gx = gyro.X, gy = gyro.Y, gz = gyro.Z;

        // Rate of change of quaternion from gyroscope.
        float qDot0 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
        float qDot1 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
        float qDot2 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
        float qDot3 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

        float aNorm = accel.Length();
        bool trustAccel = (AccelGate <= 0f || MathF.Abs(aNorm - GravityMagnitude) <= AccelGate)
            && (RateGate <= 0f || gyro.LengthSquared() <= RateGate * RateGate);
        if (trustAccel && aNorm > 1e-6f && float.IsFinite(aNorm))
        {
            float ax = accel.X / aNorm, ay = accel.Y / aNorm, az = accel.Z / aNorm;

            float f2q0 = 2f * q0, f2q1 = 2f * q1, f2q2 = 2f * q2, f2q3 = 2f * q3;
            float f4q0 = 4f * q0, f4q1 = 4f * q1, f4q2 = 4f * q2;
            float f8q1 = 8f * q1, f8q2 = 8f * q2;
            float q0q0 = q0 * q0, q1q1 = q1 * q1, q2q2 = q2 * q2, q3q3 = q3 * q3;

            // Gradient of the objective function (predicted vs measured gravity).
            float s0 = f4q0 * q2q2 + f2q2 * ax + f4q0 * q1q1 - f2q1 * ay;
            float s1 = f4q1 * q3q3 - f2q3 * ax + 4f * q0q0 * q1 - f2q0 * ay - f4q1 + f8q1 * q1q1 + f8q1 * q2q2 + f4q1 * az;
            float s2 = 4f * q0q0 * q2 + f2q0 * ax + f4q2 * q3q3 - f2q3 * ay - f4q2 + f8q2 * q1q1 + f8q2 * q2q2 + f4q2 * az;
            float s3 = 4f * q1q1 * q3 - f2q1 * ax + 4f * q2q2 * q3 - f2q2 * ay;

            float sNorm = MathF.Sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3);
            if (sNorm > 1e-9f)
            {
                qDot0 -= Beta * s0 / sNorm;
                qDot1 -= Beta * s1 / sNorm;
                qDot2 -= Beta * s2 / sNorm;
                qDot3 -= Beta * s3 / sNorm;
            }
        }

        var q = new Quaternion(q1 + qDot1 * dt, q2 + qDot2 * dt, q3 + qDot3 * dt, q0 + qDot0 * dt);
        Orientation = Quaternion.Normalize(q);
    }
}
