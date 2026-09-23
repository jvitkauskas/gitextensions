using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormRevertCommit</c>.</summary>
public partial class RevertCommitWindow : DialogWindow
{
    public RevertCommitWindow()
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
