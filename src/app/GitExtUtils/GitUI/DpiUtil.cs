using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace GitExtUtils.GitUI;

/// <summary>
/// Utility class related to DPI settings, primarily used for scaling dimensions on high-DPI displays.
/// </summary>
public static class DpiUtil
{
    public static int DpiX { get; private set; }
    public static int DpiY { get; private set; }

    public static float ScaleX { get; private set; }
    public static float ScaleY { get; private set; }

    /// <summary>
    ///  Off Windows, where there is no GDI to ask, the scaling of the UI as Avalonia renders it (e.g. 2 on a Retina display of
    ///  macOS), so that the images drawn for a size (the avatars) have the pixels of the screen. Windows keeps the DPI of GDI.
    /// </summary>
    public static void UseRenderScaling(double renderScaling)
    {
        if (OperatingSystem.IsWindows() || renderScaling <= 0)
        {
            return;
        }

        DpiX = DpiY = (int)Math.Round(96 * renderScaling);
        ScaleX = ScaleY = (float)renderScaling;
    }

    static DpiUtil()
    {
        // The DPI of the screen is read from GDI; elsewhere the Avalonia render scaling applies (phase 2).
        if (!OperatingSystem.IsWindows())
        {
            DpiX = 96;
            DpiY = 96;

            ScaleX = 1.0f;
            ScaleY = 1.0f;
            return;
        }

        using DeviceContextSafeHandle hdc = GetDC(IntPtr.Zero);
        try
        {
            const int LOGPIXELSX = 88;
            const int LOGPIXELSY = 90;

            DpiX = GetDeviceCaps(hdc, LOGPIXELSX);
            DpiY = GetDeviceCaps(hdc, LOGPIXELSY);

            ScaleX = DpiX / 96.0f;
            ScaleY = DpiY / 96.0f;
        }
        catch
        {
            DpiX = 96;
            DpiY = 96;

            ScaleX = 1.0f;
            ScaleY = 1.0f;
        }
    }

    /// <summary>
    /// Gets whether the current scaling factor is not integer.
    /// </summary>
    public static bool IsFractional => Math.Floor(ScaleX) != ScaleX || Math.Floor(ScaleY) != ScaleY;

    /// <summary>
    /// Gets whether the current pixel density is not 96 DPI.
    /// </summary>
    public static bool IsNonStandard => DpiX != 96 || DpiY != 96;

    /// <summary>
    /// Returns a scaled copy of <paramref name="size"/> which takes equivalent
    /// screen space at the current DPI as the original would at 96 DPI.
    /// </summary>
    public static Size Scale(Size size)
    {
        Scale(ref size);
        return size;
    }

    /// <summary>
    /// Returns a scaled copy of <paramref name="size"/> which takes equivalent
    /// screen space at the current DPI as the original would at <paramref name="originalDpi"/>.
    /// </summary>
    public static Size Scale(Size size, int originalDpi)
    {
        float scale = (float)DpiX / originalDpi;

        return new Size(
            (int)(size.Width * scale),
            (int)(size.Height * scale));
    }

    /// <summary>
    /// Modifies <paramref name="size"/> in place so that it takes equivalent screen
    /// space at the current DPI as the original value would at 96 DPI.
    /// </summary>
    public static void Scale(ref Size size)
    {
        size.Width = (int)(size.Width * ScaleX);
        size.Height = (int)(size.Height * ScaleY);
    }

    /// <summary>
    /// Returns a scaled copy of measurement <paramref name="i"/> which has
    /// equivalent length on screen at the current DPI as the original would
    /// at 96 DPI.
    /// </summary>
    /// <param name="i">The value to scale.</param>
    /// <param name="ceiling">If <see langword="true" />, uses ceiling rounding to ensure the result is never smaller than the scaled value.</param>
    public static int Scale(int i, bool ceiling)
    {
        return ceiling ? (int)Math.Ceiling(i * ScaleX) : (int)Math.Round(i * ScaleX);
    }

    /// <summary>
    /// Returns a scaled copy of measurement <paramref name="i"/> which has
    /// equivalent length on screen at the current DPI as the original would
    /// at 96 DPI.
    /// </summary>
    /// <param name="i">The value to scale.</param>
    public static int Scale(int i) => Scale(i, ceiling: false);

    /// <summary>
    /// Returns a scaled copy of <paramref name="i"/> which has equivalent
    /// length on screen at the current DPI as the original would at
    /// <paramref name="originalDpi"/>.
    /// </summary>
    public static int Scale(int i, int originalDpi)
    {
        float scale = (float)DpiX / originalDpi;

        return (int)(i * scale);
    }

    /// <summary>
    /// Returns a scaled copy of measurement <paramref name="i"/> which has
    /// equivalent length on screen at the current DPI at the original would
    /// at 96 DPI.
    /// </summary>
    public static float Scale(float i)
    {
        return (float)Math.Round(i * ScaleX);
    }

    /// <summary>
    /// Returns a scaled copy of <paramref name="f"/> which has equivalent
    /// length on screen at the current DPI as the original would at
    /// <paramref name="originalDpi"/>.
    /// </summary>
    public static float Scale(float f, int originalDpi)
    {
        float scale = (float)DpiX / originalDpi;

        return f * scale;
    }

    /// <summary>
    /// Modifies <paramref name="point"/> in place so that it has equivalent physical
    /// screen position at the current DPI as the original value would at 96 DPI.
    /// </summary>
    public static Point Scale(Point point)
    {
        return new Point(
            (int)(point.X * ScaleX),
            (int)(point.Y * ScaleY));
    }

    /// <summary>
    /// Modifies <paramref name="point"/> in place so that it has equivalent physical
    /// screen position at the current DPI as the original value would at <paramref name="originalDpi"/>.
    /// </summary>
    public static Point Scale(Point point, int originalDpi)
    {
        float scale = (float)DpiX / originalDpi;

        return new Point(
            (int)(point.X * scale),
            (int)(point.Y * scale));
    }

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(DeviceContextSafeHandle hdc, int index);

    [DllImport("user32.dll")]
    private static extern DeviceContextSafeHandle GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr deviceContextHandle);

    [UsedImplicitly]
    private sealed class DeviceContextSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Called by P/Invoke.
        /// </summary>
        public DeviceContextSafeHandle()
            : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            ReleaseDC(IntPtr.Zero, handle);
            return true;
        }
    }
}
