using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace GitUI.Avalonia.Converters;

/// <summary>Loads the image file at the bound path; <see langword="null"/> for no path or an unreadable file.</summary>
public sealed class FilePathToBitmapConverter : IValueConverter
{
    public static FilePathToBitmapConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            return new Bitmap(path);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
