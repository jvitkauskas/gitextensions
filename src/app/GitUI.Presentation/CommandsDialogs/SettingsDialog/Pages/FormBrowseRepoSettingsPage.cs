using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the settings of the browse repository window; ids match <c>FormBrowseRepoSettingsPage</c>.</summary>
public sealed class FormBrowseRepoSettingsPageStrings : ViewStrings
{
    public FormBrowseRepoSettingsPageStrings()
        : base("FormBrowseRepoSettingsPage")
    {
        Title = Add("$this", "Text", "Browse repository window");
        OutputHistoryTooltip = Add("_outputHistoryTooltip", "Text", "The output displayed in the process dialog and some trace output is retained and shown in the output history.\n\n- With this set, the output history is displayed in a tab in the lower pane of the Browse Repository window.\n- With this unset, the output history is displayed in a panel docked to the lower left corner of the Browse Repository window.\n\nFocus the output history and (when displayed as panel) toggle its visibility using the hotkey {0}.");
        General = Add("groupBox1", "Text", "General");
        DefaultShell = Add("lblDefaultShell", "Text", "Default shell");
        UseBrowseForFileHistory = Add("chkUseBrowseForFileHistory", "Text", "Show file history in the main window");
        UseBrowseForFileHistoryTooltip = Add("chkUseBrowseForFileHistory", "ToolTipText", "View help");
        UseDiffViewerForBlame = Add("chkUseDiffViewerForBlame", "Text", "Show blame in diff viewer");
        UseDiffViewerForBlameTooltip = Add("chkUseDiffViewerForBlame", "ToolTipText", "View help");
        ShowFindInCommitFilesGitGrep = Add("chkShowFindInCommitFilesGitGrep", "Text", "Show 'Find in commit files using git-grep'");
        ShowRevisionGridTooltip = Add("chkShowRevisionGridTooltip", "Text", "Show revision tooltips (restart required)");
        Tabs = Add("gbTabs", "Text", "Tabs (restart required)");
        ShowConsoleTab = Add("chkShowConsoleTab", "Text", "Show the Console tab");
        ShowConsoleTabTooltip = Add("chkShowConsoleTab", "ToolTipText", "View help");
        ShowGpgInformation = Add("chkShowGpgInformation", "Text", "Show GPG information");
        ShowGpgInformationTooltip = Add("chkShowGpgInformation", "ToolTipText", "View help");
        ShowOutputHistoryAsTab = Add("chkShowOutputHistoryAsTab", "Text", "Show output history as tab (otherwise as panel)");
        OutputHistoryDepth = Add("lblOutputHistoryDepth", "Text", "Output history depth (0 to disable):");
    }

    public TranslatedText Title { get; }

    public TranslatedText OutputHistoryTooltip { get; }

    public TranslatedText General { get; }

    public TranslatedText DefaultShell { get; }

    public TranslatedText UseBrowseForFileHistory { get; }

    public TranslatedText UseBrowseForFileHistoryTooltip { get; }

    public TranslatedText UseDiffViewerForBlame { get; }

    public TranslatedText UseDiffViewerForBlameTooltip { get; }

    public TranslatedText ShowFindInCommitFilesGitGrep { get; }

    public TranslatedText ShowRevisionGridTooltip { get; }

    public TranslatedText Tabs { get; }

    public TranslatedText ShowConsoleTab { get; }

    public TranslatedText ShowConsoleTabTooltip { get; }

    public TranslatedText ShowGpgInformation { get; }

    public TranslatedText ShowGpgInformationTooltip { get; }

    public TranslatedText ShowOutputHistoryAsTab { get; }

    public TranslatedText OutputHistoryDepth { get; }
}

/// <summary>A shell of <c>cboTerminal</c> (<c>IShellDescriptor</c>).</summary>
public sealed record ShellChoice(string Name, bool HasExecutable)
{
    public override string ToString() => Name;
}

/// <summary>What the settings of the browse repository window need from the application.</summary>
public interface IFormBrowseRepoSettingsPageHost : ISettingsPageServices
{
    /// <summary>As <c>IShellProvider.GetShells</c>.</summary>
    IReadOnlyList<ShellChoice> GetShells();

    /// <summary>The hotkey of <c>HotkeyCommands.Browse.FocusOutputHistoryAndToggleIfPanel</c>, as shown.</summary>
    string FocusOutputHistoryHotkey { get; }

    /// <summary>As <c>MessageBoxes.ShellNotFound</c>.</summary>
    void ShowShellNotFound();
}

/// <summary>Port of <c>FormBrowseRepoSettingsPage</c> (global settings).</summary>
public sealed partial class FormBrowseRepoSettingsPageViewModel : SettingsPageWithServicesViewModel
{
    private readonly IFormBrowseRepoSettingsPageHost _host;
    private bool _revertingShell;

    public FormBrowseRepoSettingsPageViewModel(FormBrowseRepoSettingsPageStrings strings, IFormBrowseRepoSettingsPageHost host)
        : base(host)
    {
        Strings = strings;
        _host = host;
        OutputHistoryTooltip = string.Format(strings.OutputHistoryTooltip.Text, host.FocusOutputHistoryHotkey);
    }

    public FormBrowseRepoSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "FormBrowseRepoSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>The tooltip of <c>chkShowOutputHistoryAsTab</c>, with the hotkey.</summary>
    public string OutputHistoryTooltip { get; }

    /// <summary>The items of <c>cboTerminal</c>.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<ShellChoice> Shells { get; private set; } = [];

    [ObservableProperty]
    public partial ShellChoice? Shell { get; set; }

    [ObservableProperty]
    public partial bool UseBrowseForFileHistory { get; set; }

    [ObservableProperty]
    public partial bool UseDiffViewerForBlame { get; set; }

    [ObservableProperty]
    public partial bool ShowFindInCommitFilesGitGrep { get; set; }

    [ObservableProperty]
    public partial bool ShowRevisionGridTooltip { get; set; }

    [ObservableProperty]
    public partial bool ShowConsoleTab { get; set; }

    [ObservableProperty]
    public partial bool ShowGpgInformation { get; set; }

    [ObservableProperty]
    public partial bool ShowOutputHistoryAsTab { get; set; }

    [ObservableProperty]
    public partial decimal OutputHistoryDepth { get; set; }

    // As cboTerminal_SelectionChangeCommitted: a shell without executable cannot be chosen.
    partial void OnShellChanged(ShellChoice? oldValue, ShellChoice? newValue)
    {
        if (_revertingShell || IsLoadingSettings || newValue is null || newValue.HasExecutable)
        {
            return;
        }

        _host.ShowShellNotFound();
        _revertingShell = true;
        try
        {
            Shell = oldValue;
        }
        finally
        {
            _revertingShell = false;
        }
    }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        ShowConsoleTab = AppSettings.ShowConEmuTab.Value;
        UseBrowseForFileHistory = AppSettings.UseBrowseForFileHistory.Value;
        UseDiffViewerForBlame = AppSettings.UseDiffViewerForBlame.Value;
        ShowGpgInformation = AppSettings.ShowGpgInformation.Value;
        ShowFindInCommitFilesGitGrep = AppSettings.ShowFindInCommitFilesGitGrep.Value;
        ShowRevisionGridTooltip = AppSettings.ShowRevisionGridTooltips.Value;
        ShowOutputHistoryAsTab = AppSettings.ShowOutputHistoryAsTab.Value;
        OutputHistoryDepth = Math.Clamp(AppSettings.OutputHistoryDepth.Value, 0, 1000);

        Shells = _host.GetShells();
        Shell = Shells.FirstOrDefault(shell => string.Equals(shell.Name, AppSettings.ConEmuTerminal.Value, StringComparison.InvariantCultureIgnoreCase));

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.ShowConEmuTab.Value = ShowConsoleTab;
        AppSettings.UseBrowseForFileHistory.Value = UseBrowseForFileHistory;
        AppSettings.UseDiffViewerForBlame.Value = UseDiffViewerForBlame;
        AppSettings.ShowGpgInformation.Value = ShowGpgInformation;
        AppSettings.ShowFindInCommitFilesGitGrep.Value = ShowFindInCommitFilesGitGrep;
        AppSettings.ShowRevisionGridTooltips.Value = ShowRevisionGridTooltip;

        int outputHistoryDepth = (int)OutputHistoryDepth;
        bool changed = AppSettings.ShowOutputHistoryAsTab.Value != ShowOutputHistoryAsTab || AppSettings.OutputHistoryDepth.Value != outputHistoryDepth;
        if (changed)
        {
            AppSettings.ShowOutputHistoryAsTab.Value = ShowOutputHistoryAsTab;
            AppSettings.OutputHistoryDepth.Value = outputHistoryDepth;
            AppSettings.OutputHistoryPanelVisible.Value = !ShowOutputHistoryAsTab && outputHistoryDepth > 0;
        }

        if (Shell is { } shell)
        {
            AppSettings.ConEmuTerminal.Value = shell.Name.ToLowerInvariant();
        }

        base.PageToSettings(settings);
    }
}
