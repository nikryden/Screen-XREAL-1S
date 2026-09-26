using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace XrealScreen.Render;

/// <summary>Owns the D3D11 device shared by capture and rendering.</summary>
public sealed class GraphicsDevice : IDisposable
{
    private GraphicsDevice(ID3D11Device device, ID3D11DeviceContext context)
    {
        Device = device;
        Context = context;
        // Capture callbacks run on worker threads; serialise immediate-context use.
        using var multithread = device.QueryInterface<ID3D11Multithread>();
        multithread.SetMultithreadProtected(true);
    }

    public ID3D11Device Device { get; }

    public ID3D11DeviceContext Context { get; }

    /// <summary>
    /// Creates a BGRA-capable device on the default hardware adapter, or WARP for tests.
    /// TODO(M3): pick the adapter that owns the glasses output (skill d3d11-vortice).
    /// </summary>
    public static GraphicsDevice Create(bool warp = false)
    {
        D3D11.D3D11CreateDevice(
            null,
            warp ? DriverType.Warp : DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            [FeatureLevel.Level_11_1, FeatureLevel.Level_11_0],
            out ID3D11Device? device,
            out ID3D11DeviceContext? context).CheckError();
        return new GraphicsDevice(device!, context!);
    }

    public void Dispose()
    {
        Context.Dispose();
        Device.Dispose();
    }
}
