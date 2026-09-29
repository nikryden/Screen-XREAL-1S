using System.Globalization;
using System.Numerics;

namespace XrealScreen.Device.XrealOne;

/// <summary>
/// Maps a sensor vector into the tracker's body frame, e.g. "+z,+y,+x" means
/// body.X = sensor.Z, body.Y = sensor.Y, body.Z = sensor.X.
/// </summary>
public readonly record struct AxisMap(int XFrom, float XSign, int YFrom, float YSign, int ZFrom, float ZSign)
{
    public static AxisMap Identity { get; } = Parse("+x,+y,+z");

    public Vector3 Apply(Vector3 v) => new(Pick(v, XFrom) * XSign, Pick(v, YFrom) * YSign, Pick(v, ZFrom) * ZSign);

    public static AxisMap Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            throw new FormatException($"Axis map needs 3 entries like '+x,+y,+z': '{text}'");
        }

        var (xi, xs) = ParseAxis(parts[0]);
        var (yi, ys) = ParseAxis(parts[1]);
        var (zi, zs) = ParseAxis(parts[2]);
        return new AxisMap(xi, xs, yi, ys, zi, zs);
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Format(XFrom, XSign)},{Format(YFrom, YSign)},{Format(ZFrom, ZSign)}");

    private static (int Index, float Sign) ParseAxis(string token)
    {
        float sign = token.StartsWith('-') ? -1f : 1f;
        char axis = char.ToLowerInvariant(token.TrimStart('+', '-')[0]);
        return axis switch
        {
            'x' => (0, sign),
            'y' => (1, sign),
            'z' => (2, sign),
            _ => throw new FormatException($"Unknown axis '{token}'"),
        };
    }

    private static string Format(int index, float sign) => (sign < 0 ? "-" : "+") + "xyz"[index];

    private static float Pick(Vector3 v, int index) => index switch { 0 => v.X, 1 => v.Y, _ => v.Z };
}
