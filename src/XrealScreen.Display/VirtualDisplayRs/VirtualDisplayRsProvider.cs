using System.IO.Pipes;
using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Workspace;

namespace XrealScreen.Display.VirtualDisplayRs;

/// <summary>
/// <see cref="IVirtualDisplayProvider"/> for the virtual-display-rs IddCx driver (ADR-0008).
/// The driver uses the monitor ID as the IddCx connector index, so IDs must be 0..15
/// ([verified-hw] out-of-range IDs fail with STATUS_INVALID_PARAMETER). Ownership is therefore
/// marked by the monitor name (<see cref="NamePrefix"/>), which the driver stores but ignores.
/// We allocate IDs from 15 downward; monitors of other apps (e.g. VertoXR) are preserved.
/// </summary>
public sealed class VirtualDisplayRsProvider : IVirtualDisplayProvider, IDisposable
{
    /// <summary>Highest connector index the driver supports (MAX_MONITORS = 16).</summary>
    public const uint MaxId = 15;

    /// <summary>Name prefix marking monitors created by XrealScreen.</summary>
    public const string NamePrefix = "XrealScreen";

    private readonly string _pipeName;
    private readonly TimeSpan _timeout;
    private readonly IDisplayTopology? _topology;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public VirtualDisplayRsProvider(IDisplayTopology? topology = null, string pipeName = VdrsProtocol.DefaultPipeName, TimeSpan? timeout = null)
    {
        _topology = topology;
        _pipeName = pipeName;
        _timeout = timeout ?? TimeSpan.FromSeconds(3);
    }

    public string Name => "virtual-display-rs";

    internal static bool IsOwned(VdrsMonitor monitor) => monitor.Name?.StartsWith(NamePrefix, StringComparison.Ordinal) == true;

    /// <summary>True when the display was created by XrealScreen (see <see cref="NamePrefix"/>).</summary>
    public static bool IsOwned(VirtualDisplay display) => display?.Name?.StartsWith(NamePrefix, StringComparison.Ordinal) == true;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = await GetStateAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>All monitors the driver currently knows (ours and others').</summary>
    public async Task<IReadOnlyList<VirtualDisplay>> GetAllAsync(CancellationToken cancellationToken)
    {
        var state = await GetStateAsync(cancellationToken).ConfigureAwait(false);
        return state.Select(ToVirtualDisplay).ToList();
    }

    public async Task<VirtualDisplay> AddAsync(Resolution resolution, int refreshHz, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var before = _topology?.GetActiveMonitors().Select(m => m.GdiDeviceName).ToHashSet() ?? [];
            var state = await GetStateAsync(cancellationToken).ConfigureAwait(false);
            uint? free = null;
            for (uint candidate = MaxId; candidate != uint.MaxValue; candidate--)
            {
                if (state.All(m => m.Id != candidate))
                {
                    free = candidate;
                    break;
                }
            }

            uint id = free ?? throw new InvalidOperationException($"virtual-display-rs supports at most {MaxId + 1} monitors; all are in use.");
            var monitor = new VdrsMonitor(id, $"{NamePrefix} {id}", true,
                [new VdrsMode((uint)resolution.Width, (uint)resolution.Height, [(uint)refreshHz])]);
            await SendAsync(VdrsProtocol.Notify([.. state, monitor]), cancellationToken).ConfigureAwait(false);

            string? gdi = await WaitForNewMonitorAsync(before, resolution, cancellationToken).ConfigureAwait(false);
            return new VirtualDisplay(id.ToString(System.Globalization.CultureInfo.InvariantCulture), resolution, refreshHz, gdi, monitor.Name);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(VirtualDisplay display, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(display);
        uint id = uint.Parse(display.ProviderId, System.Globalization.CultureInfo.InvariantCulture);
        var current = (await GetStateAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(m => m.Id == id);
        if (current is null)
        {
            return;
        }

        if (!IsOwned(current))
        {
            throw new InvalidOperationException($"Monitor {id} was not created by XrealScreen.");
        }

        await SendAsync(VdrsProtocol.Remove([id]), cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAllOwnedAsync(CancellationToken cancellationToken)
    {
        var owned = (await GetStateAsync(cancellationToken).ConfigureAwait(false)).Where(IsOwned).Select(m => m.Id).ToList();
        if (owned.Count > 0)
        {
            await SendAsync(VdrsProtocol.Remove(owned), cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<IReadOnlyList<VdrsMonitor>> GetStateAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);
        var buffer = new List<byte>(4096);
        var chunk = new byte[4096];
        try
        {
            await using var pipe = await ConnectAsync(cts.Token).ConfigureAwait(false);
            await pipe.WriteAsync(VdrsProtocol.State(), cts.Token).ConfigureAwait(false);
            await pipe.FlushAsync(cts.Token).ConfigureAwait(false);

            while (true)
            {
                int read = await pipe.ReadAsync(chunk, cts.Token).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new IOException("virtual-display-rs pipe closed before replying.");
                }

                for (int i = 0; i < read; i++)
                {
                    if (chunk[i] != VdrsProtocol.Terminator)
                    {
                        buffer.Add(chunk[i]);
                        continue;
                    }

                    var (tag, monitors) = VdrsProtocol.Parse(buffer.ToArray());
                    buffer.Clear();
                    if (tag == "State")
                    {
                        return monitors;
                    }

                    // "Changed" events from other clients' updates: skip.
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("virtual-display-rs did not reply to State in time (driver not installed or not running?).");
        }
    }

    private async Task SendAsync(byte[] message, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);
        try
        {
            await using var pipe = await ConnectAsync(cts.Token).ConfigureAwait(false);
            // One write per message: the driver side of the pipe is in message mode.
            await pipe.WriteAsync(message, cts.Token).ConfigureAwait(false);
            await pipe.FlushAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("virtual-display-rs pipe did not accept the command in time.");
        }
    }

    private async Task<NamedPipeClientStream> ConnectAsync(CancellationToken cancellationToken)
    {
        var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        try
        {
            await pipe.ConnectAsync(cancellationToken).ConfigureAwait(false);
            return pipe;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        catch
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<string?> WaitForNewMonitorAsync(HashSet<string> before, Resolution resolution, CancellationToken cancellationToken)
    {
        if (_topology is null)
        {
            return null;
        }

        // Windows attaches the new monitor asynchronously.
        for (int i = 0; i < 40; i++)
        {
            var added = _topology.GetActiveMonitors().FirstOrDefault(m => !before.Contains(m.GdiDeviceName) && m.Resolution == resolution)
                        ?? _topology.GetActiveMonitors().FirstOrDefault(m => !before.Contains(m.GdiDeviceName));
            if (added is not null)
            {
                return added.GdiDeviceName;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static VirtualDisplay ToVirtualDisplay(VdrsMonitor m)
    {
        var mode = m.Modes.Count > 0 ? m.Modes[0] : null;
        uint hz = mode is { RefreshRates.Count: > 0 } ? mode.RefreshRates[0] : 0;
        return new VirtualDisplay(
            m.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            new Resolution((int)(mode?.Width ?? 0), (int)(mode?.Height ?? 0)),
            (int)hz,
            GdiDeviceName: null,
            m.Name);
    }

    public void Dispose() => _gate.Dispose();
}
