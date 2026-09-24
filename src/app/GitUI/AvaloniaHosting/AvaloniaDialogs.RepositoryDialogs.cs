using GitCommands;
using GitCommands.Git;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of repository dialogs without heavy controls (docs/avalonia-port/PLAN.md, phase 2, batch 2).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowAddSubmodule(IWin32Window? owner, IGitUICommands commands)
    {
        IList<Repository> history = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Remotes.LoadRecentHistoryAsync);

        ShowDialog(
            () =>
            {
                AddSubmoduleWindow window = new();
                window.DataContext = new AddSubmoduleViewModel(
                    ViewStrings.Load<AddSubmoduleStrings>(),
                    [.. history.Select(r => r.Path)],
                    url => [.. LoadRemoteRepoBranches(commands.Module.GitExecutable, url)],
                    arguments => AvaloniaUi.RunInHostContext(() => ProcessDialogs.ShowProcess(
                        new NativeWindowOwner(window), commands, arguments, commands.Module.WorkingDir, input: null, useDialogSettings: true)),
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                return window;
            },
            owner,
            positionName: "FormAddSubmodule");
        return true;
    }

    public static bool TryShowCleanupRepository(IWin32Window? owner, IGitUICommands commands, string? path)
    {
        IGitModule module = commands.Module;
        ShowDialog(
            () =>
            {
                CleanupRepositoryWindow window = new();
                window.DataContext = new CleanupRepositoryViewModel(
                    ViewStrings.Load<CleanupRepositoryStrings>(),
                    module.WorkingDir,
                    module.WorkingDirGitDir,
                    path,
                    arguments => AvaloniaUi.RunInHostContext(() => ProcessDialogs.ReadProcess(
                        new NativeWindowOwner(window), commands, arguments, module.WorkingDir, input: null, useDialogSettings: true)),
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                return window;
            },
            owner,
            positionName: "FormCleanupRepository");
        return true;
    }

    public static bool TryShowMergeSubmodule(IWin32Window? owner, IGitUICommands commands, string filename, out bool accepted)
    {
        accepted = false;
        ConflictData conflict = ThreadHelper.JoinableTaskFactory.Run(() => commands.Module.GetConflictAsync(filename));

        accepted = ShowDialog(
            () =>
            {
                MergeSubmoduleWindow window = new();
                MergeSubmoduleStrings strings = ViewStrings.Load<MergeSubmoduleStrings>();
                window.DataContext = new MergeSubmoduleViewModel(
                    strings,
                    filename,
                    ToText(conflict.Base.ObjectId),
                    ToText(conflict.Local.ObjectId),
                    ToText(conflict.Remote.ObjectId),
                    new MergeSubmoduleHost(window, commands, filename, strings));
                return window;
            },
            owner,
            positionName: "FormMergeSubmodule");
        return true;

        static string? ToText(ObjectId? id) => id is { IsZero: false } value ? value.ToString() : null;
    }

    /// <param name="worktreeDirectory">The directory of the created worktree, <see langword="null"/> if none was created.</param>
    public static bool TryShowCreateWorktree(IWin32Window? owner, IGitUICommands commands, string? mainWorktreePath, out string? worktreeDirectory)
    {
        worktreeDirectory = null;
        IGitModule module = commands.Module;
        CreateWorktreeViewModel? viewModel = null;
        bool created = ShowDialog(
            () =>
            {
                CreateWorktreeWindow window = new();
                viewModel = new CreateWorktreeViewModel(
                    ViewStrings.Load<CreateWorktreeStrings>(),
                    [.. module.GetRefs(RefsFilter.Heads).Select(r => r.Name)],
                    module.GetSelectedBranch(),
                    mainWorktreePath,
                    commands.GetRequiredService<IGitBranchNameNormaliser>(),
                    new GitBranchNameOptions(AppSettings.AutoNormaliseSymbol),
                    AppSettings.AutoNormaliseBranchName,
                    (directory, branchOption) => AvaloniaUi.RunInHostContext(() =>
                    {
                        string relativePath = Path.GetRelativePath(module.WorkingDir, directory).ToPosixPath().Quote();
                        return commands.StartGitCommandProcessDialog(
                            new NativeWindowOwner(window),
                            CreateWorktreeCommand(module, relativePath, branchOption));
                    }),
                    new AvaloniaFileDialogService(window));
                window.DataContext = viewModel;
                return window;
            },
            owner);

        if (created)
        {
            worktreeDirectory = viewModel!.WorktreeDirectory;
        }

        return true;
    }

    /// <summary>The Avalonia port of <c>FormOpenDirectory.OpenModule</c>.</summary>
    public static bool TryShowOpenDirectory(IWin32Window? owner, IGitExecutorProvider executorProvider, IGitModule? currentModule, out IGitModule? module)
    {
        module = null;
        IList<Repository> history = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Locals.LoadRecentHistoryAsync);
        IReadOnlyList<string> directories = OpenDirectoryViewModel.GetDirectories(
            AppSettings.DefaultCloneDestinationPath,
            currentModule?.WorkingDir,
            history.Select(r => r.Path),
            AppSettings.RecentWorkingDir,
            EnvironmentConfiguration.GetHomeDir());

        IGitModule? chosenModule = null;
        ShowDialog(
            () =>
            {
                OpenDirectoryWindow window = new();
                window.DataContext = new OpenDirectoryViewModel(
                    ViewStrings.Load<OpenDirectoryStrings>(),
                    directories,
                    TranslatedStrings.Error,
                    path =>
                    {
                        chosenModule = OpenGitRepository(executorProvider, path, RepositoryHistoryManager.Locals);
                        return chosenModule is not null;
                    },
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                return window;
            },
            owner);

        module = chosenModule;
        return true;
    }

    private sealed class MergeSubmoduleHost(DialogWindow window, IGitUICommands commands, string filename, MergeSubmoduleStrings strings) : IMergeSubmoduleHost
    {
        private IGitModule Module => commands.Module;

        public string? GetCurrentCheckout()
            => Module.GetSubmodule(filename).GetCurrentCheckout() is { IsZero: false } id ? id.ToString() : null;

        public void StageSubmodule()
        {
            // As FormMergeSubmodule.StageSubmodule.
            GitArgumentBuilder args = new("add")
            {
                "--",
                filename.QuoteNE()
            };
            string output = Module.GitExecutable.GetOutput(args);
            if (string.IsNullOrWhiteSpace(output))
            {
                return;
            }

            string text = string.Format(strings.StageFilename.Text, filename);
            AvaloniaUi.RunInHostContext(() => ProcessDialogs.ShowErrorDialog(new NativeWindowOwner(window), commands, text, text, output));
        }

        public void OpenSubmodule() => AvaloniaUi.RunInHostContext(() => GitUICommands.LaunchBrowse(workingDir: Module.GetSubmoduleFullPath(filename)));

        public bool CheckoutBranch(string localCommit, string remoteCommit) => AvaloniaUi.RunInHostContext(() =>
        {
            IGitUICommands submoduleCommands = commands.WithWorkingDirectory(Module.GetSubmoduleFullPath(filename));
            return submoduleCommands.StartCheckoutBranch(new NativeWindowOwner(window), [ObjectId.Parse(localCommit), ObjectId.Parse(remoteCommit)]);
        });
    }

    // Moved out of FormAddSubmodule.

    /// <summary>
    /// Returns the branches of a remote repository as strings; ignores git errors and warnings.
    /// </summary>
    /// 'git ls-remotes --heads "URL"' is completely independent from a local repo clone.
    /// Hence there is no need for a GitModule.
    /// <param name="gitExecutable">the git executable.</param>
    /// <param name="url">the repo URL; can also be a local path.</param>
    internal static IEnumerable<string> LoadRemoteRepoBranches(IExecutable gitExecutable, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return [];
        }

        GitArgumentBuilder gitArguments = new("ls-remote") { "--heads", url.ToPosixPath().Quote() };
        string heads = gitExecutable.GetOutput(gitArguments);
        return heads.LazySplit('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(head =>
                    {
                        int branchIndex = head.IndexOf(GitRefName.RefsHeadsPrefix);
                        return branchIndex == -1 ? null : head[(branchIndex + GitRefName.RefsHeadsPrefix.Length)..];
                    })
                    .WhereNotNull()
                    .ToList();
    }

    // Moved out of FormCreateWorktree.
    internal static GitArgumentBuilder CreateWorktreeCommand(IGitModule module, string relativePath, string newBranchOption)
    {
        // https://git-scm.com/docs/git-worktree

        // Get the default value, set if unset in config.
        // Similar in DiffHighlightService.
        const string command = "worktree";
        GitCommandConfiguration commandConfiguration = new();
        IReadOnlyList<GitConfigItem> items = GitCommandConfiguration.Default.Get(command);
        foreach (GitConfigItem cfg in items)
        {
            commandConfiguration.Add(cfg, command);
        }

        SetIfUnsetInGit("worktree.useRelativePaths", "true");
        GitArgumentBuilder args = new(command, commandConfiguration)
        {
            "add",
            relativePath,
            newBranchOption,
        };

        return args;

        void SetIfUnsetInGit(string key, string value)
        {
            if (string.IsNullOrEmpty(module.GetEffectiveSetting(key)))
            {
                commandConfiguration.Add(new GitConfigItem(key, value), command);
            }
        }
    }

    // Moved out of FormOpenDirectory.
    internal static IGitModule? OpenGitRepository(IGitExecutorProvider executorProvider, string path, ILocalRepositoryManager localRepositoryManager)
    {
        if (!Directory.Exists(path))
        {
            return null;
        }

        GitModule chosenModule = new(executorProvider, path.EnsureTrailingPathSeparator());
        if (!chosenModule.IsValidGitWorkingDir())
        {
            return null;
        }

        ThreadHelper.JoinableTaskFactory.Run(() => localRepositoryManager.AddAsMostRecentAsync(chosenModule.WorkingDir));
        return chosenModule;
    }
}
