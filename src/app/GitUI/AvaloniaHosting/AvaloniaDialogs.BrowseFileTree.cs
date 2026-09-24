using GitCommands;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>The file tree tab of the Avalonia main window (<c>TreeTabPage</c>).</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseFileTreeHost
    {
        /// <summary>As <c>FileStatusList</c> in file tree mode: the "grep" of all the files (<c>SetGrep("", fileTreeMode: true)</c>).</summary>
        public async Task<FileStatusGroup> GetTreeFilesAsync(GitRevision revision, CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();

            FileStatusDiffCalculator calculator = new(() => _commands.Module);
            calculator.SetDiff([revision], headId: default, allowMultiDiff: false);
            calculator.SetGrep("", fileTreeMode: true);
            FileStatusWithDescription files = calculator.Calculate(prevList: [], refreshDiff: false, refreshGrep: true, cancellationToken).Single();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return new FileStatusGroup(files.FirstRev, files.SecondRev, files.Summary, files.Statuses, IconName: FileStatusIcons.GitGrepIconName);
        }
    }
}
