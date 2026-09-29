namespace XrealScreen.App;

/// <summary>Appends unhandled exceptions to %LOCALAPPDATA%\XrealScreen\crash.log (for bug reports).</summary>
internal static class CrashLog
{
    public static string Path { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XrealScreen", "crash.log");

    public static void Write(string source, Exception? exception)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.AppendAllText(Path, $"==== {DateTime.Now:O} [{source}]{Environment.NewLine}{exception}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
