using XrealScreen.Core.Recording;
using XrealScreen.Core.Tracking;
using XrealScreen.Device.Simulated;

namespace XrealScreen.Core.Tests;

public class XrimuFileTests
{
    [Fact]
    public async Task RoundTrip_PreservesSamplesAndMetadata()
    {
        var ct = TestContext.Current.CancellationToken;
        string path = Path.Combine(Path.GetTempPath(), $"xrs-{Guid.NewGuid():N}.xrimu");
        try
        {
            var source = new SyntheticImuSource(new SyntheticImuOptions { DurationSeconds = 0.2, GyroNoise = 0.01f });
            var original = new List<ImuSample>();
            await foreach (var s in source.ReadAsync(ct))
            {
                original.Add(s);
            }

            await XrimuFile.WriteAsync(path, original.ToAsyncEnumerable(), new Dictionary<string, string> { ["note"] = "unit test" }, ct);

            var read = new List<ImuSample>();
            await foreach (var s in XrimuFile.ReadAsync(path, ct))
            {
                read.Add(s);
            }

            Assert.Equal(original, read);
            Assert.Equal("unit_test", XrimuFile.ReadMetadata(path)["note"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
