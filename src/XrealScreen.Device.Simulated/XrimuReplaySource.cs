using System.Diagnostics;
using System.Runtime.CompilerServices;
using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Recording;
using XrealScreen.Core.Tracking;

namespace XrealScreen.Device.Simulated;

/// <summary>Replays a recorded .xrimu file, optionally paced by its timestamps.</summary>
public sealed class XrimuReplaySource(string path, bool realTime = false) : IImuSource
{
    public string Name => $"Replay {Path.GetFileName(path)}";

    public async IAsyncEnumerable<ImuSample> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        long? firstNs = null;
        long start = Stopwatch.GetTimestamp();
        await foreach (var sample in XrimuFile.ReadAsync(path, cancellationToken).ConfigureAwait(false))
        {
            if (realTime)
            {
                firstNs ??= sample.TimestampNs;
                var due = TimeSpan.FromTicks((sample.TimestampNs - firstNs.Value) / 100);
                var wait = due - Stopwatch.GetElapsedTime(start);
                if (wait > TimeSpan.FromMilliseconds(2))
                {
                    await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
                }
            }

            yield return sample;
        }
    }
}
