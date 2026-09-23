using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the revision filter dialog (phase 2, batch 7).</summary>
[TestFixture]
public sealed class RevisionFilterViewModelTests
{
    internal static readonly RevisionFilterValues Empty = new(
        false, DateTime.MinValue, false, DateTime.MinValue, false, "", false, "", false, "", false, "", true, false, 0,
        false, "", false, "", false, false, false, false, false, false, false);

    [Test]
    public void Unset_dates_default_to_today()
    {
        RevisionFilterViewModel viewModel = new(new RevisionFilterStrings(), Empty, 100_000);

        viewModel.DateFrom.Should().Be(DateTime.Today);
        viewModel.DateTo.Should().Be(DateTime.Today);
    }

    [Test]
    public void Option_dependencies_follow_the_WinForms_form()
    {
        RevisionFilterViewModel viewModel = new(new RevisionFilterStrings(), Empty, 100_000);

        viewModel.CanIgnoreCase.Should().BeFalse();
        viewModel.ByMessage = true;
        viewModel.CanIgnoreCase.Should().BeTrue();

        viewModel.CanFilterBranches.Should().BeTrue();
        viewModel.ShowCurrentBranchOnly = true;
        viewModel.CanFilterBranches.Should().BeFalse();

        viewModel.ShowCurrentBranchOnly = false;
        viewModel.ShowReflogReferences = true;
        viewModel.CanShowCurrentBranchOnly.Should().BeFalse();
        viewModel.CanFilterBranches.Should().BeFalse();
    }

    [Test]
    public void Turning_the_limit_off_shows_the_default_limit()
    {
        RevisionFilterViewModel viewModel = new(new RevisionFilterStrings(), Empty with { ByCommitsLimit = true, CommitsLimit = 500 }, 100_000);

        viewModel.CommitsLimit.Should().Be(500);
        viewModel.ByCommitsLimit = false;

        viewModel.CommitsLimit.Should().Be(100_000);
    }

    [Test]
    public void Ok_returns_the_trimmed_filter()
    {
        DateTime since = new(2026, 1, 2);
        RevisionFilterViewModel viewModel = new(new RevisionFilterStrings(), Empty, 100_000);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.ByAuthor = true;
        viewModel.Author = "  Alice ";
        viewModel.ByDateFrom = true;
        viewModel.DateFrom = since;
        viewModel.ShowFullHistory = true;
        viewModel.ShowSimplifyMerges = true;

        viewModel.OkCommand.Execute(null);

        closed.Should().BeTrue();
        viewModel.Result.Should().Be(Empty with
        {
            ByAuthor = true,
            Author = "Alice",
            ByDateFrom = true,
            DateFrom = since,
            DateTo = DateTime.Today,
            ShowFullHistory = true,
            ShowSimplifyMerges = true,
        });
    }
}
