using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormFormatPatch</c>.</summary>
public partial class FormatPatchWindow : DialogWindow
{
    public FormatPatchWindow()
    {
        InitializeComponent();
        Opened += (_, _) => revisionGrid.Focus();
    }
}
