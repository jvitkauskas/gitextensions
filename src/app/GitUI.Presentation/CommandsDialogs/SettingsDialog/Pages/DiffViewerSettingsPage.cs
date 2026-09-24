using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the diff viewer settings; ids match <c>DiffViewerSettingsPage</c> and <c>TranslatedStrings</c>.</summary>
public sealed class DiffViewerSettingsPageStrings : ViewStrings
{
    public DiffViewerSettingsPageStrings()
        : base("DiffViewerSettingsPage")
    {
        Title = Add("$this", "Text", "Diff viewer");
        General = Add("gbGeneral", "Text", "General");
        SaveCurrentViewSettingsAsDefault = Add("btnSaveCurrentViewSettingsAsDefault", "Text", "Save current view settings as default");
        SaveCurrentViewSettingsAsDefaultTooltip = Add("btnSaveCurrentViewSettingsAsDefault", "toolTip", "Saves all current view settings as the default for future sessions.\nNote: The checkboxes 'Remember the \"xyz\" preference' only affect the running instance\nas long as the default has not been saved. These preference values are held in memory\nand must be explicitly saved to become persistent defaults.");
        RememberIgnoreWhiteSpacePreference = Add("chkRememberIgnoreWhiteSpacePreference", "Text", "Remember the 'Ignore whitespaces' preference");
        RememberShowNonPrintingCharsPreference = Add("chkRememberShowNonPrintingCharsPreference", "Text", "Remember the 'Show nonprinting characters' preference");
        RememberShowEntireFilePreference = Add("chkRememberShowEntireFilePreference", "Text", "Remember the 'Show entire file' preference");
        RememberDiffAppearancePreference = Add("chkRememberDiffAppearancePreference", "Text", "Remember the 'Diff appearance' preference");
        RememberDiffAppearancePreferenceTooltip = Add("chkRememberDiffAppearancePreference", "ToolTipText", "Diff appearance: patch (default), Git word-diff or Difftastic.");
        RememberNumberOfContextLines = Add("chkRememberNumberOfContextLines", "Text", "Remember the 'Number of context lines' preference");
        RememberShowSyntaxHighlightingInDiff = Add("chkRememberShowSyntaxHighlightingInDiff", "Text", "Remember the 'Show syntax highlighting' preference");
        OmitUninterestingDiff = Add("chkOmitUninterestingDiff", "Text", "Omit uninteresting changes from combined diff");
        ContScrollToNextFileOnlyWithAlt = Add("_contScrollToNextFileOnlyWithAlt", "Text", "Enable automatic continuous scroll (without ALT button)", category: "TranslatedStrings");
        OpenSubmoduleDiffInSeparateWindow = Add("chkOpenSubmoduleDiffInSeparateWindow", "Text", "Open Submodule Diff in separate window");
        ShowDiffForAllParents = Add("_showDiffForAllParentsText", "Text", "Show file differences for all parents in browse dialog", category: "TranslatedStrings");
        ShowDiffForAllParentsTooltip = Add("_showDiffForAllParentsTooltip", "Text", """
            Show all differences between the selected commits, not limiting to only one difference.

            - For a single selected commit, show the difference with its parent commit.
            - For a single selected merge commit, show the difference with all parents.
            - For two selected commits with a common ancestor (BASE), show the difference
            between the commits as well as the difference from BASE to the selected commits.
            See documentation for more details about icons and range diffs.
            - For multiple selected commits (up to four), show the difference for
            all the first selected with the last selected commit.
            - For more than four selected commits, show the difference from the first to
            the last selected commit.
            """, category: "TranslatedStrings");
        ShowAllCustomDiffTools = Add("chkShowAllCustomDiffTools", "Text", "Show all available difftools");
        ShowAllCustomDiffToolsTooltip = Add("chkShowAllCustomDiffTools", "ToolTipText", "Show all configured difftools in a dropdown.\nThe primary difftool can still be selected by clicking the main menu entry.");
        VerticalRulerPosition = Add("label1", "Text", "Vertical ruler position [chars]");
        DiffColoring = Add("gbDiffColoring", "Text", "Diff coloring");
        UseGitColoring = Add("chkUseGitColoring", "Text", "Git coloring");
        UseGitColoringTooltip = Add("chkUseGitColoring", "ToolTipText", "Use Git coloring engine to show moved code etc.\n");
        UseGEThemeGitColoring = Add("chkUseGEThemeGitColoring", "Text", "Reverse background color");
        UseGEThemeGitColoringTooltip = Add("chkUseGEThemeGitColoring", "ToolTipText", "Color the background at changes (invert colors).");
    }

    public TranslatedText Title { get; }

    public TranslatedText General { get; }

    public TranslatedText SaveCurrentViewSettingsAsDefault { get; }

    public TranslatedText SaveCurrentViewSettingsAsDefaultTooltip { get; }

    public TranslatedText RememberIgnoreWhiteSpacePreference { get; }

    public TranslatedText RememberShowNonPrintingCharsPreference { get; }

    public TranslatedText RememberShowEntireFilePreference { get; }

    public TranslatedText RememberDiffAppearancePreference { get; }

    public TranslatedText RememberDiffAppearancePreferenceTooltip { get; }

    public TranslatedText RememberNumberOfContextLines { get; }

    public TranslatedText RememberShowSyntaxHighlightingInDiff { get; }

    public TranslatedText OmitUninterestingDiff { get; }

    public TranslatedText ContScrollToNextFileOnlyWithAlt { get; }

    public TranslatedText OpenSubmoduleDiffInSeparateWindow { get; }

    public TranslatedText ShowDiffForAllParents { get; }

    public TranslatedText ShowDiffForAllParentsTooltip { get; }

    public TranslatedText ShowAllCustomDiffTools { get; }

    public TranslatedText ShowAllCustomDiffToolsTooltip { get; }

    public TranslatedText VerticalRulerPosition { get; }

    public TranslatedText DiffColoring { get; }

    public TranslatedText UseGitColoring { get; }

    public TranslatedText UseGitColoringTooltip { get; }

    public TranslatedText UseGEThemeGitColoring { get; }

    public TranslatedText UseGEThemeGitColoringTooltip { get; }
}

/// <summary>What the diff viewer settings need from the application.</summary>
public interface IDiffViewerSettingsPageHost : ISettingsPageServices
{
    /// <summary>As <c>RevisionGridMenuCommands.SaveCurrentViewSettingsAsDefault</c>.</summary>
    void SaveCurrentViewSettingsAsDefault();
}

/// <summary>Port of <c>DiffViewerSettingsPage</c> (global settings).</summary>
public sealed partial class DiffViewerSettingsPageViewModel(DiffViewerSettingsPageStrings strings, IDiffViewerSettingsPageHost host) : SettingsPageWithServicesViewModel(host)
{
    public DiffViewerSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "DiffViewerSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    [ObservableProperty]
    public partial bool RememberIgnoreWhiteSpacePreference { get; set; }

    [ObservableProperty]
    public partial bool RememberShowNonPrintingCharsPreference { get; set; }

    [ObservableProperty]
    public partial bool RememberShowEntireFilePreference { get; set; }

    [ObservableProperty]
    public partial bool RememberDiffAppearancePreference { get; set; }

    [ObservableProperty]
    public partial bool RememberNumberOfContextLines { get; set; }

    [ObservableProperty]
    public partial bool RememberShowSyntaxHighlightingInDiff { get; set; }

    [ObservableProperty]
    public partial bool OmitUninterestingDiff { get; set; }

    [ObservableProperty]
    public partial bool ContScrollToNextFileOnlyWithAlt { get; set; }

    [ObservableProperty]
    public partial bool OpenSubmoduleDiffInSeparateWindow { get; set; }

    [ObservableProperty]
    public partial bool ShowDiffForAllParents { get; set; }

    [ObservableProperty]
    public partial bool ShowAllCustomDiffTools { get; set; }

    [ObservableProperty]
    public partial decimal VerticalRulerPosition { get; set; }

    /// <summary>As <c>chkUseGitColoring</c>; <see cref="UseGEThemeGitColoring"/> is enabled with it.</summary>
    [ObservableProperty]
    public partial bool UseGitColoring { get; set; }

    [ObservableProperty]
    public partial bool UseGEThemeGitColoring { get; set; }

    [RelayCommand]
    private void SaveCurrentViewSettingsAsDefault() => host.SaveCurrentViewSettingsAsDefault();

    protected override void SettingsToPage(SettingsSource? settings)
    {
        RememberIgnoreWhiteSpacePreference = AppSettings.RememberIgnoreWhiteSpacePreference;
        OmitUninterestingDiff = AppSettings.OmitUninterestingDiff;
        RememberShowEntireFilePreference = AppSettings.RememberShowEntireFilePreference;
        RememberDiffAppearancePreference = AppSettings.RememberDiffDisplayAppearance.Value;
        RememberShowNonPrintingCharsPreference = AppSettings.RememberShowNonPrintingCharsPreference;
        RememberNumberOfContextLines = AppSettings.RememberNumberOfContextLines;
        RememberShowSyntaxHighlightingInDiff = AppSettings.RememberShowSyntaxHighlightingInDiff;
        OpenSubmoduleDiffInSeparateWindow = AppSettings.OpenSubmoduleDiffInSeparateWindow;
        ContScrollToNextFileOnlyWithAlt = AppSettings.AutomaticContinuousScroll;
        ShowDiffForAllParents = AppSettings.ShowDiffForAllParents;
        ShowAllCustomDiffTools = AppSettings.ShowAvailableDiffTools;
        VerticalRulerPosition = Math.Clamp(AppSettings.DiffVerticalRulerPosition, 0, 1000);
        UseGitColoring = AppSettings.UseGitColoring.Value;
        UseGEThemeGitColoring = AppSettings.ReverseGitColoring.Value;

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.RememberIgnoreWhiteSpacePreference = RememberIgnoreWhiteSpacePreference;
        AppSettings.OmitUninterestingDiff = OmitUninterestingDiff;
        AppSettings.RememberShowEntireFilePreference = RememberShowEntireFilePreference;
        AppSettings.RememberDiffDisplayAppearance.Value = RememberDiffAppearancePreference;
        AppSettings.RememberShowNonPrintingCharsPreference = RememberShowNonPrintingCharsPreference;
        AppSettings.RememberNumberOfContextLines = RememberNumberOfContextLines;
        AppSettings.RememberShowSyntaxHighlightingInDiff = RememberShowSyntaxHighlightingInDiff;
        AppSettings.OpenSubmoduleDiffInSeparateWindow = OpenSubmoduleDiffInSeparateWindow;
        AppSettings.AutomaticContinuousScroll = ContScrollToNextFileOnlyWithAlt;
        AppSettings.ShowDiffForAllParents = ShowDiffForAllParents;
        AppSettings.ShowAvailableDiffTools = ShowAllCustomDiffTools;
        AppSettings.DiffVerticalRulerPosition = (int)VerticalRulerPosition;
        AppSettings.UseGitColoring.Value = UseGitColoring;
        AppSettings.ReverseGitColoring.Value = UseGEThemeGitColoring;

        base.PageToSettings(settings);
    }
}
