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
public interface IDisplayTopology
{
    IReadOnlyList<DisplayMonitor> GetActiveMonitors();

    IReadOnlyList<DisplayMode> GetSupportedModes(string gdiDeviceName);
}
