using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>Tests of the in-line differences of the Avalonia diff viewer (ports of the WinForms <c>DiffHighlightServiceTests</c>).</summary>
[TestFixture]
public sealed class InlineDiffAnalyzerTests
{
    [Test]
    public void Dims_identical_parts_at_begin_and_end()
    {
        const string before = "identical_part_before_";
        const string after = "_identical_part_after";
        const string text = $"@@ -1 +1 @@\n-{before}RemovedX{after}\n+{before}AddedY{after}\n";
        int removed = text.IndexOf("\n-", StringComparison.Ordinal) + 2;
        int added = text.IndexOf("\n+", StringComparison.Ordinal) + 2;

        IReadOnlyList<InlineDiffMarker> markers = Analyze(text);

        markers.Should().BeEquivalentTo(
        [
            new InlineDiffMarker(removed, before.Length, InlineDiffMarkerKind.DimmedRemoved),
            new InlineDiffMarker(removed + before.Length + "RemovedX".Length, after.Length, InlineDiffMarkerKind.DimmedRemoved),
            new InlineDiffMarker(added, before.Length, InlineDiffMarkerKind.DimmedAdded),
            new InlineDiffMarker(added + before.Length + "AddedY".Length, after.Length, InlineDiffMarkerKind.DimmedAdded),
        ]);
    }

    [Test]
    public void Anchors_insertions_and_deletions()
    {
        const string before = " identical_part_before ";
        const string after = " identical_part_after ";
        const string removedLine = $"-deletion{before}RemovedX{after}";
        const string addedLine = $"+{before}AddedY{after}insertion";
        const string text = $"@@ -1 +1 @@\n{removedLine}\n{addedLine}\n";
        int removed = text.IndexOf(removedLine, StringComparison.Ordinal) + 1;
        int added = text.IndexOf(addedLine, StringComparison.Ordinal) + 1;

        IReadOnlyList<InlineDiffMarker> markers = Analyze(text);

        markers.Should().BeEquivalentTo(
        [
            new InlineDiffMarker(removed + "deletion".Length, before.Length, InlineDiffMarkerKind.DimmedRemoved),
            new InlineDiffMarker(removed + "deletion".Length + before.Length + "RemovedX".Length, after.Length, InlineDiffMarkerKind.DimmedRemoved),
            new InlineDiffMarker(removed + removedLine.Length - 1, 0, InlineDiffMarkerKind.InsertionAnchor),
            new InlineDiffMarker(added, 0, InlineDiffMarkerKind.DeletionAnchor),
            new InlineDiffMarker(added, before.Length, InlineDiffMarkerKind.DimmedAdded),
            new InlineDiffMarker(added + before.Length + "AddedY".Length, after.Length, InlineDiffMarkerKind.DimmedAdded),
        ]);
    }

    [Test]
    public void Matches_the_lines_of_a_block_by_their_content_and_handles_crlf()
    {
        // The second removed line matches the first added line (re-indented), the first removed line has no match.
        const string text = "@@ -1,2 +1,1 @@\r\n-unrelated\r\n-    value = 1;\r\n+value = 2;\r\n";

        IReadOnlyList<InlineDiffMarker> markers = Analyze(text);

        markers.Should().NotBeEmpty();
        markers.Should().OnlyContain(m => text.Substring(m.Offset, m.Length).IndexOf('\r') < 0, "the line breaks are not part of the lines");
        markers.Where(m => m.Kind == InlineDiffMarkerKind.DimmedRemoved)
            .Should().OnlyContain(m => m.Offset > text.IndexOf("-    value", StringComparison.Ordinal), "'unrelated' does not match");
    }

    [Test]
    public void Nothing_to_mark_without_matching_blocks()
    {
        Analyze("@@ -1 +1,2 @@\n context\n+added\n").Should().BeEmpty();
        Analyze("plain text").Should().BeEmpty();
    }

    [Test]
    public void Line_matcher_iterates_all_combinations([Range(1, 4)] int firstEnd, [Range(1, 4)] int secondEnd)
    {
        DiffLinesMatcher.GetAllCombinations(firstEnd, secondEnd).Should().OnlyHaveUniqueItems()
            .And.HaveCount(firstEnd * secondEnd)
            .And.OnlyContain(pair => pair.FirstIndex < firstEnd && pair.SecondIndex < secondEnd);
    }

    [TestCase("camelCase", new[] { "camel", "Case" }, new[] { 0, 5 })]
    [TestCase("_precedingUnderScore", new[] { "_preceding", "Under", "Score" }, new[] { 0, 10, 15 })]
    [TestCase("multiple__under___scores_", new[] { "multiple", "under", "scores" }, new[] { 0, 10, 18 })]
    [TestCase("    sum += x;", new[] { "sum", "x" }, new[] { 4, 11 })]
    public void Line_matcher_splits_subwords(string text, string[] words, int[] offsets)
    {
        (string Word, int StartIndex)[] result = [.. DiffLinesMatcher.GetSubwords(text)];

        result.Select(pair => pair.Word).Should().Equal(words);
        result.Select(pair => pair.StartIndex).Should().Equal(offsets);
    }

    private static IReadOnlyList<InlineDiffMarker> Analyze(string text) => InlineDiffAnalyzer.Analyze(text, DiffLinesAnalyzer.Analyze(text));
}
