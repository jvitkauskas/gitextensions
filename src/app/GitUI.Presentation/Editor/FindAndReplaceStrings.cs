using GitUI.Presentation.Translations;

namespace GitUI.Presentation.Editor;

/// <summary>
///  Strings of the search panel of the text editors (the AvaloniaEdit <c>SearchPanel</c>, which replaces
///  <c>FindAndReplaceForm</c>); ids match <c>FindAndReplaceForm</c>.
/// </summary>
public sealed class FindAndReplaceStrings : ViewStrings
{
    public FindAndReplaceStrings()
        : base("FindAndReplaceForm")
    {
        Find = Add("_findString", "Text", "Find");
        FindAndReplace = Add("_findAndReplaceString", "Text", "Find & replace");
        TextNotFound = Add("_textNotFoundString", "Text", "Text not found");
        FindNext = Add("btnFindNext", "Text", "&Find next");
        FindPrevious = Add("btnFindPrevious", "Text", "Find pre&vious");
        Replace = Add("btnReplace", "Text", "&Replace");
        ReplaceAll = Add("btnReplaceAll", "Text", "Replace &All");
        MatchCase = Add("chkMatchCase", "Text", "Match &case");
        MatchWholeWord = Add("chkMatchWholeWord", "Text", "Match &whole word");
    }

    /// <summary>The title in find mode (a plain text).</summary>
    public TranslatedText Find { get; }

    /// <summary>The title in replace mode (a plain text).</summary>
    public TranslatedText FindAndReplace { get; }

    /// <summary>The message when F3 finds nothing (a plain text).</summary>
    public TranslatedText TextNotFound { get; }

    public TranslatedText FindNext { get; }

    public TranslatedText FindPrevious { get; }

    public TranslatedText Replace { get; }

    public TranslatedText ReplaceAll { get; }

    public TranslatedText MatchCase { get; }

    public TranslatedText MatchWholeWord { get; }

    /// <summary>
    ///  The texts of the search panel by the name of their AvaloniaEdit resource (<c>AvaloniaEdit.SR</c>), with the keys
    ///  AvaloniaEdit shows beside them; the other resources (the regular expression option, the match counts) stay English.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetSearchPanelTexts() => new Dictionary<string, string>
    {
        ["SearchLabel"] = Find.Text + "...",
        ["ReplaceLabel"] = Replace.PlainText + "...",
        ["SearchFindNextText"] = FindNext.PlainText + " (F3)",
        ["SearchFindPreviousText"] = FindPrevious.PlainText + " (Shift+F3)",
        ["SearchMatchCaseText"] = MatchCase.PlainText,
        ["SearchMatchWholeWordsText"] = MatchWholeWord.PlainText,
        ["SearchReplaceNext"] = Replace.PlainText + " (Alt+R)",
        ["SearchReplaceAll"] = ReplaceAll.PlainText + " (Alt+A)",
        ["SearchToggleReplace"] = FindAndReplace.Text,
        ["SearchNoMatchesFoundText"] = TextNotFound.Text,
    };
}

/// <summary>The search logic of <c>FindAndReplaceForm</c> that does not depend on the editor.</summary>
public static class TextSearch
{
    /// <summary>
    ///  As <c>TextUtilities.FindWordStart</c> and <c>FindWordEnd</c> (<c>FindAndReplaceForm.ShowFor</c>): the word (letters,
    ///  digits and underscores) around <paramref name="offset"/>, empty if there is none.
    /// </summary>
    public static string GetWordAt(string text, int offset)
    {
        offset = Math.Clamp(offset, 0, text.Length);
        int start = offset;
        while (start > 0 && IsWordChar(text[start - 1]))
        {
            start--;
        }

        int end = offset;
        while (end < text.Length && IsWordChar(text[end]))
        {
            end++;
        }

        return text[start..end];

        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
    }
}
