using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormAddFiles</c>.</summary>
public partial class AddFilesWindow : DialogWindow
{
    public AddFilesWindow()
    {
        InitializeComponent();
        Opened += (_, _) => filterTextBox.Focus();
    }
}
