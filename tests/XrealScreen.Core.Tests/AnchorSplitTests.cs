using XrealScreen.Core.Workspace;

namespace XrealScreen.Core.Tests;

public class AnchorSplitTests
{
    [Theory]
    [InlineData(3840, 1080, true)]   // 32:9
    [InlineData(2560, 1080, true)]   // 21:9
    [InlineData(1920, 2160, true)]   // 16:18
    [InlineData(1920, 1200, false)]  // UltraWide off
    [InlineData(1920, 1080, false)]
    public void DetectsUltrawideSignals(int w, int h, bool expected) =>
        Assert.Equal(expected, AnchorSplit.IsUltrawideSignal(new Resolution(w, h)));

    [Fact]
    public void Wide32x9_TwoScreens_Are1080p()
    {
        var r = AnchorSplit.Split(new Resolution(3840, 1080), 2);
        Assert.Equal([new PixelRect(0, 0, 1920, 1080), new PixelRect(1920, 0, 1920, 1080)], r);
    }

    [Fact]
    public void Wide32x9_ThreeScreens()
    {
        var r = AnchorSplit.Split(new Resolution(3840, 1080), 3);
        Assert.Equal(3, r.Count);
        Assert.All(r, s => Assert.Equal(1280, s.Width));
        Assert.Equal(2560, r[2].X);
    }

    [Fact]
    public void Tall16x18_IsStacked()
    {
        var r = AnchorSplit.Split(new Resolution(1920, 2160), 2);
        Assert.Equal([new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 1080, 1920, 1080)], r);
    }
}
