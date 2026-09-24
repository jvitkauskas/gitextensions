using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>The status of the working directory shown by the commit button (as <c>UpdateCommitButtonAndGetBrush</c>).</summary>
/// <param name="ChangeCount">The number of changed files, or <see langword="null"/> when unknown or not shown.</param>
/// <param name="Icon">The image of the state of the repository (<c>RepoStateVisualiser</c>): an asset name or PNG data.</param>
/// <param name="ArtificialCommitStatus">The changes for the artificial commits of the grid, or <see langword="null"/> when unknown or not shown.</param>
public sealed record BrowseWorkingDirectoryStatus(int? ChangeCount, object? Icon, IReadOnlyList<GitItemStatus>? ArtificialCommitStatus = null);

/// <summary>What the commit button needs from the application (<c>GitStatusMonitor</c>).</summary>
public interface IBrowseStatusHost
{
    /// <summary>Raised on the UI thread when the status of the working directory changes (<c>GitWorkingDirectoryStatusChanged</c>).</summary>
    event EventHandler<BrowseWorkingDirectoryStatus>? WorkingDirectoryStatusChanged;

    /// <summary>As <c>RefreshGitStatusMonitor</c>: the status is read again at once (e.g. after staging from the diff tab).</summary>
    void RequestStatusRefresh();
}

/// <summary>The commit button with the status of the working directory (<c>toolStripButtonCommit</c>).</summary>
public sealed partial class BrowseViewModel
{
    /// <summary>"Commit", with the number of changes if the status is shown (<c>ShowGitStatusInBrowseToolbar</c>).</summary>
    [ObservableProperty]
    public partial string CommitButtonText { get; private set; } = "";

    /// <summary>The image of the commit button, the state of the working directory.</summary>
    [ObservableProperty]
    public partial object? CommitButtonIcon { get; private set; } = "RepoStateClean";

    private void InitializeWorkingDirectoryStatus()
    {
        CommitButtonText = Strings.CommitButton.Text;
        if (_host is IBrowseStatusHost statusHost)
        {
            statusHost.WorkingDirectoryStatusChanged += (_, status) =>
            {
                CommitButtonText = status.ChangeCount is int count ? $"{Strings.CommitButton.Text} ({count})" : Strings.CommitButton.Text;
                CommitButtonIcon = status.Icon ?? "RepoStateClean";
                Grid.UpdateArtificialCommitCount(status.ArtificialCommitStatus);
            };
        }
    }
}
