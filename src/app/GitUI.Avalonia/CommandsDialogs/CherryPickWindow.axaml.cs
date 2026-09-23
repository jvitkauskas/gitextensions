using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormCherryPick</c>.</summary>
public partial class CherryPickWindow : DialogWindow
{
    public CherryPickWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            if (!mergeParents.FocusSelectedParent())
            {
                autoCommitCheckBox.Focus();
            }
        };
    }
}
