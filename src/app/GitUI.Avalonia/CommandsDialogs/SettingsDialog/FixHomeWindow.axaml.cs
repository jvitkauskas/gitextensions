using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog;

/// <summary>Avalonia port of <c>FormFixHome</c>.</summary>
public partial class FixHomeWindow : DialogWindow
{
    public FixHomeWindow()
    {
        InitializeComponent();

        // As FormFixHome.OnLoad: choose a located global config and tell the user, over the dialog.
        Opened += (_, _) => (DataContext as FixHomeViewModel)?.SelectLocatedGitConfig();
    }
}
