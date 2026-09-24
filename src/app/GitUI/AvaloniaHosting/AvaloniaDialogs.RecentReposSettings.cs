using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the recent repositories settings (docs/avalonia-port/PLAN.md, phase 2, batch 9).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <param name="saved">Whether the settings were saved (<c>DialogResult.OK</c>).</param>
    public static bool TryShowRecentReposSettings(IWin32Window? owner, out bool saved)
    {
        saved = false;
        RecentReposOptions options = new(
            AppSettings.ShorteningRecentRepoPathStrategy,
            AppSettings.HideTopRepositoriesFromRecentList.Value,
            AppSettings.SortTopRepos,
            AppSettings.SortRecentRepos,
            AppSettings.MaxTopRepositories,
            AppSettings.RecentReposComboMinWidth,
            AppSettings.RecentRepositoriesHistorySize);
        RecentReposSettingsHost host = new(ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Locals.LoadRecentHistoryAsync));

        saved = ShowDialog(
            () => new RecentReposSettingsWindow { DataContext = new RecentReposSettingsViewModel(ViewStrings.Load<RecentReposSettingsStrings>(), options, host) },
            owner,
            positionName: "FormRecentReposSettings");
        return true;
    }

    /// <summary>Edits the repository history in memory, as <c>FormRecentReposSettings</c>, until it is saved.</summary>
    private sealed class RecentReposSettingsHost(IList<Repository> history) : IRecentReposSettingsHost
    {
        private IList<Repository> _history = history;

        public (IReadOnlyList<RecentRepoItem> Top, IReadOnlyList<RecentRepoItem> Recent) Split(RecentReposOptions options)
        {
            List<RecentRepoInfo> topRepos = [];
            List<RecentRepoInfo> recentRepos = [];
            RecentRepoSplitter splitter = new()
            {
                MaxTopRepositories = options.MaxTopRepositories,
                HideTopRepositoriesFromRecentList = options.HideTopRepositoriesFromRecentList,
                ShorteningStrategy = options.ShorteningStrategy,
                SortRecentRepos = options.SortRecentRepos,
                SortTopRepos = options.SortTopRepos,
                RecentReposComboMinWidth = options.ComboMinWidth,
                MeasureFont = AppSettings.Font,
            };
            splitter.SplitRecentRepos(_history, topRepos, recentRepos);

            return ([.. topRepos.Select(r => ToItem(r, Repository.RepositoryAnchor.AnchoredInTop))],
                    [.. recentRepos.Select(r => ToItem(r, Repository.RepositoryAnchor.AnchoredInRecent))]);

            static RecentRepoItem ToItem(RecentRepoInfo repo, Repository.RepositoryAnchor listAnchor)
                => new(repo.Repo.Path, repo.Caption ?? repo.Repo.Path, repo.Repo.Anchor, repo.Repo.Anchor == listAnchor, Directory.Exists(repo.Repo.Path));
        }

        public void SetAnchor(IEnumerable<string> paths, Repository.RepositoryAnchor anchor)
        {
            HashSet<string> selected = [.. paths];
            foreach (Repository repository in _history.Where(r => selected.Contains(r.Path)))
            {
                repository.Anchor = anchor;
            }
        }

        public void RemoveFromRecent(IEnumerable<string> paths)
            => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                foreach (string path in paths)
                {
                    _history = await RepositoryHistoryManager.Locals.RemoveRecentAsync(path);
                }
            });

        public void Save(RecentReposOptions options)
        {
            // As FormRecentReposSettings.SaveSettings.
            AppSettings.ShorteningRecentRepoPathStrategy = options.ShorteningStrategy;
            AppSettings.HideTopRepositoriesFromRecentList.Value = options.HideTopRepositoriesFromRecentList;
            AppSettings.SortTopRepos = options.SortTopRepos;
            AppSettings.SortRecentRepos = options.SortRecentRepos;
            AppSettings.MaxTopRepositories = options.MaxTopRepositories;
            AppSettings.RecentReposComboMinWidth = options.ComboMinWidth;
            AppSettings.RecentRepositoriesHistorySize = options.RecentRepositoriesHistorySize;
            ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistoryManager.Locals.SaveRecentHistoryAsync(_history));
        }
    }
}
