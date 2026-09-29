using System.Numerics;
using XrealScreen.Core.Recording;
using XrealScreen.Core.Tracking;

namespace XrealScreen.Core.Tests;

/// <summary>
/// Up/down shake while nodding ([verified-hw] 2026-09-27, 1S fw 15.01.03.522: "all was shaky when tilting up
/// and down"). Replays the nod fixture (still, slow nods, still, fast nods) through pipeline variants and
/// writes nod-shake.txt (see <see cref="Measure"/>).
/// </summary>
public class NodShakeTests
{
    private const string Fixture = "1s-nod-fw15.01.03.522.xrimu";
    private const double DegPerPixel = 45.0 / 1920.0;

    private sealed record Variant(string Name, float Beta, float Gate, StabilizerSettings Stabilizer, float PredictMs, float RateGate = 0f);

    [Fact]
    public async Task NodShake_TradeoffTable()
    {
        var ct = TestContext.Current.CancellationToken;
        var samples = new List<ImuSample>();
        await foreach (var s in XrimuFile.ReadAsync(Path.Combine(AppContext.BaseDirectory, "fixtures", Fixture), ct))
        {
            samples.Add(s);
        }

        // Reference: gyro-only integration (no accelerometer tilt correction, so no wobble from head acceleration).
        var truth = Run(samples, new MadgwickFilter(0f) { AccelGate = 0f });
        var lines = new List<string> { Profile(truth) };
        foreach (var v in new Variant[]
        {
            new("app now: beta .02 gate .6, strong, 20 ms", 0.02f, 0.6f, StabilizerSettings.Strong, 20),
            new("same, 12 ms prediction", 0.02f, 0.6f, StabilizerSettings.Strong, 12),
            new("same, no prediction", 0.02f, 0.6f, StabilizerSettings.Strong, 0),
            new("stabilizer off, 20 ms", 0.02f, 0.6f, StabilizerSettings.Off, 20),
            new("no tilt correction (beta 0), strong, 20 ms", 0f, 0f, StabilizerSettings.Strong, 20),
            new("beta .02 gate .3, strong, 20 ms", 0.02f, 0.3f, StabilizerSettings.Strong, 20),
            new("beta .01 gate .3, strong, 20 ms", 0.01f, 0.3f, StabilizerSettings.Strong, 20),
            new("beta .005 gate .3, strong, 20 ms", 0.005f, 0.3f, StabilizerSettings.Strong, 20),
            new("beta .02 gate .6, ultra, 20 ms", 0.02f, 0.6f, StabilizerSettings.Ultra, 20),
            new("rate gate 0.35 rad/s (20°/s)", 0.02f, 0.6f, StabilizerSettings.Strong, 20, 0.35f),
            new("rate gate 0.17 rad/s (10°/s)", 0.02f, 0.6f, StabilizerSettings.Strong, 20, 0.17f),
            new("rate gate 0.09 rad/s (5°/s)", 0.02f, 0.6f, StabilizerSettings.Strong, 20, 0.09f),
            new("rate gate 0.05 rad/s (3°/s)", 0.02f, 0.6f, StabilizerSettings.Strong, 20, 0.05f),
            new("rate gate 5°/s, strong, 25 ms", 0.02f, 0.6f, StabilizerSettings.Strong, 25, 0.09f),
            new("rate gate 5°/s, strong, 30 ms", 0.02f, 0.6f, StabilizerSettings.Strong, 30, 0.09f),
            new("rate gate 5°/s, balanced, 20 ms", 0.02f, 0.6f, StabilizerSettings.Balanced, 20, 0.09f),
            new("rate gate 5°/s, off, 20 ms", 0.02f, 0.6f, StabilizerSettings.Off, 20, 0.09f),
        })
        {
            var poses = Run(samples, new MadgwickFilter(v.Beta) { AccelGate = v.Gate, RateGate = v.RateGate });
            var (slowShake, slowBob, fastShake, fastBob, stillShake) = Measure(poses, truth, v);
            lines.Add($"{v.Name,-44} slow nod: shake {slowShake:F1} bob {slowBob:F1} px | fast nod: shake {fastShake:F1} bob {fastBob:F1} px | still shake {stillShake:F1} px");
        }

        await File.WriteAllLinesAsync(Path.Combine(AppContext.BaseDirectory, "nod-shake.txt"), lines, ct);
        Assert.True(lines.Count > 1);
    }

    private static List<HeadPose> Run(List<ImuSample> samples, MadgwickFilter filter)
    {
        var tracker = new HeadTracker(filter);
        return samples.Select(s => tracker.Update(s)).ToList();
    }

    private static double Pitch(Quaternion q) => -QuaternionMath.Pitch(q) * 180 / Math.PI;

    /// <summary>Per-second RMS pitch rate of the recording (to locate the nod segments).</summary>
    private static string Profile(List<HeadPose> poses)
    {
        long t0 = poses[0].TimestampNs;
        var rms = poses.GroupBy(p => (int)((p.TimestampNs - t0) / 1_000_000_000))
            .Select(g => $"{g.Key}s:{Math.Sqrt(g.Average(p => p.AngularVelocity.Y * p.AngularVelocity.Y)) * 180 / Math.PI:F0}");
        return "pitch rate rms °/s per second: " + string.Join(" ", rms);
    }

    /// <summary>Display latency assumed for every variant (run 3, 20 ms prediction, was judged best in the glasses).</summary>
    private const long DisplayLatencyNs = 20_000_000;

    /// <summary>
    /// Latches at 120 Hz like the presenter. Error e(t) = shown pitch − true pitch at t + display latency.
    /// shake = e minus its ±50 ms mean (fast jitter); bob = ±50 ms mean minus ±1 s mean (the screens moving up and
    /// down with the nod, 1–10 Hz); constant offsets are ignored because they are invisible.
    /// </summary>
    private static (double SlowShake, double SlowBob, double FastShake, double FastBob, double StillShake) Measure(
        List<HeadPose> poses, List<HeadPose> truth, Variant v)
    {
        var stabilizer = new PoseStabilizer(v.Stabilizer);
        long t0 = poses[0].TimestampNs, next = t0 + 500_000_000;
        var frames = new List<(double T, double E)>();
        int j = 0;
        for (int i = 0; i < poses.Count; i++)
        {
            if (poses[i].TimestampNs < next)
            {
                continue;
            }

            next = poses[i].TimestampNs + 8_333_333;
            var p = poses[i];
            var shown = stabilizer.Filter(p.PredictRelative(v.PredictMs / 1000f), 1f / 120f, p.AngularVelocity.Length() * 180f / MathF.PI);
            while (j < truth.Count && truth[j].TimestampNs < p.TimestampNs + DisplayLatencyNs)
            {
                j++;
            }

            if (j < truth.Count)
            {
                frames.Add(((p.TimestampNs - t0) / 1e9, Pitch(shown) - Pitch(truth[j].Relative)));
            }
        }

        var shortMean = Mean(6);
        var longMean = Mean(120);
        var shake = frames.Select((f, i) => f.E - shortMean[i]).ToArray();
        var bob = shortMean.Select((m, i) => m - longMean[i]).ToArray();

        // Segments from the recording's cues (seconds since the first sample; see the .cues.csv).
        return (P95(Range(10, 17), shake), P95(Range(10, 17), bob), P95(Range(23, 28), shake), P95(Range(23, 28), bob), P95(Range(2, 8), shake));

        double[] Mean(int half)
        {
            var m = new double[frames.Count];
            for (int i = 0; i < frames.Count; i++)
            {
                int a = Math.Max(0, i - half), b = Math.Min(frames.Count - 1, i + half);
                double sum = 0;
                for (int k = a; k <= b; k++)
                {
                    sum += frames[k].E;
                }

                m[i] = sum / (b - a + 1);
            }

            return m;
        }

        IEnumerable<int> Range(double from, double to) => Enumerable.Range(0, frames.Count).Where(i => frames[i].T >= from && frames[i].T < to);
        static double P95(IEnumerable<int> idx, double[] values)
        {
            var v = idx.Select(i => Math.Abs(values[i])).Order().ToList();
            return v.Count == 0 ? double.NaN : v[(int)(v.Count * 0.95)] / DegPerPixel;
        }
    }
}
