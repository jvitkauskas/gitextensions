using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using static GitUI.AvaloniaTests.ViewModels.Batch9ViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of phase 2, batch 9: reflog and recent repositories settings.</summary>
[TestFixture]
public sealed class Batch9ViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(new ReflogWindow { DataContext = CreateReflog(isDirty: true) }, $"reflog-{theme}");
        Capture(new RecentReposSettingsWindow { DataContext = new RecentReposSettingsViewModel(new RecentReposSettingsStrings(), Options, new FakeRecentReposHost()) }, $"recent-repos-settings-{theme}");
    });

    [Test]
    public Task Reflog_lists_the_entries_and_hides_the_current_branch_of_a_detached_HEAD() => OnUiThreadAsync(() =>
    {
        ReflogWindow window = Show(new ReflogWindow { DataContext = CreateReflog(isDirty: false, isBranchCheckedOut: false) });

        window.FindControl<DataGrid>("reflogGrid")!.Columns.Select(c => c.Header).Should().Equal("SHA-1", "Ref", "Action");
        window.GetVisualDescendants().OfType<DataGridRow>().Should().HaveCount(2);
        window.FindControl<TextBlock>("dirtyWarning")!.IsVisible.Should().BeFalse();
        window.FindControl<Button>("currentBranchLink")!.IsVisible.Should().BeFalse();
        window.FindControl<ComboBox>("referencesComboBox")!.SelectedItem.Should().Be("HEAD");
        window.Close();
    });

    [Test]
    public Task RecentRepos_shows_anchored_repositories_bold_and_missing_ones_red() => OnUiThreadAsync(() =>
    {
        RecentReposSettingsWindow window = Show(new RecentReposSettingsWindow { DataContext = new RecentReposSettingsViewModel(new RecentReposSettingsStrings(), Options, new FakeRecentReposHost()) });

        TextBlock[] recent = [.. window.FindControl<ListBox>("recentListBox")!.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Classes.Count > 0 || t.Text is "b" or "gone")];
        TextBlock gone = recent.Single(t => t.Text == "gone");
        gone.FontWeight.Should().Be(FontWeight.Bold);
        gone.Classes.Should().Contain("missing");
        recent.Single(t => t.Text == "b").FontWeight.Should().Be(FontWeight.Normal);

        window.FindControl<ListBox>("topListBox")!.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "a").Classes.Should().Contain("anchored");
        window.FindControl<RadioButton>("dontShortenRadioButton")!.IsChecked.Should().BeTrue();
        window.Close();
    });

    private static ReflogViewModel CreateReflog(bool isDirty, bool isBranchCheckedOut = true)
        => new(new ReflogStrings(), ["HEAD", "feature", "origin/main"], "feature", isBranchCheckedOut, isDirty, new FakeReflogHost(), new FakeMessageBoxes());

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
}
