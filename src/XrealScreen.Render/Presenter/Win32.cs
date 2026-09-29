using System.Runtime.InteropServices;

namespace XrealScreen.Render.Presenter;

internal static unsafe partial class Win32
{
    public const uint WS_POPUP = 0x80000000;
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;
    public const uint WS_EX_TOPMOST = 0x00000008;
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_CLOSE = 0x0010;
    public const uint WM_KEYDOWN = 0x0100;
    public const int VK_ESCAPE = 0x1B;
    public const int VK_R = 0x52;
    public const int VK_Q = 0x51;
    public const uint WM_HOTKEY = 0x0312;
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_NOREPEAT = 0x4000;
    public const int HotkeyRecenter = 1;
    public const int HotkeyStop = 2;
    public const int HotkeyCloser = 3;
    public const int HotkeyFarther = 4;
    public const int HotkeyCloserNumpad = 5;
    public const int HotkeyFartherNumpad = 6;
    public const int VK_OEM_PLUS = 0xBB;
    public const int VK_OEM_MINUS = 0xBD;
    public const int VK_ADD = 0x6B;
    public const int VK_SUBTRACT = 0x6D;
    public const int IDC_ARROW = 32512;
    public const uint WM_SETCURSOR = 0x0020;
    public const int HTCLIENT = 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, IntPtr, IntPtr> lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public char* lpszMenuName;
        public char* lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
        public uint lPrivate;
    }

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW")]
    public static partial ushort RegisterClassEx(WNDCLASSEXW* wc);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr CreateWindowEx(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static partial IntPtr DefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", EntryPoint = "GetMessageW")]
    public static partial int GetMessage(MSG* msg, IntPtr hwnd, uint min, uint max);

    [LibraryImport("user32.dll")]
    public static partial int TranslateMessage(MSG* msg);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    public static partial IntPtr DispatchMessage(MSG* msg);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    public static partial void PostQuitMessage(int exitCode);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(IntPtr hwnd, int id);

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetCursor(IntPtr cursor);

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")]
    public static partial IntPtr LoadCursor(IntPtr instance, IntPtr cursorName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetProcessDpiAwarenessContext(IntPtr value);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW")]
    public static partial IntPtr CreateWaitableTimerEx(IntPtr attributes, IntPtr name, uint flags, uint access);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWaitableTimer(IntPtr timer, long* dueTime, int period, IntPtr completion, IntPtr arg, [MarshalAs(UnmanagedType.Bool)] bool resume);

    [LibraryImport("kernel32.dll")]
    public static partial uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(IntPtr handle);

    /// <summary>High-resolution timer (~0.5 ms); Thread.Sleep can be 15.6 ms coarse and missed vblanks ([verified-local]).</summary>
    public static IntPtr CreateHighResolutionTimer() =>
        CreateWaitableTimerEx(IntPtr.Zero, IntPtr.Zero, 0x2 /* CREATE_WAITABLE_TIMER_HIGH_RESOLUTION */, 0x1F0003 /* TIMER_ALL_ACCESS */);

    public static void SleepPrecise(IntPtr timer, double milliseconds)
    {
        long due = -(long)(milliseconds * 10_000); // relative, 100 ns units
        if (timer != IntPtr.Zero && SetWaitableTimer(timer, &due, 0, IntPtr.Zero, IntPtr.Zero, false))
        {
            _ = WaitForSingleObject(timer, 0xFFFFFFFF);
        }
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")]
    public static partial IntPtr GetModuleHandle(IntPtr name);

    /// <summary>Makes window/CCD coordinates physical pixels. Safe to call more than once.</summary>
    public static void EnablePerMonitorDpi() => SetProcessDpiAwarenessContext(-4 /* PER_MONITOR_AWARE_V2 */);
}
