using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUI.Presentation.SpellChecker;

/// <summary>A range of a text (e.g. a misspelled word).</summary>
public readonly record struct TextSpan(int Start, int Length)
{
    public int End => Start + Length;
}

/// <summary>A misspelled word and the suggestions to replace it.</summary>
public sealed record SpellingSuggestions(TextSpan Word, IReadOnlyList<string> Suggestions);

/// <summary>
///  The spell checker (NetSpell's <c>Spelling</c> and <c>WordDictionary</c>) and the settings of <c>EditNetSpell</c>.
/// </summary>
public interface ISpellCheckHost
{
    /// <summary>The dictionary of the repository (<c>Detached().Dictionary</c>), <see cref="SpellCheckViewModel.NoDictionary"/> for none.</summary>
    string Dictionary { get; set; }

    /// <summary>The names of the installed dictionaries (the <c>.dic</c> files of <c>GetDictionaryDir</c>).</summary>
    IReadOnlyList<string> GetDictionaries();

    /// <summary>As <c>AppSettings.MarkIllFormedLinesInCommitMsg</c>.</summary>
    bool MarkIllFormedLines { get; set; }

    /// <summary>As <c>AppSettings.ProvideAutocompletion</c>.</summary>
    bool ProvideAutoCompletion { get; set; }

    /// <summary>The misspelled words; nothing if the dictionary file does not exist.</summary>
    IReadOnlyList<TextSpan> FindMisspelledWords(string text);

    /// <summary>The suggestions for the word at <paramref name="textIndex"/>; <see langword="null"/> if it is spelled right.</summary>
    SpellingSuggestions? GetSuggestions(string text, int textIndex, int maxSuggestions);

    /// <summary>As <c>Spelling.ReplaceWord</c>: the edit replacing the word (matching the case of its first letter).</summary>
    TextEdit? ReplaceWord(string text, int textIndex, string replacement);

    /// <summary>As <c>Spelling.DeleteWord</c>: the edit removing the word (and a surrounding space).</summary>
    TextEdit? DeleteWord(string text, int textIndex);

    /// <summary>As <c>Spelling.IgnoreWord</c>: the word is not checked any more in this editor.</summary>
    void IgnoreWord(string text, int textIndex);

    /// <summary>As <c>WordDictionary.Add</c>: the word is added to the user dictionary.</summary>
    void AddToDictionary(string text, int textIndex);

    /// <summary>
    ///  The words of the auto-completion (<c>CommitAutoCompleteProvider</c>, <c>CommitMessageMetadataProvider</c>),
    ///  also spelled right from then on (<c>Spelling.AddAutoCompleteWords</c>).
    /// </summary>
    Task<IReadOnlyList<string>> GetAutoCompleteWordsAsync(CancellationToken cancellationToken);
}
