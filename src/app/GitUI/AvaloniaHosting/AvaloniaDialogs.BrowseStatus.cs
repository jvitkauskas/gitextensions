using System.Drawing.Imaging;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs;
using GitUI.UserControls;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>The status of the working directory in the commit button of the Avalonia main window (<c>InitCountArtificial</c>).</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseStatusHost
    {
        private GitStatusMonitor? _gitStatusMonitor;
        private EventHandler<BrowseWorkingDirectoryStatus>? _workingDirectoryStatusChanged;

        public event EventHandler<BrowseWorkingDirectoryStatus>? WorkingDirectoryStatusChanged
        {
            add
            {
                _workingDirectoryStatusChanged += value;
                StartGitStatusMonitor();
            }

            remove => _workingDirectoryStatusChanged -= value;
        }

        /// <summary>As <c>NeedsGitStatusMonitor</c>.</summary>
        private static bool NeedsGitStatusMonitor()
            => AppSettings.ShowGitStatusInBrowseToolbar || (AppSettings.ShowGitStatusForArtificialCommits && AppSettings.RevisionGraphShowArtificialCommits);

        private void StartGitStatusMonitor()
        {
            if (_gitStatusMonitor is not null)
            {
                return;
            }

            _gitStatusMonitor = new GitStatusMonitor(new CommandsSource(_commands), () => _window.WindowState == global::Avalonia.Controls.WindowState.Minimized);
            _gitStatusMonitor.GitStatusMonitorStateChanged += (_, e) =>
            {
                if (e.State == GitStatusMonitorState.Stopped)
                {
                    // Fall back to the button without the status.
                    ReportStatus(status: null, showCount: false);
                }
            };
            _gitStatusMonitor.GitWorkingDirectoryStatusChanged += (_, e) => ReportStatus(e?.ItemStatuses, AppSettings.ShowGitStatusInBrowseToolbar);
            _gitStatusMonitor.Active = NeedsGitStatusMonitor() && Module.IsValidGitWorkingDir();
            ReportStatus(status: null, AppSettings.ShowGitStatusInBrowseToolbar);
        }

        private void StopGitStatusMonitor()
        {
            _gitStatusMonitor?.Dispose();
            _gitStatusMonitor = null;
        }

        // As UpdateCommitButtonAndGetBrush: the image of the state and the number of changes, or the clean image without them.
        private void ReportStatus(IReadOnlyList<GitItemStatus>? status, bool showCount)
        {
            RepoStateVisualiser visualiser = new();
            Image image = showCount ? visualiser.Invoke(status).image : visualiser.Invoke([]).image;
            using MemoryStream stream = new();
            image.Save(stream, ImageFormat.Png);
            _workingDirectoryStatusChanged?.Invoke(this, new BrowseWorkingDirectoryStatus(showCount ? status?.Count : null, stream.ToArray()));
        }

        /// <summary>The commands of this repository, which don't change (another repository gets another session).</summary>
        private sealed class CommandsSource(IGitUICommands commands) : IGitUICommandsSource
        {
            public event EventHandler<GitUICommandsChangedEventArgs>? UICommandsChanged
            {
                add { }
                remove { }
            }

            public IGitUICommands UICommands => commands;
        }
    }
}
