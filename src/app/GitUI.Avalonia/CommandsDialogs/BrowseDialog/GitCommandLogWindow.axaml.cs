using Avalonia.Input;
using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>Avalonia port of <c>FormGitCommandLog</c>, a modeless window.</summary>
public partial class GitCommandLogWindow : DialogWindow
{
    public GitCommandLogWindow()
    {
        InitializeComponent();

        // As the Load and FormClosed events: the lists follow the log while the window is open.
        Opened += (_, _) =>
        {
            (DataContext as GitCommandLogViewModel)?.Start();
            logItems.Focus();
        };
        Closed += (_, _) => (DataContext as GitCommandLogViewModel)?.Dispose();

        // The ShortcutKeys of the context menus, while their list has the focus (before the list handles Ctrl+C).
        logItems.AddHandler(KeyDownEvent, OnLogItemsKeyDown, RoutingStrategies.Tunnel);
        cacheItems.AddHandler(KeyDownEvent, OnCacheItemsKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnLogItemsKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not GitCommandLogViewModel viewModel || e.KeyModifiers != KeyMapping.CommandModifier)
        {
            return;
        }

        System.Windows.Input.ICommand? command = e.Key switch
        {
            Key.S => viewModel.SaveToFileCommand,
            Key.C => viewModel.CopyCommandLineCommand,
            Key.L => viewModel.ClearLogCommand,
            _ => null,
        };
        if (command is not null)
        {
            e.Handled = true;
            command.Execute(null);
        }
    }

    private void OnCacheItemsKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is GitCommandLogViewModel viewModel && e.Key == Key.L && e.KeyModifiers == KeyMapping.CommandModifier)
        {
            e.Handled = true;
            viewModel.ClearCacheCommand.Execute(null);
        }
    }
}
