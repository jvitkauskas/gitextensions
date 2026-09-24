using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>The filter of the revision grid of the Avalonia main window, with its toolbar (<c>FilterToolBar</c>).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>As <c>InitFilters</c>: the text and path filters of the command line (the text filter by commit message).</summary>
    private static FilterInfo CreateBrowseFilter(BrowseArguments args)
    {
        FilterInfo filter = new();
        if (!string.IsNullOrWhiteSpace(args.RevFilter))
        {
            filter.Apply(new RevisionFilter(args.RevFilter.Trim(), byCommit: true, byCommitter: false, byAuthor: false, byDiffContent: false));
        }

        if (!string.IsNullOrWhiteSpace(args.PathFilter))
        {
            filter.ByPathFilter = true;
            filter.PathFilter = args.PathFilter.ToPosixPath().QuoteNE();
        }

        return filter;
    }

    /// <summary>
    ///  As the <c>IRevisionGridFilter</c> of <c>RevisionGridControl</c>: the <see cref="FilterInfo"/> of the window, the
    ///  revisions loaded again when it changes.
    /// </summary>
    internal sealed class BrowseGridFilter(IGitUICommands commands, Func<IWin32Window> owner, FilterInfo filter) : IRevisionGridFilterHost
    {
        public event EventHandler? FilterChanged;

        public FilterInfo Filter => filter;

        /// <summary>The grid whose revisions are loaded with the filter.</summary>
        public RevisionGridViewModel? Grid { get; set; }

        public RevisionGridFilterState State
        {
            get
            {
                FilterChangedEventArgs e = new(filter);
                return new RevisionGridFilterState(
                    e.ShowAllBranches,
                    e.ShowCurrentBranchOnly,
                    e.ShowFilteredBranches,
                    e.ShowOnlyFirstParent,
                    e.ShowReflogReferences,
                    e.HasFilter,
                    e.PathFilter,
                    e.BranchFilter,
                    e.MessageFilter,
                    e.CommitterFilter,
                    e.AuthorFilter,
                    e.DiffContentFilter,
                    e.FilterSummary);
            }
        }

        public bool IsValidWorkingDir => commands.Module.IsValidGitWorkingDir();

        public IReadOnlyList<string> RevisionFilterHistory
        {
            get => AppSettings.RevisionFilterDropdowns;
            set => AppSettings.RevisionFilterDropdowns = [.. value];
        }

        /// <summary>As <c>PerformRefreshRevisions</c>: the revisions are loaded again, and the filter reported.</summary>
        public void Refresh()
        {
            Grid?.Load(Grid.SelectedRow?.ObjectId);
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetAndApplyBranchFilter(string branchFilter)
        {
            filter.SetBranchFilter(branchFilter);
            Refresh();
        }

        public void SetAndApplyRevisionFilter(RevisionTextFilter textFilter)
        {
            if (filter.Apply(new RevisionFilter(textFilter.Text, textFilter.ByMessage, textFilter.ByCommitter, textFilter.ByAuthor, textFilter.ByDiffContent)))
            {
                Refresh();
            }
        }

        public void SetAndApplyPathFilter(string pathFilter)
        {
            filter.ByPathFilter = !string.IsNullOrWhiteSpace(pathFilter);
            if (filter.ByPathFilter)
            {
                filter.PathFilter = pathFilter;
            }

            Refresh();
        }

        public void ResetAllFiltersAndRefresh()
        {
            filter.ResetAllFilters();
            Refresh();
        }

        public void ShowAllBranches()
        {
            if (!filter.IsShowAllBranchesChecked)
            {
                filter.ByBranchFilter = false;
                filter.ShowCurrentBranchOnly = false;
                Refresh();
            }
        }

        public void ShowCurrentBranchOnly()
        {
            if (!filter.IsShowCurrentBranchOnlyChecked)
            {
                filter.ByBranchFilter = false;
                filter.ShowCurrentBranchOnly = true;
                Refresh();
            }
        }

        public void ShowFilteredBranches()
        {
            if (!filter.IsShowFilteredBranchesChecked)
            {
                // Must be able to set ByBranchFilter without a filter to edit it
                filter.ByBranchFilter = true;
                filter.ShowCurrentBranchOnly = false;
                Refresh();
            }
        }

        public void ToggleShowOnlyFirstParent()
        {
            filter.ShowOnlyFirstParent = !filter.ShowOnlyFirstParent;
            Refresh();
        }

        public void ToggleShowReflogReferences()
        {
            filter.ShowReflogReferences = !filter.ShowReflogReferences;
            Refresh();
        }

        public void ShowRevisionFilterDialog() => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryShowRevisionFilter(owner(), filter, out bool accepted) && accepted)
            {
                Refresh();
            }
        });

        public async Task<IReadOnlyList<string>> GetRefNamesAsync(bool local, bool tags, bool remotes)
        {
            IGitModule module = commands.Module;
            await TaskScheduler.Default;

            // As BranchesFilter: the kinds limit the refs; without a kind all are listed, with the stash and the notes.
            RefsFilter refsFilter = (local ? RefsFilter.Heads : RefsFilter.NoFilter)
                | (tags ? RefsFilter.Tags : RefsFilter.NoFilter)
                | (remotes ? RefsFilter.Remotes : RefsFilter.NoFilter);
            string[] names = [.. module.GetRefs(refsFilter).Select(gitRef => gitRef.Name)];
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return names;
        }

        public IReadOnlyList<string> GetRefLocalNames() => [.. commands.Module.GetRefs(RefsFilter.NoFilter).Select(gitRef => gitRef.LocalName)];

        public bool RevisionExists(string revision) => !commands.Module.RevParse(revision).IsZero;

        public void ShowWarning(string heading, string caption) => AvaloniaUi.RunInHostContext(() =>
        {
            TaskDialogPage page = new()
            {
                Heading = heading,
                Caption = caption,
                Buttons = { TaskDialogButton.OK },
                Icon = TaskDialogIcon.Warning,
                SizeToContent = true,
            };
            TaskDialog.ShowDialog(owner(), page);
        });

        /// <summary>
        ///  As <c>RevisionGridControl.BuildPathFilter</c>, without following the renames of a single file (which only the file
        ///  history does in the Avalonia port).
        /// </summary>
        public string GetPathFilter()
        {
            string path = filter.PathFilter;
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            // Manual arguments must be quoted if needed (internal paths are quoted), except for simple arguments.
            path = path.Trim();
            return !path.Any(c => c is '"' or '\'' or ' ') ? path.Quote() : path;
        }
    }
}
