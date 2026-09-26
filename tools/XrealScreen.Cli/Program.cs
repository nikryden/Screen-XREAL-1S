using System.CommandLine;
using XrealScreen.Cli;

var root = new RootCommand("xrs — XrealScreen developer/hardware tool (see docs/testing/hardware-test-M1.md)");
root.Subcommands.Add(DeviceCommands.CreateDevices());
root.Subcommands.Add(DeviceCommands.CreateProbe());
root.Subcommands.Add(ImuCommands.Create());
root.Subcommands.Add(DisplayCommands.Create());
root.Subcommands.Add(VddCommands.Create());

return await root.Parse(args).InvokeAsync().ConfigureAwait(false);
