using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Controls;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the create branch and create tag dialogs (phase 2, batch 3).</summary>
[TestFixture]
public sealed class CreateRefDialogViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(
            new CreateBranchWindow { DataContext = CreateRefDialogViewModelTests.CreateBranch(new CreateRefDialogViewModelTests.FakeCreateBranchHost(), new CreateBranchOptions("feature")) },
            $"create-branch-{theme}");
        Capture(
            new CreateBranchWindow
            {
                DataContext = CreateRefDialogViewModelTests.CreateBranch(
                    new CreateRefDialogViewModelTests.FakeCreateBranchHost(), new CreateBranchOptions(null, IsOrphanOnly: true), selectCommit: false),
            },
            $"create-branch-orphan-{theme}");
        Capture(new CreateTagWindow { DataContext = CreateRefDialogViewModelTests.CreateTag(new CreateRefDialogViewModelTests.FakeCreateTagHost()) }, $"create-tag-{theme}");
    });

    [Test]
    public Task CreateBranch_orphan_disables_the_commit_picker() => OnUiThreadAsync(() =>
    {
        CreateBranchViewModel viewModel = CreateRefDialogViewModelTests.CreateBranch(new CreateRefDialogViewModelTests.FakeCreateBranchHost(), new CreateBranchOptions("feature"));
        CreateBranchWindow window = Show(new CreateBranchWindow { DataContext = viewModel });
        CommitPickerView picker = window.FindControl<CommitPickerView>("commitPicker")!;

        picker.IsEnabled.Should().BeTrue();
        picker.FindControl<TextBox>("commitHashTextBox")!.Text.Should().Be(CreateRefDialogViewModelTests.Commit.ToShortString());
        picker.FindControl<TextBlock>("commitCountText")!.Text.Should().Be("2 commits behind");

        window.FindControl<CheckBox>("createOrphanCheckBox")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        picker.IsEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("checkoutCheckBox")!.IsEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("clearOrphanCheckBox")!.IsEnabled.Should().BeTrue();
    });

    [Test]
    public Task CreateTag_message_is_enabled_for_annotated_tags() => OnUiThreadAsync(() =>
    {
        CreateTagViewModel viewModel = CreateRefDialogViewModelTests.CreateTag(new CreateRefDialogViewModelTests.FakeCreateTagHost());
        CreateTagWindow window = Show(new CreateTagWindow { DataContext = viewModel });
        TextBox message = window.FindControl<TextBox>("messageTextBox")!;

        message.IsEnabled.Should().BeFalse();
        window.FindControl<ComboBox>("kindComboBox")!.SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        message.IsEnabled.Should().BeTrue();
        window.FindControl<TextBox>("keyIdTextBox")!.IsEnabled.Should().BeFalse();
    });

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
