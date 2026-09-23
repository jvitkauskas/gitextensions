using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using FakeHost = GitUI.AvaloniaTests.ViewModels.VerifyViewModelTests.FakeHost;
using FakeMessageBoxes = GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests.FakeMessageBoxes;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the verify database dialog and the text viewer.</summary>
[TestFixture]
public sealed class VerifyViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        VerifyWindow window = Show(out VerifyViewModel viewModel);
        viewModel.SelectedObject = viewModel.LostObjects[1];
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), $"verify-{theme}");
        window.Close();

        TextViewerWindow viewer = new() { DataContext = new TextViewerViewModel(new TextViewerStrings(), "{\n  \"key\": \"value\"\n}\n", "LOST_FOUND_1.json", isReadOnly: true) };
        viewer.Show();
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(viewer.CaptureRenderedFrame(), $"text-viewer-{theme}");
        viewer.Close();
    });

    [Test]
    public Task Header_check_box_selects_all_and_commit_columns_follow_the_filter() => OnUiThreadAsync(() =>
    {
        VerifyWindow window = Show(out VerifyViewModel viewModel);
        DataGrid grid = window.FindControl<DataGrid>("lostObjectsGrid")!;

        ((CheckBox)grid.Columns[0].Header!).IsChecked = true;
        viewModel.LostObjects.Should().OnlyContain(o => o.IsSelected);

        grid.Columns[1].Header.Should().Be("Date");
        grid.Columns[3].IsVisible.Should().BeTrue();
        viewModel.ShowOtherObjects = true;
        viewModel.ShowCommitsAndTags = false;
        grid.Columns[3].IsVisible.Should().BeFalse("the subject is only shown with commits");
        window.Close();
    });

    private static VerifyWindow Show(out VerifyViewModel viewModel)
    {
        viewModel = new VerifyViewModel(new VerifyStrings(), new FakeHost(), new FakeMessageBoxes());
        VerifyWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
