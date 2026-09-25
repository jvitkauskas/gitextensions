using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the checkout branch dialog (phase 2, batch 5).</summary>
[TestFixture]
public sealed class CheckoutBranchViewModelTests
{
    private static readonly ObjectId LocalCommit = ObjectId.Parse("1111111111111111111111111111111111111111");
    private static readonly ObjectId RemoteCommit = ObjectId.Parse("2222222222222222222222222222222222222222");
    private static readonly ObjectId BaseCommit = ObjectId.Parse("3333333333333333333333333333333333333333");

    [TestCase(false, "d", "dev")]
    [TestCase(true, "origin/f", "origin/feature")]
    public void Typing_a_partial_branch_does_not_request_a_commit_count(bool remote, string partialBranch, string branch)
    {
        FakeCheckoutBranchHost host = new();
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: null, remote));

        viewModel.Branch = partialBranch;

        host.CommitCountRequests.Should().BeEmpty("partial input is not a revision and must not cause a git error dialog");
        viewModel.CommitCountText.Should().BeEmpty();

        viewModel.Branch = branch;

        host.CommitCountRequests.Should().Equal(branch);
        viewModel.CommitCountText.Should().Be($"{branch}: 1 commit behind");

        viewModel.Branch = partialBranch;
        viewModel.CommitCountText.Should().BeEmpty();
        host.CommitCountRequests.Should().Equal(branch);
    }

    [Test]
    public void Lists_the_local_or_remote_branches()
    {
        CheckoutBranchViewModel viewModel = Create(new FakeCheckoutBranchHost(), Options(branch: "dev"));

        viewModel.IsLocal.Should().BeTrue();
        viewModel.Branches.Should().Equal("main", "dev");
        viewModel.Branch.Should().Be("dev");
        viewModel.CommitCountText.Should().Be("dev: 1 commit behind", "as in FormCheckoutBranch, the initial branch is compared too");

        viewModel.IsRemote = true;
        viewModel.Branches.Should().Equal("origin/main", "origin/feature");
        viewModel.Branch.Should().BeEmpty();

        viewModel.Branch = "origin/main";
        viewModel.CommitCountText.Should().Be("origin/main: 1 commit behind");
    }

    [Test]
    public void A_given_branch_that_is_not_listed_is_added()
    {
        CheckoutBranchViewModel viewModel = Create(new FakeCheckoutBranchHost(), Options(branch: "gone"));

        viewModel.Branches.Should().Contain("gone");
        viewModel.Branch.Should().Be("gone");
        viewModel.IsBranchInvalid.Should().BeFalse();

        viewModel.Branch = "typo";
        viewModel.IsBranchInvalid.Should().BeTrue();
    }

    [Test]
    public void Remote_branch_suggests_the_tracking_branch_and_a_custom_name()
    {
        CheckoutBranchViewModel viewModel = Create(new FakeCheckoutBranchHost(), Options(branch: "origin/main", remote: true));

        viewModel.LocalBranchNameText.Should().Be("'main'");
        viewModel.ResetBranchText.Should().Be("R_eset local branch with the name:", "the tracking branch exists");
        viewModel.CustomBranchName.Should().Be("origin_main");

        viewModel.Branch = "origin/feature";
        viewModel.LocalBranchNameText.Should().Be("'feature'");
        viewModel.ResetBranchText.Should().Be("Cr_eate local branch with same name:", "there is no local 'feature' branch");
    }

    [Test]
    public void Contain_filter_switches_to_the_other_branch_type_if_empty()
    {
        FakeCheckoutBranchHost host = new() { HasContainFilter = true, LocalBranches = [], RemoteBranches = ["origin/main"] };

        CheckoutBranchViewModel viewModel = Create(host, Options(branch: null));

        viewModel.IsRemote.Should().BeTrue();
        viewModel.Branch.Should().Be("origin/main", "a single branch is selected");
    }

    [TestCase(false, false, true, true)]
    [TestCase(false, true, false, false)]
    [TestCase(false, null, false, false)]
    [TestCase(false, null, true, true)]
    [TestCase(true, false, true, false)]
    public void Checks_out_without_dialog_as_the_settings_allow(bool alwaysShow, bool? isDirty, bool useDefaultAction, bool expected)
    {
        CheckoutBranchViewModel viewModel = Create(
            new FakeCheckoutBranchHost(), Options(branch: "dev") with { AlwaysShowDialog = alwaysShow, IsDirtyDir = isDirty, UseDefaultAction = useDefaultAction });

        viewModel.CanCheckoutWithoutDialog.Should().Be(expected);
    }

    [Test]
    public void Checks_out_a_local_branch_with_the_local_changes_action()
    {
        FakeCheckoutBranchHost host = new();
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "dev") with { IsDirtyDir = true });
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.HasUncommittedChanges.Should().BeTrue();
        viewModel.IsMerge = true;
        viewModel.SetAsDefault = true;
        viewModel.CheckoutCommand.Execute(null);

        host.Checkouts.Should().Equal(("dev", false, LocalChangesAction.Merge, CheckoutNewBranchMode.DontCreate, (string?)null, false));
        host.SavedDefault.Should().Be(LocalChangesAction.Merge);
        closed.Should().BeTrue();
    }

    [Test]
    public void Reset_is_not_remembered_as_default()
    {
        CheckoutBranchViewModel viewModel = Create(new FakeCheckoutBranchHost(), Options(branch: "dev"));
        viewModel.SetAsDefault = true;

        viewModel.IsReset = true;

        viewModel.SetAsDefault.Should().BeFalse();
        viewModel.CanSetAsDefault.Should().BeFalse();
    }

    [Test]
    public void Clean_working_directory_ignores_the_local_changes_action()
    {
        FakeCheckoutBranchHost host = new();
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "dev") with { IsDirtyDir = false, ChangesMode = LocalChangesAction.Stash });

        viewModel.HasUncommittedChanges.Should().BeFalse();
        viewModel.CheckoutCommand.Execute(null);

        host.Checkouts[0].LocalChanges.Should().Be(LocalChangesAction.DontChange);
        host.Stashes.Should().Be(0);
    }

    [Test]
    public void Stash_action_stashes_a_dirty_working_directory()
    {
        FakeCheckoutBranchHost host = new() { IsDirty = true };
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "dev") with { IsDirtyDir = null, ChangesMode = LocalChangesAction.Stash });

        viewModel.CheckoutCommand.Execute(null);

        host.Stashes.Should().Be(1);
        host.Checkouts[0].Stashed.Should().BeTrue();
        host.Checkouts[0].LocalChanges.Should().Be(LocalChangesAction.Stash);
    }

    [Test]
    public void Default_action_without_dialog_ignores_the_local_changes_unless_configured()
    {
        FakeCheckoutBranchHost host = new();
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "dev") with { IsDirtyDir = true, ChangesMode = LocalChangesAction.Merge, UseDefaultAction = false });

        viewModel.PerformCheckout(isVisible: false).Should().Be(CheckoutOutcome.Succeeded);

        host.Checkouts[0].LocalChanges.Should().Be(LocalChangesAction.DontChange);
    }

    [TestCase("", "Custom branch name is empty.")]
    [TestCase("bad..name", "is not valid branch name")]
    public void Remote_checkout_with_custom_name_validates_it(string name, string expectedError)
    {
        FakeCheckoutBranchHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "origin/main", remote: true) with { CreateLocalBranchForRemote = true }, messageBoxes);
        viewModel.CustomBranchName = name;

        viewModel.PerformCheckout(isVisible: true).Should().Be(CheckoutOutcome.Failed);

        messageBoxes.Errors.Should().ContainSingle().Which.Should().Contain(expectedError);
        host.Checkouts.Should().BeEmpty();
    }

    [Test]
    public void Remote_checkout_creates_a_normalised_custom_branch()
    {
        FakeCheckoutBranchHost host = new();
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "origin/main", remote: true) with { CreateLocalBranchForRemote = true });
        viewModel.CustomBranchName = "my main";

        viewModel.PerformCheckout(isVisible: true);

        host.Checkouts.Should().Equal(("origin/main", true, LocalChangesAction.DontChange, CheckoutNewBranchMode.Create, "my_main", false));
    }

    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public void Resetting_a_diverged_local_branch_is_confirmed(bool confirm, int expectedCheckouts)
    {
        FakeCheckoutBranchHost host = new() { MergeBase = BaseCommit };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new() { ConfirmResult = confirm };
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "origin/main", remote: true), messageBoxes);
        viewModel.IsResetBranch.Should().BeTrue();

        viewModel.PerformCheckout(isVisible: true);

        messageBoxes.Confirmations.Should().ContainSingle().Which.Should().Contain("“main”").And.Contain(BaseCommit.ToShortString());
        host.Checkouts.Should().HaveCount(expectedCheckouts);
        if (confirm)
        {
            host.Checkouts[0].Mode.Should().Be(CheckoutNewBranchMode.Reset);
            host.Checkouts[0].NewBranch.Should().Be("main");
        }
    }

    [Test]
    public void Fast_forward_reset_of_the_local_branch_is_not_confirmed()
    {
        FakeCheckoutBranchHost host = new() { MergeBase = LocalCommit };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "origin/main", remote: true), messageBoxes);

        viewModel.PerformCheckout(isVisible: true);

        messageBoxes.Confirmations.Should().BeEmpty();
        host.Checkouts.Should().ContainSingle();
    }

    [TestCase(CheckoutOutcome.Failed, null)]
    [TestCase(CheckoutOutcome.Cancelled, false)]
    public void Failed_checkout_keeps_the_dialog_open_and_cancelled_closes_it(CheckoutOutcome outcome, bool? expectedClosed)
    {
        FakeCheckoutBranchHost host = new() { Outcome = outcome };
        CheckoutBranchViewModel viewModel = Create(host, Options(branch: "dev"));
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.CheckoutCommand.Execute(null);

        closed.Should().Be(expectedClosed);
    }

    [Test]
    public void Remote_options_are_mutually_exclusive()
    {
        CheckoutBranchViewModel viewModel = Create(new FakeCheckoutBranchHost(), Options(branch: "origin/main", remote: true));

        viewModel.IsDontCreate = true;
        viewModel.IsResetBranch.Should().BeFalse();
        viewModel.IsCreateBranchWithCustomName.Should().BeFalse();

        viewModel.IsCreateBranchWithCustomName = true;
        viewModel.IsDontCreate.Should().BeFalse();
    }

    internal static CheckoutBranchOptions Options(string? branch, bool remote = false)
        => new(branch, remote, IsDirtyDir: false, LocalChangesAction.DontChange, CreateLocalBranchForRemote: false, AlwaysShowDialog: false, UseDefaultAction: false);

    internal static CheckoutBranchViewModel Create(ICheckoutBranchHost host, CheckoutBranchOptions options, ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null)
        => new(new CheckoutBranchStrings(), options, new FakeBranchNameNormaliser(), new GitBranchNameOptions("_"), autoNormalise: true, host, messageBoxes ?? new());

    internal sealed class FakeCheckoutBranchHost : ICheckoutBranchHost
    {
        public IReadOnlyList<string> LocalBranches { get; init; } = ["main", "dev"];

        public IReadOnlyList<string> RemoteBranches { get; init; } = ["origin/main", "origin/feature"];

        public bool HasContainFilter { get; init; }

        public bool IsDirty { get; init; }

        public ObjectId MergeBase { get; init; }

        public CheckoutOutcome Outcome { get; init; } = CheckoutOutcome.Succeeded;

        public List<(string Branch, bool Remote, LocalChangesAction LocalChanges, CheckoutNewBranchMode Mode, string? NewBranch, bool Stashed)> Checkouts { get; } = [];

        public LocalChangesAction? SavedDefault { get; private set; }

        public int Stashes { get; private set; }

        public IReadOnlyList<string> GetBranches(bool remote) => remote ? RemoteBranches : LocalBranches;

        public IReadOnlyList<string> GetRemoteNames() => ["origin"];

        public string? GetLocalTrackingBranchName(string remote, string remoteBranch) => remoteBranch[(remote.Length + 1)..];

        public ObjectId GetBranchObjectId(string branch, bool remote)
            => remote
                ? RemoteBranches.Contains(branch) ? RemoteCommit : default
                : LocalBranches.Contains(branch) ? LocalCommit : default;

        public ObjectId GetMergeBase(ObjectId a, ObjectId b) => MergeBase;

        public List<string> CommitCountRequests { get; } = [];

        public void RequestCommitCount(string branch, Action<string> report)
        {
            CommitCountRequests.Add(branch);
            report($"{branch}: 1 commit behind");
        }

        public bool IsValidBranchName(string branchName) => !branchName.Contains("..");

        public bool IsDirtyDir() => IsDirty;

        public void SaveDefaultLocalChangesAction(LocalChangesAction action) => SavedDefault = action;

        public void StashSave() => Stashes++;

        public CheckoutOutcome Checkout(string branch, bool remote, LocalChangesAction localChanges, CheckoutNewBranchMode newBranchMode, string? newBranchName, bool stashed)
        {
            Checkouts.Add((branch, remote, localChanges, newBranchMode, newBranchName, stashed));
            return Outcome;
        }
    }
}
