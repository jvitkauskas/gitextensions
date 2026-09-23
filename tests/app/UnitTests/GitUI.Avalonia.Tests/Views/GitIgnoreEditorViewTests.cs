using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using FakeHost = GitUI.AvaloniaTests.ViewModels.GitIgnoreEditorViewModelTests.FakeHost;
using FakeMessageBoxes = GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests.FakeMessageBoxes;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the .gitignore editor and the change log.</summary>
[TestFixture]
public sealed class GitIgnoreEditorViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(
            new GitIgnoreEditorWindow { DataContext = new GitIgnoreEditorViewModel(new GitIgnoreStrings(), new GitIgnoreModelStrings(), new FakeHost { Content = "bin/\nobj/\n*.user\n" }, new FakeMessageBoxes()) },
            $"gitignore-editor-{theme}");
        Capture(
            new ChangeLogWindow { DataContext = new ChangeLogViewModel(new ChangeLogStrings(), "Changelog\n=========\n\n### Version 6.0\n\n* Fixed a crash\n* **New** Avalonia dialogs\n") },
            $"change-log-{theme}");
    });

    [Test]
    public Task Editor_shows_the_file_and_the_title_of_the_edited_file() => OnUiThreadAsync(() =>
    {
        GitIgnoreEditorWindow window = new() { DataContext = new GitIgnoreEditorViewModel(new GitIgnoreStrings(), new GitLocalExcludeModelStrings(), new FakeHost { Content = "*.log" }, new FakeMessageBoxes()) };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.Title.Should().Be("Edit .git/info/exclude");
        window.FindControl<TextEditorView>("editorView")!.Editor.Text.Should().Be("*.log");
        window.Close();
    });

    private static void Capture(DialogWindow window, string name)
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }
}
