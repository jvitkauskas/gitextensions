using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace GitUI;

[SupportedOSPlatform("windows6.1")]
public static class BitmapExtensions
{
    /// <summary>The PNG data of a GDI+ image (the icons of the shell and of the plugins of API v1 and v2).</summary>
    public static byte[] ToPngData(this Image image)
    {
        using MemoryStream stream = new();
        image.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    public static Icon ToIcon(this Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        IntPtr handle = IntPtr.Zero;
        try
        {
            handle = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(handle);

            return (Icon)icon.Clone();
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(handle);
            }
        }
    }
}
