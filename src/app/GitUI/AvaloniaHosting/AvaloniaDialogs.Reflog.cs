using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the reflog dialog (docs/avalonia-port/PLAN.md, phase 2, batch 9).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowReflog(IWin32Window? owner, IGitUICommands commands)
    {
        // As FormReflog_Load.
        IGitModule module = commands.Module;
        bool isDirty = module.IsDirtyDir();
        string currentBranch = module.GetSelectedBranch();
        List<string> references =
        [
            "HEAD",
            .. module.GetRefs(RefsFilter.Heads).Select(r => r.Name).OrderBy(n => n),
            .. module.GetRemoteBranches().Select(r => r.Name).OrderBy(n => n),
        ];

        ShowDialog(
            () =>
            {
                ReflogWindow window = new();
                window.DataContext = new ReflogViewModel(
                    ViewStrings.Load<ReflogStrings>(),
                    references,
                    currentBranch,
                    isBranchCheckedOut: currentBranch != DetachedHeadParser.DetachedBranch,
                    isDirty,
                    new ReflogHost(commands, window),
                    new MessageBoxService(window));
                return window;
            },
            owner,
            positionName: "FormReflog");
        return true;
    }

    private sealed class ReflogHost(IGitUICommands commands, DialogWindow window) : IReflogHost
    {
        private IWin32Window Owner => new NativeWindowOwner(window);

        public void LoadReflog(string reference, Action<string> report)
        {
            IGitModule module = commands.Module;
            ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                GitArgumentBuilder arguments = new("reflog")
                {
                    "--no-abbrev",
                    reference
                };
                string output = module.GitExecutable.GetOutput(arguments);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                report(output);
            });
        }

        public bool CreateBranch(string sha) => AvaloniaUi.RunInHostContext(() => commands.DoActionOnRepo(() =>
        {
            // As FormReflog.createABranchOnThisCommitToolStripMenuItem_Click.
            ObjectId objectId = ObjectId.Parse(sha);
            return TryShowCreateBranch(Owner, commands, objectId, new(BranchName: null, CheckoutAfterCreation: false, UserAbleToChangeRevision: false, CouldBeOrphan: false), out bool created) && created;
        }));

        public bool ResetCurrentBranch(string sha, bool soft) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormReflog.resetCurrentBranchOnThisCommitToolStripMenuItem_Click.
            GitRevision revision = commands.Module.GetRevision(ObjectId.Parse(sha));
            ResetCurrentBranchType resetType = soft ? ResetCurrentBranchType.Soft : ResetCurrentBranchType.Hard;
            return commands.DoActionOnRepo(() =>
            {
                return TryShowResetCurrentBranch(Owner, commands, revision, resetType, out bool reset) && reset;
            });
        });

        public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);
    }
}
