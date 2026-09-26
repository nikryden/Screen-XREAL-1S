---
name: d3d11-vortice
description: Direct3D 11 with Vortice.Windows in .NET - creating the device on a specific adapter, fullscreen flip-model waitable swapchain on the glasses monitor, Windows.Graphics.Capture interop (IDirect3DDxgiInterfaceAccess, CreateDirect3D11DeviceFromDXGIDevice), frame pacing and late pose latch. Use when working on XrealScreen.Render or any GPU/capture code.
---

# D3D11 with Vortice

Packages: `Vortice.Direct3D11`, `Vortice.DXGI`, `Vortice.D3DCompiler` (or precompiled shaders), 3.8.x. WinRT capture via the Windows SDK projection of the TFM (`net10.0-windows10.0.26100.0`).

## Device on the right adapter
```csharp
using var factory = DXGI.CreateDXGIFactory2<IDXGIFactory6>(debug: false);
// Find the adapter that owns the glasses output (match HMONITOR from CCD/EnumDisplayMonitors)
for (uint i = 0; factory.EnumAdapters1(i, out var adapter).Success; i++)
    for (uint o = 0; adapter.EnumOutputs(o, out var output).Success; o++)
        if (output.Description.Monitor == glassesHmonitor) { chosen = adapter; /* keep */ }
D3D11.D3D11CreateDevice(chosen, DriverType.Unknown, DeviceCreationFlags.BgraSupport,
    new[] { FeatureLevel.Level_11_1, FeatureLevel.Level_11_0 }, out var device, out var ctx).CheckError();
```
- `DriverType.Unknown` is required when passing an adapter.
- `BgraSupport` is required for WGC interop.
- Tests: `DriverType.Warp` with a null adapter.
- VDD monitors must be on the same adapter; otherwise capture textures need cross-adapter copies (avoid; warn user).

## Swapchain (flip model, waitable)
```csharp
var desc = new SwapChainDescription1 {
    Width = w, Height = h, Format = Format.B8G8R8A8_UNorm, BufferCount = 2,
    BufferUsage = Usage.RenderTargetOutput, SwapEffect = SwapEffect.FlipDiscard,
    SampleDescription = new(1, 0), Scaling = Scaling.None,
    Flags = SwapChainFlags.FrameLatencyWaitableObject | SwapChainFlags.AllowTearing };
using var sc1 = factory.CreateSwapChainForHwnd(device, hwnd, desc);
var sc = sc1.QueryInterface<IDXGISwapChain2>();
sc.MaximumFrameLatency = 1;
var waitHandle = sc.FrameLatencyWaitableObject;
```
- Borderless popup window (`WS_POPUP`) exactly covering the glasses monitor rect → OS promotes to independent flip (check via PresentMon). Avoid exclusive fullscreen.
- Call `factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter)`.
- `ResizeBuffers` on mode change; release all back-buffer references first.

## Render loop (dedicated thread)
1. `WaitForSingleObjectEx(waitHandle, 1000, true)` — start of frame budget.
2. Update textures from capture (latest only).
3. **Late latch:** read the newest pose, predict to photon time (`now + renderTime + scanout ~ 1 refresh`).
4. Draw quads/curved meshes with view = inverse(head pose).
5. `Present(1, PresentFlags.None)` (vsync on glasses refresh, 120 Hz).
6. Stats: `GetFrameStatistics` → `PresentCount`, `SyncQPCTime`; pose-to-present latency histogram.
- Raise thread via MMCSS: `AvSetMmThreadCharacteristics("Games", ref idx)`.
- Never allocate per frame; reuse constant buffers (`Map(WriteDiscard)`).

## Windows.Graphics.Capture interop
```csharp
// D3D11 device -> WinRT IDirect3DDevice
using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var inspectable).ThrowOnFailure(); // d3d11.dll export
var winrtDevice = MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);

// Monitor -> GraphicsCaptureItem via IGraphicsCaptureItemInterop.CreateForMonitor(hmon, IID_IGraphicsCaptureItem)
var pool = Direct3D11CaptureFramePool.CreateFreeThreaded(winrtDevice,
    DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, item.Size);
var session = pool.CreateCaptureSession(item);
session.IsBorderRequired = false;       // needs graphicsCaptureWithoutBorder
session.IsCursorCaptureEnabled = true;
pool.FrameArrived += OnFrame; session.StartCapture();

// Frame surface -> ID3D11Texture2D
var access = frame.Surface.As<IDirect3DDxgiInterfaceAccess>(); // GUID A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1
var tex = new ID3D11Texture2D(access.GetInterface(typeof(ID3D11Texture2D).GUID));
```
- `CreateFreeThreaded` avoids needing a DispatcherQueue; `FrameArrived` runs on a worker thread.
- Copy (`CopyResource`) into our own per-monitor texture, then dispose the frame immediately; pool has only 2 buffers.
- Recreate the pool on `item.Size` change (`pool.Recreate`).
- HDR: use `R16G16B16A16Float` and tone-map (M8).
- Check support: `GraphicsCaptureSession.IsSupported()`; request access for borderless via `GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Borderless)`.
- Never write captured frames to disk.

## Pitfalls
- Device removed (`DXGI_ERROR_DEVICE_REMOVED`, driver update, GPU switch): recreate device, swapchain, capture sessions.
- Immediate context is not thread-safe: capture thread must use a separate context or `ID3D10Multithread.SetMultithreadProtected(true)`.
- COM lifetime: dispose every Vortice object; leaks keep the swapchain/window alive.
- Measure with PresentMon before optimizing.
