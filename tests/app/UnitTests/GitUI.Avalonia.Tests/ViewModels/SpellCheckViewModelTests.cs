using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.SpellChecker;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the spell checking and auto-completion of the message (port of <c>EditNetSpell</c>).</summary>
[TestFixture]
public sealed class SpellCheckViewModelTests
{
    [Test]
    public void A_change_clears_the_marks_until_the_text_is_checked()
    {
        FakeSpellCheckHost host = new();
        SpellCheckViewModel viewModel = new(new SpellCheckStrings(), host);

        viewModel.Check("Fix teh bug");
        viewModel.Mistakes.Should().Equal(new TextSpan(4, 3));
        viewModel.OnTextChanged("Fix teh bug!").Should().BeTrue();
        viewModel.Mistakes.Should().BeEmpty();

        viewModel.OnTextChanged("Fix").Should().BeFalse("short texts are not checked");
        host.Dictionary = SpellCheckViewModel.NoDictionary;
        viewModel.OnTextChanged("Fix teh bug").Should().BeFalse("without a dictionary");

        viewModel.Check(new string('a', SpellCheckViewModel.MaxCheckedLength) + " teh");
        viewModel.Mistakes.Should().BeEmpty("long texts are not checked");
    }

    [Test]
    public void The_subject_beyond_50_characters_the_second_line_and_the_body_beyond_72_are_ill_formed()
    {
        string subject = new('s', 55);
        string body = new('b', 80);
        string text = $"{subject}\r\nsecond\n{body}\nshort";

        SpellCheckViewModel.GetIllFormedLines(text).Should().Equal(
            new TextSpan(50, 5),
            new TextSpan(57, 6),
            new TextSpan(64 + 72, 8));

        FakeSpellCheckHost host = new();
        SpellCheckViewModel viewModel = new(new SpellCheckStrings(), host);
        viewModel.Check(text);
        viewModel.IllFormedLines.Should().HaveCount(3);
        viewModel.ToggleMarkIllFormedLines(text);
        host.MarkIllFormedLines.Should().BeFalse();
        viewModel.IllFormedLines.Should().BeEmpty();
    }

    [Test]
    public void The_menu_operations_go_to_the_spell_checker_and_check_again()
    {
        FakeSpellCheckHost host = new();
        SpellCheckViewModel viewModel = new(new SpellCheckStrings(), host);

        viewModel.GetSuggestions("Fix teh bug", 5)!.Suggestions.Should().Equal("the", "ten");
        viewModel.ReplaceWord("Fix teh bug", 5, "the").Should().Be(new TextEdit(4, 3, "the"));

        viewModel.IgnoreWord("Fix teh bug", 5);
        viewModel.Mistakes.Should().BeEmpty();
        host.Ignored.Should().Equal("teh");

        viewModel.SelectDictionary("de-DE", "Fix teh bug");
        host.Dictionary.Should().Be("de-DE");
        viewModel.GetDictionaries().Should().Equal("de-DE", "en-US");

        host.ProvideAutoCompletion = false;
        viewModel.GetSuggestions("Fix teh bug", 5).Should().BeNull("as in EditNetSpell, the suggestions come with the auto-completion");
    }

    [Test]
    public async Task The_typed_word_is_completed_from_its_start_or_its_capitals()
    {
        FakeSpellCheckHost host = new();
        SpellCheckViewModel viewModel = new(new SpellCheckStrings(), host);
        viewModel.GetCompletions("Fix Fi", 6, calledByUser: true, wasUserActivated: false).Kind.Should().Be(AutoCompletionKind.None, "the words are not loaded");

        host.Words = new TaskCompletionSource<IReadOnlyList<string>>();
        viewModel.LoadAutoCompleteWords();
        viewModel.GetCompletions("Fix Fi", 6, calledByUser: true, wasUserActivated: false).Kind.Should().Be(AutoCompletionKind.NotLoaded);
        viewModel.GetCompletions("Fix Fi", 6, calledByUser: false, wasUserActivated: false).Kind.Should().Be(AutoCompletionKind.None);
        host.Words.SetResult(["FileStatusList", "FileViewer", "Signed-off-by: "]);
        await viewModel.AutoCompleteWordsLoading;

        AutoCompletion completion = viewModel.GetCompletions("Fix Fi", 6, calledByUser: false, wasUserActivated: false);
        completion.Should().BeEquivalentTo(new AutoCompletion(AutoCompletionKind.List, 4, ["FileStatusList", "FileViewer"]));
        viewModel.GetCompletions("Fix FSL", 7, calledByUser: true, wasUserActivated: false)
            .Should().BeEquivalentTo(new AutoCompletion(AutoCompletionKind.Accept, 4, ["FileStatusList"]), "one word is accepted when called by the user");
        viewModel.GetCompletions("Fix F", 5, calledByUser: false, wasUserActivated: false).Kind.Should().Be(AutoCompletionKind.None, "one letter is not enough");
        viewModel.GetCompletions("Fix F", 5, calledByUser: false, wasUserActivated: true).Kind.Should().Be(AutoCompletionKind.List);
        viewModel.GetCompletions("Fix Xy", 6, calledByUser: true, wasUserActivated: false).Kind.Should().Be(AutoCompletionKind.None);

        viewModel.ToggleAutoCompletion();
        host.ProvideAutoCompletion.Should().BeFalse();
        viewModel.GetCompletions("Fix Fi", 6, calledByUser: true, wasUserActivated: false).Kind.Should().Be(AutoCompletionKind.None);
    }

    [TestCase("FileStatusList", "file", true)]
    [TestCase("FileStatusList", "FSL", true)]
    [TestCase("FileStatusList", "fs", true)]
    [TestCase("FileStatusList", "Status", false)]
    public void Matches_the_start_of_the_word_or_of_its_capitals(string word, string typed, bool expected)
    {
        SpellCheckViewModel.Matches(word, typed).Should().Be(expected);
    }

    /// <summary>A spell checker that knows "teh" as the only misspelled word.</summary>
    internal sealed class FakeSpellCheckHost : ISpellCheckHost
    {
        public string Dictionary { get; set; } = "en-US";

        public bool MarkIllFormedLines { get; set; } = true;

        public bool ProvideAutoCompletion { get; set; } = true;

        public List<string> Ignored { get; } = [];

        public List<string> AddedToDictionary { get; } = [];

        public TaskCompletionSource<IReadOnlyList<string>> Words { get; set; } = new();

        public IReadOnlyList<string> GetDictionaries() => ["de-DE", "en-US"];

        public IReadOnlyList<TextSpan> FindMisspelledWords(string text)
        {
            int index = text.IndexOf("teh", StringComparison.Ordinal);
            return index < 0 || Ignored.Contains("teh") || AddedToDictionary.Contains("teh") ? [] : [new TextSpan(index, 3)];
        }

        public SpellingSuggestions? GetSuggestions(string text, int textIndex, int maxSuggestions)
            => FindMisspelledWords(text).FirstOrDefault(s => s.Start <= textIndex && textIndex <= s.End) is { Length: > 0 } word
                ? new SpellingSuggestions(word, ["the", "ten"])
                : null;

        public TextEdit? ReplaceWord(string text, int textIndex, string replacement)
            => GetSuggestions(text, textIndex, 5) is { } suggestions ? new TextEdit(suggestions.Word.Start, suggestions.Word.Length, replacement) : null;

        public TextEdit? DeleteWord(string text, int textIndex)
            => GetSuggestions(text, textIndex, 5) is { } suggestions ? new TextEdit(suggestions.Word.Start - 1, suggestions.Word.Length + 1, "") : null;

        public void IgnoreWord(string text, int textIndex) => Ignored.Add(text.Substring(GetSuggestions(text, textIndex, 5)!.Word.Start, 3));

        public void AddToDictionary(string text, int textIndex) => AddedToDictionary.Add(text.Substring(GetSuggestions(text, textIndex, 5)!.Word.Start, 3));

#pragma warning disable VSTHRD003 // The test completes the words.
        public Task<IReadOnlyList<string>> GetAutoCompleteWordsAsync(CancellationToken cancellationToken) => Words.Task;
#pragma warning restore VSTHRD003
    }
}
