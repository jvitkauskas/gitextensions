using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the sparse working copy dialog; ids match <c>FormSparseWorkingCopy.Globalized</c>.</summary>
public sealed class SparseWorkingCopyStrings : ViewStrings
{
    public SparseWorkingCopyStrings()
        : base("Globalized")
    {
        Cancel = Add("Cancel", "Text", "Cancel");
        CannotLoadTheTextOfTheSparseFile = Add("CannotLoadTheTextOfTheSparseFile", "Text", "Cannot load the text of the sparse file.");
        ConfirmDisableGitSparse = Add("ConfirmDisableGitSparse", "Text", "You are about to disable Git Sparse feature for this repository, {0}.\nGit won't be able to restore the working copy to its full content this way.\n\nWould you like to have the filter modified so that it allowed for the full working copy?");
        CouldNotSave = Add("CouldNotSave", "Text", "Could not save the modified settings and rules.");
        DisableForThisRepository = Add("DisableForThisRepository", "Text", "Disable for this repository");
        DisableGitSparse = Add("DisableGitSparse", "Text", "Disable Git Sparse");
        EditsTheContentsOfTheGitInfoSparseCheckoutFile = Add("EditsTheContentsOfTheGitInfoSparseCheckoutFile", "Text", "Edits the contents of the “.git/info/sparse-checkout” file.");
        Enable = Add("Enable", "Text", "&Enable");
        HeaderDetailsText = Add("HeaderDetailsText", "Text", "Need only a small part of a large repository?\nWith sparse checkout, you can skip the rest from being extracted into your working copy.");
        LoadFile = Add("LoadFile", "Text", "Load File");
        RefreshWorkingCopyCheckboxHint = Add("RefreshWorkingCopyCheckboxHint", "Text", "As the sparse working copy rules are changed, it might become outdated.\nRefreshes the working copy against the current set of the rules to restore any missing files and remove any extra files.\n\nnActual command line: {0}");
        RefreshWorkingCopyUsingTheCurrentSettingsAndRules = Add("RefreshWorkingCopyUsingTheCurrentSettingsAndRules", "Text", "Refresh working copy using the current settings and rules");
        Save = Add("Save", "Text", "&Save");
        SaveFile = Add("SaveFile", "Text", "Save File");
        SetsTheGitPropertyToFalseForTheLocalRepository = Add("SetsTheGitPropertyToFalseForTheLocalRepository", "Text", "Sets the Git property “{0}” to False for the local repository.");
        SetsTheGitPropertyToTrueForTheLocalRepository = Add("SetsTheGitPropertyToTrueForTheLocalRepository", "Text", "Sets the Git property “{0}” to True for the local repository.");
        SparseWorkingCopy = Add("SparseWorkingCopy", "Text", "Sparse Working Copy");
        SparseWorkingCopySupportHasNotBeenEnabledForThisRepository = Add("SparseWorkingCopySupportHasNotBeenEnabledForThisRepository", "Text", "Git Sparse feature has not been enabled for this repository.");
        SparseWorkingCopySupportIsEnabled = Add("SparseWorkingCopySupportIsEnabled", "Text", "Git Sparse feature is currently enabled.");
        SpecifyTheRules = Add("SpecifyTheRulesForIncludingOrExcludingFilesAndDirectories", "Text", "Specify the pass-filter rules for files and directories:");
        SpecifyTheRulesLine2 = Add("SpecifyTheRulesForIncludingOrExcludingFilesAndDirectoriesLine2", "Text", "The rules have the same format as the “.gitignore” file, matched items are included. To exclude, prefix a rule with an exclamation mark “!”.\n“#” comments a line. This is only a filter, so it cannot change the structure like pulling up a deep subfolder to the first level.");
        WithSomeRulesStillInTheSparsePassFilter = Add("WithSomeRulesStillInTheSparsePassFilter", "Text", "with some rules still in the sparse pass-filter");
        WithTheSparsePassFilterEmptyOrMissing = Add("WithTheSparsePassFilterEmptyOrMissing", "Text", "with the sparse pass-filter empty or missing");
        YouHaveMadeChanges = Add("YouHaveMadeChangesToSettingsOrRulesWouldYouLikeToSaveThem", "Text", "You have made changes to settings or rules.\nWould you like to save them?");
    }

    public TranslatedText Cancel { get; }

    public TranslatedText CannotLoadTheTextOfTheSparseFile { get; }

    public TranslatedText ConfirmDisableGitSparse { get; }

    public TranslatedText CouldNotSave { get; }

    public TranslatedText DisableForThisRepository { get; }

    public TranslatedText DisableGitSparse { get; }

    public TranslatedText EditsTheContentsOfTheGitInfoSparseCheckoutFile { get; }

    public TranslatedText Enable { get; }

    public TranslatedText HeaderDetailsText { get; }

    public TranslatedText LoadFile { get; }

    public TranslatedText RefreshWorkingCopyCheckboxHint { get; }

    public TranslatedText RefreshWorkingCopyUsingTheCurrentSettingsAndRules { get; }

    public TranslatedText Save { get; }

    public TranslatedText SaveFile { get; }

    public TranslatedText SetsTheGitPropertyToFalseForTheLocalRepository { get; }

    public TranslatedText SetsTheGitPropertyToTrueForTheLocalRepository { get; }

    public TranslatedText SparseWorkingCopy { get; }

    public TranslatedText SparseWorkingCopySupportHasNotBeenEnabledForThisRepository { get; }

    public TranslatedText SparseWorkingCopySupportIsEnabled { get; }

    public TranslatedText SpecifyTheRules { get; }

    public TranslatedText SpecifyTheRulesLine2 { get; }

    public TranslatedText WithSomeRulesStillInTheSparsePassFilter { get; }

    public TranslatedText WithTheSparsePassFilterEmptyOrMissing { get; }

    public TranslatedText YouHaveMadeChanges { get; }
}

/// <summary>Operations of the sparse working copy dialog that need the host (the repository).</summary>
public interface ISparseWorkingCopyHost
{
    /// <summary>Whether <c>core.sparsecheckout</c> is true.</summary>
    bool IsSparseCheckoutEnabled { get; }

    void SetSparseCheckoutEnabled(bool enabled);

    /// <summary>The content of <c>.git/info/sparse-checkout</c>, or <see langword="null"/> if it does not exist; throws if it cannot be read.</summary>
    string? LoadRules();

    /// <summary>Writes <c>.git/info/sparse-checkout</c> (throws if it cannot be written).</summary>
    void SaveRules(string text);

    /// <summary>Re-applies the rules to the working copy (<see cref="SparseWorkingCopyViewModel.RefreshWorkingCopyCommandName"/> in the process dialog).</summary>
    void RefreshWorkingCopy();
}

/// <summary>
///  View model of the sparse working copy dialog: a port of <c>FormSparseWorkingCopy</c> and its
///  <c>FormSparseWorkingCopyViewModel</c> (which uses the WinForms process dialog); keep them in sync.
/// </summary>
public sealed partial class SparseWorkingCopyViewModel : DialogViewModel
{
    public const string RefreshWorkingCopyCommandName = "read-tree -m -u HEAD";

    public const string SettingCoreSparseCheckout = "core.sparsecheckout";

    private readonly ISparseWorkingCopyHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private bool _isSparseCheckoutEnabledAsSaved;

    /// <summary>The rules as on disk, to tell whether they are modified; <see langword="null"/> if the file does not exist.</summary>
    private string? _rulesTextAsOnDisk;

    private bool _closingAfterSave;

    public SparseWorkingCopyViewModel(SparseWorkingCopyStrings strings, ISparseWorkingCopyHost host, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
        IsSparseCheckoutEnabled = _isSparseCheckoutEnabledAsSaved = host.IsSparseCheckoutEnabled;
        try
        {
            _rulesTextAsOnDisk = host.LoadRules();
            Rules.Load(_rulesTextAsOnDisk ?? "");
        }
        catch (Exception ex)
        {
            messageBoxes.ShowError($"{strings.CannotLoadTheTextOfTheSparseFile.Text}\n\n{ex.Message}", $"{strings.SparseWorkingCopy.Text} – {strings.LoadFile.Text}");
        }
    }

    public SparseWorkingCopyStrings Strings { get; }

    /// <summary>The editor of the rules (<c>.git/info/sparse-checkout</c>).</summary>
    public TextEditorViewModel Rules { get; } = new();

    [ObservableProperty]
    public partial bool IsSparseCheckoutEnabled { get; set; }

    /// <summary>Whether to refresh the working copy when saving; on by default, otherwise the index bitmap is not updated.</summary>
    [ObservableProperty]
    public partial bool IsRefreshWorkingCopyOnSave { get; set; } = true;

    public string EnableToolTip => string.Format(Strings.SetsTheGitPropertyToTrueForTheLocalRepository.Text, SettingCoreSparseCheckout);

    public string DisableToolTip => string.Format(Strings.SetsTheGitPropertyToFalseForTheLocalRepository.Text, SettingCoreSparseCheckout);

    public string RefreshWorkingCopyToolTip => string.Format(Strings.RefreshWorkingCopyCheckboxHint.Text, RefreshWorkingCopyCommandName);

    /// <summary>Whether the rules are edited against what is on disk.</summary>
    public bool IsRulesTextChanged => Rules.Text != (_rulesTextAsOnDisk ?? "");

    /// <summary>Whether there is anything to save; the dialog can be cancelled without confirmation otherwise.</summary>
    public bool IsWithUnsavedChanges => IsSparseCheckoutEnabled != _isSparseCheckoutEnabledAsSaved || IsRulesTextChanged;

    [RelayCommand]
    private void Enable() => IsSparseCheckoutEnabled = true;

    [RelayCommand]
    private void Disable() => IsSparseCheckoutEnabled = false;

    /// <summary>As OK in <c>FormSparseWorkingCopy</c>: saves even without changes, to refresh the working copy if chosen.</summary>
    [RelayCommand]
    private void Save()
    {
        TrySaveChanges();

        // As FormSparseWorkingCopy, the dialog closes even if saving failed.
        _closingAfterSave = true;
        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);

    /// <summary>As <c>FormSparseWorkingCopy.BindSaveOnClose</c> when cancelling: unsaved changes are saved, discarded, or keep the dialog open.</summary>
    public override bool CanClose()
    {
        if (_closingAfterSave || !IsWithUnsavedChanges)
        {
            return true;
        }

        switch (_messageBoxes.ConfirmWithCancel(Strings.YouHaveMadeChanges.Text, $"{Strings.SparseWorkingCopy.Text} – {Strings.Cancel.Text}"))
        {
            case true:
                TrySaveChanges();
                return true;
            case false:
                return true;
            default:
                return false;
        }
    }

    private void TrySaveChanges()
    {
        try
        {
            SaveChanges();
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError($"{Strings.CouldNotSave.Text}\n\n{ex.Message}", $"{Strings.SparseWorkingCopy.Text} – {Strings.SaveFile.Text}");
        }
    }

    /// <summary>As <c>FormSparseWorkingCopyViewModel.SaveChanges</c>.</summary>
    private void SaveChanges()
    {
        // Turning off sparse leaves the working copy as it is unless the rules let everything pass.
        SaveChangesTurningOffSparseSpecialCase();

        if (IsSparseCheckoutEnabled != _isSparseCheckoutEnabledAsSaved)
        {
            _host.SetSparseCheckoutEnabled(IsSparseCheckoutEnabled);
            _isSparseCheckoutEnabledAsSaved = IsSparseCheckoutEnabled;
        }

        if (IsRulesTextChanged)
        {
            string newText = Rules.Text;
            _host.SaveRules(newText);
            _rulesTextAsOnDisk = newText;
            Rules.MarkSaved();
        }

        // Run regardless of the modifications (e.g. the rules were edited by hand or the working copy got outdated).
        if (IsRefreshWorkingCopyOnSave)
        {
            _host.RefreshWorkingCopy();
        }
    }

    /// <summary>As <c>FormSparseWorkingCopyViewModel.SaveChangesTurningOffSparseSpecialCase</c>.</summary>
    private void SaveChangesTurningOffSparseSpecialCase()
    {
        if (IsSparseCheckoutEnabled || !_isSparseCheckoutEnabledAsSaved)
        {
            return; // Not turning off
        }

        // The well-known recommendation is to have the single "/*" rule active.
        string[] lines = [.. Rules.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r'))];
        List<string> ruleLines = [.. lines.Select(l => l.Trim()).Where(l => l.Length > 0 && l[0] != '#')];
        if (ruleLines.All(l => l == "/*"))
        {
            return;
        }

        string question = string.Format(
            Strings.ConfirmDisableGitSparse.Text,
            ruleLines.Count == 0 ? Strings.WithTheSparsePassFilterEmptyOrMissing.Text : Strings.WithSomeRulesStillInTheSparsePassFilter.Text);
        if (!_messageBoxes.Confirm(question, Strings.DisableGitSparse.Text))
        {
            return;
        }

        // Comment out all existing nonempty lines, add the single "/*" line to make a total pass filter.
        Rules.Text = string.Join(Environment.NewLine, lines.Select(l => string.IsNullOrWhiteSpace(l) || l[0] == '#' ? l : "#" + l).Prepend("/*"));
    }
}
