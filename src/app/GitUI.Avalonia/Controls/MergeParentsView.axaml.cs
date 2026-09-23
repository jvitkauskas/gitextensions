using Avalonia.Controls;

namespace GitUI.Avalonia.Controls;

/// <summary>The list of parents of a merge commit in the cherry-pick and revert dialogs.</summary>
public partial class MergeParentsView : UserControl
{
    public MergeParentsView()
    {
        InitializeComponent();
    }

    /// <summary>Focuses the selected parent, if the list is shown.</summary>
    public bool FocusSelectedParent()
    {
        if (!IsVisible)
        {
            return false;
        }

        (parentsListBox.ContainerFromIndex(Math.Max(0, parentsListBox.SelectedIndex)) ?? (Control)parentsListBox).Focus();
        return true;
    }
}
