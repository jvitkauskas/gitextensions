using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using GitUI.Presentation.Editor;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using TextMateFontStyle = TextMateSharp.Themes.FontStyle;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The syntax highlighting of a file or of a diff by the TextMate grammar of its language (the grammars and the Light+ /
///  Dark+ themes of Visual Studio Code, from TextMateSharp): the lines are tokenized when they are first shown, each one
///  from the state at the end of the previous one.
/// </summary>
/// <remarks>
///  A diff is highlighted as its two files: the removed and the context lines are tokenized as the old file, the added
///  and the context lines as the new one, without the prefixes of the diff, so that e.g. a comment opened in a removed
///  line does not continue into the added lines. The text of the default color is left in the color of the editor.
/// </remarks>
internal sealed class TextMateColorizer : DocumentColorizingTransformer
{
    // Beyond these, as Visual Studio Code: the lines are not tokenized (a minified file), or the file is not highlighted.
    private const int MaxLineLength = 5000;
    private const int MaxLineCount = 50_000;
    private static readonly TimeSpan LineTimeLimit = TimeSpan.FromMilliseconds(50);

    private static readonly Dictionary<ThemeName, (RegistryOptions Options, Registry Registry)> Registries = [];

    // The foreground of the text of no scope (the default foreground of the theme).
    private const int DefaultForeground = 1;

    private readonly IGrammar _grammar;
    private readonly Theme _theme;
    private readonly Dictionary<int, IBrush?> _brushes = [];

    // For each tokenized line: its tokens (null: none), and the states of the old and new files after it.
    private readonly List<int[]?> _tokens = [];
    private readonly List<(IStateStack? Old, IStateStack? New)> _states = [];
    private Dictionary<int, DiffLineKind>? _diffLineKinds;
    private int _diffPrefixLength;
    private bool _isWordDiffOrGrep;

    private TextMateColorizer(IGrammar grammar, Theme theme)
    {
        _grammar = grammar;
        _theme = theme;
    }

    /// <summary>The colorizer of the language of <paramref name="fileName"/> (by its extension), if TextMate has a grammar for it.</summary>
    public static TextMateColorizer? TryCreate(string fileName, bool isDarkTheme)
    {
        string extension = Path.GetExtension(fileName);
        if (extension.Length == 0)
        {
            return null;
        }

        (RegistryOptions options, Registry registry) = GetRegistry(isDarkTheme ? ThemeName.DarkPlus : ThemeName.LightPlus);
        try
        {
            return options.GetScopeByExtension(extension) is { } scope && registry.LoadGrammar(scope) is { } grammar
                ? new TextMateColorizer(grammar, registry.GetTheme())
                : null;
        }
        catch (Exception ex)
        {
            // A grammar that fails to load: no highlighting rather than no file.
            System.Diagnostics.Trace.WriteLine(ex);
            return null;
        }
    }

    /// <summary>
    ///  Highlights the text as a diff whose lines are <paramref name="lines"/>, or as a file for <see langword="null"/>. The
    ///  lines of a word diff or of git grep, which are not those of the files, are not highlighted.
    /// </summary>
    public void SetDiff(IReadOnlyList<DiffLine>? lines, bool isCombinedDiff)
    {
        _diffLineKinds = null;
        if (lines is not null)
        {
            _diffLineKinds = [];
            foreach (DiffLine line in lines)
            {
                _diffLineKinds[line.LineNumInDiff] = line.Kind;
            }
        }

        _diffPrefixLength = isCombinedDiff ? 2 : 1;
        _isWordDiffOrGrep = lines?.Any(line => line.Kind is DiffLineKind.MinusLeft or DiffLineKind.PlusRight or DiffLineKind.MinusPlus or DiffLineKind.Grep) == true;
        Invalidate(fromLine: 1);
    }

    /// <summary>Tokenizes the lines from <paramref name="fromLine"/> again, e.g. after they were edited.</summary>
    public void Invalidate(int fromLine)
    {
        int keep = Math.Clamp(fromLine - 1, 0, _tokens.Count);
        _tokens.RemoveRange(keep, _tokens.Count - keep);
        _states.RemoveRange(keep, _states.Count - keep);
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        TextDocument document = CurrentContext.Document;
        if (document.LineCount > MaxLineCount || _isWordDiffOrGrep)
        {
            return;
        }

        Tokenize(document, line.LineNumber);
        if (_tokens[line.LineNumber - 1] is not { } tokens)
        {
            return;
        }

        int prefixLength = _diffLineKinds is null ? 0 : _diffPrefixLength;
        int textLength = line.Length - prefixLength;
        for (int i = 0; i < tokens.Length; i += 2)
        {
            int start = tokens[i];
            int end = i + 2 < tokens.Length ? tokens[i + 2] : textLength;
            int metadata = tokens[i + 1];
            int foreground = EncodedTokenAttributes.GetForeground(metadata);
            TextMateFontStyle fontStyle = EncodedTokenAttributes.GetFontStyle(metadata);
            if (end <= start || start >= textLength || (foreground <= DefaultForeground && fontStyle <= TextMateFontStyle.None))
            {
                continue;
            }

            IBrush? brush = foreground > DefaultForeground ? GetBrush(foreground) : null;
            ChangeLinePart(line.Offset + prefixLength + start, line.Offset + prefixLength + Math.Min(end, textLength), element =>
            {
                if (brush is not null)
                {
                    element.TextRunProperties.SetForegroundBrush(brush);
                }

                if (fontStyle > TextMateFontStyle.None)
                {
                    Typeface typeface = element.TextRunProperties.Typeface;
                    element.TextRunProperties.SetTypeface(new Typeface(
                        typeface.FontFamily,
                        (fontStyle & TextMateFontStyle.Italic) != 0 ? global::Avalonia.Media.FontStyle.Italic : typeface.Style,
                        (fontStyle & TextMateFontStyle.Bold) != 0 ? FontWeight.Bold : typeface.Weight));
                    if ((fontStyle & TextMateFontStyle.Underline) != 0)
                    {
                        element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                    }
                }
            });
        }
    }

    private static (RegistryOptions Options, Registry Registry) GetRegistry(ThemeName themeName)
    {
        if (!Registries.TryGetValue(themeName, out (RegistryOptions Options, Registry Registry) registry))
        {
            RegistryOptions options = new(themeName);
            registry = (options, new Registry(options));
            Registries[themeName] = registry;
        }

        return registry;
    }

    /// <summary>Tokenizes the lines up to <paramref name="lineNumber"/>, from the last one tokenized.</summary>
    private void Tokenize(TextDocument document, int lineNumber)
    {
        while (_tokens.Count < lineNumber)
        {
            DocumentLine line = document.GetLineByNumber(_tokens.Count + 1);
            (IStateStack? oldState, IStateStack? newState) = _states.Count > 0 ? _states[^1] : (null, null);
            int[]? tokens = null;
            switch (_diffLineKinds is null ? DiffLineKind.Plus : _diffLineKinds.GetValueOrDefault(line.LineNumber, DiffLineKind.Header))
            {
                case DiffLineKind.Plus:
                    (tokens, newState) = TokenizeLine(document, line, newState);
                    break;

                case DiffLineKind.Minus:
                    (tokens, oldState) = TokenizeLine(document, line, oldState);
                    break;

                case DiffLineKind.Context:
                    (tokens, newState) = TokenizeLine(document, line, newState);
                    (_, oldState) = TokenizeLine(document, line, oldState);
                    break;

                case DiffLineKind.FileHeader:
                    // Another file.
                    (oldState, newState) = (null, null);
                    break;
            }

            _tokens.Add(tokens);
            _states.Add((oldState, newState));
        }
    }

    private (int[]? Tokens, IStateStack? State) TokenizeLine(TextDocument document, DocumentLine line, IStateStack? state)
    {
        int prefixLength = _diffLineKinds is null ? 0 : _diffPrefixLength;
        if (line.Length - prefixLength > MaxLineLength)
        {
            return (null, state);
        }

        string text = document.GetText(line.Offset + Math.Min(prefixLength, line.Length), Math.Max(line.Length - prefixLength, 0));
        ITokenizeLineResult2 result = _grammar.TokenizeLine2(new LineText(text), state, LineTimeLimit);
        return (result.Tokens, result.RuleStack);
    }

    private IBrush? GetBrush(int colorId)
    {
        if (!_brushes.TryGetValue(colorId, out IBrush? brush))
        {
            brush = Color.TryParse(_theme.GetColor(colorId), out Color color) ? new SolidColorBrush(color) : null;
            _brushes[colorId] = brush;
        }

        return brush;
    }
}
