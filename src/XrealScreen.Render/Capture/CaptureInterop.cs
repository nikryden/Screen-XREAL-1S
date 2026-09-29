using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace XrealScreen.Render.Capture;

/// <summary>
/// Raw COM glue between D3D11 (Vortice) and Windows.Graphics.Capture (WinRT).
/// Uses vtable calls instead of [ComImport] interfaces so it stays trim/AOT friendly.
/// </summary>
internal static unsafe partial class CaptureInterop
{
    private static readonly Guid IidGraphicsCaptureItemInterop = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
    private static readonly Guid IidGraphicsCaptureItem = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly Guid IidDirect3DDxgiInterfaceAccess = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
    private static readonly Guid IidD3D11Texture2D = new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    [LibraryImport("d3d11.dll")]
    private static partial int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromPoint(POINT pt, uint flags);

    /// <summary>HMONITOR for a point on the desktop (e.g. a monitor's top-left from CCD), or zero.</summary>
    public static IntPtr MonitorAt(int x, int y) => MonitorFromPoint(new POINT { X = x, Y = y }, 0 /* MONITOR_DEFAULTTONULL */);

    public static IDirect3DDevice CreateWinRtDevice(ID3D11Device device)
    {
        using var dxgi = device.QueryInterface<IDXGIDevice>();
        Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgi.NativePointer, out IntPtr inspectable));
        try
        {
            return MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
        }
        finally
        {
            Marshal.Release(inspectable);
        }
    }

    public static GraphicsCaptureItem CreateItemForMonitor(IntPtr hmonitor)
    {
        var factory = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
        Guid iid = IidGraphicsCaptureItemInterop;
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(factory.ThisPtr, in iid, out IntPtr interop));
        try
        {
            // IGraphicsCaptureItemInterop : IUnknown { CreateForWindow (3); CreateForMonitor (4) }
            var createForMonitor = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr*, int>)(*(IntPtr**)interop)[4];
            Guid itemIid = IidGraphicsCaptureItem;
            IntPtr result;
            Marshal.ThrowExceptionForHR(createForMonitor(interop, hmonitor, &itemIid, &result));
            try
            {
                return GraphicsCaptureItem.FromAbi(result);
            }
            finally
            {
                Marshal.Release(result);
            }
        }
        finally
        {
            Marshal.Release(interop);
        }
    }

    /// <summary>Returns the ID3D11Texture2D behind a capture frame surface (caller disposes).</summary>
    public static ID3D11Texture2D GetTexture(IDirect3DSurface surface)
    {
        IntPtr unknown = MarshalInterface<IDirect3DSurface>.FromManaged(surface);
        try
        {
            Guid accessIid = IidDirect3DDxgiInterfaceAccess;
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknown, in accessIid, out IntPtr access));
            try
            {
                // IDirect3DDxgiInterfaceAccess : IUnknown { GetInterface (3) }
                var getInterface = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)(*(IntPtr**)access)[3];
                Guid texIid = IidD3D11Texture2D;
                IntPtr texture;
                Marshal.ThrowExceptionForHR(getInterface(access, &texIid, &texture));
                return new ID3D11Texture2D(texture);
            }
            finally
            {
                Marshal.Release(access);
            }
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }
}
