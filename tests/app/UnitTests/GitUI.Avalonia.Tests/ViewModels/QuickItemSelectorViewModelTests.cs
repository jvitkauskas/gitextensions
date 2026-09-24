using GitExtensions.Extensibility.Git;
using GitUI.Presentation.UserControls.RevisionGrid;
using NSubstitute;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the quick picker of the revision grid (port of <c>FormQuickItemSelector</c>).</summary>
[TestFixture]
public sealed class QuickItemSelectorViewModelTests
{
    [Test]
    public void ForRefs_groups_the_branches_and_tags_under_headers_and_selects_the_first_ref()
    {
        IGitRef tag = Ref("v1.0", tag: true);
        IGitRef remote = Ref("origin/main", remote: true);
        IGitRef feature = Ref("feature");
        IGitRef main = Ref("main");

        QuickItemSelectorViewModel viewModel = QuickItemSelectorViewModel.ForRefs(new QuickItemSelectorStrings(), QuickRefAction.Delete, [tag, remote, main, feature]);

        viewModel.ActionText.Should().Be("Delete");
        viewModel.Items.Select(i => i.Label).Should().Equal(
            "local ―――――――――――――――――"[..18],
            "feature",
            "main",
            "remote ――――――――――――――――"[..18],
            "origin/main",
            "tag ―――――――――――――――――――"[..18],
            "v1.0");
        viewModel.Items.Where(i => i.IsHeader).Should().HaveCount(3);
        viewModel.Items[1].Item.Should().BeSameAs(feature);
        viewModel.SelectedItem.Should().BeSameAs(viewModel.Items[1]);
    }

    [Test]
    public void A_header_cannot_be_accepted()
    {
        QuickItemSelectorViewModel viewModel = QuickItemSelectorViewModel.ForRefs(new QuickItemSelectorStrings(), QuickRefAction.Rename, [Ref("main")]);
        viewModel.ActionText.Should().Be("Rename");

        viewModel.SelectedItem = viewModel.Items[0];
        viewModel.AcceptCommand.CanExecute(null).Should().BeFalse();

        viewModel.SelectedItem = viewModel.Items[1];
        bool? accepted = null;
        viewModel.CloseRequested += (_, result) => accepted = result;
        viewModel.AcceptCommand.Execute(null);

        accepted.Should().BeTrue();
        viewModel.SelectedValue.Should().BeSameAs(viewModel.Items[1].Item);
    }

    [Test]
    public void ForStrings_sorts_the_strings_and_selects_the_first()
    {
        QuickItemSelectorViewModel viewModel = QuickItemSelectorViewModel.ForStrings(new QuickItemSelectorStrings(), ["upstream", "origin"]);

        viewModel.ActionText.Should().Be("Select");
        viewModel.Items.Select(i => i.Label).Should().Equal("origin", "upstream");
        viewModel.SelectedItem!.Item.Should().Be("origin");
        viewModel.SelectedValue.Should().BeNull("nothing is chosen until accepted");
    }

    [Test]
    public void Without_refs_there_is_nothing_to_select()
    {
        QuickItemSelectorViewModel viewModel = QuickItemSelectorViewModel.ForRefs(new QuickItemSelectorStrings(), QuickRefAction.Select, []);

        viewModel.Items.Should().BeEmpty();
        viewModel.SelectedItem.Should().BeNull();
        viewModel.AcceptCommand.CanExecute(null).Should().BeFalse();
    }

    internal static IGitRef Ref(string name, bool remote = false, bool tag = false)
    {
        IGitRef gitRef = Substitute.For<IGitRef>();
        gitRef.Name.Returns(name);
        gitRef.IsHead.Returns(!remote && !tag);
        gitRef.IsRemote.Returns(remote);
        gitRef.IsTag.Returns(tag);
        return gitRef;
    }
}
