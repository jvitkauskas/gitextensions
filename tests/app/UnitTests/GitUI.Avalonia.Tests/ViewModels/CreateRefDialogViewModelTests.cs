using GitCommands.Git;
using GitCommands.Git.Tag;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the create branch and create tag dialogs (phase 2, batch 3).</summary>
[TestFixture]
public sealed class CreateRefDialogViewModelTests
{
    internal static readonly ObjectId Commit = ObjectId.Parse("4b825dc642cb6eb9a060e54bf8d69288fbee4904");

    [Test]
    public void CommitPicker_selects_a_valid_commit_and_reports_the_commit_count()
    {
        FakeCommitPickerHost host = new() { Commits = { ["main"] = Commit } };
        CommitPickerViewModel picker = new(host, new ProcessViewModelTests.FakeMessageBoxes(), "Error");
        int changes = 0;
        picker.SelectedObjectIdChanged += (_, _) => changes++;

        picker.Text = " main ";
        picker.CommitText();

        picker.SelectedObjectId.Should().Be(Commit);
        picker.Text.Should().Be(Commit.ToShortString());
        picker.CommitCountText.Should().Be("2 commits behind");
        changes.Should().Be(1);
    }

    [Test]
    public void CommitPicker_discards_an_invalid_commit()
    {
        FakeCommitPickerHost host = new() { Commits = { ["main"] = Commit } };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CommitPickerViewModel picker = new(host, messageBoxes, "Error");
        picker.SetSelectedCommitHash("main");

        picker.Text = "nonsense";
        picker.CommitText();

        messageBoxes.Errors.Should().ContainSingle().Which.Should().Contain("not valid");
        picker.SelectedObjectId.Should().Be(Commit, "the previous commit is kept");
        picker.Text.Should().Be(Commit.ToShortString());
    }

    [Test]
    public void CommitPicker_chooses_a_commit()
    {
        FakeCommitPickerHost host = new() { Commits = { [Commit.ToString()] = Commit }, Chosen = Commit.ToString() };
        CommitPickerViewModel picker = new(host, new ProcessViewModelTests.FakeMessageBoxes(), "Error");

        picker.ChooseCommitCommand.Execute(null);

        host.ChooseRequests.Should().Equal((ObjectId?)null);
        picker.SelectedObjectId.Should().Be(Commit);
    }

    [Test]
    public void CreateBranch_creates_a_normalised_branch_at_the_commit()
    {
        FakeCreateBranchHost host = new();
        CreateBranchViewModel viewModel = CreateBranch(host, new CreateBranchOptions("feature x"));
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Summary.Title.Should().Be("4b825dc6", "selecting the commit shows its summary");
        viewModel.CreateCommand.Execute(null);

        viewModel.BranchName.Should().Be("feature_x");
        host.Created.Should().Equal(("feature_x", Commit, true, false, true));
        closed.Should().BeTrue();
    }

    [TestCase("", "Enter branch name.")]
    [TestCase("bad..name", "“bad..name” is not valid branch name.")]
    public void CreateBranch_validates_the_name(string name, string expectedError)
    {
        FakeCreateBranchHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CreateBranchViewModel viewModel = CreateBranch(host, new CreateBranchOptions(name), messageBoxes);

        viewModel.CreateCommand.Execute(null);

        messageBoxes.Errors.Should().Equal(expectedError);
        host.Created.Should().BeEmpty();
    }

    [Test]
    public void CreateBranch_requires_a_commit_unless_orphan()
    {
        FakeCreateBranchHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CreateBranchViewModel viewModel = CreateBranch(host, new CreateBranchOptions("new"), messageBoxes, selectCommit: false);

        viewModel.CreateCommand.Execute(null);
        messageBoxes.Errors.Should().Equal("Select 1 revision to create the branch on.");

        viewModel.CreateOrphan = true;
        viewModel.CheckoutAfterCreate.Should().BeTrue();
        viewModel.CanChangeCheckout.Should().BeFalse();
        viewModel.CommitPicker.IsEnabled.Should().BeFalse();
        viewModel.CreateCommand.Execute(null);
        host.Created.Should().Equal(("new", default(ObjectId), true, true, true));

        viewModel.CreateOrphan = false;
        viewModel.CommitPicker.IsEnabled.Should().BeTrue();
    }

    [Test]
    public void CreateBranch_in_a_repository_without_commits_creates_an_orphan()
    {
        CreateBranchViewModel viewModel = CreateBranch(new FakeCreateBranchHost(), new CreateBranchOptions(null, IsOrphanOnly: true), selectCommit: false);

        viewModel.CreateBranchCaption.Should().Be("Creating orphan branch (repository has no commits)");
        viewModel.CreateOrphan.Should().BeTrue();
        viewModel.CanChangeCreateOrphan.Should().BeFalse();
        viewModel.ClearOrphan.Should().BeFalse();
        viewModel.CanChangeClearOrphan.Should().BeTrue();
    }

    [Test]
    public void CreateBranch_options_of_the_reflog()
    {
        CreateBranchViewModel viewModel = CreateBranch(
            new FakeCreateBranchHost(), new CreateBranchOptions(null, CheckoutAfterCreation: false, UserAbleToChangeRevision: false, CouldBeOrphan: false));

        viewModel.CheckoutAfterCreate.Should().BeFalse();
        viewModel.CommitPicker.IsEnabled.Should().BeFalse();
        viewModel.CanCreateOrphan.Should().BeFalse();
    }

    [Test]
    public void CreateTag_enables_the_inputs_of_the_kind()
    {
        CreateTagViewModel viewModel = CreateTag(new FakeCreateTagHost());

        viewModel.Kinds.Should().Equal("Lightweight tag", "Annotated tag", "Sign with default GPG", "Sign with specific GPG");
        viewModel.PushToText.Should().Be("Push tag to 'upstream'");
        viewModel.CanEnterMessage.Should().BeFalse();
        viewModel.CanEnterKeyId.Should().BeFalse();

        viewModel.SelectedKindIndex = 1;
        viewModel.CanEnterMessage.Should().BeTrue();

        viewModel.SelectedKindIndex = 3;
        viewModel.CanEnterKeyId.Should().BeTrue();
        viewModel.Operation.Should().Be(TagOperation.SignWithSpecificKey);
    }

    [TestCase(false, 0)]
    [TestCase(true, 1)]
    public void CreateTag_creates_and_optionally_pushes_the_tag(bool push, int expectedPushes)
    {
        FakeCreateTagHost host = new();
        CreateTagViewModel viewModel = CreateTag(host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.TagName = "v1.0";
        viewModel.SelectedKindIndex = 1;
        viewModel.Message = "Release";
        viewModel.Force = true;
        viewModel.PushTag = push;

        viewModel.CreateCommand.Execute(null);

        host.Created.Should().ContainSingle().Which.Should().BeEquivalentTo(new GitCreateTagArgs("v1.0", Commit, TagOperation.Annotate, "Release", "", true));
        host.Pushes.Should().HaveCount(expectedPushes);
        if (push)
        {
            host.Pushes[0].Should().Be(("upstream", "v1.0"));
        }

        closed.Should().BeTrue();
    }

    [Test]
    public void CreateTag_reports_errors()
    {
        FakeCreateTagHost host = new() { Exception = new InvalidOperationException("git failed") };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CreateTagViewModel viewModel = CreateTag(host, messageBoxes);
        viewModel.TagName = "v1.0";

        viewModel.CreateCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("git failed");
    }

    internal static CreateBranchViewModel CreateBranch(
        ICreateBranchHost host,
        CreateBranchOptions options,
        ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null,
        bool selectCommit = true)
    {
        messageBoxes ??= new();
        CommitPickerViewModel picker = new(new FakeCommitPickerHost { Commits = { [Commit.ToString()] = Commit } }, messageBoxes, "Error");
        CreateBranchViewModel viewModel = new(
            new CreateBranchStrings(),
            new CommitSummaryStrings(),
            picker,
            options,
            new FakeBranchNameNormaliser(),
            new GitBranchNameOptions("_"),
            autoNormalise: true,
            host,
            messageBoxes);
        if (selectCommit)
        {
            picker.SetSelectedCommitHash(Commit.ToString());
        }

        return viewModel;
    }

    internal static CreateTagViewModel CreateTag(ICreateTagHost host, ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null)
    {
        messageBoxes ??= new();
        CommitPickerViewModel picker = new(new FakeCommitPickerHost { Commits = { [Commit.ToString()] = Commit } }, messageBoxes, "Error");
        picker.SetSelectedCommitHash(Commit.ToString());
        return new CreateTagViewModel(new CreateTagStrings(), picker, "upstream", host, messageBoxes, "Error");
    }

    internal sealed class FakeCommitPickerHost : ICommitPickerHost
    {
        public Dictionary<string, ObjectId> Commits { get; } = [];

        public string? Chosen { get; init; }

        public List<ObjectId?> ChooseRequests { get; } = [];

        public ObjectId RevParse(string? revision) => revision is not null && Commits.TryGetValue(revision, out ObjectId id) ? id : default;

        public void RequestCommitCount(ObjectId selected, Action<string> report) => report("2 commits behind");

        public string? ChooseCommit(ObjectId? current)
        {
            ChooseRequests.Add(current);
            return Chosen;
        }
    }

    internal sealed class FakeCreateBranchHost : ICreateBranchHost
    {
        public List<(string Name, ObjectId ObjectId, bool Checkout, bool Orphan, bool ClearOrphan)> Created { get; } = [];

        public CommitSummary GetSummary(ObjectId objectId) => CommitDialogViewModelTests.Summary;

        public bool IsValidBranchName(string branchName) => !branchName.Contains("..");

        public bool CreateBranch(string branchName, ObjectId objectId, bool checkout, bool orphan, bool clearOrphan)
        {
            Created.Add((branchName, objectId, checkout, orphan, clearOrphan));
            return true;
        }
    }

    internal sealed class FakeCreateTagHost : ICreateTagHost
    {
        public Exception? Exception { get; init; }

        public List<GitCreateTagArgs> Created { get; } = [];

        public List<(string Remote, string Tag)> Pushes { get; } = [];

        public bool CreateTag(GitCreateTagArgs args)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            Created.Add(args);
            return true;
        }

        public void PushTag(string remote, string tagName) => Pushes.Add((remote, tagName));
    }
}
