using Avalonia.Input;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormSubmodules</c>.</summary>
public partial class SubmodulesWindow : DialogWindow
{
    public SubmodulesWindow()
    {
        InitializeComponent();

        // As FormSubmodules.FormSubmodulesShown: load once shown, which shows a wait cursor while loading.
        Opened += (_, _) => (DataContext as SubmodulesViewModel)?.Reload();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Column headers are not in the logical tree, so they are not bound.
        if (DataContext is SubmodulesViewModel { Strings: var strings } viewModel)
        {
            submodulesGrid.Columns[0].Header = strings.NameColumn.Text;
            submodulesGrid.Columns[1].Header = strings.StatusColumn.Text;
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SubmodulesViewModel.IsLoading))
                {
                    Cursor = viewModel.IsLoading ? new Cursor(StandardCursorType.AppStarting) : Cursor.Default;
                }
            };
        }
    }
}
