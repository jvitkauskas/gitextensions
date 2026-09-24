using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitUI.Presentation.CommandsDialogs.RepoHosting;
using GitUI.Presentation.UserControls.FileStatusList;
using NSubstitute;
using static GitUI.AvaloniaTests.ViewModels.DiffViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the repository hosting dialogs (ports of <c>ForkAndCloneForm</c>, <c>CreatePullRequestForm</c>, <c>ViewPullRequestsForm</c>).</summary>
[TestFixture]
public sealed class RepoHostingViewModelTests
{
    internal const string Patch =
        "diff --git a/src/A.cs b/src/A.cs\nindex 1111111..2222222 100644\n--- a/src/A.cs\n+++ b/src/A.cs\n@@ -1 +1 @@\n-old\n+new\n"
        + "diff --git a/readme.md b/readme.md\nindex 3333333..4444444 100644\n--- a/readme.md\n+++ b/readme.md\n@@ -1 +1,2 @@\n text\n+more\n";

    [Test]
    public void ForkAndClone_lists_my_repositories_and_prepares_the_clone()
    {
        IHostedRepository fork = Repository("gitextensions", isFork: true, parentOwner: "upstream-owner");
        IHostedRepository tool = Repository("a-tool");
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetMyRepos().Returns([fork, tool]);
        ForkAndCloneHost host = new() { DefaultDestination = @"C:\repos" };
        ForkAndCloneViewModel viewModel = CreateForkAndClone(hoster, host, new FakeMessageBoxes());

        viewModel.Title.Should().Be("GitHub: Remote repository fork and clone");
        viewModel.Load();

        viewModel.Destination.Should().Be(@"C:\repos");
        viewModel.MyRepositories.Select(r => (r.Name, r.IsFork, r.Forks, r.IsPrivate)).Should().Equal(("a-tool", "No", "0", "No"), ("gitextensions", "Yes", "0", "No"));
        viewModel.CanClone.Should().BeFalse();

        viewModel.SelectedMyRepository = viewModel.MyRepositories[1];

        viewModel.CreateDirectory.Should().Be("gitextensions");
        viewModel.UpstreamRemoteNames.Should().Equal("upstream-owner", "upstream");
        viewModel.AddUpstreamRemote.Should().Be("upstream-owner");
        viewModel.CanChooseUpstreamRemote.Should().BeTrue();
        viewModel.ShowProtocols.Should().BeTrue();
        viewModel.Protocols.Should().Equal(GitProtocol.Https, GitProtocol.Ssh);
        viewModel.CanClone.Should().BeTrue();
        viewModel.CloneInfoText.Should().Be("Will clone https://example.org/gitextensions.git into C:\\repos\\gitextensions.\nYou will have push access. \"upstream-owner\" will be added as a remote.");

        viewModel.CreateDirectory = "ge";
        viewModel.CloneInfoText.Should().StartWith("Will clone https://example.org/gitextensions.git into C:\\repos\\ge.");
    }

    [Test]
    public void ForkAndClone_clones_and_adds_the_upstream_remote()
    {
        IHostedRepository fork = Repository("gitextensions", isFork: true, parentOwner: "upstream-owner");
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetMyRepos().Returns([fork]);
        ForkAndCloneHost host = new() { DefaultDestination = @"C:\repos" };
        ForkAndCloneViewModel viewModel = CreateForkAndClone(hoster, host, new FakeMessageBoxes());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Load();
        viewModel.SelectedMyRepository = viewModel.MyRepositories[0];
        viewModel.Depth = 5;

        viewModel.CloneCommand.Execute(null);

        host.Clones.Should().Equal(("https://example.org/gitextensions.git", @"C:\repos\gitextensions", 5));
        host.Remotes.Should().Equal((@"C:\repos\gitextensions", "upstream-owner", "https://example.org/upstream/gitextensions.git"));
        host.Opened.Should().Equal(@"C:\repos\gitextensions");
        closed.Should().BeTrue();
    }

    [Test]
    public void ForkAndClone_needs_a_destination()
    {
        IHostedRepository repository = Repository("gitextensions");
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetMyRepos().Returns([repository]);
        ForkAndCloneHost host = new();
        FakeMessageBoxes messageBoxes = new();
        ForkAndCloneViewModel viewModel = CreateForkAndClone(hoster, host, messageBoxes);
        viewModel.Load();
        viewModel.SelectedMyRepository = viewModel.MyRepositories[0];

        messageBoxes.Errors.Should().BeEmpty("the info text does not report the empty destination");
        viewModel.CloneCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("Clone folder can not be empty");
        host.Clones.Should().BeEmpty();
    }

    [Test]
    public async Task ForkAndClone_searches_and_forks()
    {
        IHostedRepository found = Repository("found", owner: "someone");
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetMyRepos().Returns([]);
        hoster.SearchForRepository("git").Returns([found]);
        ForkAndCloneViewModel viewModel = CreateForkAndClone(hoster, new ForkAndCloneHost(), new FakeMessageBoxes());
        viewModel.Load();
        viewModel.SelectedTabIndex = 1;
        viewModel.SearchText = "git";

        await viewModel.SearchCommand.ExecuteAsync(null);

        viewModel.SearchResults.Select(r => (r.Name, r.Owner)).Should().Equal(("found", "someone"));
        viewModel.ForkCommand.CanExecute(null).Should().BeFalse();
        viewModel.SelectedSearchResult = viewModel.SearchResults[0];
        viewModel.SelectedDescription.Should().Be("The found repository");
        viewModel.CloneInfoText.Should().StartWith("Will clone https://example.org/found.git into .\nYou can not push unless you are a collaborator.", "without destination");

        await viewModel.ForkCommand.ExecuteAsync(null);

        found.Received(1).Fork();
        viewModel.SelectedTabIndex.Should().Be(0);
        hoster.Received(2).GetMyRepos();
    }

    [Test]
    public async Task ForkAndClone_reports_an_unknown_user()
    {
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetMyRepos().Returns([]);
        hoster.GetRepositoriesOfUser("nobody").Returns(_ => throw new InvalidOperationException("404 not found"));
        FakeMessageBoxes messageBoxes = new();
        ForkAndCloneViewModel viewModel = CreateForkAndClone(hoster, new ForkAndCloneHost(), messageBoxes);
        viewModel.SearchText = " nobody ";

        await viewModel.GetFromUserCommand.ExecuteAsync(null);

        messageBoxes.Errors.Should().Equal("User not found!");
        viewModel.CanSearch.Should().BeTrue();
    }

    [Test]
    public void ForkAndClone_explains_that_the_plugin_is_not_configured()
    {
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetMyRepos().Returns(_ => throw new InvalidOperationException("no token"));
        ForkAndCloneViewModel viewModel = CreateForkAndClone(hoster, new ForkAndCloneHost(), new FakeMessageBoxes());

        viewModel.Load();

        viewModel.MyRepositories.Should().BeEmpty();
        viewModel.HelpText.Should().StartWith("Failed to get repositories. This most likely means you didn't configure GitHub, please do so via the menu \"Plugins/GitHub\".\r\n\r\nException: no token");
    }

    [Test]
    public async Task CreatePullRequest_lists_the_remotes_and_branches_and_suggests_a_title()
    {
        (IRepositoryHostPlugin hoster, IHostedRepository upstream) = CreatePullRequestHoster();
        CreatePullRequestHost host = new() { Template = "## Description" };
        CreatePullRequestViewModel viewModel = new(new CreatePullRequestStrings(), hoster, chooseRemote: null, host, new SynchronousBackgroundRunner(), new FakeMessageBoxes());

        await viewModel.LoadAsync();

        viewModel.TargetRemotes.Select(r => r.ToString()).Should().Equal("upstream (github.com/upstream/repo)");
        viewModel.SelectedTargetRemote.Should().Be(viewModel.TargetRemotes[0]);
        viewModel.TargetBranches.Should().Equal("develop", "master");
        viewModel.SelectedTargetBranch.Should().Be("master", "the default branch is selected");
        viewModel.YourBranches.Should().Equal("feature", "master");
        viewModel.SelectedYourBranch.Should().Be("master");
        host.Revisions.Should().Contain("origin/master");
        viewModel.PullRequestTitle.Should().Be("Subject of origin/master");
        viewModel.Body.Text.Should().Be("## Description");
        viewModel.CanCreate.Should().BeTrue();

        viewModel.SelectedYourBranch = "feature";
        viewModel.PullRequestTitle.Should().Be("Subject of origin/feature", "the title was not edited");
        viewModel.PullRequestTitle = "My title";
        viewModel.SelectedYourBranch = "master";
        viewModel.PullRequestTitle.Should().Be("My title", "an edited title is kept");
    }

    [Test]
    public async Task CreatePullRequest_creates_the_pull_request()
    {
        (IRepositoryHostPlugin hoster, IHostedRepository upstream) = CreatePullRequestHoster();
        FakeMessageBoxes messageBoxes = new();
        CreatePullRequestViewModel viewModel = new(new CreatePullRequestStrings(), hoster, chooseRemote: "upstream", new CreatePullRequestHost(), new SynchronousBackgroundRunner(), messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        await viewModel.LoadAsync();

        viewModel.PullRequestTitle = " ";
        viewModel.CreateCommand.Execute(null);
        messageBoxes.Errors.Should().Equal("You must specify a title.");

        viewModel.SelectedYourBranch = "feature";
        viewModel.SelectedTargetBranch = "develop";
        viewModel.PullRequestTitle = "Add the feature ";
        viewModel.Body.Text = " Details ";
        viewModel.CreateCommand.Execute(null);

        upstream.Received(1).CreatePullRequest("feature", "develop", "Add the feature", "Details");
        messageBoxes.Informations.Should().Equal("Done");
        closed.Should().BeTrue();
    }

    [Test]
    public async Task CreatePullRequest_needs_a_foreign_remote()
    {
        IHostedRemote origin = Remote("origin", isOwnedByMe: true, Repository("repo"));
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetHostedRemotesForModule().Returns([origin]);
        FakeMessageBoxes messageBoxes = new();
        CreatePullRequestViewModel viewModel = new(new CreatePullRequestStrings(), hoster, null, new CreatePullRequestHost(), new SynchronousBackgroundRunner(), messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        await viewModel.LoadAsync();

        messageBoxes.Errors.Should().Equal($"Failed to create pull request.{Environment.NewLine}Please clone GitHub repository before pull request.");
        closed.Should().BeFalse();
    }

    [Test]
    public async Task ViewPullRequests_lists_the_pull_requests_with_their_files_and_discussion()
    {
        (IRepositoryHostPlugin hoster, IPullRequestInformation pullRequest, _) = ViewPullRequestsHoster();
        ViewPullRequestsHost host = new() { CurrentRemote = "upstream" };
        FakeViewerHost viewerHost = new();
        ViewPullRequestsViewModel viewModel = CreateViewPullRequests(hoster, host, viewerHost, new FakeMessageBoxes());

        await viewModel.LoadAsync();

        viewModel.HostedRemotes.Select(r => r.Remote.Name).Should().Equal("origin", "upstream");
        viewModel.SelectedHostedRemote!.Remote.Name.Should().Be("upstream");
        viewModel.PullRequests.Select(p => (p.Id, p.Title, p.Owner, p.FetchBranch)).Should().Equal(("42", "Add a feature", "contributor", "pr/42"));
        viewModel.SelectedPullRequest.Should().Be(viewModel.PullRequests[0]);
        pullRequest.HeadRepo.Received().CloneProtocol = GitProtocol.Ssh;

        viewModel.Files.AllItems.Select(i => i.Name).Should().Equal("src/A.cs", "readme.md");
        viewModel.Files.Select(entry => entry.Item.Name == "readme.md");
        viewModel.Viewer.Editor.Text.Should().StartWith("index 3333333..4444444 100644");
        viewModel.Discussion.Select(e => (e.Author, e.Body, e.IsCommit)).Should().Equal(("contributor", "Please review", false), ("contributor", "Fix typo", true));
        viewModel.Discussion[1].CommitText.Should().Be("Commit:  abc123");
    }

    [Test]
    public async Task ViewPullRequests_skips_to_the_next_remote_without_pull_requests_at_first()
    {
        (IRepositoryHostPlugin hoster, _, _) = ViewPullRequestsHoster();
        ViewPullRequestsViewModel viewModel = CreateViewPullRequests(hoster, new ViewPullRequestsHost { CurrentRemote = "origin" }, new FakeViewerHost(), new FakeMessageBoxes());

        await viewModel.LoadAsync();

        viewModel.SelectedHostedRemote!.Remote.Name.Should().Be("upstream", "origin has no pull requests");
        viewModel.PullRequests.Should().ContainSingle();
    }

    [Test]
    public async Task ViewPullRequests_fetches_the_pull_request_and_posts_comments()
    {
        (IRepositoryHostPlugin hoster, _, IPullRequestDiscussion discussion) = ViewPullRequestsHoster();
        ViewPullRequestsHost host = new() { CurrentRemote = "upstream" };
        ViewPullRequestsViewModel viewModel = CreateViewPullRequests(hoster, host, new FakeViewerHost(), new FakeMessageBoxes());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        await viewModel.LoadAsync();

        viewModel.Comment.Text = " Looks good ";
        await viewModel.PostCommentCommand.ExecuteAsync(null);

        discussion.Received(1).Post("Looks good");
        discussion.Received(1).ForceReload();
        viewModel.Comment.Text.Should().BeEmpty();

        viewModel.FetchCommand.Execute(null);

        host.Commands.Should().Equal("fetch --no-tags --progress https://example.org/contributor/repo.git feature:pr/42");
        host.Notifications.Should().Be(1);
        closed.Should().BeTrue();
    }

    [Test]
    public async Task ViewPullRequests_adds_the_remote_of_the_pull_request_and_checks_it_out()
    {
        (IRepositoryHostPlugin hoster, _, _) = ViewPullRequestsHoster();
        ViewPullRequestsHost host = new() { CurrentRemote = "upstream" };
        ViewPullRequestsViewModel viewModel = CreateViewPullRequests(hoster, host, new FakeViewerHost(), new FakeMessageBoxes());
        await viewModel.LoadAsync();

        viewModel.AddRemoteAndFetchCommand.Execute(null);

        host.Remotes.Should().Equal(("contributor", "https://example.org/contributor/repo.git"));
        host.Commands.Should().Equal("fetch --no-tags --progress contributor feature:contributor/feature", "checkout contributor/feature");
        host.Locks.Should().Be(0, "the notifier is unlocked again");
    }

    [Test]
    public void ViewPullRequests_reports_a_patch_it_does_not_understand()
    {
        FakeMessageBoxes messageBoxes = new();
        ViewPullRequestsViewModel viewModel = CreateViewPullRequests(Hoster(), new ViewPullRequestsHost(), new FakeViewerHost(), messageBoxes);

        viewModel.SplitAndLoadDiff("diff --git something that is not a file part\n", "1111111111111111111111111111111111111111", "not a sha");

        messageBoxes.Errors.Should().Equal("Error: Unable to understand patch");
    }

    private static IRepositoryHostPlugin Hoster()
    {
        IRepositoryHostPlugin hoster = Substitute.For<IRepositoryHostPlugin>();
        hoster.Name.Returns("GitHub");
        return hoster;
    }

    internal static IHostedRepository Repository(string name, bool isFork = false, string? parentOwner = null, string owner = "me")
    {
        IHostedRepository repository = Substitute.For<IHostedRepository>();
        repository.Name.Returns(name);
        repository.Owner.Returns(owner);
        repository.IsAFork.Returns(isFork);
        repository.Description.Returns($"The {name} repository");
        repository.CloneUrl.Returns($"https://example.org/{name}.git");
        repository.ParentOwner.Returns(parentOwner);
        repository.ParentUrl.Returns(parentOwner is null ? null : $"https://example.org/upstream/{name}.git");
        repository.SupportedCloneProtocols.Returns([GitProtocol.Https, GitProtocol.Ssh]);
        return repository;
    }

    internal static IHostedRemote Remote(string name, bool isOwnedByMe, IHostedRepository repository)
    {
        IHostedRemote remote = Substitute.For<IHostedRemote>();
        remote.Name.Returns(name);
        remote.IsOwnedByMe.Returns(isOwnedByMe);
        remote.DisplayData.Returns($"{name} (github.com/{name}/repo)");
        remote.GetHostedRepository().Returns(repository);
        return remote;
    }

    private static IHostedBranch Branch(string name)
    {
        IHostedBranch branch = Substitute.For<IHostedBranch>();
        branch.Name.Returns(name);
        return branch;
    }

    private static (IRepositoryHostPlugin Hoster, IHostedRepository Upstream) CreatePullRequestHoster()
    {
        IHostedRepository mine = Repository("repo");
        IHostedBranch[] myBranches = [Branch("feature"), Branch("master")];
        mine.GetBranches().Returns(myBranches);
        mine.GetDefaultBranch().Returns("master");
        IHostedRepository upstream = Repository("repo", owner: "upstream");
        IHostedBranch[] upstreamBranches = [Branch("develop"), Branch("master")];
        upstream.GetBranches().Returns(upstreamBranches);
        upstream.GetDefaultBranch().Returns("master");
        IHostedRemote[] remotes = [Remote("origin", isOwnedByMe: true, mine), Remote("upstream", isOwnedByMe: false, upstream)];
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetHostedRemotesForModule().Returns(remotes);
        return (hoster, upstream);
    }

    internal static (IRepositoryHostPlugin Hoster, IPullRequestInformation PullRequest, IPullRequestDiscussion Discussion) ViewPullRequestsHoster()
    {
        IHostedRepository head = Repository("repo", owner: "contributor");
        head.CloneUrl.Returns("https://example.org/contributor/repo.git");

        IDiscussionEntry comment = Substitute.For<IDiscussionEntry>();
        comment.Author.Returns("contributor");
        comment.Body.Returns("Please review");
        comment.Created.Returns(new DateTime(2024, 5, 6, 7, 8, 9));
        ICommitDiscussionEntry commit = Substitute.For<ICommitDiscussionEntry>();
        commit.Author.Returns("contributor");
        commit.Body.Returns("Fix typo");
        commit.Sha.Returns("abc123");
        IPullRequestDiscussion discussion = Substitute.For<IPullRequestDiscussion>();
        discussion.Entries.Returns([comment, commit]);

        IPullRequestInformation pullRequest = Substitute.For<IPullRequestInformation>();
        pullRequest.Id.Returns("42");
        pullRequest.Title.Returns("Add a feature");
        pullRequest.Owner.Returns("contributor");
        pullRequest.FetchBranch.Returns("pr/42");
        pullRequest.HeadRef.Returns("feature");
        pullRequest.HeadRepo.Returns(head);
        pullRequest.BaseSha.Returns("1111111111111111111111111111111111111111");
        pullRequest.HeadSha.Returns("2222222222222222222222222222222222222222");
        pullRequest.GetDiffDataAsync().Returns(Task.FromResult(Patch));
        pullRequest.GetDiscussion().Returns(discussion);

        IHostedRepository originRepository = Repository("repo");
        originRepository.GetPullRequests().Returns([]);
        IHostedRepository upstreamRepository = Repository("repo", owner: "upstream");
        upstreamRepository.GetPullRequests().Returns([pullRequest]);

        IHostedRemote[] remotes = [Remote("origin", isOwnedByMe: true, originRepository), Remote("upstream", isOwnedByMe: false, upstreamRepository)];
        IRepositoryHostPlugin hoster = Hoster();
        hoster.GetHostedRemotesForModule().Returns(remotes);
        return (hoster, pullRequest, discussion);
    }

    private static ForkAndCloneViewModel CreateForkAndClone(IRepositoryHostPlugin hoster, ForkAndCloneHost host, FakeMessageBoxes messageBoxes)
        => new(new ForkAndCloneStrings(), hoster, host, new SynchronousBackgroundRunner(), messageBoxes, new FakeFileDialogs { Folder = @"D:\clones" });

    internal static ViewPullRequestsViewModel CreateViewPullRequests(IRepositoryHostPlugin hoster, IViewPullRequestsHost host, FakeViewerHost viewerHost, FakeMessageBoxes messageBoxes)
        => new(
            new ViewPullRequestsStrings(),
            hoster,
            host,
            viewerHost,
            new FileStatusListStrings(),
            new FileStatusTreeOptions(),
            new SynchronousBackgroundRunner(),
            messageBoxes);

    internal sealed class ForkAndCloneHost : IForkAndCloneHost
    {
        public string? DefaultDestination { get; init; }

        public List<(string Url, string Target, int? Depth)> Clones { get; } = [];

        public List<(string Directory, string Name, string Url)> Remotes { get; } = [];

        public List<string> Opened { get; } = [];

        public List<string> Urls { get; } = [];

        public string? GetDefaultDestination() => DefaultDestination;

        public bool Clone(string cloneUrl, string targetDirectory, int? depth)
        {
            Clones.Add((cloneUrl, targetDirectory, depth));
            return true;
        }

        public string AddRemote(string repositoryDirectory, string name, string url)
        {
            Remotes.Add((repositoryDirectory, name, url));
            return "";
        }

        public void OpenRepository(string repositoryDirectory) => Opened.Add(repositoryDirectory);

        public void OpenUrl(string url) => Urls.Add(url);
    }

    internal sealed class CreatePullRequestHost : ICreatePullRequestHost
    {
        public string? Template { get; init; }

        public List<string> Revisions { get; } = [];

        public string? GetLastCommitSubject(string revision)
        {
            Revisions.Add(revision);
            return $"Subject of {revision}\n\nBody";
        }

        public Task<string?> LoadTemplateAsync() => Task.FromResult(Template);

        public void ReportTemplateError(Exception exception) => throw exception;
    }

    internal sealed class ViewPullRequestsHost : IViewPullRequestsHost
    {
        public string CurrentRemote { get; init; } = "";

        public List<string> Commands { get; } = [];

        public List<(string Name, string Url)> Remotes { get; } = [];

        public int Notifications { get; private set; }

        public int Locks { get; private set; }

        public string GetCurrentRemote() => CurrentRemote;

        public GitProtocol GetCloneProtocol(string currentRemote) => GitProtocol.Ssh;

        public bool RunGit(string arguments)
        {
            Commands.Add(arguments);
            return true;
        }

        public string AddRemote(string name, string url)
        {
            Remotes.Add((name, url));
            return "";
        }

        public void LockRepoChanged() => Locks++;

        public void UnlockRepoChanged() => Locks--;

        public void NotifyRepoChanged() => Notifications++;
    }
}
