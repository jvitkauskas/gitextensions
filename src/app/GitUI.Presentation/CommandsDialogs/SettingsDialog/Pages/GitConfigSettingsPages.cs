using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.Config;
using GitCommands.DiffMergeTools;
using GitCommands.Settings;
using GitExtensions.Extensibility.Configurations;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>What the git config pages need to save their settings (<c>GitConfigSettingsSet.Save</c>).</summary>
public interface IGitConfigSettingsHost
{
    /// <summary>Saves the git config settings of all the levels (<c>GitConfigSettingsSet.Save</c>).</summary>
    void SaveGitConfigSettings();
}

/// <summary>
///  Port of <c>GitConfigBaseSettingsPage</c>: a page editing the git config of a level (<see cref="SettingsLevel.Effective"/>,
///  <see cref="SettingsLevel.Local"/>, <see cref="SettingsLevel.Global"/> or <see cref="SettingsLevel.System"/>), whose settings
///  are saved to the files when the page is saved.
/// </summary>
public abstract class GitConfigSettingsPageViewModelBase(IGitConfigSettingsHost host) : SettingsPageViewModel
{
    protected override void PageToSettings(SettingsSource? settings)
    {
        base.PageToSettings(settings);
        host.SaveGitConfigSettings();
    }
}

/// <summary>Strings of the git config settings; ids match <c>GitConfigSettingsPage</c>.</summary>
public sealed class GitConfigSettingsPageStrings : ViewStrings
{
    public GitConfigSettingsPageStrings()
        : base("GitConfigSettingsPage")
    {
        Title = Add("$this", "Text", "Config");
        SelectFile = Add("_selectFile", "Text", "Select file");
        UserName = Add("label3", "Text", "User name");
        UserEmail = Add("label4", "Text", "User email");
        InvalidGitPath = Add("label9", "Text", "You need to set the correct path to \ngit before you can change\nglobal settings.\n");
        CredentialHelper = Add("lblCredentialHelper", "Text", "Credential helper");
        Editor = Add("label6", "Text", "Editor");
        MergeTool = Add("lblMergeTool", "Text", "Mergetool");
        MergeToolPath = Add("lblMergeToolPath", "Text", "Path to mergetool");
        MergeToolBrowse = Add("btnMergeToolBrowse", "Text", "Browse");
        MergeToolBrowseName = Add("btnMergeToolBrowse", "AccessibleName", "Browse Path to mergetool");
        MergeToolCommand = Add("lblMergeToolCommand", "Text", "Mergetool command");
        MergeToolCommandSuggest = Add("btnMergeToolCommandSuggest", "Text", "Suggest");
        MergeToolCommandSuggestName = Add("btnMergeToolCommandSuggest", "AccessibleName", "Suggest Mergetool command");
        DiffTool = Add("lblDiffTool", "Text", "Difftool");
        DiffToolPath = Add("lblDiffToolPath", "Text", "Path to difftool");
        DiffToolBrowse = Add("btnDiffToolBrowse", "Text", "Browse");
        DiffToolBrowseName = Add("btnDiffToolBrowse", "AccessibleName", "Browse Difftool command");
        DiffToolCommand = Add("lblDiffToolCommand", "Text", "Difftool command");
        DiffToolCommandSuggest = Add("btnDiffToolCommandSuggest", "Text", "Suggest");
        DiffToolCommandSuggestName = Add("btnDiffToolCommandSuggest", "AccessibleName", "Suggest Difftool command");
        CommitTemplatePath = Add("lblCommitTemplatePath", "Text", "Path to commit template");
        CommitTemplateBrowse = Add("btnCommitTemplateBrowse", "Text", "Browse");
        CommitTemplateBrowseName = Add("btnCommitTemplateBrowse", "AccessibleName", "Browse Path to commit template");
        LineEndings = Add("groupBoxLineEndings", "Text", "Line endings");
        AutoCrlfTrue = Add("globalAutoCrlfTrue", "Text", "Checkout Windows-style, commit Unix-style line endings (\"core.autocrlf\"  is set to \"true\")");
        AutoCrlfInput = Add("globalAutoCrlfInput", "Text", "Checkout as-is, commit Unix-style line endings (\"core.autocrlf\"  is set to \"input\")");
        AutoCrlfFalse = Add("globalAutoCrlfFalse", "Text", "Checkout as-is, commit as-is (\"core.autocrlf\"  is set to \"false\")");
        AutoCrlfNotSet = Add("globalAutoCrlfNotSet", "Text", "Not set");
        FilesEncoding = Add("label60", "Text", "Files content encoding");
        ConfigureEncoding = Add("ConfigureEncoding", "Text", "Configure");
    }

    public TranslatedText Title { get; }

    public TranslatedText SelectFile { get; }

    public TranslatedText UserName { get; }

    public TranslatedText UserEmail { get; }

    public TranslatedText InvalidGitPath { get; }

    public TranslatedText CredentialHelper { get; }

    public TranslatedText Editor { get; }

    public TranslatedText MergeTool { get; }

    public TranslatedText MergeToolPath { get; }

    public TranslatedText MergeToolBrowse { get; }

    public TranslatedText MergeToolBrowseName { get; }

    public TranslatedText MergeToolCommand { get; }

    public TranslatedText MergeToolCommandSuggest { get; }

    public TranslatedText MergeToolCommandSuggestName { get; }

    public TranslatedText DiffTool { get; }

    public TranslatedText DiffToolPath { get; }

    public TranslatedText DiffToolBrowse { get; }

    public TranslatedText DiffToolBrowseName { get; }

    public TranslatedText DiffToolCommand { get; }

    public TranslatedText DiffToolCommandSuggest { get; }

    public TranslatedText DiffToolCommandSuggestName { get; }

    public TranslatedText CommitTemplatePath { get; }

    public TranslatedText CommitTemplateBrowse { get; }

    public TranslatedText CommitTemplateBrowseName { get; }

    public TranslatedText LineEndings { get; }

    public TranslatedText AutoCrlfTrue { get; }

    public TranslatedText AutoCrlfInput { get; }

    public TranslatedText AutoCrlfFalse { get; }

    public TranslatedText AutoCrlfNotSet { get; }

    public TranslatedText FilesEncoding { get; }

    public TranslatedText ConfigureEncoding { get; }
}

/// <summary>What <see cref="GitConfigSettingsPageViewModel"/> needs from the application.</summary>
public interface IGitConfigSettingsPageHost : IGitConfigSettingsHost
{
    /// <summary>As <c>CheckSettingsLogic.CanFindGitCmd</c>: whether git runs.</summary>
    bool CanFindGitCmd();

    /// <summary>The editors offered (<c>EditorHelper.GetEditors</c>).</summary>
    IReadOnlyList<string> GetEditors();

    /// <summary>Shows the dialog choosing the encodings offered (<c>FormAvailableEncodings</c>); returns whether they changed.</summary>
    bool ShowAvailableEncodings();
}

/// <summary>
///  Port of <c>GitConfigSettingsPage</c> (a <c>GitConfigBaseSettingsPage</c>): the user, the credential helper, the editor, the
///  merge and diff tools, the commit template, the line endings and the encoding of the files.
/// </summary>
public sealed partial class GitConfigSettingsPageViewModel : GitConfigSettingsPageViewModelBase
{
    private const string GitCredentialHelperPrefix = "git-credential-";

    private readonly IGitConfigSettingsPageHost _host;
    private readonly IFileDialogService _fileDialogs;
    private readonly string? _workingDir;
    private readonly GitConfigSettingsPageController _controller = new();
    private readonly DiffMergeToolConfigurationManager _diffMergeToolConfigurationManager;
    private SettingsSource? _settings;
    private bool _updatingTools;

    [GeneratedRegex(@"\$(?:LOCAL|REMOTE|BASE|MERGED)", RegexOptions.ExplicitCapture)]
    private static partial Regex WslRebaseRegex { get; }

    /// <param name="workingDir">The working directory of the repository (a WSL path makes the commands WSL ones).</param>
    public GitConfigSettingsPageViewModel(GitConfigSettingsPageStrings strings, IGitConfigSettingsPageHost host, IFileDialogService fileDialogs, string? workingDir)
        : base(host)
    {
        Strings = strings;
        _host = host;
        _fileDialogs = fileDialogs;
        _workingDir = workingDir;

        // The settings shown, or saved (those of the level shown before, when another level is chosen).
        _diffMergeToolConfigurationManager = new DiffMergeToolConfigurationManager(() => _settings);

        // As Init.
        MergeTools = [.. RegisteredDiffMergeTools.All(DiffMergeToolType.Merge)];
        DiffTools = [.. RegisteredDiffMergeTools.All(DiffMergeToolType.Diff)];
        FillEncodings();

        string[] linuxCredentialHelpers = ["oauth"];
        List<string> credentialHelpers = [];
        if (OperatingSystem.IsWindows())
        {
            credentialHelpers.AddRange(PathUtil.IsWslPath(workingDir)
                ? [.. FindGitCredentialHelpers().Select(path => path.ToWslPath()!.Replace(" ", @"\ ")), .. linuxCredentialHelpers]
                : [.. FindGitCredentialHelpers().Select(GetCredentialHelperName)]);
        }
        else
        {
            credentialHelpers.AddRange(linuxCredentialHelpers);
        }

        credentialHelpers.AddRange(["store", "cache"]);
        CredentialHelpers = credentialHelpers;

        Editors = [.. host.GetEditors().Select(AdaptCommandIfWsl).WhereNotNull()];

        return;

        static IEnumerable<string> FindGitCredentialHelpers()
        {
            try
            {
                string? gitDir = Path.GetDirectoryName(AppSettings.GitCommand);
                if (gitDir?.EndsWith("bin") is true)
                {
                    gitDir = Path.GetDirectoryName(gitDir);
                }

                return !Directory.Exists(gitDir)
                    ? []
                    : Directory.GetFiles(gitDir, $"{GitCredentialHelperPrefix}*.exe", SearchOption.AllDirectories)
                        .Where(path => !path.Contains("git-credential-helper-selector"));
            }
            catch (Exception exception)
            {
                Trace.Write(exception);
                return [];
            }
        }

        static string GetCredentialHelperName(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            return name.StartsWith(GitCredentialHelperPrefix) ? name[GitCredentialHelperPrefix.Length..] : path;
        }
    }

    public GitConfigSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "GitConfigSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>The credential helpers offered (<c>cbxCredentialHelper</c>).</summary>
    public IReadOnlyList<string> CredentialHelpers { get; }

    /// <summary>The editors offered (<c>GlobalEditor</c>).</summary>
    public IReadOnlyList<string> Editors { get; }

    public IReadOnlyList<string> MergeTools { get; }

    public IReadOnlyList<string> DiffTools { get; }

    /// <summary>The encodings offered (<c>Global_FilesEncoding</c>), shown by their name.</summary>
    public ObservableCollection<Encoding> Encodings { get; } = [];

    /// <summary>As <c>InvalidGitPathGlobal</c> and the fields enabled in <c>OnPageShown</c>: git runs.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInvalidGitPathVisible), nameof(IsMergeToolPathEnabled))]
    public partial bool CanFindGitCmd { get; private set; } = true;

    public bool IsInvalidGitPathVisible => !CanFindGitCmd;

    [ObservableProperty]
    public partial string UserName { get; set; } = "";

    [ObservableProperty]
    public partial string UserEmail { get; set; } = "";

    /// <summary>
    ///  As <c>cbxCredentialHelper</c>, hidden for the effective settings, which can only return the last value; the values of a
    ///  helper configured several times are shown joined, read-only.
    /// </summary>
    [ObservableProperty]
    public partial string CredentialHelper { get; set; } = "";

    [ObservableProperty]
    public partial bool IsCredentialHelperVisible { get; private set; } = true;

    [ObservableProperty]
    public partial bool IsCredentialHelperEnabled { get; private set; } = true;

    [ObservableProperty]
    public partial string Editor { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMergeToolSet), nameof(IsMergeToolPathEnabled))]
    public partial string MergeTool { get; set; } = "";

    /// <summary>As the fields of the merge tool enabled in <c>cboMergeTool_TextChanged</c>.</summary>
    public bool IsMergeToolSet => !string.IsNullOrEmpty(MergeTool);

    /// <summary>As <c>txtMergeToolPath.Enabled</c> and <c>txtMergeToolCommand.Enabled</c>.</summary>
    public bool IsMergeToolPathEnabled => IsMergeToolSet && CanFindGitCmd;

    [ObservableProperty]
    public partial string MergeToolPath { get; set; } = "";

    [ObservableProperty]
    public partial string MergeToolCommand { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDiffToolSet))]
    public partial string DiffTool { get; set; } = "";

    /// <summary>As the fields of the diff tool enabled in <c>cboDiffTool_TextChanged</c>.</summary>
    public bool IsDiffToolSet => !string.IsNullOrEmpty(DiffTool);

    [ObservableProperty]
    public partial string DiffToolPath { get; set; } = "";

    [ObservableProperty]
    public partial string DiffToolCommand { get; set; } = "";

    [ObservableProperty]
    public partial string CommitTemplatePath { get; set; } = "";

    [ObservableProperty]
    public partial bool AutoCrlfTrue { get; set; }

    [ObservableProperty]
    public partial bool AutoCrlfInput { get; set; }

    [ObservableProperty]
    public partial bool AutoCrlfFalse { get; set; }

    [ObservableProperty]
    public partial bool AutoCrlfNotSet { get; set; }

    [ObservableProperty]
    public partial Encoding? FilesEncoding { get; set; }

    public override void OnPageShown() => CanFindGitCmd = _host.CanFindGitCmd();

    protected override void SettingsToPage(SettingsSource? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;

        string? mergeTool = _diffMergeToolConfigurationManager.ConfiguredMergeTool;
        string? diffTool = _diffMergeToolConfigurationManager.ConfiguredDiffTool;

        FilesEncoding = new GitEncodingSettingsGetter(settings).FilesEncoding;

        UserName = settings.GetValue(SettingKeyString.UserName) ?? "";
        UserEmail = settings.GetValue(SettingKeyString.UserEmail) ?? "";
        Editor = settings.GetValue("core.editor") ?? "";
        CommitTemplatePath = settings.GetValue("commit.template") ?? "";

        // The credential helper is hidden because EffectiveGitConfigSettings can only return the last value.
        GitConfigSettings? gitConfigSettings = TryGetGitConfigSettings(settings);
        IsCredentialHelperVisible = gitConfigSettings is not null;
        IsCredentialHelperEnabled = gitConfigSettings is not null;
        if (gitConfigSettings is not null)
        {
            IReadOnlyList<string> values = gitConfigSettings.GetValues(SettingKeyString.CredentialHelper);
            IsCredentialHelperEnabled = values.Count <= 1;
            CredentialHelper = string.Join(", ", values);
        }

        _updatingTools = true;
        try
        {
            MergeTool = mergeTool ?? "";
            MergeToolPath = _diffMergeToolConfigurationManager.GetToolPath(mergeTool, DiffMergeToolType.Merge);
            MergeToolCommand = _diffMergeToolConfigurationManager.GetToolCommand(mergeTool, DiffMergeToolType.Merge);

            DiffTool = diffTool ?? "";
            DiffToolPath = _diffMergeToolConfigurationManager.GetToolPath(diffTool, DiffMergeToolType.Diff);
            DiffToolCommand = _diffMergeToolConfigurationManager.GetToolCommand(diffTool, DiffMergeToolType.Diff);
        }
        finally
        {
            _updatingTools = false;
        }

        AutoCRLFType? autocrlf = ((ISettingsValueGetter)settings).GetValue<AutoCRLFType>("core.autocrlf");
        AutoCrlfFalse = autocrlf is AutoCRLFType.@false;
        AutoCrlfInput = autocrlf is AutoCRLFType.input;
        AutoCrlfTrue = autocrlf is AutoCRLFType.@true;
        AutoCrlfNotSet = autocrlf is null;

        base.SettingsToPage(settings);

        return;

        static GitConfigSettings? TryGetGitConfigSettings(SettingsSource settingsSource)
        {
            return settingsSource is SettingsSource<IPersistentConfigValueStore> persistentSettingsSource
                ? persistentSettingsSource.ConfigValueStore as GitConfigSettings
                : settingsSource is SettingsSource<IConfigValueStore> otherGenericSettingsSource
                    ? otherGenericSettingsSource.ConfigValueStore as GitConfigSettings
                    : null;
        }
    }

    /// <summary>As <c>PageToSettings</c>: some settings are silently not saved if git does not run.</summary>
    protected override void PageToSettings(SettingsSource? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;

        new GitEncodingSettingsSetter(settings).FilesEncoding = FilesEncoding;

        base.PageToSettings(settings);

        if (!_host.CanFindGitCmd())
        {
            return;
        }

        settings.SetValue(SettingKeyString.UserName, UserName);
        settings.SetValue(SettingKeyString.UserEmail, UserEmail);
        settings.SetValue("commit.template", CommitTemplatePath);
        settings.SetValue("core.editor", Editor.ConvertPathToGitSetting());
        if (IsCredentialHelperEnabled)
        {
            settings.SetValue(SettingKeyString.CredentialHelper, CredentialHelper);
        }

        if (!string.IsNullOrWhiteSpace(DiffTool))
        {
            _diffMergeToolConfigurationManager.ConfigureDiffMergeTool(DiffTool, DiffMergeToolType.Diff, DiffToolPath, DiffToolCommand);
        }
        else
        {
            _diffMergeToolConfigurationManager.UnsetCurrentTool(DiffMergeToolType.Diff);
        }

        if (!string.IsNullOrWhiteSpace(MergeTool))
        {
            _diffMergeToolConfigurationManager.ConfigureDiffMergeTool(MergeTool, DiffMergeToolType.Merge, MergeToolPath, MergeToolCommand);
        }
        else
        {
            _diffMergeToolConfigurationManager.UnsetCurrentTool(DiffMergeToolType.Merge);
        }

        AutoCRLFType? autoCRLFType =
            AutoCrlfFalse ? AutoCRLFType.@false
            : AutoCrlfInput ? AutoCRLFType.input
            : AutoCrlfTrue ? AutoCRLFType.@true
            : null;
        settings.SetValue("core.autocrlf", autoCRLFType?.ToString());
    }

    // As cboMergeTool_TextChanged: the path and the command configured for the tool are shown.
    partial void OnMergeToolChanged(string value)
    {
        if (IsLoadingSettings || _updatingTools)
        {
            return;
        }

        ShowTool(value, DiffMergeToolType.Merge);
    }

    // As cboDiffTool_TextChanged.
    partial void OnDiffToolChanged(string value)
    {
        if (IsLoadingSettings || _updatingTools)
        {
            return;
        }

        ShowTool(value, DiffMergeToolType.Diff);
    }

    // As txtMergeToolPath_TextChanged: the command is suggested for a path typed for a known tool.
    partial void OnMergeToolPathChanged(string value)
    {
        if (!IsLoadingSettings && !_updatingTools && RegisteredDiffMergeTools.All(DiffMergeToolType.Merge).Contains(MergeTool))
        {
            SuggestMergeToolCommand();
        }
    }

    // As txtDiffToolPath_TextChanged.
    partial void OnDiffToolPathChanged(string value)
    {
        if (!IsLoadingSettings && !_updatingTools && RegisteredDiffMergeTools.All(DiffMergeToolType.Diff).Contains(DiffTool))
        {
            SuggestDiffToolCommand();
        }
    }

    private void ShowTool(string toolName, DiffMergeToolType toolType)
    {
        string toolPath = string.IsNullOrWhiteSpace(toolName) ? "" : _diffMergeToolConfigurationManager.GetToolPath(toolName, toolType);
        string toolCommand = string.IsNullOrWhiteSpace(toolPath) ? "" : _diffMergeToolConfigurationManager.GetToolCommand(toolName, toolType);
        _updatingTools = true;
        try
        {
            if (toolType == DiffMergeToolType.Merge)
            {
                MergeToolPath = toolPath;
                MergeToolCommand = toolCommand;
            }
            else
            {
                DiffToolPath = toolPath;
                DiffToolCommand = toolCommand;
            }
        }
        finally
        {
            _updatingTools = false;
        }
    }

    /// <summary>As <c>txtDiffMergeToolPath_LostFocus</c>: the path of the merge tool is converted to a posix path.</summary>
    public void NormalizeMergeToolPath()
    {
        if (!IsLoadingSettings)
        {
            SetWithoutSuggestion(() => MergeToolPath = MergeToolPath.ToPosixPath() ?? "");
        }
    }

    /// <summary>As <c>txtDiffMergeToolPath_LostFocus</c> for the diff tool.</summary>
    public void NormalizeDiffToolPath()
    {
        if (!IsLoadingSettings)
        {
            SetWithoutSuggestion(() => DiffToolPath = DiffToolPath.ToPosixPath() ?? "");
        }
    }

    private void SetWithoutSuggestion(Action set)
    {
        _updatingTools = true;
        try
        {
            set();
        }
        finally
        {
            _updatingTools = false;
        }
    }

    /// <summary>As <c>btnMergeToolCommandSuggest_Click</c>.</summary>
    [RelayCommand]
    private void SuggestMergeToolCommand()
    {
        if (string.IsNullOrWhiteSpace(MergeTool))
        {
            MergeToolCommand = "";
            return;
        }

        DiffMergeToolConfiguration diffMergeToolConfig = _diffMergeToolConfigurationManager.LoadDiffMergeToolConfig(MergeTool, MergeToolPath);
        MergeToolCommand = AdaptCommandIfWsl(diffMergeToolConfig.FullMergeCommand) ?? "";
    }

    /// <summary>As <c>btnDiffToolCommandSuggest_Click</c>.</summary>
    [RelayCommand]
    private void SuggestDiffToolCommand()
    {
        if (string.IsNullOrWhiteSpace(DiffTool))
        {
            DiffToolCommand = "";
            return;
        }

        DiffMergeToolConfiguration diffMergeToolConfig = _diffMergeToolConfigurationManager.LoadDiffMergeToolConfig(DiffTool, DiffToolPath);
        DiffToolCommand = AdaptCommandIfWsl(diffMergeToolConfig.FullDiffCommand) ?? "";
    }

    /// <summary>As <c>btnMergeToolBrowse_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowseMergeToolAsync()
    {
        string path = await BrowseDiffMergeToolAsync(MergeTool, MergeToolPath, DiffMergeToolType.Merge);
        SetWithoutSuggestion(() => MergeToolPath = path.ToPosixPath() ?? "");
        SuggestMergeToolCommand();
    }

    /// <summary>As <c>btnDiffToolBrowse_Click</c>.</summary>
    [RelayCommand]
    private async Task BrowseDiffToolAsync()
    {
        string path = await BrowseDiffMergeToolAsync(DiffTool, DiffToolPath, DiffMergeToolType.Diff);
        SetWithoutSuggestion(() => DiffToolPath = path.ToPosixPath() ?? "");
        SuggestDiffToolCommand();
    }

    private async Task<string> BrowseDiffMergeToolAsync(string toolName, string path, DiffMergeToolType toolType)
    {
        DiffMergeToolConfiguration diffMergeToolConfig = default;
        IReadOnlyList<string> knownTools = toolType == DiffMergeToolType.Diff ? DiffTools : MergeTools;
        if (knownTools.Contains(toolName))
        {
            diffMergeToolConfig = _diffMergeToolConfigurationManager.LoadDiffMergeToolConfig(toolName, null);
        }

        string initialDirectory = _controller.GetInitialDirectory(path, diffMergeToolConfig.Path);

        string filter = !string.IsNullOrWhiteSpace(diffMergeToolConfig.ExeFileName)
            ? $"{toolName}|{diffMergeToolConfig.ExeFileName}"
            : "*.exe;*.cmd;*.bat|*.exe;*.cmd;*.bat";

        return await _fileDialogs.PickFileAsync(Strings.SelectFile.Text, FileDialogFilter.Parse(filter), initialDirectory) ?? path;
    }

    /// <summary>As <c>btnCommitTemplateBrowse_Click</c> (<c>CommonLogic.SelectFile</c>).</summary>
    [RelayCommand]
    private async Task BrowseCommitTemplateAsync()
    {
        if (await _fileDialogs.PickFileAsync(Strings.SelectFile.Text, FileDialogFilter.Parse("*.txt (*.txt)|*.txt"), ".") is { } path)
        {
            CommitTemplatePath = path;
        }
    }

    /// <summary>As <c>ConfigureEncoding_Click</c>: the encodings offered are chosen.</summary>
    [RelayCommand]
    private void ConfigureEncoding()
    {
        if (_host.ShowAvailableEncodings())
        {
            Encoding? selected = FilesEncoding;
            FillEncodings();
            FilesEncoding = selected is not null && Encodings.Contains(selected) ? selected : null;
        }
    }

    // As CommonLogic.FillEncodings.
    private void FillEncodings()
    {
        Encodings.Clear();
        foreach (Encoding encoding in AppSettings.AvailableEncodings.Values)
        {
            Encodings.Add(encoding);
        }
    }

    /// <summary>As <c>AdaptCommandIfWsl</c>: in a WSL repository, the command runs the Windows tool from WSL.</summary>
    private string? AdaptCommandIfWsl(string? command)
    {
        if (string.IsNullOrEmpty(command) || !PathUtil.IsWslPath(_workingDir))
        {
            return command;
        }

        // Replace "D:" with "/mnt/d"
        int colonIndex = command.IndexOf(':');
        if (colonIndex == (command[0] == '"' ? 2 : 1))
        {
            int windowsDriveIndex = colonIndex - 1;
            command = $"{command[..windowsDriveIndex]}/mnt/{char.ToLower(command[windowsDriveIndex])}{command[(colonIndex + 1)..]}";
        }

        return WslRebaseRegex.Replace(command, @"$(wslpath -aw $&)").ToPosixPath();
    }
}

/// <summary>Strings of the advanced git config settings; ids match <c>GitConfigAdvancedSettingsPage</c>.</summary>
public sealed class GitConfigAdvancedSettingsPageStrings : ViewStrings
{
    public GitConfigAdvancedSettingsPageStrings()
        : base("GitConfigAdvancedSettingsPage")
    {
        Title = Add("$this", "Text", "Advanced");
        PullRebase = Add("checkBoxPullRebase", "Text", "Rebase local branch when pulling (instead of merge)");
        FetchPrune = Add("checkBoxFetchPrune", "Text", "Prune remote branches during fetch");
        MergeAutoStash = Add("checkboxMergeAutoStash", "Text", "Automatically stash before doing a merge");
        RebaseAutostash = Add("checkBoxRebaseAutostash", "Text", "Automatically stash before doing a rebase");
        RebaseAutosquash = Add("checkBoxRebaseAutosquash", "Text", "Automatically squash commits when doing an interactive rebase");
        UpdateRefs = Add("checkBoxUpdateRefs", "Text", "Rebase also dependent branches");
        ReReReEnabled = Add("checkBoxReReReEnabled", "Text", "Reuse recorded resolution of conflicted merges");
        ReReReAutoUpdate = Add("checkBoxReReReAutoUpdate", "Text", "Automatically apply recorded resolution of conflicted merges");
    }

    public TranslatedText Title { get; }

    public TranslatedText PullRebase { get; }

    public TranslatedText FetchPrune { get; }

    public TranslatedText MergeAutoStash { get; }

    public TranslatedText RebaseAutostash { get; }

    public TranslatedText RebaseAutosquash { get; }

    public TranslatedText UpdateRefs { get; }

    public TranslatedText ReReReEnabled { get; }

    public TranslatedText ReReReAutoUpdate { get; }
}

/// <summary>A boolean git setting of <see cref="GitConfigAdvancedSettingsPageViewModel"/>: a three-state check box.</summary>
public sealed partial class GitConfigFlag(string key, string text, bool isVisible = true) : ObservableObject
{
    /// <summary>The key of the git setting, e.g. <c>pull.rebase</c>.</summary>
    public string Key { get; } = key;

    /// <summary>As <c>GitConfigAdvancedSettingsPage_Load</c>: the text followed by the key.</summary>
    public string Text { get; } = $"{text} [{key}]";

    public bool IsVisible { get; } = isVisible;

    /// <summary>The value; <see langword="null"/> when unset or not a boolean.</summary>
    [ObservableProperty]
    public partial bool? Value { get; set; }
}

/// <summary>Port of <c>GitConfigAdvancedSettingsPage</c> (a <c>GitConfigBaseSettingsPage</c>): boolean git settings.</summary>
public sealed class GitConfigAdvancedSettingsPageViewModel : GitConfigSettingsPageViewModelBase
{
    /// <param name="supportsUpdateRefs">As <c>GitVersion.Current.SupportUpdateRefs</c>: <c>rebase.updaterefs</c> is shown.</param>
    public GitConfigAdvancedSettingsPageViewModel(GitConfigAdvancedSettingsPageStrings strings, IGitConfigSettingsHost host, bool supportsUpdateRefs)
        : base(host)
    {
        Strings = strings;
        Flags =
        [
            new("pull.rebase", strings.PullRebase.PlainText),
            new("fetch.prune", strings.FetchPrune.PlainText),
            new("merge.autostash", strings.MergeAutoStash.PlainText),
            new("rebase.autostash", strings.RebaseAutostash.PlainText),
            new("rebase.autosquash", strings.RebaseAutosquash.PlainText),
            new("rebase.updaterefs", strings.UpdateRefs.PlainText, supportsUpdateRefs),
            new("rerere.enabled", strings.ReReReEnabled.PlainText),
            new("rerere.autoupdate", strings.ReReReAutoUpdate.PlainText),
        ];
    }

    public GitConfigAdvancedSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "GitConfigAdvancedSettingsPage";

    public override IEnumerable<string> SearchKeywords => [.. Strings.Texts, .. Flags.Select(flag => flag.Key)];

    /// <summary>The settings, in the order of the page.</summary>
    public IReadOnlyList<GitConfigFlag> Flags { get; }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        foreach (GitConfigFlag flag in Flags)
        {
            flag.Value = settings.GetValue(flag.Key) switch
            {
                "true" or "yes" or "on" or "1" => true,
                "false" or "no" or "off" or "0" or "" => false,
                _ => null
            };
        }

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        foreach (GitConfigFlag flag in Flags)
        {
            settings.SetValue(flag.Key, flag.Value switch { true => "true", false => "false", null => null });
        }

        base.PageToSettings(settings);
    }
}
