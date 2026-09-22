using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.ScriptsEngine;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaTests.ViewModels;

[TestFixture]
public sealed class SmallDialogViewModelTests
{
    [TestCase(false, "add --dry-run src", "add src")]
    [TestCase(true, "add --dry-run -f src", "add -f src")]
    public void AddFiles_runs_git_add(bool force, string showArguments, string addArguments)
    {
        List<string> runs = [];
        AddFilesViewModel viewModel = new(new AddFilesStrings(), "src", arguments =>
        {
            runs.Add(arguments);
            return true;
        });
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Force = force;

        viewModel.ShowFilesCommand.Execute(null);
        closed.Should().BeNull("showing the files keeps the dialog open");
        viewModel.AddFilesCommand.Execute(null);

        runs.Should().Equal(showArguments, addArguments);
        closed.Should().BeTrue();
    }

    [Test]
    public void AddFiles_defaults_to_the_current_directory_and_stays_open_on_failure()
    {
        AddFilesViewModel viewModel = new(new AddFilesStrings(), filter: null, _ => false);
        bool closeRequested = false;
        viewModel.CloseRequested += (_, _) => closeRequested = true;

        viewModel.Filter.Should().Be(".");
        viewModel.AddFilesCommand.Execute(null);

        closeRequested.Should().BeFalse();
    }

    [TestCase(true, true, true, false)]
    [TestCase(false, true, false, true)]
    [TestCase(true, false, false, false)]
    public void ResetChanges_offers_deleting_new_files_only_for_a_mix(bool hasExisting, bool hasNew, bool canChoose, bool deleteNew)
    {
        ResetChangesViewModel viewModel = new(new ResetChangesStrings(), hasExisting, hasNew, confirmationMessage: null);

        viewModel.CanChooseDeleteNewFiles.Should().Be(canChoose);
        viewModel.DeleteNewFiles.Should().Be(deleteNew);
        viewModel.Message.Should().Be("Are you sure you want to reset your changes?");
    }

    [TestCase(false, ResetChangesAction.Reset)]
    [TestCase(true, ResetChangesAction.ResetAndDelete)]
    public void ResetChanges_returns_the_choice(bool deleteNewFiles, ResetChangesAction expected)
    {
        ResetChangesViewModel viewModel = new(new ResetChangesStrings(), true, true, "Reset 3 files?");
        viewModel.DeleteNewFiles = deleteNewFiles;

        viewModel.ResetCommand.Execute(null);

        viewModel.SelectedAction.Should().Be(expected);
        viewModel.Message.Should().Be("Reset 3 files?");
    }

    [Test]
    public void ResetChanges_cancels_by_default()
    {
        ResetChangesViewModel viewModel = new(new ResetChangesStrings(), true, true, null);

        viewModel.SelectedAction.Should().Be(ResetChangesAction.Cancel);
        viewModel.CancelCommand.Execute(null);
        viewModel.SelectedAction.Should().Be(ResetChangesAction.Cancel);
    }

    [Test]
    public void DeleteTag_deletes_locally_and_optionally_from_the_remote()
    {
        FakeDeleteTagHost host = new();
        DeleteTagViewModel viewModel = new(new DeleteTagStrings(), ["v1.0", "v2.0"], "v1.0", ["origin", "fork"], "origin", () => "https://manual", host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.DeleteFromRemote = true;
        viewModel.SelectedRemote = "fork";
        viewModel.DeleteCommand.Execute(null);

        host.Calls.Should().Equal("local v1.0", "remote fork v1.0");
        closed.Should().BeTrue();
        viewModel.HelpTooltip.Should().Be("Read more about this feature at https://manual");
    }

    [Test]
    public void DeleteTag_failure_keeps_the_dialog_open()
    {
        FakeDeleteTagHost host = new() { Fail = true };
        DeleteTagViewModel viewModel = new(new DeleteTagStrings(), [], "v9", [], "", () => "", host);
        bool closeRequested = false;
        viewModel.CloseRequested += (_, _) => closeRequested = true;

        viewModel.DeleteCommand.Execute(null);

        closeRequested.Should().BeFalse();
        host.Calls.Should().BeEmpty();
    }

    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("relative\\path", false)]
    [TestCase("C:\\repos\\new", true)]
    public void Init_accepts_only_absolute_directories(string path, bool expected)
    {
        InitViewModel.IsRootedDirectoryPath(path).Should().Be(expected);
    }

    [Test]
    public void Init_rejects_an_invalid_directory()
    {
        FakeInitHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        InitViewModel viewModel = CreateInitViewModel(host, messageBoxes, "not rooted");

        viewModel.CreateCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("Please choose a directory.");
        host.Initialized.Should().BeNull();
    }

    [Test]
    public void Init_rejects_a_file()
    {
        FakeInitHost host = new() { ExistingFile = "C:\\repos\\file.txt" };
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        InitViewModel viewModel = CreateInitViewModel(host, messageBoxes, "C:\\repos\\file.txt");

        viewModel.CreateCommand.Execute(null);

        messageBoxes.Errors.Should().ContainSingle().Which.Should().StartWith("Cannot initialize a new repository on a file.");
    }

    [Test]
    public void Init_creates_the_repository_and_reports_git_output()
    {
        FakeInitHost host = new();
        ProcessViewModelTests.FakeMessageBoxes messageBoxes = new();
        InitViewModel viewModel = CreateInitViewModel(host, messageBoxes, "C:\\repos\\new");
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.IsCentral = true;
        viewModel.CreateCommand.Execute(null);

        host.Initialized.Should().Be(("C:\\repos\\new", true));
        host.Created.Should().Be("C:\\repos\\new");
        messageBoxes.Informations.Should().Equal("Initialized empty Git repository");
        closed.Should().BeTrue();
    }

    [Test]
    public async Task Init_browse_picks_a_folder()
    {
        FakeFileDialogs fileDialogs = new() { Folder = "D:\\picked" };
        InitViewModel viewModel = new(new InitStrings(), [], "C:\\start", "Error", new FakeInitHost(), new ProcessViewModelTests.FakeMessageBoxes(), fileDialogs);

        await viewModel.BrowseCommand.ExecuteAsync(null);

        viewModel.Directory.Should().Be("D:\\picked");
        fileDialogs.StartDirectories.Should().Equal("C:\\start");
    }

    [Test]
    public void GoToLine_shows_the_range()
    {
        GoToLineViewModel viewModel = new(new GoToLineStrings(), maxLineNumber: 120);

        viewModel.Label.Should().Be("Line number (1 - 120):");
        viewModel.LineNumber.Should().Be(1);
        viewModel.MaxLineNumber.Should().Be(120);
    }

    [TestCase(null, null, "User input", "Please specify your input:")]
    [TestCase("Branch", "Name", "Branch", "Name:")]
    public void SimplePrompt_has_defaults(string? title, string? label, string expectedTitle, string expectedLabel)
    {
        SimplePromptViewModel viewModel = new(title, label, "default");

        viewModel.Title.Should().Be(expectedTitle);
        viewModel.Label.Should().Be(expectedLabel);
        viewModel.Input.Should().Be("default");

        viewModel.Input = "typed";
        viewModel.OkCommand.Execute(null);
        viewModel.UserInput.Should().Be("typed");
    }

    [Test]
    public async Task FilePrompt_quotes_picked_files()
    {
        FakeFileDialogs fileDialogs = new() { Files = ["C:\\a b.txt", "C:\\c.txt"] };
        FilePromptViewModel viewModel = new(new FilePromptStrings(), fileDialogs);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        await viewModel.BrowseCommand.ExecuteAsync(null);
        viewModel.OkCommand.Execute(null);

        viewModel.UserInput.Should().Be("\"C:\\a b.txt\" \"C:\\c.txt\"");
        closed.Should().BeTrue();
    }

    [Test]
    public void FilePrompt_without_files_cancels()
    {
        FilePromptViewModel viewModel = new(new FilePromptStrings(), new FakeFileDialogs());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.OkCommand.Execute(null);

        closed.Should().BeFalse();
    }

    [Test]
    public void Contributors_joins_names_per_tab()
    {
        ContributorsViewModel viewModel = new("Alice,\r\nBob", "Carol,\nDan", "Eve", "Frank");

        viewModel.DevelopersText.Should().Be("Team:\r\nAlice, Bob\r\n\r\nContributors:\r\nCarol, Dan");
        viewModel.TranslatorsText.Should().Be("Eve");
        viewModel.DesignersText.Should().Be("Frank");
    }

    private static InitViewModel CreateInitViewModel(FakeInitHost host, IMessageBoxService messageBoxes, string directory)
        => new(new InitStrings(), [], directory, "Error", host, messageBoxes, new FakeFileDialogs());

    private sealed class FakeDeleteTagHost : IDeleteTagHost
    {
        public bool Fail { get; init; }

        public List<string> Calls { get; } = [];

        public void DeleteLocalTag(string tagName)
        {
            if (Fail)
            {
                throw new InvalidOperationException("no such tag");
            }

            Calls.Add($"local {tagName}");
        }

        public void DeleteRemoteTag(string remote, string tagName) => Calls.Add($"remote {remote} {tagName}");

        public void OpenUrl(string url) => Calls.Add($"url {url}");
    }

    private sealed class FakeInitHost : IInitRepositoryHost
    {
        public string? ExistingFile { get; init; }

        public (string Directory, bool Central)? Initialized { get; private set; }

        public string? Created { get; private set; }

        public bool FileExists(string path) => path == ExistingFile;

        public string Init(string directory, bool central)
        {
            Initialized = (directory, central);
            return "Initialized empty Git repository";
        }

        public void OnRepositoryCreated(string directory) => Created = directory;
    }

    internal sealed class FakeFileDialogs : IFileDialogService
    {
        public IReadOnlyList<string> Files { get; init; } = [];

        public string? Folder { get; init; }

        public List<string?> StartDirectories { get; } = [];

        public Task<IReadOnlyList<string>> PickFilesAsync(bool allowMultiple, string? startDirectory = null)
        {
            StartDirectories.Add(startDirectory);
            return Task.FromResult(Files);
        }

        public Task<string?> PickFolderAsync(string? startDirectory = null)
        {
            StartDirectories.Add(startDirectory);
            return Task.FromResult(Folder);
        }
    }
}
