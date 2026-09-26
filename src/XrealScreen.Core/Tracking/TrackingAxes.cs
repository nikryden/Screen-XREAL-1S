using System.Numerics;

namespace XrealScreen.Core.Tracking;

/// <summary>Which head rotations move the screens.</summary>
public enum TrackingAxes
{
    /// <summary>Only turning left/right moves the screens; nodding and tilting do not (user preference 2026-09-26).</summary>
    YawOnly,

    /// <summary>Turning and nodding; tilting the head to a shoulder does not rotate the screens.</summary>
    YawPitch,

    /// <summary>Screens fixed in space for all rotations.</summary>
    Full,
}

public static class TrackingAxesExtensions
{
    /// <summary>Removes the rotations the user does not want the screens to follow.</summary>
    public static Quaternion Constrain(this TrackingAxes axes, Quaternion orientation) => axes switch
    {
        TrackingAxes.Full => orientation,
        TrackingAxes.YawOnly => QuaternionMath.FromYaw(QuaternionMath.Yaw(orientation)),
        TrackingAxes.YawPitch => Quaternion.Normalize(QuaternionMath.Multiply(
            QuaternionMath.FromYaw(QuaternionMath.Yaw(orientation)),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, QuaternionMath.Pitch(orientation)))),
        _ => orientation,
    };
}
