using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the .gitignore editor (port of <c>FormGitIgnore</c>) and the change log.</summary>
[TestFixture]
public sealed class GitIgnoreEditorViewModelTests
{
    private static readonly string NL = Environment.NewLine;

    [Test]
    public void Add_default_adds_only_the_missing_patterns()
    {
        FakeHost host = new() { Content = $"*.obj{NL}Thumbs.db{NL}" };
        GitIgnoreEditorViewModel viewModel = Create(host);

        viewModel.AddDefaultCommand.Execute(null);

        string[] lines = viewModel.Editor.Text.Split(NL);
        lines.Should().StartWith(["*.obj", "Thumbs.db", "", "#Ignore thumbnails created by Windows", "#Ignore files built by Visual Studio", "*.exe"]);
        lines.Count(l => l == "*.obj").Should().Be(1);
        viewModel.Editor.HasChanges.Should().BeTrue();

        string added = viewModel.Editor.Text;
        viewModel.AddDefaultCommand.Execute(null);
        viewModel.Editor.Text.Should().Be(added, "nothing is missing any more");
    }

    [Test]
    public void Add_default_prefers_the_users_default_patterns()
    {
        FakeHost host = new() { DefaultPatterns = ["*.tmp"] };
        GitIgnoreEditorViewModel viewModel = Create(host);

        viewModel.AddDefaultCommand.Execute(null);

        viewModel.Editor.Text.Should().Be($"{NL}*.tmp{NL}");
    }

    [Test]
    public void Save_writes_changes_with_a_final_new_line_and_closes()
    {
        FakeHost host = new() { Content = "*.obj" };
        GitIgnoreEditorViewModel viewModel = Create(host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.SaveCommand.Execute(null);
        host.Saved.Should().BeEmpty("nothing changed");

        viewModel.Editor.Text = "*.obj\n*.exe";
        viewModel.SaveCommand.Execute(null);

        host.Saved.Should().Equal($"*.obj\n*.exe{NL}");
        closed.Should().BeTrue();
        viewModel.CanClose().Should().BeTrue();
    }

    [Test]
    public void Add_pattern_saves_first_and_reloads_the_file()
    {
        FakeHost host = new() { Content = "*.obj" };
        GitIgnoreEditorViewModel viewModel = Create(host);
        viewModel.Editor.Text = "*.exe";
        host.OnAddPattern = () => host.Content = $"*.exe{NL}*.dll{NL}";

        viewModel.AddPatternCommand.Execute(null);

        host.Saved.Should().Equal($"*.exe{NL}");
        viewModel.Editor.Text.Should().Be($"*.exe{NL}*.dll{NL}");
        viewModel.Editor.HasChanges.Should().BeFalse();
    }

    [TestCase(true, true, 1)]
    [TestCase(false, true, 0)]
    [TestCase(null, false, 0)]
    public void Closing_asks_to_save_unsaved_changes(bool? answer, bool closes, int saves)
    {
        FakeHost host = new();
        FakeMessageBoxes messageBoxes = new() { ConfirmWithCancelResult = answer };
        GitIgnoreEditorViewModel viewModel = new(new GitIgnoreStrings(), new GitLocalExcludeModelStrings(), host, messageBoxes);
        viewModel.Editor.Text = "*.log";

        viewModel.CanClose().Should().Be(closes);

        messageBoxes.Confirmations.Should().Equal("Save changes to .git/info/exclude?");
        host.Saved.Should().HaveCount(saves);
    }

    [Test]
    public void Reports_a_failure_to_save_and_stays_open()
    {
        FakeHost host = new() { Failure = new IOException("Read-only") };
        FakeMessageBoxes messageBoxes = new() { ConfirmWithCancelResult = true };
        GitIgnoreEditorViewModel viewModel = new(new GitIgnoreStrings(), new GitIgnoreModelStrings(), host, messageBoxes);
        viewModel.Editor.Text = "*.log";

        viewModel.CanClose().Should().BeFalse();

        messageBoxes.Errors.Should().ContainSingle().Which.Should().StartWith("Failed to save .gitignore.").And.EndWith("Read-only");
    }

    [Test]
    public void Links_open_the_pattern_sites()
    {
        FakeHost host = new();
        GitIgnoreEditorViewModel viewModel = Create(host);

        viewModel.OpenPatternsCommand.Execute(null);
        viewModel.OpenGeneratorCommand.Execute(null);

        host.Urls.Should().Equal("https://github.com/github/gitignore", "https://www.gitignore.io/");
    }

    [Test]
    public void Change_log_is_read_only_with_markdown_highlighting()
    {
        ChangeLogViewModel viewModel = new(new ChangeLogStrings(), "# Changelog");

        viewModel.ChangeLog.Text.Should().Be("# Changelog");
        viewModel.ChangeLog.IsReadOnly.Should().BeTrue();
        viewModel.ChangeLog.FileName.Should().EndWith(".md");
    }

    private static GitIgnoreEditorViewModel Create(FakeHost host)
        => new(new GitIgnoreStrings(), new GitIgnoreModelStrings(), host, new FakeMessageBoxes());

    internal sealed class FakeHost : IGitIgnoreEditorHost
    {
        public string? Content { get; set; }

        public IReadOnlyList<string>? DefaultPatterns { get; init; }

        public Exception? Failure { get; init; }

        public Action? OnAddPattern { get; set; }

        public List<string> Saved { get; } = [];

        public List<string> Urls { get; } = [];

        public string? Load() => Content;

        public void Save(string text)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Saved.Add(text);
            Content = text;
        }

        public IReadOnlyList<string>? LoadDefaultIgnorePatterns() => DefaultPatterns;

        public void AddPattern() => OnAddPattern?.Invoke();

        public void OpenUrl(string url) => Urls.Add(url);
    }
}
