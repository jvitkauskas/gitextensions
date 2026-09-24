using System.ComponentModel.Design;
using System.Text;
using CommonTestUtils;
using GitCommands;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI;
using GitUI.AvaloniaHosting;
using GitUI.Editor.Diff;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.ScriptsEngine;
using GitUIPluginInterfaces;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using NSubstitute;
using ResourceManager;

namespace GitUITests.Editor.Diff;

/// <summary>
///  The Avalonia ports of the appearances of the viewer (the git word diff, difftastic, combined diffs, grep results and range
///  diffs: <see cref="DiffLinesAnalyzer"/>, <see cref="DifftasticAnalyzer"/>, <see cref="GrepAnalyzer"/>,
///  <see cref="RangeDiffAnalyzer"/>, and <see cref="FileViewerHost"/> for the git commands) must agree with the WinForms highlight
///  services (docs/avalonia-port/PLAN.md, phase 3). The outputs of git come from the samples and from a temporary repository.
///  A failure after an upstream merge points at a change to re-port.
/// </summary>
[Apartment(ApartmentState.STA)]
public class AvaloniaDiffAppearancesPortParityTests
{
    private const string Base = "namespace Demo;\n\npublic sealed class Calculator\n{\n    // Multiplies the value.\n    public int Compute(int value)\n    {\n        int result = value * 2;\n        return result + Offset;\n    }\n\n    private const int Offset = 1;\n}\n";
    private const string Changed = "namespace Demo;\n\npublic sealed class Calculator\n{\n    public int Compute(int value)\n    {\n        int result = value * 3;\n        return result + Offset;\n    }\n\n    private const int Offset = 10;\n\n    public int Twice(int value) => Compute(value) * 2;\n}\n";

    private TextEditorControl _textEditor = null!;
    private DiffViewerLineNumberControl _lineNumbers = null!;

    [SetUp]
    public void SetUp()
    {
        _textEditor = new TextEditorControl();
        _lineNumbers = new DiffViewerLineNumberControl(_textEditor.ActiveTextAreaControl.TextArea);
    }

    [TearDown]
    public void TearDown() => _textEditor.Dispose();

    [Test]
    public void Difftastic_matches_the_WinForms_viewer()
    {
        string text = ReadSample("SampleDifftastic.diff");
        bool reverse = AppSettings.ReverseGitColoring.Value;
        EnvironmentAbstraction env = new();
        string? width = env.GetEnvironmentVariable("DFT_WIDTH");
        int rightColumnStart;
        string expectedText = text;
        try
        {
            // The width of the sample (the service reads it from the environment, as git difftool passes it).
            env.SetEnvironmentVariable("DFT_WIDTH", "200");
            DifftasticHighlightService service = new(ref expectedText, _lineNumbers, out rightColumnStart);
            _textEditor.Document.TextContent = expectedText;
            service.AddTextHighlighting(_textEditor.Document);
        }
        finally
        {
            env.SetEnvironmentVariable("DFT_WIDTH", width);
        }

        AnalyzedDiff actual = DifftasticAnalyzer.Analyze(text, 200, HostThemeColors.Instance, reverse);

        actual.Text.Should().Be(expectedText);
        actual.VerticalRulerColumn.Should().Be(rightColumnStart);
        LinesOf(actual.Lines).Should().Equal(ExpectedLines());
        actual.Segments.Should().BeEquivalentTo(ExpectedSegments());

        // The kinds depend on the colors of the theme (e.g. of the other tests), which both viewers read the same.
        actual.Lines.Select(l => l.Kind).Should().Contain(DiffLineKind.PlusRight);
    }

    [Test]
    public void The_git_word_diff_sample_matches_the_WinForms_viewer()
    {
        string coloredText = ReadSample("SampleGitWord.diff");
        StringBuilder text = new();
        List<TextMarker> markers = [];
        AnsiEscapeUtilities.ParseEscape(coloredText, text, markers, traceErrors: false);
        DiffLinesInfo expected = DiffLineNumAnalyzer.Analyze(text.ToString(), markers, isCombinedDiff: false, isGitWordDiff: true);

        (string parsedText, IReadOnlyList<ColoredSegment> segments) = AnsiEscapeParser.Parse(coloredText, HostThemeColors.Instance);
        IReadOnlyList<DiffLine> actual = DiffLinesAnalyzer.Analyze(parsedText, isCombinedDiff: false, new GitColoring(segments, HostThemeColors.Instance, AppSettings.ReverseGitColoring.Value), isGitWordDiff: true);

        LinesOf(actual).Should().Equal(expected.DiffLines.Values.OrderBy(l => l.LineNumInDiff).Select(l => (l.LineNumInDiff, l.LeftLineNumber, l.RightLineNumber, l.LineType.ToString(), l.IsMovedLine)));
        actual.Select(l => l.Kind).Should().Contain(DiffLineKind.MinusPlus);
    }

    [Test]
    public void The_git_word_diff_of_git_matches_the_WinForms_viewer()
    {
        using GitModuleTestHelper helper = CreateRepository();
        helper.CreateRepoFile("calc.cs", Changed);
        FileStatusEntry entry = new(new GitRevision(ObjectId.IndexId), new GitRevision(ObjectId.WorkTreeId),
            new GitItemStatus("calc.cs") { IsChanged = true, IsTracked = true, Staged = StagedStatus.WorkTree });

        DiffDisplayAppearance appearance = AppSettings.DiffDisplayAppearance.Value;
        try
        {
            // A runtime setting, not saved.
            AppSettings.DiffDisplayAppearance.Value = DiffDisplayAppearance.GitWordDiff;
            (FileViewContent content, FileViewerViewModel viewer) = ShowChanges(helper, entry);

            content.Should().Match<FileViewContent>(c => c.Kind == FileViewKind.Diff && c.DiffMode == DiffViewMode.Diff && c.HasGitColors && !c.SupportsLinePatching);
            string text = content.Text;
            PatchHighlightService service = new(ref text, useGitColoring: true, _lineNumbers);
            _textEditor.Document.TextContent = text;
            service.AddTextHighlighting(_textEditor.Document);

            viewer.Editor.Text.Should().Be(text);
            LinesOf(viewer.Editor.DiffLines!).Should().Equal(ExpectedLines());
            viewer.Editor.DiffLines!.Select(l => l.Kind).Should().Contain([DiffLineKind.MinusLeft, DiffLineKind.MinusPlus, DiffLineKind.PlusRight]);
            viewer.Editor.InlineDiffMarkers.Should().BeEmpty();
            viewer.Editor.GitColoring!.Segments.Should().BeEquivalentTo(ExpectedSegments(), "git colors the words, no in-line differences are added");
        }
        finally
        {
            AppSettings.DiffDisplayAppearance.Value = appearance;
        }
    }

    [Test]
    public void The_combined_diff_of_git_matches_the_WinForms_viewer()
    {
        using GitModuleTestHelper helper = CreateRepository();
        IGitModule module = helper.Module;
        helper.CreateRepoFile("calc.cs", Changed);
        Git(module, "commit -am change");
        Git(module, "switch -c other HEAD~1");
        helper.CreateRepoFile("calc.cs", Base.Replace("value * 2", "value * 4"));
        Git(module, "commit -am other");
        Git(module, "switch -");
        _ = module.GitExecutable.RunCommand("merge other", throwOnErrorExit: false);
        helper.CreateRepoFile("calc.cs", Changed.Replace("value * 3", "value * 5"));
        Git(module, "add calc.cs");
        Git(module, "commit --no-edit");
        ObjectId merge = module.RevParse("HEAD");
        FileStatusEntry entry = new(new GitRevision(ObjectId.CombinedDiffId), new GitRevision(merge), new GitItemStatus("calc.cs") { IsChanged = true, IsTracked = true });

        (FileViewContent content, FileViewerViewModel viewer) = ShowChanges(helper, entry);

        content.Should().Match<FileViewContent>(c => c.Kind == FileViewKind.Diff && c.DiffMode == DiffViewMode.CombinedDiff && !c.SupportsLinePatching);
        string text = content.Text;
        _ = new CombinedDiffHighlightService(ref text, useGitColoring: content.HasGitColors, _lineNumbers);
        viewer.Editor.Text.Should().Be(text);
        LinesOf(viewer.Editor.DiffLines!).Should().Equal(ExpectedLines());
        viewer.Editor.DiffLines!.Count(l => l.Kind == DiffLineKind.Minus).Should().BeGreaterThan(1, "both parents had other lines");
    }

    [Test]
    public void The_grep_results_of_git_match_the_WinForms_viewer()
    {
        using GitModuleTestHelper helper = CreateRepository();
        helper.CreateRepoFile("calc.cs", Changed);
        Git(helper.Module, "commit -am change");
        ObjectId head = helper.Module.RevParse("HEAD");
        ObjectId parent = helper.Module.RevParse("HEAD~1");
        FileStatusEntry entry = new(new GitRevision(parent), new GitRevision(head), new GitItemStatus("calc.cs") { IsChanged = true, IsTracked = true, GrepString = "value" });

        (FileViewContent content, FileViewerViewModel viewer) = ShowChanges(helper, entry);

        content.Should().Match<FileViewContent>(c => c.Kind == FileViewKind.Diff && c.DiffMode == DiffViewMode.Grep);
        string text = content.Text;
        GrepHighlightService service = new(ref text, _lineNumbers);
        _textEditor.Document.TextContent = text;
        service.AddTextHighlighting(_textEditor.Document);

        viewer.Editor.Text.Should().Be(text);
        LinesOf(viewer.Editor.DiffLines!).Should().Equal(ExpectedLines());
        viewer.Editor.DiffLines!.Select(l => l.Kind).Should().Contain(DiffLineKind.Grep);
        viewer.Editor.GitColoring!.Segments.Should().BeEquivalentTo(ExpectedSegments());
    }

    [Test]
    public void The_range_diff_of_git_matches_the_WinForms_viewer()
    {
        using GitModuleTestHelper helper = CreateRepository();
        IGitModule module = helper.Module;
        helper.CreateRepoFile("calc.cs", Changed);
        Git(module, "commit -am change");
        ObjectId first = module.RevParse("HEAD");
        Git(module, "switch -c rebased HEAD~1");
        helper.CreateRepoFile("calc.cs", Changed.Replace("Offset = 10;", "Offset = 100;"));
        Git(module, "commit -am change");
        ObjectId second = module.RevParse("HEAD");
        FileStatusEntry entry = new(new GitRevision(first), new GitRevision(second), new GitItemStatus("Range diff") { IsRangeDiff = true });

        (FileViewContent content, FileViewerViewModel viewer) = ShowChanges(helper, entry);

        // As ViewChangesAsync, whose file name regex does not match git's colors before the "@@" (the syntax highlighting falls back to the item).
        (content.Kind, content.DiffMode, content.FileName).Should().Be((FileViewKind.Diff, DiffViewMode.RangeDiff, "Range diff"));
        string text = content.Text;
        RangeDiffHighlightService service = new(ref text, _lineNumbers);
        _textEditor.Document.TextContent = text;
        service.AddTextHighlighting(_textEditor.Document);

        viewer.Editor.Text.Should().Be(text);
        LinesOf(viewer.Editor.DiffLines!).Should().Equal(ExpectedLines());
        viewer.Editor.DiffLines!.Count(l => l.Kind == DiffLineKind.Header).Should().Be(1, "one commit is compared");
        viewer.Editor.GitColoring!.Segments.Should().BeEquivalentTo(ExpectedSegments());
    }

    private static GitModuleTestHelper CreateRepository()
    {
        GitModuleTestHelper helper = new();
        helper.Module.SetSetting("core.autocrlf", "false");
        helper.CreateRepoFile("calc.cs", Base);
        Git(helper.Module, "add calc.cs");
        Git(helper.Module, "commit -m base");
        return helper;
    }

    private static void Git(IGitModule module, string arguments)
        => module.GitExecutable.RunCommand(arguments).Should().BeTrue(arguments);

    /// <summary>The changes as <see cref="FileViewerHost"/> gets them from git, and the Avalonia viewer that shows them.</summary>
    private static (FileViewContent Content, FileViewerViewModel Viewer) ShowChanges(GitModuleTestHelper helper, FileStatusEntry entry)
    {
        ServiceContainer services = new();
        IScriptsManager scriptsManager = Substitute.For<IScriptsManager>();
        scriptsManager.GetScripts().Returns([]);
        services.AddService(scriptsManager);
        services.AddService(Substitute.For<IHotkeySettingsLoader>());
        FileViewerHost host = new(new GitUICommands(services, helper.Module));

        FileViewContent content = ThreadHelper.JoinableTaskFactory.Run(() => host.GetChangesAsync(entry, new FileViewRequest(), CancellationToken.None));
        FileViewerViewModel viewer = new(host);
        viewer.Show(content);
        return (content, viewer);
    }

    private IEnumerable<(int Line, int Left, int Right, string Kind, bool IsMoved)> ExpectedLines()
        => _lineNumbers.GetTestAccessor().Result.DiffLines.Values.OrderBy(l => l.LineNumInDiff)
            .Select(l => (l.LineNumInDiff, l.LeftLineNumber, l.RightLineNumber, l.LineType.ToString(), l.IsMovedLine));

    private static IEnumerable<(int Line, int Left, int Right, string Kind, bool IsMoved)> LinesOf(IEnumerable<DiffLine> lines)
        => lines.Where(l => l.Kind != DiffLineKind.FileHeader).Select(l => (l.LineNumInDiff, l.LeftLineNumber, l.RightLineNumber, l.Kind.ToString(), l.IsMovedLine));

    /// <summary>The colors of the markers of the WinForms viewer.</summary>
    private List<ColoredSegment> ExpectedSegments()
        => [.. _textEditor.Document.MarkerStrategy.TextMarker.Select(m => new ColoredSegment(m.Offset, m.Length, m.Color, m.OverrideForeColor ? m.ForeColor : null))];

    private static string ReadSample(string fileName)
        => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Editor", "Diff", fileName)).Replace("\r\n", "\n");
}
