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

    [Test]
    public void The_commit_info_is_copied_as_text()
    {
        FakeHost host = new();
        CommitInfoViewModel viewModel = new(host);
        viewModel.SetRevision(Revision);

        viewModel.CopyCommitInfo();

        host.Copied.Should().ContainSingle().Which.Should().StartWith("Author:\tAlice <alice@example.org>\nDate:\t2 days ago").And.EndWith("\n\nFix the bug\n\nSee #42 for the details.");

        viewModel.CopyLink("https://example.org/issues/42");
        host.Copied[^1].Should().Be("https://example.org/issues/42");
    }

    [Test]
    public void The_menu_settings_are_saved_and_reload_the_refs()
    {
        FakeHost host = new();
        CommitInfoViewModel viewModel = new(host);
        viewModel.SetRevision(Revision);
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.SetOptions(viewModel.Options with { ShowContainedInTags = false });

        host.Options.ShowContainedInTags.Should().BeFalse();
        changed.Should().Contain([nameof(CommitInfoViewModel.Options), nameof(CommitInfoViewModel.RevisionInfo)]);
    }

    [Test]
    public void Notes_are_edited_then_reloaded()
    {
        FakeHost host = new();
        GitRevision revision = new(Revision.ObjectId) { Subject = "Fix the bug", Body = "Fix the bug", Notes = "old notes" };
        CommitInfoViewModel viewModel = new(host);
        viewModel.SetRevision(revision);

        viewModel.AddNotes();

        host.EditedNotes.Should().Equal(revision.ObjectId);
        revision.Notes.Should().BeNull("reloaded with the message");
    }

    [Test]
    public void The_avatar_is_loaded_if_shown()
    {
        FakeHost host = new();
        CommitInfoViewModel viewModel = new(host);
        viewModel.SetRevision(Revision);
        viewModel.ShowAvatar.Should().BeFalse();
        viewModel.Avatar.Should().BeNull();

        host.ShowAvatar = true;
        viewModel.SetRevision(Revision);

        viewModel.ShowAvatar.Should().BeTrue();
        viewModel.Avatar.Should().Equal(FakeHost.AvatarImage);
        viewModel.AvatarSize.Should().Be(80);
    }

    [Test]
    public void Xhtml_is_converted_to_text()
    {
        XhtmlText.ToPlainText("Author: <a href='mailto:a@b'>A &lt;a@b&gt;</a><br/>Line<p>para</p>").Should().Be("Author: A <a@b>\nLine\npara");
        XhtmlText.ToPlainText("a <b>b & c").Should().Be("a b & c", "malformed");
    }
}
