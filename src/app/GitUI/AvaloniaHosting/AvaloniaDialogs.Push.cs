using System.Text.RegularExpressions;
using GitCommands;
using GitCommands.Config;
using GitCommands.Git;
using GitCommands.Remotes;
using GitCommands.Settings;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Infrastructure;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.ScriptsEngine;
using Microsoft;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the push dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  Shows the Avalonia port of <c>FormPush</c>; with <paramref name="pushOnShow"/>, pushes first and shows the dialog only
    ///  if the push fails (as <c>PushAndShowDialogWhenFailed</c>).
    /// </summary>
    /// <param name="pushed">Whether the dialog was accepted (as <c>DialogResult.OK</c>).</param>
    /// <param name="pushCompleted">Whether git pushed without an error (as <c>!ErrorOccurred</c>).</param>
    public static bool TryShowPush(IWin32Window? owner, IGitUICommands commands, bool pushOnShow, bool forceWithLease, string? branchName, out bool pushed, out bool pushCompleted)
    {
        pushed = false;
        pushCompleted = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormPush)))
        {
            return false;
        }

        AvaloniaUi.EnsureInitialized(GetOptions);
        PushWindow window = new();
        PushHost host = new(commands, window);
        PushViewModel viewModel = new(ViewStrings.Load<PushStrings>(), host, new MessageBoxService(window), branchName, forceWithLease);
        window.DataContext = viewModel;

        if (pushOnShow && viewModel.PushChanges())
        {
            pushed = true;
        }
        else
        {
            pushed = ShowDialog(() => window, owner, positionName: nameof(FormPush));
        }

        pushCompleted = pushed && !viewModel.ErrorOccurred;
        return true;
    }

    /// <summary>The repository, the settings and the dialogs of <c>FormPush</c>.</summary>
    internal sealed class PushHost(IGitUICommands commands, DialogWindow window) : IPushHost, IGitModuleForm, IScriptOptionsForm, IWin32Window
    {
        private readonly PushStrings _strings = ViewStrings.Load<PushStrings>();
        private readonly ConfigFileRemoteSettingsManager _remotesManager = new(() => commands.Module);

        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        public IGitUICommands UICommands => commands;

        public nint Handle => Owner.Handle;

        public string WorkingDirectory => Module.WorkingDir;

        public string CurrentBranch => Module.GetSelectedBranch();

        public bool IsBareRepository => Module.IsBareRepository();

        public bool IsAutoSetupMergeDisabled
            => Module.GetEffectiveSetting("branch.autosetupmerge") is { } autoSetupMerge && !string.IsNullOrWhiteSpace(autoSetupMerge) && autoSetupMerge.ToLowerInvariant() == "false";

        /// <summary>As <c>FormPushLoad</c>.</summary>
        public bool CanCreatePullRequest => PluginRegistry.TryGetGitHosterForModule(Module) is not null || HasAzureDevOpsRemote();

        public int RecursiveSubmodules { get => AppSettings.RecursiveSubmodules; set => AppSettings.RecursiveSubmodules = value; }

        public bool AlwaysShowAdvancedOptions => AppSettings.AlwaysShowAdvOpt;

        public bool DontConfirmAddTrackingReference => AppSettings.DontConfirmAddTrackingRef;

        public IReadOnlyList<IGitRef> GetRefs() => Module.GetRefs(RefsFilter.Heads | RefsFilter.Remotes);

        public IReadOnlyList<string> GetTags() => [.. Module.GetRefs(RefsFilter.Tags).Select(tag => tag.Name)];

        public IReadOnlyList<ConfigFileRemote> LoadRemotes() => [.. _remotesManager.LoadRemotes(false)];

        public string GetBranchRemote(string? branch) => Module.GetSetting(string.Format(SettingKeyString.BranchRemote, branch));

        public string? GetDefaultPushRemote(ConfigFileRemote remote, string branch) => _remotesManager.GetDefaultPushRemote(remote, branch);

        public IReadOnlyList<string> GetRecentUrls()
            => [.. ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Remotes.LoadRecentHistoryAsync).Select(r => r.Path)];

        public bool ManageRemotes(string? selectedRemote) => AvaloniaUi.RunInHostContext(() => commands.StartRemotesDialog(Owner, selectedRemote));

        public void Pull() => AvaloniaUi.RunInHostContext(() => commands.StartPullDialog(Owner));

        public void StartPageant(string? remote)
        {
            if (GitSshHelpers.IsPlink)
            {
                PuttyHelpers.StartPageantIfConfigured(() => Module.GetPuttyKeyFileForRemote(remote));
            }
        }

        public bool ConfirmNewBranchForRemote(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.ConfirmSuppressible(Owner, text, caption, AppSettings.DontConfirmPushNewBranch));

        /// <summary>As <c>LoadMultiBranchViewData</c> and its <c>ProcessHeads</c>.</summary>
        public IReadOnlyList<PushBranchRow>? LoadMultipleBranches(string remote) => AvaloniaUi.RunInHostContext(() =>
        {
            IReadOnlyList<IGitRef> remoteHeads;
            if (DetailedSettings.GetRemoteBranchesDirectlyFromRemote.ValueOrDefault(Module.GetEffectiveSettings()))
            {
                StartPageant(remote);
                RemoteProcessResult result = RunRemoteProcess(Owner, commands, $"ls-remote --heads \"{remote}\"", remote);
                if (result.ErrorOccurred)
                {
                    return null;
                }

                remoteHeads = Module.ParseRefs(CleanCommandOutput(result.Output));
            }
            else
            {
                // The remote branches of git's database.
                remoteHeads = [.. Module.GetRemoteBranches().Where(r => r.Remote == remote)];
            }

            List<IGitRef> localHeads = [.. Module.GetRefs(RefsFilter.Heads)];
            Dictionary<string, IGitRef> remoteBranches = remoteHeads.ToDictionary(h => h.LocalName, h => h);
            IReadOnlyDictionary<string, AheadBehindData>? aheadBehindData = new AheadBehindDataProvider(() => Module.GitExecutable).GetData();
            List<PushBranchRow> rows = [];
            foreach (IGitRef head in localHeads)
            {
                string remoteName = head.Remote == remote ? head.MergeWith ?? head.LocalName : string.Empty;
                bool isKnownAtRemote = remoteBranches.TryGetValue(head.Name, out IGitRef? remoteBranch);
                AheadBehindData aheadBehind = default;
                bool isAheadRemote = aheadBehindData?.TryGetValue(head.Name, out aheadBehind) is true && GitRefName.GetRemoteName(aheadBehind.RemoteRef) == remote;
                rows.Add(new PushBranchRow(
                    head.Name,
                    isAheadRemote ? GitRefName.GetRemoteBranch(aheadBehind.RemoteRef) : remoteName,
                    isAheadRemote ? aheadBehindData![head.Name].ToDisplay()
                        : !isKnownAtRemote ? string.Empty
                        : head.ObjectId == remoteBranch!.ObjectId ? "=" : "<>"));
            }

            // The remote branches left over can be deleted.
            foreach (IGitRef remoteHead in remoteHeads.Where(remoteHead => localHeads.All(h => h.Name != remoteHead.LocalName)))
            {
                rows.Add(new PushBranchRow(localBranch: null, remoteHead.LocalName, string.Empty));
            }

            return (IReadOnlyList<PushBranchRow>)rows;

            static string CleanCommandOutput(string processOutput)
            {
                // Lines of "<SHA1>\t<full-ref>".
                int firstTabIdx = processOutput.IndexOf('\t');
                return firstTabIdx == 40
                    ? processOutput
                    : firstTabIdx > 40
                        ? processOutput[(firstTabIdx - 40)..]
                        : string.Empty;
            }
        });

        /// <summary>As the end of <c>PushChanges</c>.</summary>
        public bool Push(PushRequest request, out bool errorOccurred)
        {
            bool error = false;
            bool pushed = AvaloniaUi.RunInHostContext(() =>
            {
                if (!request.PushToRemote)
                {
                    string path = request.Destination;
                    ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistoryManager.Remotes.AddAsMostRecentAsync(path));
                }
                else
                {
                    StartPageant(request.Remote);
                }

                string pushCmd = request.Tab switch
                {
                    PushTab.Branch when request.PushAllBranches => Commands.PushAll(request.Destination, request.ForcePush, request.Track, request.RecursiveSubmodules),
                    PushTab.Branch => Commands.Push(request.Destination, Module.FormatBranchName(request.LocalBranch), request.RemoteBranch, request.ForcePush, request.Track, request.RecursiveSubmodules),
                    PushTab.Tag => Commands.PushTag(request.Destination, request.Tag, request.PushAllTags, request.ForcePush),
                    _ => Commands.PushMultiple(request.Destination, request.PushActions),
                };

                IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
                if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforePush, this))
                {
                    return false;
                }

                error = RunRemoteProcess(
                    Owner,
                    commands,
                    pushCmd,
                    request.Remote,
                    string.Format(_strings.PushToCaption.Text, request.Destination),
                    onExit: (ref bool isError, IRemoteProcessDialog process) => HandlePushOnExit(ref isError, process, request)).ErrorOccurred;

                // The tracking info written by git (e.g. with --set-upstream) is read again.
                Module.InvalidateGitSettings();

                if (Module.InTheMiddleOfAction() || error)
                {
                    return false;
                }

                scriptsRunner.RunEventScripts(ScriptEvent.AfterPush, this);
                if (request.CreatePullRequest)
                {
                    if (PluginRegistry.TryGetGitHosterForModule(Module) is not null)
                    {
                        commands.StartCreatePullRequest(Owner);
                    }
                    else
                    {
                        TryOpenAzureDevOpsPullRequestInBrowser(request);
                    }
                }

                return true;
            });
            errorOccurred = error;
            return pushed;
        }

        public IScriptOptionsProvider GetScriptOptionsProvider() => new ScriptOptionsProvider(() => [], () => null, () => null);

        /// <summary>As <c>HandlePushOnExit</c>: a rejected push can be retried after a pull, or forced.</summary>
        private bool HandlePushOnExit(ref bool isError, IRemoteProcessDialog form, PushRequest request)
        {
            string currentBranch = CurrentBranch;

            // There is no way to pull another branch than the current one, nor from a URL (#1887).
            if (!isError || request.LocalBranch != currentBranch || !request.PushToRemote)
            {
                return false;
            }

            // The output of git contains color codes too.
            Regex isRejected = new($"! \\[rejected\\]( .* )?((?<currBranch>{Regex.Escape(currentBranch)})|.*) -> ");
            Match match = isRejected.Match(form.GetOutputString());
            if (!match.Success || Module.IsBareRepository())
            {
                return false;
            }

            (GitPullAction onRejectedPullAction, bool forcePush) = AskForAutoPullOnPushRejectedAction(form, match.Groups["currBranch"].Success, request.Remote);
            if (forcePush)
            {
                if (!form.ProcessArguments.Contains(" -f ") && !form.ProcessArguments.Contains(" --force"))
                {
                    // WSL may add other arguments before the command, so "push" may not be first.
                    int pos = form.ProcessArguments.IndexOf("push ");
                    DebugHelpers.Assert(pos >= 0, "Arguments should start with 'push' command");
                    form.ProcessArguments = form.ProcessArguments.Insert(pos + "push ".Length, "--force-with-lease ");
                }

                form.Retry();
                return true;
            }

            if (onRejectedPullAction == GitPullAction.Default)
            {
                onRejectedPullAction = AppSettings.DefaultPullAction;
            }

            if (onRejectedPullAction == GitPullAction.None)
            {
                return false;
            }

            if (onRejectedPullAction is not (GitPullAction.Merge or GitPullAction.Rebase))
            {
                MessageBoxes.ShowError(form, "Automatical pull can only be performed, when the default pull action is either set to Merge or Rebase.");
                return false;
            }

            if (IsRebasingMergeCommit(request))
            {
                MessageBoxes.ShowError(form, "Can not perform automatical pull, when the pull action is set to Rebase " +
                                             "and one of the commits that are about to be rebased is a merge commit.");
                return false;
            }

            commands.StartPullDialogAndPullImmediately(out bool pullCompleted, form, request.RemoteBranch, request.Remote, onRejectedPullAction);
            if (pullCompleted)
            {
                form.Retry();
                return true;
            }

            return false;
        }

        /// <summary>As <c>IsRebasingMergeCommit</c>.</summary>
        private bool IsRebasingMergeCommit(PushRequest request)
            => AppSettings.DefaultPullAction == GitPullAction.Rebase
                && request.IsCurrentBranch
                && request.IsCurrentBranchRemote
                && Module.ExistsMergeCommit($"{request.Remote}/{request.RemoteBranch}", request.LocalBranch);

        /// <summary>As <c>AskForAutoPullOnPushRejectedAction</c>.</summary>
        private (GitPullAction PullAction, bool ForcePush) AskForAutoPullOnPushRejectedAction(IWin32Window owner, bool allOptions, string destination)
        {
            bool forcePush = false;
            GitPullAction? onRejectedPullAction = AppSettings.AutoPullOnPushRejectedAction;
            if (onRejectedPullAction is not null)
            {
                return (onRejectedPullAction.Value, forcePush);
            }

            string defaultAction = AppSettings.DefaultPullAction switch
            {
                GitPullAction.Fetch or GitPullAction.FetchAll or GitPullAction.FetchPruneAll => _strings.PullActionFetch.Text,
                GitPullAction.Merge => _strings.PullActionMerge.Text,
                GitPullAction.Rebase => _strings.PullActionRebase.Text,
                _ => _strings.PullActionNone.Text,
            };

            TaskDialogPage page = new()
            {
                Text = allOptions ? _strings.PullRepositoryMergeInstruction.Text : _strings.PullRepositoryForceInstruction.Text,
                Heading = allOptions ? _strings.PullRepositoryMainMergeInstruction.Text : _strings.PullRepositoryMainForceInstruction.Text,
                Caption = string.Format(_strings.PullRepositoryCaption.Text, destination),
                Buttons = { TaskDialogButton.Cancel },
                Icon = TaskDialogIcon.Error,
                Verification = new TaskDialogVerificationCheckBox { Text = TranslatedStrings.DontShowAgain },
                AllowCancel = true,
                SizeToContent = true
            };
            TaskDialogCommandLinkButton btnPullDefault = new(string.Format(_strings.PullDefaultButton.Text, defaultAction));
            TaskDialogCommandLinkButton btnPullRebase = new(_strings.PullRebaseButton.Text);
            TaskDialogCommandLinkButton btnPullMerge = new(_strings.PullMergeButton.Text);
            TaskDialogCommandLinkButton btnPushForce = new(_strings.PushForceButton.Text);
            if (allOptions)
            {
                page.Buttons.Add(btnPullDefault);
                page.Buttons.Add(btnPullRebase);
                page.Buttons.Add(btnPullMerge);
            }

            page.Buttons.Add(btnPushForce);

            TaskDialogButton result = TaskDialog.ShowDialog(owner, page);
            if (result == TaskDialogButton.Cancel)
            {
                onRejectedPullAction = GitPullAction.None;
            }
            else if (result == btnPullDefault)
            {
                onRejectedPullAction = GitPullAction.Default;
            }
            else if (result == btnPullRebase)
            {
                onRejectedPullAction = GitPullAction.Rebase;
            }
            else if (result == btnPullMerge)
            {
                onRejectedPullAction = GitPullAction.Merge;
            }
            else if (result == btnPushForce)
            {
                forcePush = true;
            }

            if (page.Verification.Checked)
            {
                AppSettings.AutoPullOnPushRejectedAction = onRejectedPullAction;
            }

            return (onRejectedPullAction ?? GitPullAction.None, forcePush);
        }

        /// <summary>As <c>HasAzureDevOpsRemote</c>.</summary>
        private bool HasAzureDevOpsRemote()
        {
            AzureDevOpsRemoteParser parser = new();
            return Module.GetRemoteNames()
                .Select(remoteName => Module.GetSetting(string.Format(SettingKeyString.RemoteUrl, remoteName)))
                .Any(remoteUrl => !string.IsNullOrWhiteSpace(remoteUrl) && parser.IsValidRemoteUrl(remoteUrl));
        }

        /// <summary>As <c>TryOpenAzureDevOpsPullRequestInBrowser</c>.</summary>
        private void TryOpenAzureDevOpsPullRequestInBrowser(PushRequest request)
        {
            string? remoteUrl = LoadRemotes().FirstOrDefault(r => r.Name == request.Remote)?.Url;
            AzureDevOpsRemoteParser parser = new();
            if (string.IsNullOrWhiteSpace(remoteUrl)
                || !parser.TryExtractAzureDevopsDataFromRemoteUrl(remoteUrl, out string? owner, out string? project, out string? repo)
                || AzureDevOpsRemoteParser.BuildRepositoryUrl(remoteUrl, owner, project, repo) is not { } repoWebUrl)
            {
                return;
            }

            string branch = request.LocalBranch is not (PushViewModel.HeadText or PushViewModel.AllRefs) && !string.IsNullOrEmpty(request.LocalBranch)
                ? request.LocalBranch
                : Module.GetSelectedBranch();
            OsShellUtil.OpenUrlInDefaultBrowser($"{repoWebUrl}/pullrequestcreate?sourceRef={Uri.EscapeDataString(branch)}");
        }
    }
}
