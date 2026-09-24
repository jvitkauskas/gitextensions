using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>
///  Tests of the appearances of the file viewer (the git word diff, difftastic, combined diffs, range diffs, grep results, fixed
///  diffs and the syntax highlighting of the WinForms <c>FileViewer</c>) and of their options.
/// </summary>
[TestFixture]
public sealed class DiffAppearancesTests
{
    private static FileStatusEntry Entry => FileViewerContextMenuTests.Entry(StagedStatus.WorkTree);

    private static FileViewContent Diff(string text, DiffViewMode mode = DiffViewMode.Diff, string? fileName = "calc.cs", int difftasticWidth = 80)
        => new(FileViewKind.Diff, text, fileName, HasGitColors: true, DiffMode: mode, CanOpenWithDifftool: mode is DiffViewMode.Diff or DiffViewMode.CombinedDiff, DifftasticWidth: difftasticWidth);

    [Test]
    public async Task The_git_word_diff_tells_the_lines_from_the_colors()
    {
        DiffViewModelTests.FakeViewerHost host = new() { DiffAppearance = DiffDisplayAppearance.GitWordDiff, Content = Diff(GitOutputFixtures.WordDiff) };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);

        TextEditorViewModel editor = viewer.Editor;
        editor.Text.Should().NotContain("\u001b");
        string[] lines = editor.Text.Split('\n');
        DiffLine Line(string text) => editor.DiffLines!.Single(l => lines[l.LineNumInDiff - 1].Contains(text, StringComparison.Ordinal));

        Line("// Multiplies").Should().Be(new DiffLine(9, 5, DiffLine.NotApplicable, DiffLineKind.MinusLeft));
        Line("int result").Should().Be(new DiffLine(12, 8, 7, DiffLineKind.MinusPlus));
        Line("Offset = 1").Should().Be(new DiffLine(16, 12, 11, DiffLineKind.MinusPlus), "the removed and added words are on one line");
        Line("Twice").Should().Be(new DiffLine(18, DiffLine.NotApplicable, 13, DiffLineKind.PlusRight));
        Line("return result").Kind.Should().Be(DiffLineKind.Context);
        editor.InlineDiffMarkers.Should().BeEmpty("git colors the words");
        editor.GitColoring!.Segments.Should().NotBeEmpty();

        // As SetVisibilityDiffContextMenu: no patch to copy, the appearance can be changed.
        viewer.GetMenuState().CanCopyPatch.Should().BeFalse();
        viewer.IsDiffAppearanceVisible.Should().BeTrue();

        // As DiffHighlightService.IsSearchMatch: the changed lines are next changes (not the only removed or added ones).
        viewer.GetChangeLine(1, backwards: false).Should().Be(12);
    }

    [Test]
    public async Task The_same_colored_text_is_a_patch_in_the_patch_appearance()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = Diff(GitOutputFixtures.Patch) };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);

        viewer.Editor.DiffLines!.Select(l => l.Kind).Should().NotContain([DiffLineKind.MinusPlus, DiffLineKind.MinusLeft, DiffLineKind.PlusRight]);
        viewer.Editor.InlineDiffMarkers.Should().NotBeEmpty();
        viewer.GetMenuState().CanCopyPatch.Should().BeTrue();
    }

    [Test]
    public async Task Changing_the_appearance_saves_it_and_shows_the_diff_again()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = Diff(GitOutputFixtures.Patch) };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);

        viewer.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.GitWordDiff);
        host.DiffAppearance.Should().Be(DiffDisplayAppearance.GitWordDiff);
        viewer.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.GitWordDiff);
        host.DiffAppearance.Should().Be(DiffDisplayAppearance.Patch, "the chosen appearance toggles");
        viewer.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.Difftastic);
        host.DiffAppearance.Should().Be(DiffDisplayAppearance.Difftastic);
        viewer.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.Patch);
        viewer.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.Patch);
        host.DiffAppearance.Should().Be(DiffDisplayAppearance.Patch, "the patch does not toggle");
        host.Requested.Should().HaveCount(6);
    }

    [Test]
    public async Task Difftastic_shows_its_line_numbers_in_the_margin_and_its_column()
    {
        DiffViewModelTests.FakeViewerHost host = new() { DiffAppearance = DiffDisplayAppearance.Difftastic, Content = Diff(GitOutputFixtures.Difftastic, DiffViewMode.Difftastic, difftasticWidth: 200) };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);

        TextEditorViewModel editor = viewer.Editor;
        editor.DiffMode.Should().Be(DiffViewMode.Difftastic);
        editor.Text.Split('\n')[2].Should().StartWith("        private readonly AsyncLoader _async;", "the line numbers are removed from the text");
        editor.DiffLines![2].Should().Be(new DiffLine(3, 46, 47, DiffLineKind.Context));
        editor.DiffLines.Should().Contain(l => l.Kind == DiffLineKind.PlusRight && l.RightLineNumber == 49 && l.LeftLineNumber == DiffLine.NotApplicable);
        editor.DiffLines.Should().Contain(l => l.Kind == DiffLineKind.MinusLeft && l.LeftLineNumber == 51);
        editor.DiffLines![0].Kind.Should().Be(DiffLineKind.Header);
        editor.VerticalRulerColumn.Should().Be(97, "the right side starts there");
        editor.InlineDiffMarkers.Should().BeEmpty();

        // As SetVisibilityDiffContextMenu: difftastic only strips the carriage returns (DFT_STRIP_CR).
        (viewer.CanChangeContextLines, viewer.CanIgnoreWhitespaceAtEol, viewer.CanIgnoreWhitespaceChanges, viewer.IsDiffAppearanceVisible).Should().Be((true, true, false, true));
        viewer.GetMenuState().CanCopyPatch.Should().BeFalse();

        // Copied as it is, without removing prefixes.
        viewer.Copy(" a\n+b", 5);
        host.Copied.Should().Equal((" a\n+b", true));
        viewer.GetChangeLine(1, backwards: false).Should().Be(5);
    }

    [Test]
    public async Task Grep_results_have_the_line_numbers_of_the_file()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = Diff(GitOutputFixtures.Grep, DiffViewMode.Grep) };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);

        TextEditorViewModel editor = viewer.Editor;
        editor.ShowLeftLineNumbers.Should().BeFalse();
        editor.Text.Should().StartWith("public sealed class Calculator\n{\n    public int Compute(int value)\n");
        editor.DiffLines!.Take(3).Should().Equal(
            new DiffLine(1, DiffLine.NotApplicable, 3, DiffLineKind.Header),
            new DiffLine(2, DiffLine.NotApplicable, 4, DiffLineKind.Context),
            new DiffLine(3, DiffLine.NotApplicable, 5, DiffLineKind.Grep));
        editor.DiffLines.Should().Contain(new DiffLine(7, DiffLine.NotApplicable, DiffLine.NotApplicable, DiffLineKind.Header), "the separator of the context");
        editor.GitColoring!.Segments.Should().Contain(s => editor.Text.Substring(s.Offset, s.Length) == "value");

        (viewer.CanChangeContextLines, viewer.CanIgnoreWhitespaceAtEol, viewer.IsDiffAppearanceVisible).Should().Be((true, false, false));
        viewer.GetChangeLine(3, backwards: false).Should().Be(5);
        viewer.GetChangeLine(5, backwards: true).Should().Be(3);
    }

    [Test]
    public async Task A_range_diff_goes_to_the_commit_headers()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = Diff(GitOutputFixtures.RangeDiff, DiffViewMode.RangeDiff) };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);

        TextEditorViewModel editor = viewer.Editor;
        editor.ShowLeftLineNumbers.Should().BeFalse();
        editor.Text.Should().StartWith("1:  cfa3f3e ! 1:  f0627de change\n");
        editor.DiffLines![0].Should().Be(new DiffLine(1, DiffLine.NotApplicable, 1, DiffLineKind.Header));
        editor.DiffLines![1].Kind.Should().Be(DiffLineKind.Context);
        (viewer.CanChangeContextLines, viewer.CanIgnoreWhitespaceChanges, viewer.IsDiffAppearanceVisible).Should().Be((true, true, false));
        viewer.GetMenuState().CanCopyPatch.Should().BeFalse("a range diff patch is undefined");

        // As RangeDiffHighlightService.GetFullDiffPrefixes: the four columns of the outer diff are removed too.
        int start = editor.Text.IndexOf("    @@", StringComparison.Ordinal);
        viewer.Copy("    @@ calc.cs: namespace Demo;\n          }", start);
        host.Copied.Should().Equal(("@@ calc.cs: namespace Demo;\n    }", true));
    }

    [Test]
    public async Task A_combined_diff_has_the_prefixes_of_all_parents()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = Diff(GitOutputFixtures.CombinedDiff, DiffViewMode.CombinedDiff) };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);

        TextEditorViewModel editor = viewer.Editor;
        string[] lines = editor.Text.Split('\n');
        editor.DiffLines!.Single(l => l.Kind != DiffLineKind.FileHeader && lines[l.LineNumInDiff - 1].StartsWith("++", StringComparison.Ordinal))
            .Should().Be(new DiffLine(14, DiffLine.NotApplicable, 7, DiffLineKind.Plus));
        editor.DiffLines!.Count(l => l.Kind == DiffLineKind.Minus).Should().Be(3);
        (viewer.CanIgnoreWhitespaceChanges, viewer.IsDiffAppearanceVisible).Should().Be((true, false));

        viewer.Copy("++        int result = value * 5;", editor.Text.IndexOf("\n++ ", StringComparison.Ordinal) + 1);
        host.Copied.Should().Equal(("        int result = value * 5;", true));
    }

    [Test]
    public void A_diff_file_is_shown_as_a_fixed_diff()
    {
        DiffViewModelTests.FakeViewerHost host = new();
        FileViewerViewModel viewer = new(host);
        viewer.Show(new FileViewContent(FileViewKind.Text, GitOutputFixtures.Patch, "changes.patch"));

        viewer.IsDiff.Should().BeTrue();
        viewer.DiffMode.Should().Be(DiffViewMode.FixedDiff);
        viewer.Editor.GitColoring.Should().NotBeNull("the escape sequences of the file are shown");
        viewer.Editor.DiffLines.Should().Contain(l => l.Kind == DiffLineKind.Plus);

        // As SetVisibilityDiffContextMenu: the options of the diff do not apply.
        (viewer.CanChangeContextLines, viewer.CanIgnoreWhitespaceAtEol, viewer.IsDiffAppearanceVisible).Should().Be((false, false, false));
        viewer.GetMenuState().CanCopyPatch.Should().BeTrue();

        viewer.Show(new FileViewContent(FileViewKind.Text, "text", "notes.txt"));
        viewer.IsDiff.Should().BeFalse();
    }

    [Test]
    public async Task The_syntax_highlighting_of_the_file_is_an_option_of_diffs()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = Diff(GitOutputFixtures.Patch) };
        FileViewerViewModel viewer = new(host);
        await viewer.ShowChangesAsync(Entry);
        viewer.Editor.FileName.Should().Be("calc.cs", "the syntax highlighting is shown by default");

        viewer.ToggleSyntaxHighlightingCommand.Execute(null);
        await host.Shown;
        host.Settings.ShowSyntaxHighlighting.Should().BeFalse();
        host.Requested.Should().HaveCount(2, "the diff is shown again");
        viewer.Editor.FileName.Should().BeNull();

        viewer.Show(new FileViewContent(FileViewKind.Text, "using System;", "Program.cs"));
        viewer.Editor.FileName.Should().Be("Program.cs", "a text is always highlighted");
    }

    [Test]
    public async Task Treat_all_files_as_text_is_an_option_of_the_viewer()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Content = Diff(GitOutputFixtures.Patch) };
        FileViewerViewModel viewer = new(host) { ViewerWidth = 640 };
        await viewer.ShowChangesAsync(Entry);

        viewer.ToggleTreatAllFilesAsTextCommand.Execute(null);
        viewer.TreatAllFilesAsText.Should().BeTrue();
        host.Requests.Should().Equal(new FileViewRequest(null, false, 640), new FileViewRequest(null, true, 640));
    }

    [Test]
    public async Task The_difftool_opens_for_the_changes_shown()
    {
        DiffViewModelTests.FakeViewerHost host = new() { Diff = FileViewerContextMenuTests.Patch };
        FileViewerViewModel viewer = new(host);
        viewer.CanOpenWithDifftool.Should().BeFalse();

        await viewer.ShowChangesAsync(Entry);
        viewer.CanOpenWithDifftool.Should().BeTrue();
        viewer.OpenWithDifftool();
        host.DifftoolsOpened.Should().Equal("f");

        host.Content = Diff(GitOutputFixtures.Grep, DiffViewMode.Grep);
        await viewer.ShowChangesAsync(Entry);
        viewer.CanOpenWithDifftool.Should().BeFalse("as ViewGrepAsync");
    }

    [Test]
    public void The_vertical_ruler_is_the_setting_unless_difftastic_sets_it()
    {
        DiffViewModelTests.FakeViewerHost host = new() { VerticalRulerPosition = 80 };
        FileViewerViewModel viewer = new(host);

        viewer.Show(Diff(GitOutputFixtures.Patch));
        viewer.Editor.VerticalRulerColumn.Should().Be(80);
        viewer.Show(Diff(GitOutputFixtures.Difftastic, DiffViewMode.Difftastic, difftasticWidth: 200));
        viewer.Editor.VerticalRulerColumn.Should().Be(97);
        viewer.Show(new FileViewContent(FileViewKind.Text, "text"));
        viewer.Editor.VerticalRulerColumn.Should().Be(80);
    }
}
