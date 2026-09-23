using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 3: the sparse working copy dialog, with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Sparse_working_copy_enables_the_feature_and_saves_the_rules()
    {
        string rulesPath = Path.Combine(_referenceRepository.Module.ResolveGitInternalPath("info"), "sparse-checkout");
        File.Delete(rulesPath);

        bool? initiallyEnabled = null;
        DriveNextDialog(window =>
        {
            SparseWorkingCopyViewModel viewModel = (SparseWorkingCopyViewModel)window.DataContext!;
            initiallyEnabled = viewModel.IsSparseCheckoutEnabled;
            viewModel.EnableCommand.Execute(null);
            viewModel.Rules.Text = "/*\n!/B.txt\n";
            viewModel.IsRefreshWorkingCopyOnSave = false;
            Capture(window, "sparse-working-copy");
            viewModel.SaveCommand.Execute(null);
        });

        _commands.StartSparseWorkingCopyDialog(_owner).Should().BeTrue();

        initiallyEnabled.Should().BeFalse();
        _referenceRepository.Module.GetEffectiveSetting("core.sparsecheckout").Should().Be("true");
        File.ReadAllText(rulesPath, GitModule.SystemEncoding).Should().Be("/*\n!/B.txt\n");
    }
}
