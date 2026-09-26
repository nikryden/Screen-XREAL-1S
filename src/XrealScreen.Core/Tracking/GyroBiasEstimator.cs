using System.Numerics;

namespace XrealScreen.Core.Tracking;

/// <summary>
/// Estimates gyroscope bias while the head is still. Gyro bias drifts with temperature,
/// so the estimate keeps adapting whenever a still period is detected.
/// </summary>
public sealed class GyroBiasEstimator
{
    private readonly float _stillGyroThreshold;
    private readonly double _requiredStillSeconds;
    private readonly float _adaptRate;
    private double _stillSeconds;
    private Vector3 _stillMean;
    private int _stillCount;

    /// <param name="stillGyroThreshold">Max |gyro - bias| in rad/s to count as still.</param>
    /// <param name="requiredStillSeconds">Continuous stillness needed before adapting.</param>
    /// <param name="adaptRate">Blend factor applied per completed still window (0..1).</param>
    public GyroBiasEstimator(float stillGyroThreshold = 0.03f, double requiredStillSeconds = 1.0, float adaptRate = 0.5f)
    {
        _stillGyroThreshold = stillGyroThreshold;
        _requiredStillSeconds = requiredStillSeconds;
        _adaptRate = adaptRate;
    }

    public Vector3 Bias { get; private set; }

    public bool IsStill => _stillSeconds >= _requiredStillSeconds;

    public void Reset(Vector3 bias = default)
    {
        Bias = bias;
        _stillSeconds = 0;
        _stillMean = Vector3.Zero;
        _stillCount = 0;
    }

    /// <summary>Feeds a raw gyro sample; returns the bias-corrected value.</summary>
    public Vector3 Update(Vector3 rawGyro, double dt)
    {
        var corrected = rawGyro - Bias;
        bool still = corrected.Length() < _stillGyroThreshold ||
                     (_stillCount > 0 && (rawGyro - _stillMean).Length() < _stillGyroThreshold);

        if (!still)
        {
            _stillSeconds = 0;
            _stillMean = Vector3.Zero;
            _stillCount = 0;
            return corrected;
        }

        _stillCount++;
        _stillMean += (rawGyro - _stillMean) / _stillCount;
        _stillSeconds += dt;

        if (_stillSeconds >= _requiredStillSeconds)
        {
            Bias = Vector3.Lerp(Bias, _stillMean, _adaptRate);
            // Start a new window so the estimate keeps following slow drift.
            _stillSeconds = 0;
            _stillMean = Vector3.Zero;
            _stillCount = 0;
        }

        return rawGyro - Bias;
    }
}
