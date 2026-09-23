using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls;

/// <summary>Strings of the branch selector; ids match <c>BranchComboBox</c>.</summary>
public sealed class BranchSelectorStrings : ViewStrings
{
    public BranchSelectorStrings()
        : base("BranchComboBox")
    {
        BranchNotSelectable = Add("_branchCheckoutError", "Text", "Branch '{0}' is not selectable, this branch has been removed from the selection.");
    }

    public TranslatedText BranchNotSelectable { get; }
}

/// <summary>
///  View model of a branch selector: an editable list of branches, where several branches can be entered separated by
///  spaces or picked in a dialog (port of <c>BranchComboBox</c>).
/// </summary>
public sealed partial class BranchSelectorViewModel : ObservableObject
{
    private readonly Func<IReadOnlyList<string>, IReadOnlyList<string>?> _selectMultiple;
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _errorCaption;

    /// <param name="branches">The names of the branches that can be selected.</param>
    /// <param name="selectMultiple">
    ///  Lets the user check several branches, starting with the given ones; returns <see langword="null"/> if cancelled.
    /// </param>
    public BranchSelectorViewModel(
        BranchSelectorStrings strings,
        IReadOnlyList<string> branches,
        Func<IReadOnlyList<string>, IReadOnlyList<string>?> selectMultiple,
        IMessageBoxService messageBoxes,
        string errorCaption)
    {
        Strings = strings;
        Branches = branches;
        _selectMultiple = selectMultiple;
        _messageBoxes = messageBoxes;
        _errorCaption = errorCaption;
    }

    public BranchSelectorStrings Strings { get; }

    public IReadOnlyList<string> Branches { get; }

    /// <summary>The selected branches, separated by spaces.</summary>
    [ObservableProperty]
    public partial string Text { get; set; } = "";

    /// <summary>Raised when <see cref="Text"/> changes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    ///  The entered branches that exist; the others are reported (if <paramref name="reportInvalid"/>) and skipped,
    ///  as <c>BranchComboBox.GetSelectedBranches</c>.
    /// </summary>
    public IReadOnlyList<string> GetSelectedBranches(bool reportInvalid = false)
    {
        List<string> selected = [];
        foreach (string branch in Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Branches.Contains(branch))
            {
                selected.Add(branch);
            }
            else if (reportInvalid)
            {
                _messageBoxes.ShowError(string.Format(Strings.BranchNotSelectable.Text, branch), _errorCaption);
            }
        }

        return selected;
    }

    [RelayCommand]
    private void SelectMultiple()
    {
        IReadOnlyList<string>? selected = _selectMultiple(GetSelectedBranches(reportInvalid: true));
        if (selected is not null)
        {
            Text = string.Join(" ", selected);
        }
    }

    partial void OnTextChanged(string value) => SelectionChanged?.Invoke(this, EventArgs.Empty);
}
