using GitCommands;
using GitCommands.Git;
using GitCommands.Utils;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.HelperDialogs;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.ScriptsEngine;
using GitUI.UserControls.RevisionGrid;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  The context menu of the Avalonia revision grid, as <c>RevisionGridControl.ContextMenuOpening</c> fills
    ///  <c>mainContextMenu</c> for the selected revisions, with the handlers of its items.
    /// </summary>
    internal sealed class RevisionGridMenuBuilder(GitUICommands commands, Func<IWin32Window> owner, RevisionGridViewModel grid, Action refresh)
    {
        private readonly RevisionGridMenuStrings _s = ViewStrings.Load<RevisionGridMenuStrings>();
        private GitRevision? _baseCommitToCompare;

        /// <summary>Selects a reference in the left panel (<c>SelectInLeftPanel</c>), if there is one.</summary>
        public Action<string>? SelectInLeftPanel { get; set; }

        /// <summary>The filter of the grid (the main window's), which the branch and reflog items of the View menu change.</summary>
        public IRevisionGridFilterHost? Filter { get; init; }

        private IGitModule Module => commands.Module;

        public IReadOnlyList<MenuModelItem> Build()
        {
            IReadOnlyList<GitRevision> selected = grid.GetSelectedRevisionsLatestSelectedFirst();
            if (selected.Count == 0)
            {
                return [];
            }

            GitRevision revision = selected[0];
            GitRefListsForRevision refs = new(revision);
            IReadOnlyCollection<string> ambiguousRefs = GitRef.GetAmbiguousRefNames(Module.GetRefs(RefsFilter.NoFilter));
            string currentBranch = Module.GetSelectedBranch();
            string currentBranchRef = GitRefName.RefsHeadsPrefix + currentBranch;
            bool bare = Module.IsBareRepository();
            bool bareOrArtificial = bare || revision.IsArtificial;
            bool bisect = Module.InTheMiddleOfBisect();

            List<MenuModelItem> deleteTags = [];
            List<MenuModelItem> deleteBranches = [];
            List<MenuModelItem> checkoutBranches = [];
            List<MenuModelItem> mergeBranches = [];
            List<MenuModelItem> pushBranches = [];
            List<MenuModelItem> renameBranches = [];
            List<string> selectInLeftPanel = [];
            string? rebaseOnTopOf = null;

            foreach (IGitRef tag in refs.AllTags)
            {
                deleteTags.Add(RefItem(tag, () => commands.StartDeleteTagDialog(owner(), tag.Name)));
                selectInLeftPanel.Add(tag.Name);
                string name = UnambiguousName(tag);
                mergeBranches.Add(RefItem(tag, () => commands.StartMergeBranchDialog(owner(), name)));
            }

            bool currentBranchPointsToRevision = false;
            foreach (IGitRef head in refs.BranchesWithNoIdenticalRemotes)
            {
                if (head.CompleteName == currentBranchRef)
                {
                    currentBranchPointsToRevision = !revision.IsArtificial;
                }
                else
                {
                    string name = UnambiguousName(head);
                    mergeBranches.Add(RefItem(head, () => commands.StartMergeBranchDialog(owner(), name)));
                    rebaseOnTopOf ??= name;
                }
            }

            // Without a branch to rebase on or to merge, the selected commit itself.
            if (rebaseOnTopOf is null && !currentBranchPointsToRevision)
            {
                rebaseOnTopOf = revision.Guid;
            }

            if (mergeBranches.Count == 0 && !currentBranchPointsToRevision)
            {
                mergeBranches.Add(new MenuModelItem(Escape(revision.Guid), () => Run(() => commands.StartMergeBranchDialog(owner(), revision.Guid))));
            }

            bool isHeadOfCurrentBranch = false;
            bool separatorBeforeRemotes = false;
            IReadOnlyList<IGitRef> allBranches = refs.AllBranches;
            foreach (IGitRef head in allBranches)
            {
                selectInLeftPanel.Add(head.Name);
                if (!head.IsRemote)
                {
                    if (head.CompleteName == currentBranchRef)
                    {
                        isHeadOfCurrentBranch = true;
                    }
                    else
                    {
                        deleteBranches.Add(RefItem(head, () => commands.StartDeleteBranchDialog(owner(), head.Name)));
                    }

                    renameBranches.Add(RefItem(head, () => commands.StartRenameDialog(owner(), head.Name)));
                    pushBranches.Add(RefItem(head, () => commands.StartPushDialog(owner(), pushOnShow: false, forceWithLease: false, out _, head.Name)));
                }

                if (head.CompleteName != currentBranchRef)
                {
                    if (!head.IsRemote)
                    {
                        separatorBeforeRemotes = true;
                    }
                    else if (separatorBeforeRemotes)
                    {
                        checkoutBranches.Add(MenuModelItem.Separator);
                        separatorBeforeRemotes = false;
                    }

                    checkoutBranches.Add(RefItem(head, () =>
                    {
                        if (head.IsRemote)
                        {
                            commands.StartCheckoutRemoteBranch(owner(), head.Name);
                        }
                        else
                        {
                            commands.StartCheckoutBranch(owner(), head.Name);
                        }
                    }));
                }
            }

            bool firstRemote = true;
            foreach (IGitRef head in allBranches.Where(b => b.IsRemote))
            {
                if (firstRemote && deleteBranches.Count > 0)
                {
                    deleteBranches.Add(MenuModelItem.Separator);
                }

                firstRemote = false;
                deleteBranches.Add(RefItem(head, () => commands.StartDeleteRemoteBranchDialog(owner(), head.Name)));
            }

            bool isStash = !bareOrArtificial && revision.IsStash;
            List<MenuModelItem?> items =
            [
                If(revision.IsArtificial, Item(_s.ResetChanges, "ResetWorkingDirChanges", ResetChanges)),
                If(revision.IsArtificial, Item(_s.Commit, "RepoStateDirty", () => commands.StartCommitDialog(owner()))),
                If(bisect, Item(_s.MarkRevisionAsBad, null, () => ContinueBisect(GitBisectOption.Bad, revision))),
                If(bisect, Item(_s.MarkRevisionAsGood, null, () => ContinueBisect(GitBisectOption.Good, revision))),
                If(bisect, Item(_s.BisectSkipRevision, null, () => ContinueBisect(GitBisectOption.Skip, revision))),
                If(bisect, Item(_s.StopBisect, null, StopBisect)),
                MenuModelItem.Separator,
                If(!revision.IsArtificial, new MenuModelItem(_s.CopyToClipboard.AccessKeyText, Icon: "CopyToClipboard", Children: CreateCopyItems(selected))),
                MenuModelItem.Separator,
                If(isStash || (!bareOrArtificial && revision.IsAutostash), Item(_s.ApplyStash, "Stash", () => StashApply(revision))),
                If(isStash, Item(_s.PopStash, "Stash", () => StashPop(revision))),
                If(isStash, Item(_s.DropStash, "Stash", () => StashDrop(revision))),
                MenuModelItem.Separator,
                If(!bareOrArtificial && checkoutBranches.Any(i => !i.IsSeparator), SubMenu(_s.CheckoutBranch, "BranchCheckout", checkoutBranches)),
                If(!bareOrArtificial && pushBranches.Count > 0, SubMenu(_s.TsmiPushBranch, "Push", pushBranches)),
                If(!bareOrArtificial && mergeBranches.Count > 0, SubMenu(_s.MergeBranch, "Merge", mergeBranches)),
                If(!bareOrArtificial, SubMenu(_s.RebaseOn, "Rebase", CreateRebaseItems(rebaseOnTopOf, grid.GetSelectedRevisionsLatestSelectedFirst()))),
                If(!bareOrArtificial, Item(_s.ResetCurrentBranchToHere, "ResetCurrentBranchToHere", () => ResetCurrentBranch(revision))),
                MenuModelItem.Separator,
                If(SelectInLeftPanel is not null && selectInLeftPanel.Count > 0, SelectInLeftPanelItem(selectInLeftPanel)),
                If(!bareOrArtificial, Item(_s.CreateNewBranch, "BranchCreate", () => CreateBranch(revision))),
                If(!bareOrArtificial, Item(_s.ResetAnotherBranchToHere, "ResetAnotherBranchToHere", () => ResetAnotherBranch(revision))),
                If(renameBranches.Count > 0, SubMenu(_s.RenameBranch, "Renamed", renameBranches)),
                If((deleteBranches.Count > 0 && !bare) || isHeadOfCurrentBranch, SubMenu(_s.DeleteBranch, "BranchDelete", deleteBranches, enabled: deleteBranches.Count > 0 && !bare)),
                MenuModelItem.Separator,
                If(!revision.IsArtificial, Item(_s.CreateTag, "TagCreate", () => CreateTag(revision))),
                If(deleteTags.Count > 0, SubMenu(_s.DeleteTag, "TagDelete", deleteTags)),
                MenuModelItem.Separator,
                If(!bareOrArtificial, Item(_s.CheckoutRevision, "Checkout", () => commands.StartCheckoutRevisionDialog(owner(), revision.Guid))),
                If(!bareOrArtificial, Item(_s.RevertCommit, "RevertCommit", RevertCommits)),
                If(!bareOrArtificial, Item(_s.CherryPickCommit, "CherryPick", () => commands.StartCherryPickDialog(owner(), grid.GetSelectedRevisions(descending: true)))),
                If(!revision.IsArtificial, Item(_s.ArchiveRevision, "ArchiveRevision", Archive)),
                If(!bareOrArtificial, SubMenu(_s.ManipulateCommit, "Advanced", CreateManipulateItems(revision))),
                MenuModelItem.Separator,
                SubMenu(_s.Compare, "Diff", CreateCompareItems()),
                MenuModelItem.Separator,
                SubMenu(_s.Navigate, "GotoCommit", CreateNavigateItems()),
                SubMenu(_s.View, "AdvancedSettings", CreateViewItems()),
                .. CreateScriptItems(),
                If(!string.IsNullOrWhiteSpace(revision.BuildStatus?.Url), Item(_s.OpenBuildReport, "Integration", () => OsShellUtil.OpenUrlInDefaultBrowser(revision.BuildStatus!.Url!))),
                If(!string.IsNullOrWhiteSpace(revision.BuildStatus?.PullRequestUrl), Item(_s.OpenPullRequestPage, "PullRequest", () => OsShellUtil.OpenUrlInDefaultBrowser(revision.BuildStatus!.PullRequestUrl!))),
            ];

            return MenuModelItem.TrimSeparators(items.OfType<MenuModelItem>());

            string UnambiguousName(IGitRef gitRef) => ambiguousRefs.Contains(gitRef.Name) ? gitRef.CompleteName : gitRef.Name;
        }

        // As PerformFirstDropdownItemClick: a submenu of one item; its parent also runs it.
        private static MenuModelItem SubMenu(TranslatedText text, string icon, IReadOnlyList<MenuModelItem> children, bool enabled = true)
            => new(text.AccessKeyText, Icon: icon, Children: children, IsEnabled: enabled);

        private MenuModelItem Item(TranslatedText text, string? icon, Action action) => new(text.AccessKeyText, () => Run(action), icon);

        private MenuModelItem RefItem(IGitRef gitRef, Action action) => new(Escape(gitRef.Name), () => Run(action));

        private static MenuModelItem? If(bool condition, MenuModelItem item) => condition ? item : null;

        /// <summary>A text shown as is: an underscore is not an access key.</summary>
        private static string Escape(string text) => text.Replace("_", "__");

        private void Run(Action action)
        {
            AvaloniaUi.RunInHostContext(action);
            refresh();
        }

        private MenuModelItem SelectInLeftPanelItem(IReadOnlyList<string> refNames)
            => refNames.Count == 1
                ? new MenuModelItem(_s.TsmiSelectInLeftPanel.AccessKeyText, () => SelectInLeftPanel!(refNames[0]), "FileTree")
                : new MenuModelItem(_s.TsmiSelectInLeftPanel.AccessKeyText, Icon: "FileTree", Children: [.. refNames.Select(name => new MenuModelItem(Escape(name), () => SelectInLeftPanel!(name)))]);

        // As CopyContextMenuItem.OnDropDownOpening.
        private static IReadOnlyList<MenuModelItem> CreateCopyItems(IReadOnlyList<GitRevision> revisions)
        {
            List<MenuModelItem> items = [];
            List<string> branchNames = [];
            List<string> tagNames = [];
            foreach (GitRevision revision in revisions)
            {
                GitRefListsForRevision refLists = new(revision);
                branchNames.AddRange(refLists.GetAllBranchNames());
                tagNames.AddRange(refLists.GetAllTagNames());
            }

            int number = 0;
            AddNames(TranslatedStrings.Branches, branchNames, "Branch");
            AddNames(TranslatedStrings.Tags, tagNames, "Tag");

            int count = revisions.Count;
            Add(ResourceManager.TranslatedStrings.GetCommitHash(count), r => r.Guid, "CommitId", 'C');
            Add(ResourceManager.TranslatedStrings.GetMessage(count), r => r.Body ?? r.Subject, "Message", 'M');
            Add(ResourceManager.TranslatedStrings.GetAuthor(count), r => $"{r.Author} <{r.AuthorEmail}>", "Author", 'A');
            if (count == 1 && revisions[0].AuthorDate == revisions[0].CommitDate)
            {
                Add(ResourceManager.TranslatedStrings.Date, r => r.AuthorDate.ToString(), "Date", 'D');
            }
            else
            {
                Add(ResourceManager.TranslatedStrings.GetAuthorDate(count), r => r.AuthorDate.ToString(), "Date", 'T');
                Add(ResourceManager.TranslatedStrings.GetCommitDate(count), r => r.CommitDate.ToString(), "Date", 'D');
            }

            return items;

            void AddNames(string caption, List<string> names, string icon)
            {
                if (names.Count == 0)
                {
                    return;
                }

                items.Add(new MenuModelItem(caption, IsEnabled: false, IsBold: true));
                foreach (string name in names)
                {
                    // As PrependItemNumber: the first ten have their number as access key.
                    string header = ++number > 10 ? Escape(name) : $"_{number % 10}:   {Escape(name)}";
                    items.Add(new MenuModelItem(header, () => ClipboardUtil.TrySetText(name), icon));
                }

                items.Add(MenuModelItem.Separator);
            }

            void Add(string caption, Func<GitRevision, string> extract, string icon, char hotkey)
            {
                string[] texts = [.. revisions.Select(extract).Distinct()];
                string display = Escape(caption) + ":   " + Escape(texts.Select(t => t.SubstringUntil('\n')).Join(", ").ShortenTo(40));
                int position = display.IndexOf(hotkey.ToString(), StringComparison.InvariantCultureIgnoreCase);
                if (position >= 0)
                {
                    display = display.Insert(position, "_");
                }

                string text = texts.Join("\n");
                items.Add(new MenuModelItem(display.TrimEnd('\n', '\r'), () => ClipboardUtil.TrySetText(text), icon));
            }
        }

        // As RebaseOnToolStripMenuItem_DropDownOpening and its items.
        private IReadOnlyList<MenuModelItem> CreateRebaseItems(string? rebaseOnTopOf, IReadOnlyList<GitRevision> selected)
        {
            bool one = rebaseOnTopOf is not null && selected.Count == 1;
            bool advanced = rebaseOnTopOf is not null && (selected.Count == 1 || (selected.Count == 2 && selected.All(r => !r.IsArtificial)));
            return
            [
                new(_s.Rebase.AccessKeyText, () => Rebase(rebaseOnTopOf!, interactive: false), IsEnabled: one),
                new(_s.RebaseInteractively.AccessKeyText, () => Rebase(rebaseOnTopOf!, interactive: true), IsEnabled: one),
                new(_s.RebaseWithAdvOptions.AccessKeyText, () => Run(() => commands.StartRebaseDialogWithAdvOptions(owner(), rebaseOnTopOf!, selected.Count == 2 ? selected[1].ObjectId.ToShortString() : string.Empty)), IsEnabled: advanced),
            ];
        }

        private void Rebase(string onto, bool interactive) => Run(() =>
        {
            if (MessageBoxes.ConfirmSuppressible(owner(), _s.AreYouSureRebase.Text, _s.RebaseConfirmTitle.Text, AppSettings.DontConfirmRebase, heading: interactive ? _s.RebaseBranchInteractive.Text : _s.RebaseBranch.Text))
            {
                if (interactive)
                {
                    commands.StartInteractiveRebase(owner(), onto);
                }
                else
                {
                    commands.StartRebase(owner(), onto);
                }
            }
        });

        private IReadOnlyList<MenuModelItem> CreateManipulateItems(GitRevision revision)
            =>
            [
                new(_s.EditCommit.AccessKeyText, () => LaunchRebase("e", revision)),
                new(_s.RewordCommit.AccessKeyText, () => LaunchRebase("r", revision)),
                MenuModelItem.Separator,
                Item(_s.FixupCommit, null, () => commands.StartFixupCommitDialog(owner(), revision)),
                Item(_s.SquashCommit, null, () => commands.StartSquashCommitDialog(owner(), revision)),
                new(_s.AmendCommit.AccessKeyText, () => Run(() => commands.StartAmendCommitDialog(owner(), revision)), IsEnabled: Module.GitVersion.SupportAmendCommits),
                MenuModelItem.Separator,
                new(_s.GetHelpOnHowToUseTheseFeatures.AccessKeyText, () => OsShellUtil.OpenUrlInDefaultBrowser(UserManual.UserManual.UrlFor("modify_history", "using-autosquash-rebase-feature"))),
            ];

        private IReadOnlyList<MenuModelItem> CreateCompareItems()
            =>
            [
                Item(_s.OpenCommitsWithDiffTool, "Diff", DiffSelectedCommitsWithDifftool),
                MenuModelItem.Separator,
                Item(_s.CompareToBranch, null, CompareToBranch),
                Item(_s.CompareWithCurrentBranch, null, CompareWithCurrentBranch),
                MenuModelItem.Separator,
                new(_s.SelectAsBase.AccessKeyText, () => _baseCommitToCompare = grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault()),
                new(_s.CompareToBase.AccessKeyText, () => Run(CompareToBase), IsEnabled: _baseCommitToCompare is not null),
                Item(_s.CompareToWorkingDirectory, null, CompareToWorkingDirectory),
                Item(_s.CompareSelectedCommits, null, CompareSelectedCommits),
            ];

        // As RevisionGridMenuCommands.CreateNavigateMenuCommands.

        /// <summary>The Navigate submenu, also the Navigate menu of the main window (<c>NavigateMenuCommands</c>).</summary>
        public IReadOnlyList<MenuModelItem> CreateNavigateItems()
            =>
            [
                new(_s.ToggleBetweenArtificialAndHeadCommits.AccessKeyText, ToggleBetweenArtificialAndHead, "WorkingDirChanges"),
                new(_s.GotoCurrentRevision.AccessKeyText, () => grid.SelectRevision(Module.GetCurrentCheckout()), "GotoCurrentRevision"),
                new(_s.GotoCommit.AccessKeyText, GotoCommit, "GotoCommit"),
                MenuModelItem.Separator,
                new(_s.GotoChildCommit.AccessKeyText, grid.GoToChild, "GoToChildCommit"),
                new(_s.GotoParentCommit.AccessKeyText, grid.GoToParent, "GoToParentCommit"),
                new(_s.GotoFirstParentCommit.AccessKeyText, grid.GoToFirstParent, "GoToFirstParentCommit"),
                new(_s.GotoLastParentCommit.AccessKeyText, grid.GoToLastParent, "GoToLastParentCommit"),
                MenuModelItem.Separator,
                new(_s.GotoMergeBaseCommit.AccessKeyText, () => AvaloniaUi.RunInHostContext(GoToMergeBase), "GoToMergeBaseCommit", ToolTip: _s.GotoMergeBaseCommitToolTip.Text),
                MenuModelItem.Separator,
                new(_s.NavigateBackward.AccessKeyText, grid.NavigateBackward, "NavigateBackward", IsEnabled: grid.CanNavigateBackward),
                new(_s.NavigateForward.AccessKeyText, grid.NavigateForward, "NavigateForward", IsEnabled: grid.CanNavigateForward),
                MenuModelItem.Separator,
                new(_s.QuickSearch.AccessKeyText, () => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(owner(), _s.QuickSearchQuickHelp.Text, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)), ToolTip: _s.QuickSearchToolTip.Text),
                new(_s.PrevQuickSearch.AccessKeyText, () => grid.QuickSearchNext(down: false)),
                new(_s.NextQuickSearch.AccessKeyText, () => grid.QuickSearchNext(down: true)),
            ];

        // As RevisionGridMenuCommands.CreateViewMenuCommands, for the settings the Avalonia grid shows.

        /// <summary>The View submenu, also the View menu of the main window (<c>ViewMenuCommands</c>).</summary>
        public IReadOnlyList<MenuModelItem> CreateViewItems()
        {
            FilterInfo filter = new();
            RevisionGridFilterState? state = Filter?.State;
            return
            [
                new(_s.ShowAllBranches.AccessKeyText, () => SetBranchFilter(byBranchFilter: false, currentOnly: false), IsChecked: state?.ShowAllBranches ?? filter.IsShowAllBranchesChecked),
                new(_s.ShowCurrentBranchOnly.AccessKeyText, () => SetBranchFilter(byBranchFilter: false, currentOnly: true), IsChecked: state?.ShowCurrentBranchOnly ?? filter.IsShowCurrentBranchOnlyChecked),
                new(_s.ShowFilteredBranches.AccessKeyText, () => SetBranchFilter(byBranchFilter: true, currentOnly: false), IsChecked: state?.ShowFilteredBranches ?? filter.IsShowFilteredBranchesChecked),
                MenuModelItem.Separator,
                new(_s.ShowReflogReferences.AccessKeyText, ToggleReflog, IsChecked: state?.ShowReflogReferences ?? AppSettings.ShowReflogReferences),
                new(_s.ShowArtificialCommits.AccessKeyText, () => Toggle(() => AppSettings.RevisionGraphShowArtificialCommits = !AppSettings.RevisionGraphShowArtificialCommits), IsChecked: AppSettings.RevisionGraphShowArtificialCommits),
                MenuModelItem.Separator,
                new(_s.ShowRemoteBranches.AccessKeyText, () => ToggleDisplay(() => AppSettings.ShowRemoteBranches = !AppSettings.ShowRemoteBranches), IsChecked: AppSettings.ShowRemoteBranches),
                new(_s.ShowTags.AccessKeyText, () => ToggleDisplay(() => AppSettings.ShowTags = !AppSettings.ShowTags), IsChecked: AppSettings.ShowTags),
                MenuModelItem.Separator,
                new(_s.ShowAuthorDate.AccessKeyText, () => ToggleDisplay(() => AppSettings.ShowAuthorDate = !AppSettings.ShowAuthorDate), IsChecked: AppSettings.ShowAuthorDate),
                new(_s.ShowRelativeDate.AccessKeyText, () => ToggleDisplay(() => AppSettings.RelativeDate = !AppSettings.RelativeDate), IsChecked: AppSettings.RelativeDate),
                MenuModelItem.Separator,
                new(_s.ShowRevisionGraphColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowRevisionGridGraphColumn = !AppSettings.ShowRevisionGridGraphColumn), IsChecked: AppSettings.ShowRevisionGridGraphColumn),
                new(_s.ShowAuthorNameColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowAuthorNameColumn = !AppSettings.ShowAuthorNameColumn), IsChecked: AppSettings.ShowAuthorNameColumn),
                new(_s.ShowDateColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowDateColumn = !AppSettings.ShowDateColumn), IsChecked: AppSettings.ShowDateColumn),
                new(_s.ShowIdColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowObjectIdColumn = !AppSettings.ShowObjectIdColumn), IsChecked: AppSettings.ShowObjectIdColumn),
            ];

            void Toggle(Action change)
            {
                change();
                grid.Load(grid.SelectedRow?.ObjectId);
            }

            void ToggleReflog()
            {
                if (Filter is not null)
                {
                    Filter.ToggleShowReflogReferences();
                    return;
                }

                Toggle(() => AppSettings.ShowReflogReferences.Value = !AppSettings.ShowReflogReferences);
            }

            void SetBranchFilter(bool byBranchFilter, bool currentOnly)
            {
                if (Filter is not null)
                {
                    // As the handlers of the menu: through the filter of the grid, which the toolbar shows.
                    (currentOnly ? (Action)Filter.ShowCurrentBranchOnly : byBranchFilter ? Filter.ShowFilteredBranches : Filter.ShowAllBranches)();
                    return;
                }

                // As ShowAllBranches, ShowCurrentBranchOnly and ShowFilteredBranches: the settings of the filter of the grid.
                FilterInfo current = new();
                current.ByBranchFilter = byBranchFilter;
                current.ShowCurrentBranchOnly = currentOnly;
                grid.Load(grid.SelectedRow?.ObjectId);
            }

            void ToggleDisplay(Action change)
            {
                change();
                grid.DisplayOptions = GetDisplayOptions();
            }

            void ToggleColumns(Action change)
            {
                change();
                ApplyColumns(grid);
            }
        }

        // As AddUserScripts: the scripts shown in the grid, then the others under "Run script".
        private IEnumerable<MenuModelItem> CreateScriptItems()
        {
            IScriptsManager scriptsManager = commands.GetRequiredService<IScriptsManager>();
            List<ScriptInfo> scripts = [.. scriptsManager.GetScripts().Where(script => script.Enabled)];
            List<MenuModelItem> direct = [];
            List<MenuModelItem> others = [];
            foreach (ScriptInfo script in scripts)
            {
                MenuModelItem item = new(script.Name ?? "", () => RunScript(script), ToPng(script.GetIcon()));
                (script.AddToRevisionGridContextMenu ? direct : others).Add(item);
            }

            if (others.Count > 0)
            {
                direct.Insert(0, new MenuModelItem(_s.RunScript.AccessKeyText, Icon: "Console", Children: others));
            }

            return direct;
        }

        private void RunScript(ScriptInfo script) => Run(() =>
        {
            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            scriptsRunner.RunScript(script, owner(), commands, new ScriptOptionsProvider(() => [], () => null, () => null));
        });

        private void ContinueBisect(GitBisectOption option, GitRevision revision)
            => FormProcess.ShowDialog(owner(), commands, arguments: Commands.ContinueBisect(option, revision.ObjectId), Module.WorkingDir, input: null, useDialogSettings: false);

        private void StopBisect()
            => FormProcess.ShowDialog(owner(), commands, arguments: Commands.StopBisect(), Module.WorkingDir, input: null, useDialogSettings: true);

        private void ResetChanges()
            => commands.StartResetChangesDialog(owner(), Module.GetWorkTreeFiles(), onlyWorkTree: grid.SelectedRow?.ObjectId == ObjectId.WorkTreeId);

        private void StashApply(GitRevision revision) => commands.StashApply(owner(), revision.ObjectId.ToString());

        private void StashPop(GitRevision revision)
        {
            if (!string.IsNullOrEmpty(revision.ReflogSelector))
            {
                commands.StashPop(owner(), revision.ReflogSelector);
            }
        }

        // As DropStashToolStripMenuItemClick.
        private void StashDrop(GitRevision revision)
        {
            if (string.IsNullOrEmpty(revision.ReflogSelector))
            {
                return;
            }

            TaskDialogButton result = TaskDialogButton.Yes;
            if (!AppSettings.DontConfirmStashDrop)
            {
                TaskDialogPage page = new()
                {
                    Text = TranslatedStrings.AreYouSure,
                    Caption = TranslatedStrings.StashDropConfirmTitle,
                    Heading = TranslatedStrings.CannotBeUndone,
                    Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
                    Icon = TaskDialogIcon.Information,
                    Verification = new TaskDialogVerificationCheckBox { Text = TranslatedStrings.DontShowAgain },
                    SizeToContent = true,
                };
                result = TaskDialog.ShowDialog(owner().Handle, page);
                if (page.Verification.Checked)
                {
                    AppSettings.DontConfirmStashDrop = true;
                }
            }

            if (result == TaskDialogButton.Yes)
            {
                commands.StashDrop(owner(), revision.ReflogSelector);
            }
        }

        private void ResetCurrentBranch(GitRevision revision)
            => commands.DoActionOnRepo(() =>
            {
                if (TryShowResetCurrentBranch(owner(), commands, revision, FormResetCurrentBranch.ResetType.Soft, out bool reset))
                {
                    return reset;
                }

                using FormResetCurrentBranch form = FormResetCurrentBranch.Create(commands, revision);
                return form.ShowDialog(owner()) == DialogResult.OK;
            });

        private void ResetAnotherBranch(GitRevision revision)
            => commands.DoActionOnRepo(() =>
            {
                if (TryShowResetAnotherBranch(owner(), commands, revision, out bool reset))
                {
                    return reset;
                }

                using FormResetAnotherBranch form = FormResetAnotherBranch.Create(commands, revision);
                return form.ShowDialog(owner()) == DialogResult.OK;
            });

        private void CreateBranch(GitRevision revision)
            => commands.DoActionOnRepo(() =>
            {
                if (TryShowCreateBranch(owner(), commands, revision.ObjectId, new(BranchName: null), out bool created))
                {
                    return created;
                }

                using FormCreateBranch form = new(commands, revision.ObjectId);
                return form.ShowDialog(owner()) == DialogResult.OK;
            });

        private void CreateTag(GitRevision revision)
            => commands.DoActionOnRepo(() =>
            {
                if (TryShowCreateTag(owner(), commands, revision.ObjectId, out bool created))
                {
                    return created;
                }

                using FormCreateTag form = new(commands, revision.ObjectId);
                return form.ShowDialog(owner()) == DialogResult.OK;
            });

        private void RevertCommits()
        {
            foreach (GitRevision revision in grid.GetSelectedRevisions(descending: false))
            {
                commands.StartRevertCommitDialog(owner(), revision);
            }
        }

        // As ArchiveRevisionToolStripMenuItemClick.
        private void Archive()
        {
            IReadOnlyList<GitRevision> selected = grid.GetSelectedRevisionsLatestSelectedFirst();
            if (selected.Count > 2)
            {
                MessageBoxes.Show(owner(), "Select only one or two revisions. Abort.", "Archive revision", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            commands.StartArchiveDialog(owner(), selected.Count > 0 ? selected[0] : null, selected.Count == 2 ? selected[1] : null);
        }

        // As LaunchRebase: an interactive rebase editing (e) or rewording (r) the first commit.
        private void LaunchRebase(string command, GitRevision revision) => Run(() =>
        {
            string rebaseCmd = Commands.Rebase(new Commands.RebaseOptions
            {
                BranchName = revision.FirstParentId is { IsZero: false } parent ? parent.ToString() : null,
                Interactive = true,
                AutoStash = true,
                SupportRebaseMerges = Module.GitVersion.SupportRebaseMerges,
            });

            using FormProcess formProcess = new(commands, arguments: rebaseCmd, Module.WorkingDir, input: null, useDialogSettings: true);
            const string sequenceEditor = "GIT_SEQUENCE_EDITOR";
            formProcess.ProcessEnvVariables.Add(sequenceEditor, string.Format("sed -i -re '0,/pick/s//{0}/'", command));
            formProcess.ProcessEnvVariables.ForwardEnvironmentVariableToWsl(Module.WorkingDir, sequenceEditor);
            formProcess.ShowDialog(owner());
        });

        // As GetFirstAndSelected.
        private (ObjectId FirstId, GitRevision? Selected) GetFirstAndSelected()
        {
            IReadOnlyList<GitRevision> revisions = grid.GetSelectedRevisionsLatestSelectedFirst();
            return revisions.Count switch
            {
                0 => (default, null),
                1 => (revisions[0].FirstParentId, revisions[0]),
                _ => (revisions[^1].ObjectId, revisions[0]),
            };
        }

        private void DiffSelectedCommitsWithDifftool()
        {
            (ObjectId first, GitRevision? selected) = GetFirstAndSelected();
            if (selected is not null)
            {
                Module.OpenWithDifftoolDirDiff(first.IsZero ? null : first.ToString(), selected.ObjectId.ToString(), customTool: null);
            }
        }

        private void CompareToBranch()
        {
            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is not { } head)
            {
                return;
            }

            if (!TryShowCompareToBranch(owner(), commands, head.ObjectId, out string? branchName))
            {
                using FormCompareToBranch form = new(commands, head.ObjectId);
                branchName = form.ShowDialog(owner()) == DialogResult.OK ? form.BranchName : null;
            }

            if (branchName is not null)
            {
                ObjectId baseCommit = Module.RevParse(branchName);
                if (baseCommit.IsZero)
                {
                    MessageBoxes.ShowError(owner(), _s.NoRevisionFoundError.Text);
                    return;
                }

                ShowFormDiff(baseCommit, head.ObjectId, branchName, head.Subject);
            }
        }

        private void CompareWithCurrentBranch()
        {
            string branch = Module.GetSelectedBranch(emptyIfDetached: true);
            ObjectId checkout = Module.GetCurrentCheckout();
            if (string.IsNullOrWhiteSpace(branch) || checkout.IsZero)
            {
                MessageBoxes.Show(owner(), "No branch is currently selected", TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is { } baseCommit)
            {
                ShowFormDiff(baseCommit.ObjectId, checkout, baseCommit.Subject, branch);
            }
        }

        private void CompareToBase()
        {
            if (_baseCommitToCompare is null)
            {
                MessageBoxes.Show(owner(), _s.BaseForCompareNotSelectedError.Text, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is { } head)
            {
                ShowFormDiff(_baseCommitToCompare.ObjectId, head.ObjectId, _baseCommitToCompare.Subject, head.Subject);
            }
        }

        private void CompareToWorkingDirectory()
        {
            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is not { } baseCommit)
            {
                return;
            }

            if (baseCommit.ObjectId == ObjectId.WorkTreeId)
            {
                MessageBoxes.Show(owner(), "Cannot diff working directory to itself", TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ShowFormDiff(baseCommit.ObjectId, ObjectId.WorkTreeId, baseCommit.Subject, "Working directory");
        }

        private void CompareSelectedCommits()
        {
            (ObjectId firstId, GitRevision? selected) = GetFirstAndSelected();
            if (selected is not null && !firstId.IsZero)
            {
                ShowFormDiff(firstId, selected.ObjectId, grid.GetRevision(firstId)?.Subject ?? "", selected.Subject);
            }
            else
            {
                MessageBoxes.Show(owner(), "You must have two commits selected to compare", TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowFormDiff(ObjectId baseCommit, ObjectId headCommit, string baseDisplay, string headDisplay)
        {
            if (!TryShowDiff(commands, baseCommit, headCommit, baseDisplay, headDisplay))
            {
                new FormDiff(commands, baseCommit, headCommit, baseDisplay, headDisplay) { ShowInTaskbar = true }.Show();
            }
        }

        // As ToggleBetweenArtificialAndHeadCommits: working directory -> index -> HEAD.
        private void ToggleBetweenArtificialAndHead()
        {
            ObjectId current = grid.SelectedRow?.ObjectId ?? default;
            ObjectId next = current == ObjectId.WorkTreeId ? ObjectId.IndexId : current == ObjectId.IndexId ? Module.GetCurrentCheckout() : ObjectId.WorkTreeId;
            if (!grid.SelectRevision(next))
            {
                grid.SelectRevision(Module.GetCurrentCheckout());
            }
        }

        private void GotoCommit() => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryShowGoToCommit(owner(), commands, out bool accepted, out ObjectId commitId))
            {
                if (accepted && !grid.SelectRevision(commitId))
                {
                    MessageBoxes.RevisionFilteredInGrid(owner(), commitId);
                }

                return;
            }

            using FormGoToCommit form = new(commands);
            if (form.ShowDialog(owner()) == DialogResult.OK && form.ValidateAndGetSelectedObjectId() is { IsZero: false } objectId && !grid.SelectRevision(objectId))
            {
                MessageBoxes.RevisionFilteredInGrid(owner(), objectId);
            }
        });

        // As GoToMergeBase.
        private void GoToMergeBase()
        {
            IReadOnlyList<GitRevision> selected = grid.GetSelectedRevisionsLatestSelectedFirst();
            List<ObjectId> revisions = [.. selected.Select(r => r.ObjectId).Where(id => !id.IsArtificial)];
            bool hasArtificial = selected.Any(r => r.IsArtificial);
            ObjectId headId = Module.RevParse("HEAD");
            if (headId.IsZero || (revisions.Count == 0 && !hasArtificial))
            {
                return;
            }

            GitArgumentBuilder args = new("merge-base")
            {
                { revisions.Count > 2 || (revisions.Count == 2 && hasArtificial), "--octopus" },
                { revisions.Count < 1, headId.ToString() },
                { revisions.Count < 2, headId.ToString() },
                revisions
            };

            ExecutionResult result = Module.GitExecutable.Execute(args, throwOnErrorExit: false);
            const int noCommonAncestorsExitCode = 1;
            if (result.ExitCode == noCommonAncestorsExitCode || string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                MessageBoxes.ShowError(owner(), _s.NoMergeBaseCommit.Text);
                return;
            }

            result.ThrowIfErrorExit();
            ObjectId commitId = ObjectId.Parse(result.StandardOutput.TrimEnd());
            if (!grid.SelectRevision(commitId))
            {
                MessageBoxes.RevisionFilteredInGrid(owner(), commitId);
            }
        }
    }

    /// <summary>The display options of the grid from the settings (as <c>RevisionGridControl</c> reads them).</summary>
    internal static RevisionGridDisplayOptions GetDisplayOptions()
        => new(AppSettings.RelativeDate, AppSettings.ShowAuthorDate, TranslatedStrings.SearchingFor, AppSettings.RevisionGridQuickSearchTimeout, AppSettings.ShowRemoteBranches, AppSettings.ShowTags);

    /// <summary>The columns of the grid from the settings.</summary>
    internal static void ApplyColumns(RevisionGridViewModel grid)
    {
        grid.ShowGraphColumn = AppSettings.ShowRevisionGridGraphColumn;
        grid.ShowAuthorColumn = AppSettings.ShowAuthorNameColumn;
        grid.ShowDateColumn = AppSettings.ShowDateColumn;
        grid.ShowIdColumn = AppSettings.ShowObjectIdColumn;
    }
}
