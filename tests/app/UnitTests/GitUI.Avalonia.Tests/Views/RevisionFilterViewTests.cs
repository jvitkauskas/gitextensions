using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the revision filter dialog (phase 2, batch 7).</summary>
[TestFixture]
public sealed class RevisionFilterViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        RevisionFilterWindow window = new()
        {
            DataContext = new RevisionFilterViewModel(
                new RevisionFilterStrings(), RevisionFilterViewModelTests.Empty with { ByAuthor = true, Author = "Alice", ShowFullHistory = true }, 100_000),
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), $"revision-filter-{theme}");
        window.Close();
    });

    [Test]
    public Task Check_boxes_enable_their_inputs() => OnUiThreadAsync(() =>
    {
        RevisionFilterViewModel viewModel = new(new RevisionFilterStrings(), RevisionFilterViewModelTests.Empty, 100_000);
        RevisionFilterWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.FindControl<CalendarDatePicker>("sinceDatePicker")!.IsEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("simplifyMergesCheckBox")!.IsEnabled.Should().BeFalse();

        window.FindControl<CheckBox>("sinceCheckBox")!.IsChecked = true;
        window.FindControl<CheckBox>("fullHistoryCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        window.FindControl<CalendarDatePicker>("sinceDatePicker")!.IsEnabled.Should().BeTrue();
        window.FindControl<CheckBox>("simplifyMergesCheckBox")!.IsEnabled.Should().BeTrue();
        window.Close();
    });
}
