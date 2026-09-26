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

    /// <summary>Human-readable adapter name (for logs).</summary>
    public string AdapterName
    {
        get
        {
            using var dxgi = Device.QueryInterface<Vortice.DXGI.IDXGIDevice>();
            using var adapter = dxgi.GetAdapter();
            return adapter.Description.Description;
        }
    }

    /// <summary>
    /// Creates the device on the GPU whose output drives <paramref name="hmonitor"/> (the glasses),
    /// so presenting needs no cross-adapter copy. Falls back to the default adapter.
    /// </summary>
    public static GraphicsDevice CreateForMonitor(IntPtr hmonitor)
    {
        using var factory = Vortice.DXGI.DXGI.CreateDXGIFactory1<Vortice.DXGI.IDXGIFactory1>();
        for (uint i = 0; factory.EnumAdapters1(i, out Vortice.DXGI.IDXGIAdapter1? adapter).Success; i++)
        {
            using (adapter)
            {
                bool owns = false;
                for (uint o = 0; adapter!.EnumOutputs(o, out Vortice.DXGI.IDXGIOutput? output).Success; o++)
                {
                    using (output)
                    {
                        owns |= output!.Description.Monitor == hmonitor;
                    }
                }

                if (owns)
                {
                    D3D11.D3D11CreateDevice(
                        adapter,
                        DriverType.Unknown,
                        DeviceCreationFlags.BgraSupport,
                        [FeatureLevel.Level_11_1, FeatureLevel.Level_11_0],
                        out ID3D11Device? device,
                        out ID3D11DeviceContext? context).CheckError();
                    return new GraphicsDevice(device!, context!);
                }
            }
        }

        return Create();
    }

    public void Dispose()
    {
        Context.Dispose();
        Device.Dispose();
    }
}
