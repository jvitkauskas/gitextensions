using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the diff dialog, the first user of the Avalonia file status list (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>Shows the Avalonia port of <c>FormDiff</c> (modeless, as <c>RevisionGridControl.ShowFormDiff</c>).</summary>
    public static bool TryShowDiff(IGitUICommands commands, ObjectId firstId, ObjectId secondId, string firstDisplayName, string secondDisplayName)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormDiff)))
        {
            return false;
        }

        AvaloniaUi.EnsureInitialized(GetOptions);
        DiffWindow window = new() { PositionName = nameof(FormDiff), PositionStore = WindowPositionStore.Instance };
        window.DataContext = new DiffViewModel(
            ViewStrings.Load<DiffStrings>(),
            new DiffHost(commands, window),
            new FileViewerHost(commands),
            ViewStrings.Load<FileStatusListStrings>(),
            GetFileStatusTreeOptions(),
            new GitRevision(firstId),
            new GitRevision(secondId),
            firstDisplayName,
            secondDisplayName,
            GetMergeBase(commands.Module, firstId, secondId));
        AvaloniaDialogHost.Show(window, ownerHandle: 0);
        return true;
    }

    /// <summary>The sorting and settings of the file status list (<c>DiffListSortService</c>, <c>AppSettings</c>).</summary>
    internal static FileStatusTreeOptions GetFileStatusTreeOptions()
        => new(
            DiffListSortService.Instance.DiffListSorting,
            MergeSingleItemsWithFolder: AppSettings.FileStatusMergeSingleItemWithFolder.Value,
            ShowGroupNodesInFlatList: AppSettings.FileStatusShowGroupNodesInFlatList.Value);

    /// <summary>As the constructor of <c>FormDiff</c>: artificial commits are compared with the current checkout.</summary>
    private static GitRevision? GetMergeBase(IGitModule module, ObjectId firstId, ObjectId secondId)
    {
        Lazy<ObjectId> currentHead = new(() => module.GetCurrentCheckout());
        ObjectId firstMergeId = firstId.IsArtificial ? currentHead.Value : firstId;
        ObjectId secondMergeId = secondId.IsArtificial ? currentHead.Value : secondId;
        if (firstMergeId.IsZero || secondMergeId.IsZero || firstMergeId == secondMergeId)
        {
            return null;
        }

        ObjectId mergeBase = module.GetMergeBase(firstMergeId, secondMergeId);
        return mergeBase.IsZero ? null : new GitRevision(mergeBase);
    }

    private sealed class DiffHost(IGitUICommands commands, DialogWindow window) : IDiffHost
    {
        private readonly Lazy<ObjectId> _currentHead = new(() => commands.Module.GetCurrentCheckout());

        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        /// <summary>As <c>FileStatusList.SetDiffsAsync</c> with <c>FileStatusDiffCalculator</c>.</summary>
        public async Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
        {
            ObjectId headId = _currentHead.Value;
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();

            FileStatusDiffCalculator calculator = new(() => Module);
            calculator.SetDiff(revisions, headId, allowMultiDiff: true);
            IReadOnlyList<FileStatusWithDescription> diffs = calculator.Calculate(prevList: [], refreshDiff: true, refreshGrep: false, cancellationToken);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return [.. diffs.Select(ToGroup)];
        }

        /// <summary>As <c>FormDiff.PickAnotherBranch</c>.</summary>
        public (string DisplayName, GitRevision? Revision)? PickBranch(GitRevision preselect) => AvaloniaUi.RunInHostContext<(string, GitRevision?)?>(() =>
        {
            if (!TryShowCompareToBranch(Owner, commands, preselect.ObjectId, out string? branchName))
            {
                using FormCompareToBranch form = new(commands, preselect.ObjectId);
                branchName = form.ShowDialog(Owner) == DialogResult.OK ? form.BranchName : null;
            }

            if (branchName is null)
            {
                return null;
            }

            ObjectId objectId = Module.RevParse(branchName);
            return (branchName, objectId.IsZero ? null : new GitRevision(objectId));
        });

        /// <summary>As <c>FormDiff.PickAnotherCommit</c>.</summary>
        public GitRevision? PickCommit(GitRevision preselect) => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryChooseCommit(Owner, commands, preselect.Guid, out GitRevision? chosen, showArtificial: true))
            {
                return chosen;
            }

            using FormChooseCommit form = new(commands, preselectCommit: preselect.Guid, showArtificial: true);
            return form.ShowDialog(Owner) == DialogResult.OK ? form.SelectedRevision : null;
        });

        public void OpenDirectoryDiff(GitRevision first, GitRevision second)
            => Module.OpenWithDifftoolDirDiff(first.Guid, second.Guid, customTool: null);

        private static FileStatusGroup ToGroup(FileStatusWithDescription diff)
            => new(diff.FirstRev, diff.SecondRev, diff.Summary, diff.Statuses, diff.BaseA, diff.BaseB, diff.IconName);
    }
}
