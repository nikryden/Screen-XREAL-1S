using System.Text.Json;
using System.Text.Json.Serialization;

namespace XrealScreen.App;

/// <summary>App-level preferences (not workspace options). Stored in %LOCALAPPDATA%\XrealScreen\app.json.</summary>
public sealed class AppPreferences
{
    /// <summary>Closing the window hides XrealScreen to the tray instead of quitting.</summary>
    public bool KeepRunningInTray { get; set; } = true;

    /// <summary>The "still running in the tray" notification was shown once.</summary>
    public bool TrayHintShown { get; set; }

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XrealScreen", "app.json");

    public static AppPreferences Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize(File.ReadAllText(FilePath), AppPreferencesJsonContext.Default.AppPreferences) ?? new AppPreferences();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            CrashLog.Write("Preferences", ex);
        }

        return new AppPreferences();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, AppPreferencesJsonContext.Default.AppPreferences));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            CrashLog.Write("Preferences", ex);
        }
    }
}

[JsonSerializable(typeof(AppPreferences))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class AppPreferencesJsonContext : JsonSerializerContext;
