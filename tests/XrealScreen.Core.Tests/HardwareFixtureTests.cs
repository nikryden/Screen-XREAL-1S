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
