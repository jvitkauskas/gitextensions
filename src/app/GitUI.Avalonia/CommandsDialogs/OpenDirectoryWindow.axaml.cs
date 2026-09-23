using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormOpenDirectory</c>.</summary>
public partial class OpenDirectoryWindow : DialogWindow
{
    public OpenDirectoryWindow()
    {
        InitializeComponent();
        Opened += (_, _) => directoryComboBox.Focus();
    }
}
