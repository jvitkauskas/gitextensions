using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GitUI.ScriptsEngine;

// WARNING: This class is serialized to XML!
public partial class ScriptInfo
{
    // Match a single '&' (lookahead to not be followed by a second '&')
    [GeneratedRegex("&(?!&)", RegexOptions.ExplicitCapture)]
    private static partial Regex MnemonicAmpersandRegex { get; }

    private byte[]? _icon;

    public bool Enabled { get; set; } = true;

    public string? Name { get; set; }

    public string? Command { get; set; }

    public string? Arguments { get; set; }

    public bool AddToRevisionGridContextMenu { get; set; }

    public ScriptEvent OnEvent { get; set; }

    public bool AskConfirmation { get; set; }

    public bool RunInBackground { get; set; }

    public bool IsPowerShell { get; set; }

    public int HotkeyCommandIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the icon name.
    /// </summary>
    public string? Icon
    {
        get;
        set
        {
            field = value;
            _icon = null;
        }
    }

    /// <summary>
    /// Gets or sets the path to the file containing the icon.
    /// </summary>
    public string? IconFilePath
    {
        get;
        set
        {
            field = value;
            _icon = null;
        }
    }

    /// <summary>
    ///  Returns the name with mnemonic ampersands removed.
    /// </summary>
    public string GetDisplayName() => MnemonicAmpersandRegex.Replace(Name!, "");

    /// <summary>
    ///  Gets the icon (PNG data): the image file <see cref="IconFilePath"/> (on Windows also the icon of an executable),
    ///  else the icon of the resources named <see cref="Icon"/> (<see cref="EmbeddedIcons"/>).
    /// </summary>
    public byte[]? GetIcon()
    {
        return _icon ??= GetIcon();

        byte[]? GetIcon()
        {
            if (File.Exists(IconFilePath))
            {
                try
                {
                    if (PngImages.ToPng(File.ReadAllBytes(IconFilePath)) is byte[] image)
                    {
                        return image;
                    }

                    if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                    {
                        using Icon? associatedIcon = System.Drawing.Icon.ExtractAssociatedIcon(IconFilePath);
                        if (associatedIcon is not null)
                        {
                            using Bitmap bitmap = associatedIcon.ToBitmap();
                            return bitmap.ToPngData();
                        }
                    }
                }
                catch
                {
                }
            }

            if (string.IsNullOrWhiteSpace(Icon))
            {
                return null;
            }

            byte[]? icon = EmbeddedIcons.TryGet(Icon);
            if (icon is null)
            {
                // Discard the invalid name in order to not search for it again
                Trace.WriteLine(@$"The icon ""{Icon}"" for user script ""{GetDisplayName()}"" does not exist.");
                Icon = null;
            }

            return icon;
        }
    }
}
