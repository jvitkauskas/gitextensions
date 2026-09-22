using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.Git;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>
///  View model of the rename branch dialog (port of <c>GitUI.CommandsDialogs.FormRenameBranch</c>).
/// </summary>
public sealed partial class RenameBranchViewModel : DialogViewModel
{
    private readonly IGitBranchNameNormaliser _branchNameNormaliser;
    private readonly GitBranchNameOptions _branchNameOptions;
    private readonly bool _autoNormalise;
    private readonly Func<string, string, bool> _renameBranch;

    /// <param name="renameBranch">
    ///  Performs the rename (old name, new name) and returns <see langword="true"/> on success.
    ///  Supplied by the host, because running git with progress UI is a host concern.
    /// </param>
    public RenameBranchViewModel(
        RenameBranchStrings strings,
        string oldName,
        IGitBranchNameNormaliser branchNameNormaliser,
        GitBranchNameOptions branchNameOptions,
        bool autoNormalise,
        Func<string, string, bool> renameBranch)
    {
        Strings = strings;
        OldName = oldName;
        NewName = oldName;
        _branchNameNormaliser = branchNameNormaliser;
        _branchNameOptions = branchNameOptions;
        _autoNormalise = autoNormalise;
        _renameBranch = renameBranch;
    }

    public RenameBranchStrings Strings { get; }

    public string OldName { get; }

    [ObservableProperty]
    public partial string NewName { get; set; }

    /// <summary>
    ///  Normalises <see cref="NewName"/> if auto-normalisation is enabled.
    ///  Done when the name box loses focus, and before renaming.
    /// </summary>
    public void NormaliseNewName()
    {
        if (!_autoNormalise || !NewName.Any(PathUtil.IsValidPathChar))
        {
            return;
        }

        NewName = _branchNameNormaliser.Normalise(NewName, _branchNameOptions);
    }

    [RelayCommand]
    private void Rename()
    {
        NormaliseNewName();

        if (NewName == OldName)
        {
            Close(accepted: false);
            return;
        }

        // On failure the dialog stays open so that the user can correct the name (the host has shown the git error).
        if (_renameBranch(OldName, NewName))
        {
            Close(accepted: true);
        }
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}
