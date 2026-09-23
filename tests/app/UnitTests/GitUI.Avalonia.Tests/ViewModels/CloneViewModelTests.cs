using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the clone dialog (phase 2, batch 6).</summary>
[TestFixture]
public sealed class CloneViewModelTests
{
    [Test]
    public void Suggests_the_repository_name_as_subdirectory_and_shows_the_location()
    {
        string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        CloneViewModel viewModel = Create(new FakeCloneHost(), from: "https://github.com/gitextensions/gitextensions.git", destination);

        viewModel.NewDirectory.Should().Be("gitextensions");
        viewModel.InfoText.Should().Contain(Path.Combine(destination, "gitextensions")).And.EndWith("(New directory)");
        viewModel.IsInfoWarning.Should().BeFalse();

        viewModel.Destination = "";
        viewModel.InfoText.Should().Contain("[Destination:]", "the caption is shown without its access key");
        viewModel.IsInfoWarning.Should().BeTrue();
    }

    [Test]
    public void Warns_about_an_existing_directory_that_is_not_empty()
    {
        string destination = Path.GetDirectoryName(typeof(CloneViewModelTests).Assembly.Location)!;
        CloneViewModel viewModel = Create(new FakeCloneHost(), from: "", destination: Path.GetDirectoryName(destination)!);

        viewModel.NewDirectory = Path.GetFileName(destination);

        viewModel.InfoText.Should().EndWith("(Directory already exists)");
        viewModel.IsInfoWarning.Should().BeTrue();
    }

    [TestCase("", "You need to specify destination folder.")]
    [TestCase("relative\\path", "Destination folder must be an absolute path.")]
    public void Validates_the_destination(string destination, string expectedError)
    {
        FakeCloneHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        CloneViewModel viewModel = Create(host, "https://example.org/repo.git", destination, messageBoxes);

        viewModel.CloneCommand.Execute(null);

        messageBoxes.Errors.Should().Equal(expectedError);
        host.Requests.Should().BeEmpty();
    }

    [Test]
    public void Clones_with_the_chosen_options()
    {
        FakeCloneHost host = new();
        string destination = Path.Combine(Path.GetTempPath(), "clones");
        CloneViewModel viewModel = Create(host, "https://example.org/repo.git", destination);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.IsCentral = true;
        viewModel.InitializeSubmodules = false;
        viewModel.DownloadFullHistory = false;
        viewModel.Branch = "(none: don't checkout after clone)";

        viewModel.CloneCommand.Execute(null);

        host.CancelledLoads.Should().Be(1);
        host.Requests.Should().Equal(new CloneRequest("https://example.org/repo.git", Path.Combine(destination, "repo"), true, false, null, 1, false, null));
        closed.Should().BeTrue();
    }

    [Test]
    public void Default_branch_clones_the_remote_head_with_full_history()
    {
        FakeCloneHost host = new() { Result = false };
        CloneViewModel viewModel = Create(host, "https://example.org/repo.git", Path.GetTempPath());
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.CloneCommand.Execute(null);

        host.Requests[0].Branch.Should().BeEmpty();
        host.Requests[0].Depth.Should().BeNull();
        host.Requests[0].SingleBranch.Should().BeNull();
        closed.Should().BeFalse("the clone failed");
    }

    [Test]
    public void Lists_the_remote_branches_and_resets_them_when_the_repository_changes()
    {
        FakeCloneHost host = new() { Branches = ["main", "dev"] };
        CloneViewModel viewModel = Create(host, "https://example.org/repo.git", Path.GetTempPath());

        viewModel.LoadBranchesCommand.Execute(null);
        viewModel.Branches.Should().Equal("(default: remote HEAD)", "(none: don't checkout after clone)", "main", "dev");
        viewModel.Branch = "dev";

        viewModel.From = "https://example.org/other.git";
        viewModel.Branches.Should().HaveCount(2);
        viewModel.Branch.Should().Be("(default: remote HEAD)");
    }

    [Test]
    public void Loaded_ssh_key_is_passed_to_the_clone()
    {
        FakeCloneHost host = new() { SshKey = @"C:\keys\id.ppk" };
        CloneViewModel viewModel = Create(host, "git@example.org:repo.git", Path.GetTempPath());

        viewModel.LoadSshKeyCommand.Execute(null);
        viewModel.CloneCommand.Execute(null);

        host.Requests[0].PuttySshKey.Should().Be(@"C:\keys\id.ppk");
    }

    internal static CloneViewModel Create(ICloneHost host, string from, string destination, ProcessViewModelTests.FakeMessageBoxes? messageBoxes = null)
        => new(new CloneStrings(), ["https://example.org/a.git"], [@"C:\repos"], from, destination, canLoadSshKey: true, host, messageBoxes ?? new(), new SmallDialogViewModelTests.FakeFileDialogs());

    internal sealed class FakeCloneHost : ICloneHost
    {
        public IReadOnlyList<string> Branches { get; init; } = [];

        public string? SshKey { get; init; }

        public bool Result { get; init; } = true;

        public List<CloneRequest> Requests { get; } = [];

        public int CancelledLoads { get; private set; }

        public void LoadBranches(string from, Action<IReadOnlyList<string>> report) => report(Branches);

        public void CancelLoadingBranches() => CancelledLoads++;

        public string? BrowseAndLoadSshKey() => SshKey;

        public bool Clone(CloneRequest request)
        {
            Requests.Add(request);
            return Result;
        }
    }
}
