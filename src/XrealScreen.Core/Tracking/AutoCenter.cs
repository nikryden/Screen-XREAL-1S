namespace XrealScreen.Core.Tracking;

public enum RecenterPolicy
{
    /// <summary>Workspace stays where it was centered until the user recenters.</summary>
    Manual,

    /// <summary>Workspace follows the head once it leaves the dead-zone (yaw only, horizon kept).</summary>
    Follow,
}

public sealed record AutoCenterSettings
{
    public RecenterPolicy Policy { get; init; } = RecenterPolicy.Manual;

    /// <summary>Yaw offset (degrees) the head may move before the workspace starts following.</summary>
    public float DeadZoneDegrees { get; init; } = 20f;

    /// <summary>Time constant (seconds) of the follow smoothing. Larger = calmer.</summary>
    public float SmoothingSeconds { get; init; } = 0.6f;

    /// <summary>Max follow speed (degrees/second) to limit motion sickness.</summary>
    public float MaxFollowSpeedDegrees { get; init; } = 90f;
}

/// <summary>
/// Keeps the yaw reference ("where the workspace is centered") and moves it
/// according to <see cref="AutoCenterSettings"/>.
/// </summary>
public sealed class AutoCenter
{
    public AutoCenterSettings Settings { get; set; } = new();

    /// <summary>World yaw (radians) the workspace is centered on.</summary>
    public float ReferenceYaw { get; private set; }

    public void Recenter(float headYaw) => ReferenceYaw = headYaw;

    /// <summary>Updates the reference for the current head yaw; returns head yaw relative to the workspace.</summary>
    public float Update(float headYaw, float dt)
    {
        float offset = QuaternionMath.WrapAngle(headYaw - ReferenceYaw);
        if (Settings.Policy != RecenterPolicy.Follow || dt <= 0f)
        {
            return offset;
        }

        float deadZone = Settings.DeadZoneDegrees * MathF.PI / 180f;
        float excess = MathF.Abs(offset) - deadZone;
        if (excess > 0f)
        {
            // Exponential approach toward the dead-zone edge, capped by max speed.
            float alpha = Settings.SmoothingSeconds <= 0f ? 1f : 1f - MathF.Exp(-dt / Settings.SmoothingSeconds);
            float step = excess * alpha;
            float maxStep = Settings.MaxFollowSpeedDegrees * MathF.PI / 180f * dt;
            step = MathF.Min(step, maxStep);
            ReferenceYaw = QuaternionMath.WrapAngle(ReferenceYaw + MathF.CopySign(step, offset));
        }

        return QuaternionMath.WrapAngle(headYaw - ReferenceYaw);
    }
}
