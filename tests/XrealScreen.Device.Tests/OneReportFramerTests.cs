using System.Numerics;
using XrealScreen.Device.XrealOne;

namespace XrealScreen.Device.Tests;

public class OneReportFramerTests
{
    private static readonly Vector3 Gyro = new(0.01f, -0.02f, 0.5f);
    private static readonly Vector3 Accel = new(0.1f, 9.7f, 0.3f);

    [Fact]
    public void DecodesAllFields()
    {
        var framer = new OneReportFramer();
        var reports = new List<OneReport>();
        framer.Append(ReportBuilder.Build(OneReportType.Imu, 123_456_789, Gyro, Accel), reports.Add);

        var r = Assert.Single(reports);
        Assert.Equal(OneReportType.Imu, r.Type);
        Assert.Equal(0xABCDEFUL, r.DeviceId);
        Assert.Equal(123_456_789UL, r.TimestampNs);
        Assert.Equal(Gyro, r.Gyro);
        Assert.Equal(Accel, r.Accel);
        Assert.Equal(new Vector3(0.1f, 0.2f, 0.3f), r.Magnetometer);
        Assert.Equal(36.5f, r.TemperatureC);
        Assert.Equal(1, r.ImuId);
        Assert.Equal(0x030201, r.FrameId);
    }

    [Fact]
    public void HandlesGarbageAndByteByByteDelivery()
    {
        var stream = new List<byte> { 0x00, 0x28, 0x99, 0x36 };
        stream.AddRange(ReportBuilder.Build(OneReportType.Imu, 1, Gyro, Accel, magic0: 0x27));
        stream.AddRange(ReportBuilder.Build(OneReportType.Magnetometer, 2, Gyro, Accel));
        stream.AddRange(ReportBuilder.Build(OneReportType.Imu, 3, Gyro, Accel));

        var framer = new OneReportFramer();
        var reports = new List<OneReport>();
        foreach (byte b in stream)
        {
            framer.Append([b], reports.Add);
        }

        Assert.Equal([1UL, 2UL, 3UL], reports.Select(r => r.TimestampNs));
        Assert.Equal(2, framer.Stats.ImuReports);
        Assert.Equal(1, framer.Stats.MagnetometerReports);
        Assert.Equal(4, framer.Stats.DroppedBytes);
    }

    [Fact]
    public void SkipsWrongLengthAndUnknownType()
    {
        var stream = new List<byte>();
        stream.AddRange(ReportBuilder.Build(OneReportType.Imu, 1, Gyro, Accel, bodyLength: 64));
        stream.AddRange(ReportBuilder.Build((OneReportType)0x77, 2, Gyro, Accel));
        stream.AddRange(ReportBuilder.Build(OneReportType.Imu, 3, Gyro, Accel));

        var framer = new OneReportFramer();
        var reports = new List<OneReport>();
        framer.Append(stream.ToArray(), reports.Add);

        Assert.Equal(3UL, Assert.Single(reports).TimestampNs);
        Assert.Equal(1, framer.Stats.InvalidLength);
        Assert.Equal(1, framer.Stats.UnknownType);
    }

    [Fact]
    public void SplitAcrossChunks()
    {
        var bytes = ReportBuilder.Build(OneReportType.Imu, 7, Gyro, Accel);
        var framer = new OneReportFramer();
        var reports = new List<OneReport>();
        framer.Append(bytes.AsSpan(0, 50), reports.Add);
        Assert.Empty(reports);
        framer.Append(bytes.AsSpan(50), reports.Add);
        Assert.Single(reports);
    }
}
