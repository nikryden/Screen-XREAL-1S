using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace XrealScreen.Host;

[JsonSerializable(typeof(WorkspaceOptions))]
[JsonSerializable(typeof(WorkspaceBundle))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

internal static class SettingsFile
{
    /// <summary>Writes via a temp file so a crash never leaves a half-written settings file.</summary>
    public static void WriteAtomic<T>(string path, T value, JsonTypeInfo<T> typeInfo)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string tmp = path + ".tmp";
        using (var stream = File.Create(tmp))
        {
            JsonSerializer.Serialize(stream, value, typeInfo);
        }

        File.Move(tmp, path, overwrite: true);
    }
}

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

    public void Save(WorkspaceOptions options) => SettingsFile.WriteAtomic(Path, options, SettingsJsonContext.Default.WorkspaceOptions);
}
