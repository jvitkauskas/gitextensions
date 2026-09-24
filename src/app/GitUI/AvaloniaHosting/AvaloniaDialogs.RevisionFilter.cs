using GitExtensions.Extensibility;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.UserControls.RevisionGrid;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the revision filter dialog (docs/avalonia-port/PLAN.md, phase 2, batch 7).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The Avalonia port of <c>FormRevisionFilter</c>; <paramref name="filterInfo"/> is updated if accepted.</summary>
    public static bool TryShowRevisionFilter(IWin32Window? owner, FilterInfo filterInfo, out bool accepted)
    {
        accepted = false;

        // As FormRevisionFilter.OnLoad: the dialog shows the raw values.
        FilterInfo raw = filterInfo with { IsRaw = true };
        RevisionFilterViewModel viewModel = new(
            ViewStrings.Load<RevisionFilterStrings>(),
            new RevisionFilterValues(
                raw.ByDateFrom,
                raw.DateFrom,
                raw.ByDateTo,
                raw.DateTo,
                raw.ByAuthor,
                raw.Author,
                raw.ByCommitter,
                raw.Committer,
                raw.ByMessage,
                raw.Message,
                raw.ByDiffContent,
                raw.DiffContent,
                raw.IgnoreCase,
                raw.ByCommitsLimit,
                raw.CommitsLimit,
                raw.ByPathFilter,
                raw.PathFilter,
                raw.IsShowFilteredBranchesChecked,
                raw.BranchFilter,
                raw.ShowCurrentBranchOnly,
                raw.ShowReflogReferences,
                raw.ShowOnlyFirstParent,
                raw.HideMergeCommits,
                raw.ShowSimplifyByDecoration,
                raw.ShowFullHistory,
                raw.ShowSimplifyMerges),
            filterInfo.CommitsLimitDefault);

        accepted = ShowDialog(() => new RevisionFilterWindow { DataContext = viewModel }, owner);
        if (accepted && viewModel.Result is RevisionFilterValues result)
        {
            // As FormRevisionFilter.OkClick.
            filterInfo.ByDateFrom = result.ByDateFrom;
            filterInfo.DateFrom = result.DateFrom;
            filterInfo.ByDateTo = result.ByDateTo;
            filterInfo.DateTo = result.DateTo;
            filterInfo.ByAuthor = result.ByAuthor;
            filterInfo.Author = result.Author;
            filterInfo.ByCommitter = result.ByCommitter;
            filterInfo.Committer = result.Committer;
            filterInfo.ByMessage = result.ByMessage;
            filterInfo.Message = result.Message;
            filterInfo.ByDiffContent = result.ByDiffContent;
            filterInfo.DiffContent = result.DiffContent;
            filterInfo.IgnoreCase = result.IgnoreCase;
            filterInfo.ByCommitsLimit = result.ByCommitsLimit;
            filterInfo.CommitsLimit = result.CommitsLimit;
            filterInfo.ByPathFilter = result.ByPathFilter;
            filterInfo.PathFilter = result.PathFilter;
            filterInfo.ByBranchFilter = result.ByBranchFilter;
            filterInfo.BranchFilter = result.BranchFilter;
            filterInfo.ShowCurrentBranchOnly = result.ShowCurrentBranchOnly;
            filterInfo.ShowReflogReferences = result.ShowReflogReferences;
            filterInfo.ShowOnlyFirstParent = result.ShowOnlyFirstParent;
            filterInfo.HideMergeCommits = result.HideMergeCommits;
            filterInfo.ShowSimplifyByDecoration = result.ShowSimplifyByDecoration;
            filterInfo.ShowFullHistory = result.ShowFullHistory;
            filterInfo.ShowSimplifyMerges = result.ShowSimplifyMerges;
        }

        return true;
    }
}
