using System.Text;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 3: the file editors on the Avalonia text editor, with real files.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Gitattributes_editor_saves_the_file_with_a_final_new_line()
    {
        string path = Path.Combine(_referenceRepository.Module.WorkingDir, ".gitattributes");
        File.WriteAllText(path, "*.jpg binary\n");

        string? loaded = null;
        DriveNextDialog(window =>
        {
            RepoFileEditorViewModel viewModel = (RepoFileEditorViewModel)window.DataContext!;
            loaded = viewModel.Editor.Text;
            viewModel.Editor.Text = "*.jpg binary\n*.sln binary";
            Capture(window, "gitattributes-editor");
            viewModel.SaveCommand.Execute(null);
        });

        _commands.StartEditGitAttributesDialog(_owner).Should().BeTrue();

        loaded.Should().Be("*.jpg binary\n");
        File.ReadAllText(path).Should().Be("*.jpg binary\n*.sln binary" + Environment.NewLine);
    }

    [Test]
    public void File_editor_saves_the_changes_and_keeps_the_byte_order_mark()
    {
        string path = Path.Combine(_referenceRepository.Module.WorkingDir, "notes.txt");
        File.WriteAllText(path, "first line\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        DriveNextDialog(window =>
        {
            FileEditorViewModel viewModel = (FileEditorViewModel)window.DataContext!;
            viewModel.Editor.Text.Should().Be("first line\n");
            viewModel.Editor.Text = "first line\nsecond line\n";
            Capture(window, "file-editor");
            viewModel.SaveCommand.Execute(null);
            window.Close();
        });

        _commands.StartFileEditorDialog(path).Should().BeTrue("the file is saved");

        byte[] bytes = File.ReadAllBytes(path);
        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).Should().Be("first line\nsecond line\n");
    }

    [Test]
    public void File_editor_of_a_missing_file_reports_it_and_is_not_shown()
    {
        GitExtUtils.GitUI.UiTimer acknowledgeError = new() { Interval = 100 };
        acknowledgeError.Tick += (_, _) => CloseTopLevelWindow("Error", except: 0);
        acknowledgeError.Start();
        try
        {
            _commands.StartFileEditorDialog(Path.Combine(_referenceRepository.Module.WorkingDir, "missing.txt")).Should().BeFalse();
        }
        finally
        {
            acknowledgeError.Dispose();
        }
    }
}
