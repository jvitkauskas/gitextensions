using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the apply patch dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>As <c>FormApplyPatch.Skipped</c>: the patches skipped while applying the current ones, kept across the dialogs.</summary>
    private static readonly List<PatchItem> _applyPatchSkipped = [];

    /// <summary>
    ///  Shows the Avalonia port of <c>FormApplyPatch</c>, with the patch file or directory to apply (as <c>StartApplyPatchDialog</c>
    ///  calls <c>SetPatchDir</c> or <c>SetPatchFile</c>).
    /// </summary>
    public static bool TryShowApplyPatch(IWin32Window? owner, IGitUICommands commands, string? patchFile)
    {
        bool isDirectory = Directory.Exists(patchFile!);
        ShowDialog(
            () =>
            {
                ApplyPatchWindow window = new();
                MessageBoxService messageBoxes = new(window);
                PatchGridViewModel patchGrid = new(ViewStrings.Load<PatchGridStrings>(), new PatchGridHost(commands), messageBoxes, isManagingRebase: false, _applyPatchSkipped, TranslatedStrings.Error);
                window.DataContext = new ApplyPatchViewModel(
                    ViewStrings.Load<ApplyPatchStrings>(),
                    patchGrid,
                    new ApplyPatchHost(commands, window),
                    messageBoxes,
                    new AvaloniaFileDialogService(window),
                    commands.Module.WorkingDir,
                    patchFile: isDirectory ? null : patchFile ?? "",
                    patchDirectory: isDirectory ? patchFile : null,
                    TranslatedStrings.Error);
                return window;
            },
            owner,
            positionName: nameof(FormApplyPatch));
        return true;
    }

    private sealed class ApplyPatchHost(IGitUICommands commands, DialogWindow window) : IApplyPatchHost
    {
        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        public bool InTheMiddleOfPatch() => Module.InTheMiddleOfPatch();

        public bool InTheMiddleOfConflictedMerge() => Module.InTheMiddleOfConflictedMerge();

        public bool InTheMiddleOfAction() => Module.InTheMiddleOfAction();

        public string? GetPathForGitExecution(string? path) => Module.GetPathForGitExecution(path);

        public void RunGit(ArgumentString arguments)
            => AvaloniaUi.RunInHostContext(() => FormProcess.ShowDialog(Owner, commands, arguments, Module.WorkingDir, input: null, useDialogSettings: true));

        public void ApplyPatchDirectory(string directory, ArgumentString arguments) => Module.ApplyPatch(directory, arguments);

        public void NotifyRepoChanged() => commands.RepoChangedNotifier.Notify();

        public void ResolveConflicts() => AvaloniaUi.RunInHostContext(() => commands.StartResolveConflictsDialog(Owner));

        public void AddFiles() => AvaloniaUi.RunInHostContext(() => commands.StartAddFilesDialog(Owner));

        public (bool IgnoreWhitespace, bool SignOff) LoadSettings() => (AppSettings.ApplyPatchIgnoreWhitespace, AppSettings.ApplyPatchSignOff);

        public void SaveIgnoreWhitespace(bool value) => AppSettings.ApplyPatchIgnoreWhitespace = value;

        public void SaveSignOff(bool value) => AppSettings.ApplyPatchSignOff = value;
    }
}
