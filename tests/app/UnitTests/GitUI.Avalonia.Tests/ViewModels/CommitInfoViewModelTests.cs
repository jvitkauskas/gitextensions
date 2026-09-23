using GitExtensions.Extensibility.Git;
using GitUI.Presentation.UserControls;
using GitUIPluginInterfaces;
using FakeHost = GitUI.AvaloniaTests.Views.CommitInfoViewTests.FakeHost;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the commit info (port of <c>CommitInfo</c>).</summary>
[TestFixture]
public sealed class CommitInfoViewModelTests
{
    private static readonly GitRevision Revision = new(ObjectId.Parse("c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3")) { Subject = "Fix the bug" };

    [Test]
    public void Shows_the_header_then_loads_the_message_and_the_refs()
    {
        CommitInfoViewModel viewModel = new(new FakeHost());

        viewModel.SetRevision(Revision);

        viewModel.HasRevision.Should().BeTrue();
        viewModel.Header.Select(l => l.Label).Should().Equal("Author:", "Date:", "Commit hash:", "Parent:");
        viewModel.Header[3].ValueXhtml.Should().Be("a1a1a1a1", "commit hashes are links only if their command is handled");
        viewModel.Message.Should().Contain("#42");
        viewModel.RevisionInfo.Should().StartWith("Contained in branches:");

        viewModel.SetRevision(null);
        viewModel.HasRevision.Should().BeFalse();
        viewModel.Message.Should().BeEmpty();
    }

    [Test]
    public void Artificial_commits_have_no_refs()
    {
        CommitInfoViewModel viewModel = new(new FakeHost());

        viewModel.SetRevision(new GitRevision(ObjectId.WorkTreeId));

        viewModel.Message.Should().Be("Fix the bug", "the fixed message");
        viewModel.RevisionInfo.Should().BeEmpty();
    }

    [Test]
    public void Links_show_all_branches_or_raise_internal_commands()
    {
        FakeHost host = new();
        CommitInfoViewModel viewModel = new(host);
        List<(string, string?)> commands = [];
        viewModel.CommandClicked += (_, command) => commands.Add(command);
        viewModel.SetRevision(Revision);
        viewModel.Header[3].ValueXhtml.Should().Contain("gotocommit", "the command is handled");

        viewModel.OnLinkClicked("gitext://showall/branches");
        viewModel.RevisionInfo.Should().Contain(", feature");

        viewModel.OnLinkClicked("gitext://gotocommit/a1a1a1a1");
        commands.Should().Equal(("gotocommit", "a1a1a1a1"));
    }
}
