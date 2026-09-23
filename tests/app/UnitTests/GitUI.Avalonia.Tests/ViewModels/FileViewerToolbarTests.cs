using GitCommands.Settings;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>Tests of the options of the file viewer (port of the toolbar of the WinForms <c>FileViewer</c>).</summary>
[TestFixture]
public sealed class FileViewerToolbarTests
{
    private const string TwoHunks = "diff --git a/f b/f\n--- a/f\n+++ b/f\n@@ -1,3 +1,3 @@\n a\n-b\n+c\n d\n@@ -10,3 +10,4 @@\n x\n+y\n+z\n w\n";

    private static FileStatusEntry Entry => new(First, Second, CreateStatuses()[0]);

    [Test]
    public async Task The_options_are_saved_and_show_the_file_again()
    {
        DiffViewModelTests.FakeViewerHost host = new();
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);
        viewer.IsDiff.Should().BeTrue();

        viewer.IncreaseContextLinesCommand.Execute(null);
        host.Settings.NumberOfContextLines.Should().Be(4);
        viewer.DecreaseContextLinesCommand.Execute(null);
        viewer.DecreaseContextLinesCommand.Execute(null);
        viewer.DecreaseContextLinesCommand.Execute(null);
        viewer.DecreaseContextLinesCommand.Execute(null);
        host.Settings.NumberOfContextLines.Should().Be(0, "not below zero");
        viewer.ToggleShowEntireFileCommand.Execute(null);
        host.Settings.ShowEntireFile.Should().BeTrue();

        host.Requested.Should().HaveCount(7, "each change of the diff options shows the file again");

        viewer.ToggleNonPrintingCharsCommand.Execute(null);
        viewer.Editor.ShowWhitespace.Should().BeTrue();
        host.Settings.ShowNonPrintingChars.Should().BeTrue();
        host.Requested.Should().HaveCount(7, "the non-printing characters need no reload");
    }

    [Test]
    public void Whitespace_options_toggle_and_check_the_lesser_ones()
    {
        DiffViewModelTests.FakeViewerHost host = new();
        FileViewerViewModel viewer = new(host);

        viewer.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.Change);
        host.Settings.IgnoreWhitespace.Should().Be(IgnoreWhitespaceKind.Change);
        (viewer.IgnoresWhitespaceAtEol, viewer.IgnoresWhitespaceChanges, viewer.IgnoresAllWhitespace).Should().Be((true, true, false));

        viewer.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.AllSpace);
        (viewer.IgnoresWhitespaceAtEol, viewer.IgnoresWhitespaceChanges, viewer.IgnoresAllWhitespace).Should().Be((true, true, true));

        viewer.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.AllSpace);
        host.Settings.IgnoreWhitespace.Should().Be(IgnoreWhitespaceKind.None, "the checked kind switches off");
    }

    [Test]
    public async Task An_encoding_is_used_until_the_files_encoding_is_chosen_again()
    {
        DiffViewModelTests.FakeViewerHost host = new();
        FileViewerViewModel viewer = new(host);
        viewer.SelectedEncoding.Should().Be("UTF-8");
        await viewer.ShowChangesAsync(Entry);

        viewer.SelectedEncoding = "Western European (Windows)";
        viewer.SelectedEncoding = "UTF-8";

        host.Requested.Should().Equal("src/Program.cs", "src/Program.cs (Western European (Windows))", "src/Program.cs");
        viewer.OpenSettingsCommand.Execute(null);
        host.SettingsOpened.Should().Be(1);
    }

    [Test]
    public async Task The_next_and_previous_changes_are_the_first_lines_of_the_changed_blocks()
    {
        FileViewerViewModel viewer = new(new DiffViewModelTests.FakeViewerHost { Diff = TwoHunks });
        await viewer.ShowChangesAsync(Entry);

        // The lines: 5 " a", 6 "-b", 7 "+c", 8 " d", 9 "@@", 10 " x", 11 "+y", 12 "+z", 13 " w".
        viewer.GetChangeLine(1, backwards: false).Should().Be(6);
        viewer.GetChangeLine(6, backwards: false).Should().Be(11);
        viewer.GetChangeLine(11, backwards: false).Should().BeNull();
        viewer.GetChangeLine(13, backwards: true).Should().Be(11);
        viewer.GetChangeLine(11, backwards: true).Should().Be(6);

        viewer.Show(new FileViewContent(FileViewKind.Text, "text"));
        viewer.IsDiff.Should().BeFalse();
        viewer.GetChangeLine(1, backwards: false).Should().BeNull();
    }
}
