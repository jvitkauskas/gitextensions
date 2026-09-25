using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the stash dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowStash(IWin32Window? owner, IGitUICommands commands, bool manageStashes, string? initialStash)
    {
        ShowDialog(
            () =>
            {
                StashWindow window = new();
                StashViewModel viewModel = new(
                    ViewStrings.Load<StashStrings>(),
                    new StashHost(commands, window),
                    new FileViewerHost(commands),
                    ViewStrings.Load<FileStatusListStrings>(),
                    GetFileStatusTreeOptions(),
                    manageStashes,
                    initialStash);
                UseFileStatusListMenu(viewModel.Files, commands, window);
                window.Hotkeys = LoadHotkeys(commands, HotkeyCommands.StashSettingsName);
                window.DataContext = viewModel;
                return window;
            },
            owner,
            positionName: "FormStash");
        return true;
    }

    private sealed class StashHost(IGitUICommands commands, DialogWindow window) : IStashHost
    {
        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        public IReadOnlyList<GitStash> GetStashes() => [.. Module.GetStashes(noLocks: false)];

        /// <summary>As <c>FormStash.InitializeSoft</c> and <c>LoadGitItemStatuses</c>.</summary>
        public async Task<IReadOnlyList<FileStatusGroup>> GetFilesAsync(GitStash? stash, CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<FileStatusGroup> groups = stash is null ? GetWorkingDirectoryFiles() : GetStashFiles(stash);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return groups;
        }

        private IReadOnlyList<FileStatusGroup> GetWorkingDirectoryFiles()
        {
            IReadOnlyList<GitItemStatus> gitItemStatuses = Module.GetAllChangedFiles();

            // FileStatusList has no interface for both worktree<-index, index<-HEAD at the same time
            ObjectId headId = Module.RevParse("HEAD");
            GitRevision workTreeRev = new(ObjectId.WorkTreeId) { ParentIds = [ObjectId.IndexId] };
            if (headId.IsZero)
            {
                // Likely a detached head (as FileStatusList.SetDiffs)
                return [new FileStatusGroup(null, workTreeRev, TranslatedStrings.DiffWithParent + ObjectId.WorkTreeId.ToShortString(), gitItemStatuses)];
            }

            GitRevision headRev = new(headId);
            GitRevision indexRev = new(ObjectId.IndexId) { ParentIds = [headId] };

            // As FileStatusList.SetStashDiffs.
            return
            [
                new FileStatusGroup(indexRev, workTreeRev, ResourceManager.TranslatedStrings.Workspace, [.. gitItemStatuses.Where(item => item.Staged != StagedStatus.Index)]),
                new FileStatusGroup(headRev, indexRev, ResourceManager.TranslatedStrings.Index, [.. gitItemStatuses.Where(item => item.Staged == StagedStatus.Index)]),
            ];
        }

        private IReadOnlyList<FileStatusGroup> GetStashFiles(GitStash stash)
        {
            IReadOnlyList<GitItemStatus> gitItemStatuses = Module.GetStashDiffFiles(stash.Name);
            ObjectId firstId = Module.RevParse(stash.Name + "^");
            GitRevision? firstRev = firstId.IsZero ? null : new(firstId);

            ObjectId selectedId = Module.RevParse(stash.Name);
            if (selectedId.IsZero)
            {
                throw new InvalidOperationException("selectedId must not be zero");
            }

            GitRevision secondRev = new(selectedId);
            if (!firstId.IsZero)
            {
                secondRev.ParentIds = [firstId];
            }

            // As FileStatusList.SetDiffs.
            return [new FileStatusGroup(firstRev, secondRev, TranslatedStrings.DiffWithParent + (firstRev?.ObjectId.ToShortString() ?? ""), gitItemStatuses)];
        }

        public void Save(bool includeUntrackedFiles, bool keepIndex, string message, IReadOnlyList<string>? files)
            => AvaloniaUi.RunInHostContext(() => commands.StashSave(Owner, includeUntrackedFiles, keepIndex, message, files));

        /// <summary>As <c>FormStash.ClearClick</c>.</summary>
        public bool ConfirmDrop() => AvaloniaUi.RunInHostContext(() =>
        {
            if (AppSettings.DontConfirmStashDrop)
            {
                return true;
            }

            TaskDialogPage page = new()
            {
                Text = TranslatedStrings.AreYouSure,
                Caption = TranslatedStrings.StashDropConfirmTitle,
                Heading = TranslatedStrings.CannotBeUndone,
                Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
                Icon = TaskDialogIcon.Information,
                Verification = new TaskDialogVerificationCheckBox { Text = TranslatedStrings.DontShowAgain },
                SizeToContent = true
            };
            TaskDialogButton result = TaskDialog.ShowDialog(Owner.Handle, page);
            if (page.Verification.Checked)
            {
                AppSettings.DontConfirmStashDrop = true;
            }

            return result == TaskDialogButton.Yes;
        });

        public void Drop(string stashName) => AvaloniaUi.RunInHostContext(() => commands.StashDrop(Owner, stashName));

        public void Apply(string stashName) => AvaloniaUi.RunInHostContext(() => commands.StashApply(Owner, stashName));

        public (bool KeepIndex, bool IncludeUntrackedFiles) LoadSettings()
            => (AppSettings.StashKeepIndex, AppSettings.IncludeUntrackedFilesInManualStash);

        public void SaveSettings(bool keepIndex, bool includeUntrackedFiles)
        {
            AppSettings.StashKeepIndex = keepIndex;
            AppSettings.IncludeUntrackedFilesInManualStash = includeUntrackedFiles;
        }
    }
}
