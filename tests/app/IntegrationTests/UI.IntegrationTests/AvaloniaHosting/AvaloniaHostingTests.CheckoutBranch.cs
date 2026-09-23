using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The checkout branch dialog of phase 2, batch 5, shown from its WinForms entry point with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartCheckoutBranch_shows_the_dialog_and_checks_out()
    {
        bool alwaysShow = AppSettings.AlwaysShowCheckoutBranchDlg;
        AppSettings.AlwaysShowCheckoutBranchDlg = true;
        try
        {
            _referenceRepository.CreateBranch("feature", _referenceRepository.CommitHash!);

            DriveDialogs(
                window =>
                {
                    CheckoutBranchViewModel viewModel = (CheckoutBranchViewModel)window.DataContext!;
                    viewModel.Branches.Should().Contain("feature");
                    viewModel.Branch = "feature";
                    Capture(window, "checkout-branch");
                    viewModel.CheckoutCommand.Execute(null);
                },
                AcknowledgeWhenDone);

            CreateCommandsWithPassingScripts().StartCheckoutBranch(_owner, "").Should().BeTrue();

            _referenceRepository.Module.GetSelectedBranch().Should().Be("feature");
        }
        finally
        {
            AppSettings.AlwaysShowCheckoutBranchDlg = alwaysShow;
        }
    }

    [Test]
    public void StartCheckoutBranch_checks_out_a_local_branch_without_the_dialog()
    {
        bool alwaysShow = AppSettings.AlwaysShowCheckoutBranchDlg;
        bool checkForChanges = AppSettings.CheckForUncommittedChangesInCheckoutBranch;
        AppSettings.AlwaysShowCheckoutBranchDlg = false;
        AppSettings.CheckForUncommittedChangesInCheckoutBranch = true;
        try
        {
            _referenceRepository.CreateBranch("feature", _referenceRepository.CommitHash!);

            // Only the progress dialog opens.
            DriveDialogs(AcknowledgeWhenDone);

            CreateCommandsWithPassingScripts().StartCheckoutBranch(_owner, "feature").Should().BeTrue();

            _referenceRepository.Module.GetSelectedBranch().Should().Be("feature");
        }
        finally
        {
            AppSettings.AlwaysShowCheckoutBranchDlg = alwaysShow;
            AppSettings.CheckForUncommittedChangesInCheckoutBranch = checkForChanges;
        }
    }
}
