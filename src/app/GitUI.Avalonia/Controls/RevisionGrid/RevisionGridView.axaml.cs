using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;

namespace GitUI.Avalonia.Controls.RevisionGrid;

/// <summary>
///  The Avalonia revision grid (port of <c>RevisionGridControl</c>'s grid; docs/avalonia-port/PLAN.md, phase 4):
///  the graph, the references and subject, the author, the date and the commit id of each revision.
/// </summary>
public partial class RevisionGridView : UserControl
{
    public static readonly StyledProperty<RevisionGraph?> GraphProperty =
        AvaloniaProperty.Register<RevisionGridView, RevisionGraph?>(nameof(Graph));

    public static readonly StyledProperty<RevisionGraphRenderer?> RendererProperty =
        AvaloniaProperty.Register<RevisionGridView, RevisionGraphRenderer?>(nameof(Renderer));

    private RevisionGridViewModel? _viewModel;
    private int _maxLaneCount = 1;
    private int _laneCountScannedTo;
    private readonly DispatcherTimer _quickSearchTimer = new();

    public RevisionGridView()
    {
        InitializeComponent();

        revisionsGrid.LoadingRow += (_, e) => _viewModel?.EnsureGraphCached(e.Row.Index + 50);
        revisionsGrid.DoubleTapped += (_, e) => ActivateSelected(e.Source);

        // Before the grid, which moves to the next row on Enter.
        revisionsGrid.AddHandler(KeyDownEvent, OnGridKeyDown, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        revisionsGrid.HeadersVisibility = DataGridHeadersVisibility.None;
        revisionsGrid.SelectionChanged += (_, _) => _viewModel?.SetSelectedRows(revisionsGrid.SelectedItems.OfType<RevisionGridRow>());

        // As mainContextMenu: the menu of the selected revisions, built when opening; a right click selects the row first.
        revisionsGrid.ContextRequested += OnContextRequested;
        revisionsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // Quick search (as RevisionGridControl with QuickSearchProvider).
        revisionsGrid.AddHandler(TextInputEvent, OnGridTextInput, handledEventsToo: true);
        _quickSearchTimer.Tick += (_, _) =>
        {
            _quickSearchTimer.Stop();
            _viewModel?.HideQuickSearch();
        };
    }

    /// <summary>Raised when a revision is double clicked or Enter is pressed on it.</summary>
    public event EventHandler<RevisionGridRow>? RevisionActivated;

    public RevisionGraph? Graph
    {
        get => GetValue(GraphProperty);
        private set => SetValue(GraphProperty, value);
    }

    public RevisionGraphRenderer? Renderer
    {
        get => GetValue(RendererProperty);
        private set => SetValue(RendererProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Renderer = CreateRenderer();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.QuickSearchRestarted -= OnQuickSearchRestarted;
        _viewModel = DataContext as RevisionGridViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.QuickSearchRestarted += OnQuickSearchRestarted;
        Graph = _viewModel?.Graph;
        UpdateColumns();
        revisionsGrid.SelectionMode = _viewModel?.MultiSelect == true ? DataGridSelectionMode.Extended : DataGridSelectionMode.Single;
        _maxLaneCount = 1;
        _laneCountScannedTo = 0;
        base.OnDataContextChanged(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RevisionGridViewModel.CachedGraphRowCount):
                UpdateGraphColumnWidth();
                foreach (RevisionGraphCell cell in revisionsGrid.GetVisualDescendants().OfType<RevisionGraphCell>())
                {
                    cell.InvalidateVisual();
                }

                break;

            case nameof(RevisionGridViewModel.ShowGraphColumn) or nameof(RevisionGridViewModel.ShowAuthorColumn)
                or nameof(RevisionGridViewModel.ShowDateColumn) or nameof(RevisionGridViewModel.ShowIdColumn):
                UpdateColumns();
                break;

            case nameof(RevisionGridViewModel.SelectedRow) when _viewModel?.SelectedRow is { } row:
                revisionsGrid.ScrollIntoView(row, null);
                break;
        }
    }

    /// <summary>Widens the graph column to the lanes laid out so far (the WinForms column also grows with them).</summary>
    private void UpdateGraphColumnWidth()
    {
        if (Graph is not { } graph || _viewModel is null)
        {
            return;
        }

        if (_viewModel.CachedGraphRowCount < _laneCountScannedTo)
        {
            // Reloaded.
            _maxLaneCount = 1;
            _laneCountScannedTo = 0;
        }

        for (int index = _laneCountScannedTo; index < _viewModel.CachedGraphRowCount; index++)
        {
            if (graph.GetSegmentsForRow(index) is { } row)
            {
                _maxLaneCount = Math.Max(_maxLaneCount, row.GetLaneCount());
            }
        }

        _laneCountScannedTo = _viewModel.CachedGraphRowCount;

        revisionsGrid.Columns[0].Width = new DataGridLength(RevisionGraphRenderer.GetWidth(_maxLaneCount) + 4);
    }

    /// <summary>The context menu opened last, e.g. for tests.</summary>
    public ContextMenu? LastContextMenu { get; private set; }

    /// <summary>Opens the context menu of the selected revisions (as the menu key or a right click).</summary>
    public void OpenContextMenu()
    {
        if (_viewModel?.ContextMenuProvider is not { } provider)
        {
            return;
        }

        LastContextMenu = MenuModelRenderer.CreateContextMenu(provider());
        LastContextMenu.Open(revisionsGrid);
    }

    // The columns, as the column visibility of RevisionGridControl (the graph, author name, date and id columns).
    private void UpdateColumns()
    {
        if (_viewModel is null)
        {
            return;
        }

        revisionsGrid.Columns[0].IsVisible = _viewModel.ShowGraphColumn;
        revisionsGrid.Columns[2].IsVisible = _viewModel.ShowAuthorColumn;
        revisionsGrid.Columns[3].IsVisible = _viewModel.ShowDateColumn;
        revisionsGrid.Columns[4].IsVisible = _viewModel.ShowIdColumn;
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (_viewModel?.ContextMenuProvider is null || ContextMenu is not null)
        {
            // The menu set by the window (e.g. the file history), if any.
            return;
        }

        e.Handled = true;
        if (_viewModel.SelectedRow is not null)
        {
            OpenContextMenu();
        }
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPointProperties properties = e.GetCurrentPoint(revisionsGrid).Properties;
        if (properties.IsXButton1Pressed || properties.IsXButton2Pressed)
        {
            // As OnGridViewMouseClick: the mouse back and forward buttons navigate.
            e.Handled = true;
            if (properties.IsXButton1Pressed)
            {
                _viewModel?.NavigateBackward();
            }
            else
            {
                _viewModel?.NavigateForward();
            }

            return;
        }

        if (properties.IsRightButtonPressed
            && (e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true)?.DataContext is RevisionGridRow row
            && !revisionsGrid.SelectedItems.Contains(row))
        {
            revisionsGrid.SelectedItem = row;
        }
    }

    private void OnQuickSearchRestarted(object? sender, EventArgs e)
    {
        _quickSearchTimer.Stop();
        _quickSearchTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, _viewModel?.QuickSearchTimeout ?? 4000));
        _quickSearchTimer.Start();
    }

    private void OnGridTextInput(object? sender, TextInputEventArgs e)
    {
        if (_viewModel is not null && !string.IsNullOrEmpty(e.Text) && !e.Text.Any(char.IsControl))
        {
            e.Handled = true;
            _viewModel.QuickSearchType(e.Text);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key, e.KeyModifiers)
        {
            case (Key.Enter, KeyModifiers.None):
                e.Handled = true;
                ActivateSelected(null);
                break;

            case (Key.Back, KeyModifiers.None) when _viewModel?.IsQuickSearchVisible == true:
                e.Handled = true;
                _viewModel.QuickSearchBackspace();
                break;

            case (Key.Escape, KeyModifiers.None) when _viewModel?.IsQuickSearchVisible == true:
                e.Handled = true;
                _viewModel.HideQuickSearch();
                break;

            case (Key.Down, KeyModifiers.Alt) or (Key.Up, KeyModifiers.Alt) when _viewModel is not null:
                e.Handled = true;
                _viewModel.QuickSearchNext(down: e.Key == Key.Down);
                break;

            case (Key.V, KeyModifiers.Control) when _viewModel is not null:
                e.Handled = true;
                _ = PasteIntoQuickSearchAsync(_viewModel);
                break;
        }
    }

    private async Task PasteIntoQuickSearchAsync(RevisionGridViewModel viewModel)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard && await clipboard.TryGetTextAsync() is { Length: > 0 } text)
        {
            viewModel.QuickSearchPaste(text);
        }
    }

    private void ActivateSelected(object? source)
    {
        if (source is Visual visual && visual.FindAncestorOfType<DataGridRow>(includeSelf: true) is null)
        {
            // Not on a row, e.g. on the scroll bar.
            return;
        }

        if (_viewModel?.SelectedRow is { } row)
        {
            RevisionActivated?.Invoke(this, row);
        }
    }

    /// <summary>The lane brushes of the theme, chosen as the WinForms <c>RevisionGraphLaneColor</c> does.</summary>
    private RevisionGraphRenderer CreateRenderer()
    {
        List<IBrush> laneBrushes = [];
        HashSet<Color> seen = [];
        foreach (string name in Enum.GetNames<AppColor>().Where(name => name.StartsWith(nameof(AppColor.GraphBranch1)[..^1], StringComparison.Ordinal)))
        {
            if (GetColor(name) is { } color && seen.Add(color))
            {
                laneBrushes.Add(new SolidColorBrush(color));
            }
        }

        const int minBranchColors = 4;
        if (laneBrushes.Count < minBranchColors)
        {
            laneBrushes = [Brushes.Cyan, Brushes.Magenta, Brushes.Yellow, Brushes.Lime];
        }

        IBrush nonRelative = GetColor(nameof(AppColor.GraphNonRelativeBranch)) is { } nonRelativeColor ? new SolidColorBrush(nonRelativeColor) : Brushes.LightGray;
        IBrush outline = this.TryFindResource("SystemControlForegroundBaseHighBrush", ActualThemeVariant, out object? foreground) && foreground is IBrush brush
            ? brush
            : Brushes.Black;
        return new RevisionGraphRenderer(laneBrushes, nonRelative, outline);

        Color? GetColor(string appColorName)
        {
            // The theme's color, as the host provides it; the default color otherwise (e.g. in tests).
            if (this.TryFindResource(ThemeColors.AppColorPrefix + appColorName, ActualThemeVariant, out object? resource) && resource is ISolidColorBrush themed)
            {
                return themed.Color.A == 0 ? null : themed.Color;
            }

            System.Drawing.Color fallback = AppColorDefaults.GetBy(Enum.Parse<AppColor>(appColorName));
            return fallback.IsEmpty ? null : Color.FromArgb(fallback.A, fallback.R, fallback.G, fallback.B);
        }
    }
}

/// <summary>Converters choosing the class of a reference "capsule" by its kind.</summary>
public sealed class RevisionRefKindConverter(RevisionRefKind kind) : IValueConverter
{
    public static RevisionRefKindConverter Branch { get; } = new(RevisionRefKind.Branch);

    public static RevisionRefKindConverter RemoteBranch { get; } = new(RevisionRefKind.RemoteBranch);

    public static RevisionRefKindConverter Tag { get; } = new(RevisionRefKind.Tag);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is RevisionRefKind k && k == kind;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
