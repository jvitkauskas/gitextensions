using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of phase 2, batch 9: reflog and recent repositories settings.</summary>
[TestFixture]
public sealed class Batch9ViewModelTests
{
    internal const string ReflogOutput =
        "0123456789abcdef0123456789abcdef01234567 HEAD@{0}: commit: Add feature\n" +
        "89abcdef0123456789abcdef0123456789abcdef HEAD@{1}: checkout: moving from main to feature\n" +
        "not a reflog line\n" +
        "\n";

    internal static readonly RecentReposOptions Options = new(ShorteningRecentRepoPathStrategy.None, false, false, false, 3, 0, 30);

    [Test]
    public void Reflog_parses_the_output_of_git_reflog()
    {
        ReflogViewModel.Parse(ReflogOutput).Should().Equal(
            new ReflogEntry("0123456789abcdef0123456789abcdef01234567", "HEAD@{0}", "commit: Add feature"),
            new ReflogEntry("89abcdef0123456789abcdef0123456789abcdef", "HEAD@{1}", "checkout: moving from main to feature"));
    }

    [Test]
    public void Reflog_shows_HEAD_first_and_the_reflog_of_the_selected_reference()
    {
        FakeReflogHost host = new();
        ReflogViewModel viewModel = CreateReflog(host);

        host.Loaded.Should().Equal("HEAD");
        viewModel.Entries.Should().HaveCount(2);
        viewModel.CurrentBranchText.Should().Be("current branch (feature)");

        viewModel.ShowCurrentBranchCommand.Execute(null);
        viewModel.SelectedReference.Should().Be("feature");
        viewModel.ShowHeadCommand.Execute(null);
        host.Loaded.Should().Equal("HEAD", "feature", "HEAD");
    }

    [Test]
    public void Reflog_ignores_the_output_of_an_earlier_selection()
    {
        FakeReflogHost host = new() { ReportImmediately = false };
        ReflogViewModel viewModel = CreateReflog(host);

        viewModel.SelectedReference = "feature";
        host.Reports[0]("0123456789abcdef0123456789abcdef01234567 HEAD@{0}: commit: stale\n");
        viewModel.Entries.Should().BeEmpty();

        host.Reports[1](ReflogOutput);
        viewModel.Entries.Should().HaveCount(2);
    }

    [Test]
    public void Reflog_actions_apply_to_the_selected_entry()
    {
        FakeReflogHost host = new();
        ReflogViewModel viewModel = CreateReflog(host);
        viewModel.CreateBranchCommand.CanExecute(null).Should().BeFalse();

        viewModel.SelectedEntry = viewModel.Entries[1];
        viewModel.CopyShaCommand.Execute(null);
        viewModel.CreateBranchCommand.Execute(null);
        viewModel.ResetCurrentBranchCommand.Execute(null);

        host.Copied.Should().Equal("89abcdef0123456789abcdef0123456789abcdef");
        host.BranchesCreated.Should().Equal("89abcdef0123456789abcdef0123456789abcdef");
        host.Resets.Should().Equal(("89abcdef0123456789abcdef0123456789abcdef", false));
    }

    [Test]
    public void Reflog_confirms_a_reset_of_a_dirty_working_directory_and_resets_softly()
    {
        FakeReflogHost host = new();
        FakeMessageBoxes messageBoxes = new() { ConfirmResult = false };
        ReflogViewModel viewModel = CreateReflog(host, isDirty: true, messageBoxes: messageBoxes);
        viewModel.SelectedEntry = viewModel.Entries[0];

        viewModel.ResetCurrentBranchCommand.Execute(null);
        messageBoxes.Confirmations.Should().ContainSingle().Which.Should().StartWith("You have changes in your working directory");
        host.Resets.Should().BeEmpty();

        messageBoxes.ConfirmResult = true;
        viewModel.ResetCurrentBranchCommand.Execute(null);
        host.Resets.Should().Equal(("0123456789abcdef0123456789abcdef01234567", true));
    }

    [Test]
    public void Reflog_cannot_reset_a_detached_HEAD()
    {
        ReflogViewModel viewModel = CreateReflog(new FakeReflogHost(), isBranchCheckedOut: false);
        viewModel.SelectedEntry = viewModel.Entries[0];

        viewModel.ResetCurrentBranchCommand.CanExecute(null).Should().BeFalse();
        viewModel.CreateBranchCommand.CanExecute(null).Should().BeTrue();
    }

    [Test]
    public void RecentRepos_resplits_on_each_option_change()
    {
        FakeRecentReposHost host = new();
        RecentReposSettingsViewModel viewModel = new(new RecentReposSettingsStrings(), Options, host);
        host.Splits.Should().ContainSingle();

        viewModel.IsMiddleDots = true;
        viewModel.SortTopRepos = true;
        viewModel.MaxTopRepositories = 5;

        host.Splits.Select(o => o.ShorteningStrategy).Last().Should().Be(ShorteningRecentRepoPathStrategy.MiddleDots);
        host.Splits.Last().SortTopRepos.Should().BeTrue();
        host.Splits.Last().MaxTopRepositories.Should().Be(5);
        viewModel.IsDontShorten.Should().BeFalse();
        viewModel.TopRepos.Select(r => r.Path).Should().Equal(@"C:\a");
    }

    [TestCase(0, 10, RecentReposSettingsViewModel.MinComboWidthAllowed)]
    [TestCase(60, 20, 0)]
    [TestCase(60, 40, 40)]
    public void RecentRepos_combo_width_snaps_below_the_minimum(int from, int to, int expected)
    {
        RecentReposSettingsViewModel viewModel = new(new RecentReposSettingsStrings(), Options with { ComboMinWidth = from }, new FakeRecentReposHost());

        viewModel.ComboMinWidth = to;

        viewModel.ComboMinWidth.Should().Be(expected);
        viewModel.CaptionWidth.Should().Be(expected == 0 ? double.NaN : expected);
    }

    [Test]
    public void RecentRepos_anchors_removes_and_saves_the_selection()
    {
        FakeRecentReposHost host = new();
        RecentReposSettingsViewModel viewModel = new(new RecentReposSettingsStrings(), Options, host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        RecentRepoItem[] selection = [viewModel.RecentRepos[0]];

        viewModel.AnchorToTopCommand.CanExecute(selection).Should().BeTrue();
        viewModel.RemoveAnchorCommand.CanExecute(selection).Should().BeFalse("it is not anchored");
        viewModel.AnchorToTopCommand.CanExecute(Array.Empty<RecentRepoItem>()).Should().BeFalse();

        viewModel.AnchorToTopCommand.Execute(selection);
        viewModel.RemoveFromRecentCommand.Execute(selection);
        viewModel.OkCommand.Execute(null);

        host.Anchors.Should().Equal((@"C:\b", Repository.RepositoryAnchor.AnchoredInTop));
        host.Removed.Should().Equal(@"C:\b");
        host.Saved.Should().Be(Options);
        closed.Should().BeTrue();
    }

    private static ReflogViewModel CreateReflog(FakeReflogHost host, bool isDirty = false, bool isBranchCheckedOut = true, FakeMessageBoxes? messageBoxes = null)
        => new(new ReflogStrings(), ["HEAD", "feature", "main", "origin/main"], "feature", isBranchCheckedOut, isDirty, host, messageBoxes ?? new FakeMessageBoxes());

    internal sealed class FakeReflogHost : IReflogHost
    {
        public bool ReportImmediately { get; init; } = true;

        public List<string> Loaded { get; } = [];

        public List<Action<string>> Reports { get; } = [];

        public List<string> Copied { get; } = [];

        public List<string> BranchesCreated { get; } = [];

        public List<(string Sha, bool Soft)> Resets { get; } = [];

        public void LoadReflog(string reference, Action<string> report)
        {
            Loaded.Add(reference);
            Reports.Add(report);
            if (ReportImmediately)
            {
                report(ReflogOutput);
            }
        }

        public bool CreateBranch(string sha)
        {
            BranchesCreated.Add(sha);
            return true;
        }

        public bool ResetCurrentBranch(string sha, bool soft)
        {
            Resets.Add((sha, soft));
            return true;
        }

        public void CopyToClipboard(string text) => Copied.Add(text);
    }

    internal sealed class FakeRecentReposHost : IRecentReposSettingsHost
    {
        public List<RecentReposOptions> Splits { get; } = [];

        public List<(string Path, Repository.RepositoryAnchor Anchor)> Anchors { get; } = [];

        public List<string> Removed { get; } = [];

        public RecentReposOptions? Saved { get; private set; }

        public (IReadOnlyList<RecentRepoItem> Top, IReadOnlyList<RecentRepoItem> Recent) Split(RecentReposOptions options)
        {
            Splits.Add(options);
            return ([new RecentRepoItem(@"C:\a", "a", Repository.RepositoryAnchor.AnchoredInTop, true, true)],
                    [new RecentRepoItem(@"C:\b", "b", Repository.RepositoryAnchor.None, false, true), new RecentRepoItem(@"C:\gone", "gone", Repository.RepositoryAnchor.AnchoredInRecent, true, false)]);
        }

        public void SetAnchor(IEnumerable<string> paths, Repository.RepositoryAnchor anchor) => Anchors.AddRange(paths.Select(p => (p, anchor)));

        public void RemoveFromRecent(IEnumerable<string> paths) => Removed.AddRange(paths);

        public void Save(RecentReposOptions options) => Saved = options;
    }
}
