using System.CommandLine;
using System.Globalization;
using XrealScreen.Core.Workspace;
using XrealScreen.Display;
using XrealScreen.Display.VirtualDisplayRs;

namespace XrealScreen.Cli;

/// <summary>Virtual monitor control through the virtual-display-rs driver (ADR-0008).</summary>
internal static class VddCommands
{
    public static Command Create()
    {
        var vdd = new Command("vdd", "Create/remove virtual monitors (virtual-display-rs driver).");
        vdd.Subcommands.Add(CreateState());
        vdd.Subcommands.Add(CreateAdd());
        vdd.Subcommands.Add(CreateClear());
        return vdd;
    }

    // CLI process is short-lived; providers are not disposed explicitly.
    private static VirtualDisplayRsProvider Provider() => new(new CcdDisplayTopology());

    private static Command CreateState()
    {
        var command = new Command("state", "Show all virtual-display-rs monitors (ours are marked *).");
        command.SetAction(async (_, ct) =>
        {
            var provider = Provider();
            if (!await provider.IsAvailableAsync(ct).ConfigureAwait(false))
            {
                Console.Error.WriteLine(@"virtual-display-rs driver not reachable (\\.\pipe\virtualdisplaydriver).");
                return 2;
            }

            var all = await provider.GetAllAsync(ct).ConfigureAwait(false);
            if (all.Count == 0)
            {
                Console.WriteLine("(no virtual monitors)");
            }

            foreach (var d in all)
            {
                Console.WriteLine($"{(VirtualDisplayRsProvider.IsOwned(d) ? "*" : " ")} id={d.ProviderId,-2} {d.Resolution} @ {d.RefreshHz} Hz  name={d.Name ?? "-"}");
            }

            return 0;
        });
        return command;
    }

    private static Command CreateAdd()
    {
        var size = new Argument<string>("resolution") { Description = "e.g. 3840x1080, or an ultrawide mode: off | 21x9 | 32x9 | 16x18" };
        var hz = new Option<int>("--hz") { DefaultValueFactory = _ => 60 };
        var command = new Command("add", "Add one virtual monitor owned by XrealScreen.") { size, hz };
        command.SetAction(async (parse, ct) =>
        {
            var resolution = ParseResolution(parse.GetValue(size)!);
            var store = new LayoutSnapshotStore();
            if (!store.Exists)
            {
                // First change: remember the user's layout so `vdd clear` can put it back.
                await store.SaveAsync(new CcdDisplayTopology().CaptureLayout(), ct).ConfigureAwait(false);
                Console.WriteLine($"saved current layout → {store.Path}");
            }

            var display = await Provider().AddAsync(resolution, parse.GetValue(hz), ct).ConfigureAwait(false);
            Console.WriteLine($"added id={display.ProviderId} {display.Resolution} @ {display.RefreshHz} Hz → {display.GdiDeviceName ?? "(not attached yet)"}");
            return 0;
        });
        return command;
    }

    private static Command CreateClear()
    {
        var command = new Command("clear", "Remove all virtual monitors created by XrealScreen (others are kept).");
        command.SetAction(async (_, ct) =>
        {
            await Provider().RemoveAllOwnedAsync(ct).ConfigureAwait(false);
            Console.WriteLine("removed all XrealScreen virtual monitors");

            var store = new LayoutSnapshotStore();
            var layout = await store.LoadAsync(ct).ConfigureAwait(false);
            if (layout is not null)
            {
                // Windows detaches the removed monitors asynchronously.
                await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                var result = new CcdDisplayTopology().ApplyLayout(layout);
                store.Delete();
                Console.WriteLine($"restored layout: {string.Join(", ", result.Applied)}");
            }

            return 0;
        });
        return command;
    }

    private static Resolution ParseResolution(string text) => text.ToLowerInvariant() switch
    {
        "off" => UltrawideModes.GetResolution(UltrawideMode.Off),
        "21x9" or "21:9" => UltrawideModes.GetResolution(UltrawideMode.Wide21x9),
        "32x9" or "32:9" => UltrawideModes.GetResolution(UltrawideMode.Wide32x9),
        "16x18" or "16:18" => UltrawideModes.GetResolution(UltrawideMode.Tall16x18),
        _ when text.Split('x', 'X') is [var w, var h] => new Resolution(int.Parse(w, CultureInfo.InvariantCulture), int.Parse(h, CultureInfo.InvariantCulture)),
        _ => throw new FormatException($"Unknown resolution '{text}'."),
    };
}
