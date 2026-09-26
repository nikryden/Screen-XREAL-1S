using System.Buffers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Tracking;

namespace XrealScreen.Device.XrealOne;

public sealed record XrealOneImuOptions
{
    public OneEndpoints Endpoints { get; init; } = OneEndpoints.Default;

    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>Sensor → body mapping for the gyro. [hypothesis] identity, verify in M1.</summary>
    public AxisMap GyroAxes { get; init; } = AxisMap.Identity;

    /// <summary>
    /// Sensor → body mapping for the accelerometer. [from-source:MIT] one-xr remaps accel
    /// as (z, y, x) into its gyro frame; verify in M1.
    /// </summary>
    public AxisMap AccelAxes { get; init; } = AxisMap.Parse("+z,+y,+x");

    /// <summary>Optional tap for raw bytes (e.g. an <see cref="XrcapWriter"/>).</summary>
    public Func<DateTime, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? RawTap { get; init; }
}

/// <summary>Streams IMU samples from One-series glasses over TCP (stream port).</summary>
public sealed class XrealOneImuSource : IImuSource
{
    private readonly XrealOneImuOptions _options;

    public XrealOneImuSource(XrealOneImuOptions? options = null) => _options = options ?? new XrealOneImuOptions();

    public string Name => $"XREAL One-series @ {_options.Endpoints.Host}:{_options.Endpoints.StreamPort}";

    public OneReportFramer Framer { get; } = new();

    public async IAsyncEnumerable<ImuSample> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<ImuSample>(new BoundedChannelOptions(4096)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.DropOldest,
        });

        var pump = PumpAsync(channel.Writer, cancellationToken);
        await foreach (var sample in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return sample;
        }

        await pump.ConfigureAwait(false);
    }

    private async Task PumpAsync(ChannelWriter<ImuSample> writer, CancellationToken cancellationToken)
    {
        Exception? error = null;
        try
        {
            using var client = new TcpClient { NoDelay = true };
            using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                connectCts.CancelAfter(_options.ConnectTimeout);
                await client.ConnectAsync(_options.Endpoints.Host, _options.Endpoints.StreamPort, connectCts.Token).ConfigureAwait(false);
            }

            await using var stream = client.GetStream();
            var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }

                    if (_options.RawTap is { } tap)
                    {
                        await tap(DateTime.UtcNow, buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    }

                    Framer.Append(buffer.AsSpan(0, read), report =>
                    {
                        if (report.Type == OneReportType.Imu)
                        {
                            writer.TryWrite(ToSample(report, _options));
                        }
                    });
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            error = new IOException($"IMU stream {_options.Endpoints.Host}:{_options.Endpoints.StreamPort} failed: {ex.Message}", ex);
        }
        finally
        {
            writer.TryComplete(error);
        }
    }

    public static ImuSample ToSample(in OneReport report, XrealOneImuOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new ImuSample(
            (long)report.TimestampNs,
            options.GyroAxes.Apply(report.Gyro),
            options.AccelAxes.Apply(report.Accel),
            report.TemperatureC);
    }
}
