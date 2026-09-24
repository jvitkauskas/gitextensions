using CommunityToolkit.Mvvm.ComponentModel;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>The status of the working directory shown by the commit button (as <c>UpdateCommitButtonAndGetBrush</c>).</summary>
/// <param name="ChangeCount">The number of changed files, or <see langword="null"/> when unknown or not shown.</param>
/// <param name="Icon">The image of the state of the repository (<c>RepoStateVisualiser</c>): an asset name or PNG data.</param>
public sealed record BrowseWorkingDirectoryStatus(int? ChangeCount, object? Icon);

/// <summary>What the commit button needs from the application (<c>GitStatusMonitor</c>).</summary>
public interface IBrowseStatusHost
{
    /// <summary>Raised on the UI thread when the status of the working directory changes (<c>GitWorkingDirectoryStatusChanged</c>).</summary>
    event EventHandler<BrowseWorkingDirectoryStatus>? WorkingDirectoryStatusChanged;
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
            };
        }
    }
}
