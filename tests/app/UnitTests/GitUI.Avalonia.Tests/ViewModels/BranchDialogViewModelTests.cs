using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the branch dialogs (phase 2, batch 3).</summary>
[TestFixture]
public sealed class BranchDialogViewModelTests
{
    [Test]
    public void BranchSelector_skips_and_reports_unknown_branches()
    {
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        BranchSelectorViewModel selector = CreateSelector(["main", "dev"], messageBoxes: messageBoxes);
        int changes = 0;
        selector.SelectionChanged += (_, _) => changes++;

        selector.Text = "main  gone dev";

        changes.Should().Be(1);
        selector.GetSelectedBranches().Should().Equal("main", "dev");
        messageBoxes.Errors.Should().BeEmpty();

        selector.GetSelectedBranches(reportInvalid: true).Should().Equal("main", "dev");
        messageBoxes.Errors.Should().ContainSingle().Which.Should().Contain("'gone'");
    }

    [Test]
    public void BranchSelector_multiple_selection_replaces_the_text_unless_cancelled()
    {
        IReadOnlyList<string>? offered = null;
        IReadOnlyList<string>? result = ["dev", "main"];
        BranchSelectorViewModel selector = CreateSelector(["main", "dev"], selectMultiple: selected =>
        {
            offered = selected;
            return result;
        });
        selector.Text = "main";

        selector.SelectMultipleCommand.Execute(null);
        offered.Should().Equal("main");
        selector.Text.Should().Be("dev main");

        result = null;
        selector.SelectMultipleCommand.Execute(null);
        selector.Text.Should().Be("dev main");
    }

    [Test]
    public void HelpImage_persists_the_expanded_state_when_toggled()
    {
        List<bool> saved = [];
        HelpImageViewModel helpImage = new(new HelpImageStrings(), isVisible: true, isExpanded: true, saved.Add);

        saved.Should().BeEmpty("the initial state is not saved again");
        helpImage.ToggleExpandedCommand.Execute(null);

        helpImage.IsExpanded.Should().BeFalse();
        saved.Should().Equal(false);
    }

    [Test]
    public void DeleteBranch_refuses_the_current_branch()
    {
        FakeDeleteBranchHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        DeleteBranchViewModel viewModel = new(new DeleteBranchStrings(), CreateSelector(["main", "dev"], "main dev"), "main", null, host, messageBoxes);

        viewModel.DeleteCommand.Execute(null);

        messageBoxes.Errors.Should().ContainSingle().Which.Should().Contain("main");
        host.Deleted.Should().BeNull();
    }

    [TestCase("main", new[] { "dev" }, false)]
    [TestCase("main", new string[0], true)]
    [TestCase("(HEAD detached at 1234567)", new[] { "dev" }, true)]
    public void DeleteBranch_confirms_unmerged_branches(string currentBranch, string[] merged, bool expectConfirmation)
    {
        FakeDeleteBranchHost host = new() { ConfirmResult = true };
        DeleteBranchViewModel viewModel = new(
            new DeleteBranchStrings(), CreateSelector(["main", "dev"], "dev"), currentBranch, new HashSet<string>(merged), host, new ProcessViewModelTests.FakeMessageBoxes());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.DeleteCommand.Execute(null);

        host.Confirmations.Should().Be(expectConfirmation ? 1 : 0);
        host.Deleted.Should().Equal("dev");
        closed.Should().BeTrue();
    }

    [Test]
    public void DeleteBranch_skips_confirmation_when_suppressed_and_keeps_worktree_branches()
    {
        FakeDeleteBranchHost host = new() { ExcludedByWorktrees = ["wt"], ConfirmResult = false };
        DeleteBranchViewModel viewModel = new(
            new DeleteBranchStrings(), CreateSelector(["main", "dev", "wt"], "dev wt"), "main", mergedBranches: null, host, new ProcessViewModelTests.FakeMessageBoxes());

        viewModel.DeleteCommand.Execute(null);

        host.Confirmations.Should().Be(0);
        host.Deleted.Should().Equal("dev");
    }

    [Test]
    public void DeleteBranch_cancelled_confirmation_deletes_nothing()
    {
        FakeDeleteBranchHost host = new() { ConfirmResult = false };
        DeleteBranchViewModel viewModel = new(
            new DeleteBranchStrings(), CreateSelector(["main", "dev"], "dev"), "main", new HashSet<string>(), host, new ProcessViewModelTests.FakeMessageBoxes());

        viewModel.DeleteCommand.Execute(null);

        host.Confirmations.Should().Be(1);
        host.Deleted.Should().BeNull();
    }

    [Test]
    public void DeleteRemoteBranch_lists_the_local_tracking_branches()
    {
        FakeDeleteRemoteBranchHost host = new() { Tracking = { ["origin/dev"] = ["dev"], ["origin/many"] = [.. Enumerable.Range(1, 10).Select(i => $"b{i}")] } };
        DeleteRemoteBranchViewModel viewModel = new(new DeleteRemoteBranchStrings(), CreateSelector(["origin/dev", "origin/main", "origin/many"], "origin/main"), host, new ProcessViewModelTests.FakeMessageBoxes());

        viewModel.CanDeleteLocalTrackingBranch.Should().BeFalse();
        viewModel.TrackingBranchesText.Should().BeEmpty();

        viewModel.Branches.Text = "origin/dev";
        viewModel.CanDeleteLocalTrackingBranch.Should().BeTrue();
        viewModel.TrackingBranchesText.Should().Contain(" - dev");

        viewModel.Branches.Text = "origin/many";
        viewModel.TrackingBranchesText.Should().Contain(" - b8").And.NotContain(" - b9").And.Contain("and 2 more...");
    }

    [TestCase(false, true, 1, true)]
    [TestCase(true, true, 1, true)]
    [TestCase(true, false, 0, null)]
    public void DeleteRemoteBranch_requires_the_confirmation_and_asks_for_unmerged_branches(bool unmerged, bool confirm, int expectedDeletions, bool? expectedClosed)
    {
        FakeDeleteRemoteBranchHost host = new() { HasUnmerged = unmerged };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new() { ConfirmResult = confirm };
        DeleteRemoteBranchViewModel viewModel = new(new DeleteRemoteBranchStrings(), CreateSelector(["origin/dev"], "origin/dev"), host, messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.DeleteCommand.CanExecute(null).Should().BeFalse("deleting from the remote must be confirmed");
        viewModel.DeleteRemote = true;
        viewModel.DeleteCommand.CanExecute(null).Should().BeTrue();
        viewModel.DeleteCommand.Execute(null);

        messageBoxes.Confirmations.Should().HaveCount(unmerged ? 1 : 0);
        host.Deletions.Should().HaveCount(expectedDeletions);
        closed.Should().Be(expectedClosed);
    }

    [Test]
    public void MergeBranch_starts_with_the_options_without_saving_them()
    {
        FakeMergeBranchHost host = new();
        MergeBranchViewModel viewModel = CreateMerge(host, new MergeBranchOptions(NoFastForward: true, NoCommit: true, AddLogMessages: true, LogMessagesCount: 7, ShowAdvanced: false));

        viewModel.NoFastForward.Should().BeTrue();
        viewModel.IsFastForward.Should().BeFalse();
        viewModel.CanSquash.Should().BeFalse();
        viewModel.HelpImage.IsOnHoverShowImage2.Should().BeFalse();
        viewModel.NoCommit.Should().BeTrue();
        viewModel.LogMessagesCount.Should().Be(7);
        host.SavedLogSettings.Should().BeEmpty();

        viewModel.LogMessagesCount = 9;
        host.SavedLogSettings.Should().Equal((true, 9));
    }

    [Test]
    public void MergeBranch_hiding_the_advanced_options_resets_them()
    {
        MergeBranchViewModel viewModel = CreateMerge(new FakeMergeBranchHost(), new MergeBranchOptions(false, false, false, 20, ShowAdvanced: true));
        viewModel.Squash = true;
        viewModel.AllowUnrelatedHistories = true;
        viewModel.UseNonDefaultStrategy = true;
        viewModel.MergeStrategy = "ours";
        viewModel.AddMergeMessage = true;

        viewModel.ShowAdvanced = false;

        viewModel.Squash.Should().BeFalse();
        viewModel.AllowUnrelatedHistories.Should().BeFalse();
        viewModel.UseNonDefaultStrategy.Should().BeFalse();
        viewModel.MergeStrategy.Should().BeEmpty();
        viewModel.AddMergeMessage.Should().BeFalse();
    }

    [Test]
    public void MergeBranch_no_fast_forward_disables_squash()
    {
        MergeBranchViewModel viewModel = CreateMerge(new FakeMergeBranchHost(), new MergeBranchOptions(false, false, false, 20, ShowAdvanced: true));
        viewModel.Squash = true;

        viewModel.NoFastForward = true;

        viewModel.Squash.Should().BeFalse();
        viewModel.CanSquash.Should().BeFalse();
        viewModel.HelpImage.IsOnHoverShowImage2.Should().BeFalse();
    }

    [TestCase(true, true)]
    [TestCase(false, null)]
    public void MergeBranch_merges_the_chosen_options(bool merged, bool? expectedClosed)
    {
        FakeMergeBranchHost host = new() { MergeResult = merged };
        MergeBranchViewModel viewModel = CreateMerge(host, new MergeBranchOptions(false, false, AddLogMessages: true, LogMessagesCount: 5, ShowAdvanced: true));
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Branches.Text = "dev";
        viewModel.MergeStrategy = "ours";
        viewModel.AddMergeMessage = true;
        viewModel.MergeMessage = "Merge dev";

        viewModel.MergeCommand.Execute(null);

        host.Requests.Should().ContainSingle().Which.Should().Be(
            new MergeRequest("dev", FastForward: true, Squash: false, NoCommit: false, Strategy: null, AllowUnrelatedHistories: false, "Merge dev", LogMessages: 5));
        closed.Should().Be(expectedClosed);

        viewModel.UseNonDefaultStrategy = true;
        viewModel.MergeCommand.Execute(null);
        host.Requests[^1].Strategy.Should().Be("ours");
    }

    internal static BranchSelectorViewModel CreateSelector(
        IReadOnlyList<string> branches,
        string text = "",
        Func<IReadOnlyList<string>, IReadOnlyList<string>?>? selectMultiple = null,
        ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null)
        => new(new BranchSelectorStrings(), branches, selectMultiple ?? (_ => null), messageBoxes ?? new(), "Error") { Text = text };

    internal static MergeBranchViewModel CreateMerge(IMergeBranchHost host, MergeBranchOptions options)
        => new(
            new MergeBranchStrings(),
            CreateSelector(["main", "dev", "origin/dev"], "dev"),
            "main",
            options,
            new HelpImageViewModel(new HelpImageStrings(), isVisible: true, isExpanded: true, _ => { }),
            host);

    internal sealed class FakeDeleteBranchHost : IDeleteBranchHost
    {
        public IReadOnlyList<string> ExcludedByWorktrees { get; init; } = [];

        public bool ConfirmResult { get; init; }

        public int Confirmations { get; private set; }

        public IReadOnlyList<string>? Deleted { get; private set; }

        public IReadOnlyList<string> HandleWorktreeBranches(IReadOnlyList<string> branches) => [.. branches.Except(ExcludedByWorktrees)];

        public bool ConfirmDeleteUnmerged()
        {
            Confirmations++;
            return ConfirmResult;
        }

        public bool DeleteBranches(IReadOnlyList<string> branches)
        {
            Deleted = branches;
            return true;
        }
    }

    internal sealed class FakeDeleteRemoteBranchHost : IDeleteRemoteBranchHost
    {
        public Dictionary<string, IReadOnlyList<string>> Tracking { get; } = [];

        public bool HasUnmerged { get; init; }

        public List<(IReadOnlyList<string> Branches, bool DeleteTracking)> Deletions { get; } = [];

        public IReadOnlyList<string> GetTrackingBranches(IReadOnlyList<string> remoteBranches)
            => [.. remoteBranches.SelectMany(b => Tracking.TryGetValue(b, out IReadOnlyList<string>? tracking) ? tracking : [])];

        public bool HasUnmergedBranches(IReadOnlyList<string> remoteBranches) => HasUnmerged;

        public bool DeleteRemoteBranches(IReadOnlyList<string> remoteBranches, bool deleteLocalTrackingBranches)
        {
            Deletions.Add((remoteBranches, deleteLocalTrackingBranches));
            return true;
        }
    }

    internal sealed class FakeMergeBranchHost : IMergeBranchHost
    {
        public bool MergeResult { get; init; } = true;

        public List<(bool AddLogMessages, int Count)> SavedLogSettings { get; } = [];

        public List<MergeRequest> Requests { get; } = [];

        public void SaveLogMessagesSettings(bool addLogMessages, int count) => SavedLogSettings.Add((addLogMessages, count));

        public void OpenStrategyHelp()
        {
        }

        public bool Merge(MergeRequest request)
        {
            Requests.Add(request);
            return MergeResult;
        }
    }
}
