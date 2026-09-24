using System.Text.RegularExpressions;
using GitCommands;
using GitCommands.Git;
using GitCommands.Remotes;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Infrastructure;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.ScriptsEngine;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the pull dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  The Avalonia port of <c>FormPull</c>: shows the dialog, or (<paramref name="pullOnShow"/>) pulls without it and shows
    ///  it only if nothing was pulled, as <c>FormPull.PullAndShowDialogWhenFailed</c>.
    /// </summary>
    /// <param name="accepted">Whether the pull ran (<c>DialogResult.OK</c>).</param>
    /// <param name="errorOccurred">Whether the pull failed (<c>FormPull.ErrorOccurred</c>).</param>
    public static bool TryShowPull(
        IWin32Window? owner,
        IGitUICommands commands,
        string? remoteBranch,
        string? remote,
        GitPullAction pullAction,
        bool pullOnShow,
        out bool accepted,
        out bool errorOccurred)
    {
        accepted = false;
        errorOccurred = false;
        IGitModule module = commands.Module;
        PullOptions options = new(
            SelectedBranch: module.GetSelectedBranch(),
            DefaultRemoteBranch: remoteBranch,
            DefaultRemote: remote,
            PullAction: pullAction,
            DefaultPullAction: AppSettings.DefaultPullAction,
            AutoStash: AppSettings.AutoStash,

            // Detect a shallow repository by the presence of the shallow file (created by a shallow clone, removed when unshallowing).
            IsShallow: File.Exists(module.ResolveGitInternalPath("shallow")),
            WorkingDirDisplayPath: PathUtil.GetDisplayPath(module.WorkingDir),
            ErrorCaption: TranslatedStrings.Error);

        // Git and message boxes are owned by the caller when pulling without the dialog, by the dialog otherwise.
        IWin32Window? currentOwner = owner;
        DialogWindow? currentWindow = null;
        PullStrings strings = ViewStrings.Load<PullStrings>();
        PullViewModel viewModel = new(
            strings,
            options,
            CreateHelpImage("Pull"),
            new PullHost(commands, strings, () => currentOwner, () => currentWindow),
            new OwnerMessageBoxService(() => currentOwner));

        PullOutcome outcome = PullOutcome.NotPulled;
        if (pullOnShow)
        {
            outcome = viewModel.PullWithoutDialog(remote, pullAction);
        }

        if (outcome == PullOutcome.NotPulled)
        {
            bool pulled = ShowDialog(
                () =>
                {
                    PullWindow window = new() { DataContext = viewModel };
                    currentWindow = window;
                    currentOwner = new NativeWindowOwner(window);
                    return window;
                },
                owner);
            outcome = pulled ? PullOutcome.Pulled : PullOutcome.Cancelled;
        }

        accepted = outcome == PullOutcome.Pulled;
        errorOccurred = viewModel.ErrorOccurred;
        return true;
    }

    [GeneratedRegex(@"Your configuration specifies to .* the ref '.*'[\r]?\nfrom the remote, but no such ref was fetched.", RegexOptions.ExplicitCapture)]
    private static partial Regex PullRefRemovedRegex { get; }

    private sealed class PullHost(IGitUICommands commands, PullStrings strings, Func<IWin32Window?> owner, Func<DialogWindow?> window) : IPullHost
    {
        private IGitModule Module => commands.Module;

        public IReadOnlyList<string> LoadRemotes()
            => [.. new ConfigFileRemoteSettingsManager(() => Module).LoadRemotes(false).Select(remote => remote.Name ?? "")];

        public string GetSetting(string name) => Module.GetSetting(name);

        public IReadOnlyList<PullRef> GetRefs(bool remotes)
            => [.. Module.GetRefs(remotes ? RefsFilter.Remotes : RefsFilter.Heads).Select(head => new PullRef(head.Name, head.LocalName))];

        public IReadOnlyList<string> LoadUrlHistory()
            => [.. ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Remotes.LoadRecentHistoryAsync).Select(r => r.Path)];

        public void AddLocalSourceToHistory(string path)
        {
            if (Directory.Exists(path))
            {
                ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistoryManager.Remotes.AddAsMostRecentAsync(path));
            }
        }

        /// <summary>As <c>FormPull.LoadPuttyKey</c>.</summary>
        public void LoadPuttyKeys(IReadOnlyList<string> remotes) => AvaloniaUi.RunInHostContext(() =>
        {
            if (!GitSshHelpers.IsPlink)
            {
                return;
            }

            HashSet<string> files = new(new PathEqualityComparer());
            foreach (string remote in remotes)
            {
                string sshKeyFile = Module.GetPuttyKeyFileForRemote(remote);
                if (!string.IsNullOrEmpty(sshKeyFile))
                {
                    files.Add(sshKeyFile);
                }
            }

            foreach (string sshKeyFile in files)
            {
                PuttyHelpers.StartPageantIfConfigured(() => sshKeyFile);
            }
        });

        public async Task<string?> PickFolderAsync(string? startDirectory)
            => window() is DialogWindow dialog ? await new AvaloniaFileDialogService(dialog).PickFolderAsync(startDirectory) : null;

        public bool IsDetachedHead() => Module.IsDetachedHead();

        public bool ExistsMergeCommit(string remoteBranch, string branch) => Module.ExistsMergeCommit(remoteBranch, branch);

        public string GetNameRev(string name)
        {
            GitArgumentBuilder args = new("name-rev")
            {
                "--name-only",
                name.QuoteNE()
            };
            return Module.GitExecutable.GetOutput(args).Trim();
        }

        public void SaveSettings(GitPullAction pullAction, bool autoStash)
        {
            AppSettings.FormPullAction = pullAction;
            AppSettings.AutoStash = autoStash;
        }

        public bool? ConfirmRebaseMergeCommit(string text, string caption) => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.Show(owner(), text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) switch
            {
                DialogResult.Yes => true,
                DialogResult.No => (bool?)false,
                _ => null,
            });

        public DetachedHeadPullChoice AskPullOnDetachedHead(string text) => AvaloniaUi.RunInHostContext(() =>
        {
            TaskDialogPage page = new()
            {
                Text = text,
                Heading = TranslatedStrings.ErrorInstructionNotOnBranch,
                Caption = TranslatedStrings.ErrorCaptionNotOnBranch,
                Buttons = { TaskDialogButton.Cancel },
                Icon = TaskDialogIcon.Error,
                AllowCancel = true,
                SizeToContent = true
            };
            TaskDialogCommandLinkButton btnCheckout = new(TranslatedStrings.ButtonCheckoutBranch);
            TaskDialogCommandLinkButton btnContinue = new(TranslatedStrings.ButtonContinue);
            page.Buttons.Add(btnCheckout);
            page.Buttons.Add(btnContinue);

            TaskDialogButton result = TaskDialog.ShowDialog(owner()?.Handle ?? 0, page);
            return result == TaskDialogButton.Cancel ? DetachedHeadPullChoice.Cancel
                : result == btnCheckout ? DetachedHeadPullChoice.CheckoutBranch
                : DetachedHeadPullChoice.Continue;
        });

        public bool StartCheckoutBranch() => AvaloniaUi.RunInHostContext(() => commands.StartCheckoutBranch(owner()));

        /// <summary>As the task dialogs of <c>FormPull.CalculateLocalBranch</c>.</summary>
        public bool ConfirmUseLocalBranch(string caption, string heading, string text, string commandLinkText, bool showDontShowAgain) => AvaloniaUi.RunInHostContext(() =>
        {
            TaskDialogPage page = new()
            {
                Text = text,
                Caption = caption,
                Heading = heading,
                Buttons = { TaskDialogButton.Cancel },
                Icon = TaskDialogIcon.Information,
                AllowCancel = false,
                SizeToContent = true
            };
            if (showDontShowAgain)
            {
                page.Verification = new TaskDialogVerificationCheckBox { Text = TranslatedStrings.DontShowAgain };
            }

            TaskDialogCommandLinkButton btnPullFrom = new(commandLinkText);
            page.Buttons.Add(btnPullFrom);

            return TaskDialog.ShowDialog(owner()?.Handle ?? 0, page) == btnPullFrom;
        });

        public bool ConfirmFetchAndPruneAll(string text, string caption) => AvaloniaUi.RunInHostContext(()
            => MessageBoxes.ConfirmSuppressible(owner(), text, caption, AppSettings.DontConfirmFetchAndPruneAll));

        /// <summary>As <c>executeBeforeScripts</c> of <c>FormPull.PullChanges</c>.</summary>
        public bool RunBeforeScripts(bool fetchOnly) => AvaloniaUi.RunInHostContext(() =>
        {
            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            OwnerScriptHost scriptHost = new(owner, commands);

            // Request to pull/merge in addition to the fetch
            if (!fetchOnly && !scriptsRunner.RunEventScripts(ScriptEvent.BeforePull, scriptHost))
            {
                return false;
            }

            return scriptsRunner.RunEventScripts(ScriptEvent.BeforeFetch, scriptHost);
        });

        /// <summary>As <c>executeAfterScripts</c> of <c>FormPull.PullChanges</c>.</summary>
        public void RunAfterScripts(bool fetchOnly) => AvaloniaUi.RunInHostContext(() =>
        {
            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            OwnerScriptHost scriptHost = new(owner, commands);
            scriptsRunner.RunEventScripts(ScriptEvent.AfterFetch, scriptHost);

            // Request to pull/merge in addition to the fetch
            if (!fetchOnly)
            {
                scriptsRunner.RunEventScripts(ScriptEvent.AfterPull, scriptHost);
            }
        });

        public bool HasChangesToStash()
            => !Module.IsBareRepository() && Module.GitStatus(UntrackedFilesMode.No, IgnoreSubmodulesMode.All).Count > 0;

        public void StashSave() => AvaloniaUi.RunInHostContext(() => commands.StashSave(owner(), AppSettings.IncludeUntrackedFilesInAutoStash));

        /// <summary>As <c>FormPull.CreateFormProcess</c> and the process part of <c>PullChanges</c>.</summary>
        public PullProcessResult RunPull(PullCommand command) => AvaloniaUi.RunInHostContext(() =>
        {
            ArgumentString arguments = command.Fetch
                ? Module.FetchCmd(command.Source, command.RemoteBranch, command.LocalBranch, command.FetchTags, command.Unshallow, command.Prune, command.PruneTags)
                : Module.PullCmd(command.Source, command.RemoteBranch, command.Rebase, command.FetchTags, command.Unshallow);
            RemoteProcessResult result = RunRemoteProcess(
                owner(),
                commands,
                arguments,
                remote: command.IsPullAll ? null : command.Source,
                onExit: command.Fetch ? null : HandlePullOnExit);
            return new PullProcessResult(Aborted: result.Aborted, ErrorOccurred: result.ErrorOccurred);

            bool HandlePullOnExit(ref bool isError, IRemoteProcessDialog process)
            {
                if (!isError || string.IsNullOrEmpty(command.PruneRemote))
                {
                    return false;
                }

                // auto pull only if current branch was rejected
                if (PullRefRemovedRegex.IsMatch(process.GetOutputString()))
                {
                    TaskDialogPage page = new()
                    {
                        Text = strings.PruneBranchesBranch.Text,
                        Caption = strings.PruneBranchesCaption.Text,
                        Heading = strings.PruneBranchesMainInstruction.Text,
                        Buttons = { TaskDialogButton.Yes, TaskDialogButton.No, TaskDialogButton.Cancel },
                        Icon = TaskDialogIcon.Information,
                        DefaultButton = TaskDialogButton.No,
                        SizeToContent = true
                    };
                    if (TaskDialog.ShowDialog(process.Handle, page) == TaskDialogButton.Yes)
                    {
                        string remote = command.PruneRemote;
                        RunRemoteProcess(process, commands, "remote prune " + remote, remote, string.Format(strings.PruneFromCaption.Text, remote));
                    }
                }

                return false;
            }
        });

        public bool HasSubmodules() => File.Exists(new FullPathResolver(() => Module.WorkingDir).Resolve(".gitmodules"));

        /// <summary>The fast submodules check of <c>InitModules</c>.</summary>
        public bool AreSubmodulesInitialized()
            => Module.GetSubmodulesLocalPaths()
                .Select(submoduleName => Module.GetSubmodule(submoduleName))
                .All(submodule => submodule.IsValidGitWorkingDir());

        public bool? UpdateSubmodulesWithoutAsking => AppSettings.UpdateSubmodulesOnCheckout ?? AppSettings.DontConfirmUpdateSubmodulesOnCheckout;

        public void StartUpdateSubmodulesDialog() => AvaloniaUi.RunInHostContext(() => commands.StartUpdateSubmodulesDialog(owner()));

        public void UpdateSubmodules() => AvaloniaUi.RunInHostContext(() => commands.UpdateSubmodules(owner()));

        public bool IsInTheMiddleOfRebase() => Module.InTheMiddleOfRebase();

        public bool IsInTheMiddleOfAction() => Module.InTheMiddleOfAction();

        public bool StartContinueRebaseDialog() => AvaloniaUi.RunInHostContext(() => commands.StartTheContinueRebaseDialog(owner()));

        public bool HandleMergeConflicts() => AvaloniaUi.RunInHostContext(() => MergeConflictHandler.HandleMergeConflicts(commands, owner()));

        /// <summary>As the question of <c>PopStash</c> in <c>FormPull.PullChanges</c>.</summary>
        public bool ConfirmApplyStash() => AvaloniaUi.RunInHostContext(() =>
        {
            bool? applyStash = AppSettings.AutoPopStashAfterPull;
            if (applyStash is not null)
            {
                return applyStash.Value;
            }

            TaskDialogPage page = new()
            {
                Text = strings.ApplyStashedItemsAgain.Text,
                Caption = strings.ApplyStashedItemsAgainCaption.Text,
                Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
                Icon = TaskDialogIcon.Information,
                Verification = new TaskDialogVerificationCheckBox { Text = TranslatedStrings.DontShowAgain },
                AllowCancel = true,
                SizeToContent = true
            };

            TaskDialogButton answer = TaskDialog.ShowDialog(owner()?.Handle ?? 0, page);
            applyStash = answer == TaskDialogButton.Yes;

            // Dismissing the dialog is not an answer, so it must not be remembered as one
            if (page.Verification.Checked && answer != TaskDialogButton.Cancel)
            {
                AppSettings.AutoPopStashAfterPull = applyStash;
            }

            return applyStash.Value;
        });

        public void StashPop() => AvaloniaUi.RunInHostContext(() => commands.StashPop(owner()));

        public void StartRemotesDialog(string? remote) => AvaloniaUi.RunInHostContext(() => commands.StartRemotesDialog(owner(), remote));

        public void StartStashDialog() => AvaloniaUi.RunInHostContext(() => commands.StartStashDialog(owner()));

        public Task RunMergeToolAsync()
        {
            IGitModule module = Module;
            return Task.Run(() => module.RunMergeTool());
        }

        public void StartCommitDialog() => AvaloniaUi.RunInHostContext(() => commands.StartCommitDialog(owner()));
    }
}
