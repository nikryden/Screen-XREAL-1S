using System.Runtime.InteropServices;

namespace XrealScreen.Display.Native;

// Win32 CCD (Connecting and Configuring Displays) and EnumDisplaySettings interop.
// Layouts follow wingdi.h; sizes are asserted in XrealScreen.Display.Tests.

[StructLayout(LayoutKind.Sequential)]
internal struct LUID
{
    public uint LowPart;
    public int HighPart;

    public readonly long Value => ((long)HighPart << 32) | LowPart;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_RATIONAL
{
    public uint Numerator;
    public uint Denominator;

    public readonly double Value => Denominator == 0 ? 0 : (double)Numerator / Denominator;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_SOURCE_INFO
{
    public LUID AdapterId;
    public uint Id;
    public uint ModeInfoIdx;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_TARGET_INFO
{
    public LUID AdapterId;
    public uint Id;
    public uint ModeInfoIdx;
    public uint OutputTechnology;
    public uint Rotation;
    public uint Scaling;
    public DISPLAYCONFIG_RATIONAL RefreshRate;
    public uint ScanLineOrdering;
    public int TargetAvailable;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_INFO
{
    public DISPLAYCONFIG_PATH_SOURCE_INFO SourceInfo;
    public DISPLAYCONFIG_PATH_TARGET_INFO TargetInfo;
    public uint Flags;
}

/// <summary>DISPLAYCONFIG_MODE_INFO with the source/target union flattened (64 bytes).</summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct DISPLAYCONFIG_MODE_INFO
{
    public const uint TypeSource = 1;
    public const uint TypeTarget = 2;

    [FieldOffset(0)] public uint InfoType;
    [FieldOffset(4)] public uint Id;
    [FieldOffset(8)] public LUID AdapterId;

    // Source mode (InfoType == 1)
    [FieldOffset(16)] public uint SourceWidth;
    [FieldOffset(20)] public uint SourceHeight;
    [FieldOffset(24)] public uint SourcePixelFormat;
    [FieldOffset(28)] public int SourcePositionX;
    [FieldOffset(32)] public int SourcePositionY;

    // Target mode (InfoType == 2): DISPLAYCONFIG_VIDEO_SIGNAL_INFO
    [FieldOffset(16)] public ulong PixelRate;
    [FieldOffset(24)] public DISPLAYCONFIG_RATIONAL HSyncFreq;
    [FieldOffset(32)] public DISPLAYCONFIG_RATIONAL VSyncFreq;
    [FieldOffset(40)] public uint ActiveWidth;
    [FieldOffset(44)] public uint ActiveHeight;
    [FieldOffset(48)] public uint TotalWidth;
    [FieldOffset(52)] public uint TotalHeight;
    [FieldOffset(56)] public uint VideoStandard;
    [FieldOffset(60)] public uint TargetScanLineOrdering;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_DEVICE_INFO_HEADER
{
    public uint Type;
    public uint Size;
    public LUID AdapterId;
    public uint Id;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
{
    public DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
    public fixed char ViewGdiDeviceName[32];
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DISPLAYCONFIG_TARGET_DEVICE_NAME
{
    public const uint FlagFriendlyNameFromEdid = 0x1;
    public const uint FlagEdidIdsValid = 0x4;

    public DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
    public uint Flags;
    public uint OutputTechnology;
    public ushort EdidManufactureId;
    public ushort EdidProductCodeId;
    public uint ConnectorInstance;
    public fixed char MonitorFriendlyDeviceName[64];
    public fixed char MonitorDevicePath[128];
}

/// <summary>DEVMODEW, display fields only (220 bytes).</summary>
[StructLayout(LayoutKind.Explicit, Size = 220, CharSet = CharSet.Unicode)]
internal struct DEVMODEW
{
    [FieldOffset(68)] public ushort Size;
    [FieldOffset(168)] public uint BitsPerPel;
    [FieldOffset(172)] public uint PelsWidth;
    [FieldOffset(176)] public uint PelsHeight;
    [FieldOffset(184)] public uint DisplayFrequency;
}

internal static partial class CcdNative
{
    public const uint QDC_ALL_PATHS = 0x1;
    public const uint QDC_ONLY_ACTIVE_PATHS = 0x2;
    public const int ERROR_SUCCESS = 0;
    public const int ERROR_INSUFFICIENT_BUFFER = 122;
    public const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    public const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;
    public const uint DISPLAYCONFIG_PATH_ACTIVE = 0x1;
    public const int ENUM_CURRENT_SETTINGS = -1;
    public const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x20;
    public const uint SDC_VALIDATE = 0x40;
    public const uint SDC_APPLY = 0x80;
    public const uint SDC_SAVE_TO_DATABASE = 0x200;
    public const uint SDC_ALLOW_CHANGES = 0x400;

    [LibraryImport("user32.dll")]
    public static partial int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [LibraryImport("user32.dll")]
    public static partial int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        IntPtr currentTopologyId);

    [LibraryImport("user32.dll")]
    public static partial int SetDisplayConfig(
        uint numPathArrayElements,
        [In] DISPLAYCONFIG_PATH_INFO[] pathArray,
        uint numModeInfoArrayElements,
        [In] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        uint flags);

    [LibraryImport("user32.dll")]
    public static partial int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

    [LibraryImport("user32.dll")]
    public static partial int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

    [LibraryImport("user32.dll", EntryPoint = "EnumDisplaySettingsW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODEW devMode);
}
