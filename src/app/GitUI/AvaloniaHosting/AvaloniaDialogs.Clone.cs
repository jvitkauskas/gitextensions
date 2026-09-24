using GitCommands;
using GitCommands.Config;
using GitCommands.Git;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the clone dialog (docs/avalonia-port/PLAN.md, phase 2, batch 6).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowClone(IWin32Window? owner, IGitUICommands commands, string? url, bool openedFromProtocolHandler, EventHandler<GitModuleEventArgs>? gitModuleChanged)
    {
        IList<Repository> remotesHistory = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Remotes.LoadRecentHistoryAsync);
        IList<Repository> localsHistory = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Locals.LoadRecentHistoryAsync);
        IReadOnlyList<string> recentDestinations = [.. localsHistory.Select(x => x.GetParentPath())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)];
        (string from, string destination) = GetCloneDefaults(commands.Module, url);

        using CloneHost host = new(commands, openedFromProtocolHandler, gitModuleChanged, owner);
        ShowDialog(
            () =>
            {
                CloneWindow window = new();
                host.Window = window;
                window.DataContext = new CloneViewModel(
                    ViewStrings.Load<CloneStrings>(),
                    [.. remotesHistory.Select(r => r.Path)],
                    recentDestinations,
                    from,
                    destination,
                    canLoadSshKey: GitSshHelpers.IsPlink,
                    host,
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                return window;
            },
            owner);
        return true;
    }

    /// <summary>The repository and destination the clone dialog suggests, as <c>FormClone.OnRuntimeLoad</c>.</summary>
    private static (string From, string Destination) GetCloneDefaults(IGitModule module, string? url)
    {
        string from = "";
        string destination = AppSettings.DefaultCloneDestinationPath;

        if (PathUtil.CanBeGitURL(url))
        {
            from = url ?? "";
        }
        else
        {
            if (!string.IsNullOrEmpty(url) && Directory.Exists(url))
            {
                destination = url;
            }

            // Try to be more helpful to the user: use the clipboard text as a potential source URL.
            try
            {
                if (ClipboardUtil.TryGetText(out string? clipboardText) && TryExtractUrl(clipboardText, out string possibleUrl))
                {
                    from = possibleUrl;
                }
            }
            catch
            {
                // We tried.
            }

            // If the source is empty, use the remote URL of the current repository, hoping that the repository
            // to clone is hosted on the same server.
            if (string.IsNullOrWhiteSpace(from) && module.IsValidGitWorkingDir())
            {
                string? currentBranchRemote = module.GetSetting(string.Format(SettingKeyString.BranchRemote, module.GetSelectedBranch()));
                if (string.IsNullOrEmpty(currentBranchRemote))
                {
                    IReadOnlyList<string> remotes = module.GetRemoteNames();
                    currentBranchRemote = remotes.Any(s => s.Equals("origin", StringComparison.InvariantCultureIgnoreCase))
                        ? "origin"
                        : remotes.Count > 0 ? remotes[0] : null;
                }

                string pushUrl = module.GetSetting(string.Format(SettingKeyString.RemotePushUrl, currentBranchRemote));
                if (string.IsNullOrEmpty(pushUrl))
                {
                    pushUrl = module.GetSetting(string.Format(SettingKeyString.RemoteUrl, currentBranchRemote));
                }

                from = pushUrl;

                try
                {
                    // Suggest the parent of the current working directory as the destination.
                    if (!string.IsNullOrWhiteSpace(pushUrl) && string.IsNullOrWhiteSpace(destination) && !string.IsNullOrWhiteSpace(module.WorkingDir))
                    {
                        destination = Path.GetDirectoryName(module.WorkingDir.TrimEnd(Path.DirectorySeparatorChar)) ?? "";
                    }
                }
                catch
                {
                    // Exceptions on setting the destination directory can be ignored.
                }
            }
        }

        // Without a destination, clone next to the current repository.
        if (string.IsNullOrWhiteSpace(destination) && !string.IsNullOrWhiteSpace(module.WorkingDir))
        {
            if (module.IsValidGitWorkingDir())
            {
                if (Path.GetPathRoot(module.WorkingDir) != module.WorkingDir)
                {
                    destination = Path.GetDirectoryName(module.WorkingDir.TrimEnd(Path.DirectorySeparatorChar)) ?? "";
                }
            }
            else
            {
                destination = module.WorkingDir;
            }
        }

        return (from ?? "", destination ?? "");
    }

    private sealed class CloneHost(
        IGitUICommands commands,
        bool openedFromProtocolHandler,
        EventHandler<GitModuleEventArgs>? gitModuleChanged,
        IWin32Window? caller) : ICloneHost, IDisposable
    {
        private readonly CancellationTokenSequence _branchLoaderSequence = new();

        public DialogWindow? Window { get; set; }

        private IWin32Window? Owner => Window is null ? caller : new NativeWindowOwner(Window);

        public void LoadBranches(string from, Action<IReadOnlyList<string>> report)
        {
            // As FormClone.LoadBranches and UpdateBranches.
            IGitModule module = commands.Module;
            CancellationToken cancellationToken = _branchLoaderSequence.Next();
            ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                IReadOnlyList<IGitRef> refs = module.GetRemoteServerRefs(from, false, true, out string? errorOutput, cancellationToken);
                bool authenticationFail = !string.IsNullOrEmpty(errorOutput) && errorOutput.Contains("FATAL ERROR") && errorOutput.Contains("authentication");
                bool hostKeyFail = !string.IsNullOrEmpty(errorOutput) && errorOutput.Contains("the server's host key is not cached in the registry", StringComparison.InvariantCultureIgnoreCase);
                if (!string.IsNullOrEmpty(errorOutput) && !authenticationFail && !hostKeyFail)
                {
                    throw new ExternalOperationException(workingDirectory: module.WorkingDir, innerException: new Exception(errorOutput));
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (hostKeyFail)
                {
                    if (AvaloniaUi.RunInHostContext(() => ProcessDialogs.AskForCacheHostkey(Owner!, from)))
                    {
                        LoadBranches(from, report);
                    }
                }
                else if (authenticationFail)
                {
                    if (AvaloniaUi.RunInHostContext(() => TryShowPuttyError(Owner!, out bool retry, out _) && retry))
                    {
                        LoadBranches(from, report);
                    }
                }
                else
                {
                    report([.. refs.Select(branch => branch.LocalName)]);
                }
            });
        }

        public void CancelLoadingBranches() => _branchLoaderSequence.CancelCurrent();

        public string? BrowseAndLoadSshKey() => AvaloniaUi.RunInHostContext(() => BrowseForPrivateKey.BrowseAndLoad(Owner!));

        public bool Clone(CloneRequest request) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormClone.OkClick, after the validation (done by the view model).
            CloneStrings strings = ViewStrings.Load<CloneStrings>();
            try
            {
                string dirTo = request.Destination;
                if (!Directory.Exists(dirTo))
                {
                    Directory.CreateDirectory(dirTo);
                }

                // Use a commands instance rooted at the destination so that path conversion and git-executable
                // selection (Windows vs WSL) match the destination directory, not the currently open module.
                IGitUICommands destUICommands = commands.WithWorkingDirectory(dirTo);
                ArgumentString cloneCmd = Commands.Clone(
                    request.From,
                    dirTo,
                    destUICommands.Module.GetPathForGitExecution,
                    request.Central,
                    request.InitializeSubmodules,
                    request.Branch,
                    request.Depth,
                    request.SingleBranch);
                string sourceRepo = PathUtil.IsLocalFile(request.From)
                    ? destUICommands.Module.GetPathForGitExecution(request.From) ?? request.From
                    : request.From;
                if (RunRemoteProcess(Owner, destUICommands, cloneCmd, urlTryingToConnect: sourceRepo).ErrorOccurred || commands.Module.InTheMiddleOfPatch())
                {
                    return false;
                }

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await RepositoryHistoryManager.Remotes.AddAsMostRecentAsync(request.From);
                    await RepositoryHistoryManager.Locals.AddAsMostRecentAsync(dirTo);
                });

                if (!string.IsNullOrEmpty(request.PuttySshKey))
                {
                    GitModule clonedGitModule = new(commands.GetRequiredService<IGitExecutorProvider>(), dirTo);
                    clonedGitModule.SetSetting(string.Format(SettingKeyString.RemotePuttySshKey, "origin"), request.PuttySshKey);
                }

                if (openedFromProtocolHandler && AskIfNewRepositoryShouldBeOpened(strings, dirTo))
                {
                    Window?.Hide();
                    commands.WithWorkingDirectory(dirTo).StartBrowseDialog(owner: null);
                }
                else if (!openedFromProtocolHandler && gitModuleChanged is not null && AskIfNewRepositoryShouldBeOpened(strings, dirTo))
                {
                    gitModuleChanged(caller, new GitModuleEventArgs(new GitModule(commands.GetRequiredService<IGitExecutorProvider>(), dirTo)));
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBoxes.Show(Owner, "Exception: " + ex.Message, strings.ErrorCloneFailed.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        });

        private bool AskIfNewRepositoryShouldBeOpened(CloneStrings strings, string dirTo)
            => MessageBoxes.Show(Owner, string.Format(strings.QuestionOpenRepo.Text, dirTo), strings.QuestionOpenRepoCaption.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        public void Dispose() => _branchLoaderSequence.Dispose();
    }

    // Moved out of FormClone.

    /// <summary>
    /// Check whether the given string contains one or more valid git URLs and extracts
    /// the first URL that exists, if any.
    /// </summary>
    /// <remarks>
    /// PathUtil.CanBeGitURL is used as a standard way to detect a git URL.
    /// The first URL extracted from <paramref name="contents"/> is assigned to
    /// <paramref name="url"/>. If <paramref name="contents"/> contains more than one URL,
    /// subsequent URLs are not extracted.
    /// </remarks>
    /// <param name="contents">A string to attempt to extract URLs from.</param>
    /// <param name="url">A <see cref="string"/> that contains the URL, if any, extracted from <paramref name="contents"/>.</param>
    /// <returns><see langword="true"/> if a URL was extracted; otherwise <see langword="false"/>.</returns>
    internal static bool TryExtractUrl(string contents, out string url)
    {
        url = "";

        if (string.IsNullOrEmpty(contents))
        {
            return false;
        }

        string[] parts = contents.Split(' ');
        foreach (string s in parts)
        {
            if (PathUtil.CanBeGitURL(s))
            {
                url = s;
                break;
            }
        }

        return !string.IsNullOrEmpty(url);
    }
}
