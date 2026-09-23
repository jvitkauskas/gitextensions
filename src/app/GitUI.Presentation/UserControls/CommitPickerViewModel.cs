using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;

namespace GitUI.Presentation.UserControls;

/// <summary>Operations of the commit picker that need the host (git, the commit selection dialog).</summary>
public interface ICommitPickerHost
{
    /// <summary>Resolves the revision; <see cref="ObjectId"/>.Zero if it is not valid.</summary>
    ObjectId RevParse(string? revision);

    /// <summary>
    ///  Computes (in the background) how the commit relates to the current checkout, e.g. "3 commits behind", and reports it
    ///  on the UI thread; reports nothing if there is no current checkout.
    /// </summary>
    void RequestCommitCount(ObjectId selected, Action<string> report);

    /// <summary>Lets the user choose a commit; returns its hash, or <see langword="null"/> if cancelled.</summary>
    string? ChooseCommit(ObjectId? current);
}

/// <summary>
///  View model of the small commit picker: a commit hash box with a button to choose the commit
///  (port of <c>CommitPickerSmallControl</c>, which is not translated).
/// </summary>
public sealed partial class CommitPickerViewModel : ObservableObject
{
    private readonly ICommitPickerHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _errorCaption;

    public CommitPickerViewModel(ICommitPickerHost host, IMessageBoxService messageBoxes, string errorCaption)
    {
        _host = host;
        _messageBoxes = messageBoxes;
        _errorCaption = errorCaption;
    }

    /// <summary>The entered commit hash.</summary>
    [ObservableProperty]
    public partial string Text { get; set; } = "";

    /// <summary>How the commit relates to the current checkout.</summary>
    [ObservableProperty]
    public partial string CommitCountText { get; private set; } = "";

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    public ObjectId SelectedObjectId { get; private set; }

    /// <summary>Raised when <see cref="SelectedObjectId"/> is set (to a valid commit or cleared).</summary>
    public event EventHandler? SelectedObjectIdChanged;

    /// <summary>
    ///  Selects the commit; an invalid hash is reported and discarded (as <c>CommitPickerSmallControl.SetSelectedCommitHash</c>).
    /// </summary>
    public void SetSelectedCommitHash(string? commitHash)
    {
        ObjectId oldObjectId = SelectedObjectId;
        SelectedObjectId = _host.RevParse(commitHash);

        if (SelectedObjectId.IsZero && !string.IsNullOrWhiteSpace(commitHash))
        {
            SelectedObjectId = oldObjectId;
            _messageBoxes.ShowError("The given commit hash is not valid for this repository and was therefore discarded.", _errorCaption);
        }
        else
        {
            SelectedObjectIdChanged?.Invoke(this, EventArgs.Empty);
        }

        bool isArtificialCommitForEmptyRepo = commitHash == "HEAD";
        CommitCountText = "";

        if (SelectedObjectId.IsZero || isArtificialCommitForEmptyRepo)
        {
            Text = "";
            return;
        }

        ObjectId selectedObjectId = SelectedObjectId;
        Text = selectedObjectId.ToShortString();
        _host.RequestCommitCount(selectedObjectId, text =>
        {
            // Ignore results for a commit that is no longer selected.
            if (SelectedObjectId == selectedObjectId)
            {
                CommitCountText = text;
            }
        });
    }

    /// <summary>Applies the entered hash; done when the hash box loses focus.</summary>
    public void CommitText() => SetSelectedCommitHash(Text.Trim());

    [RelayCommand]
    private void ChooseCommit()
    {
        string? commitHash = _host.ChooseCommit(SelectedObjectId.IsZero ? null : SelectedObjectId);
        if (commitHash is not null)
        {
            SetSelectedCommitHash(commitHash);
        }
    }
}
