using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GitExtensions.Extensibility;

public static class FontParser
{
    private const string InvariantCultureId = "_IC_";

    public static string AsString(this FontDescriptor value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.Format(CultureInfo.InvariantCulture,
            "{0};{1};{2};{3};{4}", value.FamilyName, value.SizeInPoints, InvariantCultureId, value.IsBold ? 1 : 0, value.IsItalic ? 1 : 0);
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static FontDescriptor? Parse(this string? value, FontDescriptor? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        string[] parts = value.Split(';');
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return defaultValue;
        }

        string fontSize;
        if (parts.Length == 3 && parts[2] == InvariantCultureId)
        {
            fontSize = parts[1];
        }
        else
        {
            fontSize = parts[1].Replace(",", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
            fontSize = fontSize.Replace(".", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
        }

        // As the GDI+ font that was created from the text: a size that is not a positive number gives the default.
        if (!float.TryParse(fontSize, NumberStyles.Float, CultureInfo.InvariantCulture, out float size) || !float.IsFinite(size) || size <= 0)
        {
            return defaultValue;
        }

        bool isBold = parts.Length > 3 && parts[3] == "1";
        bool isItalic = parts.Length > 4 && parts[4] == "1";
        return new FontDescriptor(parts[0], size, isBold, isItalic);
    }
}
