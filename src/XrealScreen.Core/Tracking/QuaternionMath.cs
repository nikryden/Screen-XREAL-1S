using System.Numerics;

namespace XrealScreen.Core.Tracking;

/// <summary>Helpers for the Z-up world frame used by the tracker.</summary>
public static class QuaternionMath
{
    /// <summary>Hamilton product a ⊗ b (apply b, then a).</summary>
    public static Quaternion Multiply(Quaternion a, Quaternion b) => new(
        a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
        a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
        a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
        a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

    /// <summary>Heading around world Z, radians, range (-π, π].</summary>
    public static float Yaw(Quaternion q) =>
        MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));

    /// <summary>Rotation around world Y, radians.</summary>
    public static float Pitch(Quaternion q) =>
        MathF.Asin(Math.Clamp(2f * (q.W * q.Y - q.Z * q.X), -1f, 1f));

    /// <summary>Rotation around world X, radians.</summary>
    public static float Roll(Quaternion q) =>
        MathF.Atan2(2f * (q.W * q.X + q.Y * q.Z), 1f - 2f * (q.X * q.X + q.Y * q.Y));

    public static Quaternion FromYaw(float yawRadians) => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, yawRadians);

    /// <summary>Wraps an angle to (-π, π].</summary>
    public static float WrapAngle(float radians)
    {
        float wrapped = MathF.IEEERemainder(radians, 2f * MathF.PI);
        return wrapped <= -MathF.PI ? wrapped + 2f * MathF.PI : wrapped;
    }

    /// <summary>Integrates a body-frame angular velocity over <paramref name="dt"/> seconds.</summary>
    public static Quaternion Integrate(Quaternion orientation, Vector3 angularVelocity, float dt)
    {
        float rate = angularVelocity.Length();
        if (rate < 1e-9f || dt <= 0f)
        {
            return orientation;
        }

        var delta = Quaternion.CreateFromAxisAngle(angularVelocity / rate, rate * dt);
        return Quaternion.Normalize(Multiply(orientation, delta));
    }
}
