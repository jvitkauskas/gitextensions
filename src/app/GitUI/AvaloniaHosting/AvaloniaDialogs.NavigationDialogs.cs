using System.Diagnostics;
using System.Text;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.CommandsDialogs.BrowseDialog.DashboardControl;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUI.ScriptsEngine;
using GitUIPluginInterfaces;
using Microsoft;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of navigation and small repository dialogs (docs/avalonia-port/PLAN.md, phase 2, batch 4).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowCheckoutRevision(IWin32Window? owner, IGitUICommands commands, string? revision, out bool checkedOut)
    {
        checkedOut = false;
        checkedOut = ShowDialog(
            () =>
            {
                CheckoutRevisionWindow window = new();
                MessageBoxService messageBoxes = new(window);
                CommitPickerViewModel commitPicker = new(new CommitPickerHost(window, commands), messageBoxes, TranslatedStrings.Error);
                window.DataContext = new CheckoutRevisionViewModel(
                    ViewStrings.Load<CheckoutRevisionStrings>(), commitPicker, new CheckoutRevisionHost(window, commands), messageBoxes);
                commitPicker.SetSelectedCommitHash(revision);
                return window;
            },
            owner);
        return true;
    }

    /// <param name="branchName">The branch to compare to, <see langword="null"/> if cancelled.</param>
    public static bool TryShowCompareToBranch(IWin32Window? owner, IGitUICommands commands, ObjectId commitToCompare, out string? branchName)
    {
        branchName = null;
        CompareToBranchViewModel viewModel = new(
            ViewStrings.Load<CompareToBranchStrings>(),
            new LocalRemoteBranchSelectorViewModel(
                ViewStrings.Load<LocalRemoteBranchSelectorStrings>(), remote: true, new LocalRemoteBranchSelectorHost(commands, commitToCompare)));
        if (ShowDialog(() => new CompareToBranchWindow { DataContext = viewModel }, owner, positionName: "FormCompareToBranch"))
        {
            branchName = viewModel.BranchName;
        }

        return true;
    }

    public static bool TryShowBisect(IWin32Window? owner, IGitUICommands commands, IRevisionGridInfo revisionGrid)
    {
        ShowDialog(
            () =>
            {
                BisectWindow window = new();
                window.DataContext = new BisectViewModel(ViewStrings.Load<BisectStrings>(), new BisectHost(window, commands, revisionGrid), new MessageBoxService(window));
                return window;
            },
            owner,
            positionName: "FormBisect");
        return true;
    }

    /// <param name="commitId">The commit to go to (zero if the revision could not be resolved); only set if accepted.</param>
    public static bool TryShowGoToCommit(IWin32Window? owner, IGitUICommands commands, out bool accepted, out ObjectId commitId)
    {
        accepted = false;
        commitId = default;

        // As FormGoToCommit: at most 1000 refs per list, and a revision on the clipboard is offered.
        const int maxDropDownCount = 1_000;
        IGitModule module = commands.Module;
        IReadOnlyList<GitRefItem> tags = [.. module.GetRefs(RefsFilter.Tags).Take(maxDropDownCount).Select(r => new GitRefItem(r.LocalName, r.Guid ?? ""))];
        IReadOnlyList<GitRefItem> branches = [.. module.GetRefs(RefsFilter.Heads).Take(maxDropDownCount).Select(r => new GitRefItem(r.LocalName, r.Guid ?? ""))];
        string clipboardText = Clipboard.GetText().Trim();
        string? clipboardRevision = !string.IsNullOrEmpty(clipboardText) && !module.RevParse(clipboardText).IsZero ? clipboardText : null;

        GoToCommitViewModel viewModel = new(
            ViewStrings.Load<GoToCommitStrings>(),
            tags,
            branches,
            clipboardRevision,
            () => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(@"https://git-scm.com/docs/git-rev-parse#_specifying_revisions")));
        accepted = ShowDialog(() => new GoToCommitWindow { DataContext = viewModel }, owner, positionName: "FormGoToCommit");
        if (accepted)
        {
            commitId = module.RevParse(viewModel.SelectedRevision);
        }

        return true;
    }

    /// <param name="name">The entered category name, <see langword="null"/> if cancelled.</param>
    public static bool TryShowDashboardCategoryTitle(IWin32Window? owner, IReadOnlyList<string> existingCategories, string? originalName, out string? name)
    {
        name = null;
        DashboardCategoryTitleViewModel? viewModel = null;
        ShowDialog(
            () =>
            {
                DashboardCategoryTitleWindow window = new();
                viewModel = new DashboardCategoryTitleViewModel(ViewStrings.Load<DashboardCategoryTitleStrings>(), existingCategories, originalName, new MessageBoxService(window));
                window.DataContext = viewModel;
                return window;
            },
            owner);
        name = viewModel!.Category;
        return true;
    }

    public static bool TryShowAddToGitIgnore(IWin32Window? owner, IGitUICommands commands, bool localExclude, IReadOnlyList<string> patterns)
    {
        using AddToGitIgnoreHost host = new(commands, localExclude);
        ShowDialog(
            () =>
            {
                AddToGitIgnoreWindow window = new();
                host.Window = window;
                window.DataContext = new AddToGitIgnoreViewModel(ViewStrings.Load<AddToGitIgnoreStrings>(), localExclude, patterns, host);
                return window;
            },
            owner,
            positionName: "FormAddToGitIgnore");
        return true;
    }

    private sealed class CheckoutRevisionHost(DialogWindow window, IGitUICommands commands) : ICheckoutRevisionHost
    {
        public bool Checkout(ObjectId objectId, bool force) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormCheckoutRevision.OkClick.
            try
            {
                NativeWindowOwner owner = new(window);
                IGitModule module = commands.Module;
                ObjectId checkedOutObjectId = module.GetCurrentCheckout();
                IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
                ScriptHost scriptHost = new(window, commands);
                if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforeCheckout, scriptHost))
                {
                    return false;
                }

                string command = Commands.Checkout(objectId.ToString(), force ? LocalChangesAction.Reset : 0);
                bool success = ProcessDialogs.ShowProcess(owner, commands, command, module.WorkingDir, input: null, useDialogSettings: true);
                if (success && objectId != checkedOutObjectId)
                {
                    commands.UpdateSubmodules(owner);
                }

                scriptsRunner.RunEventScripts(ScriptEvent.AfterCheckout, scriptHost);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }
        });
    }

    private sealed class LocalRemoteBranchSelectorHost(IGitUICommands commands, ObjectId commitToCompare) : ILocalRemoteBranchSelectorHost
    {
        public IReadOnlyList<string> GetBranches(bool remote)
            => [.. commands.Module.GetRefs(remote ? RefsFilter.Remotes : RefsFilter.Heads).Select(b => b.Name)];

        public void RequestCommitCount(string branch, Action<string> report)
        {
            // As GitUI.UserControls.BranchSelector.Branches_SelectedIndexChanged.
            IGitModule module = commands.Module;
            ObjectId currentCheckout = commitToCompare.IsZero ? module.GetCurrentCheckout() : commitToCompare;
            if (currentCheckout.IsZero)
            {
                return;
            }

            ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                string text = module.GetCommitCountString(currentCheckout, branch);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                report(text);
            });
        }
    }

    private sealed class BisectHost(DialogWindow window, IGitUICommands commands, IRevisionGridInfo revisionGrid) : IBisectHost
    {
        private IGitModule Module => commands.Module;

        public bool IsInTheMiddleOfBisect() => Module.InTheMiddleOfBisect();

        public void Start() => RunGit(Commands.StartBisect(), useDialogSettings: true);

        public bool HasSelectedRange() => revisionGrid.GetSelectedRevisions().Count > 1;

        public void MarkSelectedRange()
        {
            // As FormBisect.Start_Click.
            IReadOnlyList<GitRevision> revisions = revisionGrid.GetSelectedRevisions();
            if (RunGit(Commands.ContinueBisect(GitBisectOption.Good, revisions[0].ObjectId), useDialogSettings: true))
            {
                RunGit(Commands.ContinueBisect(GitBisectOption.Bad, revisions[^1].ObjectId), useDialogSettings: true);
            }
        }

        public void Mark(BisectMark mark)
            => RunGit(Commands.ContinueBisect(mark switch { BisectMark.Good => GitBisectOption.Good, BisectMark.Bad => GitBisectOption.Bad, _ => GitBisectOption.Skip }), useDialogSettings: false);

        public void Stop() => RunGit(Commands.StopBisect(), useDialogSettings: false);

        private bool RunGit(ArgumentString arguments, bool useDialogSettings) => AvaloniaUi.RunInHostContext(()
            => ProcessDialogs.ShowProcess(new NativeWindowOwner(window), commands, arguments, Module.WorkingDir, input: null, useDialogSettings));
    }

    private sealed class AddToGitIgnoreHost(IGitUICommands commands, bool localExclude) : IAddToGitIgnoreHost, IDisposable
    {
        private readonly AsyncLoader _ignoredFilesLoader = new();
        private bool _loadedOnce;

        public DialogWindow? Window { get; set; }

        public void RequestIgnoredFiles(IReadOnlyList<string> patterns, Action<IReadOnlyList<string>> report)
        {
            // As FormAddToGitIgnore.FilePattern_TextChanged: the first list is loaded at once, later ones after typing stops.
            _ignoredFilesLoader.Cancel();
            _ignoredFilesLoader.Delay = _loadedOnce ? TimeSpan.FromMilliseconds(300) : TimeSpan.Zero;
            _loadedOnce = true;
            ThreadHelper.FileAndForget(() => _ignoredFilesLoader.LoadAsync(() => commands.Module.GetIgnoredFiles(patterns), report));
        }

        public void AddPatterns(IReadOnlyList<string> patterns) => AvaloniaUi.RunInHostContext(() =>
        {
            // As FormAddToGitIgnore.AddToIgnoreClick.
            try
            {
                IGitModule module = commands.Module;
                string? fileName = localExclude
                    ? Path.Join(module.ResolveGitInternalPath("info"), "exclude")
                    : new FullPathResolver(() => module.WorkingDir).Resolve(".gitignore");
                Validates.NotNull(fileName);
                FileInfoExtensions.MakeFileTemporaryWritable(fileName, x =>
                {
                    StringBuilder gitIgnoreFileAddition = new();
                    if (File.Exists(fileName) && !File.ReadAllText(fileName, GitModule.SystemEncoding).EndsWith(Environment.NewLine))
                    {
                        gitIgnoreFileAddition.Append(Environment.NewLine);
                    }

                    foreach (string pattern in patterns)
                    {
                        gitIgnoreFileAddition.Append(pattern);
                        gitIgnoreFileAddition.Append(Environment.NewLine);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(x)!);
                    using StreamWriter writer = new(x, append: true, GitModule.SystemEncoding);
                    writer.Write(gitIgnoreFileAddition);
                });
            }
            catch (Exception ex)
            {
                MessageBoxes.Show(Window is null ? null : new NativeWindowOwner(Window), ex.ToString(), TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        public void Dispose() => _ignoredFilesLoader.Dispose();
    }
}
