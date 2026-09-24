using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the log window of the <c>viewdiff</c> verb (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>Shows the Avalonia port of <c>FormLog</c> modally (as <c>GitUICommands.StartCompareRevisionsDialog</c>).</summary>
    /// <param name="accepted">Whether the dialog was accepted, which it never is (as <c>FormLog</c>).</param>
    public static bool TryShowLog(IWin32Window? owner, IGitUICommands commands, out bool accepted)
    {
        accepted = false;
        AvaloniaUi.EnsureInitialized(GetOptions);

        LogWindow window = new() { PositionName = "FormLog", PositionStore = WindowPositionStore.Instance };

        // As the RevisionGridControl of FormLog: its default filter, the artificial commits and a multiple selection.
        RevisionGridHost gridHost = new(commands, GetRevisionFilterFactory(showCurrentBranchOnly: false, lastRevisionToDisplayHash: null), showArtificial: true);
        RevisionGridViewModel grid = new(gridHost, new RevisionGridDisplayOptions(AppSettings.RelativeDate, AppSettings.ShowAuthorDate, TranslatedStrings.SearchingFor, AppSettings.RevisionGridQuickSearchTimeout))
        {
            MultiSelect = true,
        };
        ObjectId currentCheckout = commands.Module.GetCurrentCheckout();
        LogViewModel viewModel = new(
            ViewStrings.Load<LogStrings>(),
            new LogHost(commands, window),
            grid,
            new FileViewerHost(commands),
            ViewStrings.Load<FileStatusListStrings>(),
            GetFileStatusTreeOptions(),
            currentCheckout.IsZero ? null : currentCheckout);
        UseFileStatusListMenu(viewModel.Files, commands, window);
        window.DataContext = viewModel;
        accepted = AvaloniaDialogHost.ShowDialog(window, owner?.Handle ?? 0);
        return true;
    }

    private sealed class LogHost(IGitUICommands commands, DialogWindow window) : ILogHost
    {
        public Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken)
            => CalculateDiffsAsync(commands, revisions, headId: default, allowMultiDiff: false, cancellationToken);

        /// <summary>As <c>RevisionGridControl.ViewSelectedRevisions</c>.</summary>
        public void ViewRevisions(IReadOnlyList<GitRevision> revisions) => AvaloniaUi.RunInHostContext(() =>
        {
            NativeWindowOwner owner = new(window);
            TryShowCommitDiff(owner, commands, revisions[0].ObjectId, modeless: true);
        });
    }
}
