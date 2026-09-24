using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The template icons and the list of the changes in submodules of the commit dialog.</summary>
[TestFixture]
public sealed class CommitMenusViewTests : HeadlessTest
{
    [Test]
    public Task A_template_of_a_plugin_has_its_icon() => OnUiThreadAsync(() =>
    {
        CommitViewModelTests.FakeHost host = new()
        {
            Templates = ([new GitCommands.CommitTemplateItem("Plugin", "From a plugin", icon: null, isRegex: false), new GitCommands.CommitTemplateItem("Plain", "Plain", icon: null, isRegex: false)], []),
        };
        host.TemplateIcons["Plugin"] = CreatePng();
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(host);
        CommitWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        List<MenuItem> items = [.. window.OpenCommitTemplatesMenu().OfType<MenuItem>()];

        items[0].Icon.Should().BeOfType<Image>().Which.Source.Should().NotBeNull();
        items[1].Icon.Should().BeNull();
        window.Close();
    });

    [Test]
    public Task The_list_of_changes_in_submodules_replaces_the_message() => OnUiThreadAsync(() =>
    {
        CommitViewModelTests.FakeHost host = new() { SubmodulesChangesMessage = "Submodule sub updated" };
        (CommitViewModel viewModel, _) = CommitViewModelTests.Create(host);
        CommitWindow window = new() { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        _ = viewModel.InitializeAsync();
        Dispatcher.UIThread.RunJobs();

        Button button = window.FindControl<Button>("commitMessageButton")!;
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        MenuItem generate = window.CommitMessageMenuItems.OfType<MenuItem>().Single(i => (string?)i.Header == "Generate a list of changes in submodules");
        button.ContextMenu!.Close();

        generate.Command!.Execute(null);

        viewModel.Message.Text.Should().Be("Submodule sub updated");
        host.Shown.Should().Contain("submodules of c.txt", "the staged files are listed");
        window.Close();
    });

    private static byte[] CreatePng()
    {
        using Stream asset = AssetLoader.Open(new Uri("avares://GitUI.Avalonia/Assets/Settings.png"));
        using MemoryStream stream = new();
        asset.CopyTo(stream);
        return stream.ToArray();
    }
}
