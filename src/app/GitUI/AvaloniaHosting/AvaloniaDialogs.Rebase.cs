using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the rebase dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>As <c>FormRebase.Skipped</c>: the commits skipped during the current rebase, kept across the dialogs.</summary>
    private static readonly List<PatchItem> _rebaseSkipped = [];

    /// <summary>Shows the Avalonia port of <c>FormRebase</c> (the arguments of its constructor).</summary>
    public static bool TryShowRebase(IWin32Window? owner, IGitUICommands commands, string? from, string? to, string? defaultBranch, bool interactive, bool startRebaseImmediately)
    {
        RebaseDialogOptions options = new(
            from,
            to,
            defaultBranch,
            interactive,
            startRebaseImmediately,
            SupportUpdateRefs: commands.Module.GitVersion.SupportUpdateRefs,
            AlwaysShowAdvancedOptions: AppSettings.AlwaysShowAdvOpt);

        // FormRebase does not restore its position (enablePositionRestore: false).
        ShowDialog(
            () =>
            {
                RebaseWindow window = new();
                MessageBoxService messageBoxes = new(window);
                PatchGridViewModel patchGrid = new(ViewStrings.Load<PatchGridStrings>(), new PatchGridHost(commands), messageBoxes, isManagingRebase: true, _rebaseSkipped, TranslatedStrings.Error);
                window.DataContext = new RebaseViewModel(
                    ViewStrings.Load<RebaseStrings>(),
                    patchGrid,
                    CreateHelpImage("Rebase"),
                    new RebaseHost(commands, window),
                    messageBoxes,
                    options,
                    TranslatedStrings.Error);
                return window;
            },
            owner);
        return true;
    }

    private sealed class RebaseHost(IGitUICommands commands, DialogWindow window) : IRebaseHost
    {
        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        public string GetSelectedBranch() => Module.GetSelectedBranch();

        public IReadOnlyList<RebaseRef> GetRefs()
            => [.. Module.GetRefs(RefsFilter.Heads | RefsFilter.Remotes | RefsFilter.Tags).OfType<GitRef>().Select(r => new RebaseRef(r.Name, r.IsHead))];

        public bool InTheMiddleOfRebase() => Module.InTheMiddleOfRebase();

        public bool InTheMiddleOfConflictedMerge() => Module.InTheMiddleOfConflictedMerge();

        public bool InTheMiddleOfAction() => Module.InTheMiddleOfAction();

        public bool InTheMiddleOfPatch() => Module.InTheMiddleOfPatch();

        public bool IsDirtyDir() => Module.IsDirtyDir();

        public bool? GetEffectiveBoolSetting(string name) => Module.GetEffectiveSetting<bool>(name);

        public bool RebaseAutoStash
        {
            get => AppSettings.RebaseAutoStash;
            set => AppSettings.RebaseAutoStash = value;
        }

        public string RunGit(ArgumentString arguments) => AvaloniaUi.RunInHostContext(() =>
        {
            ProcessDialogs.ShowProcess(Owner, commands, arguments, Module.WorkingDir, input: null, useDialogSettings: true, out string cmdOutput);
            return cmdOutput;
        });

        public string ReadGit(ArgumentString arguments)
            => AvaloniaUi.RunInHostContext(() => ProcessDialogs.ReadProcess(Owner, commands, arguments, Module.WorkingDir, input: null, useDialogSettings: true));

        public bool CanContinueAction(string output) => Module.CanContinueAction(output);

        public void ResolveConflicts() => AvaloniaUi.RunInHostContext(() => commands.StartResolveConflictsDialog(Owner));

        public void AddFiles() => AvaloniaUi.RunInHostContext(() => commands.StartAddFilesDialog(Owner));

        public void Commit() => AvaloniaUi.RunInHostContext(() => commands.StartCommitDialog(Owner));

        /// <summary>As <c>FormRebase.btnChooseFromRevision_Click</c>.</summary>
        public string? ChooseFromRevision(string from, string onto) => AvaloniaUi.RunInHostContext(() =>
        {
            bool previousValueBranchFilterEnabled = AppSettings.BranchFilterEnabled;
            bool previousValueShowCurrentBranchOnly = AppSettings.ShowCurrentBranchOnly;
            bool previousValueShowReflogReferences = AppSettings.ShowReflogReferences;
            bool previousValueShowStashes = AppSettings.ShowStashes;

            try
            {
                AppSettings.ShowStashes = false;
                ObjectId firstParent = Module.RevParse("HEAD~");
                string preSelectedCommit = !string.IsNullOrWhiteSpace(from) ? from : firstParent.IsZero ? string.Empty : firstParent.ToString();

                string? mergeBaseCommitId = null;

                if (!string.IsNullOrWhiteSpace(onto))
                {
                    try
                    {
                        ObjectId commit1 = Module.RevParse(onto);
                        ObjectId commit2 = Module.RevParse("HEAD");
                        ObjectId mergeBase = Module.GetMergeBase(commit1, commit2);
                        mergeBaseCommitId = mergeBase.IsZero ? null : mergeBase.ToString();
                    }
                    catch (Exception)
                    {
                        // if an exception occurs, display whole history
                    }
                }

                return TryChooseCommit(Owner, commands, preSelectedCommit, out GitRevision? chosen, showCurrentBranchOnly: true, lastRevisionToDisplayHash: mergeBaseCommitId)
                    ? chosen?.ObjectId.ToShortString()
                    : null;
            }
            finally
            {
                AppSettings.ShowStashes = previousValueShowStashes;
                AppSettings.BranchFilterEnabled.Value = previousValueBranchFilterEnabled;
                AppSettings.ShowCurrentBranchOnly.Value = previousValueShowCurrentBranchOnly;
                AppSettings.ShowReflogReferences.Value = previousValueShowReflogReferences;
            }
        });
    }
}
