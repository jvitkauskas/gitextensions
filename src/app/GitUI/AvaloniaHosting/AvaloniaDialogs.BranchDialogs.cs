using GitCommands;
using GitCommands.Git;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Infrastructure;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUI.ScriptsEngine;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the branch dialogs (docs/avalonia-port/PLAN.md, phase 2, batch 3).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowDeleteBranch(IWin32Window? owner, IGitUICommands commands, IEnumerable<string> branches)
    {
        IGitModule module = commands.Module;
        IReadOnlyList<IGitRef> heads = module.GetRefs(RefsFilter.Heads);

        // As FormDeleteBranch.OnRuntimeLoad.
        string? currentBranch = null;
        HashSet<string>? mergedBranches = null;
        if (AppSettings.DontConfirmDeleteUnmergedBranch.Value)
        {
            currentBranch = module.GetSelectedBranch();
        }
        else
        {
            mergedBranches = [];
            foreach (string branch in module.GetMergedBranches())
            {
                if (branch.StartsWith("* "))
                {
                    currentBranch = branch.Trim('*', ' ');
                }
                else
                {
                    mergedBranches.Add(branch.Trim());
                }
            }
        }

        ShowDialog(
            () =>
            {
                DeleteBranchWindow window = new();
                MessageBoxService messageBoxes = new(window);
                DeleteBranchStrings strings = ViewStrings.Load<DeleteBranchStrings>();
                BranchSelectorViewModel selector = CreateBranchSelector(window, heads, messageBoxes);
                selector.Text = string.Join(" ", branches);
                window.DataContext = new DeleteBranchViewModel(
                    strings, selector, currentBranch, mergedBranches, new DeleteBranchHost(window, commands, heads, strings), messageBoxes);
                return window;
            },
            owner);
        return true;
    }

    public static bool TryShowDeleteRemoteBranch(IWin32Window? owner, IGitUICommands commands, string remoteBranch)
    {
        IGitModule module = commands.Module;
        IReadOnlyList<IGitRef> remoteRefs = module.GetRefs(RefsFilter.Remotes);

        // As FormDeleteRemoteBranch, the merged branches are listed in the background.
        JoinableTask<HashSet<string>> mergedBranches = ThreadHelper.JoinableTaskFactory.RunAsync(
            () => Task.Run(() => new HashSet<string>(module.GetMergedRemoteBranches())));

        ShowDialog(
            () =>
            {
                DeleteRemoteBranchWindow window = new();
                MessageBoxService messageBoxes = new(window);
                BranchSelectorViewModel selector = CreateBranchSelector(window, remoteRefs, messageBoxes);
                selector.Text = remoteBranch ?? "";
                window.DataContext = new DeleteRemoteBranchViewModel(
                    ViewStrings.Load<DeleteRemoteBranchStrings>(),
                    selector,
                    new DeleteRemoteBranchHost(window, commands, remoteRefs, mergedBranches),
                    messageBoxes);
                return window;
            },
            owner);
        return true;
    }

    public static bool TryShowMergeBranch(IWin32Window? owner, IGitUICommands commands, string? defaultBranch)
    {
        IGitModule module = commands.Module;
        SettingsSource effectiveSettings = module.GetEffectiveSettings();
        MergeBranchOptions options = new(
            NoFastForward: effectiveSettings.Detached().NoFastForwardMerge,
            NoCommit: AppSettings.DontCommitMerge,
            AddLogMessages: DetailedSettings.AddMergeLogMessages.ValueOrDefault(effectiveSettings),
            LogMessagesCount: DetailedSettings.MergeLogMessagesCount.ValueOrDefault(effectiveSettings),
            ShowAdvanced: AppSettings.AlwaysShowAdvOpt);

        // As FormMergeBranchLoad: offer merging tags too (but not stashes, notes etc.).
        string currentBranch = module.GetSelectedBranch();
        IReadOnlyList<IGitRef> refs = module.GetRefs(RefsFilter.Heads | RefsFilter.Remotes | RefsFilter.Tags);
        string? branch = defaultBranch ?? module.GetRemoteBranch(currentBranch);

        ShowDialog(
            () =>
            {
                MergeBranchWindow window = new();
                MessageBoxService messageBoxes = new(window);
                BranchSelectorViewModel selector = CreateBranchSelector(window, refs, messageBoxes);
                selector.Text = branch ?? "";
                window.DataContext = new MergeBranchViewModel(
                    ViewStrings.Load<MergeBranchStrings>(),
                    selector,
                    currentBranch,
                    options,
                    CreateHelpImage("MergeBranches"),
                    new MergeBranchHost(window, owner, commands));
                return window;
            },
            owner,
            positionName: "FormMergeBranch");
        return true;
    }

    /// <summary>A branch selector (<c>BranchComboBox</c>) offering <paramref name="refs"/>, whose multiple selection is an Avalonia dialog too.</summary>
    private static BranchSelectorViewModel CreateBranchSelector(DialogWindow window, IReadOnlyList<IGitRef> refs, MessageBoxService messageBoxes)
        => new(
            ViewStrings.Load<BranchSelectorStrings>(),
            [.. refs.Select(r => r.Name)],
            selected =>
            {
                SelectMultipleBranchesViewModel viewModel = new(
                    ViewStrings.Load<SelectMultipleBranchesStrings>(),
                    refs.Select(r => ((object)r.Name, r.Name)),
                    selected);
                ShowDialog(() => new SelectMultipleBranchesWindow { DataContext = viewModel }, new NativeWindowOwner(window));
                return [.. viewModel.SelectedBranches.Cast<string>()];
            },
            messageBoxes,
            TranslatedStrings.Error);

    /// <summary>The help image panel (<c>HelpImageDisplayUserControl</c>), expanded state persisted as by the WinForms control.</summary>
    private static HelpImageViewModel CreateHelpImage(string uniqueIsExpandedSettingsId)
    {
        string setting = "HelpIsExpanded" + uniqueIsExpandedSettingsId;
        return new HelpImageViewModel(
            ViewStrings.Load<HelpImageStrings>(),
            isVisible: !AppSettings.DontShowHelpImages,
            isExpanded: AppSettings.GetBool(setting, true),
            value => AppSettings.SetBool(setting, value));
    }

    private sealed class DeleteBranchHost(DialogWindow window, IGitUICommands commands, IReadOnlyList<IGitRef> heads, DeleteBranchStrings strings) : IDeleteBranchHost
    {
        private IGitModule Module => commands.Module;

        public IReadOnlyList<string> HandleWorktreeBranches(IReadOnlyList<string> branches) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormDeleteBranch.HandleWorktreeBranches.
            IReadOnlyList<GitWorktree> worktrees = Module.GetWorktrees();
            if (worktrees.Count <= 1)
            {
                return branches;
            }

            NativeWindowOwner owner = new(window);
            IGitRef[] selectedBranches = [.. ToRefs(branches)];
            string currentWorkingDir = Path.GetFullPath(Module.WorkingDir).TrimEnd(Path.DirectorySeparatorChar);
            WorktreeBranchClassification classification = ClassifyWorktreeBranches(selectedBranches, worktrees, currentWorkingDir);
            if (!classification.HasDeletedWorktrees
                && classification.MainWorktreeBranches.Count == 0
                && classification.LinkedWorktreeBranches.Count == 0)
            {
                return branches;
            }

            // Prune stale worktree entries whose directories no longer exist, so they no longer block branch deletion.
            if (classification.HasDeletedWorktrees)
            {
                commands.StartCommandLineProcessDialog(owner, command: null, "worktree prune");
            }

            HashSet<string> excludedBranches = [];
            if (classification.MainWorktreeBranches.Count > 0)
            {
                (IGitRef branch, GitWorktree worktree) = classification.MainWorktreeBranches[0];
                MessageBoxes.Show(
                    owner,
                    string.Format(strings.CannotDeleteBranchInMainWorktree.Text, branch.Name, worktree.Path),
                    strings.DeleteBranchCaption.Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                excludedBranches.Add(branch.Name);
            }

            if (classification.LinkedWorktreeBranches.Count > 0)
            {
                string branchList = string.Join("\n", classification.LinkedWorktreeBranches.Select(x => $"• {x.Branch.Name}  →  {x.Worktree.Path}"));
                TaskDialogPage page = new()
                {
                    Text = string.Format(strings.BranchUsedByWorktreeQuestion.Text, branchList),
                    Caption = strings.DeleteBranchCaption.Text,
                    Heading = TranslatedStrings.CannotBeUndone,
                    Icon = TaskDialogIcon.Warning,
                    Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
                    DefaultButton = TaskDialogButton.No,
                    AllowCancel = true,
                    SizeToContent = true,
                };

                if (TaskDialog.ShowDialog(owner.Handle, page) == TaskDialogButton.Yes)
                {
                    bool anyDeleted = false;
                    foreach ((IGitRef branch, GitWorktree worktree) in classification.LinkedWorktreeBranches)
                    {
                        if (worktree.Path.TryDeleteDirectory(out string? errorMessage))
                        {
                            anyDeleted = true;
                        }
                        else
                        {
                            MessageBoxes.Show(
                                owner,
                                $"{string.Format(TranslatedStrings.DeleteWorktreeFailed, worktree.Path)}\n{errorMessage}",
                                TranslatedStrings.Error,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            excludedBranches.Add(branch.Name);
                        }
                    }

                    if (anyDeleted)
                    {
                        commands.StartCommandLineProcessDialog(owner, command: null, "worktree prune");
                    }
                }
                else
                {
                    foreach ((IGitRef branch, _) in classification.LinkedWorktreeBranches)
                    {
                        excludedBranches.Add(branch.Name);
                    }
                }
            }

            return (IReadOnlyList<string>)[.. branches.Where(b => !excludedBranches.Contains(b))];
        });

        public bool ConfirmDeleteUnmerged() => AvaloniaUi.RunInHostContext(() => MessageBoxes.ConfirmSuppressible(
            new NativeWindowOwner(window),
            strings.DeleteBranchQuestion.Text,
            strings.DeleteBranchConfirmTitle.Text,
            AppSettings.DontConfirmDeleteUnmergedBranch,
            icon: TaskDialogIcon.Warning,
            footnote: strings.UseReflogHint.Text,
            defaultNo: true));

        public bool DeleteBranches(IReadOnlyList<string> branches) => AvaloniaUi.RunInHostContext(()
            => commands.StartCommandLineProcessDialog(new NativeWindowOwner(window), Commands.DeleteBranch([.. ToRefs(branches)], force: true)));

        private IEnumerable<IGitRef> ToRefs(IEnumerable<string> names) => names.Select(name => heads.First(h => h.Name == name));
    }

    private sealed class DeleteRemoteBranchHost(
        DialogWindow window,
        IGitUICommands commands,
        IReadOnlyList<IGitRef> remoteRefs,
        JoinableTask<HashSet<string>> mergedBranches) : IDeleteRemoteBranchHost
    {
        private IGitModule Module => commands.Module;

        public IReadOnlyList<string> GetTrackingBranches(IReadOnlyList<string> remoteBranches)
        {
            IGitRef[] refs = [.. ToRefs(remoteBranches)];
            return [.. Module.GetRefs(RefsFilter.Heads).Where(b => refs.Any(r => b.IsTrackingRemote(r))).Select(r => r.LocalName)];
        }

        public bool HasUnmergedBranches(IReadOnlyList<string> remoteBranches)
        {
            HashSet<string> merged = mergedBranches.Join();
            return ToRefs(remoteBranches).Any(branch => !merged.Contains(branch.CompleteName));
        }

        public bool DeleteRemoteBranches(IReadOnlyList<string> remoteBranches, bool deleteLocalTrackingBranches) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormDeleteRemoteBranch.Delete_Click.
            NativeWindowOwner owner = new(window);
            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            ScriptHost scriptHost = new(window, commands);
            List<IGitRef> selectedBranches = [.. ToRefs(remoteBranches)];
            foreach ((string remote, IEnumerable<IGitRef> branches) in selectedBranches.GroupBy(b => b.Remote))
            {
                if (GitSshHelpers.IsPlink)
                {
                    PuttyHelpers.StartPageantIfConfigured(() => Module.GetPuttyKeyFileForRemote(remote));
                }

                if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforePush, scriptHost))
                {
                    return false;
                }

                IGitCommand cmd = Commands.DeleteRemoteBranches(remote, branches.Select(x => x.LocalName));
                RemoteProcessResult result = RunRemoteProcess(owner, commands, cmd.Arguments, remote);

                if (!result.ErrorOccurred && !Module.InTheMiddleOfAction())
                {
                    scriptsRunner.RunEventScripts(ScriptEvent.AfterPush, scriptHost);
                    if (deleteLocalTrackingBranches)
                    {
                        commands.StartDeleteBranchDialog(owner, GetTrackingBranches(remoteBranches));
                    }
                }
            }

            commands.RepoChangedNotifier.Notify();
            return true;
        });

        private IEnumerable<IGitRef> ToRefs(IEnumerable<string> names) => names.Select(name => remoteRefs.First(r => r.Name == name));
    }

    private sealed class MergeBranchHost(DialogWindow window, IWin32Window? owner, IGitUICommands commands) : IMergeBranchHost
    {
        private IGitModule Module => commands.Module;

        public void SaveLogMessagesSettings(bool addLogMessages, int count)
        {
            SettingsSource effectiveSettings = Module.GetEffectiveSettings();
            DetailedSettings.AddMergeLogMessages[effectiveSettings] = addLogMessages;
            DetailedSettings.MergeLogMessagesCount[effectiveSettings] = count;
        }

        public void OpenStrategyHelp() => AvaloniaUi.RunInHostContext(()
            => OsShellUtil.OpenUrlInDefaultBrowser(UserManual.UserManual.UrlFor("branches", "advanced-merge-options")));

        public bool Merge(MergeRequest request) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormMergeBranch.OkClick.
            NativeWindowOwner windowOwner = new(window);
            IDetachedSettings detachedSettings = Module.GetEffectiveSettings().Detached();
            detachedSettings.NoFastForwardMerge = !request.FastForward;
            AppSettings.DontCommitMerge = request.NoCommit;

            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            ScriptHost scriptHost = new(window, commands);
            if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforeMerge, scriptHost))
            {
                return false;
            }

            string? mergeMessagePath = null;
            if (request.MergeMessage is not null)
            {
                // [!] Do not reset the last commit message stored in AppSettings.LastCommitMessage
                CommitMessageManager commitMessageManager = new(owner as Control ?? new Control(), Module.WorkingDirGitDir, Module.CommitEncoding);
                ThreadHelper.JoinableTaskFactory.Run(() => commitMessageManager.WriteCommitMessageToFileAsync(
                    request.MergeMessage, CommitMessageType.Merge, usingCommitTemplate: false, ensureCommitMessageSecondLineEmpty: false));
                mergeMessagePath = commitMessageManager.MergeMessagePath;
            }

            ArgumentString command = Commands.MergeBranch(
                request.Branch,
                request.FastForward,
                request.Squash,
                request.NoCommit,
                request.Strategy ?? "",
                request.AllowUnrelatedHistories,
                mergeMessagePath,
                Module.GetPathForGitExecution,
                request.LogMessages);
            bool success = ProcessDialogs.ShowProcess(windowOwner, commands, command, Module.WorkingDir, input: null, useDialogSettings: true, out string commandOutput);

            bool wasConflict = MergeConflictHandler.HandleMergeConflicts(commands, windowOwner, !request.NoCommit)
                || Module.CanContinueAction(commandOutput);
            if (!success && !wasConflict)
            {
                return false;
            }

            scriptsRunner.RunEventScripts(ScriptEvent.AfterMerge, scriptHost);
            commands.RepoChangedNotifier.Notify();
            return true;
        });
    }

    // Moved out of FormDeleteBranch.

    /// <summary>
    ///  Classifies selected branches by how they relate to worktrees: in the main worktree,
    ///  in a linked worktree, or in a deleted (stale) worktree. Branches in the current
    ///  working directory's worktree are excluded (handled separately as "current branch").
    /// </summary>
    internal static WorktreeBranchClassification ClassifyWorktreeBranches(
        IReadOnlyList<IGitRef> selectedBranches,
        IReadOnlyList<GitWorktree> worktrees,
        string currentWorkingDir)
    {
        bool hasDeletedWorktrees = false;
        List<(IGitRef Branch, GitWorktree Worktree)> mainWorktreeBranches = [];
        List<(IGitRef Branch, GitWorktree Worktree)> linkedWorktreeBranches = [];

        for (int i = 0; i < worktrees.Count; i++)
        {
            GitWorktree worktree = worktrees[i];
            if (worktree.Branch is null)
            {
                continue;
            }

            if (worktree.IsDeleted)
            {
                if (selectedBranches.Any(b => b.Name == worktree.Branch))
                {
                    hasDeletedWorktrees = true;
                }

                continue;
            }

            string worktreeDir = Path.GetFullPath(worktree.Path).TrimEnd(Path.DirectorySeparatorChar);
            if (string.Equals(worktreeDir, currentWorkingDir, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (IGitRef branch in selectedBranches)
            {
                if (branch.Name == worktree.Branch)
                {
                    if (i == 0)
                    {
                        mainWorktreeBranches.Add((branch, worktree));
                    }
                    else
                    {
                        linkedWorktreeBranches.Add((branch, worktree));
                    }

                    break;
                }
            }
        }

        return new(hasDeletedWorktrees, mainWorktreeBranches, linkedWorktreeBranches);
    }

    internal readonly record struct WorktreeBranchClassification(
        bool HasDeletedWorktrees,
        IReadOnlyList<(IGitRef Branch, GitWorktree Worktree)> MainWorktreeBranches,
        IReadOnlyList<(IGitRef Branch, GitWorktree Worktree)> LinkedWorktreeBranches);
}
