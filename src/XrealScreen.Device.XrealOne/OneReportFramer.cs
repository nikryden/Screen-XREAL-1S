// Framing and field offsets ported from Skarian/one-xr (MIT, Copyright (c) 2026 Neil Skaria),
// OneXrReportMessageParser.kt. See THIRD-PARTY-NOTICES.md and docs/legal/SOURCES.md.

using System.Buffers.Binary;
using System.Numerics;

namespace XrealScreen.Device.XrealOne;

/// <summary>Running parse counters (for diagnostics and M1 protocol verification).</summary>
public sealed class FramerStats
{
    public long BytesReceived { get; internal set; }
    public long DroppedBytes { get; internal set; }
    public long Reports { get; internal set; }
    public long ImuReports { get; internal set; }
    public long MagnetometerReports { get; internal set; }
    public long InvalidLength { get; internal set; }
    public long UnknownType { get; internal set; }

    public override string ToString() =>
        $"bytes={BytesReceived} reports={Reports} imu={ImuReports} mag={MagnetometerReports} dropped={DroppedBytes} badLen={InvalidLength} unknownType={UnknownType}";
}

/// <summary>
/// Splits the stream-port byte stream into reports.
/// Wire format: header [0x28|0x27, 0x36, u32 big-endian body length] + 128-byte little-endian body.
/// </summary>
public sealed class OneReportFramer
{
    public const int HeaderBytes = 6;
    public const int ReportBodyBytes = 128;
    private const byte PrimaryMagic0 = 0x28;
    private const byte AlternateMagic0 = 0x27;
    private const byte Magic1 = 0x36;
    private const int MaxPendingBytes = 128 * 1024;

    private byte[] _buffer = new byte[16 * 1024];
    private int _count;

    public FramerStats Stats { get; } = new();

    /// <summary>Appends received bytes and invokes <paramref name="onReport"/> for each complete report.</summary>
    public void Append(ReadOnlySpan<byte> chunk, Action<OneReport> onReport)
    {
        ArgumentNullException.ThrowIfNull(onReport);
        Stats.BytesReceived += chunk.Length;
        EnsureCapacity(_count + chunk.Length);
        chunk.CopyTo(_buffer.AsSpan(_count));
        _count += chunk.Length;

        int offset = 0;
        while (_count - offset >= HeaderBytes)
        {
            var pending = _buffer.AsSpan(offset, _count - offset);
            int header = FindHeader(pending);
            if (header < 0)
            {
                // Keep the last byte: it may be the first half of a split magic.
                Stats.DroppedBytes += pending.Length - 1;
                offset = _count - 1;
                break;
            }

            if (header > 0)
            {
                Stats.DroppedBytes += header;
                offset += header;
                continue;
            }

            uint bodyLength = BinaryPrimitives.ReadUInt32BigEndian(pending[2..]);
            if (bodyLength != ReportBodyBytes)
            {
                Stats.InvalidLength++;
                Stats.DroppedBytes++;
                offset++;
                continue;
            }

            if (pending.Length < HeaderBytes + ReportBodyBytes)
            {
                break;
            }

            if (TryDecodeBody(pending.Slice(HeaderBytes, ReportBodyBytes), out var report))
            {
                Stats.Reports++;
                if (report.Type == OneReportType.Imu)
                {
                    Stats.ImuReports++;
                }
                else
                {
                    Stats.MagnetometerReports++;
                }

                onReport(report);
            }
            else
            {
                Stats.UnknownType++;
            }

            offset += HeaderBytes + ReportBodyBytes;
        }

        Compact(offset);
    }

    internal static bool TryDecodeBody(ReadOnlySpan<byte> body, out OneReport report)
    {
        uint type = BinaryPrimitives.ReadUInt32LittleEndian(body[0x18..]);
        if (type is not ((uint)OneReportType.Imu or (uint)OneReportType.Magnetometer))
        {
            report = default;
            return false;
        }

        report = new OneReport(
            DeviceId: BinaryPrimitives.ReadUInt64LittleEndian(body),
            TimestampNs: BinaryPrimitives.ReadUInt64LittleEndian(body[0x08..]),
            Type: (OneReportType)type,
            Gyro: ReadVector(body, 0x1C),
            Accel: ReadVector(body, 0x28),
            Magnetometer: ReadVector(body, 0x34),
            TemperatureC: BinaryPrimitives.ReadSingleLittleEndian(body[0x40..]),
            ImuId: body[0x44],
            FrameId: body[0x45] | (body[0x46] << 8) | (body[0x47] << 16));
        return true;
    }

    private static Vector3 ReadVector(ReadOnlySpan<byte> body, int offset) => new(
        BinaryPrimitives.ReadSingleLittleEndian(body[offset..]),
        BinaryPrimitives.ReadSingleLittleEndian(body[(offset + 4)..]),
        BinaryPrimitives.ReadSingleLittleEndian(body[(offset + 8)..]));

    private static int FindHeader(ReadOnlySpan<byte> data)
    {
        for (int i = 0; i + 1 < data.Length; i++)
        {
            if ((data[i] == PrimaryMagic0 || data[i] == AlternateMagic0) && data[i + 1] == Magic1)
            {
                return i;
            }
        }

        return -1;
    }

    private void Compact(int consumed)
    {
        int remaining = _count - consumed;
        if (remaining > MaxPendingBytes)
        {
            Stats.DroppedBytes += remaining - MaxPendingBytes;
            consumed = _count - MaxPendingBytes;
            remaining = MaxPendingBytes;
        }

        if (consumed > 0 && remaining > 0)
        {
            Buffer.BlockCopy(_buffer, consumed, _buffer, 0, remaining);
        }

        _count = remaining;
    }

    private void EnsureCapacity(int size)
    {
        if (size > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(size, _buffer.Length * 2));
        }
    }
}
