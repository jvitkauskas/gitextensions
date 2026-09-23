using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Controls;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the pull dialog (phase 5).</summary>
[TestFixture]
public sealed class PullViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Capture(new PullWindow { DataContext = PullViewModelTests.Create(new PullViewModelTests.FakePullHost(), PullViewModelTests.Options()) }, $"pull-{theme}");
        Capture(
            new PullWindow
            {
                DataContext = PullViewModelTests.Create(
                    new PullViewModelTests.FakePullHost(), PullViewModelTests.Options(pullAction: GitPullAction.Fetch) with { IsShallow = true }),
            },
            $"pull-fetch-{theme}");
    });

    [Test]
    public Task The_options_follow_the_merge_option() => OnUiThreadAsync(() =>
    {
        PullViewModel viewModel = PullViewModelTests.Create(new PullViewModelTests.FakePullHost(), PullViewModelTests.Options());
        PullWindow window = Show(new PullWindow { DataContext = viewModel });
        TextBox localBranch = window.FindControl<TextBox>("localBranchTextBox")!;
        HelpImageView helpImage = window.FindControl<HelpImageView>("helpImage")!;

        window.Title.Should().Be("Pull (C:/repo)");
        localBranch.IsEnabled.Should().BeFalse();
        localBranch.Text.Should().Be("main");
        window.FindControl<CheckBox>("pruneCheckBox")!.IsEnabled.Should().BeFalse();
        window.FindControl<CheckBox>("unshallowCheckBox")!.IsVisible.Should().BeFalse();
        window.FindControl<ComboBox>("pullSourceComboBox")!.IsEnabled.Should().BeFalse();
        window.FindControl<ComboBox>("pullSourceComboBox")!.Text.Should().Be("https://example.com/origin.git");
        helpImage.Image2.Should().NotBeNull("merging shows the fast forward scenario while hovered");
        object? mergeImage = helpImage.Image1;

        window.FindControl<RadioButton>("fetchRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        viewModel.IsFetch.Should().BeTrue();
        viewModel.IsMerge.Should().BeFalse();
        window.Title.Should().Be("Fetch (C:/repo)");
        localBranch.IsEnabled.Should().BeTrue();
        localBranch.Text.Should().BeEmpty();
        window.FindControl<CheckBox>("pruneCheckBox")!.IsEnabled.Should().BeTrue();
        helpImage.Image1.Should().NotBeSameAs(mergeImage);
        helpImage.Image2.Should().BeNull();

        window.FindControl<RadioButton>("pullFromUrlRadioButton")!.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        ComboBox pullSource = window.FindControl<ComboBox>("pullSourceComboBox")!;
        pullSource.IsEnabled.Should().BeTrue();
        pullSource.ItemCount.Should().Be(1);
        pullSource.Text.Should().Be("https://example.com/origin.git", "the text is kept when the recent URLs are listed");
        window.FindControl<ComboBox>("remotesComboBox")!.IsEnabled.Should().BeFalse();
        window.Close();
    });

    [Test]
    public Task The_remote_branches_are_listed_when_the_list_drops_down() => OnUiThreadAsync(() =>
    {
        PullViewModel viewModel = PullViewModelTests.Create(new PullViewModelTests.FakePullHost(), PullViewModelTests.Options(remoteBranch: "feature"));
        PullWindow window = Show(new PullWindow { DataContext = viewModel });
        ComboBox remoteBranches = window.FindControl<ComboBox>("remoteBranchComboBox")!;

        remoteBranches.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        remoteBranches.ItemCount.Should().Be(3);
        remoteBranches.Text.Should().Be("feature");
        viewModel.RemoteBranch.Should().Be("feature");
        window.Close();
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
