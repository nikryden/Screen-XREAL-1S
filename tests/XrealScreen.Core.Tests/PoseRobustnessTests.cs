using System.Numerics;
using XrealScreen.Core.Tracking;

namespace XrealScreen.Core.Tests;

/// <summary>
/// Regression: the renderer latched a default HeadPose (zero quaternion) before the first IMU sample;
/// the resulting NaN stuck in the stabilizer and the screens vanished for the whole session.
/// </summary>
public class PoseRobustnessTests
{
    [Fact]
    public void DefaultPose_PredictsStraightAhead()
    {
        HeadPose pose = default;
        Assert.Equal(Quaternion.Identity, pose.PredictRelative(0.012f));
    }

    [Theory]
    [InlineData("strong")]
    [InlineData("balanced")]
    [InlineData("ultra")]
    [InlineData("off")]
    public void Stabilizer_IgnoresInvalidInput_AndKeepsWorking(string preset)
    {
        var stabilizer = new PoseStabilizer(StabilizerSettings.FromName(preset));
        var zero = default(Quaternion);
        var nan = new Quaternion(float.NaN, 0, 0, 1);

        Assert.True(QuaternionMath.IsValidRotation(stabilizer.Filter(zero, 0f)));
        Assert.True(QuaternionMath.IsValidRotation(stabilizer.Filter(nan, 1 / 120f)));

        var target = QuaternionMath.FromYaw(0.5f);
        Quaternion q = Quaternion.Identity;
        for (int i = 0; i < 2000; i++)
        {
            q = stabilizer.Filter(i % 50 == 0 ? nan : target, 1 / 120f);
        }

        Assert.True(QuaternionMath.IsValidRotation(q));
        Assert.True(QuaternionMath.AngleBetween(q, target) < 0.01f, "filter still converges after invalid samples");
    }

    [Fact]
    public void AllAxes_ConstrainIdentitySafely()
    {
        foreach (var axes in Enum.GetValues<TrackingAxes>())
        {
            Assert.True(QuaternionMath.IsValidRotation(axes.Constrain(Quaternion.Identity)));
        }
    }
}
