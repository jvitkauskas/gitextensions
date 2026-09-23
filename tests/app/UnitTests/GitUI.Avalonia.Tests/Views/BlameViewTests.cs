using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Controls.Blame;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.Blame;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the blame and its dialog.</summary>
[TestFixture]
[SetCulture("en-US")]
public sealed class BlameViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        BlameDialogViewModel viewModel = new(new BlameStrings(), new BlameViewModelTests.FakeHost(), new CommitInfoViewTests.FakeHost(), "src/file.cs", BlameViewModelTests.Revision, initialLine: 3);
        BlameWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        viewModel.Blame.HoverLine(3);
        Dispatcher.UIThread.RunJobs();

        window.Title.Should().Be("Blame (src/file.cs)");
        window.Blame.Editor.Text.Should().StartWith("namespace Sample;");
        window.Blame.Editor.TextArea.Caret.Line.Should().Be(3);
        window.Blame.Gutter.Bounds.Width.Should().BeGreaterThan(100, "the author lines are shown");
        SaveScreenshot(window.CaptureRenderedFrame(), $"blame-{theme}");
        window.Close();
    });

    [Test]
    public Task The_caret_selects_the_commit_and_the_menu_follows_the_line() => OnUiThreadAsync(() =>
    {
        BlameViewModelTests.FakeHost host = new();
        BlameViewModel viewModel = BlameViewModelTests.Create(host);
        BlameView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 700, Height = 400 };
        window.Show();
        _ = viewModel.LoadAsync(BlameViewModelTests.Revision, children: null, "src/file.cs");
        Dispatcher.UIThread.RunJobs();

        view.Editor.TextArea.Caret.Line = 5;
        host.Revisions.Should().Equal(BlameViewModelTests.Old.ObjectId);

        view.OpenMenuFor(3);
        MenuItem blameRevision = view.Menu.Items.OfType<MenuItem>().First();
        blameRevision.Header.Should().Be("Blame _this revision");
        blameRevision.IsEnabled.Should().BeFalse("no grid lists the revisions");
        MenuItem showChanges = view.Menu.Items.OfType<MenuItem>().ElementAt(2);
        showChanges.IsEnabled.Should().BeTrue();
        showChanges.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.CommitDiffs.Should().Equal(BlameViewModelTests.Recent.ObjectId);

        view.OpenMenuFor(0);
        showChanges.IsEnabled.Should().BeFalse("not on a line");
        window.Close();
    });

    [Test]
    public Task The_line_numbers_follow_the_setting() => OnUiThreadAsync(() =>
    {
        BlameViewModel viewModel = BlameViewModelTests.Create(new BlameViewModelTests.FakeHost { Options = new BlameDisplayOptions(ShowLineNumbers: true) });
        BlameView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 700, Height = 400 };
        window.Show();
        _ = viewModel.LoadAsync(BlameViewModelTests.Revision, children: null, "src/file.cs");
        Dispatcher.UIThread.RunJobs();

        view.Editor.ShowLineNumbers.Should().BeTrue();
        view.Editor.TextArea.LeftMargins[^1].Should().BeSameAs(view.Gutter, "the author lines are after the line numbers");
        window.Close();
    });
}
