using System.Numerics;
using System.Runtime.CompilerServices;
using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Tracking;

namespace XrealScreen.Device.Simulated;

public enum SyntheticMotion
{
    /// <summary>Head perfectly still (tests bias estimation and drift).</summary>
    Still,

    /// <summary>Head turns left/right sinusoidally around the vertical axis.</summary>
    YawSweep,

    /// <summary>Head turns at a constant rate around the vertical axis.</summary>
    ConstantYaw,
}

public sealed record SyntheticImuOptions
{
    public SyntheticMotion Motion { get; init; } = SyntheticMotion.YawSweep;
    public int RateHz { get; init; } = 1000;
    public double DurationSeconds { get; init; } = 10;

    /// <summary>Peak yaw (sweep) in degrees, or rate (constant) in degrees/second.</summary>
    public float AmplitudeDegrees { get; init; } = 40f;
    public double PeriodSeconds { get; init; } = 4;
    public Vector3 GyroBias { get; init; } = Vector3.Zero;
    public float GyroNoise { get; init; }
    public int Seed { get; init; } = 1;

    /// <summary>Emit in real time (for UI demos) instead of as fast as possible.</summary>
    public bool RealTime { get; init; }
}

/// <summary>Generates IMU samples in the tracker body frame (Z up, gravity reads +Z).</summary>
public sealed class SyntheticImuSource(SyntheticImuOptions? options = null) : IImuSource
{
    private readonly SyntheticImuOptions _options = options ?? new SyntheticImuOptions();

    public string Name => $"Synthetic ({_options.Motion})";

    public async IAsyncEnumerable<ImuSample> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var random = new Random(_options.Seed);
        long count = (long)(_options.DurationSeconds * _options.RateHz);
        double dt = 1.0 / _options.RateHz;
        using var timer = _options.RealTime ? new PeriodicTimer(TimeSpan.FromMilliseconds(10)) : null;
        long emittedUntil = 0;
        long start = System.Diagnostics.Stopwatch.GetTimestamp();

        for (long i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (timer is not null && i >= emittedUntil)
            {
                await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
                emittedUntil = (long)(System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalSeconds * _options.RateHz);
            }

            double t = i * dt;
            float yawRate = YawRate(t);
            // Body frame: while yawing about the vertical axis with no tilt, body Z stays vertical.
            var gyro = new Vector3(0, 0, yawRate) + _options.GyroBias + Noise(random);
            var accel = new Vector3(0, 0, 9.81f);
            yield return new ImuSample((long)(t * 1e9), gyro, accel, 30f);
        }
    }

    /// <summary>True yaw (radians) at time <paramref name="t"/>, for test assertions.</summary>
    public float TrueYaw(double t)
    {
        float amp = _options.AmplitudeDegrees * MathF.PI / 180f;
        return _options.Motion switch
        {
            SyntheticMotion.YawSweep => amp * (float)Math.Sin(2 * Math.PI * t / _options.PeriodSeconds),
            SyntheticMotion.ConstantYaw => QuaternionMath.WrapAngle(amp * (float)t),
            _ => 0f,
        };
    }

    private float YawRate(double t)
    {
        float amp = _options.AmplitudeDegrees * MathF.PI / 180f;
        return _options.Motion switch
        {
            SyntheticMotion.YawSweep => amp * (float)(2 * Math.PI / _options.PeriodSeconds * Math.Cos(2 * Math.PI * t / _options.PeriodSeconds)),
            SyntheticMotion.ConstantYaw => amp,
            _ => 0f,
        };
    }

    private Vector3 Noise(Random random) => _options.GyroNoise <= 0
        ? Vector3.Zero
        : new Vector3(Gauss(random), Gauss(random), Gauss(random)) * _options.GyroNoise;

    private static float Gauss(Random random)
    {
        double u1 = 1.0 - random.NextDouble();
        double u2 = random.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}
