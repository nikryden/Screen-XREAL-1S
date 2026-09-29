using XrealScreen.Core.Abstractions;
using XrealScreen.Display;
using XrealScreen.Display.VirtualDisplayRs;

namespace XrealScreen.Host;

/// <summary>
/// Cleans up after a session that did not end normally (app crash, power loss, killed process):
/// removes our virtual monitors and restores the saved desktop layout including refresh rates.
/// Run at app start before a new session (M6 crash-safe restore).
/// </summary>
public static class WorkspaceRecovery
{
    /// <summary>Returns a user-facing message when something was restored, or null when the desktop was clean.</summary>
    public static async Task<string?> RecoverAsync(CancellationToken cancellationToken)
    {
        var store = new LayoutSnapshotStore();
        var topology = new CcdDisplayTopology();
        using var provider = new VirtualDisplayRsProvider(topology);

        int leftovers = 0;
        if (await provider.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            leftovers = (await provider.GetAllAsync(cancellationToken).ConfigureAwait(false)).Count(VirtualDisplayRsProvider.IsOwned);
        }

        if (leftovers == 0 && !store.Exists)
        {
            return null;
        }

        if (leftovers > 0)
        {
            await provider.RemoveAllOwnedAsync(cancellationToken).ConfigureAwait(false);
            await Task.Delay(1500, cancellationToken).ConfigureAwait(false);
        }

        var layout = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (layout is not null)
        {
            topology.ApplyLayout(layout);
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            RestoreRefreshRates(topology, layout);
            store.Delete();
        }

        return $"Restored your monitors after an unexpected stop ({leftovers} leftover virtual screen(s) removed{(layout is null ? string.Empty : ", layout and refresh rates restored")}).";
    }

    private static void RestoreRefreshRates(CcdDisplayTopology topology, DisplayLayout layout)
    {
        foreach (var saved in layout.Monitors)
        {
            var now = topology.GetActiveMonitors().FirstOrDefault(m => string.Equals(m.DevicePath, saved.DevicePath, StringComparison.OrdinalIgnoreCase));
            if (now is not null && Math.Abs(now.RefreshHz - saved.RefreshHz) > 1 && now.Resolution.Width == saved.Width && now.Resolution.Height == saved.Height)
            {
                topology.TrySetMode(now.GdiDeviceName, new DisplayMode(now.Resolution, (int)Math.Round(saved.RefreshHz), 32));
            }
        }
    }
}
