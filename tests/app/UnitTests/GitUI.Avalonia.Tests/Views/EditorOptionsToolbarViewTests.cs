using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Editor;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The options toolbar of the file editors (the <c>fileviewerToolbar</c> of their WinForms <c>FileViewer</c>).</summary>
[TestFixture]
public sealed class EditorOptionsToolbarViewTests : HeadlessTest
{
    [Test]
    public void Nonprinting_characters_are_shown_and_saved_and_the_encoding_reads_the_file_again()
    {
        TextEditorViewModel editor = new();
        editor.Load("text in UTF-8", "notes.txt");
        OptionsHost host = new() { ShowNonPrintingChars = true };
        EncodingReader reader = new();
        TextEditorOptionsViewModel options = new(editor, host, reader);

        editor.ShowWhitespace.Should().BeTrue("as saved");
        options.SelectedEncoding.Should().Be("Unicode (UTF-8)");
        reader.Reads.Should().BeEmpty("the file is read in its encoding already");

        options.ShowNonPrintingChars = false;
        editor.ShowWhitespace.Should().BeFalse();
        host.ShowNonPrintingChars.Should().BeFalse();

        options.SelectedEncoding = "Western European (Windows)";
        reader.Reads.Should().Equal("Western European (Windows)");
        editor.Text.Should().Be("text in Western European (Windows)");
        editor.FileName.Should().Be("notes.txt");

        // Reading the file again would lose the changes.
        editor.Text += "!";
        options.CanChangeEncoding.Should().BeFalse();
        options.SelectedEncoding = "Unicode (UTF-8)";
        reader.Reads.Should().HaveCount(1);

        options.OpenSettingsCommand.Execute(null);
        host.SettingsOpened.Should().Be(1);
    }

    [Test]
    public Task The_toolbar_shows_over_the_text_of_the_file_editor() => OnUiThreadAsync(() =>
    {
        FileEditorViewModel viewModel = new(new FileEditorStrings(), @"C:\repo\notes.txt", "some text", showWarning: false, readOnly: false, lineNumber: null,
            "Error", new NoFileHost(), new ProcessViewModelTests.FakeMessageBoxes());
        viewModel.Options = new TextEditorOptionsViewModel(viewModel.Editor, new OptionsHost(), new EncodingReader());
        FileEditorWindow window = new() { DataContext = viewModel, Width = 600, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        EditorOptionsToolbar toolbar = window.GetVisualDescendants().OfType<EditorOptionsToolbar>().Single();
        toolbar.Toolbar.IsVisible.Should().BeFalse();

        window.MouseMove(new Point(200, 200));
        Dispatcher.UIThread.RunJobs();

        toolbar.Toolbar.IsVisible.Should().BeTrue();
        toolbar.GetVisualDescendants().OfType<ComboBox>().Single().IsEffectivelyVisible.Should().BeTrue();
        window.Close();
    });

    private sealed class OptionsHost : ITextEditorOptionsHost
    {
        public bool ShowNonPrintingChars { get; set; }

        public int SettingsOpened { get; private set; }

        public void OpenSettings() => SettingsOpened++;
    }

    private sealed class EncodingReader : IEncodingReader
    {
        public List<string> Reads { get; } = [];

        public IReadOnlyList<string> AvailableEncodings { get; } = ["Unicode (UTF-8)", "Western European (Windows)"];

        public string EncodingName { get; private set; } = "Unicode (UTF-8)";

        public string Read(string encodingName)
        {
            Reads.Add(encodingName);
            EncodingName = encodingName;
            return $"text in {encodingName}";
        }
    }

    private sealed class NoFileHost : IFileEditorHost
    {
        public void Save(string fileName, string text)
        {
        }
    }
}
