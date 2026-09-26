using System.Numerics;
using XrealScreen.Core.Tracking;

namespace XrealScreen.Core.Tests;

public class TrackingAxesTests
{
    private static float Rad(float d) => d * MathF.PI / 180f;
    private static float Deg(float r) => r * 180f / MathF.PI;

    // Yaw 30° left, nose up 20°, tilt 15° — built in Z-Y-X order like the tracker's Euler angles.
    private static readonly Quaternion Head = QuaternionMath.Multiply(
        QuaternionMath.Multiply(QuaternionMath.FromYaw(Rad(30)), Quaternion.CreateFromAxisAngle(Vector3.UnitY, Rad(-20))),
        Quaternion.CreateFromAxisAngle(Vector3.UnitX, Rad(15)));

    [Fact]
    public void YawOnly_KeepsYaw_DropsPitchAndRoll()
    {
        var q = TrackingAxes.YawOnly.Constrain(Head);
        Assert.Equal(30f, Deg(QuaternionMath.Yaw(q)), 2);
        Assert.Equal(0f, Deg(QuaternionMath.Pitch(q)), 2);
        Assert.Equal(0f, Deg(QuaternionMath.Roll(q)), 2);
    }

    [Fact]
    public void YawPitch_DropsOnlyRoll()
    {
        var q = TrackingAxes.YawPitch.Constrain(Head);
        Assert.Equal(30f, Deg(QuaternionMath.Yaw(q)), 2);
        Assert.Equal(-20f, Deg(QuaternionMath.Pitch(q)), 2);
        Assert.Equal(0f, Deg(QuaternionMath.Roll(q)), 2);
    }

    [Fact]
    public void Full_IsUnchanged() => Assert.Equal(Head, TrackingAxes.Full.Constrain(Head));
}
