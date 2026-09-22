using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormInit</c>.</summary>
public partial class InitWindow : DialogWindow
{
    public InitWindow()
    {
        InitializeComponent();
        Opened += (_, _) => directoryComboBox.Focus();
    }
}
