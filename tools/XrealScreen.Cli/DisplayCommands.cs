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
        return display;
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
