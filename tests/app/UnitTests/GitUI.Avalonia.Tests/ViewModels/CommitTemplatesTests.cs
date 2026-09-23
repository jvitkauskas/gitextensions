using GitCommands;
using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>Tests of the templates menu of the commit dialog (its Conventional Commits cases are those of <c>FormCommitTests</c>).</summary>
[TestFixture]
public sealed class CommitTemplatesTests
{
    [TestCase("", 0, false, "feat: ", 6)]
    [TestCase("text", 3, false, "feat: text", 9)]
    [TestCase("", 0, true, "feat(): ", 5)]
    [TestCase("text", 3, true, "feat(): text", 5)]
    [TestCase("fix: ", 0, false, "feat: ", 6)]
    [TestCase("fix: text", 3, false, "feat: text", 6)]
    [TestCase("fix: ", 0, true, "feat(): ", 5)]
    [TestCase("fix: text", 3, true, "feat(): text", 5)]
    [TestCase("fix(scope): ", 0, true, "feat(scope): ", 13)]
    [TestCase("fix(scope): text", 14, true, "feat(scope): text", 15)]
    public void The_conventional_commit_type_is_prefixed_or_replaced(string message, int caret, bool insertScope, string expectedTitle, int expectedPosition)
    {
        (string title, int selectionStart) = ConventionalCommits.PrefixOrReplaceKeyword(message, caret, "feat", insertScope);

        title.Should().Be(expectedTitle);
        selectionStart.Should().Be(expectedPosition);
    }

    [Test]
    public async Task A_type_replaces_the_first_line_and_moves_the_caret()
    {
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(new CommitViewModelTests.FakeHost());
        await viewModel.InitializeAsync();
        viewModel.Message.Text = "fix: text\n\nbody";
        viewModel.Message.CaretOffset = 3;

        viewModel.ApplyConventionalType("feat");

        viewModel.Message.Text.Should().Be("feat: text\n\nbody");
        viewModel.Message.CaretOffset.Should().Be(6);
    }

    [Test]
    public async Task Footers_are_added_as_last_lines()
    {
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(new CommitViewModelTests.FakeHost());
        await viewModel.InitializeAsync();
        viewModel.Message.Text = "feat: text";
        viewModel.Message.CaretOffset = 2;

        viewModel.AddConventionalFooter("Reviewed-by: ");
        viewModel.Message.Text.Should().Be($"feat: text{Environment.NewLine}Reviewed-by: ");
        viewModel.Message.CaretOffset.Should().Be(viewModel.Message.Text.Length);

        viewModel.AddConventionalFooter(ConventionalCommits.SkipCi, keepCursorPosition: true);
        viewModel.Message.Text.Should().EndWith($"{Environment.NewLine}[skip ci]");
        viewModel.Message.CaretOffset.Should().Be(viewModel.Message.Text.Length - "[skip ci]".Length - Environment.NewLine.Length);
    }

    [Test]
    public async Task Templates_replace_the_message_with_the_matches_of_the_branch()
    {
        CommitViewModelTests.FakeHost host = new()
        {
            Branch = "feature/JIRA-42-login",
            Templates = ([new CommitTemplateItem("Plugin", "From a plugin", icon: null, isRegex: false)],
                [new CommitTemplateItem("Ticket", "{{JIRA-(\\d+)}}: ", icon: null, isRegex: true), new CommitTemplateItem("", "unnamed", icon: null, isRegex: false)]),
        };
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(host);
        await viewModel.InitializeAsync();

        (IReadOnlyList<CommitTemplateItem> registered, IReadOnlyList<CommitTemplateItem> fromSettings) = viewModel.GetCommitTemplates();
        registered.Select(t => t.Name).Should().Equal("Plugin");
        fromSettings.Select(t => t.Name).Should().Equal("Ticket");

        viewModel.ApplyTemplate(fromSettings[0]);
        viewModel.Message.Text.Should().Be("42: ");
        viewModel.ApplyTemplate(new CommitTemplateItem("Literal", "{{JIRA-(\\d+)}}", icon: null, isRegex: false));
        viewModel.Message.Text.Should().Be("{{JIRA-(\\d+)}}", "not a regex template");
        viewModel.ApplyTemplate(new CommitTemplateItem("Other group", "{{(feature)/(\\w+)}}[2]", icon: null, isRegex: true));
        viewModel.Message.Text.Should().Be("JIRA");
    }
}
