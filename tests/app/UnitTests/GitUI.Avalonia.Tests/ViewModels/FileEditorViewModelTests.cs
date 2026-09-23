using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the text editor and the file editors (phase 3).</summary>
[TestFixture]
public sealed class FileEditorViewModelTests
{
    [Test]
    public void Text_editor_tracks_changes_since_loaded_or_saved()
    {
        TextEditorViewModel editor = new();
        int loaded = 0;
        editor.TextLoaded += (_, _) => loaded++;

        editor.Load("line 1\nline 2", "file.cs", line: 2);
        editor.HasChanges.Should().BeFalse();
        editor.FileName.Should().Be("file.cs");
        editor.LineToShow.Should().Be(2);
        loaded.Should().Be(1);

        editor.Text = "changed";
        editor.HasChanges.Should().BeTrue();
        editor.MarkSaved();
        editor.HasChanges.Should().BeFalse();
    }

    [Test]
    public void Repo_file_editor_saves_with_a_final_new_line_and_closes()
    {
        FakeRepoFileHost host = new() { Content = "*.jpg binary" };
        RepoFileEditorViewModel viewModel = new(new GitAttributesEditorStrings(), ".gitattributes", host, new FakeMessageBoxes());
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Editor.Text.Should().Be("*.jpg binary");
        viewModel.Editor.Text = "*.png binary";
        viewModel.SaveCommand.Execute(null);

        host.Saved.Should().Equal("*.png binary" + Environment.NewLine);
        closed.Should().BeTrue();
        viewModel.CanClose().Should().BeTrue("the changes are saved");
    }

    [TestCase(true, true, 1)]
    [TestCase(false, true, 0)]
    [TestCase(null, false, 0)]
    public void Repo_file_editor_asks_to_save_unsaved_changes_when_closing(bool? answer, bool closes, int saves)
    {
        FakeRepoFileHost host = new();
        FakeMessageBoxes messageBoxes = new() { ConfirmWithCancelResult = answer };
        RepoFileEditorViewModel viewModel = new(new MailMapEditorStrings(), ".mailmap", host, messageBoxes);
        viewModel.CanClose().Should().BeTrue("there is nothing to save");

        viewModel.Editor.Text = "Alice <alice@example.org>";
        viewModel.CanClose().Should().Be(closes);

        messageBoxes.Confirmations.Should().Equal("Save changes to .mailmap?");
        host.Saved.Should().HaveCount(saves);
    }

    [Test]
    public void Repo_file_editor_keeps_the_dialog_open_when_saving_fails()
    {
        FakeRepoFileHost host = new() { Failure = new IOException("Access denied") };
        FakeMessageBoxes messageBoxes = new();
        RepoFileEditorViewModel viewModel = new(new GitAttributesEditorStrings(), ".gitattributes", host, messageBoxes);
        viewModel.Editor.Text = "*.jpg binary";

        viewModel.CanClose().Should().BeFalse();

        messageBoxes.Errors.Should().ContainSingle().Which.Should().StartWith("Failed to save .gitattributes.").And.EndWith("Access denied");
    }

    [Test]
    public void File_editor_saves_only_changes()
    {
        FakeFileHost host = new();
        FileEditorViewModel viewModel = Create(host, new FakeMessageBoxes());

        viewModel.Title.Should().Be("COMMIT_EDITMSG");
        viewModel.Editor.LineToShow.Should().Be(3);
        viewModel.SaveCommand.CanExecute(null).Should().BeFalse();

        viewModel.Editor.Text = "Fix the bug";
        viewModel.SaveCommand.CanExecute(null).Should().BeTrue();
        viewModel.SaveCommand.Execute(null);

        host.Saved.Should().Equal(("COMMIT_EDITMSG", "Fix the bug"));
        viewModel.SaveCommand.CanExecute(null).Should().BeFalse();
        viewModel.CanClose().Should().BeTrue();
        viewModel.Accepted.Should().BeTrue("the file is saved");
    }

    [TestCase(true, true, true)]
    [TestCase(false, true, false)]
    [TestCase(null, false, false)]
    public void File_editor_asks_to_save_unsaved_changes_when_closing(bool? answer, bool closes, bool accepted)
    {
        FakeFileHost host = new();
        FakeMessageBoxes messageBoxes = new() { ConfirmWithCancelResult = answer };
        FileEditorViewModel viewModel = Create(host, messageBoxes);
        viewModel.Editor.Text = "Fix the bug";

        viewModel.CanClose().Should().Be(closes);

        viewModel.Accepted.Should().Be(accepted);
        host.Saved.Should().HaveCount(answer == true ? 1 : 0);
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void File_editor_closes_without_saving_or_stays_open_when_saving_fails(bool closeAnyway, bool closes)
    {
        FakeFileHost host = new() { Failure = new IOException("Disk full") };
        FakeMessageBoxes messageBoxes = new() { ConfirmWithCancelResult = true, ConfirmResult = closeAnyway };
        FileEditorViewModel viewModel = Create(host, messageBoxes);
        viewModel.Editor.Text = "Fix the bug";

        viewModel.CanClose().Should().Be(closes);

        messageBoxes.Confirmations.Should().HaveCount(2).And.EndWith($"Cannot save file:{Environment.NewLine}Disk full");
    }

    private static FileEditorViewModel Create(FakeFileHost host, FakeMessageBoxes messageBoxes)
        => new(new FileEditorStrings(), "COMMIT_EDITMSG", "Initial\n\n# comment", showWarning: false, readOnly: false, lineNumber: 3, "Error", host, messageBoxes);

    private sealed class FakeRepoFileHost : IRepoFileEditorHost
    {
        public string Content { get; init; } = "";

        public Exception? Failure { get; init; }

        public List<string> Saved { get; } = [];

        public string Load() => Content;

        public void Save(string text)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Saved.Add(text);
        }
    }

    private sealed class FakeFileHost : IFileEditorHost
    {
        public Exception? Failure { get; init; }

        public List<(string FileName, string Text)> Saved { get; } = [];

        public void Save(string fileName, string text)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Saved.Add((fileName, text));
        }
    }
}
