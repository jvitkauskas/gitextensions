using System.ComponentModel.Design;
using GitCommands;
using GitCommands.Git;
using GitUI;
using GitUI.AutoCompletion;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.SpellChecker;
using GitUI.SpellChecker;

namespace GitUITests.SpellChecker;

/// <summary>
///  Compares the Avalonia port of <c>EditNetSpell</c> (<see cref="WordAtCursor"/>, <see cref="SpellCheckViewModel.Matches"/>)
///  with <see cref="WordAtCursorExtractor"/> and <see cref="AutoCompleteWord"/>, so that an upstream change fails here until
///  it is ported (docs/avalonia-port/ledger.md); and tests <see cref="SpellCheckHost"/> with the English dictionary.
/// </summary>
[TestFixture]
public sealed class AvaloniaSpellCheckPortTests
{
    private static readonly string[] _texts = ["", "word", "a.b c", "  .NET core", "(x).y", "foo[0].bar", "Fix FileStatusList", "one-two_three+four", "tab\tword"];

    [Test]
    public void The_words_at_the_cursor_match_the_winforms_extractor()
    {
        WordAtCursorExtractor extractor = new();
        foreach (string text in _texts)
        {
            for (int index = -1; index <= text.Length; index++)
            {
                WordAtCursor.GetWordBounds(text, index).Should().Be(extractor.GetWordBounds(text, index), $"the bounds in \"{text}\" at {index}");
                if (index < text.Length)
                {
                    WordAtCursor.Extract(text, index).Should().Be(extractor.Extract(text, index), $"the word in \"{text}\" at {index}");
                }
            }
        }
    }

    [TestCase("FileStatusList", "file")]
    [TestCase("FileStatusList", "FSL")]
    [TestCase("FileStatusList", "fsl")]
    [TestCase("FileStatusList", "Status")]
    [TestCase("Signed-off-by: ", "sig")]
    [TestCase("x", "")]
    public void Matching_matches_the_winforms_auto_complete_word(string word, string typed)
    {
        SpellCheckViewModel.Matches(word, typed).Should().Be(new AutoCompleteWord(word).Matches(typed));
    }

    [Test]
    public void The_host_checks_with_netspell_and_edits_as_it()
    {
        SpellCheckHost host = CreateHost();
        host.GetDictionaries().Should().Contain("en-US");

        const string text = "Teh fix of teh bug";
        host.FindMisspelledWords(text).Should().Equal(new TextSpan(0, 3), new TextSpan(11, 3));
        host.FindMisspelledWords("Fix the bug").Should().BeEmpty();

        SpellingSuggestions suggestions = host.GetSuggestions(text, 12, maxSuggestions: 5)!;
        suggestions.Word.Should().Be(new TextSpan(11, 3));
        suggestions.Suggestions.Should().Contain("tech").And.HaveCountLessThanOrEqualTo(5);
        host.GetSuggestions(text, 5, maxSuggestions: 5).Should().BeNull("\"fix\" is spelled right");

        host.ReplaceWord(text, 1, "the").Should().Be(new TextEdit(0, 3, "The"), "the case of the first letter is kept");
        host.DeleteWord(text, 12).Should().Be(new TextEdit(11, 4, ""), "with the space after it");

        host.IgnoreWord(text, 12);
        host.FindMisspelledWords(text).Should().Equal([new TextSpan(0, 3)], "the ignored words are case sensitive");
    }

    private static SpellCheckHost CreateHost()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null && !File.Exists(Path.Join(directory, "GitExtensions.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNull("the tests run in the repository");
        GitUICommands commands = new(new ServiceContainer(), new GitModule(new GitExecutorProvider(new GitDirectoryResolver()), ""));
        SpellCheckHost host = new(commands, Path.Join(directory, "setup", "assets", "Dictionaries"));
        host.Dictionary.Should().Be("en-US");
        return host;
    }
}
