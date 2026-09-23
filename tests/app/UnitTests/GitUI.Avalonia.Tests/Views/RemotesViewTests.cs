using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitCommands.Git;
using GitCommands.Remotes;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs;
using NSubstitute;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;
using static GitUI.AvaloniaTests.ViewModels.SmallDialogViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the remotes dialog (phase 2, batch 10).</summary>
[TestFixture]
public sealed class RemotesViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        RemotesWindow window = new() { DataContext = Create("upstream", isPuttyEnabled: true) };

        window.Show();
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), $"remotes-{theme}");

        ((RemotesViewModel)window.DataContext!).SelectedTabIndex = 1;
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), $"remotes-pull-behavior-{theme}");
        window.Close();
    });

    [Test]
    public Task Shows_the_group_headers_the_push_url_and_the_color_of_the_selected_remote() => OnUiThreadAsync(() =>
    {
        RemotesViewModel viewModel = Create("upstream", isPuttyEnabled: false);
        RemotesWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        TextBlock[] texts = [.. window.FindControl<ListBox>("remotesListBox")!.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible)];
        texts.Select(t => t.Text).Should().Equal("Active", "origin", "upstream", "Inactive", "old");
        texts.Single(t => t.Text == "old").Classes.Should().Contain("disabledRemote");

        window.FindControl<ComboBox>("pushUrlComboBox")!.IsVisible.Should().BeTrue();
        ((ISolidColorBrush)window.FindControl<Border>("colorSwatch")!.Background!).Color.Should().Be(Colors.Red);
        window.FindControl<Button>("resetColorButton")!.IsVisible.Should().BeTrue();
        window.FindControl<Button>("testConnectionButton")!.IsEffectivelyVisible.Should().BeFalse("PuTTY is not used");

        viewModel.SelectedRemote = viewModel.Remotes[0];
        Dispatcher.UIThread.RunJobs();
        window.FindControl<ComboBox>("pushUrlComboBox")!.IsVisible.Should().BeFalse();
        window.FindControl<Button>("resetColorButton")!.IsVisible.Should().BeFalse();
        window.Close();
    });

    private static RemotesViewModel Create(string preselectRemote, bool isPuttyEnabled)
    {
        IConfigFileRemoteSettingsManager manager = Substitute.For<IConfigFileRemoteSettingsManager>();
        manager.LoadRemotes(true).Returns(
        [
            new ConfigFileRemote { Name = "origin", Url = "https://github.com/owner/repo.git" },
            new ConfigFileRemote { Name = "old", Url = "https://example.org/legacy.git", Disabled = true },
            new ConfigFileRemote { Name = "upstream", Url = "https://github.com/upstream/repo.git", PushUrl = "git@github.com:upstream/repo.git", Color = "#FF0000" },
        ]);
        RemotesViewModel viewModel = new(
            new RemotesStrings(),
            manager,
            new FakeBranchNameNormaliser(),
            new GitBranchNameOptions("_"),
            isPuttyEnabled,
            showAdvancedOptions: true,
            [],
            new RemotesViewModelTests.FakeRemotesHost(),
            new FakeMessageBoxes(),
            new FakeFileDialogs());
        viewModel.Initialize(preselectRemote);
        return viewModel;
    }
}
