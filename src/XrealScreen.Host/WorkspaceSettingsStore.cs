using System.Text.Json;
using System.Text.Json.Serialization;

namespace XrealScreen.Host;

[JsonSerializable(typeof(WorkspaceOptions))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

/// <summary>Persists the user's workspace options (%LOCALAPPDATA%\XrealScreen\settings.json).</summary>
public sealed class WorkspaceSettingsStore(string? path = null)
{
    public string Path { get; } = path ?? System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XrealScreen", "settings.json");

    public WorkspaceOptions Load()
    {
        try
        {
            if (File.Exists(Path))
            {
                using var stream = File.OpenRead(Path);
                return JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.WorkspaceOptions) ?? new WorkspaceOptions();
            }
        }
        catch (JsonException)
        {
            // Corrupt or old file: fall back to defaults.
        }

        return new WorkspaceOptions();
    }

    public void Save(WorkspaceOptions options)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        string tmp = Path + ".tmp";
        using (var stream = File.Create(tmp))
        {
            JsonSerializer.Serialize(stream, options, SettingsJsonContext.Default.WorkspaceOptions);
        }

        File.Move(tmp, Path, overwrite: true);
    }
}
