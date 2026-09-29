using System.IO.Pipes;
using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Workspace;
using XrealScreen.Display.VirtualDisplayRs;

namespace XrealScreen.Display.Tests;

public class VirtualDisplayRsProviderTests
{
    [Fact]
    public void Protocol_MatchesDriverWireFormat()
    {
        var notify = VdrsProtocol.Notify([new VdrsMonitor(7, null, true, [new VdrsMode(1920, 1080, [60, 120])])]);
        Assert.Equal(0x04, notify[^1]);
        Assert.Equal(
            """{"Notify":[{"id":7,"name":null,"enabled":true,"modes":[{"width":1920,"height":1080,"refresh_rates":[60,120]}]}]}""",
            System.Text.Encoding.UTF8.GetString(notify, 0, notify.Length - 1));
        Assert.Equal("\"State\"\u0004", System.Text.Encoding.UTF8.GetString(VdrsProtocol.State()));
        Assert.Equal("{\"Remove\":[1,2]}\u0004", System.Text.Encoding.UTF8.GetString(VdrsProtocol.Remove([1, 2])));
    }

    [Fact]
    public void Protocol_ParsesStateReply()
    {
        var (tag, monitors) = VdrsProtocol.Parse("""{"State":[{"id":3,"name":"x","enabled":true,"modes":[{"width":2560,"height":1080,"refresh_rates":[90]}]}]}"""u8);
        Assert.Equal("State", tag);
        Assert.Equal(2560u, Assert.Single(monitors).Modes[0].Width);
    }

    [Fact]
    public async Task Add_PreservesForeignMonitors_AndUsesOwnIdRange()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var server = new FakeVdrsServer([new VdrsMonitor(1, "other app", true, [new VdrsMode(1920, 1080, [60])])]);
        var provider = new VirtualDisplayRsProvider(topology: null, pipeName: server.PipeName);

        var added = await provider.AddAsync(new Resolution(3840, 1080), 60, ct);

        Assert.Equal("15", added.ProviderId);
        Assert.True(VirtualDisplayRsProvider.IsOwned(added));
        await WaitForAsync(() => server.Monitors.Count == 2);
        Assert.Equal([1u, 15u], server.Monitors.Select(m => m.Id).Order());
        Assert.All(server.Monitors, m => Assert.InRange(m.Id, 0u, VirtualDisplayRsProvider.MaxId));
    }

    [Fact]
    public async Task RemoveAllOwned_KeepsForeignMonitors()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var server = new FakeVdrsServer(
        [
            new VdrsMonitor(1, "other app", true, [new VdrsMode(1920, 1080, [60])]),
            new VdrsMonitor(15, "XrealScreen 15", true, [new VdrsMode(1920, 1080, [60])]),
            new VdrsMonitor(14, "XrealScreen 14", true, [new VdrsMode(1920, 1080, [60])]),
            new VdrsMonitor(2, null, true, [new VdrsMode(1920, 1080, [60])]),
        ]);
        var provider = new VirtualDisplayRsProvider(pipeName: server.PipeName);

        await provider.RemoveAllOwnedAsync(ct);

        await WaitForAsync(() => server.Monitors.Count == 2);
        Assert.Equal([1u, 2u], server.Monitors.Select(m => m.Id).Order());
    }

    [Fact]
    public async Task Remove_RefusesForeignMonitor()
    {
        await using var server = new FakeVdrsServer([new VdrsMonitor(1, "other app", true, [new VdrsMode(1920, 1080, [60])])]);
        var provider = new VirtualDisplayRsProvider(pipeName: server.PipeName);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.RemoveAsync(new VirtualDisplay("1", new Resolution(1920, 1080), 60, null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Add_FailsWhenAllSixteenConnectorsAreUsed()
    {
        await using var server = new FakeVdrsServer(Enumerable.Range(0, 16).Select(i => new VdrsMonitor((uint)i, null, true, [new VdrsMode(1920, 1080, [60])])));
        var provider = new VirtualDisplayRsProvider(pipeName: server.PipeName);
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AddAsync(new Resolution(1920, 1080), 60, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IsAvailable_FalseWhenNoDriver()
    {
        var provider = new VirtualDisplayRsProvider(pipeName: $"xrs-missing-{Guid.NewGuid():N}", timeout: TimeSpan.FromMilliseconds(300));
        Assert.False(await provider.IsAvailableAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>The fake server applies commands asynchronously after the client disconnects.</summary>
    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (int i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Minimal in-process stand-in for the driver's pipe server.</summary>
    private sealed class FakeVdrsServer : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;
        private readonly Lock _lock = new();
        private List<VdrsMonitor> _monitors;

        public FakeVdrsServer(IEnumerable<VdrsMonitor> initial)
        {
            _monitors = initial.ToList();
            _loop = Task.Run(RunAsync);
        }

        public string PipeName { get; } = $"xrs-test-{Guid.NewGuid():N}";

        public List<VdrsMonitor> Monitors
        {
            get
            {
                lock (_lock)
                {
                    return _monitors.ToList();
                }
            }
        }

        private async Task RunAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                await using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 4, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                try
                {
                    await pipe.WaitForConnectionAsync(_cts.Token);
                    var message = new List<byte>();
                    var buf = new byte[1];
                    while (await pipe.ReadAsync(buf, _cts.Token) == 1 && buf[0] != VdrsProtocol.Terminator)
                    {
                        message.Add(buf[0]);
                    }

                    var (tag, monitors) = VdrsProtocol.Parse(message.ToArray());
                    lock (_lock)
                    {
                        if (tag == "Notify")
                        {
                            _monitors = monitors.ToList();
                        }
                        else if (tag == "Remove")
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(message.ToArray());
                            var ids = doc.RootElement.GetProperty("Remove").EnumerateArray().Select(e => e.GetUInt32()).ToHashSet();
                            _monitors = _monitors.Where(m => !ids.Contains(m.Id)).ToList();
                        }
                    }

                    if (tag == "State")
                    {
                        // Send an unrelated event first, like the real driver may.
                        await pipe.WriteAsync(System.Text.Encoding.UTF8.GetBytes("{\"Changed\":[]}\u0004"), _cts.Token);
                        var reply = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                            new Dictionary<string, List<VdrsMonitor>> { ["State"] = Monitors.ToList() },
                            VdrsJsonContext.Default.DictionaryStringListVdrsMonitor);
                        await pipe.WriteAsync(reply, _cts.Token);
                        await pipe.WriteAsync(new[] { VdrsProtocol.Terminator }, _cts.Token);
                        await pipe.FlushAsync(_cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (IOException)
                {
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
            }

            _cts.Dispose();
        }
    }
}
