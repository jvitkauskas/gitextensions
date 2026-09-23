using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.HelperDialogs;
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
        if (!AvaloniaUi.IsEnabledFor(nameof(FormChooseCommit)))
        {
            return false;
        }

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
                positionName: nameof(FormChooseCommit)))
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
            if (TryShowGoToCommit(Owner, commands, out bool accepted, out ObjectId commitId))
            {
                return accepted ? commitId : null;
            }

            using FormGoToCommit form = new(commands);
            return form.ShowDialog(Owner) == DialogResult.OK ? form.ValidateAndGetSelectedObjectId() : null;
        });

        public void ShowRevisionFiltered(ObjectId objectId) => AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(Owner, objectId));

        public void ShowRevisionNotFound() => AvaloniaUi.RunInHostContext(() => MessageBoxes.CannotFindGitRevision(owner: Owner));
    }

    /// <summary>Loads the revisions for the Avalonia revision grid, as <c>RevisionGridControl.PerformRefreshRevisions</c> does.</summary>
    /// <param name="showArtificial">Whether to show the working directory and index changes (<c>ShowUncommittedChangesIfPossible</c>).</param>
    private sealed class RevisionGridHost(IGitUICommands commands, Func<ObjectId, ArgumentString> getRevisionFilter, bool showArtificial) : IRevisionGridHost
    {
        private readonly GitRevisionTester _revisionTester = new(new FullPathResolver(() => commands.Module.WorkingDir));

        public bool MatchesQuickSearch(GitRevision revision, string criteria) => _revisionTester.Matches(revision, criteria);

        public string CurrentBranch => commands.Module.GetSelectedBranch(emptyIfDetached: true);

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

                    // The stash ref is shown as the stash revisions, which git log does not list here.
                    ILookup<ObjectId, IGitRef> refsByObjectId = module.GetRefs(RefsFilter.NoFilter)
                        .Where(gitRef => !gitRef.ObjectId.IsZero && gitRef.CompleteName != GitRefName.RefsStashPrefix)
                        .ToLookup(gitRef => gitRef.ObjectId);

                    // As RevisionGridControl.ShowArtificialRevisions: the artificial commits are inserted before HEAD.
                    bool addArtificial = showArtificial && AppSettings.RevisionGraphShowArtificialCommits && !module.IsBareRepository();
                    bool headIsHandled = false;
                    RevisionBatchObserver observer = new(batch =>
                    {
                        foreach (GitRevision revision in batch)
                        {
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
                        pathFilter: "",
                        hasNotes: false,
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

        public void RunInBackground(Action work, Action then)
            => ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                work();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                then();
            });
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
