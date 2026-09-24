using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using GitExtUtils.GitUI.Theming;
using Color = System.Drawing.Color;

namespace GitUI.Presentation.Editor;

/// <summary>The theme colors of the diff viewer, as the host's Git Extensions theme defines them.</summary>
public interface IThemeColors
{
    /// <summary>The color of the theme (as <c>AppColorExtension.GetThemeColor</c>).</summary>
    Color GetColor(AppColor color);

    /// <summary>Whether the app is in dark mode (as <c>Application.IsDarkModeEnabled</c>).</summary>
    bool IsDarkMode { get; }
}

/// <summary>The colors of the default (invariant) theme, e.g. for tests.</summary>
public sealed class DefaultThemeColors : IThemeColors
{
    public static DefaultThemeColors Instance { get; } = new();

    public Color GetColor(AppColor color) => Theme.Default.GetColor(color);

    public bool IsDarkMode => false;
}

/// <summary>A range of text colored by git (<see cref="ForeColor"/> is <see langword="null"/> if it is not set).</summary>
public sealed record ColoredSegment(int Offset, int Length, Color BackColor, Color? ForeColor);

/// <summary>
///  Converts the ANSI escape sequences of git's colored output to colored segments of the text. A port of the WinForms
///  <c>AnsiEscapeUtilities.ParseEscape</c> (docs/avalonia-port/PLAN.md, phase 3); keep it in sync. The colors are computed
///  as the WinForms viewer does, from the theme's <see cref="AppColor"/>s and <c>ColorHelper</c>.
/// </summary>
public static partial class AnsiEscapeParser
{
    [GeneratedRegex(@"\u001b\[((?<escNo>\d+)\s*[:;]?\s*)*m", RegexOptions.ExplicitCapture)]
    private static partial Regex EscapeRegex { get; }

    // Color code definitions
    private const int _blackId = 0;
    private const int _redId = 1;
    private const int _greenId = 2;
    private const int _yellowId = 3;
    private const int _blueId = 4;
    private const int _magentaId = 5;
    private const int _cyanId = 6;
    private const int _whiteId = 7;

    private const int _boldOffset = 8;

    /// <summary>Whether the text contains escape sequences.</summary>
    public static bool HasEscapes(string text) => text.Contains('\u001b');

    /// <summary>Returns the text without the escape sequences, and its colored segments.</summary>
    public static (string Text, IReadOnlyList<ColoredSegment> Segments) Parse(string text, IThemeColors colors, bool themeColors = false)
    {
        StringBuilder sb = new(text.Length);
        List<ColoredSegment> segments = [];
        Parse(text, sb, segments, colors, themeColors);
        return (sb.ToString(), segments);
    }

    /// <summary>
    ///  As <c>AnsiEscapeUtilities.ParseEscape</c>: appends the text without the escape sequences to <paramref name="sb"/>, and its
    ///  colored segments (with the offsets in <paramref name="sb"/>) to <paramref name="segments"/>, merged with the last one if
    ///  they continue it.
    /// </summary>
    public static void Parse(string text, StringBuilder sb, List<ColoredSegment> segments, IThemeColors colors, bool themeColors = false)
    {
        List<ColoredSegment> markers = segments;
        int defaultForeColorId = colors.IsDarkMode ? _whiteId : _blackId;
        int prevLineOffset = 0;
        int currentColorId = defaultForeColorId; // current color, used when just bold etc is set
        HighlightInfo currentHighlight = new()
        {
            DocOffset = sb.Length,
            Length = -1,
            BackColor = colors.GetColor(AppColor.EditorBackground),
        };

        for (Match match = EscapeRegex.Match(text); match.Success; match = match.NextMatch())
        {
            sb.Append(text.AsSpan(prevLineOffset, match.Index - prevLineOffset));
            prevLineOffset = match.Index + match.Length;

            // An escape sequence can include several attributes (empty/unparsable is break).
            List<int> escapeCodes = [.. match.Groups["escNo"].Captures.Select(i => int.TryParse(i.ValueSpan, out int attribute) ? attribute : 0)];

            if (TryGetColorsFromEscapeSequence(escapeCodes, out Color? backColor, out Color? foreColor, ref currentColorId, defaultForeColorId, colors, themeColors))
            {
                if (currentHighlight.Length >= 0)
                {
                    // End current match so new can be started, this is expected but normally not used by Git (?).
                    // Also assume that the new colors are "complete", not just overlay.
                    EndCurrentHighlight();
                }

                // Start of new segment
                currentHighlight.DocOffset = sb.Length;
                currentHighlight.Length = 0;
                currentHighlight.BackColor = backColor;
                currentHighlight.ForeColor = foreColor;
            }
            else
            {
                EndCurrentHighlight();
            }
        }

        sb.Append(text.AsSpan(prevLineOffset));
        EndCurrentHighlight();

        return;

        void EndCurrentHighlight()
        {
            // Reset escape sequence, end of segment
            if (currentHighlight.Length < 0 || sb.Length == currentHighlight.DocOffset)
            {
                // Previous was a reset, just ignore.
                currentHighlight.Length = -1;
                return;
            }

            if (currentHighlight.Length > 0)
            {
                // This could be escapeCodes setting without any effect,
                // Git also ends "configured with empty" (like diff context set in GE) without start with end segment.
                return;
            }

            int len = sb.Length - currentHighlight.DocOffset;
            if (len == 1 && sb[^1] == '\r')
            {
                // Marker without any visible effect, likely diff.colormovedws
                currentHighlight.Length = -1;
                ++currentHighlight.DocOffset;
                return;
            }

            currentHighlight.Length = len;
            AddMarker(currentHighlight, markers, sb, colors);
            currentHighlight.Length = -1;
        }
    }

    /// <summary>As <c>AnsiEscapeUtilities.TryGetColorsFromEscapeSequence</c>.</summary>
    private static bool TryGetColorsFromEscapeSequence(List<int> escapeCodes, out Color? backColor, out Color? foreColor, ref int currentColorId, int defaultForeColorId, IThemeColors colors, bool themeColors)
    {
        bool result = false;
        backColor = null;
        foreColor = null;
        int currentFore = -1;
        int currentBack = -1;

        // A subset of attributes supported, most are ignored, other handled as bold/dim
        bool reverse = false; // swap fore/back
        bool bold = false;
        bool dim = false;
        bool isChange = false; // Handle only bold/dim changes to current colors

        if (escapeCodes.Count == 0)
        {
            // Empty sequence is the same as reset to normal
            escapeCodes = [0];
        }

        for (int i = 0; i < escapeCodes.Count; ++i)
        {
            switch (escapeCodes[i])
            {
                case 0: // Reset or normal, all attributes become turned off
                    result = false;
                    reverse = false;
                    backColor = null;
                    foreColor = null;
                    currentColorId = defaultForeColorId;
                    currentFore = -1;
                    currentBack = -1;
                    bold = false;
                    dim = false;
                    break;
                case 1: // bold
                case 4: // underline/ul
                case 5: // slow blink
                case 6: // fast blink
                case 9: // strike
                    bold = true;
                    isChange = true;
                    break;
                case 2: // dim
                case 3: // italic
                case 8: // conceal
                    dim = true;
                    isChange = true;
                    break;
                case 7: // reverse
                    reverse = true;
                    break;
                case 22: // normal color, intensity
                    bold = false;
                    dim = false;
                    break;
                case 39: // Default foreground color
                    foreColor = null;
                    currentFore = currentColorId = defaultForeColorId;
                    break;
                case 49: // Default background color
                    backColor = null;
                    currentBack = -1;
                    isChange = true;
                    break;
                case >= 30 and <= 37: // Set foreground color
                    currentFore = escapeCodes[i] - 30;
                    break;
                case >= 90 and <= 97: // Set bold foreground color
                    // can be combined with explicit bold to get extra bold
                    currentFore = escapeCodes[i] - 90 + _boldOffset;
                    break;
                case >= 40 and <= 47: // Set background color
                    currentBack = escapeCodes[i] - 40;
                    break;
                case >= 100 and <= 107: // Set bold background color
                    currentBack = escapeCodes[i] - 100 + _boldOffset;
                    break;
                case 38: // Set foreground color with sequence
                case 48: // Set background color with sequence
                    if (i >= escapeCodes.Count - 2)
                    {
                        Trace.WriteLine($"Unexpected too few arguments for {i} {escapeCodes}");
                        break;
                    }

                    bool fore = escapeCodes[i] == 38;
                    ++i;

                    if (escapeCodes[i] == 5)
                    {
                        // ESC[38:5:⟨n⟩m Select foreground color
                        ++i;
                        int id = escapeCodes[i];
                        if (fore)
                        {
                            currentFore = id;
                        }
                        else
                        {
                            currentBack = id;
                        }
                    }
                    else if (escapeCodes[i] == 2)
                    {
                        // ESC[38;2;⟨r⟩;⟨g⟩;⟨b⟩ m Select RGB foreground color
                        if (i >= escapeCodes.Count - 3)
                        {
                            Trace.WriteLine($"Unexpected too few arguments for {i} {escapeCodes}");
                            break;
                        }

                        Color color = Color.FromArgb(escapeCodes[i + 1], escapeCodes[i + 2], escapeCodes[i + 3]);
                        i += 3;

                        // Unknown fixed identifier, reset id
                        currentColorId = defaultForeColorId;

                        // Set the color, override if set later
                        if (fore)
                        {
                            currentFore = -1;
                            foreColor = color;
                        }
                        else
                        {
                            currentBack = -1;
                            backColor = color;
                        }
                    }
                    else
                    {
                        Trace.WriteLine($"Unexpected argument for {i} {escapeCodes}");
                    }

                    break;
                default: // Ignore unhandled sequences
                    break;
            }
        }

        if (currentFore is >= _blackId and <= _whiteId + _boldOffset)
        {
            // Mask bold, last three bits
            currentColorId = currentFore & (_boldOffset - 1);
        }

        if (themeColors && !reverse && !dim
            && (currentBack < _blackId && backColor is null)
            && foreColor is null
            && currentFore is _redId or _greenId or _redId + _boldOffset or _greenId + _boldOffset)
        {
            // Assume this is a fit for the theme colors with reverse color (e.g. difftastic)
            // Change extra-bold -> bold, bold -> normal, normal -> dim (and dim -> dim) to match GE theme better
            bool backBold = bold;
            bool backDim = false;
            if (currentFore > _whiteId)
            {
                // bold is valid
                currentFore -= _boldOffset;
            }
            else if (bold)
            {
                backBold = false;
            }
            else
            {
                backDim = true;
            }

            backColor = Get8bitColor(currentFore, fore: false, bold: backBold, dim: backDim, colors);
            currentFore = -1;
        }

        if (isChange && (foreColor is null && backColor is null && currentFore < _blackId && currentBack < _blackId))
        {
            currentFore = currentColorId;
        }

        if (reverse)
        {
            (backColor, foreColor) = (foreColor, backColor);
            (currentBack, currentFore) = (currentFore, currentBack);
        }

        if (currentFore >= _blackId)
        {
            foreColor = Get8bitColor(currentFore, fore: true, bold, dim, colors);
        }

        if (currentBack >= _blackId)
        {
            backColor = Get8bitColor(currentBack, fore: false, bold, dim, colors);
        }

        if (backColor is Color back && foreColor is null)
        {
            foreColor = back.GetTextColor();
        }

        // Set result if there are changes
        // No action if there are only attribute changes like bold/dim/reverse
        if (backColor is not null || foreColor is not null)
        {
            result = true;
        }

        return result;
    }

    /// <summary>As <c>AnsiEscapeUtilities.Get8bitColor</c>: 3, 4, and 8-bit colors.</summary>
    private static Color Get8bitColor(int colorCode, bool fore, bool bold, bool dim, IThemeColors colors)
    {
        bool extraBold = false;
        if (bold)
        {
            if (colorCode is >= _blackId and <= _whiteId)
            {
                // bold from defined colors
                colorCode += _boldOffset;
            }
            else if (colorCode <= _whiteId + _boldOffset)
            {
                // extra bright adjustment, not standard defined
                extraBold = true;
            }
        }

        Color color = colorCode switch
        {
            _blackId => colors.GetColor(fore ? AppColor.AnsiTerminalBlackForeNormal : AppColor.AnsiTerminalBlackBackNormal),
            _blackId + _boldOffset => colors.GetColor(fore ? AppColor.AnsiTerminalBlackForeBold : AppColor.AnsiTerminalBlackBackBold),
            _redId => colors.GetColor(fore ? AppColor.AnsiTerminalRedForeNormal : AppColor.AnsiTerminalRedBackNormal),
            _redId + _boldOffset => colors.GetColor(fore ? AppColor.AnsiTerminalRedForeBold : AppColor.AnsiTerminalRedBackBold),
            _greenId => colors.GetColor(fore ? AppColor.AnsiTerminalGreenForeNormal : AppColor.AnsiTerminalGreenBackNormal),
            _greenId + _boldOffset => colors.GetColor(fore ? AppColor.AnsiTerminalGreenForeBold : AppColor.AnsiTerminalGreenBackBold),
            _yellowId => colors.GetColor(fore ? AppColor.AnsiTerminalYellowForeNormal : AppColor.AnsiTerminalYellowBackNormal),
            _yellowId + _boldOffset => colors.GetColor(fore ? AppColor.AnsiTerminalYellowForeBold : AppColor.AnsiTerminalYellowBackBold),
            _blueId => colors.GetColor(fore ? AppColor.AnsiTerminalBlueForeNormal : AppColor.AnsiTerminalBlueBackNormal),
            _blueId + _boldOffset => colors.GetColor(fore ? AppColor.AnsiTerminalBlueForeBold : AppColor.AnsiTerminalBlueBackBold),
            _magentaId => colors.GetColor(fore ? AppColor.AnsiTerminalMagentaForeNormal : AppColor.AnsiTerminalMagentaBackNormal),
            _magentaId + _boldOffset => colors.GetColor(fore ? AppColor.AnsiTerminalMagentaForeBold : AppColor.AnsiTerminalMagentaBackBold),
            _cyanId => colors.GetColor(fore ? AppColor.AnsiTerminalCyanForeNormal : AppColor.AnsiTerminalCyanBackNormal),
            _cyanId + _boldOffset => colors.GetColor(fore ? AppColor.AnsiTerminalCyanForeBold : AppColor.AnsiTerminalCyanBackBold),
            _whiteId => colors.GetColor(fore ? AppColor.AnsiTerminalWhiteForeNormal : AppColor.AnsiTerminalWhiteBackNormal),
            _whiteId + _boldOffset => colors.GetColor(fore ? AppColor.AnsiTerminalWhiteForeBold : AppColor.AnsiTerminalWhiteBackBold),
            >= 16 and < 232 => Get216Colors(colorCode),
            >= 232 and <= 255 => Get24StepGray(colorCode),
            _ => throw new ArgumentOutOfRangeException(nameof(colorCode), colorCode, "Unexpected value for ANSI color.")
        };

        if (extraBold)
        {
            color = color.MakeDarkerBy(-0.1);
        }

        if (dim)
        {
            color = color.DimColor();
        }

        return color;

        static Color Get216Colors(int level)
        {
            int i = level - 16;
            int blue = (i % 6) * 51;
            int green = ((i % 36) / 6) * 51;
            int red = (i / 36) * 51;
            return Color.FromArgb(red, green, blue);
        }

        static Color Get24StepGray(int level)
        {
            // Convert 0-23 to 0-253
            int i = (level - 232) * 11;
            return Color.FromArgb(i, i, i);
        }
    }

    /// <summary>As <c>AnsiEscapeUtilities.TryGetTextMarker</c>: adds the segment, or merges it with the previous one.</summary>
    private static void AddMarker(HighlightInfo hl, List<ColoredSegment> markers, StringBuilder sb, IThemeColors colors)
    {
        // BackColor must always be set
        Color backColor = hl.BackColor ?? colors.GetColor(AppColor.EditorBackground);

        // Check if segment can be merged with the previous
        if (markers.Count > 0
            && markers[^1] is { ForeColor: Color prevForeColor } prevMarker
            && prevMarker.BackColor == backColor
            && prevForeColor == hl.ForeColor)
        {
            int gapLen = hl.DocOffset - (prevMarker.Offset + prevMarker.Length);
            if (gapLen == 0)
            {
                // zero gap, Git often have consecutive sections (like '+' in separate)
            }
            else if (gapLen == 1 && sb[hl.DocOffset - 1] is ('\n' or '\r'))
            {
                // Only \n, gap is a newline, not colored in the viewer
            }
            else if (gapLen == 2 && sb[hl.DocOffset - 2] == '\r' && sb[hl.DocOffset - 1] == '\n')
            {
                // Only \r\n
            }
            else
            {
                gapLen = -1;
            }

            if (gapLen >= 0)
            {
                markers[^1] = prevMarker with { Length = prevMarker.Length + gapLen + hl.Length };
                return;
            }
        }

        markers.Add(new ColoredSegment(hl.DocOffset, hl.Length, backColor, hl.ForeColor));
    }

    private struct HighlightInfo
    {
        public int DocOffset { get; set; }
        public int Length { get; set; }
        public Color? BackColor { get; set; }
        public Color? ForeColor { get; set; }
    }
}
