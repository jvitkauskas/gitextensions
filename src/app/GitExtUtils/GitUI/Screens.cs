using System.Runtime.InteropServices;

namespace GitExtUtils.GitUI;

/// <summary>The monitors of the desktop (as the WinForms <c>Screen.AllScreens</c>).</summary>
public static class Screens
{
    private const uint MONITORINFOF_PRIMARY = 1;

    /// <summary>The bounds of each monitor, in pixels, and whether it is the primary monitor.</summary>
    public static IReadOnlyList<(Rectangle Bounds, bool IsPrimary)> GetAll()
    {
        List<(Rectangle Bounds, bool IsPrimary)> screens = [];
        EnumDisplayMonitors(0, 0, (monitor, _, _, _) =>
        {
            MonitorInfo info = new() { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfoW(monitor, ref info))
            {
                screens.Add((Rectangle.FromLTRB(info.Monitor.Left, info.Monitor.Top, info.Monitor.Right, info.Monitor.Bottom), (info.Flags & MONITORINFOF_PRIMARY) != 0));
            }

            return true;
        }, 0);
        return screens;
    }

    private delegate bool MonitorEnumProcedure(nint monitor, nint deviceContext, nint rect, nint data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint deviceContext, nint clip, MonitorEnumProcedure callback, nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfoW(nint monitor, ref MonitorInfo info);
}
