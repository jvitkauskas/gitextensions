using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands;
using GitCommands.Settings;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the blame viewer settings; ids match <c>BlameViewerSettingsPage</c>.</summary>
public sealed class BlameViewerSettingsPageStrings : ViewStrings
{
    public BlameViewerSettingsPageStrings()
        : base("BlameViewerSettingsPage")
    {
        Title = Add("$this", "Text", "Blame viewer");
        BlameSettings = Add("groupBoxBlameSettings", "Text", "Blame settings");
        IgnoreWhitespace = Add("cbIgnoreWhitespace", "Text", "Ignore whitespace");
        DetectMoveAndCopyInThisFile = Add("cbDetectMoveAndCopyInThisFile", "Text", "Detect moved or copied lines within blamed file");
        DetectMoveAndCopyInAllFiles = Add("cbDetectMoveAndCopyInAllFiles", "Text", "Detect moved or copied lines from all files in same commit");
        BlameWarningTooltip = Add("_blameWarningTooltip", "Text", "Could prevent blame to calculate the accurate line number when blaming previous revisions.");
        DisplayResult = Add("groupBoxDisplayResult", "Text", "Display result settings");
        DisplayAuthorFirst = Add("cbDisplayAuthorFirst", "Text", "Display author first");
        ShowAuthor = Add("cbShowAuthor", "Text", "Show author");
        ShowAuthorDate = Add("cbShowAuthorDate", "Text", "Show author date");
        ShowAuthorTime = Add("cbShowAuthorTime", "Text", "Show author time");
        ShowLineNumbers = Add("cbShowLineNumbers", "Text", "Show line numbers");
        ShowOriginalFilePath = Add("cbShowOriginalFilePath", "Text", "Show original file path");
        ShowAuthorAvatar = Add("cbShowAuthorAvatar", "Text", "Show author avatar");
    }

    public TranslatedText Title { get; }

    public TranslatedText BlameSettings { get; }

    public TranslatedText IgnoreWhitespace { get; }

    public TranslatedText DetectMoveAndCopyInThisFile { get; }

    public TranslatedText DetectMoveAndCopyInAllFiles { get; }

    public TranslatedText BlameWarningTooltip { get; }

    public TranslatedText DisplayResult { get; }

    public TranslatedText DisplayAuthorFirst { get; }

    public TranslatedText ShowAuthor { get; }

    public TranslatedText ShowAuthorDate { get; }

    public TranslatedText ShowAuthorTime { get; }

    public TranslatedText ShowLineNumbers { get; }

    public TranslatedText ShowOriginalFilePath { get; }

    public TranslatedText ShowAuthorAvatar { get; }
}

/// <summary>Port of <c>BlameViewerSettingsPage</c> (global settings).</summary>
public sealed partial class BlameViewerSettingsPageViewModel(BlameViewerSettingsPageStrings strings) : SettingsPageViewModel
{
    public BlameViewerSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "BlameViewerSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    [ObservableProperty]
    public partial bool IgnoreWhitespace { get; set; }

    [ObservableProperty]
    public partial bool DetectMoveAndCopyInThisFile { get; set; }

    [ObservableProperty]
    public partial bool DetectMoveAndCopyInAllFiles { get; set; }

    [ObservableProperty]
    public partial bool DisplayAuthorFirst { get; set; }

    [ObservableProperty]
    public partial bool ShowAuthor { get; set; }

    [ObservableProperty]
    public partial bool ShowAuthorDate { get; set; }

    [ObservableProperty]
    public partial bool ShowAuthorTime { get; set; }

    [ObservableProperty]
    public partial bool ShowLineNumbers { get; set; }

    [ObservableProperty]
    public partial bool ShowOriginalFilePath { get; set; }

    [ObservableProperty]
    public partial bool ShowAuthorAvatar { get; set; }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        IgnoreWhitespace = AppSettings.IgnoreWhitespaceOnBlame;
        DetectMoveAndCopyInThisFile = AppSettings.DetectCopyInFileOnBlame;
        DetectMoveAndCopyInAllFiles = AppSettings.DetectCopyInAllOnBlame;
        DisplayAuthorFirst = AppSettings.BlameDisplayAuthorFirst;
        ShowAuthor = AppSettings.BlameShowAuthor;
        ShowAuthorDate = AppSettings.BlameShowAuthorDate;
        ShowAuthorTime = AppSettings.BlameShowAuthorTime;
        ShowLineNumbers = AppSettings.BlameShowLineNumbers;
        ShowOriginalFilePath = AppSettings.BlameShowOriginalFilePath;
        ShowAuthorAvatar = AppSettings.BlameShowAuthorAvatar;
        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.IgnoreWhitespaceOnBlame = IgnoreWhitespace;
        AppSettings.DetectCopyInAllOnBlame = DetectMoveAndCopyInAllFiles;
        AppSettings.DetectCopyInFileOnBlame = DetectMoveAndCopyInThisFile;
        AppSettings.BlameDisplayAuthorFirst = DisplayAuthorFirst;
        AppSettings.BlameShowAuthor = ShowAuthor;
        AppSettings.BlameShowAuthorDate = ShowAuthorDate;
        AppSettings.BlameShowAuthorTime = ShowAuthorTime;
        AppSettings.BlameShowLineNumbers = ShowLineNumbers;
        AppSettings.BlameShowOriginalFilePath = ShowOriginalFilePath;
        AppSettings.BlameShowAuthorAvatar = ShowAuthorAvatar;
        base.PageToSettings(settings);
    }
}

/// <summary>Strings of the detailed settings; ids match <c>DetailedSettingsPage</c>.</summary>
public sealed class DetailedSettingsPageStrings : ViewStrings
{
    public DetailedSettingsPageStrings()
        : base("DetailedSettingsPage")
    {
        Title = Add("$this", "Text", "Detailed");
        PushWindow = Add("PushWindowGB", "Text", "&Push window");
        RemotesFromServer = Add("chkRemotesFromServer", "Text", "Get remote branches directly from the remote");
        MergeWindow = Add("mergeWindowGroup", "Text", "&Merge window");
        AddLogMessages = Add("addLogMessages", "Text", "Add log messages");
        RevisionGraph = Add("gbRevisionGraph", "Text", "&Revision graph");
        MergeGraphLanesHavingCommonParent = Add("chkMergeGraphLanesHavingCommonParent", "Text", "Merge graph lanes having common parent");
        RenderGraphWithDiagonals = Add("chkRenderGraphWithDiagonals", "Text", "Render graph with diagonals");
        StraightenGraphDiagonals = Add("chkStraightenGraphDiagonals", "Text", "Straighten graph diagonals");
    }

    public TranslatedText Title { get; }

    public TranslatedText PushWindow { get; }

    public TranslatedText RemotesFromServer { get; }

    public TranslatedText MergeWindow { get; }

    public TranslatedText AddLogMessages { get; }

    public TranslatedText RevisionGraph { get; }

    public TranslatedText MergeGraphLanesHavingCommonParent { get; }

    public TranslatedText RenderGraphWithDiagonals { get; }

    public TranslatedText StraightenGraphDiagonals { get; }
}

/// <summary>Port of <c>DetailedSettingsPage</c> (a <c>DistributedSettingsPage</c>; the revision graph is global only).</summary>
public sealed partial class DetailedSettingsPageViewModel : SettingsPageViewModel
{
    public DetailedSettingsPageViewModel(DetailedSettingsPageStrings strings)
    {
        Strings = strings;
        RemotesFromServer = Add(new BoolSettingValue(DetailedSettings.GetRemoteBranchesDirectlyFromRemote));
        AddLogMessages = Add(new BoolSettingValue(DetailedSettings.AddMergeLogMessages));
        MessagesCount = Add(new NumberTextSettingValue<int>(DetailedSettings.MergeLogMessagesCount));
    }

    public DetailedSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "DetailedSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    public BoolSettingValue RemotesFromServer { get; }

    public BoolSettingValue AddLogMessages { get; }

    public NumberTextSettingValue<int> MessagesCount { get; }

    [ObservableProperty]
    public partial bool MergeGraphLanesHavingCommonParent { get; set; }

    [ObservableProperty]
    public partial bool RenderGraphWithDiagonals { get; set; }

    [ObservableProperty]
    public partial bool StraightenGraphDiagonals { get; set; }

    /// <summary>As <c>gbRevisionGraph.Enabled</c>: the graph settings are global.</summary>
    [ObservableProperty]
    public partial bool IsRevisionGraphEnabled { get; private set; }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        IsRevisionGraphEnabled = settings?.SettingLevel == SettingLevel.Global;
        MergeGraphLanesHavingCommonParent = AppSettings.MergeGraphLanesHavingCommonParent.Value;
        RenderGraphWithDiagonals = AppSettings.RenderGraphWithDiagonals.Value;
        StraightenGraphDiagonals = AppSettings.StraightenGraphDiagonals.Value;
        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.MergeGraphLanesHavingCommonParent.Value = MergeGraphLanesHavingCommonParent;
        AppSettings.RenderGraphWithDiagonals.Value = RenderGraphWithDiagonals;
        AppSettings.StraightenGraphDiagonals.Value = StraightenGraphDiagonals;
        base.PageToSettings(settings);
    }
}
