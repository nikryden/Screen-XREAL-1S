using System.Numerics;
using Vortice.Direct3D11;
using Vortice.DXGI;
using XrealScreen.Core.Workspace;
using XrealScreen.Render.Scene;

namespace XrealScreen.Render.Tests;

public sealed class NeckModelTests : IDisposable
{
    private const int W = 160, H = 100;
    private const uint Red = 0xFFFF0000;

    private readonly GraphicsDevice _gd = GraphicsDevice.Create(warp: true);
    private readonly WorkspaceScene _scene;
    private readonly ID3D11Texture2D _target;
    private readonly ID3D11RenderTargetView _rtv;
    private readonly ID3D11Texture2D _staging;

    public NeckModelTests()
    {
        _scene = new WorkspaceScene(_gd);
        _target = _gd.Device.CreateTexture2D(new Texture2DDescription(Format.B8G8R8A8_UNorm, W, H, 1, 1, BindFlags.RenderTarget));
        _rtv = _gd.Device.CreateRenderTargetView(_target);
        _staging = _gd.Device.CreateTexture2D(new Texture2DDescription(Format.B8G8R8A8_UNorm, W, H, 1, 1, BindFlags.None, ResourceUsage.Staging, CpuAccessFlags.Read));
    }

    // Looking down = positive rotation about +Y (left) in the FLU body frame.
    private static Quaternion LookDown(float deg) => Quaternion.CreateFromAxisAngle(Vector3.UnitY, deg * MathF.PI / 180f);

    [Fact]
    public void EyeOffset_IsZeroLookingStraight_AndForwardDownWhenLookingDown()
    {
        Assert.Equal(Vector3.Zero, ViewMath.EyeOffset(Quaternion.Identity, ViewMath.DefaultNeckToEye));
        var e = ViewMath.EyeOffset(LookDown(30), ViewMath.DefaultNeckToEye);
        Assert.True(e.X > 0.03f, $"eyes move forward: {e}");
        Assert.True(e.Z < -0.03f, $"eyes move down: {e}");
    }

    [Fact]
    public void LeaningIn_MakesScreenBelowLarger()
    {
        AddRedScreen(new ScreenPlacement(1, 0, -30, 1.2f, 0.675f, 1.5f));
        int without = RedPixels(LookDown(30), Vector3.Zero);
        int with = RedPixels(LookDown(30), ViewMath.DefaultNeckToEye);
        Assert.True(with > without * 1.02, $"neck model {with} px vs {without} px");
    }

    [Fact]
    public void SetPlacements_Closer_MakesScreenLarger()
    {
        AddRedScreen(new ScreenPlacement(1, 0, 0, 0.6f, 0.34f, 1.5f));
        int far = RedPixels(Quaternion.Identity, Vector3.Zero);
        _scene.SetPlacements([new ScreenPlacement(1, 0, 0, 0.6f, 0.34f, 1.0f)]);
        int near = RedPixels(Quaternion.Identity, Vector3.Zero);
        Assert.True(near > far * 1.5, $"closer {near} px vs farther {far} px");
    }

    private void AddRedScreen(ScreenPlacement placement)
    {
        var s = _scene.AddScreen(placement, 16, 9);
        _gd.Context.UpdateSubresource(Enumerable.Repeat(Red, 16 * 9).ToArray(), s.Texture, 0, 16 * 4, 0);
    }

    private int RedPixels(Quaternion head, Vector3 neckToEye)
    {
        _scene.Render(_rtv, W, H, ViewMath.ViewProjection(head, GlassesOptics.Xreal1S, neckToEye: neckToEye));
        _gd.Context.CopyResource(_staging, _target);
        var map = _gd.Context.Map(_staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            int count = 0;
            unsafe
            {
                for (int y = 0; y < H; y++)
                {
                    var row = new ReadOnlySpan<uint>((byte*)map.DataPointer + y * map.RowPitch, W);
                    foreach (uint px in row)
                    {
                        count += px == Red ? 1 : 0;
                    }
                }
            }

            return count;
        }
        finally
        {
            _gd.Context.Unmap(_staging, 0);
        }
    }

    public void Dispose()
    {
        _staging.Dispose();
        _rtv.Dispose();
        _target.Dispose();
        _scene.Dispose();
        _gd.Dispose();
    }
}
