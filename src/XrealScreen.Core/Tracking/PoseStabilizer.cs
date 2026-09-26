using System.Numerics;

namespace XrealScreen.Core.Tracking;

/// <summary>Settings for <see cref="PoseStabilizer"/>.</summary>
/// <param name="MinCutoffHz">Smoothing cutoff when the head is still. Lower = calmer, but slower to follow micro-motion. 0 disables the stabilizer.</param>
/// <param name="SpeedCoefficient">Extra cutoff (Hz) per degree/second of head speed, so turns are followed without lag.</param>
/// <param name="SpeedCutoffHz">Low-pass for the speed estimate itself.</param>
/// <remarks>
/// Presets measured on the 1S rotation fixture (p95 still shake / p95 world-lock error while turning):
/// Off 1.31 px / 0.06°, Balanced 0.96 px / 0.40°, Strong 0.75 px / 0.77°, Ultra 0.43 px / 1.55°
/// (StabilizerTuningTests). In the glasses the user rated Strong best of off/balanced/strong
/// (2026-09-26) and asked for an extra "ultra steady" option.
/// </remarks>
public sealed record StabilizerSettings(float MinCutoffHz = 0.5f, float SpeedCoefficient = 0.1f, float SpeedCutoffHz = 2f)
{
    public static StabilizerSettings Off { get; } = new(0f);

    public static StabilizerSettings Balanced { get; } = new(1.0f, 0.3f);

    public static StabilizerSettings Strong { get; } = new(0.5f, 0.1f);

    /// <summary>Calmest; screens trail noticeably behind fast head turns.</summary>
    public static StabilizerSettings Ultra { get; } = new(0.2f, 0.03f);

    public static StabilizerSettings FromName(string name) => name.ToLowerInvariant() switch
    {
        "off" => Off,
        "balanced" => Balanced,
        "strong" => Strong,
        "ultra" => Ultra,
        _ => throw new ArgumentException($"Unknown stabilizer preset '{name}' (off | balanced | strong | ultra).", nameof(name)),
    };

    public bool Enabled => MinCutoffHz > 0f;
}

/// <summary>
/// One Euro filter (Casiez, Roussel, Vogel, CHI 2012) applied to the rendered head orientation:
/// heavy smoothing while the head is (nearly) still, little smoothing while it turns.
/// Removes the pixel-level shimmer the wearer saw when holding still ([verified-hw] feedback 2026-09-26),
/// at the cost of letting very small head motions move the image slightly with the head.
/// Call once per rendered frame on the render thread.
/// </summary>
public sealed class PoseStabilizer
{
    private Quaternion _filtered = Quaternion.Identity;
    private float _speed; // deg/s, smoothed
    private bool _initialized;

    public PoseStabilizer(StabilizerSettings? settings = null) => Settings = settings ?? new StabilizerSettings();

    public StabilizerSettings Settings { get; set; }

    public void Reset() => _initialized = false;

    public Quaternion Filter(Quaternion input, float dt)
    {
        // Never let an invalid sample poison the filter state (it would stay NaN forever).
        if (!QuaternionMath.IsValidRotation(input))
        {
            return _initialized && QuaternionMath.IsValidRotation(_filtered) ? _filtered : Quaternion.Identity;
        }

        if (_initialized && (!QuaternionMath.IsValidRotation(_filtered) || !float.IsFinite(_speed)))
        {
            _initialized = false;
        }

        if (!float.IsFinite(dt))
        {
            dt = 0f;
        }

        input = Quaternion.Normalize(input);
        if (!Settings.Enabled || !_initialized || dt <= 0f)
        {
            _filtered = input;
            _speed = 0f;
            _initialized = true;
            return input;
        }

        // Keep both quaternions in the same hemisphere so the angle/slerp take the short path.
        if (Quaternion.Dot(_filtered, input) < 0f)
        {
            input = Quaternion.Negate(input);
        }

        float angle = QuaternionMath.AngleBetween(_filtered, input);
        float rawSpeed = angle * 180f / MathF.PI / dt;
        _speed += Alpha(Settings.SpeedCutoffHz, dt) * (rawSpeed - _speed);

        float cutoff = Settings.MinCutoffHz + Settings.SpeedCoefficient * _speed;
        _filtered = Quaternion.Normalize(Quaternion.Slerp(_filtered, input, Alpha(cutoff, dt)));
        return _filtered;
    }

    private static float Alpha(float cutoffHz, float dt)
    {
        float tau = 1f / (2f * MathF.PI * cutoffHz);
        return 1f / (1f + tau / dt);
    }
}
