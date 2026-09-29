using System.Text.Json;
using System.Text.Json.Serialization;
using XrealScreen.Core.Abstractions;

namespace XrealScreen.Display;

[JsonSerializable(typeof(DisplayLayout))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class LayoutJsonContext : JsonSerializerContext;

/// <summary>
/// Persists the user's layout before XrealScreen changes the topology, so it can be restored on
/// exit, after a crash (watchdog, M6) or from the panic hotkey. Deleted after a successful restore.
/// </summary>
public sealed class LayoutSnapshotStore(string? path = null)
{
    public string Path { get; } = path ?? System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XrealScreen", "topology-snapshot.json");

    public bool Exists => File.Exists(Path);

    public async Task SaveAsync(DisplayLayout layout, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        string tmp = Path + ".tmp";
        await using (var stream = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(stream, layout, LayoutJsonContext.Default.DisplayLayout, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tmp, Path, overwrite: true);
    }

    public async Task<DisplayLayout?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!Exists)
        {
            return null;
        }

        await using var stream = File.OpenRead(Path);
        return await JsonSerializer.DeserializeAsync(stream, LayoutJsonContext.Default.DisplayLayout, cancellationToken).ConfigureAwait(false);
    }

    public void Delete() => File.Delete(Path);
}
