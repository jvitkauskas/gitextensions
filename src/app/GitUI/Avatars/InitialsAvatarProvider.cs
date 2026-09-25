using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils.GitUI.Theming;

namespace GitUI.Avatars;

/// <summary>
/// A provider that generates avatar images based on the initials of the user.
/// </summary>
public class InitialsAvatarProvider : IAvatarProvider
{
    private int _unkownCounter = 0;
    private static readonly char[] _emailInitialSeparator = ['.', '-', '_'];
    private string _fontFamily = "";

    public InitialsAvatarProvider()
    {
        UpdateFontsSettings();
    }

    /// <inheritdoc/>
    public Task<byte[]?> GetAvatarAsync(string email, string? name, int imageSize)
    {
        (string initials, int colorIndex) = GetInitialsAndColorIndex(email, name);

        (Color foregroundColor, Color backgroundColor) = _avatarColors[colorIndex];
        byte[] avatar = PngImages.DrawText(initials, foregroundColor, backgroundColor, imageSize, _fontFamily);

        return Task.FromResult<byte[]?>(avatar);
    }

    public bool PerformsIo => false;

    /// <summary>
    /// Calculate the most simpler non-cryptographic deterministic hash (to get the same result every times it is calculated for the same string)
    /// We just need an inexpensive way to convert a string to an integer, and calculating a hash is a good way to do it.
    /// We are not using <c>GetHashCode()</c> because it returns a different value for each process.
    /// Borrowed from https://stackoverflow.com/a/5155015
    /// </summary>
    /// <param name="str">The string to calculate a hash for.</param>
    /// <returns>The calculated hash.</returns>
    private static int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in str)
            {
                hash = (hash * 31) + c;
            }

            return Math.Abs(hash);
        }
    }

    protected internal (string initials, int hashCode) GetInitialsAndColorIndex(string email, string? name)
    {
        (string? selectedName, char[]? separator) = NameSelector(name, email);

        if (selectedName is null)
        {
            return ("?", _unkownCounter++);
        }

        string[] nameParts = selectedName.Split(separator);
        string initials = GetInitialsFromNames(nameParts);

        return (initials, GetDeterministicHashCode(email) % _avatarColors.Length);
    }

    private static (string? name, char[]? separator) NameSelector(string? name, string? email)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return (name.Trim(), null);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            string withoutDomain = email.LazySplit('@').First().TrimStart();
            return (withoutDomain, _emailInitialSeparator);
        }

        return (null, null);
    }

    private static string GetInitialsFromNames(string[]? possibleNames)
    {
        possibleNames = possibleNames?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        if (possibleNames?.Length is not > 0)
        {
            return "?";
        }

        string[]? names = possibleNames.Where(s => char.IsLetter(s[0]) || char.IsDigit(s[0])).ToArray();

        // if no valid name-elements are found, return acceptable fallback
        if (names?.Length is not > 0)
        {
            return possibleNames.Length > 1 ? $"{possibleNames[0][0]}{possibleNames[1][0]}" : $"{possibleNames[0][0]}";
        }

        string name = names[0];

        // If only a single valid name-element is found ...
        if (names.Length == 1)
        {
            // ... and that name-element is only a single character long ...
            if (name.Length == 1)
            {
                // ... return that character as uppercase
                return name.ToUpper();
            }

            if (char.IsUpper(name[1]))
            {
                return $"{char.ToUpper(name[0])}{name[1]}";
            }

            string[] splitNames = name.Split(_emailInitialSeparator);

            if (splitNames.Length > 1)
            {
                return GetInitialsFromNames(splitNames);
            }

            char[] upperChars = [.. name.Where(char.IsUpper)];
            if (upperChars.Length > 1)
            {
                return $"{upperChars[0]}{upperChars[^1]}";
            }

            // return first letter upper-case and second letter original/lower case.
            return $"{char.ToUpper(name[0])}{name[1]}";
        }

        // Return initials from first and last name-element as uppercase
        return $"{name[0]}{names[^1][0]}".ToUpper();
    }

    private readonly (Color foregroundColor, Color backgroundColor)[] _avatarColors = [.. AppSettings.AvatarAuthorInitialsPalette.Split(',').Select(GetAvatarDrawingMaterial)];

    private static (Color foregroundColor, Color backgroundColor) GetAvatarDrawingMaterial(string colorCode)
    {
        Color backgroundColor = ConvertToColor(colorCode);

        return (backgroundColor.GetContrastColor(AppSettings.AvatarAuthorInitialsLuminanceThreshold), backgroundColor);

        static Color ConvertToColor(string colorCode)
        {
            try
            {
                return ColorTranslator.FromHtml(colorCode);
            }
            catch (Exception)
            {
                return Color.Black;
            }
        }
    }

    public void UpdateFontsSettings()
    {
        // SkiaSharp falls back to the default family of the system if it does not have the family of the settings.
        _fontFamily = AppSettings.Font.FamilyName;
    }
}
