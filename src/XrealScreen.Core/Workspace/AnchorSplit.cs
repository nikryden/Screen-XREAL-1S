namespace XrealScreen.Core.Workspace;

/// <summary>A pixel rectangle inside the glasses' video signal.</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height);

/// <summary>
/// Glasses-anchor workspace (ADR-0010): the glasses world-lock their own ultrawide image (X1 chip,
/// OSD Anchor mode) and we split that image into equal virtual screens with 1:1 pixels.
/// </summary>
public static class AnchorSplit
{
    public const int MaxScreens = 3;

    /// <summary>
    /// True when the glasses send an ultrawide / tall signal (OSD UltraWide 21:9, 32:9 or 16:18).
    /// With UltraWide Off the 1S sends 1920×1200 ([verified-hw]).
    /// </summary>
    public static bool IsUltrawideSignal(Resolution glasses) =>
        glasses.Height > glasses.Width || (double)glasses.Width / glasses.Height >= 2.2;

    /// <summary>
    /// Splits the glasses signal into <paramref name="count"/> equal screens: side by side for wide
    /// signals, stacked for tall ones (16:18). Widths/heights are rounded down to even pixels.
    /// </summary>
    public static IReadOnlyList<PixelRect> Split(Resolution glasses, int count)
    {
        count = Math.Clamp(count, 1, MaxScreens);
        bool stacked = glasses.Height > glasses.Width;
        var result = new List<PixelRect>(count);
        if (stacked)
        {
            int h = glasses.Height / count / 2 * 2;
            for (int i = 0; i < count; i++)
            {
                result.Add(new PixelRect(0, i * h, glasses.Width, h));
            }
        }
        else
        {
            int w = glasses.Width / count / 2 * 2;
            for (int i = 0; i < count; i++)
            {
                result.Add(new PixelRect(i * w, 0, w, glasses.Height));
            }
        }

        return result;
    }
}
