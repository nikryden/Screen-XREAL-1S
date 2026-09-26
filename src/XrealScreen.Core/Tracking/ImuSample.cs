using System.Numerics;

namespace XrealScreen.Core.Tracking;

/// <summary>
/// One inertial sample in the device's sensor frame.
/// </summary>
/// <param name="TimestampNs">Device clock in nanoseconds (monotonic, arbitrary epoch).</param>
/// <param name="Gyro">Angular velocity in rad/s.</param>
/// <param name="Accel">Specific force; any unit (the filter only uses its direction).</param>
/// <param name="TemperatureC">Sensor temperature, or <see cref="float.NaN"/> when unknown.</param>
public readonly record struct ImuSample(long TimestampNs, Vector3 Gyro, Vector3 Accel, float TemperatureC = float.NaN);
