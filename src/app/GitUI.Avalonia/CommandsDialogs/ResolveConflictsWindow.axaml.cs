using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormResolveConflicts</c>.</summary>
public partial class ResolveConflictsWindow : DialogWindow
{
    private ResolveConflictsViewModel? _viewModel;

    public ResolveConflictsWindow()
    {
        InitializeComponent();

        conflictsGrid.SelectionChanged += (_, _) =>
        {
            if (_viewModel is not null)
            {
                _viewModel.SelectedConflicts = [.. conflictsGrid.SelectedItems.OfType<ConflictItem>()];
            }
        };

        // As ConflictedFiles_DoubleClick and ConflictedFiles_KeyDown: the merge tool opens on the selected files.
        conflictsGrid.DoubleTapped += (_, e) =>
        {
            if ((e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true) is not null)
            {
                _viewModel?.MergeCommand.Execute(null);
            }
        };
        conflictsGrid.AddHandler(KeyDownEvent, OnGridKeyDown, RoutingStrategies.Tunnel);

        // As ConflictedFiles_CellMouseDown: a right click selects the row under the mouse, unless several rows are selected.
        conflictsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);

        // As ConflictedFilesContextMenu_Opening: the menu does not open without a selected file.
        conflictsContextMenu.Opening += (_, e) => e.Cancel = _viewModel?.HasSelection != true;

        // As merge.ContextMenuStrip: the merge button has the menu of the list too.
        mergeButton.ContextRequested += (_, e) =>
        {
            e.Handled = true;
            if (_viewModel?.HasSelection == true)
            {
                conflictsContextMenu.Open(mergeButton);
            }
        };

        // As OnRuntimeLoad: the merge button gets the focus, then the merge tool and the conflicts are loaded.
        Opened += (_, _) =>
        {
            mergeButton.Focus();
            Dispatcher.UIThread.Post(() => _viewModel?.InitializeView());
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as ResolveConflictsViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Column headers are not in the logical tree, so they are not bound.
            conflictsGrid.Columns[0].Header = _viewModel.Strings.FileNameHeader.Text;
            UpdateCustomMergeTools();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResolveConflictsViewModel.CustomMergeTools))
        {
            UpdateCustomMergeTools();
        }
    }

    /// <summary>
    ///  As <c>CustomDiffMergeToolProvider.LoadCustomDiffMergeTools</c>: with several custom merge tools, "Open in mergetool" lists them,
    ///  the first (the default) in bold; otherwise the item itself opens the default tool.
    /// </summary>
    private void UpdateCustomMergeTools()
    {
        customMergeToolMenuItem.Items.Clear();
        if (_viewModel is not { HasCustomMergeTools: true } viewModel)
        {
            return;
        }

        foreach (string tool in viewModel.CustomMergeTools)
        {
            customMergeToolMenuItem.Items.Add(new MenuItem
            {
                Header = tool,
                Command = viewModel.CustomMergeToolCommand,
                CommandParameter = tool,
                FontWeight = customMergeToolMenuItem.Items.Count == 0 ? FontWeight.Bold : FontWeight.Normal,
            });
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && _viewModel is not null)
        {
            e.Handled = true;
            _viewModel.MergeCommand.Execute(null);
        }
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(conflictsGrid).Properties.IsRightButtonPressed || conflictsGrid.SelectedItems.Count > 1)
        {
            return;
        }

        if ((e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true) is { DataContext: ConflictItem item })
        {
            conflictsGrid.SelectedItem = item;
        }
    }
}
