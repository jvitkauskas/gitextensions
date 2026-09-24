using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The filter toolbar of the main window (port of <c>FilterToolBar</c>).</summary>
[TestFixture]
public sealed class FilterToolBarViewTests : HeadlessTest
{
    [Test]
    public void The_branch_filter_ignores_the_references_that_do_not_exist()
    {
        FakeFilterHost host = new();
        FilterToolBarViewModel viewModel = new(new FilterToolBarStrings(), host) { BranchFilter = "main missing --all feat* ^main origin/main..main" };

        viewModel.ApplyBranchFilter();

        host.Calls.Should().Equal("branch main --all feat* ^main origin/main..main");
        host.Warnings.Should().Equal("\"missing\" is not a Git revision and will be ignored.|Git revision does not exist");

        // From the left panel, the branches are not checked.
        viewModel.SetBranchFilter("missing");
        host.Calls[^1].Should().Be("branch missing");
        host.Warnings.Should().HaveCount(1);
    }

    [Test]
    public void The_text_filter_applies_its_kinds_and_shows_the_filter_of_the_grid()
    {
        FakeFilterHost host = new() { History = ["older"] };
        FilterToolBarViewModel viewModel = new(new FilterToolBarStrings(), host);
        viewModel.RevisionFilterItems.Should().StartWith("older").And.HaveCount(4, "the saved filters and the examples of git options");

        viewModel.RevisionFilter = " fix ";
        viewModel.ApplyRevisionFilter();
        host.Calls.Should().Equal("text fix message");

        // Another kind applies the text again.
        viewModel.ByAuthor = true;
        host.Calls[^1].Should().Be("text fix message author");

        // As revisionGridFilter_FilterChanged: the kinds with the filter of the grid are checked, the filter saved.
        host.Raise(new RevisionGridFilterState(AuthorFilter: "bob", CommitterFilter: "bob", HasFilter: true, FilterSummary: "Author: bob"));
        viewModel.RevisionFilter.Should().Be("bob");
        (viewModel.ByMessage, viewModel.ByCommitter, viewModel.ByAuthor, viewModel.ByDiffContent).Should().Be((false, true, true, false));
        viewModel.RevisionFilterItems[0].Should().Be("bob");
        host.History.Should().StartWith("bob", "older");
        host.Calls.Should().HaveCount(2, "showing the filter of the grid does not apply it again");
        viewModel.AdvancedFilterIcon.Should().Be("FunnelExclamation");
        viewModel.AdvancedFilterToolTip.Should().Be("Author: bob");
        viewModel.CanResetAllFilters.Should().BeTrue();

        // Without a text filter, the text is cleared and the kinds kept.
        host.Raise(new RevisionGridFilterState());
        viewModel.RevisionFilter.Should().BeEmpty();
        viewModel.ByAuthor.Should().BeTrue();
        viewModel.AdvancedFilterIcon.Should().Be("FunnelPencil");
    }

    [Test]
    public async Task The_branch_filter_lists_the_matching_refs_of_the_chosen_kinds()
    {
        FakeFilterHost host = new();
        FilterToolBarViewModel viewModel = new(new FilterToolBarStrings(), host) { BranchFilter = "FEAT", IncludeTags = true };

        await viewModel.UpdateBranchItemsAsync();
        host.RefKinds.Should().Equal((true, true, false));
        viewModel.BranchItems.Should().Equal("feature", "feature/two");

        viewModel.BranchFilter = "nothing";
        await viewModel.UpdateBranchItemsAsync();
        viewModel.BranchItems.Should().Equal("<No results found>");
    }

    [Test]
    public Task The_toolbar_shows_the_branch_mode_and_runs_the_toggles() => OnUiThreadAsync(() =>
    {
        FakeFilterHost host = new();
        FilterToolBarViewModel viewModel = new(new FilterToolBarStrings(), host);
        FilterToolBarView view = new() { DataContext = viewModel };
        Window window = new() { Width = 900, Height = 60, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        view.FindControl<DropDownButton>("branchesButton")!.GetLogicalDescendants().OfType<TextBlock>().Single().Text.Should().Be("All branches");
        host.Raise(new RevisionGridFilterState(ShowAllBranches: false, ShowCurrentBranchOnly: true));
        Dispatcher.UIThread.RunJobs();
        view.FindControl<DropDownButton>("branchesButton")!.GetLogicalDescendants().OfType<TextBlock>().Single().Text.Should().Be("Current branch only");

        view.FindControl<ToggleButton>("firstParentButton")!.Command!.Execute(null);
        view.FindControl<ToggleButton>("reflogButton")!.Command!.Execute(null);
        host.Calls.Should().Equal("first parent", "reflog");

        // Without a filter the advanced filter button opens the dialog.
        view.FindControl<SplitButton>("advancedFilterButton")!.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(SplitButton.ClickEvent));
        host.Calls[^1].Should().Be("dialog");
        SaveScreenshot(window.CaptureRenderedFrame(), "filter-toolbar");
        window.Close();
    });

    internal sealed class FakeFilterHost : IRevisionGridFilterHost
    {
        public event EventHandler? FilterChanged;

        public RevisionGridFilterState State { get; private set; } = new();

        public List<string> Calls { get; } = [];

        public List<string> Warnings { get; } = [];

        public List<(bool Local, bool Tags, bool Remotes)> RefKinds { get; } = [];

        public IReadOnlyList<string> History { get; set; } = [];

        public bool IsValidWorkingDir => true;

        public IReadOnlyList<string> RevisionFilterHistory
        {
            get => History;
            set => History = value;
        }

        public void Raise(RevisionGridFilterState state)
        {
            State = state;
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetAndApplyBranchFilter(string filter) => Calls.Add($"branch {filter}");

        public void SetAndApplyRevisionFilter(RevisionTextFilter filter)
            => Calls.Add($"text {filter.Text}{(filter.ByMessage ? " message" : "")}{(filter.ByCommitter ? " committer" : "")}{(filter.ByAuthor ? " author" : "")}{(filter.ByDiffContent ? " diff" : "")}");

        public void SetAndApplyPathFilter(string filter) => Calls.Add($"path {filter}");

        public void ResetAllFiltersAndRefresh() => Calls.Add("reset");

        public void ShowAllBranches() => Calls.Add("all");

        public void ShowCurrentBranchOnly() => Calls.Add("current");

        public void ShowFilteredBranches() => Calls.Add("filtered");

        public void ToggleShowOnlyFirstParent() => Calls.Add("first parent");

        public void ToggleShowReflogReferences() => Calls.Add("reflog");

        public void ShowRevisionFilterDialog() => Calls.Add("dialog");

        public Task<IReadOnlyList<string>> GetRefNamesAsync(bool local, bool tags, bool remotes)
        {
            RefKinds.Add((local, tags, remotes));
            return Task.FromResult<IReadOnlyList<string>>(["main", "feature", "feature/two", "v1.0"]);
        }

        public IReadOnlyList<string> GetRefLocalNames() => ["main", "origin/main"];

        public bool RevisionExists(string revision) => revision == "main";

        public void ShowWarning(string heading, string caption) => Warnings.Add($"{heading}|{caption}");
    }
}
