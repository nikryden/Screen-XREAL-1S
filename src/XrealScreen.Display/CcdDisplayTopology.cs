using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Workspace;
using XrealScreen.Display.Native;

namespace XrealScreen.Display;

/// <summary>Reads the active display topology with the Windows CCD API (user session only).</summary>
public sealed class CcdDisplayTopology : IDisplayTopology
{
    public IReadOnlyList<DisplayMonitor> GetActiveMonitors()
    {
        var (paths, modes) = Query(CcdNative.QDC_ONLY_ACTIVE_PATHS);
        var result = new List<DisplayMonitor>(paths.Length);
        foreach (var path in paths)
        {
            if ((path.Flags & CcdNative.DISPLAYCONFIG_PATH_ACTIVE) == 0)
            {
                continue;
            }

            var target = GetTargetName(path.TargetInfo.AdapterId, path.TargetInfo.Id);
            string gdi = GetSourceGdiName(path.SourceInfo.AdapterId, path.SourceInfo.Id);

            var resolution = new Resolution(0, 0);
            int x = 0, y = 0;
            uint srcIdx = path.SourceInfo.ModeInfoIdx;
            if (srcIdx < modes.Length && modes[srcIdx].InfoType == DISPLAYCONFIG_MODE_INFO.TypeSource)
            {
                var m = modes[srcIdx];
                resolution = new Resolution((int)m.SourceWidth, (int)m.SourceHeight);
                x = m.SourcePositionX;
                y = m.SourcePositionY;
            }

            result.Add(new DisplayMonitor(
                gdi,
                target.FriendlyName,
                target.Manufacturer,
                target.ProductCode,
                target.DevicePath,
                resolution,
                x,
                y,
                Math.Round(path.TargetInfo.RefreshRate.Value, 2),
                IsPrimary: x == 0 && y == 0,
                OutputTechnologyName(path.TargetInfo.OutputTechnology),
                path.TargetInfo.AdapterId.Value,
                path.TargetInfo.Id));
        }

        return result;
    }

    public IReadOnlyList<DisplayMode> GetSupportedModes(string gdiDeviceName)
    {
        var modes = new HashSet<DisplayMode>();
        var dm = new DEVMODEW { Size = 220 };
        for (int i = 0; CcdNative.EnumDisplaySettings(gdiDeviceName, i, ref dm); i++)
        {
            modes.Add(new DisplayMode(new Resolution((int)dm.PelsWidth, (int)dm.PelsHeight), (int)dm.DisplayFrequency, (int)dm.BitsPerPel));
            dm = new DEVMODEW { Size = 220 };
        }

        return modes
            .OrderByDescending(m => m.Resolution.Width * m.Resolution.Height)
            .ThenByDescending(m => m.RefreshHz)
            .ThenByDescending(m => m.BitsPerPixel)
            .ToList();
    }

    public bool TrySetMode(string gdiDeviceName, DisplayMode mode)
    {
        var dm = new DEVMODEW
        {
            Size = 220,
            Fields = CcdNative.DM_PELSWIDTH | CcdNative.DM_PELSHEIGHT | CcdNative.DM_DISPLAYFREQUENCY,
            PelsWidth = (uint)mode.Resolution.Width,
            PelsHeight = (uint)mode.Resolution.Height,
            DisplayFrequency = (uint)mode.RefreshHz,
        };
        return CcdNative.ChangeDisplaySettingsEx(gdiDeviceName, ref dm, IntPtr.Zero, 0, IntPtr.Zero) == CcdNative.DISP_CHANGE_SUCCESSFUL;
    }

    public DisplayLayout CaptureLayout() => new(
        DateTimeOffset.Now,
        GetActiveMonitors()
            .Select(m => new MonitorPlacement(m.DevicePath, m.EdidId, m.FriendlyName, m.X, m.Y, m.Resolution.Width, m.Resolution.Height, m.RefreshHz, m.IsPrimary))
            .ToList());

    public LayoutApplyResult ApplyLayout(DisplayLayout layout, bool validateOnly = false)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var (paths, modes) = Query(CcdNative.QDC_ONLY_ACTIVE_PATHS);
        var devicePaths = paths.Select(p => GetTargetName(p.TargetInfo.AdapterId, p.TargetInfo.Id).DevicePath).ToArray();
        var result = LayoutPlanner.Plan(paths, modes, i => devicePaths[i], layout);

        uint flags = CcdNative.SDC_USE_SUPPLIED_DISPLAY_CONFIG | CcdNative.SDC_ALLOW_CHANGES |
                     (validateOnly ? CcdNative.SDC_VALIDATE : CcdNative.SDC_APPLY | CcdNative.SDC_SAVE_TO_DATABASE);
        int err = CcdNative.SetDisplayConfig((uint)paths.Length, paths, (uint)modes.Length, modes, flags);
        ThrowIfError(err, nameof(CcdNative.SetDisplayConfig));
        return result;
    }

    private static (DISPLAYCONFIG_PATH_INFO[] Paths, DISPLAYCONFIG_MODE_INFO[] Modes) Query(uint flags)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            int err = CcdNative.GetDisplayConfigBufferSizes(flags, out uint numPaths, out uint numModes);
            ThrowIfError(err, nameof(CcdNative.GetDisplayConfigBufferSizes));

            var paths = new DISPLAYCONFIG_PATH_INFO[numPaths];
            var modes = new DISPLAYCONFIG_MODE_INFO[numModes];
            err = CcdNative.QueryDisplayConfig(flags, ref numPaths, paths, ref numModes, modes, IntPtr.Zero);
            if (err == CcdNative.ERROR_INSUFFICIENT_BUFFER)
            {
                continue; // topology changed between the two calls
            }

            ThrowIfError(err, nameof(CcdNative.QueryDisplayConfig));
            Array.Resize(ref paths, (int)numPaths);
            Array.Resize(ref modes, (int)numModes);
            return (paths, modes);
        }

        throw new InvalidOperationException("Display topology kept changing during QueryDisplayConfig.");
    }

    private static unsafe string GetSourceGdiName(LUID adapter, uint id)
    {
        var req = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
        req.Header.Type = CcdNative.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
        req.Header.Size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME);
        req.Header.AdapterId = adapter;
        req.Header.Id = id;
        if (CcdNative.DisplayConfigGetDeviceInfo(ref req) != CcdNative.ERROR_SUCCESS)
        {
            return string.Empty;
        }

        return new string(req.ViewGdiDeviceName);
    }

    private static unsafe (string FriendlyName, string Manufacturer, int ProductCode, string DevicePath) GetTargetName(LUID adapter, uint id)
    {
        var req = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
        req.Header.Type = CcdNative.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        req.Header.Size = (uint)sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);
        req.Header.AdapterId = adapter;
        req.Header.Id = id;
        if (CcdNative.DisplayConfigGetDeviceInfo(ref req) != CcdNative.ERROR_SUCCESS)
        {
            return (string.Empty, string.Empty, -1, string.Empty);
        }

        bool idsValid = (req.Flags & DISPLAYCONFIG_TARGET_DEVICE_NAME.FlagEdidIdsValid) != 0;
        return (
            new string(req.MonitorFriendlyDeviceName),
            idsValid ? EdidIds.DecodeManufacturer(req.EdidManufactureId) : string.Empty,
            idsValid ? req.EdidProductCodeId : -1,
            new string(req.MonitorDevicePath));
    }

    private static string OutputTechnologyName(uint value) => value switch
    {
        0 => "VGA",
        4 => "DVI",
        5 => "HDMI",
        6 => "LVDS",
        10 => "DisplayPort",
        11 => "DisplayPort (embedded)",
        12 => "UDI",
        13 => "UDI (embedded)",
        14 => "SDTV dongle",
        15 => "Miracast",
        16 => "Indirect (wired)",
        17 => "Indirect (virtual)",
        18 => "DisplayPort (USB tunnel)",
        0x80000000 => "Internal",
        uint.MaxValue => "Other",
        _ => $"0x{value:X}",
    };

    private static void ThrowIfError(int err, string api)
    {
        if (err != CcdNative.ERROR_SUCCESS)
        {
            throw new System.ComponentModel.Win32Exception(err, $"{api} failed ({err}).");
        }
    }
}
