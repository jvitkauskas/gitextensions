using CommunityToolkit.Mvvm.ComponentModel;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls;

/// <summary>Strings of the local/remote branch selector; ids match <c>GitUI.UserControls.BranchSelector</c>.</summary>
public sealed class LocalRemoteBranchSelectorStrings : ViewStrings
{
    public LocalRemoteBranchSelectorStrings()
        : base("BranchSelector")
    {
        LocalBranch = Add("LocalBranch", "Text", "Local branch");
        RemoteBranch = Add("Remotebranch", "Text", "Remote branch");
        SelectBranch = Add("label1", "Text", "Select branch");
    }

    public TranslatedText LocalBranch { get; }

    public TranslatedText RemoteBranch { get; }

    public TranslatedText SelectBranch { get; }
}

/// <summary>Operations of the local/remote branch selector that need the host (git).</summary>
public interface ILocalRemoteBranchSelectorHost
{
    /// <summary>The names of the local or remote branches.</summary>
    IReadOnlyList<string> GetBranches(bool remote);

    /// <summary>
    ///  Computes (in the background) how the branch relates to the commit to compare with, e.g. "3 commits ahead", and
    ///  reports it on the UI thread.
    /// </summary>
    void RequestCommitCount(string branch, Action<string> report);
}

/// <summary>
///  View model of a branch selector choosing between local and remote branches, showing how the selected branch
///  relates to a commit (port of <c>GitUI.UserControls.BranchSelector</c>).
/// </summary>
public sealed partial class LocalRemoteBranchSelectorViewModel : ObservableObject
{
    private readonly ILocalRemoteBranchSelectorHost _host;

    public LocalRemoteBranchSelectorViewModel(LocalRemoteBranchSelectorStrings strings, bool remote, ILocalRemoteBranchSelectorHost host)
    {
        Strings = strings;
        _host = host;
        IsRemote = remote;
        Branches = host.GetBranches(remote);
    }

    public LocalRemoteBranchSelectorStrings Strings { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocal))]
    public partial bool IsRemote { get; set; }

    public bool IsLocal
    {
        get => !IsRemote;
        set => IsRemote = !value;
    }

    [ObservableProperty]
    public partial IReadOnlyList<string> Branches { get; private set; }

    /// <summary>The entered or selected branch name.</summary>
    [ObservableProperty]
    public partial string BranchName { get; set; } = "";

    /// <summary>How the selected branch relates to the commit to compare with.</summary>
    [ObservableProperty]
    public partial string CommitCountText { get; private set; } = "";

    partial void OnIsRemoteChanged(bool value)
    {
        if (_host is null)
        {
            return;
        }

        Branches = _host.GetBranches(value);
        BranchName = "";
    }

    partial void OnBranchNameChanged(string value)
    {
        CommitCountText = "";
        if (string.IsNullOrWhiteSpace(value) || !Branches.Contains(value))
        {
            return;
        }

        _host.RequestCommitCount(value, text =>
        {
            // Ignore results for a branch that is no longer selected.
            if (BranchName == value)
            {
                CommitCountText = text;
            }
        });
    }
}
