using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Workspace;

namespace XrealScreen.Display.Tests;

public class WorkspaceArrangementTests
{
    private static DisplayMonitor M(string gdi, string path, int x, int w, bool primary = false) =>
        new(gdi, gdi, "X", 1, path, new Resolution(w, 1080), x, 0, 60, primary, "HDMI", 1, 1);

    [Fact]
    public void VirtualMonitorsGoNextToDesk_GlassesLast()
    {
        var monitors = new[]
        {
            M("D1", "desk", 0, 1920, primary: true),
            M("G", "glasses", 1920, 1920),
            M("V1", "v1", 3840, 1920),
            M("V2", "v2", 5760, 3840),
        };

        var layout = WorkspaceArrangement.Arrange(monitors, ["V1", "V2"], "G");
        int X(string path) => layout.Monitors.Single(m => m.DevicePath == path).X;

        Assert.Equal(0, X("desk"));
        Assert.Equal(1920, X("v1"));
        Assert.Equal(3840, X("v2"));
        Assert.Equal(7680, X("glasses"));
    }

    [Fact]
    public void GlassesPrimary_VirtualMonitorsGoLeftOfDesk_NoOverlap()
    {
        // Windows made the glasses primary (21:9): desk at -1920, glasses at 0.
        var monitors = new[]
        {
            M("D1", "desk", -1920, 1920),
            M("G", "glasses", 0, 2560, primary: true),
            M("V1", "v1", 2560, 1270),
            M("V2", "v2", 3830, 1270),
        };

        var layout = WorkspaceArrangement.Arrange(monitors, ["V1", "V2"], "G");
        int X(string path) => layout.Monitors.Single(m => m.DevicePath == path).X;

        Assert.Equal(0, X("glasses"));
        Assert.Equal(-1920, X("desk"));
        Assert.Equal(-1920 - 2540, X("v1"));
        Assert.Equal(-1920 - 1270, X("v2"));
        var spans = layout.Monitors.Select(m => (m.X, End: m.X + m.Width)).OrderBy(s => s.X).ToList();
        for (int i = 1; i < spans.Count; i++)
        {
            Assert.True(spans[i].X >= spans[i - 1].End, "no overlapping monitors");
        }
    }

    [Fact]
    public void DeskOnTheLeftOfPrimary_IsKept()
    {
        var monitors = new[]
        {
            M("D2", "left-desk", -1920, 1920),
            M("D1", "desk", 0, 1920, primary: true),
            M("G", "glasses", 1920, 1920),
            M("V1", "v1", 3840, 1920),
        };

        var layout = WorkspaceArrangement.Arrange(monitors, ["V1"], "G");
        Assert.Equal(-1920, layout.Monitors.Single(m => m.DevicePath == "left-desk").X);
        Assert.Equal(1920, layout.Monitors.Single(m => m.DevicePath == "v1").X);
        Assert.Equal(3840, layout.Monitors.Single(m => m.DevicePath == "glasses").X);
    }
}
