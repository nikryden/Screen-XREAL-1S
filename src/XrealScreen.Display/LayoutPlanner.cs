using XrealScreen.Core.Abstractions;
using XrealScreen.Display.Native;

namespace XrealScreen.Display;

/// <summary>
/// Pure logic behind <see cref="CcdDisplayTopology.ApplyLayout"/>: writes saved positions and
/// resolutions into queried CCD mode arrays. Separate from P/Invoke so it can be unit-tested.
/// </summary>
internal static class LayoutPlanner
{
    /// <param name="paths">Active paths from QueryDisplayConfig.</param>
    /// <param name="modes">Mode array from the same query; modified in place.</param>
    /// <param name="devicePathOf">Monitor device path for the target of path i.</param>
    public static LayoutApplyResult Plan(DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes, Func<int, string> devicePathOf, DisplayLayout layout)
    {
        var byPath = layout.Monitors.ToDictionary(m => m.DevicePath, StringComparer.OrdinalIgnoreCase);
        var applied = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < paths.Length; i++)
        {
            string devicePath = devicePathOf(i);
            if (!byPath.TryGetValue(devicePath, out var saved))
            {
                continue;
            }

            seen.Add(devicePath);
            uint idx = paths[i].SourceInfo.ModeInfoIdx;
            if (idx >= modes.Length || modes[idx].InfoType != DISPLAYCONFIG_MODE_INFO.TypeSource)
            {
                continue;
            }

            modes[idx].SourcePositionX = saved.X;
            modes[idx].SourcePositionY = saved.Y;
            modes[idx].SourceWidth = (uint)saved.Width;
            modes[idx].SourceHeight = (uint)saved.Height;
            applied.Add(saved.FriendlyName.Length > 0 ? saved.FriendlyName : devicePath);
        }

        var missing = layout.Monitors.Where(m => !seen.Contains(m.DevicePath)).Select(m => m.FriendlyName.Length > 0 ? m.FriendlyName : m.DevicePath).ToList();
        ShiftOthersClear(paths, modes, devicePathOf, byPath);
        return new LayoutApplyResult(applied, missing);
    }

    /// <summary>
    /// Monitors that are not in the layout (e.g. virtual ones) are moved to the right of the restored
    /// desktop so they cannot overlap a restored monitor.
    /// </summary>
    private static void ShiftOthersClear(DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes, Func<int, string> devicePathOf, Dictionary<string, MonitorPlacement> saved)
    {
        if (saved.Count == 0)
        {
            return;
        }

        int right = saved.Values.Max(m => m.X + m.Width);
        int top = saved.Values.Min(m => m.Y);
        for (int i = 0; i < paths.Length; i++)
        {
            if (saved.ContainsKey(devicePathOf(i)))
            {
                continue;
            }

            uint idx = paths[i].SourceInfo.ModeInfoIdx;
            if (idx >= modes.Length || modes[idx].InfoType != DISPLAYCONFIG_MODE_INFO.TypeSource)
            {
                continue;
            }

            modes[idx].SourcePositionX = right;
            modes[idx].SourcePositionY = top;
            right += (int)modes[idx].SourceWidth;
        }
    }
}
