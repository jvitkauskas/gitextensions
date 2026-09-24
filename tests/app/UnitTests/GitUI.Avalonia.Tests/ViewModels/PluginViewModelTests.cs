using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Plugins.CreateLocalBranches;
using GitExtensions.Plugins.DeleteUnusedBranches;
using GitExtensions.Plugins.FindLargeFiles;
using GitExtensions.Plugins.Gource;
using GitExtensions.Plugins.ProxySwitcher;
using GitExtensions.Plugins.ReleaseNotesGenerator;
using GitExtUtils;
using Microsoft.VisualStudio.Threading;
using NSubstitute;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the Avalonia ports of the plugins' forms (docs/avalonia-port/PLAN.md, phase 7).</summary>
[TestFixture]
public sealed class PluginViewModelTests
{
    // GetOutput and Execute run git through the JoinableTaskFactory.
    [SetUp]
    public void SetUp() => ThreadHelper.JoinableTaskContext = new JoinableTaskContext();

    [TearDown]
    public void TearDown() => ThreadHelper.JoinableTaskContext = null!;

    [Test]
    public void CreateLocalBranches_tracks_the_branches_of_the_remote()
    {
        FakeGitExecutable git = new FakeGitExecutable()
            .Returns(Args("branch", "-a"), "* master\n  remotes/origin/HEAD -> origin/master\n  remotes/origin/feature\n  remotes/fork/other\n");
        FakeMessageBoxes messageBoxes = new();
        CreateLocalBranchesViewModel viewModel = new(new CreateLocalBranchesStrings(), git, messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Remote.Should().Be("origin");
        viewModel.CreateCommand.Execute(null);

        git.Commands.Should().Contain(Args("branch", "--track", "feature", "remotes/origin/feature"));
        git.Commands.Should().NotContain(command => command.Contains("remotes/fork/other"));
        messageBoxes.Informations.Should().Equal("4 local tracking branches have been created/updated.");
        closed.Should().BeTrue();
    }

    [Test]
    public void CreateLocalBranches_reports_that_there_are_no_branches()
    {
        FakeMessageBoxes messageBoxes = new();
        CreateLocalBranchesViewModel viewModel = new(new CreateLocalBranchesStrings(), new FakeGitExecutable(), messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.CreateCommand.Execute(null);

        messageBoxes.Errors.Should().Equal(CreateLocalBranchesViewModel.NoRemoteBranchesFound);
        closed.Should().BeFalse();
    }

    [Test]
    public async Task DeleteUnusedBranches_lists_the_merged_branches_and_checks_the_old_ones()
    {
        FakeGitExecutable git = DeleteUnusedBranchesGit();
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches(git, new FakeMessageBoxes(), new DeleteUnusedBranchesHost());

        await viewModel.LoadAsync();

        viewModel.InstructionText.Should().Be("Choose branches to delete. Only branches that are fully merged in 'HEAD' will be deleted.");
        viewModel.Branches.Select(row => (row.Name, row.Author, row.Message, row.Delete)).Should().Equal(
            ("old", "Alice", "Old work", true),
            ("recent", "Bob", "Recent work", false));
        viewModel.StatusText.Should().Be("1/2 branches selected.");
        viewModel.AllSelected.Should().BeFalse();
        viewModel.SearchButtonText.Should().Be("Search branches");
        viewModel.IsRefreshing.Should().BeFalse();
    }

    [Test]
    public async Task DeleteUnusedBranches_filters_with_the_regex()
    {
        FakeGitExecutable git = DeleteUnusedBranchesGit();
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches(git, new FakeMessageBoxes(), new DeleteUnusedBranchesHost());
        await viewModel.LoadAsync();

        viewModel.UseRegexFilter = true;
        viewModel.RegexFilter = "^REC";
        viewModel.RegexCaseInsensitive = true;
        viewModel.Branches.Should().BeEmpty("changing an option clears the results, as ClearResults");
        viewModel.StatusText.Should().Be("Press 'Search branches' to search for branches to delete.");
        await viewModel.RefreshCommand.ExecuteAsync(null);

        viewModel.Branches.Select(row => row.Name).Should().Equal("recent");

        viewModel.RegexDoesNotMatch = true;
        await viewModel.RefreshCommand.ExecuteAsync(null);
        viewModel.Branches.Select(row => row.Name).Should().Equal("old");
    }

    [Test]
    public async Task DeleteUnusedBranches_checks_all_from_the_header_and_counts_the_selection()
    {
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches(DeleteUnusedBranchesGit(), new FakeMessageBoxes(), new DeleteUnusedBranchesHost());
        await viewModel.LoadAsync();

        viewModel.AllSelected = true;
        viewModel.Branches.Should().OnlyContain(row => row.Delete);
        viewModel.StatusText.Should().Be("2/2 branches selected.");

        viewModel.Branches[0].Delete = false;
        viewModel.AllSelected.Should().BeFalse();
        viewModel.Branches[1].Delete.Should().BeTrue("unchecking a row does not uncheck the others");
        viewModel.StatusText.Should().Be("1/2 branches selected.");
    }

    [Test]
    public async Task DeleteUnusedBranches_deletes_the_checked_branches()
    {
        FakeGitExecutable git = DeleteUnusedBranchesGit();
        FakeMessageBoxes messageBoxes = new();
        DeleteUnusedBranchesHost host = new();
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches(git, messageBoxes, host);
        await viewModel.LoadAsync();

        await viewModel.DeleteCommand.ExecuteAsync(null);

        messageBoxes.Confirmations.Should().Equal("Are you sure to delete 1 selected branches?");
        git.Commands.Should().Contain(Args("branch", "-d", "old"));
        git.Commands.Should().NotContain(command => command.StartsWith("push"));
        host.RepoChangedNotifications.Should().Be(1);
        viewModel.HasDeletedBranch.Should().BeTrue();
        viewModel.IsDeleting.Should().BeFalse();
        git.Commands.Count(command => command == Args("branch", "--list", "--merged HEAD")).Should().Be(2, "the branches are searched again");
    }

    [Test]
    public async Task DeleteUnusedBranches_asks_before_deleting_on_the_remote()
    {
        FakeGitExecutable git = DeleteUnusedBranchesGit();
        git.Returns(Args("branch", "--list", "-r", "--merged HEAD"), "  origin/HEAD -> origin/master\n  origin/old\n  fork/old\n");
        git.Returns(LogArgs("origin/old"), "2000-01-01 10:00:00 +0100\nAlice\nOld work");
        FakeMessageBoxes messageBoxes = new();
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches(git, messageBoxes, new DeleteUnusedBranchesHost(), includeRemoteBranches: true);
        await viewModel.LoadAsync();
        viewModel.Branches.Select(row => row.Name).Should().Equal("origin/old");

        await viewModel.DeleteCommand.ExecuteAsync(null);

        messageBoxes.Confirmations.Should().HaveCount(2);
        messageBoxes.Confirmations[1].Should().StartWith("DANGEROUS ACTION!\nBranches will be deleted on the remote 'origin'.");
        git.Commands.Should().Contain(Args("push", "origin", ":old"));
    }

    [Test]
    public void DeleteUnusedBranches_needs_a_checked_branch_to_delete()
    {
        FakeMessageBoxes messageBoxes = new();
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches(new FakeGitExecutable(), messageBoxes, new DeleteUnusedBranchesHost());

        viewModel.DeleteCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("Select branches to delete using checkboxes in 'Delete' column.");
        viewModel.HasDeletedBranch.Should().BeFalse();
    }

    [Test]
    public async Task DeleteUnusedBranches_warns_about_unmerged_branches_and_forces_their_deletion()
    {
        FakeGitExecutable git = DeleteUnusedBranchesGit();
        git.Returns(Args("branch", "--list"), "* master\n  old\n");
        FakeMessageBoxes messageBoxes = new();
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches(git, messageBoxes, new DeleteUnusedBranchesHost(), includeUnmerged: true);

        await viewModel.LoadAsync();
        messageBoxes.Warnings.Should().Equal("Deleting unmerged branches will result in dangling commits. Use with caution!");
        await viewModel.DeleteCommand.ExecuteAsync(null);

        git.Commands.Should().Contain(Args("branch", "-D", "old"));
    }

    [Test]
    public async Task DeleteUnusedBranches_opens_the_settings_after_closing()
    {
        DeleteUnusedBranchesViewModel viewModel = CreateDeleteUnusedBranches(DeleteUnusedBranchesGit(), new FakeMessageBoxes(), new DeleteUnusedBranchesHost());
        await viewModel.LoadAsync();
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.OpenSettingsCommand.Execute(null);

        viewModel.SettingsRequested.Should().BeTrue();
        closed.Should().BeFalse();
    }

    [Test]
    public async Task FindLargeFiles_lists_the_blobs_above_the_threshold()
    {
        const string first = "1111111111111111111111111111111111111111";
        const string second = "2222222222222222222222222222222222222222";
        FakeGitExecutable git = new FakeGitExecutable()
            .Returns(Args("rev-list", "HEAD"), $"{second}\n{first}\n")
            .Returns(Args("ls-tree", "-zrl", $"\"{second}\""), "100644 blob aaaa 2097152\tbig.bin\0100644 blob bbbb 10\tsmall.txt\0100644 blob cccc 3145728\tnew.bin\0")
            .Returns(Args("ls-tree", "-zrl", $"\"{first}\""), "100644 blob aaaa 2097152\tbig.bin\0")
            .Returns(Args("show", "-s", second, "--format=\"%ci\""), "2020-02-02 10:00:00 +0000")
            .Returns(Args("show", "-s", first, "--format=\"%ci\""), "2020-01-01 10:00:00 +0000");
        IGitModule module = Substitute.For<IGitModule>();
        module.GitExecutable.Returns(git);
        module.ResolveGitInternalPath(Arg.Any<string>()).Returns(Path.Combine(Path.GetTempPath(), "no such directory", Guid.NewGuid().ToString()));
        FindLargeFilesViewModel viewModel = new(new FindLargeFilesStrings(), threshold: 1, module, "git.exe", new SynchronousBackgroundRunner(), new FakeMessageBoxes(), _ => { });

        await viewModel.SearchAsync();

        viewModel.GitObjects.Select(row => (row.Sha, row.Path, row.Size, row.CommitCount)).Should().Equal(
            ("aaaa", "big.bin", "2.00 Mb", 2),
            ("cccc", "new.bin", "3.00 Mb", 1));
        viewModel.GitObjects[0].LastCommitDate.Should().Be(DateTime.Parse("2020-02-02 10:00:00 +0000"));
        viewModel.GitObjects[0].CompressedSize.Should().Be("<Unknown>");
        viewModel.ProgressMaximum.Should().Be(2);
        viewModel.IsSearching.Should().BeFalse();
        viewModel.IsEditable.Should().BeTrue();
    }

    [Test]
    public void FindLargeFiles_deletes_the_checked_files_with_a_batch_file()
    {
        List<string> batchFiles = [];
        FakeMessageBoxes messageBoxes = new();
        FindLargeFilesViewModel viewModel = new(new FindLargeFilesStrings(), 1, Substitute.For<IGitModule>(), "C:\\git.exe", new SynchronousBackgroundRunner(), messageBoxes, batchFiles.Add);
        viewModel.GitObjects.Add(new GitObjectRow(new GitObject("aaaa", "big.bin", 2097152, "commit")) { Delete = true });
        viewModel.GitObjects.Add(new GitObjectRow(new GitObject("bbbb", "kept.bin", 2097152, "commit")));
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.DeleteCommand.Execute(null);

        messageBoxes.Confirmations.Should().Equal("Are you sure to delete the selected files?");
        batchFiles.Should().ContainSingle().Which.Should().Contain("'big.bin'").And.NotContain("kept.bin").And.StartWith("SET gitexe=\"C:\\git.exe\"");
        closed.Should().BeFalse();
    }

    [Test]
    public async Task Gource_needs_gource()
    {
        FakeMessageBoxes messageBoxes = new();
        GourceHost host = new();
        GourceStartViewModel viewModel = new(new GourceStartStrings(), @"C:\no\gource.exe", @"C:\repo", "--hide filenames", host, messageBoxes, new FakeFileDialogs(), "Error");

        await viewModel.StartCommand.ExecuteAsync(null);

        messageBoxes.Errors.Should().Equal(GourceStartViewModel.CannotFindGource);
        host.Started.Should().BeEmpty();
        viewModel.GourceArguments.Should().Be("--hide filenames");
    }

    [Test]
    public async Task Gource_starts_with_the_avatars()
    {
        string gource = Path.Combine(Path.GetTempPath(), $"gource-{Guid.NewGuid()}.exe");
        File.WriteAllText(gource, "");
        try
        {
            GourceHost host = new();
            GourceStartViewModel viewModel = new(new GourceStartStrings(), gource, @"C:\repo", "--user-image-dir \"$(AVATARS)\"", host, new FakeMessageBoxes(), new FakeFileDialogs(), "Error");
            bool? closed = null;
            viewModel.CloseRequested += (_, accepted) => closed = accepted;
            viewModel.WorkingDirectory = @"C:\other";
            viewModel.Arguments = "--seconds-per-day 1 --user-image-dir \"$(AVATARS)\"";

            await viewModel.StartCommand.ExecuteAsync(null);

            host.Started.Should().Equal((gource, "--seconds-per-day 1 --user-image-dir \"C:\\avatars\"", @"C:\other"));
            viewModel.GourceArguments.Should().Be("--seconds-per-day 1 --user-image-dir \"$(AVATARS)\"");
            viewModel.PathToGource.Should().Be(gource);
            closed.Should().BeTrue();
        }
        finally
        {
            File.Delete(gource);
        }
    }

    [Test]
    public async Task Gource_browses_for_gource_and_opens_the_links()
    {
        GourceHost host = new();
        FakeFileDialogs fileDialogs = new() { Files = [@"C:\tools\gource.exe"], Folder = @"C:\repo2" };
        GourceStartViewModel viewModel = new(new GourceStartStrings(), "", @"C:\repo", "", host, new FakeMessageBoxes(), fileDialogs, "Error");

        await viewModel.BrowseGourceCommand.ExecuteAsync(null);
        await viewModel.BrowseWorkingDirectoryCommand.ExecuteAsync(null);
        viewModel.OpenGourceProjectCommand.Execute(null);

        viewModel.GourcePath.Should().Be(@"C:\tools\gource.exe");
        viewModel.WorkingDirectory.Should().Be(@"C:\repo2");
        fileDialogs.FilePickers.Should().Equal(("Path to Gource", "Gource (gource.exe)", "gource.exe"));
        host.Urls.Should().Equal(GourceStartViewModel.GourceProjectUrl);
    }

    [Test]
    public void ProxySwitcher_shows_the_proxies_without_their_passwords()
    {
        ProxyGit git = new() { EffectiveProxy = "user:secret@proxy:8080", GlobalProxy = "user:secret@proxy:8080" };

        ProxySwitcherViewModel viewModel = new(new ProxySwitcherStrings(), new ProxySettings("user", "pw", "proxy", "8080"), git);

        viewModel.LocalHttpProxy.Should().Be("user:****@proxy:8080");
        viewModel.GlobalHttpProxy.Should().Be("user:****@proxy:8080");
        viewModel.ApplyGlobally.Should().BeTrue();
    }

    [TestCase(true, "user", "pw", "\"user:pw@proxy:8080\"")]
    [TestCase(false, "user", "", "\"user@proxy:8080\"")]
    [TestCase(false, "", "pw", "\"proxy:8080\"")]
    public void ProxySwitcher_sets_the_proxy(bool applyGlobally, string username, string password, string expectedProxy)
    {
        ProxyGit git = new();
        ProxySwitcherViewModel viewModel = new(new ProxySwitcherStrings(), new ProxySettings(username, password, "proxy", "8080"), git);
        viewModel.ApplyGlobally = applyGlobally;

        viewModel.SetProxyCommand.Execute(null);

        git.Commands.Should().Equal(new GitArgumentBuilder("config") { { applyGlobally, "--global" }, "http.proxy", expectedProxy }.ToString());
        git.Refreshes.Should().Be(2);
    }

    [TestCase(true, "config --global --unset http.proxy")]
    [TestCase(false, "config --unset http.proxy")]
    public void ProxySwitcher_unsets_the_proxy(bool applyGlobally, string expected)
    {
        ProxyGit git = new() { EffectiveProxy = "a", GlobalProxy = "b" };
        ProxySwitcherViewModel viewModel = new(new ProxySwitcherStrings(), new ProxySettings("", "", "proxy", ""), git);
        viewModel.ApplyGlobally = applyGlobally;

        viewModel.UnsetProxyCommand.Execute(null);

        git.Commands.Should().Equal(expected);
    }

    [Test]
    public void ProxySwitcher_needs_a_proxy_host()
    {
        ProxySwitcherViewModel.IsConfigured(new ProxySettings("user", "pw", "", "8080")).Should().BeFalse();
        ProxySwitcherViewModel.IsConfigured(new ProxySettings("", "", "proxy", "")).Should().BeTrue();
    }

    [Test]
    public void ReleaseNotesGenerator_validates_the_commit_expressions()
    {
        FakeMessageBoxes messageBoxes = new();
        ReleaseNotesGeneratorViewModel viewModel = new(new ReleaseNotesGeneratorStrings(), new FakeGitExecutable(), new ReleaseNotesClipboard(), messageBoxes);
        List<ReleaseNotesInput> focused = [];
        viewModel.FocusRequested += (_, input) => focused.Add(input);

        viewModel.GenerateCommand.Execute(null);
        viewModel.RevisionFrom = "v1.0";
        viewModel.RevisionTo = " ";
        viewModel.GenerateCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("'From' commit must be specified", "'To' commit must be specified");
        focused.Should().Equal(ReleaseNotesInput.From, ReleaseNotesInput.To);
        viewModel.CanCopy.Should().BeFalse();
        viewModel.RevisionCount.Should().Be("n/a");
    }

    [Test]
    public void ReleaseNotesGenerator_generates_and_copies_the_log()
    {
        string arguments = new GitArgumentBuilder("log") { "--pretty=\"format:%h@%s%b\" --abbrev-commit v1.0..HEAD" }.ToString();
        FakeGitExecutable git = new FakeGitExecutable().Returns(arguments, "abc1234@First change\nwith details\n\ndef5678@Second <change>");
        ReleaseNotesClipboard clipboard = new();
        ReleaseNotesGeneratorViewModel viewModel = new(new ReleaseNotesGeneratorStrings(), git, clipboard, new FakeMessageBoxes()) { RevisionFrom = "v1.0" };

        viewModel.GenerateCommand.Execute(null);

        viewModel.RevisionCount.Should().Be("2");
        viewModel.CanCopy.Should().BeTrue();
        viewModel.Result.Should().Be(string.Join(Environment.NewLine, "abc1234@First change", "with details", "", "def5678@Second <change>"));

        viewModel.CopyAsTextTableTabCommand.Execute(null);
        viewModel.CopyAsTextTableSpaceCommand.Execute(null);
        viewModel.CopyAsHtmlCommand.Execute(null);
        viewModel.CopyOriginalOutputCommand.Execute(null);

        string nl = Environment.NewLine;
        clipboard.Texts.Should().Equal(
            $"Commit log from 'v1.0' to 'HEAD' (most recent changes are listed on top):{nl}abc1234\tFirst change{nl}\twith details{nl}def5678\tSecond <change>{nl}",
            $"Commit log from 'v1.0' to 'HEAD' (most recent changes are listed on top):{nl}abc1234 First change{nl}        with details{nl}def5678 Second <change>{nl}",
            viewModel.Result);
        clipboard.Html.Should().ContainSingle().Which.Should().Be(
            "<p>Commit log from 'v1.0' to 'HEAD' (most recent changes are listed on top):</p><table>\r\n"
            + "<tr>\r\n  <td>abc1234</td>\r\n  <td>First change<br/>with details<br/></td>\r\n</tr>\r\n"
            + "<tr>\r\n  <td>def5678</td>\r\n  <td>Second &lt;change&gt;</td>\r\n</tr>\r\n</table>");
    }

    private static string Args(string command, params string[] arguments)
    {
        GitArgumentBuilder builder = new(command);
        foreach (string argument in arguments)
        {
            builder.Add(argument);
        }

        return builder.ToString();
    }

    private static string LogArgs(string branch)
        => new GitArgumentBuilder("log") { "--pretty=\"format:%ci\n%an\n%s\"", "--max-count=1", branch.Quote(), "--" }.ToString();

    private static FakeGitExecutable DeleteUnusedBranchesGit()
        => new FakeGitExecutable()
            .Returns(Args("branch", "--list", "--merged HEAD"), "* master\n+ old\n  recent\n  HEAD\n")
            .Returns(LogArgs("old"), "2000-01-01 10:00:00 +0100\nAlice\nOld work")
            .Returns(LogArgs("recent"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\nBob\nRecent work");

    private static DeleteUnusedBranchesViewModel CreateDeleteUnusedBranches(
        FakeGitExecutable git,
        FakeMessageBoxes messageBoxes,
        DeleteUnusedBranchesHost host,
        bool includeRemoteBranches = false,
        bool includeUnmerged = false)
    {
        IGitModule module = Substitute.For<IGitModule>();
        module.GitExecutable.Returns(git);
        module.GetSelectedBranch(Arg.Any<bool>()).Returns("master");
        DeleteUnusedBranchesFormSettings settings = new(
            daysOlderThan: 30,
            mergedInBranch: "HEAD",
            removeDeleteRemoteBranchesFromFlag: includeRemoteBranches,
            remoteName: "origin",
            userRegexToFilterBranchesFlag: false,
            regexFilter: "/(feature|develop)/",
            regexCaseInsensitiveFlag: false,
            regexInvertedFlag: false,
            includeUnmergedBranchesFlag: includeUnmerged);
        return new DeleteUnusedBranchesViewModel(new DeleteUnusedBranchesStrings(), settings, module, new SynchronousBackgroundRunner(), messageBoxes, host);
    }

    private sealed class DeleteUnusedBranchesHost : IDeleteUnusedBranchesHost
    {
        public int RepoChangedNotifications { get; private set; }

        public List<Exception> Errors { get; } = [];

        public void NotifyRepoChanged() => RepoChangedNotifications++;

        public void ReportError(Exception exception) => Errors.Add(exception);
    }

    private sealed class GourceHost : IGourceStartHost
    {
        public List<(string Command, string Arguments, string WorkingDirectory)> Started { get; } = [];

        public List<string> Urls { get; } = [];

        public Task<string> LoadAvatarsAsync() => Task.FromResult(@"C:\avatars");

        public void StartDetached(string command, string arguments, string workingDirectory) => Started.Add((command, arguments, workingDirectory));

        public void OpenUrl(string url) => Urls.Add(url);
    }

    private sealed class ProxyGit : IProxySwitcherGit
    {
        public string EffectiveProxy { get; set; } = "";

        public string GlobalProxy { get; set; } = "";

        public List<string> Commands { get; } = [];

        public int Refreshes { get; private set; }

        public string GetEffectiveProxy()
        {
            Refreshes++;
            return EffectiveProxy;
        }

        public string GetGlobalProxy() => GlobalProxy;

        public void Run(ArgumentString arguments) => Commands.Add(arguments.ToString());
    }

    private sealed class ReleaseNotesClipboard : IReleaseNotesClipboard
    {
        public List<string> Texts { get; } = [];

        public List<string> Html { get; } = [];

        public void CopyText(string text) => Texts.Add(text);

        public void CopyHtml(string htmlFragment) => Html.Add(htmlFragment);
    }
}
