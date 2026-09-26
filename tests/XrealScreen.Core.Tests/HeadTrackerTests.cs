using System.Numerics;
using XrealScreen.Core.Tracking;
using XrealScreen.Device.Simulated;

namespace XrealScreen.Core.Tests;

public class HeadTrackerTests
{
    [Fact]
    public async Task YawSweep_IsTrackedWithinOneDegree()
    {
        var options = new SyntheticImuOptions { Motion = SyntheticMotion.YawSweep, DurationSeconds = 8, AmplitudeDegrees = 40 };
        var source = new SyntheticImuSource(options);
        var tracker = new HeadTracker();
        float maxError = 0;

        await foreach (var s in source.ReadAsync(TestContext.Current.CancellationToken))
        {
            var pose = tracker.Update(s);
            float expected = source.TrueYaw(s.TimestampNs / 1e9) * 180f / MathF.PI;
            maxError = MathF.Max(maxError, MathF.Abs(pose.YawDegrees - expected));
        }

        Assert.True(maxError < 1f, $"max yaw error {maxError:F3}°");
    }

    [Fact]
    public async Task StillWithGyroBias_BiasIsLearnedAndDriftStaysSmall()
    {
        var bias = new Vector3(0.01f, -0.005f, 0.02f); // ~1.1°/s yaw drift if uncorrected
        var source = new SyntheticImuSource(new SyntheticImuOptions { Motion = SyntheticMotion.Still, DurationSeconds = 30, GyroBias = bias, GyroNoise = 0.002f });
        var tracker = new HeadTracker();
        HeadPose last = default;

        await foreach (var s in source.ReadAsync(TestContext.Current.CancellationToken))
        {
            last = tracker.Update(s);
        }

        Assert.True((tracker.GyroBias - bias).Length() < 0.002f, $"bias estimate {tracker.GyroBias}");
        // Uncorrected drift would be ~34°; with bias learning it must stay small.
        Assert.True(MathF.Abs(last.YawDegrees) < 3f, $"yaw drift {last.YawDegrees:F2}°");
    }

    [Fact]
    public void TiltedStart_AlignsToGravityImmediately()
    {
        var tracker = new HeadTracker();
        // Head pitched: gravity partly along body X.
        var accel = Vector3.Normalize(new Vector3(0.5f, 0, 0.866f)) * 9.81f;
        var pose = tracker.Update(new ImuSample(0, Vector3.Zero, accel));

        var up = Vector3.Transform(Vector3.Normalize(accel), pose.World);
        Assert.True(Vector3.Distance(up, Vector3.UnitZ) < 1e-4f, $"measured up maps to {up}");
    }

    [Fact]
    public async Task Recenter_ZeroesYaw()
    {
        var source = new SyntheticImuSource(new SyntheticImuOptions { Motion = SyntheticMotion.ConstantYaw, AmplitudeDegrees = 30, DurationSeconds = 1 });
        var tracker = new HeadTracker();
        await foreach (var s in source.ReadAsync(TestContext.Current.CancellationToken))
        {
            tracker.Update(s);
        }

        Assert.InRange(tracker.Current.YawDegrees, 29f, 31f);
        tracker.RequestRecenter();
        var pose = tracker.Update(new ImuSample(tracker.Current.TimestampNs + 1_000_000, Vector3.Zero, new Vector3(0, 0, 9.81f)));
        Assert.InRange(pose.YawDegrees, -0.1f, 0.1f);
    }

    [Fact]
    public void PredictRelative_ExtrapolatesAngularVelocity()
    {
        var pose = new HeadPose(0, Quaternion.Identity, Quaternion.Identity, new Vector3(0, 0, MathF.PI / 2));
        var predicted = pose.PredictRelative(1f);
        Assert.InRange(QuaternionMath.Yaw(predicted) * 180f / MathF.PI, 89.9f, 90.1f);
    }
}
