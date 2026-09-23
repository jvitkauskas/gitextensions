using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>Tests of the diff mode of the text editor and the view patch dialog (phase 3).</summary>
[TestFixture]
public sealed class DiffViewerViewModelTests
{
    internal const string Diff = """
        diff --git a/file.txt b/file.txt
        index 1111111..2222222 100644
        --- a/file.txt
        +++ b/file.txt
        @@ -10,4 +10,4 @@ class Example
         context 1
        -removed
        +added 1
        +added 2
         context 2
        \ No newline at end of file

        """;

    [Test]
    public void Analyzer_numbers_the_lines_of_both_files()
    {
        IReadOnlyList<DiffLine> lines = DiffLinesAnalyzer.Analyze(Diff.ReplaceLineEndings("\n"));

        lines.Select(l => (l.Kind, l.LeftLineNumber, l.RightLineNumber)).Should().Equal(
            (DiffLineKind.FileHeader, -1, -1),
            (DiffLineKind.FileHeader, -1, -1),
            (DiffLineKind.FileHeader, -1, -1),
            (DiffLineKind.FileHeader, -1, -1),
            (DiffLineKind.Header, -1, -1),
            (DiffLineKind.Context, 10, 10),
            (DiffLineKind.Minus, 11, -1),
            (DiffLineKind.Plus, -1, 11),
            (DiffLineKind.Plus, -1, 12),
            (DiffLineKind.Context, 12, 13),
            (DiffLineKind.Header, -1, -1));
        lines.Select(l => l.LineNumInDiff).Should().Equal(Enumerable.Range(1, 11));
    }

    [Test]
    public void Analyzer_handles_crlf_line_endings_and_hunks_without_counts()
    {
        IReadOnlyList<DiffLine> lines = DiffLinesAnalyzer.Analyze("@@ -3 +3 @@\r\n-a\r\n+b\r\n");

        lines.Select(l => (l.Kind, l.LeftLineNumber, l.RightLineNumber)).Should().Equal(
            (DiffLineKind.Header, -1, -1),
            (DiffLineKind.Minus, 3, -1),
            (DiffLineKind.Plus, -1, 3));
    }

    [Test]
    public void Analyzer_numbers_only_the_result_of_a_combined_diff()
    {
        const string combined = "diff --cc file.txt\n@@@ -1,2 -1,2 +1,2 @@@\n  same\n- ours\n +theirs\n++both\n";
        DiffLinesAnalyzer.IsCombinedDiff(combined).Should().BeTrue();

        IReadOnlyList<DiffLine> lines = DiffLinesAnalyzer.Analyze(combined);

        lines.Select(l => (l.Kind, l.LeftLineNumber, l.RightLineNumber)).Should().Equal(
            (DiffLineKind.FileHeader, -1, -1),
            (DiffLineKind.Header, -1, -1),
            (DiffLineKind.Context, -1, 1),
            (DiffLineKind.Minus, -1, -1),
            (DiffLineKind.Plus, -1, 2),
            (DiffLineKind.Plus, -1, 3));
    }

    [Test]
    public void Text_editor_leaves_diff_mode_when_loading_a_file()
    {
        TextEditorViewModel editor = new();
        editor.LoadDiff(Diff);
        editor.IsReadOnly.Should().BeTrue();
        editor.DiffLines.Should().HaveCount(11);

        editor.Load("text", "file.txt");
        editor.DiffLines.Should().BeNull();
    }

    [Test]
    public void View_patch_loads_the_file_and_shows_the_first_patch()
    {
        FakeHost host = new() { Patches = [CreatePatch("a.txt"), CreatePatch("b.txt")] };

        ViewPatchViewModel viewModel = new(new ViewPatchStrings(), host, @"C:\patches\fix.patch");

        host.Loaded.Should().Equal(@"C:\patches\fix.patch");
        viewModel.Patches.Select(p => p.FileNameA).Should().Equal("a.txt", "b.txt");
        viewModel.SelectedPatch.Should().BeSameAs(viewModel.Patches[0]);
        viewModel.Diff.Text.Should().Be(Diff);
        viewModel.Diff.DiffLines.Should().NotBeNull();

        viewModel.SelectedPatch = null;
        viewModel.Diff.Text.Should().Be(Diff, "the diff stays shown, as in FormViewPatch");
    }

    [Test]
    public void View_patch_browses_for_a_file_and_ignores_one_that_cannot_be_read()
    {
        FakeHost host = new() { Browsed = @"C:\patches\other.patch", Failure = new IOException("Missing") };
        ViewPatchViewModel viewModel = new(new ViewPatchStrings(), host);
        host.Loaded.Should().BeEmpty("no file is given");

        viewModel.BrowseCommand.Execute(null);

        host.Filters.Should().Equal(("Patch file (*.Patch)|*.patch", "Select patch file"));
        viewModel.PatchFile.Should().Be(@"C:\patches\other.patch");
        viewModel.Patches.Should().BeEmpty();
    }

    private static Patch CreatePatch(string fileName)
        => new("diff --git", "index", PatchFileType.Text, fileName, fileName, PatchChangeType.ChangeFile, Diff);

    private sealed class FakeHost : IViewPatchHost
    {
        public IReadOnlyList<Patch> Patches { get; init; } = [];

        public string? Browsed { get; init; }

        public Exception? Failure { get; init; }

        public List<string> Loaded { get; } = [];

        public List<(string Filter, string Title)> Filters { get; } = [];

        public string? BrowsePatchFile(string filter, string title)
        {
            Filters.Add((filter, title));
            return Browsed;
        }

        public IReadOnlyList<Patch> LoadPatches(string path)
        {
            Loaded.Add(path);
            return Failure is null ? Patches : throw Failure;
        }
    }
}
