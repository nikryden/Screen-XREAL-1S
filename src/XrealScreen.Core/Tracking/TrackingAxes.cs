using System.Numerics;

namespace XrealScreen.Core.Tracking;

/// <summary>Which head rotations move the screens.</summary>
/// <remarks>Persisted by name; the UI lists the values in declaration order.</remarks>
public enum TrackingAxes
{
    /// <summary>
    /// Screens stay fixed for turning and stay level with the horizon when the head tilts (like real
    /// monitors), but do not move up/down when nodding. User preference 2026-09-26 (default).
    /// </summary>
    YawRoll,

    /// <summary>Only turning left/right moves the screens; tilting and nodding are ignored (screens tilt with the head).</summary>
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
        TrackingAxes.YawRoll => Quaternion.Normalize(QuaternionMath.Multiply(
            QuaternionMath.FromYaw(QuaternionMath.Yaw(orientation)),
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, QuaternionMath.Roll(orientation)))),
        TrackingAxes.YawPitch => Quaternion.Normalize(QuaternionMath.Multiply(
            QuaternionMath.FromYaw(QuaternionMath.Yaw(orientation)),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, QuaternionMath.Pitch(orientation)))),
        _ => orientation,
    };
}
