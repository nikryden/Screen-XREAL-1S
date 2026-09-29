using System.Text.Json;

namespace XrealScreen.Host;

/// <summary>Everything in an exported settings file: the current workspace and all saved workspaces.</summary>
public sealed record WorkspaceBundle
{
    public const int CurrentFormat = 1;

    public int Format { get; init; } = CurrentFormat;

    public WorkspaceOptions Current { get; init; } = new();

    public Dictionary<string, WorkspaceOptions> Workspaces { get; init; } = [];
}

/// <summary>
/// Named workspaces (%LOCALAPPDATA%\XrealScreen\workspaces\&lt;name&gt;.json) and settings export/import.
/// </summary>
public sealed class WorkspaceLibrary(string? directory = null)
{
    public const int MaxNameLength = 60;

    public string Directory { get; } = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XrealScreen", "workspaces");

    /// <summary>Trimmed name, or null when it is empty, too long or not usable as a file name.</summary>
    public static string? NormalizeName(string? name)
    {
        string trimmed = (name ?? string.Empty).Trim();
        bool valid = trimmed.Length is > 0 and <= MaxNameLength
            && trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && !trimmed.EndsWith('.');
        return valid ? trimmed : null;
    }

    public IReadOnlyList<string> List()
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return [];
        }

        return [.. System.IO.Directory.EnumerateFiles(Directory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Order(StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>The saved workspace, or null when it does not exist or cannot be read.</summary>
    public WorkspaceOptions? Load(string name)
    {
        try
        {
            using var stream = File.OpenRead(PathOf(name));
            return JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.WorkspaceOptions);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or JsonException)
        {
            return null;
        }
    }

    public void Save(string name, WorkspaceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        SettingsFile.WriteAtomic(PathOf(name), options, SettingsJsonContext.Default.WorkspaceOptions);
    }

    public void Delete(string name) => File.Delete(PathOf(name));

    /// <summary>Writes the current workspace and all saved workspaces to one file.</summary>
    public void Export(string path, WorkspaceOptions current)
    {
        var bundle = new WorkspaceBundle
        {
            Current = current,
            Workspaces = List().Select(n => (Name: n, Options: Load(n)))
                .Where(w => w.Options is not null)
                .ToDictionary(w => w.Name, w => w.Options!),
        };
        SettingsFile.WriteAtomic(path, bundle, SettingsJsonContext.Default.WorkspaceBundle);
    }

    /// <summary>
    /// Reads an exported file, saves its workspaces (same names are replaced) and returns its current workspace.
    /// </summary>
    /// <exception cref="InvalidDataException">The file is not an XrealScreen settings file.</exception>
    public WorkspaceOptions Import(string path)
    {
        WorkspaceBundle? bundle;
        try
        {
            using var stream = File.OpenRead(path);
            bundle = JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.WorkspaceBundle);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Not an XrealScreen settings file: {ex.Message}", ex);
        }

        if (bundle is null || bundle.Format is < 1 or > WorkspaceBundle.CurrentFormat)
        {
            throw new InvalidDataException("Not an XrealScreen settings file (unknown format).");
        }

        foreach (var (name, options) in bundle.Workspaces)
        {
            if (NormalizeName(name) is { } valid && options is not null)
            {
                Save(valid, options);
            }
        }

        return bundle.Current ?? new WorkspaceOptions();
    }

    private string PathOf(string name) =>
        Path.Combine(Directory, (NormalizeName(name) ?? throw new ArgumentException($"Invalid workspace name: '{name}'", nameof(name))) + ".json");
}
