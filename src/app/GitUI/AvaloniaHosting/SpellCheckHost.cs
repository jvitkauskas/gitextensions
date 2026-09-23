using System.Diagnostics;
using GitCommands;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitUI.AutoCompletion;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.SpellChecker;
using Microsoft.VisualStudio.Threading;
using NetSpell.SpellChecker;
using NetSpell.SpellChecker.Dictionary;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The NetSpell spell checker and the settings of <c>EditNetSpell</c> for the Avalonia message editor
///  (docs/avalonia-port/PLAN.md, phase 5); keep it in sync.
/// </summary>
internal sealed class SpellCheckHost : ISpellCheckHost
{
    /// <summary>Shared by the editors, as the static dictionary of <c>EditNetSpell</c>.</summary>
    private static WordDictionary? _wordDictionary;

    private readonly IGitUICommands _commands;
    private readonly string _dictionaryDirectory;
    private readonly IReadOnlyList<IAutoCompleteProvider> _autoCompleteProviders;
    private readonly Spelling _spelling = new()
    {
        ShowDialog = false,
        IgnoreAllCapsWords = true,
        IgnoreWordsWithDigits = true
    };

    /// <param name="dictionaryDirectory">The folder of the dictionaries, by default <c>AppSettings.GetDictionaryDir</c>.</param>
    public SpellCheckHost(IGitUICommands commands, string? dictionaryDirectory = null)
    {
        _commands = commands;
        _dictionaryDirectory = dictionaryDirectory ?? AppSettings.GetDictionaryDir();
        _autoCompleteProviders = [new CommitAutoCompleteProvider(() => commands.Module), new CommitMessageMetadataProvider()];
        LoadDictionary();
    }

    private IGitModule Module => _commands.Module;

    private DistributedSettings Settings => Module.GetEffectiveSettings() as DistributedSettings ?? AppSettings.SettingsContainer;

    public string Dictionary
    {
        get => Settings.Detached().Dictionary;
        set
        {
            // As DicToolStripMenuItemClick: with a repository, the dictionary is set for it only.
            DistributedSettings settings = Module.GetLocalSettings() as DistributedSettings ?? Settings;
            settings.Detached().Dictionary = value;
            LoadDictionary();
        }
    }

    public bool MarkIllFormedLines { get => AppSettings.MarkIllFormedLinesInCommitMsg; set => AppSettings.MarkIllFormedLinesInCommitMsg = value; }

    public bool ProvideAutoCompletion { get => AppSettings.ProvideAutocompletion; set => AppSettings.ProvideAutocompletion = value; }

    public IReadOnlyList<string> GetDictionaries()
    {
        try
        {
            return [.. Directory.GetFiles(_dictionaryDirectory, "*.dic", SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f).Name.Replace(".dic", ""))];
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return [];
        }
    }

    public IReadOnlyList<TextSpan> FindMisspelledWords(string text)
    {
        List<TextSpan> mistakes = [];
        if (!File.Exists(_spelling.Dictionary.DictionaryFile))
        {
            return mistakes;
        }

        EventHandler<SpellingEventArgs> onMisspelled = (_, e) => mistakes.Add(new TextSpan(e.TextIndex, e.Word.Length));
        _spelling.MisspelledWord += onMisspelled;
        try
        {
            _spelling.Text = text;
            _spelling.SpellCheck();
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
        }
        finally
        {
            _spelling.MisspelledWord -= onMisspelled;
        }

        return mistakes;
    }

    public SpellingSuggestions? GetSuggestions(string text, int textIndex, int maxSuggestions)
    {
        try
        {
            if (!File.Exists(_spelling.Dictionary.DictionaryFile) || !SelectWord(text, textIndex) || _spelling.TestWord())
            {
                return null;
            }

            _spelling.MaxSuggestions = maxSuggestions;
            _spelling.Suggest();
            return new SpellingSuggestions(new TextSpan(_spelling.TextIndex, _spelling.CurrentWord.Length), [.. _spelling.Suggestions]);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return null;
        }
    }

    public TextEdit? ReplaceWord(string text, int textIndex, string replacement)
    {
        if (!SelectWord(text, textIndex))
        {
            return null;
        }

        TextEdit? edit = null;
        EventHandler<ReplaceWordEventArgs> onReplaced = (_, e) => edit = new TextEdit(e.TextIndex, e.Word.Length, e.ReplacementWord);
        _spelling.ReplacedWord += onReplaced;
        try
        {
            _spelling.ReplaceWord(replacement);
        }
        finally
        {
            _spelling.ReplacedWord -= onReplaced;
        }

        return edit;
    }

    public TextEdit? DeleteWord(string text, int textIndex)
    {
        if (!SelectWord(text, textIndex))
        {
            return null;
        }

        TextEdit? edit = null;
        EventHandler<SpellingEventArgs> onDeleted = (_, e) => edit = new TextEdit(e.TextIndex, e.Word.Length, "");
        _spelling.DeletedWord += onDeleted;
        try
        {
            _spelling.DeleteWord();
        }
        finally
        {
            _spelling.DeletedWord -= onDeleted;
        }

        return edit;
    }

    public void IgnoreWord(string text, int textIndex)
    {
        // Spelling.IgnoreWord (of EditNetSpell) only skips the word until the next check; the word is ignored in the editor.
        if (SelectWord(text, textIndex))
        {
            _spelling.IgnoreAllWord();
        }
    }

    public void AddToDictionary(string text, int textIndex)
    {
        if (SelectWord(text, textIndex))
        {
            _spelling.Dictionary.Add(_spelling.CurrentWord);
        }
    }

    /// <summary>As <c>InitializeAutoCompleteWordsTask</c> and <c>ToggleAutoCompletion</c>.</summary>
    public async Task<IReadOnlyList<string>> GetAutoCompleteWordsAsync(CancellationToken cancellationToken)
    {
        await TaskScheduler.Default.SwitchTo(alwaysYield: true);
        IEnumerable<AutoCompleteWord>[] results = await Task.WhenAll(_autoCompleteProviders.Select(p => p.GetAutoCompleteWordsAsync(cancellationToken)));
        IReadOnlyList<string> words = [.. results.SelectMany(result => result).Distinct().Select(w => w.Word)];

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        _spelling.AddAutoCompleteWords(words);
        return words;
    }

    /// <summary>As <c>LoadDictionary</c>.</summary>
    private void LoadDictionary()
    {
        string dictionaryFile = string.Concat(Path.Join(_dictionaryDirectory, Settings.Detached().Dictionary), ".dic");
        if (_wordDictionary is null || _wordDictionary.DictionaryFile != dictionaryFile)
        {
            _wordDictionary = new WordDictionary { DictionaryFile = dictionaryFile };
        }

        _spelling.Dictionary = _wordDictionary;
    }

    /// <summary>The word at the index is the current word of <see cref="Spelling"/>.</summary>
    private bool SelectWord(string text, int textIndex)
    {
        _spelling.Text = text;
        _spelling.WordIndex = _spelling.GetWordIndexFromTextIndex(textIndex);
        return _spelling.CurrentWord.Length > 0;
    }
}
