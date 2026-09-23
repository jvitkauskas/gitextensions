using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.UserControls.RevisionGrid;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The revision filter dialog of phase 2, batch 7, with a real <see cref="FilterInfo"/>.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Revision_filter_dialog_updates_the_filter()
    {
        FilterInfo filterInfo = new();

        DriveNextDialog(window =>
        {
            RevisionFilterViewModel viewModel = (RevisionFilterViewModel)window.DataContext!;
            viewModel.ByAuthor = true;
            viewModel.Author = " Alice ";
            viewModel.ShowOnlyFirstParent = true;
            Capture(window, "revision-filter");
            viewModel.OkCommand.Execute(null);
        });

        AvaloniaDialogs.TryShowRevisionFilter(_owner, filterInfo, out bool accepted).Should().BeTrue();

        accepted.Should().BeTrue();
        filterInfo.ByAuthor.Should().BeTrue();
        filterInfo.Author.Should().Be("Alice");
        filterInfo.ShowOnlyFirstParent.Should().BeTrue();
    }
}
