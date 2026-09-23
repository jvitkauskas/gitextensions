namespace GitUI.Presentation.SpellChecker;

/// <summary>
///  Port of <c>WordAtCursorExtractor</c> and <c>SpellCheckerHelper.IsSeparator</c>: the word before the cursor (for the
///  auto-completion) and the bounds of a word (for the double click); a leading dot belongs to a word.
/// </summary>
public static class WordAtCursor
{
    /// <summary>As <c>IsSeparator</c>: not a letter, a digit or one of <c>_+-</c>.</summary>
    public static bool IsSeparator(char c) => !"_+-".Contains(c) && !char.IsLetterOrDigit(c);

    /// <summary>The word that ends at <paramref name="index"/> (the character before the cursor).</summary>
    public static string Extract(string text, int index)
    {
        int start = FindStartOfWord(text, index);
        return start < 0 ? string.Empty : text.Substring(start, index - start + 1);
    }

    public static (int Start, int Length) GetWordBounds(string text, int index)
    {
        int start = Math.Min(FindStartOfWord(text, index), index);
        if (start < 0)
        {
            return (0, 0);
        }

        int end = FindEndOfWord(text, index);
        return (start, Math.Max(1, end - start));
    }

    public static int FindStartOfWord(string text, int index)
    {
        index = Math.Min(index, text.Length - 1);
        if (index < 0)
        {
            return -1;
        }

        while (index >= 0 && BelongsToStartOfWord(text, index))
        {
            --index;
        }

        return index + 1;
    }

    public static int FindEndOfWord(string text, int index)
    {
        index = Math.Min(index, text.Length);
        if (index < 0)
        {
            return -1;
        }

        while (index < text.Length && !IsSeparator(text[index]))
        {
            ++index;
        }

        return index;
    }

    private static bool BelongsToStartOfWord(string text, int index)
        => text[index] == '.' ? IsLeadingChar(text, index) : !IsSeparator(text[index]);

    private static bool IsLeadingChar(string text, int index)
        => index == 0 || (text[index - 1] != ')' && text[index - 1] != ']' && IsSeparator(text[index - 1]));
}
