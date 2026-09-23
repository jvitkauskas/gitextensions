using GitExtUtils.GitUI.Theming;
using GitUI.Editor.Diff;
using GitUI.Presentation.Editor;
using GitUI.Theming;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

namespace GitUITests.Editor.Diff;

/// <summary>
///  The Avalonia ports of the diff analysis (<see cref="DiffLinesAnalyzer"/>, <see cref="InlineDiffAnalyzer"/>) must agree with the
///  WinForms <see cref="DiffLineNumAnalyzer"/> and <see cref="DiffHighlightService"/> for patches without git's colors
///  (docs/avalonia-port/PLAN.md, phase 3). A failure after an upstream merge points at a change to re-port.
/// </summary>
[Apartment(ApartmentState.STA)]
public class AvaloniaDiffPortParityTests
{
    private static IEnumerable<string> SampleDiffs
        => Directory.EnumerateFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "Editor", "Diff"), "*.diff").Select(Path.GetFileName)!;

    [TestCaseSource(nameof(SampleDiffs))]
    public void Line_numbers_match_the_WinForms_viewer(string fileName)
    {
        string text = ReadSample(fileName);
        bool isCombinedDiff = DiffLinesAnalyzer.IsCombinedDiff(text);

        DiffLinesInfo expected = DiffLineNumAnalyzer.Analyze(text, [], isCombinedDiff);
        IReadOnlyList<DiffLine> actual = DiffLinesAnalyzer.Analyze(text, isCombinedDiff);

        // The WinForms analyzer skips the file header lines before the first hunk.
        actual.Where(line => line.Kind != DiffLineKind.FileHeader)
            .Select(line => (line.LineNumInDiff, line.LeftLineNumber, line.RightLineNumber, Kind: line.Kind.ToString()))
            .Should().Equal(expected.DiffLines.Values.OrderBy(line => line.LineNumInDiff)
                .Select(line => (line.LineNumInDiff, line.LeftLineNumber, line.RightLineNumber, Kind: line.LineType.ToString())));
    }

    [TestCaseSource(nameof(SampleDiffs))]
    public void Inline_differences_match_the_WinForms_viewer(string fileName)
    {
        string text = ReadSample(fileName);
        bool isCombinedDiff = DiffLinesAnalyzer.IsCombinedDiff(text);

        List<InlineDiffMarker> expected = GetWinFormsInlineMarkers(text, isCombinedDiff);
        IReadOnlyList<InlineDiffMarker> actual = InlineDiffAnalyzer.Analyze(text, DiffLinesAnalyzer.Analyze(text, isCombinedDiff));

        actual.Should().BeEquivalentTo(expected);
        if (fileName is "Sample.diff" or "gaps.diff")
        {
            actual.Should().NotBeEmpty("the sample has matching removed and added lines");
        }
    }

    private static string ReadSample(string fileName)
    {
        // Ensure that the test doesn't depend on core.autocrlf.
        return File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Editor", "Diff", fileName)).Replace("\r\n", "\n");
    }

    private static List<InlineDiffMarker> GetWinFormsInlineMarkers(string text, bool isCombinedDiff)
    {
        using TextEditorControl textEditor = new();
        DiffViewerLineNumberControl lineNumbers = new(textEditor.ActiveTextAreaControl.TextArea);
        string highlightedText = text;
        DiffHighlightService service = isCombinedDiff
            ? new CombinedDiffHighlightService(ref highlightedText, useGitColoring: false, lineNumbers)
            : new PatchHighlightService(ref highlightedText, useGitColoring: false, lineNumbers);
        IDocument document = textEditor.Document;
        document.TextContent = highlightedText;
        service.AddTextHighlighting(document);

        Color dimmedAdded = AppColor.AnsiTerminalGreenBackNormal.GetThemeColor().DimColor().DimColor();
        Color dimmedRemoved = AppColor.AnsiTerminalRedBackNormal.GetThemeColor().DimColor().DimColor();
        Color addedAnchor = AppColor.AnsiTerminalGreenForeBold.GetThemeColor();
        Color removedAnchor = AppColor.AnsiTerminalRedForeBold.GetThemeColor();

        List<InlineDiffMarker> markers = [];
        foreach (TextMarker marker in document.MarkerStrategy.TextMarker)
        {
            InlineDiffMarkerKind? kind = (marker.TextMarkerType, marker.Color) switch
            {
                (TextMarkerType.InterChar, Color c) when c == addedAnchor => InlineDiffMarkerKind.InsertionAnchor,
                (TextMarkerType.InterChar, Color c) when c == removedAnchor => InlineDiffMarkerKind.DeletionAnchor,
                (TextMarkerType.SolidBlock, Color c) when c == dimmedAdded => InlineDiffMarkerKind.DimmedAdded,
                (TextMarkerType.SolidBlock, Color c) when c == dimmedRemoved => InlineDiffMarkerKind.DimmedRemoved,
                _ => null, // The colors of the lines.
            };
            if (kind is { } markerKind)
            {
                // ICSharpCode stores the zero-length anchors (InterChar) with length 1.
                int length = marker.TextMarkerType == TextMarkerType.InterChar ? 0 : marker.Length;
                markers.Add(new InlineDiffMarker(marker.Offset, length, markerKind));
            }
        }

        return markers;
    }
}
