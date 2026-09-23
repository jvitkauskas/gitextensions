using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormViewPatch</c>.</summary>
public partial class ViewPatchWindow : DialogWindow
{
    public ViewPatchWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Column headers are not in the logical tree, so they are not bound.
        if (DataContext is ViewPatchViewModel { Strings: var strings })
        {
            patchesGrid.Columns[0].Header = strings.FileNameColumn.Text;
            patchesGrid.Columns[1].Header = strings.ChangeColumn.Text;
            patchesGrid.Columns[2].Header = strings.TypeColumn.Text;
        }
    }
}
