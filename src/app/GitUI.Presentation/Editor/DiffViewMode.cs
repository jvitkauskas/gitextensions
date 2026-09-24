namespace GitUI.Presentation.Editor;

/// <summary>What kind of diff the viewer shows: the diff kinds of the WinForms <c>ViewMode</c>, with the same names.</summary>
public enum DiffViewMode
{
    /// <summary>A patch (<c>git diff</c>), also git's word diff (<c>DiffDisplayAppearance.GitWordDiff</c>).</summary>
    Diff,

    /// <summary>A patch that the options do not change, e.g. a .diff or .patch file.</summary>
    FixedDiff,

    /// <summary>The output of difftastic (<c>git difftool --tool=difftastic</c>), side by side.</summary>
    Difftastic,

    /// <summary>The output of <c>git range-diff</c>.</summary>
    RangeDiff,

    /// <summary>The combined diff of a merge commit (changes from all parents).</summary>
    CombinedDiff,

    /// <summary>The output of <c>git grep</c>.</summary>
    Grep,
}

/// <summary>As <c>ViewModeExtension</c>.</summary>
public static class DiffViewModeExtensions
{
    /// <summary>As <c>IsNormalDiffView</c>: a patch.</summary>
    public static bool IsNormalDiffView(this DiffViewMode mode)
        => mode is DiffViewMode.Diff or DiffViewMode.FixedDiff or DiffViewMode.CombinedDiff;

    /// <summary>As <c>IsDiffView</c>: not grep results.</summary>
    public static bool IsDiffView(this DiffViewMode mode)
        => mode.IsNormalDiffView() || mode is DiffViewMode.RangeDiff or DiffViewMode.Difftastic;

    /// <summary>
    ///  As <c>IsSearchMatch</c> of the highlight services: the lines that next and previous change go to (for a range diff, the
    ///  headers of the commits).
    /// </summary>
    public static bool IsSearchMatch(this DiffViewMode mode, DiffLineKind kind)
        => mode switch
        {
            // As RangeDiffHighlightService.IsSearchMatch.
            DiffViewMode.RangeDiff => kind is DiffLineKind.Header,

            // As DifftasticHighlightService.IsSearchMatch.
            DiffViewMode.Difftastic => kind is DiffLineKind.Plus or DiffLineKind.Minus or DiffLineKind.MinusPlus or DiffLineKind.MinusLeft or DiffLineKind.PlusRight,

            // As DiffHighlightService.IsSearchMatch and GrepHighlightService.IsSearchMatch.
            _ => kind is DiffLineKind.Minus or DiffLineKind.Plus or DiffLineKind.MinusPlus or DiffLineKind.Grep,
        };

    /// <summary>
    ///  As <c>GetFullDiffPrefixes</c> of the highlight services: the prefixes of the lines that copying removes, or
    ///  <see langword="null"/> if the text is copied as it is (as <c>CopyToolStripMenuItemClick</c>).
    /// </summary>
    public static string[]? GetFullDiffPrefixes(this DiffViewMode mode)
        => mode switch
        {
            // As CombinedDiffHighlightService.
            DiffViewMode.CombinedDiff => ["  ", "++", "+ ", " +", "--", "- ", " -"],

            // As RangeDiffHighlightService.
            DiffViewMode.RangeDiff => ["      ", "    ++", "    + ", "     +", "    --", "    - ", "     -", "    +-", "    -+", "    "],

            // As PatchHighlightService.
            DiffViewMode.Diff or DiffViewMode.FixedDiff => [" ", "+", "-"],
            _ => null,
        };
}
