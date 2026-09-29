using System.Numerics;
using XrealScreen.Core.Devices;
using XrealScreen.Device.XrealOne;

namespace XrealScreen.Device.Tests;

public class DeviceMiscTests
{
    [Fact]
    public void AxisMap_ParsesAndApplies()
    {
        var map = AxisMap.Parse("+z,-y,+x");
        Assert.Equal(new Vector3(3, -2, 1), map.Apply(new Vector3(1, 2, 3)));
        Assert.Equal("+z,-y,+x", map.ToString());
    }

    [Fact]
    public void Catalog_Finds1S()
    {
        Assert.Equal(GlassesModel.XReal1S, GlassesCatalog.Find(0x3318, 0x043E)?.Model);
        Assert.Null(GlassesCatalog.Find(0x1234, 0x043E));
    }

    [Fact]
    public async Task Xrcap_RoundTrip()
    {
        var ct = TestContext.Current.CancellationToken;
        string path = Path.Combine(Path.GetTempPath(), $"xrs-{Guid.NewGuid():N}.xrcap");
        try
        {
            var t = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
            await using (var writer = await XrcapWriter.CreateAsync(path, "note=test", ct))
            {
                await writer.WriteAsync(t, new byte[] { 1, 2, 3 }, ct);
                await writer.WriteAsync(t.AddMilliseconds(1), new byte[] { 4 }, ct);
            }

            var chunks = new List<XrcapReader.Chunk>();
            await foreach (var c in XrcapReader.ReadAsync(path, ct))
            {
                chunks.Add(c);
            }

            Assert.Equal(2, chunks.Count);
            Assert.Equal([1, 2, 3], chunks[0].Data);
            Assert.Equal(t, chunks[0].ReceivedUtc);
            Assert.Equal("note=test", XrcapReader.ReadMetadata(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
