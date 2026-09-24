using GitCommands;
using GitCommands.Git;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs.RepoHosting;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.RepoHosting;
using GitUI.NBugReports;
using GitUI.Presentation.CommandsDialogs.RepoHosting;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces.RepositoryHosts;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the repository hosting dialogs of the hosting plugins (GitHub), docs/avalonia-port/PLAN.md, phase 7.</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>Shows the Avalonia port of <c>ForkAndCloneForm</c> (modal, as <c>StartCloneForkFromHoster</c>).</summary>
    public static bool TryShowForkAndClone(IWin32Window? owner, IGitUICommands commands, IRepositoryHostPlugin gitHoster, EventHandler<GitModuleEventArgs>? gitModuleChanged)
    {
        ShowDialog(
            () =>
            {
                ForkAndCloneWindow window = new();
                window.DataContext = new ForkAndCloneViewModel(
                    ViewStrings.Load<ForkAndCloneStrings>(),
                    gitHoster,
                    new ForkAndCloneHost(commands, window, gitModuleChanged),
                    AvaloniaPluginDialogs.BackgroundRunner,
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                return window;
            },
            owner);
        return true;
    }

    /// <summary>Shows the Avalonia port of <c>ViewPullRequestsForm</c> (modeless, as <c>StartPullRequestsDialog</c>).</summary>
    public static bool TryShowPullRequests(IWin32Window? owner, IGitUICommands commands, IRepositoryHostPlugin gitHoster)
    {
        AvaloniaPluginDialogs.Show(
            () =>
            {
                ViewPullRequestsWindow window = new();
                ViewPullRequestsViewModel viewModel = new(
                    ViewStrings.Load<ViewPullRequestsStrings>(),
                    gitHoster,
                    new ViewPullRequestsHost(commands, window),
                    new FileViewerHost(commands),
                    ViewStrings.Load<FileStatusListStrings>(),
                    GetFileStatusTreeOptions(),
                    AvaloniaPluginDialogs.BackgroundRunner,
                    new MessageBoxService(window),
                    new SpellCheckHost(commands));
                UseFileStatusListMenu(viewModel.Files, commands, window);
                window.DataContext = viewModel;
                return window;
            },
            owner,
            positionName: "ViewPullRequestsForm",
            showInTaskbar: true);
        return true;
    }

    /// <summary>Shows the Avalonia port of <c>CreatePullRequestForm</c> (modeless, as <c>StartCreatePullRequest</c>).</summary>
    public static bool TryShowCreatePullRequest(IWin32Window? owner, IGitUICommands commands, IRepositoryHostPlugin gitHoster, string? chooseRemote)
    {
        AvaloniaPluginDialogs.Show(
            () =>
            {
                CreatePullRequestWindow window = new();
                window.DataContext = new CreatePullRequestViewModel(
                    ViewStrings.Load<CreatePullRequestStrings>(),
                    gitHoster,
                    chooseRemote,
                    new CreatePullRequestHost(commands.Module),
                    AvaloniaPluginDialogs.BackgroundRunner,
                    new MessageBoxService(window),
                    new SpellCheckHost(commands));
                return window;
            },
            owner,
            positionName: "CreatePullRequestForm",
            showInTaskbar: true);
        return true;
    }

    private sealed class ForkAndCloneHost(IGitUICommands commands, DialogWindow window, EventHandler<GitModuleEventArgs>? gitModuleChanged) : IForkAndCloneHost
    {
        private NativeWindowOwner Owner => new(window);

        /// <summary>As <c>ForkAndCloneForm.Init</c>.</summary>
        public string? GetDefaultDestination()
        {
            if (!string.IsNullOrEmpty(AppSettings.DefaultCloneDestinationPath))
            {
                return AppSettings.DefaultCloneDestinationPath;
            }

            IList<Repository> repositoryHistory = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Locals.LoadRecentHistoryAsync);
            Repository? lastRepo = repositoryHistory.Count > 0 ? repositoryHistory[0] : null;
            return string.IsNullOrEmpty(lastRepo?.Path) ? null : Path.GetDirectoryName(lastRepo.Path.Trim('/', '\\'));
        }

        /// <summary>As <c>ForkAndCloneForm.Clone</c>.</summary>
        public bool Clone(string cloneUrl, string targetDirectory, int? depth) => AvaloniaUi.RunInHostContext(() =>
        {
            ArgumentString cmd = Commands.Clone(cloneUrl, targetDirectory, commands.Module.GetPathForGitExecution, depth: depth);
            return !RunRemoteProcess(Owner, commands, cmd, remote: cloneUrl).ErrorOccurred;
        });

        public string AddRemote(string repositoryDirectory, string name, string url)
        {
            GitModule module = new(commands.GetRequiredService<IGitExecutorProvider>(), repositoryDirectory);
            return module.AddRemote(name, url);
        }

        public void OpenRepository(string repositoryDirectory) => AvaloniaUi.RunInHostContext(() =>
        {
            GitModule module = new(commands.GetRequiredService<IGitExecutorProvider>(), repositoryDirectory);
            gitModuleChanged?.Invoke(Owner, new GitModuleEventArgs(module));
        });

        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));
    }

    private sealed class CreatePullRequestHost(IGitModule module) : ICreatePullRequestHost
    {
        public string? GetLastCommitSubject(string revision)
            => module.GetPreviousCommitMessages(count: 1, revision: revision, authorPattern: string.Empty).FirstOrDefault();

        /// <summary>As <c>CreatePullRequestForm.LoadPRTemplate</c>.</summary>
        public async Task<string?> LoadTemplateAsync()
        {
            string templatePath = Path.Join(module.WorkingDir, ".github", "PULL_REQUEST_TEMPLATE.md");
            return File.Exists(templatePath) ? await File.ReadAllTextAsync(templatePath) : null;
        }

        public void ReportTemplateError(Exception exception) => AvaloniaUi.RunInHostContext(() =>
        {
            string templatePath = Path.Join(module.WorkingDir, ".github", "PULL_REQUEST_TEMPLATE.md");
            BugReportInvoker.Report(
                new UserExternalOperationException(
                    ViewStrings.Load<CreatePullRequestStrings>().FailedToLoadTemplate.Text,
                    new ExternalOperationException(arguments: templatePath, workingDirectory: module.WorkingDir, innerException: exception)),
                isTerminating: false);
        });
    }

    private sealed class ViewPullRequestsHost(IGitUICommands commands, DialogWindow window) : IViewPullRequestsHost
    {
        private NativeWindowOwner Owner => new(window);

        private IGitModule Module => commands.Module;

        public string GetCurrentRemote() => Module.GetCurrentRemote();

        /// <summary>As <c>SelectHostedRepositoryForCurrentRemote</c>.</summary>
        public GitProtocol GetCloneProtocol(string currentRemote)
            => ThreadHelper.JoinableTaskFactory.Run(Module.GetRemotesAsync)
                .First(r => string.IsNullOrEmpty(currentRemote) || r.Name == currentRemote).FetchUrl.IsUrlUsingHttp() ? GitProtocol.Https : GitProtocol.Ssh;

        public bool RunGit(string arguments)
            => AvaloniaUi.RunInHostContext(() => ProcessDialogs.ShowProcess(Owner, commands, arguments: arguments, Module.WorkingDir, input: null, useDialogSettings: true));

        public string AddRemote(string name, string url) => Module.AddRemote(name, url);

        public void LockRepoChanged() => commands.RepoChangedNotifier.Lock();

        public void UnlockRepoChanged() => AvaloniaUi.RunInHostContext(() => commands.RepoChangedNotifier.UnLock(false));

        public void NotifyRepoChanged() => AvaloniaUi.RunInHostContext(commands.RepoChangedNotifier.Notify);
    }
}
