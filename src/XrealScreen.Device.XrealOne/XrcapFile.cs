using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace XrealScreen.Device.XrealOne;

/// <summary>
/// ".xrcap" = raw TCP bytes exactly as received, for offline protocol analysis and
/// re-decoding after parser changes. Layout: "XRCAP1\n" + UTF-8 metadata line + '\n',
/// then records [i64 LE host ticks (100 ns, UTC)][i32 LE length][bytes].
/// </summary>
public sealed class XrcapWriter : IAsyncDisposable
{
    public static readonly byte[] Magic = "XRCAP1\n"u8.ToArray();
    private readonly Stream _stream;

    private XrcapWriter(Stream stream) => _stream = stream;

    public static async Task<XrcapWriter> CreateAsync(string path, string metadata, CancellationToken cancellationToken = default)
    {
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024, useAsync: true);
        await stream.WriteAsync(Magic, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(metadata.Replace('\n', ' ') + "\n"), cancellationToken).ConfigureAwait(false);
        return new XrcapWriter(stream);
    }

    public async ValueTask WriteAsync(DateTime receivedUtc, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var header = new byte[12];
        BinaryPrimitives.WriteInt64LittleEndian(header, receivedUtc.Ticks);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8), data.Length);
        await _stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await _stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => _stream.DisposeAsync();
}

public static class XrcapReader
{
    public sealed record Chunk(DateTime ReceivedUtc, byte[] Data);

    public static async IAsyncEnumerable<Chunk> ReadAsync(string path, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        var magic = new byte[XrcapWriter.Magic.Length];
        await stream.ReadExactlyAsync(magic, cancellationToken).ConfigureAwait(false);
        if (!magic.AsSpan().SequenceEqual(XrcapWriter.Magic))
        {
            throw new InvalidDataException($"{path} is not an xrcap file.");
        }

        // Skip metadata line.
        int b;
        while ((b = stream.ReadByte()) is not -1 and not '\n')
        {
        }

        var header = new byte[12];
        while (true)
        {
            int read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
            if (read < header.Length)
            {
                yield break;
            }

            long ticks = BinaryPrimitives.ReadInt64LittleEndian(header);
            int length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8));
            var data = new byte[length];
            await stream.ReadExactlyAsync(data, cancellationToken).ConfigureAwait(false);
            yield return new Chunk(new DateTime(ticks, DateTimeKind.Utc), data);
        }
    }

    public static string ReadMetadata(string path)
    {
        using var stream = File.OpenRead(path);
        stream.Seek(XrcapWriter.Magic.Length, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadLine() ?? string.Empty;
    }
}
