using CommunityToolkit.Mvvm.ComponentModel;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the buttons of the main toolbar; ids match <c>FormBrowse</c> and its <c>WorkingDirectoryToolStripSplitButton</c>.</summary>
public sealed class BrowseToolbarStrings : ViewStrings
{
    public BrowseToolbarStrings()
        : base("FormBrowse")
    {
        NoWorkingFolder = Add("_noWorkingFolderText", "Text", "No working directory");
        ConfigureWorkingDirMenu = Add("_configureWorkingDirMenu", "Text", "Co&nfigure this menu...");
        WorkingDirToolTip = Add("_toolTip", "Text", "Change working directory\nLeft click opens the drop-down menu.\nThen hold Ctrl in order to open the selected repository in a new instance.\nRight click starts the \"Open repository\" dialog.");
        WorktreesToolTip = Add("toolStripWorktrees", "ToolTipText", "Worktrees");
        SubmodulesToolTip = Add("toolStripButtonLevelUp", "ToolTipText", "Submodules");
        ShellToolTip = Add("userShell", "ToolTipText", "Git bash");
        GoToSuperProject = Add("_goToSuperProject", "Text", "Go to superproject");
        NoSubmodulesPresent = Add("_noSubmodulesPresent", "Text", "No submodules");
        TopProjectModuleFormat = Add("_topProjectModuleFormat", "Text", "Top project: {0}");
        SuperprojectModuleFormat = Add("_superprojectModuleFormat", "Text", "Superproject: {0}");
        UpdateCurrentSubmodule = Add("_updateCurrentSubmodule", "Text", "Update current submodule");
        Loading = Add("_loading", "Text", "Loading...");
    }

    public TranslatedText GoToSuperProject { get; }

    public TranslatedText NoSubmodulesPresent { get; }

    public TranslatedText TopProjectModuleFormat { get; }

    public TranslatedText SuperprojectModuleFormat { get; }

    public TranslatedText UpdateCurrentSubmodule { get; }

    public TranslatedText Loading { get; }

    public TranslatedText NoWorkingFolder { get; }

    public TranslatedText ConfigureWorkingDirMenu { get; }

    public TranslatedText WorkingDirToolTip { get; }

    public TranslatedText WorktreesToolTip { get; }

    public TranslatedText SubmodulesToolTip { get; }

    public TranslatedText ShellToolTip { get; }
}

/// <summary>A shell of the shell button (an <c>IShellDescriptor</c>).</summary>
/// <param name="Icon">The image: an asset name or PNG data.</param>
/// <param name="Shell">The shell, for <see cref="IBrowseToolbarHost.RunShell"/>.</param>
public sealed record BrowseShell(string Name, object? Icon, object Shell);

/// <summary>The worktrees of the repository, as the worktrees button lists them.</summary>
/// <param name="Count">The number of worktrees (the button is shown with more than one).</param>
/// <param name="Items">The worktrees (the current one checked), then create, prune and manage.</param>
public sealed record BrowseWorktreeMenu(int Count, IReadOnlyList<BrowseMenuItem> Items);

/// <summary>What the buttons of the main toolbar need from the application.</summary>
public interface IBrowseToolbarHost
{
    /// <summary>The caption of the working directory button (as <c>WorkingDirectoryToolStripSplitButton.RefreshContent</c>), empty without one.</summary>
    string WorkingDirectoryCaption { get; }

    /// <summary>As <c>toolStripWorktrees_DropDownOpening</c>, in the background.</summary>
    Task<BrowseWorktreeMenu> GetWorktreeMenuAsync();

    /// <summary>The shells with an executable (<c>IShellProvider.GetShells</c>), the default one first.</summary>
    IReadOnlyList<BrowseShell> GetShells();

    /// <summary>As <c>userShell_Click</c>: the shell in the working directory.</summary>
    void RunShell(BrowseShell shell);

    /// <summary>As <c>LoadUserMenu</c>: the enabled scripts shown in the user menu bar, which run when clicked.</summary>
    IReadOnlyList<BrowseMenuItem> GetToolbarScripts();

    /// <summary>
    ///  Raised on the UI thread with the items of the submodules button (as <c>PopulateToolbar</c> and
    ///  <c>UpdateSubmoduleMenuStatus</c>), or <see langword="null"/> while the submodules are read.
    /// </summary>
    event EventHandler<IReadOnlyList<BrowseMenuItem>?>? SubmoduleMenuChanged;

    /// <summary>Whether the repository is a submodule (<c>Module.SuperprojectModule</c>).</summary>
    bool HasSuperproject { get; }

    /// <summary>Whether the button is enabled: a working directory that is not bare.</summary>
    bool CanShowSubmodules { get; }

    /// <summary>As <c>toolStripButtonLevelUp_ButtonClick</c> with a superproject: the superproject in this window.</summary>
    void GoToSuperproject();
}

/// <summary>The working directory button of the main toolbar (<c>_NO_TRANSLATE_WorkingDir</c>).</summary>
public sealed partial class BrowseViewModel
{
    public BrowseToolbarStrings ToolbarStrings { get; } = ViewStrings.Load<BrowseToolbarStrings>();

    /// <summary>The working directory, as the recent repositories show it.</summary>
    [ObservableProperty]
    public partial string WorkingDirectoryText { get; private set; } = "";

    /// <summary>
    ///  As <c>FillDropDown</c>: the favourite repositories, the recent ones, open and close, and the settings of the menu.
    /// </summary>
    public IReadOnlyList<BrowseMenuItem> GetWorkingDirectoryItems() =>
    [
        new(Strings.FavouriteRepositories.AccessKeyText, null, "Star", Children: _host.GetRepositoriesMenu(favourites: true)),
        BrowseMenuItem.Separator,
        .. _host.GetRepositoriesMenu(favourites: false),
        BrowseMenuItem.Separator,
        new(Strings.Open.AccessKeyText, BrowseCommand.Open, "RepoOpen"),
        new(Strings.CloseRepository.AccessKeyText, BrowseCommand.CloseRepository),
        BrowseMenuItem.Separator,
        new(ToolbarStrings.ConfigureWorkingDirMenu.AccessKeyText, BrowseCommand.RecentRepositoriesSettings),
    ];

    /// <summary>The worktrees menu (<c>toolStripWorktrees</c>), shown with more than one worktree.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<BrowseMenuItem> WorktreeItems { get; private set; } = [];

    [ObservableProperty]
    public partial bool ShowWorktrees { get; private set; }

    /// <summary>The shells of the shell button (<c>userShell</c>); the button runs the first one.</summary>
    public IReadOnlyList<BrowseShell> Shells { get; private set; } = [];

    public BrowseShell? DefaultShell => Shells.Count > 0 ? Shells[0] : null;

    public bool HasShells => Shells.Count > 0;

    /// <summary>The buttons of the scripts toolbar (<c>ToolStripScripts</c>).</summary>
    public IReadOnlyList<BrowseMenuItem> ScriptItems { get; private set; } = [];

    /// <summary>The items of the drop down of the shell button.</summary>
    public IReadOnlyList<BrowseMenuItem> ShellItems
        => [.. Shells.Select(shell => new BrowseMenuItem(shell.Name.Replace("_", "__"), null, shell.Icon) { Invoke = () => RunShell(shell) })];

    /// <summary>As the click of <c>userShell</c>: the default shell.</summary>
    public void RunDefaultShell()
    {
        if (DefaultShell is { } shell)
        {
            RunShell(shell);
        }
    }

    private void RunShell(BrowseShell shell) => (_host as IBrowseToolbarHost)?.RunShell(shell);

    /// <summary>The items of the submodules button (<c>toolStripButtonLevelUp</c>).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<BrowseMenuItem> SubmoduleItems { get; private set; } = [];

    /// <summary>The image of the submodules button: up to the superproject, or the submodules.</summary>
    public string SubmodulesIcon => (_host as IBrowseToolbarHost)?.HasSuperproject is true ? "NavigateUp" : "SubmodulesManage";

    public string SubmodulesToolTip => (_host as IBrowseToolbarHost)?.HasSuperproject is true ? ToolbarStrings.GoToSuperProject.Text : ToolbarStrings.SubmodulesToolTip.Text;

    public bool CanShowSubmodules { get; private set; }

    /// <summary>Raised when the drop down of the submodules button should open (a click without a superproject).</summary>
    public event EventHandler? SubmodulesMenuRequested;

    /// <summary>As <c>toolStripButtonLevelUp_ButtonClick</c>: to the superproject, else the drop down of the submodules.</summary>
    public void GoUpOrShowSubmodules()
    {
        if (_host is IBrowseToolbarHost { HasSuperproject: true } toolbarHost)
        {
            toolbarHost.GoToSuperproject();
            return;
        }

        SubmodulesMenuRequested?.Invoke(this, EventArgs.Empty);
    }

    private void InitializeToolbar()
    {
        IBrowseToolbarHost? toolbarHost = _host as IBrowseToolbarHost;
        string caption = toolbarHost?.WorkingDirectoryCaption ?? "";
        WorkingDirectoryText = string.IsNullOrWhiteSpace(caption) ? ToolbarStrings.NoWorkingFolder.Text : caption;
        if (toolbarHost is null)
        {
            return;
        }

        Shells = toolbarHost.GetShells();
        CanShowSubmodules = !IsDashboard && toolbarHost.CanShowSubmodules;
        SubmoduleItems = [new BrowseMenuItem(ToolbarStrings.Loading.Text, null) { IsEnabled = false }];
        toolbarHost.SubmoduleMenuChanged += (_, items) => SubmoduleItems = items ?? [new BrowseMenuItem(ToolbarStrings.Loading.Text, null) { IsEnabled = false }];
        ScriptItems = IsDashboard ? [] : toolbarHost.GetToolbarScripts();
        if (!IsDashboard)
        {
            _ = LoadWorktreesAsync(toolbarHost);
        }
    }

    // As UpdateWorktreeToolStripVisibility.
    private async Task LoadWorktreesAsync(IBrowseToolbarHost toolbarHost)
    {
        try
        {
            BrowseWorktreeMenu menu = await toolbarHost.GetWorktreeMenuAsync();
            WorktreeItems = menu.Items;
            ShowWorktrees = menu.Count > 1;
        }
        catch (Exception)
        {
            // As the button without worktrees (e.g. git failed): hidden.
            ShowWorktrees = false;
        }
    }
}
