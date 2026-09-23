using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the commit dialog settings; ids match <c>CommitDialogSettingsPage</c>.</summary>
public sealed class CommitDialogSettingsPageStrings : ViewStrings
{
    public CommitDialogSettingsPageStrings()
        : base("CommitDialogSettingsPage")
    {
        Title = Add("$this", "Text", "Commit dialog");
        Behaviour = Add("groupBoxBehaviour", "Text", "Behaviour");
        ShowErrorsWhenStagingFiles = Add("chkShowErrorsWhenStagingFiles", "Text", "Show errors when staging files");
        EnsureCommitMessageSecondLineEmpty = Add("chkEnsureCommitMessageSecondLineEmpty", "Text", "Ensure the second line of commit message is empty");
        WriteCommitMessageInCommitWindow = Add("chkWriteCommitMessageInCommitWindow", "Text", "Compose commit messages in Commit dialog\n(otherwise the message will be requested during commit)");
        NumberOfPreviousMessages = Add("lblCommitDialogNumberOfPreviousMessages", "Text", "Number of previous messages in commit dialog");
        Autocomplete = Add("chkAutocomplete", "Text", "Provide auto-completion in commit dialog");
        RememberAmendCommitState = Add("cbRememberAmendCommitState", "Text", "Remember 'Amend commit' checkbox on commit form close");
        AdditionalButtons = Add("grpAdditionalButtons", "Text", "Show additional buttons in commit button area");
        ShowCommitAndPush = Add("chkShowCommitAndPush", "Text", "Commit && Push");
        ShowResetWorkTreeChanges = Add("chkShowResetWorkTreeChanges", "Text", "Reset Unstaged Changes");
        ShowResetAllChanges = Add("chkShowResetAllChanges", "Text", "Reset All Changes");
    }

    public TranslatedText Title { get; }

    public TranslatedText Behaviour { get; }

    public TranslatedText ShowErrorsWhenStagingFiles { get; }

    public TranslatedText EnsureCommitMessageSecondLineEmpty { get; }

    public TranslatedText WriteCommitMessageInCommitWindow { get; }

    public TranslatedText NumberOfPreviousMessages { get; }

    public TranslatedText Autocomplete { get; }

    public TranslatedText RememberAmendCommitState { get; }

    public TranslatedText AdditionalButtons { get; }

    public TranslatedText ShowCommitAndPush { get; }

    public TranslatedText ShowResetWorkTreeChanges { get; }

    public TranslatedText ShowResetAllChanges { get; }
}

/// <summary>Port of <c>CommitDialogSettingsPage</c> (global settings).</summary>
public sealed partial class CommitDialogSettingsPageViewModel(CommitDialogSettingsPageStrings strings) : SettingsPageViewModel
{
    public CommitDialogSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "CommitDialogSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    [ObservableProperty]
    public partial bool ShowErrorsWhenStagingFiles { get; set; }

    [ObservableProperty]
    public partial bool EnsureCommitMessageSecondLineEmpty { get; set; }

    [ObservableProperty]
    public partial bool WriteCommitMessageInCommitWindow { get; set; }

    [ObservableProperty]
    public partial decimal NumberOfPreviousMessages { get; set; }

    [ObservableProperty]
    public partial bool Autocomplete { get; set; }

    [ObservableProperty]
    public partial bool RememberAmendCommitState { get; set; }

    [ObservableProperty]
    public partial bool ShowCommitAndPush { get; set; }

    [ObservableProperty]
    public partial bool ShowResetWorkTreeChanges { get; set; }

    [ObservableProperty]
    public partial bool ShowResetAllChanges { get; set; }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        ShowErrorsWhenStagingFiles = AppSettings.ShowErrorsWhenStagingFiles;
        EnsureCommitMessageSecondLineEmpty = AppSettings.EnsureCommitMessageSecondLineEmpty;
        WriteCommitMessageInCommitWindow = AppSettings.UseFormCommitMessage;
        NumberOfPreviousMessages = AppSettings.CommitDialogNumberOfPreviousMessages;
        ShowCommitAndPush = AppSettings.ShowCommitAndPush;
        ShowResetWorkTreeChanges = AppSettings.ShowResetWorkTreeChanges;
        ShowResetAllChanges = AppSettings.ShowResetAllChanges;
        Autocomplete = AppSettings.ProvideAutocompletion;
        RememberAmendCommitState = AppSettings.RememberAmendCommitState;
        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.ShowErrorsWhenStagingFiles = ShowErrorsWhenStagingFiles;
        AppSettings.EnsureCommitMessageSecondLineEmpty = EnsureCommitMessageSecondLineEmpty;
        AppSettings.UseFormCommitMessage = WriteCommitMessageInCommitWindow;
        AppSettings.CommitDialogNumberOfPreviousMessages = (int)NumberOfPreviousMessages;
        AppSettings.ShowCommitAndPush = ShowCommitAndPush;
        AppSettings.ShowResetWorkTreeChanges = ShowResetWorkTreeChanges;
        AppSettings.ShowResetAllChanges = ShowResetAllChanges;
        AppSettings.ProvideAutocompletion = Autocomplete;
        AppSettings.RememberAmendCommitState = RememberAmendCommitState;
        base.PageToSettings(settings);
    }
}
