namespace XrealScreen.Core.Workspace;

/// <summary>A pixel rectangle inside the glasses' video signal.</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height);

/// <summary>Shape of the screens in the glasses-anchor workspace.</summary>
public enum AnchorAspect
{
    /// <summary>16:9 screens (most apps and video).</summary>
    Ratio16x9,

    /// <summary>16:10 screens (a little taller).</summary>
    Ratio16x10,

    /// <summary>Use the full height/width of each slot (aspect follows the glasses signal).</summary>
    Fill,
}

/// <summary>
/// Glasses-anchor workspace (ADR-0010): the glasses world-lock their own ultrawide image (X1 chip,
/// OSD Anchor mode) and we split that image into virtual screens with 1:1 pixels.
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

    public static double? AspectValue(AnchorAspect aspect) => aspect switch
    {
        AnchorAspect.Ratio16x9 => 16.0 / 9.0,
        AnchorAspect.Ratio16x10 => 16.0 / 10.0,
        _ => null,
    };

    /// <summary>
    /// Splits the glasses signal into <paramref name="count"/> screens with <paramref name="gapPixels"/>
    /// between them: side by side for wide signals, stacked for tall ones (16:18). Each screen is as large
    /// as possible while keeping <paramref name="aspect"/>; the group is centered. Sizes are even pixels.
    /// </summary>
    public static IReadOnlyList<PixelRect> Split(Resolution glasses, int count, int gapPixels = 0, AnchorAspect aspect = AnchorAspect.Fill)
    {
        count = Math.Clamp(count, 1, MaxScreens);
        gapPixels = Math.Max(0, gapPixels);
        bool stacked = glasses.Height > glasses.Width;
        double? ratio = AspectValue(aspect);

        // Slot = the space one screen may use along the split axis; full extent across it.
        double slotMain = ((stacked ? glasses.Height : glasses.Width) - (double)gapPixels * (count - 1)) / count;
        double cross = stacked ? glasses.Width : glasses.Height;
        double w, h;
        if (stacked)
        {
            (w, h) = (cross, slotMain);
            if (ratio is { } r)
            {
                h = Math.Min(slotMain, cross / r);
                w = h * r;
            }
        }
        else
        {
            (w, h) = (slotMain, cross);
            if (ratio is { } r)
            {
                w = Math.Min(slotMain, cross * r);
                h = w / r;
            }
        }

        int width = Even(w), height = Even(h);
        var result = new List<PixelRect>(count);
        if (stacked)
        {
            int total = height * count + gapPixels * (count - 1);
            int y = (glasses.Height - total) / 2;
            int x = (glasses.Width - width) / 2;
            for (int i = 0; i < count; i++)
            {
                result.Add(new PixelRect(x, y + i * (height + gapPixels), width, height));
            }
        }
        else
        {
            int total = width * count + gapPixels * (count - 1);
            int x = (glasses.Width - total) / 2;
            int y = (glasses.Height - height) / 2;
            for (int i = 0; i < count; i++)
            {
                result.Add(new PixelRect(x + i * (width + gapPixels), y, width, height));
            }
        }

        return result;

        static int Even(double v) => Math.Max(2, (int)Math.Floor(v / 2) * 2);
    }
}
