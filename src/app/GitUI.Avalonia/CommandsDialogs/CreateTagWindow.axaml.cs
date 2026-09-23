using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormCreateTag</c>.</summary>
public partial class CreateTagWindow : DialogWindow
{
    public CreateTagWindow()
    {
        InitializeComponent();
        Opened += (_, _) => tagNameTextBox.Focus();
    }
}
