using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using GitUI.Avalonia.Controls.LeftPanel;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.UserControls.LeftPanel;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>
///  The left panel of the main window (<c>RepoObjectsTree</c> in <c>MainSplitContainer.Panel1</c>), in the first column; hidden
///  with its toggle, and without a <see cref="BrowseViewModel.LeftPanel"/> (e.g. in the dashboard).
/// </summary>
public partial class BrowseWindow
{
    private BrowseViewModel? _leftPanelOwner;
    private LeftPanelViewModel? _leftPanelViewModel;
    private GridLength _leftPanelWidth = new(260);
    private GridLength _outputPanelHeight = new(200);

    public LeftPanelView LeftPanel => leftPanel;

    public ToggleButton ToggleLeftPanelButton => toggleLeftPanel;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DataContextProperty)
        {
            _leftPanelOwner?.PropertyChanged -= OnBrowseViewModelPropertyChanged;
            _leftPanelOwner = DataContext as BrowseViewModel;
            _leftPanelOwner?.PropertyChanged += OnBrowseViewModelPropertyChanged;
            UpdateLeftPanel();
        }
    }

    private void OnBrowseViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BrowseViewModel.LeftPanel))
        {
            UpdateLeftPanel();
        }
        else if (e.PropertyName == nameof(BrowseViewModel.ShowOutputHistoryPanel))
        {
            UpdateLeftPanelColumn();
        }
    }

    private void UpdateLeftPanel()
    {
        _leftPanelViewModel?.PropertyChanged -= OnLeftPanelPropertyChanged;
        _leftPanelViewModel = _leftPanelOwner?.LeftPanel;
        _leftPanelViewModel?.PropertyChanged += OnLeftPanelPropertyChanged;
        ApplyLeftPanelShown();
        UpdateLeftPanelColumn();
    }

    private void OnLeftPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LeftPanelViewModel.IsVisible))
        {
            _leftPanelShown = _leftPanelViewModel!.IsVisible;
            UpdateLeftPanelColumn();
        }
    }

    /// <summary>As <c>MainSplitContainer.Panel1Collapsed</c>: a hidden left panel takes no space, and keeps its width.</summary>
    private void UpdateLeftPanelColumn()
    {
        bool leftPanelVisible = _leftPanelViewModel?.IsVisible is true;
        bool outputVisible = _leftPanelOwner?.ShowOutputHistoryPanel is true;

        // As LeftSplitContainer: the output history panel below the left panel, or alone with all the height.
        RowDefinition outputRow = leftColumn.RowDefinitions[2];
        if (outputRow.Height.IsAbsolute && outputRow.Height.Value > 0)
        {
            _outputPanelHeight = outputRow.Height;
        }

        leftColumn.RowDefinitions[0].Height = leftPanelVisible ? GridLength.Star : new GridLength(0);
        outputRow.Height = !outputVisible ? new GridLength(0) : leftPanelVisible ? _outputPanelHeight : GridLength.Star;
        outputHistoryPanel.IsVisible = outputVisible;
        outputHistoryPanelSplitter.IsVisible = outputVisible && leftPanelVisible;

        bool visible = leftPanelVisible || outputVisible;
        ColumnDefinition column = mainSplit.ColumnDefinitions[0];
        if (!visible && column.Width.Value > 0)
        {
            _leftPanelWidth = column.Width;
        }

        column.Width = visible ? _leftPanelWidth : new GridLength(0);
        leftPanel.IsVisible = leftPanelVisible;
        leftPanelSplitter.IsVisible = visible;
    }
}
