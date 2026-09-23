using CommunityToolkit.Mvvm.ComponentModel;
using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUI.Presentation.SpellChecker;

/// <summary>How <see cref="SpellCheckViewModel.GetCompletions"/> ends.</summary>
public enum AutoCompletionKind
{
    /// <summary>Nothing to complete: the list is closed.</summary>
    None,

    /// <summary>The words are still loaded (the message of <c>AutoCompleteToolTip</c> when called by the user).</summary>
    NotLoaded,

    /// <summary>The list of the matching words is shown.</summary>
    List,

    /// <summary>The only matching word replaces the typed one (when called by the user).</summary>
    Accept,
}

/// <summary>The words that complete the one typed before the cursor, from <see cref="Start"/>.</summary>
public sealed record AutoCompletion(AutoCompletionKind Kind, int Start = 0, IReadOnlyList<string>? Words = null)
{
    public static AutoCompletion None { get; } = new(AutoCompletionKind.None);
}

/// <summary>
///  Port of the spell checking and auto-completion of <c>EditNetSpell</c> (docs/avalonia-port/PLAN.md, phase 5): the
///  misspelled words and the ill-formed lines to mark, the items of the context menu and the words to complete.
/// </summary>
public sealed partial class SpellCheckViewModel : ObservableObject
{
    /// <summary>The value of the dictionary setting without a dictionary.</summary>
    public const string NoDictionary = "None";

    /// <summary>Longer texts are not checked (as <c>CheckSpelling</c>).</summary>
    public const int MaxCheckedLength = 5000;

    /// <summary>As <c>AddWordSuggestions</c>.</summary>
    public const int MaxSuggestions = 5;

    /// <summary>The message shown when the words are not loaded yet (not translated in <c>EditNetSpell</c> either).</summary>
    public const string AutoCompleteNotAvailableText = "AutoComplete is not available yet (it is still parsing the changed files).";

    private readonly ISpellCheckHost _host;
    private CancellationTokenSource _autoCompleteCancellation = new();
    private bool _isLoadingAutoCompleteWords;
    private IReadOnlyList<string>? _autoCompleteWords;

    public SpellCheckViewModel(SpellCheckStrings strings, ISpellCheckHost host)
    {
        Strings = strings;
        _host = host;
    }

    public SpellCheckStrings Strings { get; }

    /// <summary>The misspelled words, underlined with a wave.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TextSpan> Mistakes { get; private set; } = [];

    /// <summary>The text beyond the lengths of <see cref="GetIllFormedLines"/>, marked.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<TextSpan> IllFormedLines { get; private set; } = [];

    public string Dictionary => _host.Dictionary;

    public bool MarkIllFormedLines => _host.MarkIllFormedLines;

    public bool ProvideAutoCompletion => _host.ProvideAutoCompletion;

    /// <summary>The words of the auto-completion are loaded.</summary>
    public bool IsAutoCompleteLoaded => _autoCompleteWords is not null;

    /// <summary>The loading of the words of the auto-completion (e.g. for tests).</summary>
    public Task AutoCompleteWordsLoading { get; private set; } = Task.CompletedTask;

    public IReadOnlyList<string> GetDictionaries() => _host.GetDictionaries();

    /// <summary>
    ///  As <c>TextBoxTextChanged</c>: the marks are cleared until the text is checked again.
    /// </summary>
    /// <returns><see langword="true"/> if the text is to be checked (after a delay).</returns>
    public bool OnTextChanged(string text)
    {
        Mistakes = [];
        IllFormedLines = [];
        return _host.Dictionary != NoDictionary && text.Length >= 4;
    }

    /// <summary>As <c>CheckSpelling</c>.</summary>
    public void Check(string text)
    {
        Mistakes = text.Length < MaxCheckedLength ? _host.FindMisspelledWords(text) : [];
        IllFormedLines = _host.MarkIllFormedLines ? GetIllFormedLines(text) : [];
    }

    /// <summary>As <c>MarkLines</c>: the subject beyond 50 characters, the second line and the others beyond 72.</summary>
    public static IReadOnlyList<TextSpan> GetIllFormedLines(string text)
    {
        List<TextSpan> ranges = [];
        int lineStart = 0;
        for (int line = 0; lineStart <= text.Length; line++)
        {
            int lineEnd = text.IndexOf('\n', lineStart);
            int nextLineStart = lineEnd < 0 ? text.Length + 1 : lineEnd + 1;
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            if (lineEnd > lineStart && text[lineEnd - 1] == '\r')
            {
                lineEnd--;
            }

            int maxLength = line switch
            {
                0 => 50,
                1 => 0,
                _ => 72
            };

            int length = lineEnd - lineStart;
            if (length > maxLength)
            {
                ranges.Add(new TextSpan(lineStart + maxLength, length - maxLength));
            }

            lineStart = nextLineStart;
        }

        return ranges;
    }

    /// <summary>
    ///  As <c>AddWordSuggestions</c>: the suggestions for the misspelled word at the cursor (only with the
    ///  auto-completion, as in <c>EditNetSpell</c>).
    /// </summary>
    public SpellingSuggestions? GetSuggestions(string text, int textIndex)
        => _host.ProvideAutoCompletion ? _host.GetSuggestions(text, textIndex, MaxSuggestions) : null;

    public TextEdit? ReplaceWord(string text, int textIndex, string replacement) => _host.ReplaceWord(text, textIndex, replacement);

    public TextEdit? DeleteWord(string text, int textIndex) => _host.DeleteWord(text, textIndex);

    public void IgnoreWord(string text, int textIndex)
    {
        _host.IgnoreWord(text, textIndex);
        Check(text);
    }

    public void AddToDictionary(string text, int textIndex)
    {
        _host.AddToDictionary(text, textIndex);
        Check(text);
    }

    /// <summary>As <c>DicToolStripMenuItemClick</c>.</summary>
    public void SelectDictionary(string dictionary, string text)
    {
        _host.Dictionary = dictionary;
        Check(text);
    }

    /// <summary>As <c>MarkIllFormedLinesInCommitMsgClick</c>.</summary>
    public void ToggleMarkIllFormedLines(string text)
    {
        _host.MarkIllFormedLines = !_host.MarkIllFormedLines;
        Check(text);
    }

    /// <summary>As the item "Provide auto completion" and <c>ToggleAutoCompletion</c>.</summary>
    public void ToggleAutoCompletion()
    {
        _host.ProvideAutoCompletion = !_host.ProvideAutoCompletion;
        LoadAutoCompleteWords();
    }

    /// <summary>As <c>ToggleAutoCompletion</c> and <c>RefreshAutoCompleteWords</c>: the words are loaded again.</summary>
    public void LoadAutoCompleteWords()
    {
        CancelAutoComplete();
        _autoCompleteWords = null;
        _isLoadingAutoCompleteWords = false;
        if (!_host.ProvideAutoCompletion)
        {
            return;
        }

        _autoCompleteCancellation = new CancellationTokenSource();
        _isLoadingAutoCompleteWords = true;
        AutoCompleteWordsLoading = LoadAsync(_autoCompleteCancellation.Token);

        async Task LoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<string> words = await _host.GetAutoCompleteWordsAsync(cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    _autoCompleteWords = words;
                }
            }
            catch (OperationCanceledException)
            {
                // Loaded again or closed.
            }
        }
    }

    /// <summary>As <c>CancelAutoComplete</c> (when the dialog closes).</summary>
    public void CancelAutoComplete() => _autoCompleteCancellation.Cancel();

    /// <summary>
    ///  As <c>UpdateOrShowAutoComplete</c>: the words that complete the one before <paramref name="caret"/>; one letter is
    ///  enough when <paramref name="calledByUser"/> (Ctrl+Space) or when the list was opened so.
    /// </summary>
    public AutoCompletion GetCompletions(string text, int caret, bool calledByUser, bool wasUserActivated)
    {
        if (!_host.ProvideAutoCompletion)
        {
            return AutoCompletion.None;
        }

        if (_autoCompleteWords is not { } autoCompleteWords)
        {
            return calledByUser && _isLoadingAutoCompleteWords ? new AutoCompletion(AutoCompletionKind.NotLoaded) : AutoCompletion.None;
        }

        string word = WordAtCursor.Extract(text, caret - 1);
        if (word.Length <= 1 && !calledByUser && !wasUserActivated)
        {
            return AutoCompletion.None;
        }

        List<string> words = [.. autoCompleteWords.Where(w => Matches(w, word))];
        if (words.Count == 0)
        {
            return AutoCompletion.None;
        }

        return new AutoCompletion(words.Count == 1 && calledByUser ? AutoCompletionKind.Accept : AutoCompletionKind.List, caret - word.Length, words);
    }

    /// <summary>As <c>AutoCompleteWord.Matches</c>: the start of the word or of its capitals ("camel humps").</summary>
    public static bool Matches(string word, string typedWord)
        => word.StartsWith(typedWord, StringComparison.OrdinalIgnoreCase)
            || string.Concat(word.Where(char.IsUpper)).StartsWith(typedWord, StringComparison.OrdinalIgnoreCase);
}
