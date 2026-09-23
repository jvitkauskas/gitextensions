using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.Avalonia.HelperDialogs;

/// <summary>Avalonia port of <c>FormChooseCommit</c>.</summary>
public partial class ChooseCommitWindow : DialogWindow
{
    public ChooseCommitWindow()
    {
        InitializeComponent();

        // As FormChooseCommit.revisionGrid_DoubleClickRevision (and Enter).
        revisionGrid.RevisionActivated += (_, row) => (DataContext as ChooseCommitViewModel)?.Accept(row);
        Opened += (_, _) => revisionGrid.Focus();
    }
}
