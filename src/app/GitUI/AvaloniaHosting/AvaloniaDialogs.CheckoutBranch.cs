using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.ScriptsEngine;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the checkout branch dialog (docs/avalonia-port/PLAN.md, phase 2, batch 5).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  The Avalonia port of <c>FormCheckoutBranch.DoDefaultActionOrShow</c>: checks out without the dialog if the settings
    ///  allow it, otherwise shows it.
    /// </summary>
    /// <param name="notCancelled">
    ///  Whether the checkout was not cancelled (<c>DoDefaultActionOrShow(owner) != DialogResult.Cancel</c>).
    /// </param>
    public static bool TryShowCheckoutBranch(IWin32Window? owner, IGitUICommands commands, string branch, bool remote, IReadOnlyList<ObjectId>? containObjectIds, out bool notCancelled)
    {
        notCancelled = false;
        // The dirty check is very expensive on large repositories, so it is optional (as in FormCheckoutBranch).
        IGitModule module = commands.Module;
        CheckoutBranchOptions options = new(
            branch,
            remote,
            AppSettings.CheckForUncommittedChangesInCheckoutBranch ? module.IsDirtyDir() : null,
            AppSettings.CheckoutBranchAction,
            AppSettings.CreateLocalBranchForRemote,
            AppSettings.AlwaysShowCheckoutBranchDlg,
            AppSettings.UseDefaultCheckoutBranchAction);

        // Git and message boxes are owned by the caller when checking out without the dialog, by the dialog otherwise.
        IWin32Window? currentOwner = owner;
        CheckoutBranchHost host = new(commands, containObjectIds, () => currentOwner);
        CheckoutBranchViewModel viewModel = new(
            ViewStrings.Load<CheckoutBranchStrings>(),
            options,
            commands.GetRequiredService<IGitBranchNameNormaliser>(),
            new GitBranchNameOptions(AppSettings.AutoNormaliseSymbol),
            AppSettings.AutoNormaliseBranchName,
            host,
            new OwnerMessageBoxService(() => currentOwner));

        if (viewModel.CanCheckoutWithoutDialog)
        {
            notCancelled = viewModel.PerformCheckout(isVisible: false) != CheckoutOutcome.Cancelled;
            return true;
        }

        notCancelled = ShowDialog(
            () =>
            {
                CheckoutBranchWindow window = new() { DataContext = viewModel };
                currentOwner = new NativeWindowOwner(window);
                return window;
            },
            owner,
            positionName: nameof(FormCheckoutBranch));
        return true;
    }

    /// <summary>Message boxes owned by a WinForms window, or by the Avalonia dialog through its handle.</summary>
    private sealed class OwnerMessageBoxService(Func<IWin32Window?> owner) : IMessageBoxService
    {
        public void ShowError(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.ShowError(owner(), text, caption));

        public void ShowInformation(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(owner(), text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information));

        public void ShowWarning(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(owner(), text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning));

        public bool Confirm(string text, string caption, bool defaultNo = false)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(
                owner(), text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, defaultNo ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1) == DialogResult.Yes);

        public bool? ConfirmWithCancel(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(owner(), text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) switch
            {
                DialogResult.Yes => true,
                DialogResult.No => (bool?)false,
                _ => null,
            });
    }

    /// <summary>Lets event scripts run for a WinForms owner (see <see cref="ScriptHost"/> for an Avalonia dialog).</summary>
    private sealed class OwnerScriptHost(Func<IWin32Window?> owner, IGitUICommands commands) : IGitModuleForm, IScriptOptionsForm, IWin32Window
    {
        public IGitUICommands UICommands => commands;

        public nint Handle => owner()?.Handle ?? 0;

        public IScriptOptionsProvider GetScriptOptionsProvider() => ScriptOptionsProviderBase.Default;
    }

    private sealed class CheckoutBranchHost(IGitUICommands commands, IReadOnlyList<ObjectId>? containObjectIds, Func<IWin32Window?> owner) : ICheckoutBranchHost
    {
        private IReadOnlyList<IGitRef>? _localBranches;
        private IReadOnlyList<IGitRef>? _remoteBranches;

        private IGitModule Module => commands.Module;

        public bool HasContainFilter => containObjectIds is not null;

        public IReadOnlyList<string> GetBranches(bool remote)
        {
            if (containObjectIds is null)
            {
                return [.. GetRefs(remote).Select(b => b.Name)];
            }

            // As FormCheckoutBranch.PopulateBranches: the branches that contain all the commits.
            HashSet<string> result = [];
            for (int index = 0; index < containObjectIds.Count; index++)
            {
                IEnumerable<string> branches = Module.GetAllBranchesWhichContainGivenCommit(containObjectIds[index], getLocal: !remote, getRemote: remote, cancellationToken: default)
                    .Where(a => !DetachedHeadParser.IsDetachedHead(a) && !a.EndsWith("/HEAD"));
                if (index == 0)
                {
                    result.UnionWith(branches);
                }
                else
                {
                    result.IntersectWith(branches);
                }
            }

            return [.. result];
        }

        public IReadOnlyList<string> GetRemoteNames() => Module.GetRemoteNames();

        public string? GetLocalTrackingBranchName(string remote, string remoteBranch) => Module.GetLocalTrackingBranchName(remote, remoteBranch);

        public ObjectId GetBranchObjectId(string branch, bool remote)
            => GetRefs(remote).FirstOrDefault(head => head.Name.Equals(branch, StringComparison.OrdinalIgnoreCase))?.ObjectId ?? default;

        public ObjectId GetMergeBase(ObjectId a, ObjectId b) => Module.GetMergeBase(a, b);

        public void RequestCommitCount(string branch, Action<string> report)
        {
            IGitModule module = Module;
            ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;

                // Not applicable if there is no checkout yet.
                ObjectId currentCheckout = module.GetCurrentCheckout();
                string text = currentCheckout.IsZero ? "" : module.GetCommitCountString(currentCheckout, branch);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                report(text);
            });
        }

        public bool IsValidBranchName(string branchName) => Module.CheckBranchFormat(branchName);

        public bool IsDirtyDir() => Module.IsDirtyDir();

        public void SaveDefaultLocalChangesAction(LocalChangesAction action) => AppSettings.CheckoutBranchAction = action;

        public void StashSave() => AvaloniaUi.RunInHostContext(() => commands.StashSave(owner(), AppSettings.IncludeUntrackedFilesInAutoStash));

        public CheckoutOutcome Checkout(string branch, bool remote, LocalChangesAction localChanges, CheckoutNewBranchMode newBranchMode, string? newBranchName, bool stashed)
            => AvaloniaUi.RunInHostContext(() =>
            {
                // As the end of FormCheckoutBranch.PerformCheckout.
                ObjectId originalId = Module.GetCurrentCheckout();
                IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
                OwnerScriptHost scriptHost = new(owner, commands);
                if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforeCheckout, scriptHost))
                {
                    return CheckoutOutcome.Cancelled;
                }

                if (!commands.StartCommandLineProcessDialog(owner(), Commands.CheckoutBranch(branch, remote, localChanges, newBranchMode, newBranchName)))
                {
                    return CheckoutOutcome.Failed;
                }

                if (stashed && ConfirmApplyStash())
                {
                    commands.StashPop(owner());
                }

                if (originalId != Module.GetCurrentCheckout())
                {
                    commands.UpdateSubmodules(owner());
                }

                scriptsRunner.RunEventScripts(ScriptEvent.AfterCheckout, scriptHost);
                return CheckoutOutcome.Succeeded;
            });

        private bool ConfirmApplyStash()
        {
            bool? applyStash = AppSettings.AutoPopStashAfterCheckoutBranch;
            if (applyStash is not null)
            {
                return applyStash.Value;
            }

            CheckoutBranchStrings strings = ViewStrings.Load<CheckoutBranchStrings>();
            TaskDialogPage page = new()
            {
                Text = strings.ApplyStashedItemsAgain.Text,
                Caption = strings.ApplyStashedItemsAgainCaption.Text,
                Icon = TaskDialogIcon.Information,
                Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
                Verification = new TaskDialogVerificationCheckBox { Text = TranslatedStrings.DontShowAgain },
                AllowCancel = true,
                SizeToContent = true,
            };

            TaskDialogButton answer = TaskDialog.ShowDialog(owner()?.Handle ?? 0, page);
            applyStash = answer == TaskDialogButton.Yes;

            // Dismissing the dialog is not an answer, so it must not be remembered as one.
            if (page.Verification.Checked && answer != TaskDialogButton.Cancel)
            {
                AppSettings.AutoPopStashAfterCheckoutBranch = applyStash;
            }

            return applyStash.Value;
        }

        private IReadOnlyList<IGitRef> GetRefs(bool remote)
            => remote
                ? _remoteBranches ??= Module.GetRefs(RefsFilter.Remotes)
                : _localBranches ??= Module.GetRefs(RefsFilter.Heads);
    }
}
