using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils.GitUI;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the choose commit dialog, the first user of the Avalonia revision grid (docs/avalonia-port/PLAN.md, phase 4).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  Shows the Avalonia port of <c>FormChooseCommit</c>; returns <see langword="false"/> if it is disabled.
    /// </summary>
    /// <param name="selected">The chosen commit, or <see langword="null"/> if cancelled.</param>
    public static bool TryChooseCommit(
        IWin32Window? owner,
        IGitUICommands commands,
        string? preselectCommit,
        out GitRevision? selected,
        bool showArtificial = false,
        bool showCurrentBranchOnly = false,
        string? lastRevisionToDisplayHash = null)
    {
        selected = null;
        IGitModule module = commands.Module;
        ObjectId? toBeSelected = null;
        if (!string.IsNullOrEmpty(preselectCommit) && module.RevParse(preselectCommit) is { IsZero: false } objectId)
        {
            toBeSelected = objectId;
        }

        RevisionGridHost gridHost = new(commands, GetRevisionFilterFactory(showCurrentBranchOnly, lastRevisionToDisplayHash), showArtificial);
        ChooseCommitViewModel? viewModel = null;
        try
        {
            if (ShowDialog(
                () =>
                {
                    ChooseCommitWindow window = new();
                    RevisionGridViewModel grid = new(gridHost, new RevisionGridDisplayOptions(AppSettings.RelativeDate, AppSettings.ShowAuthorDate, TranslatedStrings.SearchingFor, AppSettings.RevisionGridQuickSearchTimeout));
                    viewModel = new ChooseCommitViewModel(ViewStrings.Load<ChooseCommitStrings>(), grid, new ChooseCommitHost(commands, window));
                    window.DataContext = viewModel;
                    grid.Load(toBeSelected);
                    return window;
                },
                owner,
                positionName: "FormChooseCommit"))
            {
                selected = viewModel?.SelectedRevision;
            }
        }
        finally
        {
            viewModel?.Dispose();
        }

        return true;
    }

    /// <summary>
    ///  The git log arguments of the grid's filter (<c>FilterInfo</c>), as <c>FormChooseCommit</c> configures its grid.
    ///  Unlike the WinForms grid, showing the current branch only does not change the setting of the main grid.
    /// </summary>
    private static Func<ObjectId, ArgumentString> GetRevisionFilterFactory(bool showCurrentBranchOnly, string? lastRevisionToDisplayHash)
        => currentCheckout =>
        {
            FilterInfo filter = new();
            if (lastRevisionToDisplayHash is not null)
            {
                filter.LastRevisionToDisplayHash = lastRevisionToDisplayHash;
            }

            bool byBranchFilter = filter.ByBranchFilter;
            bool currentBranchOnly = filter.ShowCurrentBranchOnly;
            try
            {
                if (showCurrentBranchOnly)
                {
                    filter.ByBranchFilter = false;
                    filter.ShowCurrentBranchOnly = true;
                }

                return filter.GetRevisionFilter(new Lazy<ObjectId>(() => currentCheckout));
            }
            finally
            {
                filter.ByBranchFilter = byBranchFilter;
                filter.ShowCurrentBranchOnly = currentBranchOnly;
            }
        };

    private sealed class ChooseCommitHost(IGitUICommands commands, DialogWindow window) : IChooseCommitHost
    {
        private IWin32Window Owner => new NativeWindowOwner(window);

        public ObjectId? ChooseCommitToGoTo() => AvaloniaUi.RunInHostContext<ObjectId?>(() =>
        {
            return TryShowGoToCommit(Owner, commands, out bool accepted, out ObjectId commitId) && accepted ? commitId : null;
        });

        public void ShowRevisionFiltered(ObjectId objectId) => AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(Owner, objectId));

        public void ShowRevisionNotFound() => AvaloniaUi.RunInHostContext(() => MessageBoxes.CannotFindGitRevision(owner: Owner));
    }

    /// <summary>Loads the revisions for the Avalonia revision grid, as <c>RevisionGridControl.PerformRefreshRevisions</c> does.</summary>
    /// <param name="showArtificial">Whether to show the working directory and index changes (<c>ShowUncommittedChangesIfPossible</c>).</param>
    /// <param name="getPathFilter">The path arguments of git log, in the background (as <c>BuildPathFilter</c>); none if <see langword="null"/>.</param>
    private sealed class RevisionGridHost(IGitUICommands commands, Func<ObjectId, ArgumentString> getRevisionFilter, bool showArtificial, Func<CancellationToken, string>? getPathFilter = null) : IRevisionGridHost
    {
        private readonly GitRevisionTester _revisionTester = new(new FullPathResolver(() => commands.Module.WorkingDir));
        private HoverHighlightCalculator? _hoverHighlight;

        // As MessageColumnProvider.MaxSuperprojectRefs.
        private const int MaxSuperprojectRefs = 4;

        private SuperProjectInfo? _superproject;
        private RevisionGraph? _hoverGraph;
        private VisibleRowRange _visibleRange;

        public bool MatchesQuickSearch(GitRevision revision, string criteria) => _revisionTester.Matches(revision, criteria);

        public string CurrentBranch => commands.Module.GetSelectedBranch(emptyIfDetached: true);

        // As AuthorRevisionHighlighting.ProcessRevisionSelectionChange without a selected revision.
        public string UserEmail => commands.Module.GetEffectiveSetting(GitCommands.Config.SettingKeyString.UserEmail);

        public string GetAuthorToolTip(GitRevision revision) => GetAuthorAndCommiterToolTip(revision);

        public void OpenUrl(string url) => OsShellUtil.OpenUrlInDefaultBrowser(url);

        // Moved out of AuthorNameColumnProvider.
        private static string GetAuthorAndCommiterToolTip(GitRevision revision)
        {
            string toolTip;
            if (revision.Author == revision.Committer && revision.AuthorEmail == revision.CommitterEmail)
            {
                toolTip = $"{revision.Author} <{revision.AuthorEmail}> {TranslatedStrings.AuthoredAndCommitted}";
            }
            else
            {
                toolTip =
                    $"{revision.Author} <{revision.AuthorEmail}> {TranslatedStrings.Authored}\n" +
                    $"{revision.Committer} <{revision.CommitterEmail}> {TranslatedStrings.Committed}";
            }

            return toolTip;
        }

        // As AvatarColumnProvider.GetAvatar: the image of the provider, or the placeholder (Images.User80).
        public async Task<byte[]?> GetAvatarAsync(string email, string? name, int size)
        {
            Image? image = null;
            try
            {
                image = await GitUI.Avatars.AvatarService.DefaultProvider.GetAvatarAsync(email, name, DpiUtil.Scale(size)).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The placeholder, as the WinForms column when the avatar cannot be loaded.
            }

            image ??= Properties.Images.User80;
            using MemoryStream stream = new();
            image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }

        // As RevisionGraphColumnProvider.SetHoverHighlightAsync (HoverHighlightCalculator, in the visible rows).
        public async Task<IReadOnlySet<ObjectId>?> GetHoverHighlightAsync(RevisionGraph graph, IGitRef? gitRef, int rowIndex, int firstVisibleRow, int visibleRowCount)
        {
            if (_hoverHighlight is null || _hoverGraph != graph)
            {
                _hoverHighlight?.Dispose();
                _hoverHighlight = new HoverHighlightCalculator(graph, () => _visibleRange);
                _hoverGraph = graph;
            }

            _visibleRange = new VisibleRowRange(Math.Max(0, firstVisibleRow), Math.Max(0, visibleRowCount));
            HoverHighlightCalculator calculator = _hoverHighlight;
            await calculator.SetAsync(gitRef, rowIndex);
            if (!calculator.ConsumeIsDirty())
            {
                throw new OperationCanceledException();
            }

            return calculator.HighlightedIds;
        }

        public void LoadRevisions(RevisionGraph graph, Action reportBatch, Action<Exception?> completed, CancellationToken cancellationToken)
        {
            IGitModule module = commands.Module;
            ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                Exception? failure = null;
                try
                {
                    ObjectId currentCheckout = module.GetCurrentCheckout();
                    graph.HeadId = currentCheckout;

                    // As GetSuperprojectCheckoutAsync: in a submodule, the checkout and references of the superproject.
                    _superproject = await GetSuperprojectInfoAsync(module);

                    // As PerformRefreshRevisions: the 'stash' ref is excluded when the stashes are shown as rows.
                    bool showStashes = AppSettings.ShowStashes;
                    ILookup<ObjectId, IGitRef> refsByObjectId = module.GetRefs(RefsFilter.NoFilter)
                        .Where(gitRef => !gitRef.ObjectId.IsZero && (!showStashes || gitRef.CompleteName != GitRefName.RefsStashPrefix))
                        .ToLookup(gitRef => gitRef.ObjectId);
                    StashRows? stashes = showStashes && !module.IsBareRepository() ? StashRows.Read(module, cancellationToken) : null;

                    // As RevisionGridControl.ShowArtificialRevisions: the artificial commits are inserted before HEAD.
                    bool addArtificial = showArtificial && AppSettings.RevisionGraphShowArtificialCommits && !module.IsBareRepository();
                    bool headIsHandled = false;
                    RevisionBatchObserver observer = new(batch =>
                    {
                        foreach (GitRevision revision in batch)
                        {
                            List<GitRevision>? stashRows = null;
                            if (stashes is not null && !stashes.Handle(revision, out stashRows))
                            {
                                // A helper commit of a stash (index, or untracked without changes).
                                continue;
                            }

                            foreach (GitRevision stashRow in stashRows ?? [])
                            {
                                stashRow.Refs = [.. refsByObjectId[stashRow.ObjectId]];
                                graph.Add(stashRow);
                            }

                            revision.Refs = [.. refsByObjectId[revision.ObjectId]];
                            if (addArtificial && !headIsHandled && (revision.ObjectId == currentCheckout || currentCheckout.IsZero))
                            {
                                headIsHandled = true;
                                (GitRevision workTree, GitRevision index) = CreateArtificialRevisions(module, currentCheckout);
                                graph.Add(workTree);
                                graph.Add(index);
                            }

                            graph.Add(revision);
                        }

                        ReportOnUiThread(reportBatch);
                    });

                    new RevisionReader(module).GetLog(
                        observer,
                        getRevisionFilter(currentCheckout),
                        pathFilter: getPathFilter?.Invoke(cancellationToken) ?? "",
                        hasNotes: AppSettings.ShowGitNotesColumn.Value || AppSettings.ShowGitNotes,
                        ResourceManager.TranslatedStrings.Autostash,
                        cancellationToken);
                    observer.Failure?.Throw();

                    if (addArtificial && !headIsHandled)
                    {
                        // HEAD is not listed (filtered): show the artificial commits first.
                        (GitRevision workTree, GitRevision index) = CreateArtificialRevisions(module, currentCheckout);
                        graph.Insert(workTree, index, []);
                        ReportOnUiThread(reportBatch);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    failure = ex;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                completed(failure);
            });

            static void ReportOnUiThread(Action report)
                => ThreadHelper.FileAndForget(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    report();
                });
        }

        /// <summary>As <c>DrawSuperprojectInfo</c> and <c>DrawSuperprojectRefs</c> of <c>MessageColumnProvider</c>.</summary>
        public IReadOnlyList<RevisionRefItem> GetSuperprojectRefs(GitRevision revision)
        {
            if (_superproject is not { } superproject || revision.IsArtificial)
            {
                return [];
            }

            List<RevisionRefItem> items = [];
            AddMarker(superproject.CurrentCommit, "\u25CF");
            AddMarker(superproject.ConflictBase, "Base");
            AddMarker(superproject.ConflictLocal, "Local");
            AddMarker(superproject.ConflictRemote, "Remote");

            // The references not also of the submodule, at most MaxSuperprojectRefs (the last as an ellipsis).
            if (superproject.Refs is not null && superproject.Refs.TryGetValue(revision.ObjectId, out IReadOnlyList<IGitRef>? refs))
            {
                List<IGitRef> shown = [.. refs.Where(gitRef => !revision.Refs.Any(own => own.CompleteName == gitRef.CompleteName))];
                for (int i = 0; i < Math.Min(MaxSuperprojectRefs, shown.Count); i++)
                {
                    string name = i < MaxSuperprojectRefs - 1 ? shown[i].Name : "\u2026";
                    items.Add(new RevisionRefItem(name, RevisionRefKind.Superproject, IsCurrentBranch: shown[i].IsSelected));
                }
            }

            return items;

            void AddMarker(ObjectId id, string name)
            {
                if (id == revision.ObjectId)
                {
                    items.Add(new RevisionRefItem(name, RevisionRefKind.Superproject, IsCurrentBranch: false));
                }
            }
        }

        // Copied from GetSuperprojectCheckoutAsync of RevisionGridControl (a local function there).
        private static async Task<SuperProjectInfo?> GetSuperprojectInfoAsync(IGitModule module)
        {
            if (module.SuperprojectModule is not { } superprojectModule)
            {
                return null;
            }

            SuperProjectInfo info = new();
            (char code, ObjectId commit) = await module.GetSuperprojectCurrentCheckoutAsync().ConfigureAwait(false);
            if (code == 'U')
            {
                ConflictData conflict = await superprojectModule.GetConflictAsync(module.SubmodulePath).ConfigureAwait(false);
                info.ConflictBase = conflict.Base.ObjectId;
                info.ConflictLocal = conflict.Local.ObjectId;
                info.ConflictRemote = conflict.Remote.ObjectId;
            }
            else
            {
                info.CurrentCommit = commit;
            }

            Dictionary<IGitRef, IGitItem?> refs = await superprojectModule.GetSubmoduleItemsForEachRefAsync(module.SubmodulePath, noLocks: true).ConfigureAwait(false);
            if (refs is not null)
            {
                info.Refs = refs
                    .Where(pair => pair.Value is not null && !pair.Value.ObjectId.IsZero)
                    .GroupBy(pair => pair.Value!.ObjectId)
                    .ToDictionary(group => group.Key, group => (IReadOnlyList<IGitRef>)[.. group.Select(pair => pair.Key)]);
            }

            return info;
        }

        public void RunInBackground(Action work, Action then)
            => ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                work();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                then();
            });
    }

    /// <summary>
    ///  The stashes shown as rows, as <c>PerformRefreshRevisions</c> reads them (<c>GetStashRevs</c>) and inserts them in
    ///  <c>OnRevisionRead</c>: each stash (with its reflog selector as name) before its base commit, the untracked files
    ///  commit after it if it has changes, and not the index commit.
    /// </summary>
    /// <remarks>
    ///  git log lists the most recent stash (refs/stash) with its index and untracked commits; unlike the WinForms grid, these
    ///  helper commits are not shown either, and that stash gets the parents of the others.
    /// </remarks>
    private sealed class StashRows
    {
        private readonly Dictionary<ObjectId, GitRevision> _stashesById;
        private readonly ILookup<ObjectId, GitRevision>? _stashesByParentId;
        private readonly Dictionary<ObjectId, GitRevision> _untrackedByStashId = [];
        private readonly HashSet<ObjectId> _helperIds = [];

        private StashRows(IReadOnlyCollection<GitRevision> stashes, ILookup<ObjectId, GitRevision>? stashesByParentId)
        {
            _stashesById = stashes.ToDictionary(r => r.ObjectId);
            _stashesByParentId = stashesByParentId;
        }

        public static StashRows? Read(IGitModule module, CancellationToken cancellationToken)
        {
            // Get the "main" stash commit, including the reflog selector.
            IReadOnlyCollection<GitRevision> stashes = new RevisionReader(module).GetStashes(cancellationToken);
            if (stashes.Count == 0)
            {
                return null;
            }

            // Git stores stashes in 2 or 3 commits. The (first) "stash" commit is listed by git-stash-list,
            // does not include untracked files, may be stored in the third "untracked" commit.
            // The second "index" commit are ignored (can be seen with reflog).
            if (AppSettings.ShowReflogReferences)
            {
                // The "untracked" commits are already shown in the grid.
                return new StashRows(stashes, stashesByParentId: null);
            }

            StashRows rows = new(stashes, stashes.Where(r => !r.FirstParentId.IsZero).ToLookup(r => r.FirstParentId));

            // "untracked" commits to insert (parent to "stash" commits); the command listing them is quite slow, hence the
            // limited number of stashes evaluated for untracked files.
            Dictionary<ObjectId, ObjectId> untrackedIdByStashId = stashes
                .Where(stash => stash.ParentIds!.Count >= 3)
                .Take(AppSettings.MaxStashesWithUntrackedFiles)
                .ToDictionary(stash => stash.ObjectId, stash => stash.ParentIds![2]);
            Dictionary<ObjectId, GitRevision> untrackedRevs = new RevisionReader(module)
                .GetRevisionsFromList([.. untrackedIdByStashId.Values.Distinct()], cancellationToken)
                .ToDictionary(r => r.ObjectId);
            foreach ((ObjectId stashId, ObjectId untrackedId) in untrackedIdByStashId)
            {
                if (untrackedRevs.TryGetValue(untrackedId, out GitRevision? untracked))
                {
                    rows._untrackedByStashId[stashId] = untracked;
                }
            }

            // Remove parents not included ("index" and empty "untracked" commits).
            foreach (GitRevision stash in stashes)
            {
                rows._helperIds.UnionWith(stash.ParentIds!.Skip(1));
                stash.ParentIds = rows._untrackedByStashId.ContainsKey(stash.ObjectId)
                    ? [stash.FirstParentId, stash.ParentIds![2]]
                    : [stash.FirstParentId];
            }

            rows._helperIds.ExceptWith(rows._untrackedByStashId.Values.Select(r => r.ObjectId));
            return rows;
        }

        /// <summary>
        ///  As <c>OnRevisionRead</c>: <paramref name="stashRows"/> are the stashes to add before <paramref name="revision"/>
        ///  (it is their base commit), with their untracked commits; <see langword="false"/> if the revision is not shown.
        /// </summary>
        public bool Handle(GitRevision revision, out List<GitRevision>? stashRows)
        {
            stashRows = null;
            if (_stashesById.Count == 0)
            {
                return !IsHelper(revision);
            }

            if (_stashesById.Remove(revision.ObjectId, out GitRevision? gridStash))
            {
                // The most recent stash, listed by git log: its name, and the parents of the listed stashes.
                revision.ReflogSelector = gridStash.ReflogSelector;
                if (_stashesByParentId is not null)
                {
                    revision.ParentIds = gridStash.ParentIds;
                }

                return true;
            }

            if (IsHelper(revision))
            {
                return false;
            }

            if (_stashesByParentId?.Contains(revision.ObjectId) is true)
            {
                foreach (GitRevision stash in _stashesByParentId[revision.ObjectId])
                {
                    // Add if not already added (reflogs etc list before parent commit).
                    if (_stashesById.Remove(stash.ObjectId))
                    {
                        stashRows ??= [];
                        stashRows.Add(stash);
                        if (_untrackedByStashId.TryGetValue(stash.ObjectId, out GitRevision? untracked))
                        {
                            stashRows.Add(untracked);
                        }
                    }
                }
            }

            return true;
        }

        // The index commit of a stash, or its untracked commit without changes.
        private bool IsHelper(GitRevision revision)
            => _helperIds.Contains(revision.ObjectId);
    }

    /// <summary>The working directory and index commits, as <c>RevisionGridControl.AddArtificialRevisions</c> creates them.</summary>
    private static (GitRevision WorkTree, GitRevision Index) CreateArtificialRevisions(IGitModule module, ObjectId currentCheckout)
    {
        string userName = module.GetEffectiveSetting(GitCommands.Config.SettingKeyString.UserName);
        string userEmail = module.GetEffectiveSetting(GitCommands.Config.SettingKeyString.UserEmail);
        GitRevision workTree = new(ObjectId.WorkTreeId)
        {
            Author = userName,
            AuthorEmail = userEmail,
            Committer = userName,
            CommitterEmail = userEmail,
            Subject = ResourceManager.TranslatedStrings.Workspace,
            ParentIds = [ObjectId.IndexId],
            Notes = "",
        };
        GitRevision index = new(ObjectId.IndexId)
        {
            Author = userName,
            AuthorEmail = userEmail,
            Committer = userName,
            CommitterEmail = userEmail,
            Subject = ResourceManager.TranslatedStrings.Index,
            ParentIds = currentCheckout.IsZero ? null : [currentCheckout],
            Notes = "",
        };
        return (workTree, index);
    }

    /// <summary>Receives the batches of <c>RevisionReader.GetLog</c> on its reading thread.</summary>
    private sealed class RevisionBatchObserver(Action<IReadOnlyList<GitRevision>> onBatch) : IObserver<IReadOnlyList<GitRevision>>
    {
        public System.Runtime.ExceptionServices.ExceptionDispatchInfo? Failure { get; private set; }

        public void OnNext(IReadOnlyList<GitRevision> value) => onBatch(value);

        public void OnError(Exception error) => Failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error);

        public void OnCompleted()
        {
        }
    }
}
