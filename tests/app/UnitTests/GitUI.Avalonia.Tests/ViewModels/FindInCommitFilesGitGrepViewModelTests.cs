using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the git grep prompt of the file status list (port of <c>FormFindInCommitFilesGitGrep</c>).</summary>
[TestFixture]
public sealed class FindInCommitFilesGitGrepViewModelTests
{
    [Test]
    public void The_options_are_read_from_and_written_to_the_settings()
    {
        FakeHost host = new() { UserArguments = "--no-index", IgnoreCase = true, MatchWholeWord = true, ShowSearchBox = true };
        FindInCommitFilesGitGrepViewModel viewModel = new(new FindInCommitFilesGitGrepStrings(), host);

        viewModel.UserArguments.Should().Be("--no-index");
        viewModel.MatchCase.Should().BeFalse();
        viewModel.MatchWholeWord.Should().BeTrue();
        viewModel.ShowSearchBox.Should().BeTrue();
        host.Actions.Should().BeEmpty();

        viewModel.UserArguments = "-P";
        viewModel.MatchCase = true;
        viewModel.MatchWholeWord = false;

        host.UserArguments.Should().Be("-P");
        host.IgnoreCase.Should().BeFalse();
        host.MatchWholeWord.Should().BeFalse();
    }

    [Test]
    public void SetState_shows_the_expression_and_the_previous_ones_without_toggling_the_search_box()
    {
        FakeHost host = new() { ShowSearchBox = true };
        FindInCommitFilesGitGrepViewModel viewModel = new(new FindInCommitFilesGitGrepStrings(), host);
        int focusRequests = 0;
        viewModel.FocusExpressionRequested += (_, _) => focusRequests++;

        viewModel.SetState("TODO", ["TODO", "FIXME"], showSearchBox: false);

        viewModel.Expression.Should().Be("TODO");
        viewModel.SearchItems.Should().Equal("TODO", "FIXME");
        viewModel.ShowSearchBox.Should().BeFalse();
        host.ShowSearchBox.Should().BeFalse("as SetShowFindInCommitFilesGitGrep, the setting follows the box of the list");
        host.Actions.Should().BeEmpty();
        focusRequests.Should().Be(1);

        viewModel.SetState(null, [], showSearchBox: false);
        viewModel.Expression.Should().Be("TODO", "the expression is kept without a new one");
    }

    [Test]
    public void Find_searches_and_the_show_option_toggles_the_search_box()
    {
        FakeHost host = new();
        FindInCommitFilesGitGrepViewModel viewModel = new(new FindInCommitFilesGitGrepStrings(), host);
        viewModel.Expression = "TODO";

        viewModel.SearchCommand.Execute(null);
        viewModel.ShowSearchBox = true;

        host.Actions.Should().Equal("search TODO", "box True");
        host.ShowSearchBox.Should().BeTrue();
    }

    [TestCase("TODO", true, "")]
    [TestCase("", true, "search ")]
    [TestCase("TODO", false, "search ")]
    public void Closing_ends_the_search_if_the_box_is_hidden_or_the_expression_empty(string expression, bool showSearchBox, string expected)
    {
        FakeHost host = new() { ShowSearchBox = showSearchBox };
        FindInCommitFilesGitGrepViewModel viewModel = new(new FindInCommitFilesGitGrepStrings(), host) { Expression = expression };

        viewModel.CanClose().Should().BeTrue();

        string.Join(",", host.Actions).Should().Be(expected);
    }

    internal sealed class FakeHost : IFindInCommitFilesGitGrepHost
    {
        public List<string> Actions { get; } = [];

        public string UserArguments { get; set; } = "";

        public bool IgnoreCase { get; set; }

        public bool MatchWholeWord { get; set; }

        public bool ShowSearchBox { get; set; }

        public void Search(string expression) => Actions.Add($"search {expression}");

        public void SetSearchBoxVisible(bool visible) => Actions.Add($"box {visible}");
    }
}
