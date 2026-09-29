using XrealScreen.Core.Tracking;

namespace XrealScreen.Host.Tests;

public sealed class WorkspaceLibraryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xrs-lib-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private WorkspaceLibrary NewLibrary(string sub = "workspaces") => new(Path.Combine(_root, sub));

    [Theory]
    [InlineData("  Desk  ", "Desk")]
    [InlineData("Office 3 screens", "Office 3 screens")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("a/b", null)]
    [InlineData("what?", null)]
    [InlineData("dot.", null)]
    public void NormalizeName_TrimsAndRejectsInvalidFileNames(string input, string? expected) =>
        Assert.Equal(expected, WorkspaceLibrary.NormalizeName(input));

    [Fact]
    public void SaveLoadListDelete_RoundTrips()
    {
        var lib = NewLibrary();
        Assert.Empty(lib.List());

        var desk = new WorkspaceOptions { Kind = WorkspaceKind.GlassesAnchor, ScreenCount = 2, AnchorGapPixels = 24 };
        lib.Save("Desk", desk);
        lib.Save("couch", new WorkspaceOptions { ScreenCount = 1 });

        Assert.Equal(["couch", "Desk"], lib.List());
        Assert.Equal(desk, lib.Load("Desk"));
        Assert.Null(lib.Load("missing"));

        lib.Delete("couch");
        Assert.Equal(["Desk"], lib.List());
    }

    [Fact]
    public void Save_InvalidName_Throws() =>
        Assert.Throws<ArgumentException>(() => NewLibrary().Save("a/b", new WorkspaceOptions()));

    [Fact]
    public void ExportImport_CopiesCurrentAndAllWorkspaces()
    {
        var source = NewLibrary("a");
        source.Save("Desk", new WorkspaceOptions { ScreenCount = 2 });
        source.Save("Travel", new WorkspaceOptions { ScreenCount = 1, Stabilizer = "ultra" });
        var current = new WorkspaceOptions { DistanceMeters = 2.25f, Axes = TrackingAxes.Full };
        string file = Path.Combine(_root, "export.json");
        source.Export(file, current);

        var target = NewLibrary("b");
        target.Save("Desk", new WorkspaceOptions { ScreenCount = 3 });
        var imported = target.Import(file);

        Assert.Equal(current, imported);
        Assert.Equal(["Desk", "Travel"], target.List());
        Assert.Equal(2, target.Load("Desk")!.ScreenCount); // same name is replaced
        Assert.Equal("ultra", target.Load("Travel")!.Stabilizer);
    }

    [Fact]
    public void Import_NotASettingsFile_ThrowsInvalidData()
    {
        Directory.CreateDirectory(_root);
        string file = Path.Combine(_root, "bad.json");
        File.WriteAllText(file, "this is not json");
        Assert.Throws<InvalidDataException>(() => NewLibrary().Import(file));

        File.WriteAllText(file, """{ "Format": 99 }""");
        Assert.Throws<InvalidDataException>(() => NewLibrary().Import(file));
    }
}
