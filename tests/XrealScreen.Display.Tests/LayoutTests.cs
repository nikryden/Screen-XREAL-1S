using XrealScreen.Core.Abstractions;
using XrealScreen.Display.Native;

namespace XrealScreen.Display.Tests;

public class LayoutTests
{
    private static (DISPLAYCONFIG_PATH_INFO[] Paths, DISPLAYCONFIG_MODE_INFO[] Modes) Topology(params (int X, int Y, int W, int H)[] sources)
    {
        var paths = new DISPLAYCONFIG_PATH_INFO[sources.Length];
        var modes = new DISPLAYCONFIG_MODE_INFO[sources.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            paths[i].SourceInfo.ModeInfoIdx = (uint)i;
            modes[i].InfoType = DISPLAYCONFIG_MODE_INFO.TypeSource;
            modes[i].SourcePositionX = sources[i].X;
            modes[i].SourcePositionY = sources[i].Y;
            modes[i].SourceWidth = (uint)sources[i].W;
            modes[i].SourceHeight = (uint)sources[i].H;
        }

        return (paths, modes);
    }

    private static MonitorPlacement Saved(string path, int x, int y, int w, int h) =>
        new(path, "EDID", path, x, y, w, h, 60, x == 0 && y == 0);

    [Fact]
    public void Plan_RestoresPositionsAndResolutions_ByDevicePath()
    {
        // Now: glasses primary at 0,0 in 21:9, desk monitor to the right.
        var (paths, modes) = Topology((0, 0, 2560, 1080), (2560, 0, 1920, 1080));
        string[] ids = ["glasses", "desk"];
        var layout = new DisplayLayout(DateTimeOffset.Now, [Saved("desk", 0, 0, 1920, 1080), Saved("glasses", 1920, 0, 1920, 1200)]);

        var result = LayoutPlanner.Plan(paths, modes, i => ids[i], layout);

        Assert.Equal((1920, 0, 1920u, 1200u), (modes[0].SourcePositionX, modes[0].SourcePositionY, modes[0].SourceWidth, modes[0].SourceHeight));
        Assert.Equal((0, 0), (modes[1].SourcePositionX, modes[1].SourcePositionY));
        Assert.Equal(2, result.Applied.Count);
        Assert.Empty(result.Missing);
    }

    [Fact]
    public void Plan_MovesUnknownMonitorsRightOfRestoredDesktop()
    {
        var (paths, modes) = Topology((0, 0, 1920, 1080), (1920, 0, 3840, 1080), (5760, 0, 1920, 2160));
        string[] ids = ["desk", "virtual-a", "virtual-b"];
        var layout = new DisplayLayout(DateTimeOffset.Now, [Saved("desk", -1920, 0, 1920, 1080), Saved("glasses", 0, 0, 1920, 1200)]);

        var result = LayoutPlanner.Plan(paths, modes, i => ids[i], layout);

        Assert.Equal(-1920, modes[0].SourcePositionX);
        Assert.Equal(1920, modes[1].SourcePositionX); // right edge of saved layout = glasses 0..1920
        Assert.Equal(1920 + 3840, modes[2].SourcePositionX);
        Assert.Equal(["glasses"], result.Missing);
    }

    [Fact]
    public async Task SnapshotStore_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        string file = Path.Combine(Path.GetTempPath(), $"xrs-layout-{Guid.NewGuid():N}.json");
        var store = new LayoutSnapshotStore(file);
        var layout = new DisplayLayout(DateTimeOffset.Now, [Saved(@"\\?\DISPLAY#MRG4102#x", 0, 0, 1920, 1200)]);

        await store.SaveAsync(layout, ct);
        var loaded = await store.LoadAsync(ct);
        store.Delete();

        Assert.Equal(layout.Monitors, loaded!.Monitors);
        Assert.False(store.Exists);
    }
}
