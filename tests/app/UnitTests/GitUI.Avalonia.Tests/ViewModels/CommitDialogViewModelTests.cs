using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the commit dialogs (phase 2, batch 3).</summary>
[TestFixture]
public sealed class CommitDialogViewModelTests
{
    internal static readonly CommitSummary Summary = new("4b825dc6", "Alice", "2 days ago", "Fix the build", ["v1.0"], ["main", "release"]);

    internal static readonly RevisionInfo Commit = new("4b825dc642cb6eb9a060e54bf8d69288fbee4904", Summary, []);

    internal static readonly RevisionInfo Merge = new(
        "a1b2c3d4e5f60718293a4b5c6d7e8f9012345678",
        Summary with { Subject = "Merge branch 'feature'" },
        [new ParentCommit(1, "Fix the build", "Alice", "1/2/2026"), new ParentCommit(2, "Add feature", "Bob", "1/1/2026")]);

    [Test]
    public void CommitSummary_formats_the_commit()
    {
        CommitSummaryViewModel summary = new(new CommitSummaryStrings());

        summary.Title.Should().Be("No revision");
        summary.Author.Should().Be("---");
        summary.TagsText.Should().Be("---");
        summary.HasTags.Should().BeFalse();

        summary.Summary = Summary with { Tags = [], Branches = [.. Enumerable.Range(1, 20).Select(i => $"branch-{i}")] };

        summary.Title.Should().Be("4b825dc6");
        summary.Subject.Should().Be("Fix the build");
        summary.TagsText.Should().Be("n/a");
        summary.HasTags.Should().BeFalse();
        summary.HasBranches.Should().BeTrue();
        summary.BranchesText.Length.Should().BeLessThanOrEqualTo(75);
        summary.BranchesText.Should().StartWith("branch-1, branch-2");
    }

    [Test]
    public void CherryPick_of_a_merge_selects_the_first_parent_and_passes_it()
    {
        FakeCherryPickHost host = new();
        CherryPickViewModel viewModel = CreateCherryPick(Merge, host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.IsMerge.Should().BeTrue();
        viewModel.SelectedParent.Should().Be(Merge.Parents[0]);
        viewModel.SelectedParent = Merge.Parents[1];
        viewModel.AddReference = true;
        viewModel.CherryPickCommand.Execute(null);

        host.Picks.Should().Equal((Merge.Guid, true, 2, true));
        host.SavedOptions.Should().Be(new CherryPickOptions(AutoCommit: true, AddReference: true));
        closed.Should().BeTrue();
    }

    [Test]
    public void CherryPick_of_a_merge_requires_a_parent()
    {
        FakeCherryPickHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CherryPickViewModel viewModel = CreateCherryPick(Merge, host, messageBoxes);
        viewModel.SelectedParent = null;

        viewModel.CherryPickCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("None parent is selected!");
        host.Picks.Should().BeEmpty();
    }

    [Test]
    public void CherryPick_can_choose_another_commit()
    {
        FakeCherryPickHost host = new() { Chosen = Commit };
        CherryPickViewModel viewModel = CreateCherryPick(Merge, host);

        viewModel.ChooseRevisionCommand.Execute(null);

        host.ChooseRequests.Should().Equal(Merge.Guid);
        viewModel.Revision.Should().Be(Commit);
        viewModel.IsMerge.Should().BeFalse();
        viewModel.SelectedParent.Should().BeNull();
        viewModel.Summary.Subject.Should().Be("Fix the build");

        viewModel.CherryPickCommand.Execute(null);
        host.Picks.Should().Equal((Commit.Guid, true, 0, false));
    }

    [Test]
    public void CherryPick_without_a_commit_does_nothing()
    {
        FakeCherryPickHost host = new();
        CherryPickViewModel viewModel = CreateCherryPick(null, host);
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.CherryPickCommand.Execute(null);

        host.Picks.Should().BeEmpty();
        closed.Should().BeFalse();
    }

    [TestCase(false, 0)]
    [TestCase(true, 1)]
    public void Revert_passes_the_parent_of_a_merge(bool isMerge, int expectedParent)
    {
        FakeRevertHost host = new();
        RevertCommitViewModel viewModel = new(
            new RevertCommitStrings(), new CommitSummaryStrings(), isMerge ? Merge : Commit, host, new ProcessViewModelTests.FakeMessageBoxes(), "Error");
        viewModel.AutoCommit = true;

        viewModel.RevertCommand.Execute(null);

        host.Reverts.Should().Equal(((isMerge ? Merge : Commit).Guid, true, expectedParent));
    }

    [TestCase(ResetKind.Soft, false, 1, true)]
    [TestCase(ResetKind.Hard, true, 1, true)]
    [TestCase(ResetKind.Hard, false, 0, null)]
    public void ResetCurrentBranch_confirms_a_hard_reset(ResetKind kind, bool confirm, int expectedResets, bool? expectedClosed)
    {
        FakeResetCurrentBranchHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new() { ConfirmResult = confirm };
        ResetCurrentBranchViewModel viewModel = new(new ResetCurrentBranchStrings(), new CommitSummaryStrings(), "main", Summary, ResetKind.Mixed, host, messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.BranchInfo.Should().Be("Reset branch 'main' to revision:");
        viewModel.IsMixed.Should().BeTrue();
        viewModel.Kind = kind;
        viewModel.OkCommand.Execute(null);

        messageBoxes.Confirmations.Should().HaveCount(kind == ResetKind.Hard ? 1 : 0);
        host.Resets.Should().HaveCount(expectedResets);
        closed.Should().Be(expectedClosed);
    }

    [Test]
    public void ResetCurrentBranch_radio_properties_select_the_kind()
    {
        ResetCurrentBranchViewModel viewModel = new(
            new ResetCurrentBranchStrings(), new CommitSummaryStrings(), "main", Summary, ResetKind.Soft, new FakeResetCurrentBranchHost(), new ProcessViewModelTests.FakeMessageBoxes());

        viewModel.IsKeep = true;
        viewModel.Kind.Should().Be(ResetKind.Keep);
        viewModel.IsSoft.Should().BeFalse();

        viewModel.IsHard = false;
        viewModel.Kind.Should().Be(ResetKind.Keep, "unchecking a radio button does not change the kind");
    }

    [Test]
    public void ResetAnotherBranch_requires_a_known_branch_and_a_fast_forward_unless_forced()
    {
        FakeResetAnotherBranchHost host = new() { Ancestors = { "ff" } };
        ResetAnotherBranchViewModel viewModel = CreateResetAnother(host, defaultBranch: null);

        viewModel.IsBranchValid.Should().BeFalse();
        viewModel.IsBranchInvalid.Should().BeFalse("nothing was entered yet");
        viewModel.OkCommand.CanExecute(null).Should().BeFalse();

        viewModel.Branch = "unknown";
        viewModel.IsBranchInvalid.Should().BeTrue();
        viewModel.OkCommand.CanExecute(null).Should().BeFalse();

        viewModel.Branch = "ff";
        viewModel.IsNonFastForward.Should().BeFalse();
        viewModel.OkCommand.CanExecute(null).Should().BeTrue();

        viewModel.Branch = "diverged";
        viewModel.IsNonFastForward.Should().BeTrue();
        viewModel.OkCommand.CanExecute(null).Should().BeFalse();

        viewModel.ForceReset = true;
        viewModel.IsNonFastForward.Should().BeFalse();
        viewModel.OkCommand.CanExecute(null).Should().BeTrue();
    }

    [Test]
    public void ResetAnotherBranch_resets_and_saves_the_checkout_option()
    {
        FakeResetAnotherBranchHost host = new() { Ancestors = { "ff" } };
        ResetAnotherBranchViewModel viewModel = CreateResetAnother(host, defaultBranch: "ff");
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        host.SavedCheckout.Should().BeEmpty("the initial value is not saved again");
        viewModel.CheckoutAfterReset = false;
        host.SavedCheckout.Should().Equal(false);

        viewModel.OkCommand.Execute(null);

        host.Resets.Should().Equal(("ff", false));
        closed.Should().BeTrue();
    }

    [Test]
    public void Archive_filters_exclude_each_other()
    {
        ArchiveViewModel viewModel = CreateArchive(new FakeArchiveHost(), new SmallDialogViewModelTests.FakeFileDialogs(), diffRevision: Merge, path: null);

        viewModel.IsRevisionFilterEnabled.Should().BeTrue();
        viewModel.DiffSummary.Subject.Should().Be("Merge branch 'feature'");

        viewModel.IsPathFilterEnabled = true;
        viewModel.IsRevisionFilterEnabled.Should().BeFalse();

        viewModel.IsRevisionFilterEnabled = true;
        viewModel.IsPathFilterEnabled.Should().BeFalse();
    }

    [Test]
    public async Task Archive_saves_the_revision_with_the_chosen_paths()
    {
        FakeArchiveHost host = new();
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs = new() { SaveFile = @"C:\out\repo.zip" };
        ArchiveViewModel viewModel = CreateArchive(host, fileDialogs, diffRevision: null, path: "src/app.cs");
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        await viewModel.SaveCommand.ExecuteAsync(null);

        fileDialogs.SuggestedFileName.Should().Be($"repo_{Commit.Guid}_src/app_cs");
        host.Archives.Should().Equal(("zip", Commit.Guid, @"C:\out\repo.zip", "\"src/app.cs\""));
        closed.Should().BeTrue();
    }

    [Test]
    public async Task Archive_of_changed_files_requires_a_revision_to_compare_with()
    {
        FakeArchiveHost host = new() { ChangedFiles = ["a.txt", "b c.txt"] };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs = new() { SaveFile = @"C:\out\repo.tar" };
        ArchiveViewModel viewModel = CreateArchive(host, fileDialogs, diffRevision: null, path: null, messageBoxes);
        viewModel.IsTar = true;
        viewModel.IsRevisionFilterEnabled = true;

        await viewModel.SaveCommand.ExecuteAsync(null);
        messageBoxes.Errors.Should().Equal("You need to choose a target revision.");
        host.Archives.Should().BeEmpty();

        host.Chosen = Merge;
        viewModel.ChooseDiffRevisionCommand.Execute(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        host.Archives.Should().Equal(("tar", Commit.Guid, @"C:\out\repo.tar", "\"a.txt\" \"b c.txt\""));
    }

    [Test]
    public async Task Archive_cancelled_save_keeps_the_dialog_open()
    {
        FakeArchiveHost host = new();
        ArchiveViewModel viewModel = CreateArchive(host, new SmallDialogViewModelTests.FakeFileDialogs(), diffRevision: null, path: null);
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        await viewModel.SaveCommand.ExecuteAsync(null);

        host.Archives.Should().BeEmpty();
        closed.Should().BeFalse();
    }

    internal static CherryPickViewModel CreateCherryPick(RevisionInfo? revision, ICherryPickHost host, ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null)
        => new(new CherryPickStrings(), new CommitSummaryStrings(), revision, new CherryPickOptions(AutoCommit: true, AddReference: false), host, messageBoxes ?? new(), "Error");

    internal static ResetAnotherBranchViewModel CreateResetAnother(IResetAnotherBranchHost host, string? defaultBranch)
        => new(
            new ResetAnotherBranchStrings(),
            new CommitSummaryStrings(),
            ["ff", "diverged"],
            defaultBranch,
            Summary,
            checkoutAfterReset: true,
            host,
            new ProcessViewModelTests.FakeMessageBoxes(),
            "Error");

    internal static ArchiveViewModel CreateArchive(
        IArchiveHost host,
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs,
        RevisionInfo? diffRevision,
        string? path,
        ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null)
        => new(new ArchiveStrings(), new CommitSummaryStrings(), Commit, diffRevision, path, "repo", host, fileDialogs, messageBoxes ?? new(), "Error");

    internal sealed class FakeCherryPickHost : ICherryPickHost
    {
        public RevisionInfo? Chosen { get; init; }

        public List<string?> ChooseRequests { get; } = [];

        public List<(string Guid, bool AutoCommit, int Parent, bool AddReference)> Picks { get; } = [];

        public CherryPickOptions? SavedOptions { get; private set; }

        public RevisionInfo? ChooseRevision(string? currentGuid)
        {
            ChooseRequests.Add(currentGuid);
            return Chosen;
        }

        public void CherryPick(string guid, bool autoCommit, int parentNumber, bool addReference) => Picks.Add((guid, autoCommit, parentNumber, addReference));

        public void SaveOptions(CherryPickOptions options) => SavedOptions = options;
    }

    internal sealed class FakeRevertHost : IRevertCommitHost
    {
        public List<(string Guid, bool AutoCommit, int Parent)> Reverts { get; } = [];

        public void Revert(string guid, bool autoCommit, int parentNumber) => Reverts.Add((guid, autoCommit, parentNumber));
    }

    internal sealed class FakeResetCurrentBranchHost : IResetCurrentBranchHost
    {
        public List<ResetKind> Resets { get; } = [];

        public void Reset(ResetKind kind) => Resets.Add(kind);

        public void OpenHelp(ResetKind kind)
        {
        }
    }

    internal sealed class FakeResetAnotherBranchHost : IResetAnotherBranchHost
    {
        public HashSet<string> Ancestors { get; } = [];

        public List<(string Branch, bool Checkout)> Resets { get; } = [];

        public List<bool> SavedCheckout { get; } = [];

        public bool IsAncestor(string branch) => Ancestors.Contains(branch);

        public bool Reset(string branch, bool checkout)
        {
            Resets.Add((branch, checkout));
            return true;
        }

        public void SaveCheckoutAfterReset(bool value) => SavedCheckout.Add(value);
    }

    internal sealed class FakeArchiveHost : IArchiveHost
    {
        public RevisionInfo? Chosen { get; set; }

        public IReadOnlyList<string> ChangedFiles { get; init; } = [];

        public List<(string Format, string? Revision, string Output, string Paths)> Archives { get; } = [];

        public RevisionInfo? ChooseRevision(string? currentGuid) => Chosen;

        public IReadOnlyList<string> GetChangedFiles(string? fromGuid, string? toGuid) => ChangedFiles;

        public void Archive(string format, string? revisionGuid, string outputPath, string pathArguments) => Archives.Add((format, revisionGuid, outputPath, pathArguments));
    }
}
