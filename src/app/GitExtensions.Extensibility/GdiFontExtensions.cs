using System.Runtime.Versioning;

namespace GitExtensions.Extensibility;

/// <summary>The fonts of the settings as GDI+ fonts, for the Windows code that measures or draws with GDI+.</summary>
[SupportedOSPlatform("windows6.1")]
public static class GdiFontExtensions
{
    /// <summary>A new GDI+ font (the caller disposes it); GDI+ substitutes another family if the system lacks this one.</summary>
    public static Font ToFont(this FontDescriptor font)
    {
        ArgumentNullException.ThrowIfNull(font);
        FontStyle style = (font.IsBold ? FontStyle.Bold : FontStyle.Regular) | (font.IsItalic ? FontStyle.Italic : FontStyle.Regular);
        return new Font(font.FamilyName, font.SizeInPoints, style, GraphicsUnit.Point);
    }

    public static FontDescriptor ToFontDescriptor(this Font font)
    {
        ArgumentNullException.ThrowIfNull(font);
        return new FontDescriptor(font.FontFamily.Name, font.SizeInPoints, font.Bold, font.Italic);
    }
}
