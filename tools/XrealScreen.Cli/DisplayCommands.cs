using System.CommandLine;
using XrealScreen.Display;

namespace XrealScreen.Cli;

internal static class DisplayCommands
{
    public static Command Create()
    {
        var display = new Command("display", "Inspect Windows monitors and the glasses' display modes.");
        display.Subcommands.Add(CreateList());
        display.Subcommands.Add(CreateModes());
        display.Subcommands.Add(CreateSave());
        display.Subcommands.Add(CreateRestore());
        return display;
    }

    private static Command CreateSave()
    {
        var command = new Command("save", "Save the current monitor layout (positions, resolutions) as the restore point.");
        command.SetAction(async (_, ct) =>
        {
            var store = new LayoutSnapshotStore();
            var layout = new CcdDisplayTopology().CaptureLayout();
            await store.SaveAsync(layout, ct).ConfigureAwait(false);
            PrintLayout(layout);
            Console.WriteLine($"saved → {store.Path}");
            return 0;
        });
        return command;
    }

    private static Command CreateRestore()
    {
        var validate = new Option<bool>("--validate") { Description = "Only check that Windows would accept the layout." };
        var keep = new Option<bool>("--keep") { Description = "Keep the snapshot file after restoring." };
        var command = new Command("restore", "Restore the saved monitor layout.") { validate, keep };
        command.SetAction(async (parse, ct) =>
        {
            var store = new LayoutSnapshotStore();
            var layout = await store.LoadAsync(ct).ConfigureAwait(false);
            if (layout is null)
            {
                Console.Error.WriteLine($"no saved layout at {store.Path}");
                return 2;
            }

            var result = new CcdDisplayTopology().ApplyLayout(layout, parse.GetValue(validate));
            Console.WriteLine($"{(parse.GetValue(validate) ? "valid" : "restored")}: {string.Join(", ", result.Applied)}");
            if (result.Missing.Count > 0)
            {
                Console.WriteLine($"not connected now: {string.Join(", ", result.Missing)}");
            }

            if (!parse.GetValue(validate) && !parse.GetValue(keep))
            {
                store.Delete();
            }

            return 0;
        });
        return command;
    }

    private static void PrintLayout(XrealScreen.Core.Abstractions.DisplayLayout layout)
    {
        foreach (var m in layout.Monitors)
        {
            Console.WriteLine($"  {m.FriendlyName,-18} {m.EdidId,-8} {m.Width}×{m.Height} @ {m.RefreshHz} Hz at ({m.X},{m.Y}){(m.IsPrimary ? " primary" : "")}");
        }
    }

    private static Command CreateList()
    {
        var command = new Command("list", "List active monitors (CCD) and mark the glasses.");
        command.SetAction(_ =>
        {
            var topology = new CcdDisplayTopology();
            foreach (var m in topology.GetActiveMonitors())
            {
                var model = GlassesDisplayLocator.Identify(m);
                Console.WriteLine($"{(model is null ? "  " : "* ")}{m.GdiDeviceName,-14} {m.FriendlyName,-18} EDID={m.EdidId,-8} {m.Resolution} @ {m.RefreshHz} Hz  pos=({m.X},{m.Y}){(m.IsPrimary ? " primary" : "")}  {m.OutputTechnology}  adapter=0x{m.AdapterLuid:X}{(model is null ? "" : $"  <- glasses: {model}")}");
                Console.WriteLine($"    {m.DevicePath}");
            }

            return 0;
        });
        return command;
    }

    private static Command CreateModes()
    {
        var device = new Argument<string?>("gdi") { Description = @"GDI name like \\.\DISPLAY2 (default: the glasses).", Arity = ArgumentArity.ZeroOrOne };
        var command = new Command("modes", "List display modes a monitor supports (default: the glasses).") { device };
        command.SetAction(parse =>
        {
            var topology = new CcdDisplayTopology();
            string? gdi = parse.GetValue(device) ?? GlassesDisplayLocator.FindGlasses(topology.GetActiveMonitors())?.GdiDeviceName;
            if (gdi is null)
            {
                Console.Error.WriteLine("Glasses monitor not found (is the display extended, not duplicated?). Pass a GDI name.");
                return 2;
            }

            Console.WriteLine($"Modes of {gdi}:");
            foreach (var mode in topology.GetSupportedModes(gdi).Where(m => m.BitsPerPixel == 32))
            {
                Console.WriteLine($"  {mode}");
            }

            return 0;
        });
        return command;
    }
}
