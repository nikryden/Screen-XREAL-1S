using XrealScreen.Core.Tracking;

namespace XrealScreen.Core.Abstractions;

/// <summary>A stream of IMU samples: real glasses, a recording, or a simulator.</summary>
public interface IImuSource
{
    string Name { get; }

    /// <summary>Streams samples until cancelled or the source ends.</summary>
    IAsyncEnumerable<ImuSample> ReadAsync(CancellationToken cancellationToken);
}
