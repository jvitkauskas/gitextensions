using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>Tests of the formatting of the commit message (port of <c>FormCommit.FormatAllText</c>).</summary>
[TestFixture]
public sealed class CommitMessageFormatterTests
{
    private static readonly string NL = Environment.NewLine;

    [Test]
    public void Text_on_the_second_line_moves_to_the_third()
    {
        CommitMessageFormatOptions options = new(SecondLineMustBeEmpty: true);

        CommitMessageFormatter.Apply("Subject\nx", CommitMessageFormatter.GetEdits("Subject\nx", options)).Should().Be($"Subject\n{NL}x");
        CommitMessageFormatter.GetEdits("Subject\n\nbody", options).Should().BeEmpty();
        CommitMessageFormatter.GetEdits("Subject", options).Should().BeEmpty();
        CommitMessageFormatter.Apply("Subject\r\nx", CommitMessageFormatter.GetEdits("Subject\r\nx", options with { IndentAfterFirstLine = true }))
            .Should().Be($"Subject\r\n{NL} - x");
    }

    [Test]
    public void Long_lines_of_the_body_are_wrapped_if_set_so()
    {
        string text = "Subject that is long enough\n\none two three four five";
        CommitMessageFormatOptions options = new(MaxLineLength: 10, SecondLineMustBeEmpty: true, AutoWrap: true);

        CommitMessageFormatter.Apply(text, CommitMessageFormatter.GetEdits(text, options)).Should().Be($"Subject that is long enough\n\none two{NL}three{NL}four five");
        CommitMessageFormatter.GetEdits(text, options with { AutoWrap = false }).Should().BeEmpty();
    }

    [Test]
    public void The_body_can_be_wrapped_on_demand()
    {
        string text = "A subject longer than ten\none two three";

        CommitMessageFormatter.Apply(text, CommitMessageFormatter.GetBodyWrapEdits(text, 10)).Should().Be($"A subject longer than ten\none two{NL}three");
        CommitMessageFormatter.GetBodyWrapEdits("Subject\nshort", 0).Should().BeEmpty("the default limit is 72");
    }
}
