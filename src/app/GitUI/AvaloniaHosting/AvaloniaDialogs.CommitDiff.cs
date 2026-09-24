using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the commit diff dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>Shows the Avalonia port of <c>FormCommitDiff</c>, modal or modeless (as <c>RevisionGridControl.ViewSelectedRevisions</c>).</summary>
    public static bool TryShowCommitDiff(IWin32Window? owner, IGitUICommands commands, ObjectId objectId, bool modeless = false)
    {
        AvaloniaUi.EnsureInitialized(GetOptions);
        CommitDiffWindow window = new() { PositionName = "FormCommitDiff", PositionStore = WindowPositionStore.Instance };
        CommitDiffViewModel viewModel = new(
            ViewStrings.Load<CommitDiffStrings>(),
            new CommitDiffHost(commands),
            new FileViewerHost(commands),
            new CommitInfoHost(commands),
            ViewStrings.Load<FileStatusListStrings>(),
            GetFileStatusTreeOptions(),
            objectId);
        UseFileStatusListMenu(viewModel.Files, commands, window);
        window.DataContext = viewModel;
        if (modeless)
        {
            AvaloniaDialogHost.Show(window, owner?.Handle ?? 0);
        }
        else
        {
            AvaloniaDialogHost.ShowDialog(window, owner?.Handle ?? 0);
        }

        return true;
    }

    private sealed class CommitDiffHost(IGitUICommands commands) : ICommitDiffHost
    {
        public GitRevision? GetRevision(ObjectId objectId) => commands.Module.GetRevision(objectId);

        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(GitRevision revision, CancellationToken cancellationToken)
            => CalculateDiffsAsync(commands, [revision], headId: default, allowMultiDiff: false, cancellationToken);

        public string WorkingDirectory => commands.Module.WorkingDir;
    }
}
