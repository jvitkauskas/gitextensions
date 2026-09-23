using GitCommands;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 3: the .gitignore / .git/info/exclude editor on the Avalonia text editor, with a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Gitignore_editor_saves_the_file(bool localExclude)
    {
        string path = localExclude
            ? Path.Combine(_referenceRepository.Module.ResolveGitInternalPath("info"), "exclude")
            : Path.Combine(_referenceRepository.Module.WorkingDir, ".gitignore");
        File.WriteAllText(path, "bin/\n");

        string? title = null;
        string? loaded = null;
        DriveNextDialog(window =>
        {
            GitIgnoreEditorViewModel viewModel = (GitIgnoreEditorViewModel)window.DataContext!;
            title = window.Title;
            loaded = viewModel.Editor.Text;
            viewModel.Editor.Text = "bin/\nobj/";
            Capture(window, localExclude ? "local-exclude-editor" : "gitignore-editor");
            viewModel.SaveCommand.Execute(null);
        });

        _commands.StartEditGitIgnoreDialog(_owner, localExclude).Should().BeTrue();

        title.Should().Be(localExclude ? "Edit .git/info/exclude" : "Edit .gitignore");
        loaded.Should().Be("bin/\n");
        File.ReadAllText(path, GitModule.SystemEncoding).Should().Be("bin/\nobj/" + Environment.NewLine);
    }
}
