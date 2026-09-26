namespace XrealScreen.Core.Devices;

/// <summary>
/// Glasses models. Numeric values mirror the XREAL SDK's device-type enum so logs are
/// comparable (facts only, see docs/findings/xreal-sdk.md). Do not renumber.
/// </summary>
public enum GlassesModel
{
    Unknown = 0,
    Light = 1,
    Air = 2,
    Air2Pro = 3,
    Air2 = 4,
    Air2Ultra = 5,
    One = 10,
    OnePro = 11,
    XReal1S = 15,
}

public enum GlassesTransport
{
    /// <summary>IMU over USB HID (Air series). Not implemented yet.</summary>
    Hid,

    /// <summary>IMU/control over the USB network adapter (One series, 1S assumed).</summary>
    Network,
}

public sealed record GlassesProduct(int ProductId, GlassesModel Model, GlassesTransport Transport, string Source);

public static class GlassesCatalog
{
    public const int XrealVendorId = 0x3318;

    /// <summary>
    /// Known product IDs. "Source" states where the mapping comes from; entries tagged
    /// [from-GPL:facts-only] are facts taken from XRLinuxDriver, no code.
    /// </summary>
    public static IReadOnlyList<GlassesProduct> Products { get; } =
    [
        new(0x0435, GlassesModel.OnePro, GlassesTransport.Network, "[from-GPL:facts-only] XRLinuxDriver"),
        new(0x0436, GlassesModel.OnePro, GlassesTransport.Network, "[from-GPL:facts-only] XRLinuxDriver"),
        new(0x0437, GlassesModel.One, GlassesTransport.Network, "[from-GPL:facts-only] XRLinuxDriver"),
        new(0x0438, GlassesModel.One, GlassesTransport.Network, "[from-GPL:facts-only] XRLinuxDriver"),
        new(0x043D, GlassesModel.XReal1S, GlassesTransport.Network, "[from-GPL:facts-only] XRLinuxDriver; [hypothesis] transport"),
        new(0x043E, GlassesModel.XReal1S, GlassesTransport.Network, "[from-GPL:facts-only] XRLinuxDriver; [hypothesis] transport"),
    ];

    public static GlassesProduct? Find(int vendorId, int productId) =>
        vendorId == XrealVendorId ? Products.FirstOrDefault(p => p.ProductId == productId) : null;
}
