using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;
using GitUI;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 7: the plugins of plugin API v1 in the Avalonia main window.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void A_plugin_of_API_v1_runs_from_the_plugins_menu_of_the_Avalonia_main_window_with_its_window_as_OwnerForm()
    {
        Environment.SetEnvironmentVariable(AvaloniaUi.EnvironmentVariable, "all,FormBrowse");
        V1OnlyPlugin plugin = new();
        lock (PluginRegistry.Plugins)
        {
            PluginRegistry.Plugins.Add(plugin);
        }

        try
        {
            nint windowHandle = 0;
            bool closed = false;
            DriveNextDialog(window =>
            {
                window.Should().BeOfType<BrowseWindow>();
                window.Closed += (_, _) => closed = true;
                BrowseViewModel viewModel = (BrowseViewModel)window.DataContext!;
                WaitUntil(
                    () => FindPluginItem(viewModel) is not null,
                    () =>
                    {
                        windowHandle = window.NativeHandle;

                        // As a click of the item of the plugin.
                        FindPluginItem(viewModel)!.Invoke!();
                        window.Close();
                    });
            });

            _commands.StartBrowseDialog(_owner).Should().BeTrue();

            WaitForMainWindowToClose(() => closed);

            plugin.Executed.Should().NotBeNull("the plugin ran");
            windowHandle.Should().NotBe(0);
            plugin.OwnerForm.Should().NotBeNull("a v1 plugin shows its dialogs over OwnerForm");
            plugin.OwnerForm!.Handle.Should().Be(windowHandle, "the Avalonia main window owns the dialogs of the plugin");
            plugin.Executed!.Owner.Should().Be(new WindowOwner(windowHandle));
            plugin.Executed.GitModule.WorkingDir.Should().Be(_commands.Module.WorkingDir);
        }
        finally
        {
            lock (PluginRegistry.Plugins)
            {
                PluginRegistry.Plugins.Remove(plugin);
            }
        }

        BrowseMenuItem? FindPluginItem(BrowseViewModel viewModel)
            => viewModel.Menus
                .FirstOrDefault(menu => menu.Header == viewModel.PluginStrings.PluginsMenu.AccessKeyText)?
                .Children?
                .FirstOrDefault(item => item.Header == V1OnlyPlugin.PluginName);
    }

    /// <summary>A plugin of plugin API v1: it implements the v1 interface only, and reads the WinForms owner.</summary>
    private sealed class V1OnlyPlugin : IGitPlugin
    {
        public const string PluginName = "Fake plugin of API v1";

        public GitUIEventArgs? Executed { get; private set; }

        public IWin32Window? OwnerForm { get; private set; }

        public Guid Id { get; } = Guid.NewGuid();

        public string? Name => PluginName;

        public string? Description => PluginName;

        public Image? Icon => null;

        public IGitPluginSettingsContainer? SettingsContainer { get; set; }

        public bool HasSettings => false;

        public IEnumerable<ISetting> GetSettings() => [];

        public void Register(IGitUICommands gitUiCommands)
        {
        }

        public void Unregister(IGitUICommands gitUiCommands)
        {
        }

        public bool Execute(GitUIEventArgs args)
        {
            Executed = args;
            OwnerForm = args.OwnerForm;
            return false;
        }
    }
}
