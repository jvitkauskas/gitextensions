using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the fourth batch of phase 2 dialogs.</summary>
[TestFixture]
public sealed class Batch4ViewModelTests
{
    [TestCase(true, true)]
    [TestCase(false, null)]
    public void CheckoutRevision_checks_out_the_selected_commit(bool proceeded, bool? expectedClosed)
    {
        FakeCheckoutHost host = new() { Result = proceeded };
        CheckoutRevisionViewModel viewModel = CreateCheckout(host, CreateRefDialogViewModelTests.Commit.ToString());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Force = true;

        viewModel.CheckoutCommand.Execute(null);

        host.Checkouts.Should().Equal((CreateRefDialogViewModelTests.Commit, true));
        closed.Should().Be(expectedClosed);
    }

    [Test]
    public void CheckoutRevision_requires_a_commit()
    {
        FakeCheckoutHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CheckoutRevisionViewModel viewModel = CreateCheckout(host, revision: null, messageBoxes);

        viewModel.CheckoutCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("Select 1 revision to checkout.");
        host.Checkouts.Should().BeEmpty();
    }

    [Test]
    public void LocalRemoteBranchSelector_switches_the_branches_and_reports_the_commit_count()
    {
        FakeBranchSelectorHost host = new();
        LocalRemoteBranchSelectorViewModel selector = new(new LocalRemoteBranchSelectorStrings(), remote: true, host);

        selector.IsRemote.Should().BeTrue();
        selector.Branches.Should().Equal("origin/main", "origin/dev");

        selector.BranchName = "origin/dev";
        selector.CommitCountText.Should().Be("origin/dev: 3 commits");

        selector.BranchName = "typed";
        selector.CommitCountText.Should().BeEmpty("only existing branches are compared");

        selector.IsLocal = true;
        selector.Branches.Should().Equal("main", "dev");
        selector.BranchName.Should().BeEmpty();
    }

    [Test]
    public void CompareToBranch_requires_a_branch()
    {
        CompareToBranchViewModel viewModel = new(new CompareToBranchStrings(), new LocalRemoteBranchSelectorViewModel(new LocalRemoteBranchSelectorStrings(), true, new FakeBranchSelectorHost()));
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.CompareCommand.Execute(null);
        closed.Should().BeNull();

        viewModel.BranchSelector.BranchName = "origin/main";
        viewModel.CompareCommand.Execute(null);

        closed.Should().BeTrue();
        viewModel.BranchName.Should().Be("origin/main");
    }

    [TestCase(true, true, new[] { "start", "range" }, true)]
    [TestCase(true, false, new[] { "start" }, null)]
    [TestCase(false, true, new[] { "start" }, null)]
    public void Bisect_start_offers_marking_the_selected_range(bool hasRange, bool confirm, string[] expectedCalls, bool? expectedClosed)
    {
        FakeBisectHost host = new() { HasRange = hasRange };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new() { ConfirmResult = confirm };
        BisectViewModel viewModel = new(new BisectStrings(), host, messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.CanStart.Should().BeTrue();
        viewModel.StartCommand.Execute(null);

        host.Calls.Should().Equal(expectedCalls);
        viewModel.IsInTheMiddleOfBisect.Should().BeTrue();
        viewModel.CanStart.Should().BeFalse();
        closed.Should().Be(expectedClosed);
    }

    [TestCase(BisectMark.Good, "good")]
    [TestCase(BisectMark.Bad, "bad")]
    [TestCase(BisectMark.Skip, "skip")]
    public void Bisect_marks_the_current_revision(BisectMark mark, string expectedCall)
    {
        FakeBisectHost host = new() { InTheMiddle = true };
        BisectViewModel viewModel = new(new BisectStrings(), host, new ProcessViewModelTests.FakeMessageBoxes());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.MarkCommand.Execute(mark);

        host.Calls.Should().Equal(expectedCall);
        closed.Should().BeTrue();
    }

    [Test]
    public void GoToCommit_takes_the_revision_from_the_focused_input()
    {
        GoToCommitViewModel viewModel = new(
            new GoToCommitStrings(), [new GitRefItem("v1.0", "tag-guid")], [new GitRefItem("main", "branch-guid")], clipboardRevision: "HEAD~1", () => { });

        viewModel.CommitExpression.Should().Be("HEAD~1", "a revision on the clipboard is offered");
        viewModel.SelectedRevision.Should().Be("HEAD~1");

        viewModel.TagText = "v1.0";
        viewModel.Source = GoToCommitSource.Tag;
        viewModel.SelectedRevision.Should().Be("tag-guid");

        viewModel.Source = GoToCommitSource.Branch;
        viewModel.SelectedRevision.Should().BeEmpty("no branch is entered");
    }

    [Test]
    public void GoToCommit_picking_a_branch_goes_to_it()
    {
        GoToCommitViewModel viewModel = new(new GoToCommitStrings(), [], [new GitRefItem("main", "branch-guid")], null, () => { });
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.GoTo(GoToCommitSource.Branch, viewModel.Branches[0]);

        closed.Should().BeTrue();
        viewModel.SelectedRevision.Should().Be("branch-guid");
    }

    [TestCase(null, "Enter Caption")]
    [TestCase("Work", "Rename category")]
    public void DashboardCategoryTitle_titles_new_and_renamed_categories(string? originalName, string expectedTitle)
    {
        DashboardCategoryTitleViewModel viewModel = new(new DashboardCategoryTitleStrings(), ["Work"], originalName, new ProcessViewModelTests.FakeMessageBoxes());

        viewModel.Title.Should().Be(expectedTitle);
        viewModel.OkCommand.CanExecute(null).Should().Be(originalName is null, "an unchanged name cannot be accepted");
    }

    [TestCase("", "Category name is required")]
    [TestCase("Work", "Category name already exists")]
    public void DashboardCategoryTitle_validates_the_name(string name, string expectedError)
    {
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        DashboardCategoryTitleViewModel viewModel = new(new DashboardCategoryTitleStrings(), ["Work"], "Old", messageBoxes) { CategoryName = name };

        viewModel.OkCommand.Execute(null);

        messageBoxes.Errors.Should().Equal(expectedError);
        viewModel.Category.Should().BeNull();
    }

    [Test]
    public void DashboardCategoryTitle_accepts_a_new_name()
    {
        DashboardCategoryTitleViewModel viewModel = new(new DashboardCategoryTitleStrings(), ["Work"], null, new ProcessViewModelTests.FakeMessageBoxes()) { CategoryName = "Home" };
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.OkCommand.Execute(null);

        viewModel.Category.Should().Be("Home");
        closed.Should().BeTrue();
    }

    [Test]
    public void AddToGitIgnore_previews_the_ignored_files()
    {
        FakeGitIgnoreHost host = new();
        AddToGitIgnoreViewModel viewModel = new(new AddToGitIgnoreStrings(), localExclude: true, ["*.log", "bin/"], host);

        viewModel.Title.Should().Be("Add file(s) to .git/info/exclude");
        viewModel.IsUpdating.Should().BeTrue();
        viewModel.PreviewFiles.Should().Equal("Updating ...");
        host.Requests.Should().ContainSingle().Which.Should().Equal("*.log", "bin/");

        host.Report!(["a.log", "b.log"]);

        viewModel.IsUpdating.Should().BeFalse();
        viewModel.PreviewFiles.Should().Equal("a.log", "b.log");
        viewModel.MatchStatus.Should().Be("2 file(s) matched");
        viewModel.HasNoMatch.Should().BeFalse();
    }

    [Test]
    public void AddToGitIgnore_ignores_stale_previews_and_reports_no_match()
    {
        FakeGitIgnoreHost host = new();
        AddToGitIgnoreViewModel viewModel = new(new AddToGitIgnoreStrings(), localExclude: false, ["*.log"], host);
        Action<IReadOnlyList<string>> staleReport = host.Report!;

        viewModel.Patterns = "*.tmp";
        staleReport(["a.log"]);
        viewModel.IsUpdating.Should().BeTrue("the result is for patterns that were changed since");

        host.Report!([]);
        viewModel.HasNoMatch.Should().BeTrue();
        viewModel.MatchStatus.Should().Be("0 file(s) matched");
    }

    [TestCase("*.log\r\n\r\nbin/", true)]
    [TestCase("", false)]
    public void AddToGitIgnore_adds_the_non_empty_patterns(string patterns, bool expectAdded)
    {
        FakeGitIgnoreHost host = new();
        AddToGitIgnoreViewModel viewModel = new(new AddToGitIgnoreStrings(), false, [], host) { Patterns = patterns };
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.IgnoreCommand.Execute(null);

        if (expectAdded)
        {
            host.Added.Should().Equal("*.log", "bin/");
        }
        else
        {
            host.Added.Should().BeNull();
        }

        closed.Should().Be(expectAdded);
    }

    internal static CheckoutRevisionViewModel CreateCheckout(ICheckoutRevisionHost host, string? revision, ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null)
    {
        messageBoxes ??= new();
        CommitPickerViewModel picker = new(
            new CreateRefDialogViewModelTests.FakeCommitPickerHost { Commits = { [CreateRefDialogViewModelTests.Commit.ToString()] = CreateRefDialogViewModelTests.Commit } },
            messageBoxes,
            "Error");
        picker.SetSelectedCommitHash(revision);
        return new CheckoutRevisionViewModel(new CheckoutRevisionStrings(), picker, host, messageBoxes);
    }

    internal sealed class FakeCheckoutHost : ICheckoutRevisionHost
    {
        public bool Result { get; init; } = true;

        public List<(ObjectId Commit, bool Force)> Checkouts { get; } = [];

        public bool Checkout(ObjectId objectId, bool force)
        {
            Checkouts.Add((objectId, force));
            return Result;
        }
    }

    internal sealed class FakeBranchSelectorHost : ILocalRemoteBranchSelectorHost
    {
        public IReadOnlyList<string> GetBranches(bool remote) => remote ? ["origin/main", "origin/dev"] : ["main", "dev"];

        public void RequestCommitCount(string branch, Action<string> report) => report($"{branch}: 3 commits");
    }

    internal sealed class FakeBisectHost : IBisectHost
    {
        public bool InTheMiddle { get; set; }

        public bool HasRange { get; init; }

        public List<string> Calls { get; } = [];

        public bool IsInTheMiddleOfBisect() => InTheMiddle;

        public void Start()
        {
            Calls.Add("start");
            InTheMiddle = true;
        }

        public bool HasSelectedRange() => HasRange;

        public void MarkSelectedRange() => Calls.Add("range");

        public void Mark(BisectMark mark) => Calls.Add(mark.ToString().ToLowerInvariant());

        public void Stop() => Calls.Add("stop");
    }

    internal sealed class FakeGitIgnoreHost : IAddToGitIgnoreHost
    {
        public List<IReadOnlyList<string>> Requests { get; } = [];

        public Action<IReadOnlyList<string>>? Report { get; private set; }

        public IReadOnlyList<string>? Added { get; private set; }

        public void RequestIgnoredFiles(IReadOnlyList<string> patterns, Action<IReadOnlyList<string>> report)
        {
            Requests.Add(patterns);
            Report = report;
        }

        public void AddPatterns(IReadOnlyList<string> patterns) => Added = patterns;
    }
}
