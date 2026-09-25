namespace GitExtensions.Extensibility;

/// <summary>
///  A font of the settings: its family, size and style, without GDI+ (<c>System.Drawing.Font</c> only works on Windows).
///  It is saved in the text format of <see cref="FontParser"/>, as the GDI+ fonts were.
/// </summary>
/// <param name="FamilyName">The name of the font family, resolved by the UI (which falls back to another family if the
///  system does not have it).</param>
/// <param name="SizeInPoints">The size, in points.</param>
public sealed record FontDescriptor(string FamilyName, float SizeInPoints, bool IsBold = false, bool IsItalic = false)
{
    /// <summary>The size in device independent pixels (1/96 inch), as the UI measures it.</summary>
    public double SizeInPixels => SizeInPoints * 96 / 72;
}
