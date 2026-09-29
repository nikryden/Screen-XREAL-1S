// Wire format of MolotovCherry/virtual-display-rs driver IPC (MIT, Copyright (c) 2024 Cherry),
// re-implemented from rust/driver-ipc/src/core.rs and client.rs (main 22fcd2e, driver 0.4.0).
// See docs/findings/virtual-display-drivers.md and THIRD-PARTY-NOTICES.md.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XrealScreen.Display.VirtualDisplayRs;

internal sealed record VdrsMode(
    [property: JsonPropertyName("width")] uint Width,
    [property: JsonPropertyName("height")] uint Height,
    [property: JsonPropertyName("refresh_rates")] IReadOnlyList<uint> RefreshRates);

internal sealed record VdrsMonitor(
    [property: JsonPropertyName("id")] uint Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("modes")] IReadOnlyList<VdrsMode> Modes);

[JsonSerializable(typeof(Dictionary<string, List<VdrsMonitor>>))]
[JsonSerializable(typeof(Dictionary<string, List<uint>>))]
[JsonSerializable(typeof(List<VdrsMonitor>))]
[JsonSerializable(typeof(string))]
internal sealed partial class VdrsJsonContext : JsonSerializerContext;

/// <summary>
/// Messages are serde "externally tagged" JSON, UTF-8, each terminated by byte 0x04:
/// <c>{"Notify":[monitors]}</c> (replaces the full monitor list), <c>{"Remove":[ids]}</c>,
/// <c>"RemoveAll"</c>, <c>"State"</c> → reply <c>{"State":[monitors]}</c>; the driver also
/// broadcasts <c>{"Changed":[monitors]}</c> to every client after a change.
/// </summary>
internal static class VdrsProtocol
{
    public const byte Terminator = 0x04;
    public const string DefaultPipeName = "virtualdisplaydriver";

    public static byte[] Notify(IEnumerable<VdrsMonitor> monitors) =>
        Frame(JsonSerializer.SerializeToUtf8Bytes(
            new Dictionary<string, List<VdrsMonitor>> { ["Notify"] = monitors.ToList() },
            VdrsJsonContext.Default.DictionaryStringListVdrsMonitor));

    public static byte[] Remove(IEnumerable<uint> ids) =>
        Frame(JsonSerializer.SerializeToUtf8Bytes(
            new Dictionary<string, List<uint>> { ["Remove"] = ids.ToList() },
            VdrsJsonContext.Default.DictionaryStringListUInt32));

    public static byte[] RemoveAll() => Frame(Encoding.UTF8.GetBytes("\"RemoveAll\""));

    public static byte[] State() => Frame(Encoding.UTF8.GetBytes("\"State\""));

    /// <summary>Parses one message body (without terminator). Returns the tag and monitors for State/Changed.</summary>
    public static (string Tag, IReadOnlyList<VdrsMonitor> Monitors) Parse(ReadOnlySpan<byte> json)
    {
        using var doc = JsonDocument.Parse(json.ToArray());
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.String)
        {
            return (root.GetString() ?? string.Empty, []);
        }

        foreach (var property in root.EnumerateObject())
        {
            var monitors = property.Value.ValueKind == JsonValueKind.Array && property.Name is "State" or "Changed" or "Notify"
                ? property.Value.Deserialize(VdrsJsonContext.Default.ListVdrsMonitor) ?? []
                : [];
            return (property.Name, monitors);
        }

        return (string.Empty, []);
    }

    private static byte[] Frame(byte[] body)
    {
        var message = new byte[body.Length + 1];
        body.CopyTo(message, 0);
        message[^1] = Terminator;
        return message;
    }
}
