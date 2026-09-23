using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using ResourceManager;
using ResourceManager.CommitDataRenders;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the dialogs that act on a commit shown with its summary (docs/avalonia-port/PLAN.md, phase 2, batch 3).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowCherryPick(IWin32Window? owner, IGitUICommands commands, GitRevision? revision, out bool picked)
    {
        picked = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormCherryPick)))
        {
            return false;
        }

        picked = ShowCherryPick(owner, commands, revision);
        return true;
    }

    /// <summary>Cherry-picks the revisions one after the other, as <c>GitUICommands.StartCherryPickDialog</c>.</summary>
    public static bool TryShowCherryPicks(IWin32Window? owner, IGitUICommands commands, IEnumerable<GitRevision> revisions, out bool repoChanged)
    {
        repoChanged = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormCherryPick)))
        {
            return false;
        }

        // Each dialog starts with the options of the previous one, which saves them when accepted.
        foreach (GitRevision revision in revisions)
        {
            if (!ShowCherryPick(owner, commands, revision))
            {
                break;
            }

            repoChanged = true;
        }

        return true;
    }

    public static bool TryShowRevertCommit(IWin32Window? owner, IGitUICommands commands, GitRevision revision, out bool reverted)
    {
        reverted = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormRevertCommit)))
        {
            return false;
        }

        RevisionInfo revisionInfo = ToRevisionInfo(commands.Module, revision);
        reverted = ShowDialog(
            () =>
            {
                RevertCommitWindow window = new();
                window.DataContext = new RevertCommitViewModel(
                    ViewStrings.Load<RevertCommitStrings>(),
                    ViewStrings.Load<CommitSummaryStrings>(),
                    revisionInfo,
                    new RevertCommitHost(window, owner, commands),
                    new MessageBoxService(window),
                    TranslatedStrings.Error);
                return window;
            },
            owner);
        return true;
    }

    public static bool TryShowResetCurrentBranch(IWin32Window? owner, IGitUICommands commands, GitRevision revision, FormResetCurrentBranch.ResetType resetType, out bool reset)
    {
        reset = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormResetCurrentBranch)))
        {
            return false;
        }

        reset = ShowDialog(
            () =>
            {
                ResetCurrentBranchWindow window = new();
                window.DataContext = new ResetCurrentBranchViewModel(
                    ViewStrings.Load<ResetCurrentBranchStrings>(),
                    ViewStrings.Load<CommitSummaryStrings>(),
                    commands.Module.GetSelectedBranch(),
                    ToCommitSummary(revision),
                    (ResetKind)resetType,
                    new ResetCurrentBranchHost(window, commands, revision),
                    new MessageBoxService(window));
                return window;
            },
            owner,
            positionName: nameof(FormResetCurrentBranch));
        return true;
    }

    public static bool TryShowResetAnotherBranch(IWin32Window? owner, IGitUICommands commands, GitRevision revision, out bool reset)
    {
        reset = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormResetAnotherBranch)))
        {
            return false;
        }

        // As FormResetAnotherBranch.InitLocalBranchesWithoutCurrent.
        IGitModule module = commands.Module;
        string currentBranch = module.GetSelectedBranch();
        bool isDetachedHead = currentBranch == DetachedHeadParser.DetachedBranch;
        List<IGitRef> selectedRevisionRemotes = [.. revision.Refs.Where(r => r.IsRemote)];
        IGitRef[] localRefs = [.. module.GetRefs(RefsFilter.Heads)
            .Where(r => r.IsHead)
            .Where(r => isDetachedHead || r.LocalName != currentBranch)
            .Where(r => revision.ObjectId != r.ObjectId) // Don't display local branches already at this revision
            .OrderByDescending(r => selectedRevisionRemotes.Any(r.IsTrackingRemote)) // Put local branches that track these remotes first
            .ThenByDescending(r => selectedRevisionRemotes.Any(r2 => r2.LocalName == r.LocalName))];
        string? defaultBranch = null;
        if (selectedRevisionRemotes.Count == 1)
        {
            IGitRef availableRemote = selectedRevisionRemotes[0];
            IGitRef[] defaultCandidateRefs = [.. localRefs.Where(r => r.IsTrackingRemote(availableRemote) || r.LocalName == availableRemote.LocalName)];
            if (defaultCandidateRefs.Length == 1)
            {
                defaultBranch = defaultCandidateRefs[0].Name;
            }
        }

        reset = ShowDialog(
            () =>
            {
                ResetAnotherBranchWindow window = new();
                window.DataContext = new ResetAnotherBranchViewModel(
                    ViewStrings.Load<ResetAnotherBranchStrings>(),
                    ViewStrings.Load<CommitSummaryStrings>(),
                    [.. localRefs.Select(r => r.Name)],
                    defaultBranch,
                    ToCommitSummary(revision),
                    AppSettings.CheckoutOtherBranchAfterReset.Value,
                    new ResetAnotherBranchHost(window, commands, revision, localRefs),
                    new MessageBoxService(window),
                    TranslatedStrings.Error);
                return window;
            },
            owner,
            positionName: nameof(FormResetAnotherBranch));
        return true;
    }

    public static bool TryShowArchive(IWin32Window? owner, IGitUICommands commands, GitRevision? revision, GitRevision? diffRevision, string? path)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormArchive)))
        {
            return false;
        }

        IGitModule module = commands.Module;
        ShowDialog(
            () =>
            {
                ArchiveWindow window = new();
                window.DataContext = new ArchiveViewModel(
                    ViewStrings.Load<ArchiveStrings>(),
                    ViewStrings.Load<CommitSummaryStrings>(),
                    revision is null ? null : ToRevisionInfo(module, revision, withParents: false),
                    diffRevision is null ? null : ToRevisionInfo(module, diffRevision, withParents: false),
                    path,
                    new DirectoryInfo(module.WorkingDir).Name,
                    new ArchiveHost(window, commands),
                    new AvaloniaFileDialogService(window),
                    new MessageBoxService(window),
                    TranslatedStrings.Error);
                return window;
            },
            owner,
            positionName: nameof(FormArchive));
        return true;
    }

    private static bool ShowCherryPick(IWin32Window? owner, IGitUICommands commands, GitRevision? revision)
    {
        RevisionInfo? revisionInfo = revision is null ? null : ToRevisionInfo(commands.Module, revision);
        return ShowDialog(
            () =>
            {
                CherryPickWindow window = new();
                window.DataContext = new CherryPickViewModel(
                    ViewStrings.Load<CherryPickStrings>(),
                    ViewStrings.Load<CommitSummaryStrings>(),
                    revisionInfo,
                    new CherryPickOptions(AppSettings.CommitAutomaticallyAfterCherryPick, AppSettings.AddCommitReferenceToCherryPick),
                    new CherryPickHost(window, commands),
                    new MessageBoxService(window),
                    TranslatedStrings.Error);
                return window;
            },
            owner);
    }

    /// <summary>What <c>CommitSummaryUserControl</c> shows of the revision.</summary>
    private static CommitSummary ToCommitSummary(GitRevision revision)
        => new(
            revision.ObjectId.ToShortString(),
            revision.Author ?? "",
            new DateFormatter().FormatDateAsRelativeLocal(revision.CommitDate),
            revision.Subject,
            [.. revision.Refs.Where(r => r.IsTag).Select(r => r.LocalName)],
            [.. revision.Refs.Where(r => r.IsHead).Select(r => r.LocalName)]);

    private static RevisionInfo ToRevisionInfo(IGitModule module, GitRevision revision, bool withParents = true)
    {
        IReadOnlyList<ParentCommit> parents = withParents && module.IsMerge(revision.ObjectId)
            ? [.. module.GetParentRevisions(revision.ObjectId).Select((parent, i) => new ParentCommit(i + 1, parent.Subject, parent.Author ?? "", parent.CommitDate.ToShortDateString()))]
            : [];
        return new RevisionInfo(revision.Guid, ToCommitSummary(revision), parents);
    }

    /// <summary>Lets the user choose a commit in the (WinForms) revision grid of <see cref="FormChooseCommit"/>.</summary>
    private static RevisionInfo? ChooseRevision(DialogWindow window, IGitUICommands commands, string? currentGuid, bool withParents = true)
        => AvaloniaUi.RunInHostContext(() =>
        {
            using FormChooseCommit chooseForm = new(commands, currentGuid);
            return chooseForm.ShowDialog(new NativeWindowOwner(window)) == DialogResult.OK && chooseForm.SelectedRevision is GitRevision selected
                ? ToRevisionInfo(commands.Module, selected, withParents)
                : null;
        });

    private sealed class CherryPickHost(DialogWindow window, IGitUICommands commands) : ICherryPickHost
    {
        public RevisionInfo? ChooseRevision(string? currentGuid) => AvaloniaDialogs.ChooseRevision(window, commands, currentGuid);

        public void CherryPick(string guid, bool autoCommit, int parentNumber, bool addReference) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormCherryPick.btnPick_Click.
            NativeWindowOwner owner = new(window);
            ArgumentBuilder args = [];
            if (parentNumber > 0)
            {
                args.Add("-m " + parentNumber);
            }

            if (addReference)
            {
                args.Add("-x");
            }

            ArgumentString command = Commands.CherryPick(ObjectId.Parse(guid), autoCommit, args.ToString());

            // Don't verify whether the command is successful.
            // If it fails, likely there is a conflict that needs to be resolved.
            FormProcess.ShowDialog(owner, commands, command, commands.Module.WorkingDir, input: null, useDialogSettings: true);
            MergeConflictHandler.HandleMergeConflicts(commands, owner, autoCommit);
        });

        public void SaveOptions(CherryPickOptions options)
        {
            AppSettings.CommitAutomaticallyAfterCherryPick = options.AutoCommit;
            AppSettings.AddCommitReferenceToCherryPick = options.AddReference;
        }
    }

    private sealed class RevertCommitHost(DialogWindow window, IWin32Window? owner, IGitUICommands commands) : IRevertCommitHost
    {
        public void Revert(string guid, bool autoCommit, int parentNumber) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormRevertCommit.Revert_Click.
            NativeWindowOwner windowOwner = new(window);
            IGitModule module = commands.Module;
            CommitMessageManager commitMessageManager = new(owner as Control ?? new Control(), module.WorkingDirGitDir, module.CommitEncoding);
            string existingCommitMessage = ThreadHelper.JoinableTaskFactory.Run(() => commitMessageManager.GetMergeOrCommitMessageAsync());

            ArgumentString command = Commands.Revert(ObjectId.Parse(guid), autoCommit, parentNumber);

            // Don't verify whether the command is successful.
            // If it fails, likely there is a conflict that needs to be resolved.
            FormProcess.ShowDialog(windowOwner, commands, command, module.WorkingDir, input: null, useDialogSettings: true);

            if (!string.IsNullOrWhiteSpace(existingCommitMessage))
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    try
                    {
                        await TaskScheduler.Default;

                        string message = await commitMessageManager.GetMergeOrCommitMessageAsync();
                        string newCommitMessageContent = $"{existingCommitMessage}\n\n{message}";
                        await commitMessageManager.WriteCommitMessageToFileAsync(
                            newCommitMessageContent, CommitMessageType.Merge, usingCommitTemplate: false, ensureCommitMessageSecondLineEmpty: false);
                    }
                    catch (Exception)
                    {
                    }
                });
            }

            MergeConflictHandler.HandleMergeConflicts(commands, windowOwner, autoCommit);
        });
    }

    private sealed class ResetCurrentBranchHost(DialogWindow window, IGitUICommands commands, GitRevision revision) : IResetCurrentBranchHost
    {
        public void Reset(ResetKind kind) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormResetCurrentBranch.Ok_Click (the view model confirms a hard reset).
            NativeWindowOwner owner = new(window);
            IGitModule module = commands.Module;
            bool updateSubmodules = AppSettings.UpdateSubmodulesOnCheckout is true && module.HasSubmodules() && revision.ObjectId != module.GetCurrentCheckout();

            ResetMode mode = kind switch
            {
                ResetKind.Soft => ResetMode.Soft,
                ResetKind.Mixed => ResetMode.Mixed,
                ResetKind.Keep => ResetMode.Keep,
                ResetKind.Merge => ResetMode.Merge,
                _ => ResetMode.Hard,
            };
            ObjectId currentCheckout = module.GetCurrentCheckout();
            bool success = FormProcess.ShowDialog(owner, commands, Commands.Reset(mode, revision.Guid, quiet: false), module.WorkingDir, input: null, useDialogSettings: true);
            if (mode == ResetMode.Hard && success && currentCheckout != revision.ObjectId)
            {
                commands.UpdateSubmodules(owner);
            }

            if (updateSubmodules)
            {
                commands.StartUpdateSubmodulesDialog(owner);
            }

            commands.RepoChangedNotifier.Notify();
        });

        public void OpenHelp(ResetKind kind) => AvaloniaUi.RunInHostContext(()
            => OsShellUtil.OpenUrlInDefaultBrowser($"https://git-scm.com/docs/git-reset#Documentation/git-reset.txt---{kind.ToString().ToLowerInvariant()}"));
    }

    private sealed class ResetAnotherBranchHost(DialogWindow window, IGitUICommands commands, GitRevision revision, IGitRef[] localRefs) : IResetAnotherBranchHost
    {
        public bool IsAncestor(string branch)
        {
            IGitRef gitRef = localRefs.First(r => r.Name == branch);
            GitArgumentBuilder command = new("merge-base")
            {
                "--is-ancestor",
                gitRef.CompleteName.QuoteNE(),
                revision.ObjectId,
            };
            return commands.Module.GitExecutable.Execute(command, throwOnErrorExit: false).ExitedSuccessfully;
        }

        public bool Reset(string branch, bool checkout) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormResetAnotherBranch.Ok_Click.
            NativeWindowOwner owner = new(window);
            IGitRef gitRef = localRefs.First(r => r.Name == branch);
            ArgumentString command = Commands.UpdateRef(gitRef.CompleteName, revision.ObjectId);
            if (!FormProcess.ShowDialog(owner, commands, command, commands.Module.WorkingDir, input: null, useDialogSettings: true))
            {
                return false;
            }

            if (checkout)
            {
                commands.StartCheckoutBranch(owner, gitRef.Name);
            }

            commands.RepoChangedNotifier.Notify();
            return true;
        });

        public void SaveCheckoutAfterReset(bool value) => AppSettings.CheckoutOtherBranchAfterReset.Value = value;
    }

    private sealed class ArchiveHost(DialogWindow window, IGitUICommands commands) : IArchiveHost
    {
        public RevisionInfo? ChooseRevision(string? currentGuid) => AvaloniaDialogs.ChooseRevision(window, commands, currentGuid, withParents: false);

        public IReadOnlyList<string> GetChangedFiles(string? fromGuid, string? toGuid)
            => [.. commands.Module.GetDiffFilesWithUntracked(fromGuid, toGuid, StagedStatus.None, noCache: false, cancellationToken: default)
                .Where(f => !f.IsDeleted)
                .Select(f => f.Name)];

        public void Archive(string format, string? revisionGuid, string outputPath, string pathArguments) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormArchive.Save_Click.
            string arguments = string.Format(@"archive --format=""{0}"" {1} --output ""{2}"" {3}", format, revisionGuid, outputPath, pathArguments);
            FormProcess.ShowDialog(new NativeWindowOwner(window), commands, arguments, commands.Module.WorkingDir, input: null, useDialogSettings: true);
        });
    }
}
