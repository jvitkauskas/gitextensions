using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The create branch and create tag dialogs of phase 2, batch 3, shown from their WinForms entry points with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartCreateBranchDialog_creates_and_checks_out_the_branch()
    {
        DriveDialogs(
            window =>
            {
                CreateBranchViewModel viewModel = (CreateBranchViewModel)window.DataContext!;
                viewModel.CommitPicker.SelectedObjectId.ToString().Should().Be(_referenceRepository.CommitHash);
                viewModel.BranchName = "new-branch";
                Capture(window, "create-branch");
                viewModel.CreateCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        _commands.StartCreateBranchDialog(_owner).Should().BeTrue();

        _referenceRepository.Module.GetSelectedBranch().Should().Be("new-branch");
    }

    [Test]
    public void StartCreateTagDialog_creates_an_annotated_tag()
    {
        DriveDialogs(
            window =>
            {
                CreateTagViewModel viewModel = (CreateTagViewModel)window.DataContext!;
                viewModel.TagName = "v2.0";
                viewModel.SelectedKindIndex = 1;
                viewModel.Message = "Release 2.0";
                Capture(window, "create-tag");
                viewModel.CreateCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        _commands.StartCreateTagDialog(_owner).Should().BeTrue();

        _referenceRepository.Module.GetRefs(RefsFilter.Tags).Select(r => r.LocalName).Should().Contain("v2.0");
    }
}
