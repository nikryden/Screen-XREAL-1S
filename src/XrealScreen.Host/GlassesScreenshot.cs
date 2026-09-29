using System.Globalization;
using XrealScreen.Display;

namespace XrealScreen.Host;

/// <summary>Screenshots of what the glasses show (the workspace as seen in the glasses, see-through areas black).</summary>
public static class GlassesScreenshot
{
    public static string DefaultFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "XrealScreen");

    /// <summary>Saves the glasses monitor as "XrealScreen yyyy-MM-dd HH-mm-ss.png" in <paramref name="folder"/>.</summary>
    /// <returns>The file path.</returns>
    /// <exception cref="InvalidOperationException">The glasses are not an active monitor.</exception>
    public static async Task<string> SaveAsync(string? folder, CancellationToken cancellationToken)
    {
        var glasses = GlassesDisplayLocator.FindGlasses(new CcdDisplayTopology().GetActiveMonitors())
            ?? throw new InvalidOperationException("The glasses are not connected as a monitor.");

        string name = string.Create(CultureInfo.InvariantCulture, $"XrealScreen {DateTime.Now:yyyy-MM-dd HH-mm-ss}");
        string path = Path.Combine(folder ?? DefaultFolder, name + ".png");
        for (int i = 2; File.Exists(path); i++)
        {
            path = Path.Combine(folder ?? DefaultFolder, $"{name} ({i}).png");
        }

        var bounds = new System.Drawing.Rectangle(glasses.X, glasses.Y, glasses.Resolution.Width, glasses.Resolution.Height);
        await ScreenCapture.SavePngAsync(bounds, path, cancellationToken).ConfigureAwait(false);
        return path;
    }
}
