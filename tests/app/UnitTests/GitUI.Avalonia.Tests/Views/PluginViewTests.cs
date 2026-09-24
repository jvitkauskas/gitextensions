using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Plugins.CreateLocalBranches;
using GitExtensions.Plugins.DeleteUnusedBranches;
using GitExtensions.Plugins.FindLargeFiles;
using GitExtensions.Plugins.Gource;
using GitExtensions.Plugins.ProxySwitcher;
using GitExtensions.Plugins.ReleaseNotesGenerator;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using Microsoft.VisualStudio.Threading;
using NSubstitute;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the Avalonia ports of the plugins' forms (docs/avalonia-port/PLAN.md, phase 7).</summary>
[TestFixture]
public sealed class PluginViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadWithJoinableTasksAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(
            new CreateLocalBranchesWindow { DataContext = new CreateLocalBranchesViewModel(new CreateLocalBranchesStrings(), new FakeGitExecutable(), new FakeMessageBoxes()) },
            $"plugin-create-local-branches-{theme}");

        DeleteUnusedBranchesWindow deleteWindow = Show(new DeleteUnusedBranchesWindow { DataContext = CreateDeleteUnusedBranches() });
        SaveScreenshot(deleteWindow.CaptureRenderedFrame(), $"plugin-delete-unused-branches-{theme}");
        deleteWindow.Close();

        FindLargeFilesWindow largeFilesWindow = Show(new FindLargeFilesWindow { DataContext = CreateFindLargeFiles() });
        SaveScreenshot(largeFilesWindow.CaptureRenderedFrame(), $"plugin-find-large-files-{theme}");
        largeFilesWindow.Close();

        Capture(
            new GourceStartWindow
            {
                DataContext = new GourceStartViewModel(
                    new GourceStartStrings(), @"C:\tools\gource\gource.exe", @"C:\repo", "--hide filenames --user-image-dir \"$(AVATARS)\"",
                    new NullGourceHost(), new FakeMessageBoxes(), new FakeFileDialogs(), "Error"),
            },
            $"plugin-gource-{theme}");

        Capture(
            new ProxySwitcherWindow { DataContext = new ProxySwitcherViewModel(new ProxySwitcherStrings(), new ProxySettings("user", "pw", "proxy", "8080"), new FixedProxyGit()) },
            $"plugin-proxy-switcher-{theme}");

        ReleaseNotesGeneratorViewModel releaseNotes = new(
            new ReleaseNotesGeneratorStrings(),
            new FakeGitExecutable().Returns(
                new GitArgumentBuilder("log") { "--pretty=\"format:%h@%s%b\" --abbrev-commit v1.0..HEAD" }.ToString(),
                "abc1234@First change\ndef5678@Second change"),
            new NullClipboard(),
            new FakeMessageBoxes())
        {
            RevisionFrom = "v1.0",
        };
        ReleaseNotesGeneratorWindow releaseNotesWindow = Show(new ReleaseNotesGeneratorWindow { DataContext = releaseNotes });
        SaveScreenshot(releaseNotesWindow.CaptureRenderedFrame(), $"plugin-release-notes-generator-empty-{theme}");
        releaseNotes.GenerateCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(releaseNotesWindow.CaptureRenderedFrame(), $"plugin-release-notes-generator-{theme}");
        releaseNotesWindow.Close();
    });

    [Test]
    public Task CreateLocalBranches_binds_the_remote() => OnUiThreadAsync(() =>
    {
        CreateLocalBranchesViewModel viewModel = new(new CreateLocalBranchesStrings(), new FakeGitExecutable(), new FakeMessageBoxes());
        CreateLocalBranchesWindow window = Show(new CreateLocalBranchesWindow { DataContext = viewModel });

        window.FindControl<TextBox>("remoteTextBox")!.Text = "upstream";
        Dispatcher.UIThread.RunJobs();

        viewModel.Remote.Should().Be("upstream");
        window.FindControl<Button>("createButton")!.IsDefault.Should().BeTrue();
        window.Close();
    });

    [Test]
    public Task DeleteUnusedBranches_checks_all_branches_from_the_header() => OnUiThreadWithJoinableTasksAsync(() =>
    {
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches();
        DeleteUnusedBranchesWindow window = Show(new DeleteUnusedBranchesWindow { DataContext = viewModel });

        DataGrid grid = window.FindControl<DataGrid>("branchesGrid")!;
        grid.Columns.Skip(1).Select(c => c.Header).Should().Equal("Name", "Last activity", "Last author", "Last message");
        viewModel.Branches.Should().HaveCount(2);
        CheckBox selectAll = (CheckBox)grid.Columns[0].Header!;
        selectAll.IsChecked.Should().BeFalse();

        selectAll.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        viewModel.Branches.Should().OnlyContain(row => row.Delete);
        window.FindControl<TextBlock>("statusText")!.Text.Should().Be("2/2 branches selected.");
        grid.GetVisualDescendants().OfType<CheckBox>().Where(box => box != selectAll).Should().OnlyContain(box => box.IsChecked == true);
        window.Close();
    });

    [Test]
    public Task FindLargeFiles_lists_the_files_once_searched() => OnUiThreadWithJoinableTasksAsync(() =>
    {
        FindLargeFilesWindow window = Show(new FindLargeFilesWindow { DataContext = CreateFindLargeFiles() });

        DataGrid grid = window.FindControl<DataGrid>("objectsGrid")!;
        grid.Columns.Select(c => c.Header).Should().Equal("SHA", "Path", "Size", "Compressed size", "Commit count", "Last commit date", "Delete");
        grid.GetVisualDescendants().OfType<DataGridRow>().Should().HaveCount(2);
        window.FindControl<ProgressBar>("progressBar")!.IsVisible.Should().BeFalse("the search is over");
        window.Close();
    });

    private static string LogArgs(string branch)
        => new GitArgumentBuilder("log") { "--pretty=\"format:%ci\n%an\n%s\"", "--max-count=1", $"\"{branch}\"", "--" }.ToString();

    /// <summary>Runs <paramref name="test"/> on the UI thread with a <c>JoinableTaskFactory</c>, which runs git for the view models.</summary>
    private static Task OnUiThreadWithJoinableTasksAsync(Action test) => OnUiThreadAsync(() =>
    {
        ThreadHelper.JoinableTaskContext = new JoinableTaskContext();
        try
        {
            test();
        }
        finally
        {
            ThreadHelper.JoinableTaskContext = null!;
        }
    });

    private static T Show<T>(T window)
        where T : Window
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Capture(DialogWindow window, string name)
    {
        Show(window);
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }

    private static DeleteUnusedBranchesViewModel CreateDeleteUnusedBranches()
    {
        FakeGitExecutable git = new FakeGitExecutable()
            .Returns(new GitArgumentBuilder("branch") { "--list", "--merged HEAD" }.ToString(), "* master\n  feature/old-login\n  feature/new-menu\n")
            .Returns(LogArgs("feature/old-login"), "2021-03-04 10:00:00 +0100\nAlice\nFix the login page")
            .Returns(LogArgs("feature/new-menu"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\nBob\nAdd the new menu");
        IGitModule module = Substitute.For<IGitModule>();
        module.GitExecutable.Returns(git);
        module.GetSelectedBranch(Arg.Any<bool>()).Returns("master");
        return new DeleteUnusedBranchesViewModel(
            new DeleteUnusedBranchesStrings(),
            new DeleteUnusedBranchesFormSettings(30, "HEAD", false, "origin", false, "/(feature|develop)/", false, false, false),
            module,
            new SynchronousBackgroundRunner(),
            new FakeMessageBoxes(),
            new NullDeleteUnusedBranchesHost());
    }

    private static FindLargeFilesViewModel CreateFindLargeFiles()
    {
        const string commit = "1111111111111111111111111111111111111111";
        FakeGitExecutable git = new FakeGitExecutable()
            .Returns(new GitArgumentBuilder("rev-list") { "HEAD" }.ToString(), $"{commit}\n")
            .Returns(
                new GitArgumentBuilder("ls-tree") { "-zrl", $"\"{commit}\"" }.ToString(),
                "100644 blob 2e65efe2a145dda7ee51d1741299f848e5bf752e 2097152\tassets/video.mp4\0100644 blob 8f94139338f9404f26296befa88755fc2598c289 5242880\tdocs/manual.pdf\0")
            .Returns(new GitArgumentBuilder("show") { "-s", commit, "--format=\"%ci\"" }.ToString(), "2024-05-06 07:08:09 +0000");
        IGitModule module = Substitute.For<IGitModule>();
        module.GitExecutable.Returns(git);
        module.ResolveGitInternalPath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "no such directory", Guid.NewGuid().ToString()));
        return new FindLargeFilesViewModel(new FindLargeFilesStrings(), 1, module, "git.exe", new SynchronousBackgroundRunner(), new FakeMessageBoxes(), _ => { });
    }

    private sealed class NullDeleteUnusedBranchesHost : IDeleteUnusedBranchesHost
    {
        public void NotifyRepoChanged()
        {
        }

        public void ReportError(Exception exception)
        {
        }
    }

    private sealed class NullGourceHost : IGourceStartHost
    {
        public Task<string> LoadAvatarsAsync() => Task.FromResult("");

        public void StartDetached(string command, string arguments, string workingDirectory)
        {
        }

        public void OpenUrl(string url)
        {
        }
    }

    private sealed class FixedProxyGit : IProxySwitcherGit
    {
        public string GetEffectiveProxy() => "user:secret@proxy.example.org:8080";

        public string GetGlobalProxy() => "";

        public void Run(ArgumentString arguments)
        {
        }
    }

    private sealed class NullClipboard : IReleaseNotesClipboard
    {
        public void CopyText(string text)
        {
        }

        public void CopyHtml(string htmlFragment)
        {
        }
    }
}
