using System.Diagnostics;
using Vortice.DXGI;

namespace XrealScreen.Render;

/// <summary>Diagnostics for display timing.</summary>
public static class OutputTiming
{
    /// <summary>Measures the refresh rate of the DXGI output with the given GDI name from its vertical blanks.</summary>
    public static double? MeasureVblankHz(string gdiDeviceName, TimeSpan duration)
    {
        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
        for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1? adapter).Success; i++)
        {
            using (adapter)
            {
                for (uint o = 0; adapter!.EnumOutputs(o, out IDXGIOutput? output).Success; o++)
                {
                    using (output)
                    {
                        if (!string.Equals(output!.Description.DeviceName, gdiDeviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        output.WaitForVBlank();
                        long start = Stopwatch.GetTimestamp();
                        int count = 0;
                        while (Stopwatch.GetElapsedTime(start) < duration)
                        {
                            output.WaitForVBlank();
                            count++;
                        }

                        return count / Stopwatch.GetElapsedTime(start).TotalSeconds;
                    }
                }
            }
        }

        return null;
    }
}
