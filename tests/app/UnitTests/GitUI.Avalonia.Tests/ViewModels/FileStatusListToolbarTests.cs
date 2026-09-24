using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>The toolbar of the file status list: git grep, the A/B filter, the groups and the settings (<c>FileStatusList.Toolbar.cs</c>).</summary>
[TestFixture]
public sealed class FileStatusListToolbarTests
{
    [Test]
    public void Git_grep_adds_the_matching_files_as_a_group_and_remembers_the_expression()
    {
        List<(GitRevision Revision, string Arguments)> greps = [];
        FileStatusListViewModel viewModel = CreateWithGrep(greps);
        viewModel.SetDiff(First, Second, CreateStatuses());

        viewModel.GitGrepText = "needle";

        greps.Should().Equal((Second, @"-e ""needle"""));
        viewModel.Nodes.Should().HaveCount(2, "the diff and the git grep results");
        viewModel.Nodes[1].Children.Select(node => node.Entry!.Item.Name).Should().Equal("found.txt");
        viewModel.GitGrepHistory.Should().BeEmpty("the history changes when the box is left, not while typing");
        viewModel.AddGitGrepHistory(viewModel.GitGrepText);
        viewModel.GitGrepHistory.Should().Equal("needle");

        // An expression with its own -e is passed as is; searched from the prompt, it is first in the history.
        viewModel.SearchGitGrep(@"-e ""a\\b""");
        greps[^1].Arguments.Should().Be(@"-e ""a\\b""");
        viewModel.GitGrepHistory.Should().Equal(@"-e ""a\\b""", "needle");

        viewModel.SearchGitGrep("");
        viewModel.IsGitGrepActive.Should().BeFalse();
        viewModel.Nodes.Should().NotContain(node => node.Group != null && FileStatusTreeBuilder.IsGrepItemStatuses(node.Group));
    }

    [Test]
    public void Git_grep_runs_again_for_the_files_of_another_revision()
    {
        List<(GitRevision Revision, string Arguments)> greps = [];
        FileStatusListViewModel viewModel = CreateWithGrep(greps);
        viewModel.SetDiff(First, Second, CreateStatuses());
        viewModel.GitGrepText = "needle";

        viewModel.SetDiff(Second, First, CreateStatuses());

        greps.Select(grep => grep.Revision).Should().Equal(Second, First);
    }

    [Test]
    public void Git_grep_of_the_file_tree_shows_only_the_matching_files()
    {
        FileStatusListViewModel viewModel = new(new FileStatusListStrings()) { IsFileTreeMode = true };
        viewModel.GitGrep = (revision, arguments, fileTreeMode, cancellationToken) => Task.FromResult<FileStatusGroup?>(GrepGroup(revision));
        viewModel.GitGrepDelayMilliseconds = 0;
        viewModel.SetDiff(null, Second, CreateStatuses());

        viewModel.GitGrepText = "needle";

        viewModel.AllEntries.Select(entry => entry.Item.Name).Should().Equal("found.txt");
    }

    [Test]
    public void The_git_grep_button_shows_the_box_or_the_prompt_as_chosen()
    {
        FileStatusListViewModel viewModel = CreateWithGrep([]);
        int prompts = 0;
        viewModel.GitGrepDialogRequested += (_, _) => prompts++;
        viewModel.SetGitGrepBoxVisible(false);

        viewModel.ToggleGitGrepCommand.Execute(GitGrepUsing.InputBox);
        viewModel.IsGitGrepBoxVisible.Should().BeTrue();
        prompts.Should().Be(0);

        // Shown: the button hides it.
        viewModel.ToggleGitGrepCommand.Execute(null);
        viewModel.IsGitGrepBoxVisible.Should().BeFalse();

        viewModel.ToggleGitGrepCommand.Execute(GitGrepUsing.Dialog);
        prompts.Should().Be(1);
        viewModel.IsGitGrepBoxVisible.Should().BeFalse();
        AppSettings.FileStatusFindInFilesGitGrepTypeIndex.Value.Should().Be((int)GitGrepUsing.Dialog);
        viewModel.GitGrepUsing = GitGrepUsing.InputBox;
    }

    [Test]
    public void The_git_grep_prompt_starts_with_the_selected_text_of_the_viewer()
    {
        FileStatusListViewModel viewModel = CreateWithGrep([]);
        string? prompt = null;
        viewModel.GitGrepDialogRequested += (_, text) => prompt = text;
        viewModel.GetSelectedText = () => "selected";

        viewModel.OpenGitGrepDialogCommand.Execute(null);

        prompt.Should().Be("selected");
    }

    [Test]
    public void The_AB_buttons_filter_the_files_by_their_diff_status()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetGroups(
        [
            new FileStatusGroup(First, Second, "A", [new GitItemStatus("onlyA.txt") { IsChanged = true, DiffStatus = DiffBranchStatus.OnlyAChange }], IconName: FileStatusIcons.DiffA),
            new FileStatusGroup(First, Second, "B", [new GitItemStatus("same.txt") { IsChanged = true, DiffStatus = DiffBranchStatus.SameChange }], IconName: FileStatusIcons.DiffB),
        ]);
        viewModel.HasDiffABGroups.Should().BeTrue();
        viewModel.AllEntries.Should().HaveCount(2);

        viewModel.ShowOnlyA = false;

        viewModel.AllEntries.Select(entry => entry.Item.Name).Should().Equal("same.txt");
    }

    [Test]
    public void Collapse_groups_collapses_the_diff_groups_or_expands_the_selected_group()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetGroups(
        [
            new FileStatusGroup(First, Second, "first", CreateStatuses()),
            new FileStatusGroup(null, Second, "second", CreateStatuses()),
        ]);
        viewModel.Nodes[0].IsExpanded.Should().BeTrue();

        viewModel.CollapseGroupsCommand.Execute(null);
        viewModel.Nodes.Should().OnlyContain(node => !node.IsExpanded);

        viewModel.CollapseGroupsCommand.Execute(null);
        viewModel.Nodes.Count(node => node.IsExpanded).Should().Be(1, "the selected group is expanded again");
    }

    [Test]
    public void Select_all_selects_the_files_below_the_selected_folder()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetDiff(First, Second, CreateStatuses());
        FileStatusNode src = viewModel.Nodes.Single(node => node.Text == "src");
        viewModel.SelectedNodes.Clear();
        viewModel.SelectedNodes.Add(src);
        viewModel.HasSelectedNodesWithChildren.Should().BeTrue();

        viewModel.SelectAllCommand.Execute(null);

        viewModel.SelectedEntries.Select(entry => entry.Item.Name).Should().BeEquivalentTo("src/Program.cs", "src/Util/Helper.cs");
    }

    [Test]
    public void The_files_shown_by_the_settings_refresh_the_list()
    {
        FileStatusListViewModel viewModel = new(new FileStatusListStrings()) { HasFileSettings = true };
        int refreshes = 0;
        viewModel.RefreshRequested += (_, _) => refreshes++;

        viewModel.ShowUntrackedFiles = false;
        viewModel.ShowSkipWorktreeFiles = true;

        refreshes.Should().Be(2);
        viewModel.FileOptions.Should().Be(new FileStatusFileOptions(ShowSkipWorktreeFiles: true, ShowUntrackedFiles: false));
    }

    private static FileStatusListViewModel CreateWithGrep(List<(GitRevision Revision, string Arguments)> greps)
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.GitGrep = (revision, arguments, fileTreeMode, cancellationToken) =>
        {
            greps.Add((revision, arguments));
            return Task.FromResult<FileStatusGroup?>(GrepGroup(revision));
        };
        viewModel.GitGrepDelayMilliseconds = 0;
        return viewModel;
    }

    private static FileStatusGroup GrepGroup(GitRevision revision)
        => new(null, revision, "grep", [new GitItemStatus("found.txt") { IsTracked = true, GrepString = "needle" }], IconName: FileStatusIcons.GitGrepIconName);
}
