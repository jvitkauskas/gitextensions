using System.Diagnostics;
using GitCommands;
using GitCommands.Git;
using GitCommands.Git.Tag;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUI.ScriptsEngine;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the create branch and create tag dialogs (docs/avalonia-port/PLAN.md, phase 2, batch 3).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <param name="options">The options of <c>FormCreateBranch</c>; the branch name is the default prefix.</param>
    public static bool TryShowCreateBranch(IWin32Window? owner, IGitUICommands commands, ObjectId objectId, CreateBranchOptions options, out bool created)
    {
        created = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormCreateBranch)))
        {
            return false;
        }

        // As the FormCreateBranch constructor.
        IGitModule module = commands.Module;
        if (objectId.IsArtificial)
        {
            objectId = default;
        }

        if (objectId.IsZero)
        {
            objectId = module.GetCurrentCheckout();
        }

        string? branchName = options.BranchName;
        if (!objectId.IsZero)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                GitRevision revision = module.GetRevision(objectId, shortFormat: true, loadRefs: true);
                IGitRef? firstRef = revision.Refs.FirstOrDefault(r => !r.IsTag) ?? revision.Refs.FirstOrDefault(r => r.IsTag);
                branchName = firstRef?.LocalName;
            }
        }
        else if (!module.IsBareRepository())
        {
            options = options with { IsOrphanOnly = true, CouldBeOrphan = true };
        }

        options = options with { BranchName = branchName };
        created = ShowDialog(
            () =>
            {
                CreateBranchWindow window = new();
                MessageBoxService messageBoxes = new(window);
                CommitPickerViewModel commitPicker = new(new CommitPickerHost(window, commands), messageBoxes, TranslatedStrings.Error);
                window.DataContext = new CreateBranchViewModel(
                    ViewStrings.Load<CreateBranchStrings>(),
                    ViewStrings.Load<CommitSummaryStrings>(),
                    commitPicker,
                    options,
                    commands.GetRequiredService<IGitBranchNameNormaliser>(),
                    new GitBranchNameOptions(AppSettings.AutoNormaliseSymbol),
                    AppSettings.AutoNormaliseBranchName,
                    new CreateBranchHost(window, commands),
                    messageBoxes);
                if (!objectId.IsZero)
                {
                    commitPicker.SetSelectedCommitHash(objectId.ToString());
                }

                return window;
            },
            owner);
        return true;
    }

    public static bool TryShowCreateTag(IWin32Window? owner, IGitUICommands commands, ObjectId objectId, out bool created)
    {
        created = false;
        if (!AvaloniaUi.IsEnabledFor(nameof(FormCreateTag)))
        {
            return false;
        }

        // As FormCreateTag.
        IGitModule module = commands.Module;
        if (objectId.IsArtificial)
        {
            objectId = default;
        }

        if (objectId.IsZero)
        {
            objectId = module.GetCurrentCheckout();
        }

        string remote = module.GetCurrentRemote();
        if (string.IsNullOrEmpty(remote))
        {
            remote = "origin";
        }

        created = ShowDialog(
            () =>
            {
                CreateTagWindow window = new();
                MessageBoxService messageBoxes = new(window);
                CreateTagStrings strings = ViewStrings.Load<CreateTagStrings>();
                CommitPickerViewModel commitPicker = new(new CommitPickerHost(window, commands), messageBoxes, TranslatedStrings.Error);
                window.DataContext = new CreateTagViewModel(strings, commitPicker, remote, new CreateTagHost(window, commands, strings), messageBoxes, TranslatedStrings.Error);
                if (!objectId.IsZero)
                {
                    commitPicker.SetSelectedCommitHash(objectId.ToString());
                }

                return window;
            },
            owner,
            positionName: nameof(FormCreateTag));
        return true;
    }

    private sealed class CommitPickerHost(DialogWindow window, IGitUICommands commands) : ICommitPickerHost
    {
        public ObjectId RevParse(string? revision) => commands.Module.RevParse(revision!);

        public void RequestCommitCount(ObjectId selected, Action<string> report)
        {
            // As CommitPickerSmallControl.SetSelectedCommitHash.
            IGitModule module = commands.Module;
            ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                ObjectId currentCheckout = module.GetCurrentCheckout();
                if (currentCheckout.IsZero)
                {
                    return;
                }

                string toRef = selected.IsArtificial ? "HEAD" : selected.ToString();
                string text = module.GetCommitCountString(currentCheckout, toRef);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                report(text);
            });
        }

        public string? ChooseCommit(ObjectId? current) => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryChooseCommit(new NativeWindowOwner(window), commands, current?.ToString(), out GitRevision? chosen))
            {
                return chosen?.Guid;
            }

            using FormChooseCommit chooseForm = new(commands, current?.ToString());
            return chooseForm.ShowDialog(new NativeWindowOwner(window)) == DialogResult.OK ? chooseForm.SelectedRevision?.Guid : null;
        });
    }

    private sealed class CreateBranchHost(DialogWindow window, IGitUICommands commands) : ICreateBranchHost
    {
        public CommitSummary GetSummary(ObjectId objectId) => ToCommitSummary(commands.Module.GetRevision(objectId, shortFormat: true, loadRefs: true));

        public bool IsValidBranchName(string branchName) => commands.Module.CheckBranchFormat(branchName);

        public bool CreateBranch(string branchName, ObjectId objectId, bool checkout, bool orphan, bool clearOrphan) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormCreateBranch.cmdOk_Click.
            NativeWindowOwner owner = new(window);
            IGitModule module = commands.Module;
            try
            {
                ObjectId originalHash = module.GetCurrentCheckout();
                ArgumentString command = orphan
                    ? Commands.CreateOrphan(branchName, objectId)
                    : Commands.Branch(branchName, objectId, checkout);

                bool success = FormProcess.ShowDialog(owner, commands, command, module.WorkingDir, input: null, useDialogSettings: true);
                if (orphan && success && clearOrphan)
                {
                    FormProcess.ShowDialog(owner, commands, Commands.Remove(), module.WorkingDir, input: null, useDialogSettings: true);
                }

                if (success && checkout && objectId != originalHash)
                {
                    commands.UpdateSubmodules(owner);
                }

                return success;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }
        });
    }

    private sealed class CreateTagHost(DialogWindow window, IGitUICommands commands, CreateTagStrings strings) : ICreateTagHost
    {
        public bool CreateTag(GitCreateTagArgs args) => AvaloniaUi.RunInHostContext(()
            => new GitTagController(commands).CreateTag(args, new NativeWindowOwner(window)));

        public void PushTag(string remote, string tagName) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormCreateTag.PushTag.
            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            ScriptHost scriptHost = new(window, commands);
            if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforePush, scriptHost))
            {
                return;
            }

            using FormRemoteProcess form = new(commands, Commands.PushTag(remote, tagName, false))
            {
                Remote = remote,
                Text = string.Format(strings.PushTo.Text, remote),
            };
            form.ShowDialog(new NativeWindowOwner(window));

            if (!commands.Module.InTheMiddleOfAction() && !form.ErrorOccurred())
            {
                scriptsRunner.RunEventScripts(ScriptEvent.AfterPush, scriptHost);
            }
        });
    }
}
