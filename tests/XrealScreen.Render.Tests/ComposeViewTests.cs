using System.Numerics;
using XrealScreen.Core.Tracking;
using XrealScreen.Render.Scene;

namespace XrealScreen.Render.Tests;

public sealed class ComposeViewTests
{
    private const float Deg = MathF.PI / 180f;

    // FLU body frame: turning left = +Z, looking down = +Y, tilting to the right shoulder = +X.
    private static Quaternion Head(float yawDeg, float downDeg = 0, float rollDeg = 0) => QuaternionMath.Multiply(
        QuaternionMath.FromYaw(yawDeg * Deg),
        QuaternionMath.Multiply(Quaternion.CreateFromAxisAngle(Vector3.UnitY, downDeg * Deg), Quaternion.CreateFromAxisAngle(Vector3.UnitX, rollDeg * Deg)));

    /// <summary>Where a world point appears in the view's head frame (x forward, y left, z up).</summary>
    private static Vector3 Seen(HeadView view, Vector3 world) =>
        Vector3.Transform(world - view.EyeOffset, Quaternion.Conjugate(view.Orientation));

    [Fact]
    public void YawOnly_NoddingAndTilting_LeaveTheScreensStill()
    {
        var level = ViewMath.ComposeView(Head(25), TrackingAxes.YawOnly, 0, ViewMath.DefaultNeckToEye);
        var nodTilt = ViewMath.ComposeView(Head(25, downDeg: 20, rollDeg: -15), TrackingAxes.YawOnly, 0, ViewMath.DefaultNeckToEye);

        var p = new Vector3(1.5f, 0.2f, 0.1f);
        Assert.True(Vector3.Distance(Seen(level, p), Seen(nodTilt, p)) < 1e-4f, $"{Seen(level, p)} vs {Seen(nodTilt, p)}");
    }

    [Fact]
    public void YawRoll_KeepsTheNeckModel_ForLeaningIn()
    {
        var view = ViewMath.ComposeView(Head(0, downDeg: 30), TrackingAxes.YawRoll, 0, ViewMath.DefaultNeckToEye);
        Assert.True(view.EyeOffset.X > 0.03f, $"eyes move forward: {view.EyeOffset}");
    }

    [Theory]
    [InlineData(TrackingAxes.YawOnly)]
    [InlineData(TrackingAxes.YawRoll)]
    [InlineData(TrackingAxes.Full)]
    public void PositiveTilt_MovesTheScreensUp(TrackingAxes axes)
    {
        var view = ViewMath.ComposeView(Quaternion.Identity, axes, 10, Vector3.Zero);
        var ahead = Seen(view, new Vector3(1.5f, 0, 0));
        Assert.Equal(10f, MathF.Atan2(ahead.Z, ahead.X) / Deg, 2);
    }

    [Fact]
    public void Tilt_StaysVertical_AfterTurning()
    {
        // Turned 40° left: the screen straight ahead of the eyes is still only moved up, not sideways.
        var view = ViewMath.ComposeView(Head(40), TrackingAxes.YawOnly, -8, Vector3.Zero);
        var ahead = Seen(view, Vector3.Transform(new Vector3(1.5f, 0, 0), QuaternionMath.FromYaw(40 * Deg)));
        Assert.Equal(0f, ahead.Y, 4);
        Assert.Equal(-8f, MathF.Atan2(ahead.Z, ahead.X) / Deg, 2);
    }

    [Fact]
    public void ZeroTilt_IsTheConstrainedOrientation()
    {
        var head = Head(15, downDeg: 10, rollDeg: 5);
        Assert.Equal(TrackingAxes.YawPitch.Constrain(head), ViewMath.ComposeView(head, TrackingAxes.YawPitch, 0, Vector3.Zero).Orientation);
    }
}
