using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the Avalonia text editor and the file editors (phase 3).</summary>
[TestFixture]
public sealed class TextEditorViewTests : HeadlessTest
{
    private const string Code = "namespace Demo;\n\n// A comment\npublic sealed class Example\n{\n    public int Value => 42;\n}\n";

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(new FileEditorWindow { DataContext = CreateFileEditor(showWarning: true) }, $"file-editor-{theme}");
        Capture(
            new RepoFileEditorWindow { DataContext = new RepoFileEditorViewModel(new GitAttributesEditorStrings(), ".gitattributes", new Host("*.jpg binary\n*.sln binary\n"), new FakeMessageBoxes()) },
            $"gitattributes-editor-{theme}");
    });

    [Test]
    public Task Shows_the_loaded_text_with_highlighting_and_reports_edits() => OnUiThreadAsync(() =>
    {
        FileEditorViewModel viewModel = CreateFileEditor(showWarning: false);
        FileEditorWindow window = Show(new FileEditorWindow { DataContext = viewModel });
        AvaloniaEdit.TextEditor editor = window.GetVisualDescendants().OfType<TextEditorView>().Single().Editor;

        editor.Text.Should().Be(Code);
        editor.SyntaxHighlighting!.Name.Should().Be("C#");
        editor.TextArea.Caret.Line.Should().Be(4, "the requested line is shown");
        editor.ShowLineNumbers.Should().BeTrue();
        window.FindControl<Border>("warningPanel")!.IsVisible.Should().BeFalse();

        editor.Document.Insert(0, "// Header\n");
        viewModel.Editor.Text.Should().StartWith("// Header\n");
        viewModel.SaveCommand.CanExecute(null).Should().BeTrue();

        viewModel.Editor.Load("reloaded", "notes.txt");
        Dispatcher.UIThread.RunJobs();
        editor.Text.Should().Be("reloaded");
        editor.SyntaxHighlighting.Should().BeNull();
        viewModel.Editor.HasChanges.Should().BeFalse();
        window.Close();
    });

    [Test]
    public Task Read_only_and_whitespace_options_apply_to_the_editor() => OnUiThreadAsync(() =>
    {
        TextEditorViewModel viewModel = new() { IsReadOnly = true, ShowWhitespace = true };
        viewModel.Load("a\tb c");
        TextEditorView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 300, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        view.Editor.IsReadOnly.Should().BeTrue();
        view.Editor.Options.ShowTabs.Should().BeTrue();
        view.Editor.Options.ShowSpaces.Should().BeTrue();

        viewModel.IsReadOnly = false;
        view.Editor.IsReadOnly.Should().BeFalse();
        window.Close();
    });

    private static FileEditorViewModel CreateFileEditor(bool showWarning)
        => new(new FileEditorStrings(), @"C:\repo\Example.cs", Code, showWarning, readOnly: false, lineNumber: 4, "Error", new Host(""), new FakeMessageBoxes());

    private static T Show<T>(T window)
        where T : DialogWindow
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

    private sealed class Host(string content) : IRepoFileEditorHost, IFileEditorHost
    {
        public string Load() => content;

        public void Save(string text)
        {
        }

        public void Save(string fileName, string text)
        {
        }
    }
}
