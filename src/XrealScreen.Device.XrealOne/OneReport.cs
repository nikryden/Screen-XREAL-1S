using System.Numerics;

namespace XrealScreen.Device.XrealOne;

public enum OneReportType : uint
{
    Magnetometer = 0x04,
    Imu = 0x0B,
}

/// <summary>
/// One decoded 128-byte report from the stream port.
/// Field layout: [from-source:MIT] Skarian/one-xr OneXrReportMessageParser.
/// Units: gyro rad/s [from-source:MIT], accel unit [hypothesis: m/s²], timestamp ns (device clock).
/// </summary>
public readonly record struct OneReport(
    ulong DeviceId,
    ulong TimestampNs,
    OneReportType Type,
    Vector3 Gyro,
    Vector3 Accel,
    Vector3 Magnetometer,
    float TemperatureC,
    byte ImuId,
    int FrameId);
