using System.Numerics;

namespace XrealScreen.Core.Tracking;

/// <summary>Head orientation relative to the centered workspace.</summary>
/// <param name="TimestampNs">Timestamp of the last IMU sample used.</param>
/// <param name="World">Absolute orientation (Z-up world, arbitrary initial yaw).</param>
/// <param name="Relative">Orientation relative to the workspace center (yaw re-referenced).</param>
/// <param name="AngularVelocity">Bias-corrected body rate, rad/s (used for prediction).</param>
public readonly record struct HeadPose(long TimestampNs, Quaternion World, Quaternion Relative, Vector3 AngularVelocity)
{
    public float YawDegrees => QuaternionMath.Yaw(Relative) * 180f / MathF.PI;
    public float PitchDegrees => QuaternionMath.Pitch(Relative) * 180f / MathF.PI;
    public float RollDegrees => QuaternionMath.Roll(Relative) * 180f / MathF.PI;

    /// <summary>Extrapolates the relative pose <paramref name="seconds"/> ahead (render latency compensation).</summary>
    public Quaternion PredictRelative(float seconds) => QuaternionMath.Integrate(Relative, AngularVelocity, seconds);
}

/// <summary>
/// IMU → orientation pipeline: gyro bias estimation, Madgwick fusion, auto-center.
/// Not thread-safe; feed it from one thread (the IMU reader) and publish <see cref="HeadPose"/> copies.
/// </summary>
public sealed class HeadTracker
{
    private const double MaxDtSeconds = 0.1;
    private readonly MadgwickFilter _filter;
    private readonly GyroBiasEstimator _bias;
    private long _lastTimestampNs = long.MinValue;
    private bool _recenterPending = true;

    public HeadTracker(MadgwickFilter? filter = null, GyroBiasEstimator? bias = null, AutoCenter? autoCenter = null)
    {
        _filter = filter ?? new MadgwickFilter();
        _bias = bias ?? new GyroBiasEstimator();
        AutoCenter = autoCenter ?? new AutoCenter();
    }

    public AutoCenter AutoCenter { get; }

    public HeadPose Current { get; private set; } = new(0, Quaternion.Identity, Quaternion.Identity, Vector3.Zero);

    public long SampleCount { get; private set; }

    public Vector3 GyroBias => _bias.Bias;

    /// <summary>Requests a recenter on the next sample (thread-safe flag).</summary>
    public void RequestRecenter() => Volatile.Write(ref _recenterPending, true);

    public HeadPose Update(in ImuSample sample)
    {
        double dt = _lastTimestampNs == long.MinValue ? 0 : (sample.TimestampNs - _lastTimestampNs) / 1e9;
        _lastTimestampNs = sample.TimestampNs;
        if (dt < 0 || dt > MaxDtSeconds)
        {
            // Clock jump or stream gap: skip integration for this sample.
            dt = 0;
        }

        var gyro = _bias.Update(sample.Gyro, dt);

        if (SampleCount == 0)
        {
            // Start aligned with gravity so tilt converges immediately.
            _filter.Reset(AlignToGravity(sample.Accel));
        }
        else
        {
            _filter.Update(gyro, sample.Accel, (float)dt);
        }

        SampleCount++;
        var world = _filter.Orientation;
        float headYaw = QuaternionMath.Yaw(world);

        if (Volatile.Read(ref _recenterPending))
        {
            AutoCenter.Recenter(headYaw);
            Volatile.Write(ref _recenterPending, false);
        }

        float relativeYaw = AutoCenter.Update(headYaw, (float)dt);
        // Remove the reference yaw, keep pitch/roll untouched (horizon locked).
        var relative = QuaternionMath.Multiply(QuaternionMath.FromYaw(relativeYaw - headYaw), world);

        Current = new HeadPose(sample.TimestampNs, world, Quaternion.Normalize(relative), gyro);
        return Current;
    }

    private static Quaternion AlignToGravity(Vector3 accel)
    {
        float n = accel.Length();
        if (n < 1e-6f)
        {
            return Quaternion.Identity;
        }

        // Rotation taking the measured "up" (sensor frame) to world +Z.
        var up = accel / n;
        var axis = Vector3.Cross(up, Vector3.UnitZ);
        float sin = axis.Length();
        float cos = Vector3.Dot(up, Vector3.UnitZ);
        if (sin < 1e-6f)
        {
            return cos > 0 ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
        }

        // Filter quaternions rotate sensor-frame vectors into the world frame.
        return Quaternion.CreateFromAxisAngle(axis / sin, MathF.Atan2(sin, cos));
    }
}
