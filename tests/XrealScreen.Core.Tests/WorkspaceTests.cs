using XrealScreen.Core.Workspace;

namespace XrealScreen.Core.Tests;

public class WorkspaceTests
{
    [Theory]
    [InlineData(UltrawideMode.Off, 1920, 1080)]
    [InlineData(UltrawideMode.Wide21x9, 2560, 1080)]
    [InlineData(UltrawideMode.Wide32x9, 3840, 1080)]
    [InlineData(UltrawideMode.Tall16x18, 1920, 2160)]
    public void UltrawideResolutions(UltrawideMode mode, int width, int height) =>
        Assert.Equal(new Resolution(width, height), UltrawideModes.GetResolution(mode));

    [Fact]
    public void Tall16x18_IsTwo16x9Stacked()
    {
        var r = UltrawideModes.GetResolution(UltrawideMode.Tall16x18);
        Assert.Equal(16.0 / 18.0, r.AspectRatio, 6);
    }

    [Fact]
    public void Arc_ThreeScreens_IsSymmetricAroundCenter()
    {
        var screens = Enumerable.Range(1, 3).Select(i => new ScreenSpec(i, UltrawideMode.Off)).ToList();
        var p = WorkspaceLayout.Compute(screens, new WorkspaceSettings());

        Assert.Equal(3, p.Count);
        Assert.Equal(0f, p[1].YawDegrees, 3);
        Assert.Equal(-p[0].YawDegrees, p[2].YawDegrees, 3);
        Assert.True(p[0].YawDegrees > 0, "first screen is on the left (positive yaw)");
        Assert.All(p, s => Assert.Equal(0f, s.PitchDegrees, 3));
    }

    [Fact]
    public void Grid_FourScreens_TwoRows()
    {
        var screens = Enumerable.Range(1, 4).Select(i => new ScreenSpec(i, UltrawideMode.Off)).ToList();
        var p = WorkspaceLayout.Compute(screens, new WorkspaceSettings { Preset = LayoutPreset.Grid });

        Assert.True(p[0].PitchDegrees > 0);
        Assert.True(p[3].PitchDegrees < 0);
        Assert.Equal(p[0].PitchDegrees, -p[3].PitchDegrees, 3);
    }

    [Fact]
    public void TooManyScreens_Throws()
    {
        var screens = Enumerable.Range(1, WorkspaceLayout.MaxScreens + 1).Select(i => new ScreenSpec(i, UltrawideMode.Off)).ToList();
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkspaceLayout.Compute(screens, new WorkspaceSettings()));
    }
}
