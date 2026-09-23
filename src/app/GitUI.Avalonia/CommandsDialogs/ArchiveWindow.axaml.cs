using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormArchive</c>.</summary>
public partial class ArchiveWindow : DialogWindow
{
    public ArchiveWindow()
    {
        InitializeComponent();
        Opened += (_, _) => saveButton.Focus();
    }
}
