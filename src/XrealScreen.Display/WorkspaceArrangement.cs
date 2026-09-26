using XrealScreen.Core.Abstractions;

namespace XrealScreen.Display;

/// <summary>
/// Desktop order for the virtual workspace: [desk monitors][virtual 1..n][glasses].
/// The glasses area is covered by the 3D window, so a mouse pointer there is head-locked and
/// clicks do nothing ([verified-hw] user feedback 2026-09-26). Putting the virtual monitors right
/// next to the desk monitors means the pointer reaches them without crossing the glasses area.
/// </summary>
public static class WorkspaceArrangement
{
    /// <param name="monitors">Active monitors.</param>
    /// <param name="virtualGdiNames">Our virtual monitors, in left-to-right workspace order.</param>
    /// <param name="glassesGdiName">The glasses output.</param>
    public static DisplayLayout Arrange(IReadOnlyList<DisplayMonitor> monitors, IReadOnlyList<string> virtualGdiNames, string glassesGdiName)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        ArgumentNullException.ThrowIfNull(virtualGdiNames);
        var placements = new List<MonitorPlacement>();
        var fixedMonitors = monitors.Where(m => m.GdiDeviceName != glassesGdiName && !virtualGdiNames.Contains(m.GdiDeviceName)).ToList();
        foreach (var m in fixedMonitors)
        {
            placements.Add(Place(m, m.X, m.Y));
        }

        int x = fixedMonitors.Count == 0 ? 0 : fixedMonitors.Max(m => m.X + m.Resolution.Width);
        int y = fixedMonitors.FirstOrDefault(m => m.IsPrimary)?.Y ?? 0;
        foreach (var name in virtualGdiNames)
        {
            var m = monitors.FirstOrDefault(v => v.GdiDeviceName == name);
            if (m is null)
            {
                continue;
            }

            placements.Add(Place(m, x, y));
            x += m.Resolution.Width;
        }

        var glasses = monitors.FirstOrDefault(m => m.GdiDeviceName == glassesGdiName);
        if (glasses is not null)
        {
            // If the glasses are the only/primary monitor they must stay at (0,0).
            placements.Add(glasses.IsPrimary ? Place(glasses, glasses.X, glasses.Y) : Place(glasses, x, y));
        }

        return new DisplayLayout(DateTimeOffset.Now, placements);
    }

    private static MonitorPlacement Place(DisplayMonitor m, int x, int y) =>
        new(m.DevicePath, m.EdidId, m.FriendlyName, x, y, m.Resolution.Width, m.Resolution.Height, m.RefreshHz, x == 0 && y == 0);
}
