using Avalonia.Controls;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>
///  The splitters of the main window saved when it closes and restored when it opens (the <c>SplitterManager</c> of
///  <c>FormBrowse</c>, with the names of its <c>SplitContainer</c>s): the width of the left panel and whether it is shown, the
///  height of the tabs and of the output history panel, and the width of the file lists of the diff and file tree tabs.
/// </summary>
public partial class BrowseWindow
{
    private const string MainSplitter = "MainSplitContainer";
    private const string RightSplitter = "RightSplitContainer";
    private const string LeftSplitter = "LeftSplitContainer";
    private const string DiffSplitter = "revisionDiff.DiffSplitContainer";
    private const string TreeSplitter = "fileTree.DiffSplitContainer";

    // As MainSplitContainer.Panel1Collapsed: whether the left panel is shown, also for the panels of other repositories.
    private bool _leftPanelShown = true;

    private int Dpi => (int)Math.Round(96 * RenderScaling);

    // As SplitterManager.RestoreSplitters: the first panel keeps its size (the second one for the tabs and the output history).
    private void RestoreSplitters(BrowseViewModel viewModel)
    {
        if (viewModel.GetSplitter(MainSplitter) is { } main)
        {
            if (main.Distance > 0)
            {
                _leftPanelWidth = new GridLength(ToDip(main.Distance, main.Dpi));
            }

            _leftPanelShown = !main.Panel1Collapsed;
        }

        if (SecondPanelSize(viewModel.GetSplitter(RightSplitter)) is double tabsHeight)
        {
            _tabsHeight = tabsHeight;
            if (contentGrid.RowDefinitions[2].Height.Value > 0)
            {
                contentGrid.RowDefinitions[2].Height = new GridLength(tabsHeight);
            }
        }

        if (SecondPanelSize(viewModel.GetSplitter(LeftSplitter)) is double outputHeight)
        {
            _outputPanelHeight = new GridLength(outputHeight);
        }

        RestoreFirstColumn(diffPanel, viewModel.GetSplitter(DiffSplitter));
        RestoreFirstColumn(treePanel, viewModel.GetSplitter(TreeSplitter));
        ApplyLeftPanelShown();
        UpdateLeftPanelColumn();

        void RestoreFirstColumn(Grid grid, SplitterPosition? position)
        {
            if (position is { Distance: > 0 })
            {
                grid.ColumnDefinitions[0].Width = new GridLength(ToDip(position.Distance, position.Dpi));
            }
        }

        static double? SecondPanelSize(SplitterPosition? position)
            => position is { Distance: > 0 } && position.Size > position.Distance ? ToDip(position.Size - position.Distance, position.Dpi) : null;
    }

    // As SplitterManager.SaveSplitters (FormBrowse.OnFormClosing).
    private void SaveSplitters(BrowseViewModel viewModel)
    {
        int dpi = Dpi;
        double leftPanelWidth = mainSplit.ColumnDefinitions[0].Width.Value > 0 ? mainSplit.ColumnDefinitions[0].Width.Value : _leftPanelWidth.Value;
        viewModel.SaveSplitter(MainSplitter, new(ToPixels(leftPanelWidth), ToPixels(mainSplit.Bounds.Width), dpi, Panel1Collapsed: !_leftPanelShown));

        RowDefinition tabsRow = contentGrid.RowDefinitions[2];
        double tabsHeight = tabsRow.Height.Value > 0 ? tabsRow.Height.Value : _tabsHeight;
        viewModel.SaveSplitter(RightSplitter, SecondPanel(contentGrid.Bounds.Height, tabsHeight));

        RowDefinition outputRow = leftColumn.RowDefinitions[2];
        double outputHeight = outputRow.Height.IsAbsolute && outputRow.Height.Value > 0 ? outputRow.Height.Value : _outputPanelHeight.Value;
        viewModel.SaveSplitter(LeftSplitter, SecondPanel(leftColumn.Bounds.Height, outputHeight));

        viewModel.SaveSplitter(DiffSplitter, new(ToPixels(diffPanel.ColumnDefinitions[0].Width.Value), ToPixels(diffPanel.Bounds.Width), dpi));
        viewModel.SaveSplitter(TreeSplitter, new(ToPixels(treePanel.ColumnDefinitions[0].Width.Value), ToPixels(treePanel.Bounds.Width), dpi));

        SplitterPosition SecondPanel(double size, double secondPanelSize)
            => new(ToPixels(Math.Max(0, size - secondPanelSize)), ToPixels(size), dpi);
    }

    // The left panel of a repository shown as the window keeps it.
    private void ApplyLeftPanelShown()
    {
        if (_leftPanelViewModel is { } leftPanel && leftPanel.IsVisible != _leftPanelShown)
        {
            leftPanel.IsVisible = _leftPanelShown;
        }
    }

    private int ToPixels(double dip) => (int)Math.Round(dip * RenderScaling);

    private static double ToDip(int pixels, int dpi) => pixels * 96.0 / (dpi > 0 ? dpi : 96);
}
