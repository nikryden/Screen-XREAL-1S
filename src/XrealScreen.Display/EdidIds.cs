namespace XrealScreen.Display;

/// <summary>EDID identifier helpers.</summary>
public static class EdidIds
{
    /// <summary>
    /// Decodes the EDID manufacturer ID as returned by CCD (big-endian word, three 5-bit letters, 1 = 'A').
    /// </summary>
    public static string DecodeManufacturer(ushort edidManufactureId)
    {
        int v = (ushort)((edidManufactureId >> 8) | (edidManufactureId << 8));
        Span<char> c = stackalloc char[3];
        c[0] = (char)('@' + ((v >> 10) & 0x1F));
        c[1] = (char)('@' + ((v >> 5) & 0x1F));
        c[2] = (char)('@' + (v & 0x1F));
        return new string(c);
    }

    /// <summary>Inverse of <see cref="DecodeManufacturer"/> (for tests and fixtures).</summary>
    public static ushort EncodeManufacturer(string pnpId)
    {
        ArgumentNullException.ThrowIfNull(pnpId);
        if (pnpId.Length != 3)
        {
            throw new ArgumentException("PnP ID must have 3 letters.", nameof(pnpId));
        }

        int v = ((pnpId[0] - '@') << 10) | ((pnpId[1] - '@') << 5) | (pnpId[2] - '@');
        return (ushort)(((v & 0xFF) << 8) | (v >> 8));
    }
}
