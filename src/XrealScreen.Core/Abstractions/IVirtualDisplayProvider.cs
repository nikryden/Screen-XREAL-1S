using XrealScreen.Core.Workspace;

namespace XrealScreen.Core.Abstractions;

/// <summary>A virtual monitor created by this app.</summary>
/// <param name="ProviderId">Provider-specific identifier.</param>
/// <param name="GdiDeviceName">Windows device name (e.g. \\.\DISPLAY5) once the monitor is attached.</param>
public sealed record VirtualDisplay(string ProviderId, Resolution Resolution, int RefreshHz, string? GdiDeviceName);

/// <summary>
/// Creates and removes virtual monitors (Indirect Display Driver). The first implementation
/// wraps VirtualDrivers/Virtual-Display-Driver (ADR-0001); an own IddCx driver may replace it.
/// Implementations must only remove monitors they created.
/// </summary>
public interface IVirtualDisplayProvider
{
    string Name { get; }

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    Task<VirtualDisplay> AddAsync(Resolution resolution, int refreshHz, CancellationToken cancellationToken);

    Task RemoveAsync(VirtualDisplay display, CancellationToken cancellationToken);

    Task RemoveAllOwnedAsync(CancellationToken cancellationToken);
}
