using Avalonia.Threading;
using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.FindLargeFiles;

/// <summary>Avalonia port of <c>FindLargeFilesForm</c>; behaviour lives in <c>FindLargeFilesViewModel</c>.</summary>
public partial class FindLargeFilesWindow : DialogWindow
{
    public FindLargeFilesWindow()
    {
        InitializeComponent();

        // As OnLoad: the search starts once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as FindLargeFilesViewModel)?.SearchAsync());
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Column headers are not in the logical tree, so they are not bound.
        if (DataContext is FindLargeFilesViewModel { Strings: var strings })
        {
            objectsGrid.Columns[0].Header = strings.ShaColumn.Text;
            objectsGrid.Columns[1].Header = strings.PathColumn.Text;
            objectsGrid.Columns[2].Header = strings.SizeColumn.Text;
            objectsGrid.Columns[3].Header = strings.CompressedSizeColumn.Text;
            objectsGrid.Columns[4].Header = strings.CommitCountColumn.Text;
            objectsGrid.Columns[5].Header = strings.LastCommitDateColumn.Text;
            objectsGrid.Columns[6].Header = strings.DeleteColumn.Text;
        }
    }
}
