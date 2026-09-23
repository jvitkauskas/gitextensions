using System.Text;
using GitCommands;
using GitExtUtils.GitUI.Theming;
using GitUI.AvaloniaHosting;
using GitUI.Editor.Diff;
using GitUI.Presentation.Editor;
using GitUI.Theming;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace GitUITests.Editor.Diff;

/// <summary>
///  The Avalonia port of git's colors (<see cref="AnsiEscapeParser"/>, and the moved lines and in-line differences of a colored
///  diff) must agree with the WinForms <see cref="AnsiEscapeUtilities"/> and <see cref="DiffHighlightService"/>
///  (docs/avalonia-port/PLAN.md, phase 3). A failure after an upstream merge points at a change to re-port.
/// </summary>
[Apartment(ApartmentState.STA)]
public class AvaloniaAnsiPortParityTests
{
    private static IEnumerable<TestCaseData> ColoredSampleCases
        => from fileName in new[] { "AnsiTerminalColors.diff", "diff_added_moved.diff", "diff_removed_moved.diff", "SampleDifftastic.diff", "SampleGitWord.diff" }
           from themeColors in new[] { false, true }
           select new TestCaseData(fileName, themeColors);

    private static IEnumerable<string> ColoredPatchDiffs => ["diff_added_moved.diff", "diff_removed_moved.diff", ChangedLinesSample];

    // git -c color.diff.old="red reverse" -c color.diff.new="green reverse" diff --color=always
    private const string ChangedLinesSample = "changed lines";
    private const string ChangedLinesDiff
        = "\u001b[1mdiff --git a/a.cs b/a.cs\u001b[m\n"
        + "\u001b[1mindex 4f94160..aea988b 100644\u001b[m\n"
        + "\u001b[1m--- a/a.cs\u001b[m\n"
        + "\u001b[1m+++ b/a.cs\u001b[m\n"
        + "\u001b[36m@@ -1,5 +1,5 @@\u001b[m\n"
        + " public int Compute(int value)\u001b[m\n"
        + " {\u001b[m\n"
        + "\u001b[7;31m-    return value * 2 + Offset;\u001b[m\n"
        + "\u001b[7;31m-    // deprecated\u001b[m\n"
        + "\u001b[7;32m+\u001b[m\u001b[7;32m    return value * 3 + Offset;\u001b[m\n"
        + "\u001b[7;32m+\u001b[m\u001b[7;32m    // deprecated since 5.0\u001b[m\n"
        + " }\u001b[m\n";

    [TestCaseSource(nameof(ColoredSampleCases))]
    public void Colors_match_the_WinForms_viewer(string fileName, bool themeColors)
    {
        string text = ReadSample(fileName);
        StringBuilder expectedText = new();
        List<TextMarker> expectedMarkers = [];
        AnsiEscapeUtilities.ParseEscape(text, expectedText, expectedMarkers, themeColors, traceErrors: false);

        (string actualText, IReadOnlyList<ColoredSegment> actualSegments) = AnsiEscapeParser.Parse(text, HostThemeColors.Instance, themeColors);

        actualText.Should().Be(expectedText.ToString());
        actualSegments.Should().NotBeEmpty();
        actualSegments.Should().Equal(expectedMarkers.Select(m => new ColoredSegment(m.Offset, m.Length, m.Color, m.OverrideForeColor ? m.ForeColor : null)));
    }

    [TestCaseSource(nameof(ColoredPatchDiffs))]
    public void Moved_lines_match_the_WinForms_viewer(string fileName)
    {
        string coloredText = ReadSample(fileName);
        StringBuilder text = new();
        List<TextMarker> markers = [];
        AnsiEscapeUtilities.ParseEscape(coloredText, text, markers, traceErrors: false);

        DiffLinesInfo expected = DiffLineNumAnalyzer.Analyze(text.ToString(), markers, isCombinedDiff: false);
        (string parsedText, IReadOnlyList<ColoredSegment> segments) = AnsiEscapeParser.Parse(coloredText, HostThemeColors.Instance);
        IReadOnlyList<DiffLine> actual = DiffLinesAnalyzer.Analyze(parsedText, isCombinedDiff: false, new GitColoring(segments, HostThemeColors.Instance, AppSettings.ReverseGitColoring.Value));

        actual.Where(line => line.Kind != DiffLineKind.FileHeader).Select(line => (line.LineNumInDiff, line.IsMovedLine))
            .Should().Equal(expected.DiffLines.Values.OrderBy(line => line.LineNumInDiff).Select(line => (line.LineNumInDiff, line.IsMovedLine)));
        actual.Any(line => line.IsMovedLine).Should().Be(fileName != ChangedLinesSample, "the samples have moved lines, except the changed lines");
    }

    [TestCaseSource(nameof(ColoredPatchDiffs))]
    public void Inline_differences_of_a_colored_diff_match_the_WinForms_viewer(string fileName)
    {
        string coloredText = ReadSample(fileName);

        using TextEditorControl textEditor = new();
        DiffViewerLineNumberControl lineNumbers = new(textEditor.ActiveTextAreaControl.TextArea);
        string text = coloredText;
        PatchHighlightService service = new(ref text, useGitColoring: true, lineNumbers);
        IDocument document = textEditor.Document;
        document.TextContent = text;
        service.AddTextHighlighting(document);

        TextEditorViewModelProbe probe = new(coloredText);
        probe.Text.Should().Be(text);
        probe.InlineDiffMarkers.Should().BeEquivalentTo(GetInlineMarkers(document));
        if (fileName == ChangedLinesSample)
        {
            probe.InlineDiffMarkers.Should().NotBeEmpty("the lines are changed, not moved");
        }
    }

    private static string ReadSample(string fileName)
        => fileName == ChangedLinesSample
            ? ChangedLinesDiff
            : File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Editor", "Diff", fileName)).Replace("\r\n", "\n");

    /// <summary>The in-line markers of the WinForms viewer: anchors, and the dimmed parts (on the background or the text).</summary>
    private static List<InlineDiffMarker> GetInlineMarkers(IDocument document)
    {
        bool dimBackground = AppSettings.ReverseGitColoring.Value;
        Color addedAnchor = AppColor.AnsiTerminalGreenForeBold.GetThemeColor();
        Color removedAnchor = AppColor.AnsiTerminalRedForeBold.GetThemeColor();
        Color dimmedAdded = dimBackground ? AppColor.AnsiTerminalGreenBackNormal.GetThemeColor().DimColor().DimColor() : addedAnchor.DimColor();
        Color dimmedRemoved = dimBackground ? AppColor.AnsiTerminalRedBackNormal.GetThemeColor().DimColor().DimColor() : removedAnchor.DimColor();

        List<InlineDiffMarker> markers = [];
        foreach (TextMarker marker in document.MarkerStrategy.TextMarker)
        {
            Color color = dimBackground || marker.TextMarkerType == TextMarkerType.InterChar ? marker.Color : marker.ForeColor;
            InlineDiffMarkerKind? kind = (marker.TextMarkerType, color) switch
            {
                (TextMarkerType.InterChar, Color c) when c == addedAnchor => InlineDiffMarkerKind.InsertionAnchor,
                (TextMarkerType.InterChar, Color c) when c == removedAnchor => InlineDiffMarkerKind.DeletionAnchor,
                (TextMarkerType.SolidBlock, Color c) when c == dimmedAdded => InlineDiffMarkerKind.DimmedAdded,
                (TextMarkerType.SolidBlock, Color c) when c == dimmedRemoved => InlineDiffMarkerKind.DimmedRemoved,
                _ => null, // git's colors
            };
            if (kind is { } markerKind)
            {
                // ICSharpCode stores the zero-length anchors (InterChar) with length 1.
                markers.Add(new InlineDiffMarker(marker.Offset, marker.TextMarkerType == TextMarkerType.InterChar ? 0 : marker.Length, markerKind));
            }
        }

        return markers;
    }

    /// <summary>The text and in-line markers the Avalonia text editor shows for git's colored diff.</summary>
    private sealed class TextEditorViewModelProbe
    {
        public TextEditorViewModelProbe(string coloredText)
        {
            TextEditorViewModel viewModel = new();
            viewModel.LoadDiff(coloredText, HostThemeColors.Instance, AppSettings.ReverseGitColoring.Value);
            Text = viewModel.Text;
            InlineDiffMarkers = viewModel.InlineDiffMarkers;
        }

        public string Text { get; }

        public IReadOnlyList<InlineDiffMarker> InlineDiffMarkers { get; }
    }
}
