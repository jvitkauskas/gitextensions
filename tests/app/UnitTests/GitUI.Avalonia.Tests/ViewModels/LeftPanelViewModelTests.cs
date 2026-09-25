using GitCommands;
using GitCommands.Git;
using GitCommands.Submodules;
using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaTests.Views;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.LeftPanel;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the left panel (the port of <c>RepoObjectsTree</c>, phase 7).</summary>
[TestFixture]
public sealed class LeftPanelViewModelTests
{
    [Test]
    public void Loads_the_trees_when_the_grid_loads()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost _, FakeLeftPanelSettings _) = Create();

        panel.Trees.Select(t => t.Text).Should().Equal("Branches", "Remotes", "Worktrees", "Tags", "Submodules", "Stashes");

        // Branches: the prioritized one first, the folders of the names, the current one bold with its ahead/behind counts.
        panel.BranchesTree.IsExpanded.Should().BeTrue();
        panel.BranchesTree.Children.Select(n => n.Text).Should().Equal("main (1↑ 2↓)", "experiment", "feature");
        LocalBranchNode main = (LocalBranchNode)panel.BranchesTree.Children[0];
        main.IsBold.Should().BeTrue();
        main.IconKey.Should().Be(LeftPanelIcons.BranchLocal);
        main.RelatedBranch.Should().Be("refs/remotes/origin/main");
        BranchPathNode feature = (BranchPathNode)panel.BranchesTree.Children[2];
        feature.IconKey.Should().Be(LeftPanelIcons.BranchFolder);
        feature.Children.Select(n => n.Text).Should().Equal("login");

        // A branch whose revision is not in the grid is grayed out.
        LocalBranchNode login = (LocalBranchNode)feature.Children[0];
        login.Visible.Should().BeFalse();
        login.IsGrayed.Should().BeTrue();
        login.IconKey.Should().Be(LeftPanelIcons.EyeClosed);
        login.ToolTip.Should().Be("'feature/login' is not currently visible");
        main.Visible.Should().BeTrue();
        main.IsGrayed.Should().BeFalse();

        // Remotes: the remotes with their branches, those without branches, the inactive ones in a folder.
        panel.RemotesTree.Children.Select(n => n.Text).Should().Equal("origin", "upstream", "[ Inactive ]");
        RemoteRepoNode origin = (RemoteRepoNode)panel.RemotesTree.Children[0];
        origin.IconKey.Should().Be(LeftPanelIcons.GitHub);
        origin.ToolTip.Should().Be("https://github.com/owner/repo.git");
        origin.Children.Select(n => n.Text).Should().Equal("main (1↓ 2↑)");
        ((RemoteRepoNode)panel.RemotesTree.Children[1]).IconKey.Should().Be(LeftPanelIcons.Remote);
        panel.RemotesTree.Children[2].IconKey.Should().Be(LeftPanelIcons.EyeClosed);
        panel.RemotesTree.Children[2].Children.Should().ContainSingle().Which.Should().BeOfType<RemoteRepoNode>().Which.Enabled.Should().BeFalse();

        // Tags and stashes are collapsed; a stash is hidden unless the grid lists it.
        panel.TagsTree.IsExpanded.Should().BeFalse();
        panel.TagsTree.Children.Select(n => n.Text).Should().Equal("v1.0", "old");
        panel.TagsTree.Children[0].IconKey.Should().Be(LeftPanelIcons.TagHorizontal);
        panel.StashesTree.IsExpanded.Should().BeFalse();
        StashNode stash = (StashNode)panel.StashesTree.Children.Single();
        stash.Text.Should().Be("@{0}: WIP on main");
        stash.FullPath.Should().Be("stash@{0}");
        stash.Visible.Should().BeFalse();

        // Worktrees: relative to the parent of the main worktree, the common folder of the linked ones removed.
        panel.WorktreesTree.IsExpanded.Should().BeTrue();
        panel.WorktreesTree.Children.Select(n => n.Text).Should().Equal("repo (main)", "feature-a (feature-a)", "feature-b (detached at 1234567)");
        panel.WorktreesTree.Children[0].IsBold.Should().BeTrue();
        panel.WorktreesTree.Children[2].IsGrayed.Should().BeTrue("the worktree is deleted");

        // Without a selection, the current branch is selected (without selecting it in the grid).
        panel.SelectedNode.Should().BeSameAs(main);
        panel.SearchCandidates.Should().Contain(["main", "feature/login", "origin/main", "v1.0", "old/v0.9"]).And.NotContain("feature");
    }

    [Test]
    public void Selecting_a_node_selects_its_revision_in_the_grid_and_a_double_click_runs_its_operation()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        GitRevision side = panel.Grid.Rows.Single(r => r.Subject == "Experiment").Revision;

        LocalBranchNode experiment = (LocalBranchNode)panel.BranchesTree.Children[1];
        panel.SelectedNode = experiment;
        panel.Grid.SelectedRow!.Revision.Should().BeSameAs(side);

        panel.SelectedNode = panel.TagsTree.Children[0];
        panel.Grid.SelectedRow!.Subject.Should().Be("Release");

        // A branch hidden from the grid is not selected in it.
        panel.SelectedNode = panel.BranchesTree.Children[2].Children[0];
        panel.Grid.SelectedRow!.Subject.Should().Be("Release");
        host.RevisionsNotInGrid.Should().BeEmpty();

        // A worktree at a revision that the grid does not list: as GoToRef, the user is told.
        panel.SelectedNode = panel.WorktreesTree.Children[2];
        host.RevisionsNotInGrid.Should().ContainSingle();

        panel.DoubleClickNode(experiment);
        panel.DoubleClickNode(panel.TagsTree.Children[0]);
        panel.DoubleClickNode(panel.RemotesTree.Children[0]);
        panel.DoubleClickNode(panel.StashesTree.Children[0]);
        panel.DoubleClickNode(panel.WorktreesTree.Children[0]);
        panel.DoubleClickNode(panel.WorktreesTree.Children[1]);
        host.Runs.Select(r => (r.Action, r.Node?.Text)).Should().Equal(
            (LeftPanelAction.CheckoutBranch, "experiment"),
            (LeftPanelAction.CreateBranchFromTag, "v1.0"),
            (LeftPanelAction.ManageRemotes, "origin"),
            (LeftPanelAction.OpenStash, "@{0}: WIP on main"),
            (LeftPanelAction.OpenWorktree, "feature-a (feature-a)"));
    }

    [Test]
    public void The_arrow_keys_move_the_selection_without_selecting_the_revision_until_space_or_enter()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost _, FakeLeftPanelSettings _) = Create();
        panel.SelectByClick(panel.TagsTree.Children[0]);
        panel.Grid.SelectedRow!.Subject.Should().Be("Release");

        panel.NavigateByKeyboard();
        panel.SelectedNode = panel.BranchesTree.Children[1];
        panel.Grid.SelectedRow!.Subject.Should().Be("Release");

        panel.ActivateSelectedNode();
        panel.Grid.SelectedRow!.Subject.Should().Be("Experiment");

        // A click on the selected node selects its revision again.
        panel.Grid.SelectedRow = panel.Grid.Rows[0];
        panel.SelectByClick(panel.BranchesTree.Children[1]);
        panel.Grid.SelectedRow!.Subject.Should().Be("Experiment");
    }

    [Test]
    public void The_menu_of_a_visible_local_branch_has_the_user_scripts()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        host.Scripts = [new(7, "Deploy", IsDirect: false), new(8, "Lint", IsDirect: true)];

        // As AddUserScripts: "Run script" with the scripts not in the grid's menu, then the others in the menu itself.
        IReadOnlyList<LeftPanelMenuItem> items = OpenMenu(panel, panel.BranchesTree.Children[0]);
        items.Select(i => i.Header).Should().EndWith(["-", "Run script", "Lint"]);
        LeftPanelMenuItem runScript = items.Single(i => i.Header == "Run script");
        runScript.Icon.Should().Be("Console");
        runScript.Children!.Select(i => i.Header).Should().Equal("Deploy");
        runScript.Children![0].Execute!();
        items.Single(i => i.Header == "Lint").Execute!();
        host.ScriptsRun.Should().Equal(7, 8);

        // Not for a hidden branch nor a tag.
        OpenMenu(panel, panel.BranchesTree.Children[2].Children[0]).Should().NotContain(i => i.Header == "Lint");
        OpenMenu(panel, panel.TagsTree.Children[0]).Should().NotContain(i => i.Header == "Lint");
    }

    [Test]
    public async Task A_click_with_alt_on_a_branch_selects_its_related_branch()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        GitRevision release = panel.Grid.Rows.Single(r => r.Subject == "Release").Revision;
        host.Refs[3] = new GitRef(TestGitModule.Instance, release.ObjectId, "refs/remotes/origin/main", "origin");
        await panel.RefreshAsync();
        LeftPanelNode main = panel.BranchesTree.Children[0];

        panel.SelectByClick(main);
        panel.Grid.SelectedRow!.Revision.Should().NotBeSameAs(release);

        // As BaseBranchLeafNode.SelectRevision: with Alt, the tracked branch; and the local branch of a remote one.
        panel.SelectByClick(main, alternate: true);
        panel.Grid.SelectedRow!.Revision.Should().BeSameAs(release);
        panel.SelectByClick(panel.RemotesTree.Children[0].Children[0], alternate: true);
        panel.Grid.SelectedRow!.Revision.Should().NotBeSameAs(release);
        panel.SelectByClick(panel.RemotesTree.Children[0].Children[0]);
        panel.Grid.SelectedRow!.Revision.Should().BeSameAs(release);
    }

    [Test]
    public void A_refresh_keeps_the_expanded_nodes_the_selection_and_the_multi_selection()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        panel.TagsTree.IsExpanded = true;
        panel.TagsTree.Children[1].IsExpanded = true;
        panel.BranchesTree.IsExpanded = false;
        LeftPanelNode v09 = panel.TagsTree.Children[1].Children[0];
        panel.SelectedNode = panel.TagsTree.Children[0];
        panel.ClickNode(panel.TagsTree.Children[0], multiple: false, includingDescendants: false);
        panel.ClickNode(panel.BranchesTree.Children[1], multiple: true, includingDescendants: false);

        host.Refs.Add(new GitRef(TestGitModule.Instance, host.Refs[0].ObjectId, "refs/tags/v2.0"));
        panel.Grid.Load(panel.Grid.SelectedRow!.ObjectId);

        panel.TagsTree.Children.Select(n => n.Text).Should().Equal("v1.0", "old", "v2.0");
        panel.TagsTree.IsExpanded.Should().BeTrue();
        panel.TagsTree.Children[1].IsExpanded.Should().BeTrue();
        panel.TagsTree.Children[1].Children[0].Should().NotBeSameAs(v09, "the nodes are loaded again");
        panel.BranchesTree.IsExpanded.Should().BeFalse();
        panel.SelectedNode.Should().BeSameAs(panel.TagsTree.Children[0]);
        panel.GetMultiSelectedNodes().Select(n => n.Text).Should().Equal("experiment", "v1.0");
        panel.TagsTree.Children[0].IsMultiSelected.Should().BeTrue();
    }

    [Test]
    public void Ctrl_click_multi_selects_and_the_filter_for_selected_filters_the_grid()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        panel.ClickNode(panel.BranchesTree.Children[0], multiple: false, includingDescendants: false);
        panel.ClickNode(panel.TagsTree, multiple: true, includingDescendants: true);
        panel.GetMultiSelectedNodes().Select(n => n.Text).Should().Equal("main (1↑ 2↓)", "Tags", "v1.0", "old", "v0.9");

        // A right click on a selected node keeps the multi-selection; a plain click selects only the node.
        panel.ClickNode(panel.TagsTree.Children[0], multiple: false, includingDescendants: false, rightButton: true);
        panel.GetMultiSelectedNodes().Should().HaveCount(5);

        panel.SelectedNode = panel.TagsTree.Children[0];
        LeftPanelMenuItem filter = panel.GetContextMenu().Single(i => i.Header == "_Filter for selected");
        filter.ToolTip.Should().StartWith("Filter the revision grid");
        filter.Execute!();
        host.Filters.Should().Equal("main v1.0 old/v0.9");

        host.IsBranchFilterActive = true;
        LeftPanelMenuItem showAll = panel.GetContextMenu().Single(i => i.Header == "Show all branches");
        showAll.Execute!();
        host.Filters.Should().Equal("main v1.0 old/v0.9", null);

        panel.ClickNode(panel.RemotesTree, multiple: false, includingDescendants: false);
        panel.GetMultiSelectedNodes().Should().Equal(panel.RemotesTree);
    }

    [Test]
    public void The_context_menu_of_a_local_branch()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings settings) = Create();
        LocalBranchNode main = (LocalBranchNode)panel.BranchesTree.Children[0];
        IReadOnlyList<LeftPanelMenuItem> menu = OpenMenu(panel, main);

        // The current branch can only be branched from and renamed.
        Headers(menu).Should().Equal(
            "_Copy to clipboard", "_Filter for selected", "-",
            "Chec_kout branch...", "_Merge into current branch...", "_Rebase current branch on this branch...", "Create _branch...",
            "Re_set current branch to here...", "-", "R_ename branch...", "_Delete branch...", "-", "_Sort by");
        menu.Where(i => !i.IsSeparator && !i.IsEnabled).Select(i => i.Header).Should().Equal(
            "Chec_kout branch...", "_Merge into current branch...", "_Rebase current branch on this branch...", "Re_set current branch to here...", "_Delete branch...");
        menu.Single(i => i.Header == "_Delete branch...").ToolTip.Should().Be("Delete the branch, which must be fully merged in its upstream branch or in HEAD");

        // The copy menu: the references, the hash, the message, the author and the date of the selected revision.
        LeftPanelMenuItem copy = menu[0];
        Headers(copy.Children!).Should().StartWith(["Branches", "_1:   main", "_2:   origin/main", "-"]);
        copy.Children!.Should().Contain(i => i.Header.StartsWith("_Commit hash", StringComparison.Ordinal));
        copy.Children!.Single(i => i.Header == "_2:   origin/main").Execute!();
        host.Copied.Should().Equal("origin/main");

        // Sorting (no order for the git default).
        LeftPanelMenuItem sortBy = menu[^1];
        sortBy.Children!.Single(i => i.IsChecked == true).Header.Should().Be("Git default");
        sortBy.Children!.Single(i => i.Header == "Alpha-numeric").Execute!();
        settings.RefsSortBy.Should().Be(GitRefsSortBy.refname);
        Headers(OpenMenu(panel, panel.BranchesTree.Children[0])).Should().EndWith(["_Sort by", "_Sort order"]);

        // Another branch: all items are enabled.
        IReadOnlyList<LeftPanelMenuItem> other = OpenMenu(panel, panel.BranchesTree.Children[1]);
        other.Where(i => !i.IsSeparator).Should().OnlyContain(i => i.IsEnabled);
        other.Single(i => i.Header == "Chec_kout branch...").Execute!();
        other.Single(i => i.Header == "_Delete branch...").Execute!();
        host.Runs.Select(r => (r.Action, r.Node?.Text)).Should().Equal(
            (LeftPanelAction.CheckoutBranch, "experiment"), (LeftPanelAction.DeleteBranch, "experiment"));
    }

    [Test]
    public void The_context_menus_of_remotes_tags_folders_stashes_and_worktrees()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        RemoteRepoNode origin = (RemoteRepoNode)panel.RemotesTree.Children[0];

        Headers(OpenMenu(panel, origin.Children[0])).Should().Equal(
            "_Copy to clipboard", "_Filter for selected", "-",
            "Chec_kout remote branch...", "_Merge into current branch...", "_Rebase current branch on this remote branch...", "Create _branch...",
            "Re_set current branch to here...", "-", "_Delete remote branch...", "-",
            "_Fetch & Checkout", "Fetch & Merge (_Pull)", "Fetch & Re_base", "Fetc_h & Create Branch", "Fe_tch", "-", "_Sort by");
        Headers(OpenMenu(panel, panel.TagsTree.Children[0])).Should().Equal(
            "_Filter for selected", "-",
            "Chec_kout tag revision...", "_Merge into current branch...", "_Rebase current branch on this tag revision...", "Create _branch...",
            "Re_set current branch to here...", "-", "_Delete tag...", "-", "_Sort by");
        Headers(OpenMenu(panel, panel.RemotesTree)).Should().Equal(
            "_Manage...", "Fetch all remotes", "Fetch and prune all remotes", "-", "Collapse", "Expand", "-", "Move Up", "Move Down");
        Headers(OpenMenu(panel, origin)).Should().Equal(
            "_Manage...", "_Deactivate", "_Fetch", "Fetch and _prune", "Open remote Url", "-", "Collapse", "Expand");
        Headers(OpenMenu(panel, panel.RemotesTree.Children[2].Children[0])).Should().Equal("_Manage...", "_Activate", "A_ctivate and fetch", "Open remote Url");
        Headers(OpenMenu(panel, panel.BranchesTree.Children[2])).Should().Equal("Create Branch...", "Delete All", "-", "Collapse", "Expand");
        Headers(OpenMenu(panel, panel.StashesTree)).Should().Equal("_Stash", "S_tash staged", "_Manage stashes...", "-", "Collapse", "Expand", "-", "Move Up", "Move Down");
        Headers(OpenMenu(panel, panel.StashesTree.Children[0])).Should().Equal("_Open stash", "_Apply stash", "_Pop stash", "_Drop stash...");
        Headers(OpenMenu(panel, panel.WorktreesTree)).Should().Equal("_Create worktree...", "_Prune worktrees", "_Manage worktrees...", "-", "Collapse", "Expand", "-", "Move Up", "Move Down");

        // The current worktree cannot be opened nor deleted, the main one not deleted.
        IReadOnlyList<LeftPanelMenuItem> current = OpenMenu(panel, panel.WorktreesTree.Children[0]);
        Headers(current).Should().Equal("_Open worktree", "_Delete worktree...", "-", "Copy _path", "Show _in folder");
        current.Select(i => i.IsEnabled).Should().Equal(false, false, true, true, true);
        OpenMenu(panel, panel.WorktreesTree.Children[1]).Select(i => i.IsEnabled).Should().Equal(true, true, true, true, true);

        // The first tree cannot move up; moving the remotes down swaps them with the worktrees.
        OpenMenu(panel, panel.BranchesTree).Single(i => i.Header == "Move Up").IsEnabled.Should().BeFalse();
        OpenMenu(panel, origin).Single(i => i.Header == "_Fetch").Execute!();
        OpenMenu(panel, panel.StashesTree).Single(i => i.Header == "S_tash staged").Execute!();
        host.Runs.Select(r => r.Action).Should().Equal(LeftPanelAction.FetchRemote, LeftPanelAction.StashStaged);

        // Without a selection, the menu does not open.
        panel.ClickNode(panel.StashesTree, multiple: true, includingDescendants: false);
        panel.SelectedNode = null;
        panel.GetContextMenu().Should().BeEmpty();
    }

    [Test]
    public void The_trees_can_be_hidden_and_reordered()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost _, FakeLeftPanelSettings settings) = Create();

        panel.ShowRemotes = false;
        panel.Trees.Select(t => t.Kind).Should().Equal(LeftPanelTreeKind.Branches, LeftPanelTreeKind.Worktrees, LeftPanelTreeKind.Tags, LeftPanelTreeKind.Submodules, LeftPanelTreeKind.Stashes);
        settings.Shown[LeftPanelTreeKind.Remotes].Should().BeFalse();

        // Moving down swaps with the next shown tree.
        panel.MoveTree(panel.BranchesTree, up: false);
        panel.Trees.Select(t => t.Kind).Should().Equal(LeftPanelTreeKind.Worktrees, LeftPanelTreeKind.Branches, LeftPanelTreeKind.Tags, LeftPanelTreeKind.Submodules, LeftPanelTreeKind.Stashes);
        settings.Indexes[LeftPanelTreeKind.Worktrees].Should().Be(0);
        settings.Indexes[LeftPanelTreeKind.Branches].Should().Be(2);

        panel.ShowRemotes = true;
        panel.Trees.Select(t => t.Kind).Should().Equal(LeftPanelTreeKind.Worktrees, LeftPanelTreeKind.Remotes, LeftPanelTreeKind.Branches, LeftPanelTreeKind.Tags, LeftPanelTreeKind.Submodules, LeftPanelTreeKind.Stashes);
        panel.RemotesTree.Children.Should().NotBeEmpty("a shown tree is loaded");
    }

    [Test]
    public void Invalid_tree_positions_are_fixed_keeping_their_order()
    {
        FakeLeftPanelSettings settings = new();
        settings.Indexes[LeftPanelTreeKind.Tags] = 7;
        settings.Indexes[LeftPanelTreeKind.Branches] = 4;
        (LeftPanelViewModel panel, FakeLeftPanelHost _, FakeLeftPanelSettings _) = Create(settings);

        panel.Trees.Select(t => t.Kind).Should().Equal(LeftPanelTreeKind.Remotes, LeftPanelTreeKind.Worktrees, LeftPanelTreeKind.Branches, LeftPanelTreeKind.Submodules, LeftPanelTreeKind.Stashes, LeftPanelTreeKind.Tags);
        settings.Indexes.Values.Order().Should().Equal(0, 1, 2, 3, 4, 5);
    }

    [Test]
    public void The_search_selects_the_matches_in_turn_and_a_new_text_starts_again()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost _, FakeLeftPanelSettings _) = Create();

        panel.SearchText = "MAIN";
        panel.Search();
        List<LeftPanelNode> matches = [.. panel.AllTrees.SelectMany(t => t.SelfAndDescendants()).Where(n => n.IsSearchMatch)];
        matches.Select(n => n.Text).Should().BeEquivalentTo(["main (1↑ 2↓)", "main (1↓ 2↑)", "repo (main)"]);
        panel.SelectedNode!.Text.Should().Be("main (1↑ 2↓)");
        panel.RemotesTree.Children[0].IsExpanded.Should().BeFalse();
        panel.Search();
        panel.SelectedNode!.Text.Should().Be("repo (main)");
        panel.Search();
        panel.SelectedNode!.Text.Should().Be("main (1↓ 2↑)");
        panel.RemotesTree.Children[0].IsExpanded.Should().BeTrue("the path to the match is expanded");
        panel.Search();
        panel.SelectedNode!.Text.Should().Be("main (1↑ 2↓)", "the search starts again");

        panel.SearchText = "v0.9";
        panel.ExecuteHotkey(LeftPanelHotkeyCommand.Search).Should().BeTrue();
        matches.Should().OnlyContain(n => !n.IsSearchMatch);
        panel.SelectedNode!.Text.Should().Be("v0.9");
        panel.TagsTree.IsExpanded.Should().BeTrue();
    }

    [Test]
    public void The_hotkeys_delete_rename_and_multi_select_the_selected_node()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        panel.Hotkeys.Should().Contain(h => h.CommandCode == (int)LeftPanelHotkeyCommand.Rename);
        panel.SelectedNode = panel.BranchesTree.Children[1];

        panel.ExecuteHotkey(LeftPanelHotkeyCommand.Rename);
        panel.ExecuteHotkey(LeftPanelHotkeyCommand.Delete);
        panel.SelectedNode = panel.RemotesTree.Children[0].Children[0];
        panel.ExecuteHotkey(LeftPanelHotkeyCommand.Delete);
        panel.SelectedNode = panel.TagsTree.Children[0];
        panel.ExecuteHotkey(LeftPanelHotkeyCommand.Delete);
        host.Runs.Select(r => r.Action).Should().Equal(LeftPanelAction.RenameBranch, LeftPanelAction.DeleteBranch, LeftPanelAction.DeleteRemoteBranch, LeftPanelAction.DeleteTag);

        panel.SelectedNode = panel.TagsTree.Children[1];
        panel.ExecuteHotkey(LeftPanelHotkeyCommand.MultiSelectWithChildren);
        panel.GetMultiSelectedNodes().Select(n => n.Text).Should().Equal("old", "v0.9");
    }

    [Test]
    public void The_branches_merged_into_the_selected_revision_get_the_merged_icon()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        host.MergedBranches = ["refs/heads/experiment", "refs/remotes/origin/main", "refs/heads/main"];
        RevisionGridRow head = panel.Grid.Rows.Single(r => r.Subject == "Update the documentation");

        // Not when the revision is selected through its branch (as SelectionChanged).
        panel.Grid.SetSelectedRows([head]);
        host.MergedBranchesOf.Should().BeEmpty("the selected node is main");

        panel.SelectedNode = panel.TagsTree.Children[0];
        panel.Grid.SetSelectedRows([head]);

        LocalBranchNode experiment = (LocalBranchNode)panel.BranchesTree.Children[1];
        experiment.IsMerged.Should().BeTrue();
        experiment.IconKey.Should().Be(LeftPanelIcons.BranchLocalMerged);
        experiment.ToolTip.Should().Be("'experiment' is contained in the currently selected commit");
        ((LocalBranchNode)panel.BranchesTree.Children[0]).IsMerged.Should().BeFalse("main points to the selected revision");
        ((RemoteBranchNode)panel.RemotesTree.Children[0].Children[0]).IsMerged.Should().BeFalse("origin/main points to the selected revision");
        host.MergedBranchesOf.Should().Equal(head.Revision.Guid);
    }

    [Test]
    public void The_submodules_are_shown_as_a_tree_of_folders()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        host.SubmodulesRequested.Should().Be(1);
        DetailedSubmoduleInfo dirty = new() { IsDirty = true };
        host.RaiseSubmodules(new LeftPanelSubmodules(
            new SubmoduleInfo("repo [main]", @"C:\src\repo\", bold: true),
            [
                new SubmoduleInfo("ext/lib/a [no branch]", @"C:\src\repo\ext\lib\a\", bold: false) { Detailed = dirty },
                new SubmoduleInfo("ext/lib/b", @"C:\src\repo\ext\lib\b\", bold: false),
                new SubmoduleInfo("docs [main]", @"C:\src\repo\docs\", bold: false),
            ],
            [@"C:\src\repo\"],
            @"C:\src\repo\",
            CurrentSubmoduleStatus: null,
            StructureUpdated: true));

        SubmoduleNode top = (SubmoduleNode)panel.SubmodulesTree.Children.Single();
        top.Text.Should().Be("repo [main]");
        top.IsBold.Should().BeTrue();
        top.IsExpanded.Should().BeTrue();
        top.Children.Select(n => n.Text).Should().Equal("ext/lib", "docs [main]");
        SubmoduleFolderNode folder = (SubmoduleFolderNode)top.Children[0];
        folder.IsItalic.Should().BeTrue();
        folder.Children.Select(n => n.Text).Should().Equal("a [no branch]", "b");
        SubmoduleNode a = (SubmoduleNode)folder.Children[0];
        a.IconKey.Should().Be(LeftPanelIcons.SubmoduleDirty);
        a.LocalPath.Should().Be("ext/lib/a");
        a.SuperPath.Should().Be(@"C:\src\repo\");
        a.ToolTip.Should().Be("tooltip of ext/lib/a [no branch]");

        // The current module opens in a new instance, another one in the main window.
        Headers(OpenMenu(panel, top)).Should().Equal("O_pen", "_Manage...", "_Update", "Synchronize", "_Reset", "_Stash", "_Commit", "-", "Collapse", "Expand");
        Headers(OpenMenu(panel, a)).Should().Equal("_Open", "O_pen", "_Update", "_Reset", "_Stash", "_Commit");
        panel.DoubleClickNode(top);
        panel.DoubleClickNode(a);
        host.Runs.Select(r => r.Action).Should().Equal(LeftPanelAction.OpenSubmoduleInNewInstance, LeftPanelAction.OpenSubmodule);

        // A status update keeps the nodes.
        host.RaiseSubmodules(new LeftPanelSubmodules(
            new SubmoduleInfo("repo [main]", @"C:\src\repo\", bold: true),
            [
                new SubmoduleInfo("ext/lib/a [no branch]", @"C:\src\repo\ext\lib\a\", bold: false),
                new SubmoduleInfo("ext/lib/b", @"C:\src\repo\ext\lib\b\", bold: false),
                new SubmoduleInfo("docs [main]", @"C:\src\repo\docs\", bold: false),
            ],
            [@"C:\src\repo\"],
            @"C:\src\repo\",
            CurrentSubmoduleStatus: null,
            StructureUpdated: false));
        panel.SubmodulesTree.Children.Single().Should().BeSameAs(top);
        a.IconKey.Should().Be(LeftPanelIcons.FolderSubmodule);
    }

    [Test]
    public void The_worktree_common_prefix_stops_at_a_word_or_a_folder()
    {
        WorktreeTree.GetCommonPrefix(["repo_dev", "repo_test"]).Should().Be("repo_");
        WorktreeTree.GetCommonPrefix(["apricot", "apple"]).Should().Be("");
        WorktreeTree.GetCommonPrefix([@"repo.worktrees\a", @"repo.worktrees\b"]).Should().Be(@"repo.worktrees\");
        WorktreeTree.GetCommonPrefix(["only"]).Should().Be("");
    }

    [Test]
    public void Select_in_left_panel_shows_the_panel_and_selects_the_node_of_the_reference()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost _, FakeLeftPanelSettings _) = Create();
        panel.IsVisible = false;
        bool focusRequested = false;
        panel.FocusRequested += (_, _) => focusRequested = true;

        panel.SelectInLeftPanel("v1.0");

        panel.IsVisible.Should().BeTrue();
        panel.SelectedNode!.Text.Should().Be("v1.0");
        panel.TagsTree.IsExpanded.Should().BeTrue();
        panel.Grid.SelectedRow!.Subject.Should().Be("Release");
        focusRequested.Should().BeTrue();
        panel.SelectGitRef("unknown").Should().BeFalse();
    }

    [Test]
    public void A_hidden_panel_is_not_refreshed_until_it_is_shown()
    {
        (LeftPanelViewModel panel, FakeLeftPanelHost host, FakeLeftPanelSettings _) = Create();
        int refreshes = host.RefsRequested;

        panel.IsVisible = false;
        panel.Grid.Load();
        host.RefsRequested.Should().Be(refreshes);

        panel.IsVisible = true;
        host.RefsRequested.Should().Be(refreshes + 1);
    }

    internal static (LeftPanelViewModel Panel, FakeLeftPanelHost Host, FakeLeftPanelSettings Settings) Create(FakeLeftPanelSettings? settings = null)
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        FakeLeftPanelHost host = new(history);
        settings ??= new FakeLeftPanelSettings();
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost(history), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false));
        LeftPanelViewModel panel = new(new LeftPanelStrings(), host, settings, grid);
        grid.Load();
        return (panel, host, settings);
    }

    internal static IReadOnlyList<LeftPanelMenuItem> OpenMenu(LeftPanelViewModel panel, LeftPanelNode node)
    {
        panel.ClickNode(node, multiple: false, includingDescendants: false);
        panel.SelectedNode = null;
        panel.SelectedNode = node;
        return panel.GetContextMenu();
    }

    private static IEnumerable<string> Headers(IEnumerable<LeftPanelMenuItem> items) => items.Select(i => i.Header);

    internal sealed class FakeLeftPanelSettings : ILeftPanelSettings
    {
        public Dictionary<LeftPanelTreeKind, bool> Shown { get; } = Enum.GetValues<LeftPanelTreeKind>().ToDictionary(k => k, _ => true);

        public Dictionary<LeftPanelTreeKind, int> Indexes { get; } = Enum.GetValues<LeftPanelTreeKind>().ToDictionary(k => k, k => (int)k);

        public GitRefsSortBy RefsSortBy { get; set; }

        public GitRefsSortOrder RefsSortOrder { get; set; } = GitRefsSortOrder.Descending;

        public string PrioritizedBranchNames => "main[^/]*|master[^/]*|release/.*";

        public string PrioritizedRemoteNames => "origin|upstream";

        public bool IsShown(LeftPanelTreeKind kind) => Shown[kind];

        public void SetShown(LeftPanelTreeKind kind, bool shown) => Shown[kind] = shown;

        public int GetIndex(LeftPanelTreeKind kind) => Indexes[kind];

        public void SetIndex(LeftPanelTreeKind kind, int index) => Indexes[kind] = index;
    }

    /// <summary>
    ///  The references of <see cref="RevisionGridViewTests.CreateHistory"/> and more (a branch and a tag that the grid does not
    ///  list), remotes, a stash and worktrees; everything runs synchronously.
    /// </summary>
    internal sealed class FakeLeftPanelHost : ILeftPanelHost
    {
        public FakeLeftPanelHost(IReadOnlyList<GitRevision> history)
        {
            GitRevision head = history[0];
            GitRevision side = history.Single(r => r.Subject == "Experiment");
            GitRevision release = history.Single(r => r.Subject == "Release");
            ObjectId unknown = ObjectId.Random();
            Refs =
            [
                new GitRef(TestGitModule.Instance, side.ObjectId, "refs/heads/experiment"),
                new GitRef(TestGitModule.Instance, unknown, "refs/heads/feature/login"),
                new GitRef(TestGitModule.Instance, head.ObjectId, "refs/heads/main"),
                new GitRef(TestGitModule.Instance, head.ObjectId, "refs/remotes/origin/main", "origin"),
                new GitRef(TestGitModule.Instance, release.ObjectId, "refs/tags/v1.0"),
                new GitRef(TestGitModule.Instance, unknown, "refs/tags/old/v0.9"),
            ];
            Stashes = [new GitRevision(ObjectId.Random()) { Subject = "WIP on main", ReflogSelector = "refs/stash@{0}" }];
        }

        public event EventHandler<LeftPanelSubmodules>? SubmodulesUpdated;

        public List<IGitRef> Refs { get; }

        public List<GitRevision> Stashes { get; }

        public List<(LeftPanelAction Action, LeftPanelNode? Node)> Runs { get; } = [];

        public List<string?> Filters { get; } = [];

        public List<string> Copied { get; } = [];

        public List<ObjectId> RevisionsNotInGrid { get; } = [];

        public List<string> MergedBranchesOf { get; } = [];

        public IReadOnlyCollection<string> MergedBranches { get; set; } = [];

        public int SubmodulesRequested { get; private set; }

        public int RefsRequested { get; private set; }

        public bool IsValidWorkingDir => true;

        public bool IsBareRepository => false;

        public string WorkingDir => @"C:\src\repo\";

        public IReadOnlyList<HotkeyBinding> Hotkeys { get; } =
        [
            new((int)LeftPanelHotkeyCommand.Delete, 0x2E),
            new((int)LeftPanelHotkeyCommand.MultiSelect, 0x20 | HotkeyBinding.Control),
            new((int)LeftPanelHotkeyCommand.MultiSelectWithChildren, 0x20 | HotkeyBinding.Control | HotkeyBinding.Shift),
            new((int)LeftPanelHotkeyCommand.Rename, 0x71),
            new((int)LeftPanelHotkeyCommand.Search, 0x72),
        ];

        public bool IsBranchFilterActive { get; set; }

        public IReadOnlyList<LeftPanelScript> Scripts { get; set; } = [];

        public List<int> ScriptsRun { get; } = [];

        public IReadOnlyList<LeftPanelScript> GetScripts() => Scripts;

        public void RunScript(int scriptId) => ScriptsRun.Add(scriptId);

        public IReadOnlyList<IGitRef> GetRefs()
        {
            RefsRequested++;
            return [.. Refs];
        }

        public string GetCurrentBranch() => "main";

        public IReadOnlyDictionary<string, AheadBehindData>? GetAheadBehindData()
            => new Dictionary<string, AheadBehindData> { ["main"] = new("main", "refs/remotes/origin/main", "1", "2") };

        public IReadOnlyList<Remote> GetRemotes()
            => [new("upstream", "git@example.org:owner/repo.git", "git@example.org:owner/repo.git"), new("origin", "https://github.com/owner/repo.git", "https://github.com/owner/repo.git")];

        public IReadOnlyList<Remote> GetDisabledRemotes() => [new("old", "https://example.org/old.git", "https://example.org/old.git")];

        public IReadOnlyCollection<GitRevision> GetStashes() => Stashes;

        public IReadOnlyList<GitWorktree> GetWorktrees()
            =>
            [
                new(@"C:\src\repo", GitWorktreeHeadType.Branch, "abcdef1234567890", "main", IsDeleted: false) { IsMain = true },
                new(@"C:\src\repo.worktrees\feature-a", GitWorktreeHeadType.Branch, null, "feature-a", IsDeleted: false),
                new(@"C:\src\repo.worktrees\feature-b", GitWorktreeHeadType.Detached, "1234567890abcdef1234567890abcdef12345678", null, IsDeleted: true),
            ];

        public bool DirectoryExists(string path) => true;

        public Task<IReadOnlyCollection<string>> GetMergedBranchesAsync(string commit, CancellationToken cancellationToken)
        {
            MergedBranchesOf.Add(commit);
            return Task.FromResult(MergedBranches);
        }

        public string GetSubmoduleToolTip(SubmoduleInfo info, IReadOnlyList<GitItemStatus>? gitStatus) => $"tooltip of {info.Text}";

        public void UpdateSubmodules() => SubmodulesRequested++;

        public void RaiseSubmodules(LeftPanelSubmodules submodules) => SubmodulesUpdated?.Invoke(this, submodules);

        public Task<T> RunInBackgroundAsync<T>(Func<T> work, CancellationToken cancellationToken) => Task.FromResult(work());

        public bool Run(LeftPanelAction action, LeftPanelNode? node)
        {
            Runs.Add((action, node));
            return true;
        }

        public void FilterRevisionGrid(string? refs) => Filters.Add(refs);

        public void CopyToClipboard(string text) => Copied.Add(text);

        public void ShowRevisionNotInGrid(ObjectId objectId) => RevisionsNotInGrid.Add(objectId);

        public void ShowSubmoduleDirectoryMissing(string directory, string submoduleName)
        {
        }
    }
}
