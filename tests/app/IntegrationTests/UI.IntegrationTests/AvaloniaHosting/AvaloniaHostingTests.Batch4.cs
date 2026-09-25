using GitCommands;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.AvaloniaHosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUIPluginInterfaces;
using NSubstitute;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>The fourth batch of phase 2 dialogs, shown from their WinForms entry points with real git.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void StartCheckoutRevisionDialog_checks_out_the_revision()
    {
        string first = _referenceRepository.CommitHash!;
        _referenceRepository.CreateCommit("Second commit", "second content");

        DriveDialogs(
            window =>
            {
                CheckoutRevisionViewModel viewModel = (CheckoutRevisionViewModel)window.DataContext!;
                viewModel.CommitPicker.SelectedObjectId.ToString().Should().Be(first);
                Capture(window, "checkout-revision");
                viewModel.CheckoutCommand.Execute(null);
            },
            AcknowledgeWhenDone);

        GitUI.GitUICommands commands = CreateCommandsWithPassingScripts();
        commands.StartCheckoutRevisionDialog(_owner, first).Should().BeTrue();

        _referenceRepository.Module.RevParse("HEAD").ToString().Should().Be(first);
    }

    [Test]
    public void Compare_to_branch_dialog_returns_the_branch()
    {
        _referenceRepository.CreateBranch("other", _referenceRepository.CommitHash!);

        DriveNextDialog(window =>
        {
            CompareToBranchViewModel viewModel = (CompareToBranchViewModel)window.DataContext!;
            viewModel.BranchSelector.IsLocal = true;
            viewModel.BranchSelector.BranchName = "other";
            Capture(window, "compare-to-branch");
            viewModel.CompareCommand.Execute(null);
        });

        AvaloniaDialogs.TryShowCompareToBranch(_owner, _commands, ObjectId.Parse(_referenceRepository.CommitHash!), out string? branchName).Should().BeTrue();

        branchName.Should().Be("other");
    }

    [Test]
    public void Bisect_dialog_starts_a_bisect()
    {
        IRevisionGridInfo revisionGrid = Substitute.For<IRevisionGridInfo>();
        revisionGrid.GetSelectedRevisions().Returns([]);
        try
        {
            DriveDialogs(
                window =>
                {
                    BisectViewModel viewModel = (BisectViewModel)window.DataContext!;
                    viewModel.CanStart.Should().BeTrue();
                    viewModel.StartCommand.Execute(null);
                    viewModel.IsInTheMiddleOfBisect.Should().BeTrue();
                    Capture(window, "bisect");
                    window.Close();
                },
                AcknowledgeWhenDone);

            AvaloniaDialogs.TryShowBisect(_owner, _commands, revisionGrid).Should().BeTrue();

            _referenceRepository.Module.InTheMiddleOfBisect().Should().BeTrue();
        }
        finally
        {
            _referenceRepository.Module.GitExecutable.GetOutput("bisect reset");
        }
    }

    [Test]
    public void Go_to_commit_dialog_resolves_the_expression()
    {
        DriveNextDialog(window =>
        {
            GoToCommitViewModel viewModel = (GoToCommitViewModel)window.DataContext!;
            viewModel.Branches.Select(b => b.Name).Should().Contain("master");
            viewModel.CommitExpression = "master";
            viewModel.Source = GoToCommitSource.Expression;
            Capture(window, "go-to-commit");
            viewModel.GoCommand.Execute(null);
        });

        AvaloniaDialogs.TryShowGoToCommit(_owner, _commands, out bool accepted, out ObjectId commitId).Should().BeTrue();

        accepted.Should().BeTrue();
        commitId.ToString().Should().Be(_referenceRepository.CommitHash);
    }

    [Test]
    public void Dashboard_category_dialog_returns_the_name()
    {
        DriveNextDialog(window =>
        {
            DashboardCategoryTitleViewModel viewModel = (DashboardCategoryTitleViewModel)window.DataContext!;
            viewModel.CategoryName = "Personal";
            viewModel.OkCommand.Execute(null);
        });

        AvaloniaDialogs.TryShowDashboardCategoryTitle(_owner, ["Work"], originalName: null, out string? name).Should().BeTrue();

        name.Should().Be("Personal");
    }

    [Test]
    public void StartAddToGitIgnoreDialog_appends_the_patterns()
    {
        string gitIgnore = Path.Combine(_referenceRepository.Module.WorkingDir, ".gitignore");
        File.WriteAllText(Path.Combine(_referenceRepository.Module.WorkingDir, "build.log"), "log");
        IReadOnlyList<string>? preview = null;

        DriveNextDialog(window =>
        {
            AddToGitIgnoreViewModel viewModel = (AddToGitIgnoreViewModel)window.DataContext!;
            WaitUntil(() => !viewModel.IsUpdating, () =>
            {
                preview = viewModel.PreviewFiles;
                Capture(window, "add-to-gitignore");
                viewModel.IgnoreCommand.Execute(null);
            });
        });

        _commands.StartAddToGitIgnoreDialog(_owner, false, "*.log").Should().BeTrue();

        preview.Should().Equal("build.log");
        File.ReadAllText(gitIgnore).Should().Contain("*.log");
    }

    private GitUI.GitUICommands CreateCommandsWithPassingScripts()
    {
        System.ComponentModel.Design.ServiceContainer services = GlobalServiceContainer.CreateDefaultMockServiceContainer();
        services.RemoveService(typeof(GitUI.ScriptsEngine.IScriptsRunner));
        services.AddService<GitUI.ScriptsEngine.IScriptsRunner>(new PassingScriptsRunner());
        return new GitUI.GitUICommands(services, _referenceRepository.Module);
    }

    /// <summary>Runs <paramref name="action"/> on the UI thread once <paramref name="condition"/> holds (polled).</summary>
    private void WaitUntil(Func<bool> condition, Action action)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);

        void Poll()
        {
            try
            {
                if (condition() || DateTime.UtcNow > deadline)
                {
                    action();
                    return;
                }

                Avalonia.Threading.DispatcherTimer.RunOnce(Poll, TimeSpan.FromMilliseconds(100));
            }
            catch (Exception ex)
            {
                // Close the dialogs, or the modal loop that shows them (and so the test) would not end.
                _driveFailure = ex;
                CloseDrivenDialogs();
            }
        }

        Poll();
    }
}
