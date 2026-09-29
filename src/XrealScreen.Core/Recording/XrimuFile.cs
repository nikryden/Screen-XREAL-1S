using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrealScreen.Core.Tracking;

namespace XrealScreen.Core.Recording;

/// <summary>
/// ".xrimu" = decoded IMU samples as CSV (human- and diff-readable fixtures).
/// Line 1: "# xrimu v1" followed by key=value metadata; line 2: column header.
/// </summary>
public static class XrimuFile
{
    public const string Magic = "# xrimu v1";
    public const string Header = "t_ns,gx,gy,gz,ax,ay,az,temp_c";

    public static async Task WriteAsync(string path, IAsyncEnumerable<ImuSample> samples, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(path, append: false);
        await WriteHeaderAsync(writer, metadata).ConfigureAwait(false);
        await foreach (var s in samples.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await writer.WriteLineAsync(FormatLine(s)).ConfigureAwait(false);
        }
    }

    public static async Task WriteHeaderAsync(TextWriter writer, IReadOnlyDictionary<string, string>? metadata)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var meta = metadata is null ? string.Empty : " " + string.Join(' ', metadata.Select(kv => $"{kv.Key}={kv.Value.Replace(' ', '_')}"));
        await writer.WriteLineAsync(Magic + meta).ConfigureAwait(false);
        await writer.WriteLineAsync(Header).ConfigureAwait(false);
    }

    public static string FormatLine(in ImuSample s) => string.Create(CultureInfo.InvariantCulture,
        $"{s.TimestampNs},{s.Gyro.X:R},{s.Gyro.Y:R},{s.Gyro.Z:R},{s.Accel.X:R},{s.Accel.Y:R},{s.Accel.Z:R},{s.TemperatureC:R}");

    public static async IAsyncEnumerable<ImuSample> ReadAsync(string path, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(path);
        var first = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (first is null || !first.StartsWith(Magic, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{path} is not an xrimu v1 file.");
        }

        _ = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false); // column header
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            yield return ParseLine(line);
        }
    }

    public static IReadOnlyDictionary<string, string> ReadMetadata(string path)
    {
        using var reader = new StreamReader(path);
        var first = reader.ReadLine() ?? string.Empty;
        return first.Length <= Magic.Length
            ? new Dictionary<string, string>()
            : first[Magic.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);
    }

    public static ImuSample ParseLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var p = line.Split(',');
        if (p.Length < 8)
        {
            throw new FormatException($"Expected 8 columns: '{line}'");
        }

        var ci = CultureInfo.InvariantCulture;
        return new ImuSample(
            long.Parse(p[0], ci),
            new Vector3(float.Parse(p[1], ci), float.Parse(p[2], ci), float.Parse(p[3], ci)),
            new Vector3(float.Parse(p[4], ci), float.Parse(p[5], ci), float.Parse(p[6], ci)),
            float.Parse(p[7], ci));
    }
}
