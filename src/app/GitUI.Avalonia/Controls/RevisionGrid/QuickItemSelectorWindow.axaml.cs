using Avalonia.Input;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitUI.Avalonia.Controls.RevisionGrid;

/// <summary>
///  Avalonia port of <c>FormQuickItemSelector</c> (<c>FormQuickGitRefSelector</c>, <c>FormQuickStringSelector</c>): a borderless
///  picker shown under the selected revision.
/// </summary>
public partial class QuickItemSelectorWindow : DialogWindow
{
    public QuickItemSelectorWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            itemsListBox.Focus();
            if (itemsListBox.SelectedItem is { } item)
            {
                itemsListBox.ScrollIntoView(item);
            }
        };

        // As lbxRefs_MouseDoubleClick: a double click accepts the item; so does Enter (the AcceptButton), which the list
        // would otherwise handle itself.
        itemsListBox.DoubleTapped += (_, _) => Accept();
        itemsListBox.AddHandler(
            KeyDownEvent,
            (_, e) =>
            {
                if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
                {
                    e.Handled = true;
                    Accept();
                }
            },
            global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    public global::Avalonia.Controls.ListBox ItemsListBox => itemsListBox;

    private void Accept()
    {
        if (DataContext is QuickItemSelectorViewModel viewModel && viewModel.AcceptCommand.CanExecute(null))
        {
            viewModel.AcceptCommand.Execute(null);
        }
    }
}
