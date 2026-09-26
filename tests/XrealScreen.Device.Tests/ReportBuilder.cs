using System.Buffers.Binary;
using System.Numerics;
using XrealScreen.Device.XrealOne;

namespace XrealScreen.Device.Tests;

/// <summary>Builds synthetic stream-port reports matching the documented layout.</summary>
internal static class ReportBuilder
{
    public static byte[] Build(OneReportType type, ulong timestampNs, Vector3 gyro, Vector3 accel, byte magic0 = 0x28, uint bodyLength = 128)
    {
        var bytes = new byte[OneReportFramer.HeaderBytes + 128];
        bytes[0] = magic0;
        bytes[1] = 0x36;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(2), bodyLength);
        var body = bytes.AsSpan(OneReportFramer.HeaderBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(body, 0xABCDEF);
        BinaryPrimitives.WriteUInt64LittleEndian(body[0x08..], timestampNs);
        BinaryPrimitives.WriteUInt32LittleEndian(body[0x18..], (uint)type);
        WriteVector(body, 0x1C, gyro);
        WriteVector(body, 0x28, accel);
        WriteVector(body, 0x34, new Vector3(0.1f, 0.2f, 0.3f));
        BinaryPrimitives.WriteSingleLittleEndian(body[0x40..], 36.5f);
        body[0x44] = 1;
        body[0x45] = 0x01;
        body[0x46] = 0x02;
        body[0x47] = 0x03;
        return bytes;
    }

    private static void WriteVector(Span<byte> body, int offset, Vector3 v)
    {
        BinaryPrimitives.WriteSingleLittleEndian(body[offset..], v.X);
        BinaryPrimitives.WriteSingleLittleEndian(body[(offset + 4)..], v.Y);
        BinaryPrimitives.WriteSingleLittleEndian(body[(offset + 8)..], v.Z);
    }
}
