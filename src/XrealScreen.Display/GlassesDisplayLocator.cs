using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Devices;

namespace XrealScreen.Display;

/// <summary>Finds the glasses among the active monitors by EDID.</summary>
public static class GlassesDisplayLocator
{
    public static DisplayMonitor? FindGlasses(IEnumerable<DisplayMonitor> monitors) =>
        monitors.FirstOrDefault(m => GlassesCatalog.FindDisplay(m.EdidManufacturer, m.EdidProductCode) is not null);

    public static GlassesModel? Identify(DisplayMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        return GlassesCatalog.FindDisplay(monitor.EdidManufacturer, monitor.EdidProductCode)?.Model;
    }
}
