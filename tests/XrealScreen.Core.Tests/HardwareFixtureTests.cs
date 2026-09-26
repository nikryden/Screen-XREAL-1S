using System.Numerics;
using XrealScreen.Core.Recording;
using XrealScreen.Core.Tracking;

namespace XrealScreen.Core.Tests;

/// <summary>
/// Regression tests on real XREAL 1S recordings (fw 15.01.03.522, 2026-09-26), decoded with
/// the verified sensor-to-body mapping. Cue times: tests/fixtures/1s-rotate-fw15.01.03.522.cues.csv.
/// </summary>
public class HardwareFixtureTests
{
    private static readonly string FixtureDir = Path.Combine(AppContext.BaseDirectory, "fixtures");

    private static async Task<List<(double T, HeadPose Pose)>> ReplayAsync(string file, CancellationToken ct)
    {
        var tracker = new HeadTracker();
        var result = new List<(double, HeadPose)>();
        long? t0 = null;
        await foreach (var s in XrimuFile.ReadAsync(Path.Combine(FixtureDir, file), ct))
        {
            t0 ??= s.TimestampNs;
            result.Add(((s.TimestampNs - t0.Value) / 1e9, tracker.Update(s)));
        }

        return result;
    }

    private static IEnumerable<HeadPose> Window(List<(double T, HeadPose Pose)> poses, double from, double to) =>
        poses.Where(p => p.T >= from && p.T < to).Select(p => p.Pose);

    [Fact]
    public async Task Rotation_MovesExpectedAxesWithExpectedSigns()
    {
        var poses = await ReplayAsync("1s-rotate-fw15.01.03.522.xrimu", TestContext.Current.CancellationToken);

        Assert.True(Window(poses, 4.9, 8.4).Max(p => p.YawDegrees) > 40, "turn left → yaw positive");
        Assert.True(Window(poses, 10.9, 14.4).Min(p => p.YawDegrees) < -25, "turn right → yaw negative");
        Assert.True(Window(poses, 16.9, 20.4).Max(p => p.PitchDegrees) > 20, "look up → pitch positive");
        Assert.True(Window(poses, 22.9, 26.4).Min(p => p.PitchDegrees) < -20, "look down → pitch negative");
        Assert.True(Window(poses, 28.9, 32.4).Min(p => p.RollDegrees) < -15, "tilt left → roll negative");
        Assert.True(Window(poses, 34.9, 38.4).Max(p => p.RollDegrees) > 20, "tilt right → roll positive");
    }

    [Fact]
    public async Task Rotation_LevelHeadStartsNearZeroRoll()
    {
        var poses = await ReplayAsync("1s-rotate-fw15.01.03.522.xrimu", TestContext.Current.CancellationToken);
        Assert.All(Window(poses, 0.5, 4.5), p => Assert.InRange(p.RollDegrees, -8f, 8f));
    }

    [Fact]
    public async Task Still_YawDriftIsSmall()
    {
        var poses = await ReplayAsync("1s-still-11s-fw15.01.03.522.xrimu", TestContext.Current.CancellationToken);
        float drift = MathF.Abs(poses[^1].Pose.YawDegrees - poses[0].Pose.YawDegrees);
        Assert.True(drift < 2f, $"yaw drift over 11 s on a table: {drift:F2}°");
    }
}

public class HardwareJitterTests(ITestOutputHelper output)
{
    private const string Fixture = "1s-rotate-fw15.01.03.522.xrimu";

    /// <summary>Assumed pose-latch → photon delay (vblank pacing + DWM + scan-out ≈ 1.5 frames at 120 Hz).</summary>
    private const float DisplayLatencyMs = 12f;

    /// <summary>
    /// p95 angle (deg) between the pose drawn for a frame (predicted <paramref name="predictMs"/> ahead)
    /// and the pose the head actually has when the frame is seen (<see cref="DisplayLatencyMs"/> later), over the whole rotation recording
    /// sampled at 120 Hz. This is what the wearer perceives as the image swimming/correcting.
    /// </summary>
    private static async Task<double> PredictionErrorAsync(Func<HeadTracker> create, float predictMs, CancellationToken ct)
    {
        var tracker = create();
        var poses = new List<(long T, HeadPose Pose)>();
        await foreach (var s in XrimuFile.ReadAsync(Path.Combine(AppContext.BaseDirectory, "fixtures", Fixture), ct))
        {
            poses.Add((s.TimestampNs, tracker.Update(s)));
        }

        long horizon = (long)(DisplayLatencyMs * 1e6);
        var errors = new List<double>();
        int j = 0;
        long next = poses[0].T + 500_000_000;
        for (int i = 0; i < poses.Count; i++)
        {
            if (poses[i].T < next)
            {
                continue;
            }

            next = poses[i].T + 8_333_333;
            while (j < poses.Count && poses[j].T < poses[i].T + horizon)
            {
                j++;
            }

            if (j >= poses.Count)
            {
                break;
            }

            var shown = poses[i].Pose.PredictRelative(predictMs / 1000f);
            var actual = poses[j].Pose.Relative;
            errors.Add(QuaternionMath.AngleBetween(shown, actual) * 180 / Math.PI);
        }

        errors.Sort();
        return errors[(int)(errors.Count * 0.95)];
    }

    [Fact]
    public async Task TunedTracker_PredictsBetterThanOriginal()
    {
        var ct = TestContext.Current.CancellationToken;
        HeadTracker Original() => new(new MadgwickFilter(0.05f) { AccelGate = 0 }) { PredictionSmoothingSeconds = 0 };
        var variants = new (string Name, Func<HeadTracker> Create)[]
        {
            ("original (beta .05, no gate, raw rate)", Original),
            ("+ smoothed rate 20 ms only", () => new(new MadgwickFilter(0.05f) { AccelGate = 0 }) { PredictionSmoothingSeconds = 0.02f }),
            ("+ accel gate only", () => new(new MadgwickFilter(0.05f)) { PredictionSmoothingSeconds = 0 }),
            ("+ beta .02 only", () => new(new MadgwickFilter(0.02f) { AccelGate = 0 }) { PredictionSmoothingSeconds = 0 }),
            ("beta .02 + gate + smoothing 5 ms", () => new(new MadgwickFilter(0.02f)) { PredictionSmoothingSeconds = 0.005f }),
            ("beta .02 + gate + smoothing 10 ms", () => new(new MadgwickFilter(0.02f)) { PredictionSmoothingSeconds = 0.01f }),
            ("tuned (defaults)", () => new HeadTracker()),
        };

        var results = new Dictionary<string, double>();
        foreach (var (name, create) in variants)
        {
            foreach (float ms in new[] { 0f, 12f })
            {
                double e = await PredictionErrorAsync(create, ms, ct);
                results[$"{name} @ {ms} ms"] = e;
                output.WriteLine($"{name,-40} predict {ms,2} ms: p95 error {e:F3}°");
            }
        }

        // Prediction must matter, and the tuned defaults must clearly beat the original filter.
        Assert.True(results["tuned (defaults) @ 0 ms"] > 5 * results["tuned (defaults) @ 12 ms"]);
        Assert.True(results["tuned (defaults) @ 12 ms"] < 0.7 * results["original (beta .05, no gate, raw rate) @ 12 ms"]);
    }
}

public class StabilizerTuningTests(ITestOutputHelper output)
{
    /// <summary>
    /// Replays the real rotation fixture at 120 Hz frames with 12 ms prediction and returns
    /// (p95 frame-to-frame change while nominally still, p95 error vs the true pose 12 ms later over the whole run).
    /// </summary>
    private static async Task<(double StillJitter, double TrackingError)> MeasureAsync(StabilizerSettings settings, CancellationToken ct)
    {
        var tracker = new HeadTracker();
        var poses = new List<(long T, HeadPose Pose)>();
        await foreach (var s in XrimuFile.ReadAsync(Path.Combine(AppContext.BaseDirectory, "fixtures", "1s-rotate-fw15.01.03.522.xrimu"), ct))
        {
            poses.Add((s.TimestampNs, tracker.Update(s)));
        }

        var stabilizer = new PoseStabilizer(settings);
        long t0 = poses[0].T, next = t0 + 500_000_000;
        var jitter = new List<double>();
        var error = new List<double>();
        Quaternion? previous = null;
        int j = 0;
        for (int i = 0; i < poses.Count; i++)
        {
            if (poses[i].T < next)
            {
                continue;
            }

            next = poses[i].T + 8_333_333;
            var shown = stabilizer.Filter(poses[i].Pose.PredictRelative(0.012f), 1f / 120f);
            double t = (poses[i].T - t0) / 1e9;
            if (previous is { } p && t < 4.5)
            {
                jitter.Add(Angle(p, shown));
            }

            previous = shown;
            while (j < poses.Count && poses[j].T < poses[i].T + 12_000_000)
            {
                j++;
            }

            if (j < poses.Count)
            {
                error.Add(Angle(shown, poses[j].Pose.Relative));
            }
        }

        return (P95(jitter), P95(error));

        static double Angle(Quaternion a, Quaternion b) => QuaternionMath.AngleBetween(a, b) * 180 / Math.PI;
        static double P95(List<double> v)
        {
            v.Sort();
            return v[(int)(v.Count * 0.95)];
        }
    }

    [Fact]
    public async Task Stabilizer_ReducesStillJitter_WithBoundedTrackingError()
    {
        var ct = TestContext.Current.CancellationToken;
        var candidates = new (string Name, StabilizerSettings Settings)[]
        {
            ("off", StabilizerSettings.Off),
            ("min 0.3 Hz, k 0.05", new(0.3f, 0.05f)),
            ("min 0.5 Hz, k 0.1", new(0.5f, 0.1f)),
            ("min 0.5 Hz, k 0.3", new(0.5f, 0.3f)),
            ("min 1.0 Hz, k 0.1", new(1.0f, 0.1f)),
            ("min 1.0 Hz, k 0.3", new(1.0f, 0.3f)),
            ("balanced (default)", new()),
            ("min 0.5 Hz, k 1.0", new(0.5f, 1.0f)),
        };

        var results = new Dictionary<string, (double Jitter, double Error)>();
        foreach (var (name, settings) in candidates)
        {
            var r = await MeasureAsync(settings, ct);
            results[name] = r;
            output.WriteLine($"{name,-28} still jitter p95 {r.StillJitter:F4}° ({r.StillJitter / 0.0234:F2} px)   tracking error p95 {r.TrackingError:F3}°");
        }

        var off = results["off"];
        var def = results["balanced (default)"];
        Assert.True(def.Jitter < off.Jitter * 0.8, "balanced must cut still shake by at least 20%");
        Assert.True(def.Error < 0.5, "balanced must keep world-lock error under 0.5° p95 while turning");
    }
}
