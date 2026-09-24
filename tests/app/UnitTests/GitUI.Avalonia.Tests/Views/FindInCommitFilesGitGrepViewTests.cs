using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.Editor;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.Editor;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the git grep prompt (port of <c>FormFindInCommitFilesGitGrep</c>).</summary>
[TestFixture]
public sealed class FindInCommitFilesGitGrepViewTests : HeadlessTest
{
    [Test]
    public Task Enter_searches_and_closing_ends_a_search_without_its_box() => OnUiThreadAsync(() =>
    {
        (FindInCommitFilesGitGrepWindow window, FindInCommitFilesGitGrepViewModelTests.FakeHost host) = Show(showSearchBox: false);
        ComboBox expression = window.FindControl<ComboBox>("expressionComboBox")!;

        expression.Text.Should().Be("TODO");
        expression.ItemCount.Should().Be(2);
        window.FindControl<CheckBox>("matchWholeWordCheckBox")!.IsChecked.Should().BeTrue();

        expression.Text = "FIXME";
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        host.Actions.Should().Equal("search FIXME");

        window.FindControl<CheckBox>("matchCaseCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        host.IgnoreCase.Should().BeFalse();

        window.Close();
        host.Actions.Should().Equal("search FIXME", "search ");
    });

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (FindInCommitFilesGitGrepWindow window, _) = Show(showSearchBox: true);

        SaveScreenshot(window.CaptureRenderedFrame(), $"find-in-commit-files-git-grep-{theme}");
        window.Close();
    });

    private static (FindInCommitFilesGitGrepWindow Window, FindInCommitFilesGitGrepViewModelTests.FakeHost Host) Show(bool showSearchBox)
    {
        FindInCommitFilesGitGrepViewModelTests.FakeHost host = new() { IgnoreCase = true, MatchWholeWord = true, UserArguments = "--untracked" };
        FindInCommitFilesGitGrepViewModel viewModel = new(new FindInCommitFilesGitGrepStrings(), host);
        viewModel.SetState("TODO", ["TODO", "FIXME"], showSearchBox);
        FindInCommitFilesGitGrepWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, host);
    }
}
