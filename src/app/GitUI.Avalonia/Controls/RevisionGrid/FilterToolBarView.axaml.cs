using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitUI.Avalonia.Controls.RevisionGrid;

/// <summary>The branch and text filters of the revision grid of the main window (port of <c>FilterToolBar</c>).</summary>
public partial class FilterToolBarView : UserControl
{
    public FilterToolBarView()
    {
        InitializeComponent();

        // As tsbtnAdvancedFilter_ButtonClick: without a filter the dialog opens, else the menu of the button.
        advancedFilterButton.Click += (_, e) =>
        {
            if (ViewModel is not { } viewModel)
            {
                return;
            }

            if (viewModel.OpensAdvancedFilterMenu)
            {
                advancedFilterButton.Flyout?.ShowAt(advancedFilterButton);
            }
            else
            {
                viewModel.ShowAdvancedFilterCommand.Execute(null);
            }

            e.Handled = true;
        };

        // As tscboBranchFilter_DropDown: the refs matching the filter are listed when it drops down.
        branchFilterBox.DropDownOpened += (_, _) => _ = ViewModel?.UpdateBranchItemsAsync();

        // As the KeyUp handlers: Enter applies the filter.
        branchFilterBox.AddHandler(KeyDownEvent, (_, e) => OnEnter(e, vm => vm.ApplyBranchFilter()), RoutingStrategies.Tunnel);
        revisionFilterBox.AddHandler(KeyDownEvent, (_, e) => OnEnter(e, vm => vm.ApplyRevisionFilter()), RoutingStrategies.Tunnel);
    }

    public ComboBox BranchFilterBox => branchFilterBox;

    public ComboBox RevisionFilterBox => revisionFilterBox;

    private FilterToolBarViewModel? ViewModel => DataContext as FilterToolBarViewModel;

    /// <summary>As <c>SetFocus</c>: the text filter, or the branch filter if the text filter has the focus.</summary>
    public void FocusFilter()
    {
        if (revisionFilterBox.IsKeyboardFocusWithin)
        {
            branchFilterBox.Focus();
        }
        else
        {
            revisionFilterBox.Focus();
        }
    }

    private void OnEnter(KeyEventArgs e, Action<FilterToolBarViewModel> apply)
    {
        if (e.Key == Key.Enter && ViewModel is { } viewModel)
        {
            apply(viewModel);
            e.Handled = true;
        }
    }
}
