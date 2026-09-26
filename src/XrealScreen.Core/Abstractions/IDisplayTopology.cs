using XrealScreen.Core.Workspace;

namespace XrealScreen.Core.Abstractions;

/// <summary>An active monitor as Windows sees it (one CCD path).</summary>
/// <param name="GdiDeviceName">e.g. <c>\\.\DISPLAY2</c>. Unstable across reboots — never use as identity.</param>
/// <param name="EdidManufacturer">Three-letter PnP ID (e.g. <c>MRG</c> for XREAL), empty when unknown.</param>
/// <param name="EdidProductCode">EDID product code, or -1 when unknown.</param>
/// <param name="DevicePath">Monitor device interface path (stable identity together with EDID IDs).</param>
/// <param name="AdapterLuid">GPU adapter LUID (stable only for the current boot).</param>
public sealed record DisplayMonitor(
    string GdiDeviceName,
    string FriendlyName,
    string EdidManufacturer,
    int EdidProductCode,
    string DevicePath,
    Resolution Resolution,
    int X,
    int Y,
    double RefreshHz,
    bool IsPrimary,
    string OutputTechnology,
    long AdapterLuid,
    uint TargetId)
{
    public string EdidId => EdidProductCode < 0 ? EdidManufacturer : $"{EdidManufacturer}{EdidProductCode:X4}";
}

/// <summary>A display mode a monitor supports.</summary>
public readonly record struct DisplayMode(Resolution Resolution, int RefreshHz, int BitsPerPixel)
{
    public override string ToString() => $"{Resolution} @ {RefreshHz} Hz, {BitsPerPixel} bpp";
}

/// <summary>Reads (and in later M2 steps changes) the Windows display topology.</summary>
/// <summary>Where one monitor sits in the Windows desktop. Identity = <see cref="DevicePath"/>.</summary>
public sealed record MonitorPlacement(string DevicePath, string EdidId, string FriendlyName, int X, int Y, int Width, int Height, double RefreshHz, bool IsPrimary);

/// <summary>A saved desktop layout (active monitors only).</summary>
public sealed record DisplayLayout(DateTimeOffset CapturedAt, IReadOnlyList<MonitorPlacement> Monitors);

/// <summary>Result of applying a layout.</summary>
/// <param name="Applied">Monitors whose position/resolution were set.</param>
/// <param name="Missing">Saved monitors that are not active now (unplugged or disabled).</param>
public sealed record LayoutApplyResult(IReadOnlyList<string> Applied, IReadOnlyList<string> Missing);

/// <summary>Reads and changes the Windows display topology (user session only, ADR-0004).</summary>
public interface IDisplayTopology
{
    IReadOnlyList<DisplayMonitor> GetActiveMonitors();

    IReadOnlyList<DisplayMode> GetSupportedModes(string gdiDeviceName);

    DisplayLayout CaptureLayout();

    /// <summary>
    /// Restores positions and resolutions of the saved monitors that are still active.
    /// Monitors not in the layout (e.g. our virtual monitors) are left as they are.
    /// </summary>
    LayoutApplyResult ApplyLayout(DisplayLayout layout, bool validateOnly = false);

    /// <summary>
    /// Sets resolution and refresh rate of one monitor for this session only (not saved; reverts on
    /// reboot or when Windows re-enumerates the monitor). Returns false when Windows rejects the mode.
    /// </summary>
    bool TrySetMode(string gdiDeviceName, DisplayMode mode);
}
