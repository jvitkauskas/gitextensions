using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.CommitDialog;
using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUITests.CommandsDialogs;

/// <summary>
///  Compares the Avalonia port of the word wrapping of the commit message (<see cref="CommitMessageFormatter"/>) with
///  <see cref="WordWrapper"/> (and the hotkey commands with <c>FormCommit.Command</c>), so that an upstream change fails here until it is ported (docs/avalonia-port/ledger.md).
/// </summary>
[TestFixture]
public sealed class AvaloniaCommitPortParityTests
{
    [TestCase("", 10)]
    [TestCase("short", 10)]
    [TestCase("one two three four five six seven", 10)]
    [TestCase("averyveryverylongword and more", 8)]
    [TestCase("  leading and  double  spaces ", 12)]
    [TestCase("tab\tseparated words in a line", 9)]
    [TestCase("exactly ten", 11)]
    public void Wrapping_matches_the_winforms_word_wrapper(string line, int limit)
    {
        CommitMessageFormatter.WrapSingleLine(line, limit).Should().Be(WordWrapper.WrapSingleLine(line, limit));
    }

    [Test]
    public void The_hotkey_commands_match_the_commands_of_the_winforms_form()
    {
        Enum.GetValues<CommitHotkeyCommand>().ToDictionary(c => c.ToString(), c => (int)c)
            .Should().Equal(Enum.GetValues<FormCommit.Command>().ToDictionary(c => c.ToString(), c => (int)c));
    }
}
