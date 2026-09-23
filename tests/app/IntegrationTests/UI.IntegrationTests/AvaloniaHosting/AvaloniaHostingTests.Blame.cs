using GitUI.Avalonia.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 5: the blame dialog (the <c>blame</c> verb), with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Blame_shows_the_authors_of_the_lines()
    {
        _referenceRepository.CreateCommit("First", "one\ntwo\n", "A.txt");
        _referenceRepository.CreateCommit("Second", "one\nchanged\n", "A.txt");

        string? title = null;
        string? text = null;
        List<string?> authorLines = [];
        int caretLine = 0;
        DriveNextDialog(window => WaitUntil(
            () => window.DataContext is BlameDialogViewModel { Blame.Blame: not null },
            () =>
            {
                BlameDialogViewModel viewModel = (BlameDialogViewModel)window.DataContext!;
                title = window.Title;
                text = viewModel.Blame.File.Text;
                authorLines.AddRange(viewModel.Blame.AuthorLines);
                caretLine = ((BlameWindow)window).Blame.Editor.TextArea.Caret.Line;
                Capture(window, "blame");
                window.Close();
            }));

        _commands.GetTestAccessor().RunCommandBasedOnArgument(["ge.exe", "blame", "A.txt", "2"]).Should().BeTrue();

        title.Should().Be("Blame (A.txt)");
        text.Should().Be($"one{Environment.NewLine}changed{Environment.NewLine}");
        authorLines.Should().HaveCount(2).And.OnlyContain(line => line != null, "each line has its own commit");
        caretLine.Should().Be(2);
    }
}
